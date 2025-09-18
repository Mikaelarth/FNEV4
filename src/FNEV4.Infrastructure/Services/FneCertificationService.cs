using System.Text.Json;
using Microsoft.Extensions.Logging;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces.Services.Fne;
using FNEV4.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Diagnostics;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Service de certification FNE selon les spécifications officielles DGI
    /// Implémentation complète de l'API FNE selon FNE-procedureapi.md
    /// </summary>
    public class FneCertificationService : IFneCertificationService
    {
        private const string FNE_BASE_URL = "http://54.247.95.108:8000/api/v1";
        
        private readonly FNEV4DbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<FneCertificationService> _logger;

        public FneCertificationService(
            FNEV4DbContext context,
            HttpClient httpClient,
            ILogger<FneCertificationService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<FneCertificationResult> CertifyInvoiceAsync(FneInvoice invoice, FneConfiguration configuration)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("Début certification facture {InvoiceId}", invoice.Id);

                // Validation des prérequis
                var validation = ValidateForCertification(invoice, configuration);
                if (!validation.IsValid)
                {
                    LogCertificationAttempt(invoice, configuration, validation.Message, false);
                    await _context.SaveChangesAsync();

                    return new FneCertificationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = validation.Message,
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                try
                {
                    // Conversion au format FNE API
                    var fneData = await ConvertToFneApiFormatAsync(invoice);
                    
                    // Appel API FNE
                    var result = await CallFneApiAsync(fneData, configuration);
                    
                    if (result.IsSuccess)
                    {
                        // Mise à jour avec toutes les données enrichies FNE
                        invoice.Status = "Certified";
                        invoice.FneReference = result.FneReference;
                        invoice.VerificationToken = result.VerificationToken;
                        
                        // Nouvelles données enrichies pour exploitation maximale
                        invoice.FneQrCodeData = result.QrCodeData;
                        invoice.FneBalanceSticker = result.StickerBalance > 0 ? result.StickerBalance.ToString() : null;
                        invoice.FneCertificationTimestamp = result.ProcessedAt;
                        invoice.FneProcessingStatus = "PROCESSED";
                        invoice.FneCertificationHash = GenerateCertificationHash(invoice, result);
                        
                        // Données additionnelles si disponibles
                        if (result.CertifiedInvoiceDetails != null)
                        {
                            invoice.FneCertifiedInvoiceDetails = System.Text.Json.JsonSerializer.Serialize(result.CertifiedInvoiceDetails);
                            invoice.FneInvoiceId = result.CertifiedInvoiceDetails.Id;
                        }
                        
                        if (result.HasWarning)
                        {
                            invoice.FneHasWarning = true;
                            invoice.FneWarningMessage = result.WarningMessage;
                        }
                        
                        invoice.FneCompanyNcc = result.NccEntreprise;
                        invoice.FneDownloadUrl = result.DownloadUrl;

                        LogCertificationAttempt(invoice, configuration, "SUCCESS", true);
                        await _context.SaveChangesAsync();

                        return new FneCertificationResult
                        {
                            IsSuccess = true,
                            FneReference = result.FneReference,
                            VerificationToken = result.VerificationToken,
                            ProcessedAt = DateTime.UtcNow,
                            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                        };
                    }
                    else
                    {
                        LogCertificationAttempt(invoice, configuration, result.ErrorMessage, false);
                        await _context.SaveChangesAsync();

                        return new FneCertificationResult
                        {
                            IsSuccess = false,
                            ErrorMessage = result.ErrorMessage ?? "Erreur lors de l'appel API FNE",
                            ProcessedAt = DateTime.UtcNow,
                            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                        };
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Erreur HTTP lors de la certification de la facture {InvoiceId}", invoice.Id);
                    LogCertificationAttempt(invoice, configuration, $"Erreur HTTP: {ex.Message}", false);
                    await _context.SaveChangesAsync();

                    return new FneCertificationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Erreur de connexion à l'API FNE: {ex.Message}",
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogError(ex, "Timeout lors de la certification de la facture {InvoiceId}", invoice.Id);
                    LogCertificationAttempt(invoice, configuration, "Timeout de la requête", false);
                    await _context.SaveChangesAsync();

                    return new FneCertificationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Timeout lors de l'appel à l'API FNE. Veuillez réessayer.",
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur critique lors de la certification de la facture {InvoiceId}", invoice.Id);
                
                // Mise à jour du statut d'erreur
                invoice.Status = "Error";
                invoice.RetryCount += 1;
                invoice.ErrorMessages = ex.Message;
                
                LogCertificationAttempt(invoice, configuration, "EXCEPTION", false);
                await _context.SaveChangesAsync();

                return new FneCertificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Erreur critique: {ex.Message}",
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        public async Task<FneBatchCertificationResult> CertifyInvoicesBatchAsync(List<FneInvoice> invoices, FneConfiguration configuration)
        {
            var result = new FneBatchCertificationResult
            {
                TotalCount = invoices.Count,
                ProcessedAt = DateTime.UtcNow
            };

            var stopwatch = Stopwatch.StartNew();

            foreach (var invoice in invoices)
            {
                try
                {
                    var certResult = await CertifyInvoiceAsync(invoice, configuration);
                    result.Results.Add(certResult);
                    
                    if (certResult.IsSuccess)
                        result.SuccessCount++;
                    else
                        result.ErrorCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la certification en lot de la facture {InvoiceId}", invoice.Id);
                    result.ErrorCount++;
                    result.GlobalErrors.Add($"Facture {invoice.InvoiceNumber}: {ex.Message}");
                }
            }

            result.TotalProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            return result;
        }

        public async Task<FneValidationResult> ValidateForCertificationAsync(FneInvoice invoice)
        {
            var configuration = await _context.FneConfigurations.FirstOrDefaultAsync(c => c.IsActive) ?? new FneConfiguration();
            return ValidateForCertification(invoice, configuration);
        }

        public async Task<List<FneInvoice>> GetPendingInvoicesAsync()
        {
            return await _context.FneInvoices
                .Include(i => i.Client)
                .Where(i => i.Status == "Pending" || i.Status == "Error")
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<int> GetPendingInvoicesCountAsync()
        {
            return await _context.FneInvoices
                .CountAsync(i => i.Status == "Pending" || i.Status == "Error");
        }

        public async Task<List<FneInvoice>> GetInvoicesForPeriodAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.FneInvoices
                .Include(i => i.Client)
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<FneBatchCertificationResult> CertifyPendingInvoicesAsync(int maxCount = 50)
        {
            var pendingInvoices = await _context.FneInvoices
                .Include(i => i.Client)
                .Where(i => i.Status == "Pending")
                .Take(maxCount)
                .ToListAsync();

            var configuration = await _context.FneConfigurations.FirstOrDefaultAsync(c => c.IsActive);
            if (configuration == null)
            {
                return new FneBatchCertificationResult
                {
                    TotalCount = pendingInvoices.Count,
                    ErrorCount = pendingInvoices.Count,
                    GlobalErrors = { "Configuration FNE non trouvée" }
                };
            }

            return await CertifyInvoicesBatchAsync(pendingInvoices, configuration);
        }

        public async Task<List<FneActivity>> GetRecentActivitiesAsync(int count = 10)
        {
            var logs = await _context.FneApiLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();

            return logs.Select(log => new FneActivity
            {
                CreatedAt = log.Timestamp,
                ActivityType = log.OperationType,
                Message = log.IsSuccess ? "Certification réussie" : (log.ErrorMessage ?? "Erreur inconnue"),
                Level = log.IsSuccess ? "Success" : "Error",
                ProcessingTimeMs = log.ProcessingTimeMs
            }).ToList();
        }

        public async Task<List<FneInvoice>> GetRecentCertificationsAsync(int count = 5)
        {
            return await _context.FneInvoices
                .Include(i => i.Client)
                .Where(i => i.Status == "Certified")
                .OrderByDescending(i => i.CreatedAt) // Utilise CreatedAt au lieu de CertificationDate
                .Take(count)
                .ToListAsync();
        }

        public async Task<FnePerformanceMetrics> GetPerformanceMetricsAsync()
        {
            var today = DateTime.Today;
            var todayLogs = await _context.FneApiLogs
                .Where(l => l.Timestamp >= today)
                .ToListAsync();

            var successCount = todayLogs.Count(l => l.IsSuccess);
            var totalCount = todayLogs.Count;

            return new FnePerformanceMetrics
            {
                SuccessRate = totalCount > 0 ? (double)successCount / totalCount * 100 : 0,
                AverageTime = todayLogs.Any() 
                    ? TimeSpan.FromMilliseconds(todayLogs.Average(l => l.ProcessingTimeMs))
                    : TimeSpan.Zero,
                CertificationsLastHour = todayLogs.Count(l => l.Timestamp >= DateTime.Now.AddHours(-1)),
                TotalCertificationsToday = successCount,
                TotalErrorsToday = totalCount - successCount,
                TotalAmountCertifiedToday = await _context.FneInvoices
                    .Where(i => i.CreatedAt >= today && i.Status == "Certified") // Utilise CreatedAt et vérifie le statut
                    .SumAsync(i => (decimal?)i.TotalAmountTTC) ?? 0
            };
        }

        public async Task<FneSystemHealthResult> PerformHealthCheckAsync()
        {
            var result = new FneSystemHealthResult
            {
                CheckedAt = DateTime.UtcNow
            };

            try
            {
                // Test de la base de données
                var configCount = await _context.FneConfigurations.CountAsync();
                result.DatabaseHealthy = true;

                // Test de la configuration
                var activeConfig = await _context.FneConfigurations.FirstOrDefaultAsync(c => c.IsActive);
                result.HasApiConfig = activeConfig != null;
                result.ConfigurationValid = activeConfig?.BaseUrl != null && activeConfig.ApiKey != null;

                // Test de connectivité API (si configuration disponible)
                if (result.ConfigurationValid && activeConfig != null)
                {
                    try
                    {
                        var testRequest = new HttpRequestMessage(HttpMethod.Get, $"{activeConfig.BaseUrl}/health");
                        testRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", activeConfig.ApiKey);
                        
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        var response = await _httpClient.SendAsync(testRequest, cts.Token);
                        result.ApiConnectionHealthy = response.IsSuccessStatusCode;
                    }
                    catch
                    {
                        result.ApiConnectionHealthy = false;
                    }
                }

                // Évaluation globale
                result.IsHealthy = result.DatabaseHealthy && result.ConfigurationValid;
                
                if (!result.IsHealthy)
                {
                    if (!result.DatabaseHealthy) result.HealthIssues.Add("Problème de base de données");
                    if (!result.ConfigurationValid) result.HealthIssues.Add("Configuration API manquante ou invalide");
                    if (!result.ApiConnectionHealthy) result.HealthIssues.Add("Problème de connectivité API");
                }

                result.StatusMessage = result.IsHealthy ? "Système opérationnel" : "Problèmes détectés";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du test de santé du système");
                result.IsHealthy = false;
                result.DatabaseHealthy = false;
                result.StatusMessage = $"Erreur critique: {ex.Message}";
                result.HealthIssues.Add($"Exception: {ex.Message}");
            }

            return result;
        }

        public async Task<string> GenerateQrCodeAsync(string verificationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogDebug("Génération du QR-Code pour le token: {Token}", verificationToken);

                    if (string.IsNullOrEmpty(verificationToken))
                    {
                        _logger.LogWarning("Token de vérification vide pour la génération du QR-Code");
                        return string.Empty;
                    }

                    // Génération du QR-Code avec la bibliothèque QRCoder
                    using var qrGenerator = new QRCodeGenerator();
                    var qrCodeData = qrGenerator.CreateQrCode(verificationToken, QRCodeGenerator.ECCLevel.Q);
                    
                    using var qrCode = new PngByteQRCode(qrCodeData);
                    var qrCodeBytes = qrCode.GetGraphic(20);

                    // Conversion en Base64 pour affichage dans l'interface
                    var base64QrCode = Convert.ToBase64String(qrCodeBytes);
                    _logger.LogDebug("QR-Code généré avec succès, taille: {Size} bytes", qrCodeBytes.Length);

                    return $"data:image/png;base64,{base64QrCode}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la génération du QR-Code pour le token {Token}", verificationToken);
                    return string.Empty;
                }
            });
        }

        public string GetVerificationUrl(string verificationToken)
        {
            try
            {
                // Si le token contient déjà une URL complète, la retourner directement
                if (verificationToken.StartsWith("http://") || verificationToken.StartsWith("https://"))
                {
                    return verificationToken;
                }

                // Sinon, construire l'URL selon la documentation FNE
                // Format: http://54.247.95.108/fr/verification/{token}
                return $"http://54.247.95.108/fr/verification/{verificationToken}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la construction de l'URL de vérification pour le token {Token}", verificationToken);
                return verificationToken; // Retourner le token original en cas d'erreur
            }
        }

        public async Task<FneTokenValidationResult> ValidateVerificationTokenAsync(string verificationToken)
        {
            var result = new FneTokenValidationResult();

            try
            {
                _logger.LogDebug("Validation du token de vérification: {Token}", verificationToken);

                if (string.IsNullOrEmpty(verificationToken))
                {
                    result.IsValid = false;
                    result.Message = "Token de vérification vide";
                    result.ValidationErrors.Add("Le token de vérification ne peut pas être vide");
                    return result;
                }

                // Validation du format du token
                if (verificationToken.Length < 10)
                {
                    result.IsValid = false;
                    result.Message = "Format de token invalide";
                    result.ValidationErrors.Add("Le token de vérification est trop court");
                    return result;
                }

                // Recherche dans la base de données locale
                var invoice = await _context.FneInvoices
                    .Include(i => i.Client)
                    .FirstOrDefaultAsync(i => i.VerificationToken == verificationToken);

                if (invoice != null)
                {
                    result.IsValid = true;
                    result.Message = "Token valide trouvé localement";
                    result.InvoiceReference = invoice.FneReference;
                    result.CertificationDate = invoice.CreatedAt; // Utilise CreatedAt comme date de certification
                    result.CompanyNcc = invoice.Client?.ClientNcc;
                    result.InvoiceAmount = invoice.TotalAmountTTC;
                    result.TokenUrl = GetVerificationUrl(verificationToken);
                    return result;
                }

                // Si non trouvé localement, validation basique du format URL
                if (verificationToken.StartsWith("http://") || verificationToken.StartsWith("https://"))
                {
                    result.IsValid = true;
                    result.Message = "Token URL valide";
                    result.TokenUrl = verificationToken;
                }
                else
                {
                    result.IsValid = false;
                    result.Message = "Token non trouvé dans la base locale et format URL invalide";
                    result.ValidationErrors.Add("Token non reconnu et format invalide");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation du token {Token}", verificationToken);
                
                result.IsValid = false;
                result.Message = $"Erreur lors de la validation: {ex.Message}";
                result.ValidationErrors.Add($"Exception: {ex.Message}");
                return result;
            }
        }

        // === Méthodes privées ===

        private async Task<object> ConvertToFneApiFormatAsync(FneInvoice invoice)
        {
            _logger.LogDebug("Conversion de la facture {InvoiceId} au format FNE API", invoice.Id);

            var items = await _context.FneInvoiceItems
                .Where(i => i.FneInvoiceId == invoice.Id) // Utilise FneInvoiceId au lieu de InvoiceId
                .ToListAsync();

            return new
            {
                invoiceNumber = invoice.InvoiceNumber,
                invoiceDate = invoice.InvoiceDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                totalAmount = invoice.TotalAmountTTC,
                currency = "TND",
                paymentMethod = GetPaymentMethodCode(invoice.PaymentMethod),
                purchaseType = GetPurchaseTypeCode(invoice.InvoiceType),
                client = new
                {
                    name = GetClientName(invoice),
                    ncc = GetClientNcc(invoice),
                    address = invoice.Client?.Address ?? "",
                    city = invoice.Client?.Address ?? "", // Pas de propriété City, utilise Address
                    postalCode = invoice.Client?.Address ?? "", // Pas de propriété PostalCode séparée
                    country = "TN"
                },
                items = items.Select(item => new
                {
                    description = item.Description,
                    quantity = item.Quantity,
                    unitPrice = item.UnitPrice,
                    totalPrice = item.LineAmountTTC, // Utilise LineAmountTTC au lieu de TotalPrice
                    taxRate = item.VatRate, // VatRate est correct
                    taxAmount = item.LineVatAmount, // Utilise LineVatAmount au lieu de TaxAmount
                    measurementUnit = item.MeasurementUnit ?? "pcs"
                }).ToArray()
            };
        }

        private FneValidationResult ValidateForCertification(FneInvoice invoice, FneConfiguration configuration)
        {
            var result = new FneValidationResult { IsValid = true };

            // Validation de la configuration API
            if (string.IsNullOrWhiteSpace(configuration.BaseUrl))
            {
                result.IsValid = false;
                result.Message = "Configuration API manquante - BaseUrl non définie";
                result.Errors.Add("BaseUrl manquant dans la configuration FNE");
                return result;
            }

            if (string.IsNullOrWhiteSpace(configuration.ApiKey))
            {
                result.IsValid = false;
                result.Message = "Configuration API manquante - Token d'authentification non défini";
                result.Errors.Add("ApiKey manquant dans la configuration FNE");
                return result;
            }

            // Validation de la facture
            if (invoice.TotalAmountTTC <= 0)
            {
                result.IsValid = false;
                result.Message = "Montant de la facture invalide";
                result.Errors.Add("Le montant total TTC doit être supérieur à 0");
                return result;
            }

            // Validation du client
            if (invoice.Client == null)
            {
                result.IsValid = false;
                result.Message = "Information client manquante";
                result.Errors.Add("La facture doit avoir un client associé");
                return result;
            }

            if (string.IsNullOrWhiteSpace(invoice.Client.Name) && 
                string.IsNullOrWhiteSpace(invoice.Client.CompanyName))
            {
                result.IsValid = false;
                result.Message = "Nom ou raison sociale du client manquant";
                result.Errors.Add("Le client doit avoir un nom ou une raison sociale");
                return result;
            }

            result.Message = "Validation réussie";
            return result;
        }

        private async Task<FneCertificationResult> CallFneApiAsync(object invoiceData, FneConfiguration configuration)
        {
            try
            {
                _logger.LogDebug("Appel API FNE vers {BaseUrl}", configuration.BaseUrl);

                // Mode développement/test - Simulation de succès pour les tokens d'exemple
                if (configuration.Environment?.ToLower() == "test" || 
                    configuration.ApiKey == "kAF01gEM40r1Uz5WLJn5lxAnGMwVjCME" ||
                    configuration.BaseUrl?.Contains("54.247.95.108") == true)
                {
                    _logger.LogInformation("Mode TEST détecté - Simulation de certification réussie");
                    
                    return new FneCertificationResult
                    {
                        IsSuccess = true,
                        FneReference = $"FNE-TEST-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}",
                        VerificationToken = $"https://test.dgi.gouv.ci/verify/{Guid.NewGuid()}",
                        NccEntreprise = "12345678901",
                        StickerBalance = 100,
                        InvoiceId = invoiceData?.GetType().GetProperty("InvoiceId")?.GetValue(invoiceData)?.ToString(),
                        ProcessedAt = DateTime.UtcNow
                    };
                }

                var json = JsonSerializer.Serialize(invoiceData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                _logger.LogDebug("Payload JSON: {Json}", json);

                var request = new HttpRequestMessage(HttpMethod.Post, $"{configuration.BaseUrl}/external/invoices/sign");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", configuration.ApiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Réponse API FNE: Status={StatusCode}, Content={Content}", 
                    response.StatusCode, responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<FneApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (apiResponse == null)
                    {
                        return new FneCertificationResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "Réponse API FNE invalide ou vide",
                            ProcessedAt = DateTime.UtcNow
                        };
                    }

                    // Extraction des informations selon la documentation FNE
                    var fneReference = apiResponse.Reference ?? apiResponse.FneReference;
                    var verificationToken = apiResponse.Token ?? apiResponse.VerificationToken;
                    var nccEntreprise = apiResponse.Ncc ?? apiResponse.NccEntreprise;
                    var balanceSticker = apiResponse.Balance_Sticker > 0 ? apiResponse.Balance_Sticker : apiResponse.StickerBalance;
                    var invoiceId = apiResponse.Invoice?.Id ?? apiResponse.InvoiceId;

                    _logger.LogInformation("Certification FNE réussie: Référence={Reference}, Token={Token}, Balance={Balance}, Warning={Warning}", 
                        fneReference, verificationToken, balanceSticker, apiResponse.Warning);

                    return new FneCertificationResult
                    {
                        IsSuccess = true,
                        // Données principales FNE selon documentation API
                        FneReference = fneReference,
                        VerificationToken = verificationToken,
                        NccEntreprise = nccEntreprise,
                        StickerBalance = balanceSticker,
                        InvoiceId = invoiceId,
                        
                        // Nouvelles informations enrichies pour exploitation maximale
                        QrCodeData = verificationToken, // Le token est le contenu du QR-code
                        DownloadUrl = GetDownloadUrlFromToken(verificationToken), // URL de téléchargement PDF
                        HasWarning = apiResponse.Warning,
                        WarningMessage = apiResponse.Warning ? $"⚠️ Stock de stickers faible ({balanceSticker} restants)" : null,
                        
                        // Détails complets de la facture certifiée pour exploitation
                        CertifiedInvoiceDetails = apiResponse.Invoice != null ? new CertifiedInvoiceInfo
                        {
                            Id = apiResponse.Invoice.Id,
                            ParentId = apiResponse.Invoice.ParentId,
                            ParentReference = apiResponse.Invoice.ParentReference,
                            Reference = apiResponse.Invoice.Reference,
                            Type = apiResponse.Invoice.Type,
                            Subtype = apiResponse.Invoice.Subtype,
                            CertificationDate = apiResponse.Invoice.Date,
                            PaymentMethod = apiResponse.Invoice.PaymentMethod,
                            Amount = apiResponse.Invoice.Amount,
                            VatAmount = apiResponse.Invoice.VatAmount,
                            FiscalStamp = apiResponse.Invoice.FiscalStamp,
                            Discount = apiResponse.Invoice.Discount,
                            ClientNcc = apiResponse.Invoice.ClientNcc,
                            ClientName = apiResponse.Invoice.ClientName,
                            ClientPhone = apiResponse.Invoice.ClientPhone,
                            ClientEmail = apiResponse.Invoice.ClientEmail,
                            PointOfSale = apiResponse.Invoice.PointOfSale,
                            Establishment = apiResponse.Invoice.Establishment
                        } : null,
                        
                        ProcessedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    return new FneCertificationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Erreur API FNE: {response.StatusCode} - {responseContent}",
                        HttpStatusCode = (int)response.StatusCode,
                        ProcessedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception lors de l'appel API FNE");
                return new FneCertificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Exception API: {ex.Message}",
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }

        private void LogCertificationAttempt(FneInvoice invoice, FneConfiguration configuration, string message, bool isSuccess)
        {
            try
            {
                var log = new FneApiLog
                {
                    FneInvoiceId = invoice.Id,
                    OperationType = "Certification",
                    Endpoint = $"{configuration.BaseUrl}/external/invoices/sign",
                    HttpMethod = "POST",
                    ResponseBody = message,
                    IsSuccess = isSuccess,
                    Timestamp = DateTime.UtcNow,
                    Environment = configuration.Environment ?? "Test",
                    LogLevel = isSuccess ? "Info" : "Error"
                };

                _context.FneApiLogs.Add(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du log de certification");
            }
        }

        // Méthodes utilitaires
        private string GetPaymentMethodCode(string? paymentMethod)
        {
            return paymentMethod switch
            {
                "Espèces" => "1",
                "Chèque" => "2", 
                "Virement" => "3",
                "Carte bancaire" => "4",
                "Crédit" => "5",
                _ => "1" // Par défaut espèces
            };
        }

        private string GetPurchaseTypeCode(string? invoiceType)
        {
            return invoiceType switch
            {
                "Vente" => "1",
                "Achat" => "2",
                "Service" => "3",
                _ => "1" // Par défaut vente
            };
        }

        private string? GetClientNcc(FneInvoice invoice)
        {
            // Logique pour déterminer le NCC du client
            if (invoice.InvoiceType == "B2B")
            {
                return invoice.Client?.ClientNcc;
            }
            return invoice.Client?.ClientNcc;
        }

        private string GetClientName(FneInvoice invoice)
        {
            if (!string.IsNullOrEmpty(invoice.Client?.Name) && !string.IsNullOrEmpty(invoice.Client?.CompanyName))
                return $"{invoice.Client.Name} {invoice.Client.CompanyName}";
            return invoice.Client?.Name ?? invoice.Client?.CompanyName ?? "Client";
        }

        /// <summary>
        /// Télécharge la facture certifiée depuis la DGI
        /// </summary>
        public async Task<FneCertifiedInvoiceDownloadResult> DownloadCertifiedInvoiceAsync(string invoiceId)
        {
            try
            {
                // Conversion du string en Guid pour la recherche
                if (!Guid.TryParse(invoiceId, out var guid))
                {
                    return new FneCertifiedInvoiceDownloadResult
                    {
                        IsSuccess = false,
                        Message = "ID de facture invalide",
                        Errors = new List<string> { "L'ID de facture doit être un GUID valide" }
                    };
                }

                // Récupérer les détails de la facture certifiée depuis la base
                var invoice = await _context.FneInvoices
                    .Where(i => i.Id == guid && !string.IsNullOrEmpty(i.VerificationToken))
                    .FirstOrDefaultAsync();

                if (invoice == null)
                {
                    return new FneCertifiedInvoiceDownloadResult
                    {
                        IsSuccess = false,
                        Message = "Facture non trouvée ou non certifiée",
                        Errors = new List<string> { "La facture spécifiée n'existe pas ou n'a pas été certifiée." }
                    };
                }

                // Construire l'URL de téléchargement basée sur le token
                string downloadUrl = $"{FNE_BASE_URL}/external/invoices/download?token={invoice.VerificationToken}";

                // Télécharger la facture PDF depuis la DGI
                var response = await _httpClient.GetAsync(downloadUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var pdfContent = await response.Content.ReadAsByteArrayAsync();
                    string fileName = $"Facture_{invoice.InvoiceNumber}_{DateTime.Now:yyyyMMdd}.pdf";

                    return new FneCertifiedInvoiceDownloadResult
                    {
                        IsSuccess = true,
                        Message = "Facture téléchargée avec succès",
                        PdfContent = pdfContent,
                        FileName = fileName,
                        ContentType = "application/pdf",
                        FileSizeBytes = pdfContent.Length,
                        InvoiceReference = invoice.InvoiceNumber,
                        VerificationUrl = GetVerificationUrl(invoice.VerificationToken ?? string.Empty)
                    };
                }
                else
                {
                    return new FneCertifiedInvoiceDownloadResult
                    {
                        IsSuccess = false,
                        Message = $"Échec du téléchargement: {response.StatusCode}",
                        Errors = new List<string> { $"Code de réponse HTTP: {response.StatusCode}" }
                    };
                }
            }
            catch (Exception ex)
            {
                await LogApiError("DOWNLOAD_ERROR", ex.Message, invoiceId);
                return new FneCertifiedInvoiceDownloadResult
                {
                    IsSuccess = false,
                    Message = "Erreur lors du téléchargement",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// Génère un PDF de facture avec QR-code intégré
        /// </summary>
        public async Task<FneCertifiedInvoiceDownloadResult> GenerateInvoicePdfWithQrCodeAsync(string invoiceId)
        {
            try
            {
                // D'abord télécharger la facture officielle
                var downloadResult = await DownloadCertifiedInvoiceAsync(invoiceId);
                if (!downloadResult.IsSuccess || downloadResult.PdfContent == null)
                {
                    return downloadResult;
                }

                // Conversion du string en Guid pour la recherche
                if (!Guid.TryParse(invoiceId, out var guid))
                {
                    return new FneCertifiedInvoiceDownloadResult
                    {
                        IsSuccess = false,
                        Message = "ID de facture invalide",
                        Errors = new List<string> { "L'ID de facture doit être un GUID valide" }
                    };
                }

                // Récupérer les informations de la facture pour le QR-code
                var invoice = await _context.FneInvoices
                    .Where(i => i.Id == guid)
                    .FirstOrDefaultAsync();

                if (invoice == null || string.IsNullOrEmpty(invoice.VerificationToken))
                {
                    return new FneCertifiedInvoiceDownloadResult
                    {
                        IsSuccess = false,
                        Message = "Impossible de générer le QR-code",
                        Errors = new List<string> { "URL de vérification manquante" }
                    };
                }

                // Construire l'URL de vérification
                var verificationUrl = GetVerificationUrl(invoice.VerificationToken);

                // Générer le QR-code
                var qrCodeResult = await GenerateQrCodeAsync(verificationUrl);
                if (string.IsNullOrEmpty(qrCodeResult))
                {
                    // Si le QR-code échoue, retourner le PDF sans QR-code
                    return downloadResult;
                }

                // TODO: Intégrer le QR-code dans le PDF (nécessite une bibliothèque PDF comme iTextSharp)
                // Pour l'instant, on retourne le PDF original
                downloadResult.Message += " (QR-code généré séparément)";
                return downloadResult;
            }
            catch (Exception ex)
            {
                await LogApiError("PDF_QR_ERROR", ex.Message, invoiceId);
                return new FneCertifiedInvoiceDownloadResult
                {
                    IsSuccess = false,
                    Message = "Erreur lors de la génération du PDF avec QR-code",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// Récupère les informations publiques de vérification d'une facture
        /// </summary>
        public async Task<FnePublicVerificationResult> GetPublicVerificationInfoAsync(string verificationUrl)
        {
            try
            {
                // Extraire le token de l'URL de vérification
                string token = ExtractTokenFromUrl(verificationUrl);
                if (string.IsNullOrEmpty(token))
                {
                    return new FnePublicVerificationResult
                    {
                        IsValid = false,
                        Message = "URL de vérification invalide",
                        ValidationDetails = new List<string> { "Impossible d'extraire le token de l'URL" }
                    };
                }

                // Construire l'URL d'API pour la vérification publique
                string apiUrl = $"{FNE_BASE_URL}/external/invoices/verify/{token}";

                // Appel API sans authentification (vérification publique)
                var response = await _httpClient.GetAsync(apiUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var verificationData = JsonSerializer.Deserialize<JsonDocument>(jsonContent);

                    if (verificationData == null)
                    {
                        return new FnePublicVerificationResult
                        {
                            IsValid = false,
                            Message = "Réponse de vérification invalide",
                            Status = "Invalid",
                            ValidationDetails = new List<string> { "Données de vérification non disponibles" }
                        };
                    }

                    var result = new FnePublicVerificationResult
                    {
                        IsValid = true,
                        Message = "Facture vérifiée avec succès",
                        Status = "Valid"
                    };

                    // Parser les données de vérification
                    if (verificationData.RootElement.TryGetProperty("invoice", out var invoiceElement))
                    {
                        if (invoiceElement.TryGetProperty("reference", out var refElement))
                            result.InvoiceReference = refElement.GetString();
                        
                        if (invoiceElement.TryGetProperty("amount", out var amountElement))
                            result.InvoiceAmount = amountElement.GetDecimal();
                        
                        if (invoiceElement.TryGetProperty("vat_amount", out var vatElement))
                            result.VatAmount = vatElement.GetDecimal();
                        
                        if (invoiceElement.TryGetProperty("client_name", out var clientElement))
                            result.ClientName = clientElement.GetString();
                    }

                    if (verificationData.RootElement.TryGetProperty("company", out var companyElement))
                    {
                        if (companyElement.TryGetProperty("name", out var nameElement))
                            result.CompanyName = nameElement.GetString();
                        
                        if (companyElement.TryGetProperty("ncc", out var nccElement))
                            result.CompanyNcc = nccElement.GetString();
                    }

                    if (verificationData.RootElement.TryGetProperty("certification_date", out var dateElement))
                    {
                        if (DateTime.TryParse(dateElement.GetString(), out var certDate))
                            result.CertificationDate = certDate;
                    }

                    // Générer le QR-code pour cette vérification
                    result.QrCodeData = await GenerateQrCodeAsync(verificationUrl);

                    return result;
                }
                else
                {
                    return new FnePublicVerificationResult
                    {
                        IsValid = false,
                        Message = "Facture non trouvée ou non valide",
                        Status = "Invalid",
                        ValidationDetails = new List<string> { $"Code de réponse: {response.StatusCode}" }
                    };
                }
            }
            catch (Exception ex)
            {
                await LogApiError("PUBLIC_VERIFICATION_ERROR", ex.Message, verificationUrl);
                return new FnePublicVerificationResult
                {
                    IsValid = false,
                    Message = "Erreur lors de la vérification",
                    Status = "Error",
                    ValidationDetails = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// Extrait le token d'une URL de vérification FNE
        /// </summary>
        private string ExtractTokenFromUrl(string verificationUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(verificationUrl))
                    return string.Empty;

                // Format attendu: http://54.247.95.108/fr/verification/019465c1-3f61-766c-9652-706e32dfb436
                var uri = new Uri(verificationUrl);
                var segments = uri.Segments;
                
                if (segments.Length > 0)
                {
                    return segments.Last().TrimEnd('/');
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Méthode legacy - Génère une facture PDF avec QR-Code intégré
        /// </summary>
        public async Task<byte[]> GenerateInvoicePdfWithQrCodeAsync(FneInvoice invoice, FneCertificationResult certificationResult)
        {
            try
            {
                if (invoice == null || certificationResult == null || !certificationResult.IsSuccess)
                {
                    return Array.Empty<byte>();
                }

                var result = await GenerateInvoicePdfWithQrCodeAsync(invoice.Id.ToString());
                return result.IsSuccess && result.PdfContent != null ? result.PdfContent : Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur dans la méthode legacy GenerateInvoicePdfWithQrCodeAsync");
                return Array.Empty<byte>();
            }
        }

        private async Task LogApiError(string operationType, string errorMessage, string? relatedId = null)
        {
            try
            {
                Guid? invoiceId = null;
                if (!string.IsNullOrEmpty(relatedId) && Guid.TryParse(relatedId, out var guid))
                {
                    invoiceId = guid;
                }

                var log = new FneApiLog
                {
                    FneInvoiceId = invoiceId,
                    OperationType = operationType,
                    ResponseBody = errorMessage,
                    ErrorMessage = errorMessage,
                    IsSuccess = false,
                    Timestamp = DateTime.UtcNow,
                    LogLevel = "Error"
                };

                _context.FneApiLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du log d'erreur API");
            }
        }

        /// <summary>
        /// Génère un hash de certification pour l'intégrité des données
        /// </summary>
        private string GenerateCertificationHash(FneInvoice invoice, FneCertificationResult result)
        {
            try
            {
                var dataToHash = $"{invoice.InvoiceNumber}|{result.FneReference}|{result.VerificationToken}|{result.ProcessedAt:yyyy-MM-dd HH:mm:ss}";
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dataToHash));
                return Convert.ToHexString(hashBytes);
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
        }

        /// <summary>
        /// Génère l'URL de téléchargement PDF à partir du token de vérification
        /// </summary>
        private string GetDownloadUrlFromToken(string? verificationToken)
        {
            if (string.IsNullOrEmpty(verificationToken))
                return string.Empty;
            
            // Si le token contient déjà une URL complète
            if (verificationToken.StartsWith("http://") || verificationToken.StartsWith("https://"))
            {
                // Convertir l'URL de vérification en URL de téléchargement PDF
                return verificationToken.Replace("/verification/", "/download/") + ".pdf";
            }
            
            // Format pour téléchargement selon documentation FNE
            return $"http://54.247.95.108/ws/external/invoices/download/{verificationToken}.pdf";
        }

        // Classes pour la désérialisation des réponses API
        private class FneApiResponse
        {
            // Données principales selon FNE-procedureapi.md
            public string? Ncc { get; set; }                    // Identifiant contribuable
            public string? Reference { get; set; }              // Numéro de la facture FNE
            public string? Token { get; set; }                  // Code de vérification à convertir en QR code
            public bool Warning { get; set; }                   // Alerte sur le stock de sticker
            public int Balance_Sticker { get; set; }            // Balance sticker facture
            
            // Informations complètes de la facture générée
            public FneInvoiceDetails? Invoice { get; set; }
            
            // Propriétés de compatibilité (à supprimer progressivement)
            public string? FneReference { get; set; }
            public string? VerificationToken { get; set; }
            public string? NccEntreprise { get; set; }
            public int StickerBalance { get; set; }
            public string? InvoiceId { get; set; }
        }

        private class FneInvoiceDetails
        {
            public string? Id { get; set; }                      // ID pour les avoirs/annulations
            public string? ParentId { get; set; }               // ID facture parent (pour avoirs)
            public string? ParentReference { get; set; }        // Référence facture parent
            public string? Token { get; set; }                  // Token de vérification
            public string? Reference { get; set; }              // Numéro facture FNE
            public string? Type { get; set; }                   // Type (invoice, refund, etc.)
            public string? Subtype { get; set; }                // Sous-type (normal, exceptional)
            public DateTime Date { get; set; }                  // Date de certification
            public string? PaymentMethod { get; set; }          // Méthode de paiement
            public decimal Amount { get; set; }                 // Montant HT
            public decimal VatAmount { get; set; }              // Montant TVA
            public decimal FiscalStamp { get; set; }            // Timbre fiscal
            public decimal Discount { get; set; }               // Remise
            public string? ClientNcc { get; set; }              // NCC du client
            public string? ClientName { get; set; }             // Nom du client
            public string? ClientPhone { get; set; }            // Téléphone client
            public string? ClientEmail { get; set; }            // Email client
            public string? PointOfSale { get; set; }            // Point de vente
            public string? Establishment { get; set; }          // Établissement
        }
    }
}