using System.Diagnostics;
using Microsoft.Extensions.Logging;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces.Services.Fne;
using FNEV4.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Service de certification FNE pour tests et développement (Mode Mock)
    /// ⚠️ NE PAS UTILISER EN PRODUCTION - Retourne des données fictives
    /// </summary>
    public class FneCertificationServiceMock : IFneCertificationService
    {
        private readonly FNEV4DbContext _context;
        private readonly ILogger<FneCertificationServiceMock> _logger;
        private readonly IDgiService _dgiService;

        public FneCertificationServiceMock(
            FNEV4DbContext context,
            ILogger<FneCertificationServiceMock> logger,
            IDgiService dgiService)
        {
            _context = context;
            _logger = logger;
            _dgiService = dgiService;
        }

        public async Task<FneCertificationResult> CertifyInvoiceAsync(FneInvoice invoice, FneConfiguration configuration)
        {
            _logger.LogError("🚫 ERREUR CRITIQUE - Service de certification non configuré pour la production ! Aucune certification réelle ne peut être effectuée.");
            
            await Task.Delay(50); // Petit délai pour éviter spam
            
            return new FneCertificationResult
            {
                IsSuccess = false,
                ErrorMessage = "ERREUR CRITIQUE: Service de certification non configuré pour la production. Contactez l'administrateur système.",
                FneReference = null,
                VerificationToken = null,
                ProcessedAt = DateTime.UtcNow
            };
        }

        public async Task<FneValidationResult> ValidateForCertificationAsync(FneInvoice invoice)
        {
            await Task.Delay(10);
            
            return new FneValidationResult
            {
                IsValid = true,
                Message = "Validation simulée réussie",
                HasValidClient = true,
                HasValidItems = true,
                HasValidAmounts = true
            };
        }

        public async Task<FneBatchCertificationResult> CertifyInvoicesBatchAsync(List<FneInvoice> invoices, FneConfiguration configuration)
        {
            var result = new FneBatchCertificationResult { TotalCount = invoices.Count };
            
            foreach (var invoice in invoices)
            {
                var certResult = await CertifyInvoiceAsync(invoice, configuration);
                result.Results.Add(certResult);
                
                if (certResult.IsSuccess)
                    result.SuccessCount++;
                else
                    result.ErrorCount++;
            }
            
            return result;
        }

        public async Task<List<FneInvoice>> GetPendingInvoicesAsync()
        {
            return await _context.FneInvoices
                .OrderBy(i => i.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        public async Task<int> GetPendingInvoicesCountAsync()
        {
            return await _context.FneInvoices.CountAsync();
        }

        public async Task<List<FneInvoice>> GetInvoicesForPeriodAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.FneInvoices
                .Where(i => i.CreatedAt >= startDate && i.CreatedAt <= endDate)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<FneBatchCertificationResult> CertifyPendingInvoicesAsync(int maxCount = 50)
        {
            var invoices = await GetPendingInvoicesAsync();
            var limitedInvoices = invoices.Take(maxCount).ToList();
            
            var configuration = new FneConfiguration(); // Configuration par défaut
            
            return await CertifyInvoicesBatchAsync(limitedInvoices, configuration);
        }

        public async Task<List<FneActivity>> GetRecentActivitiesAsync(int count = 10)
        {
            await Task.Delay(10);
            
            return new List<FneActivity>
            {
                new FneActivity
                {
                    CreatedAt = DateTime.Now,
                    ActivityType = "Test",
                    Message = "Activité de test",
                    Level = "Info"
                }
            };
        }

        public async Task<List<FneInvoice>> GetRecentCertificationsAsync(int count = 5)
        {
            return await _context.FneInvoices
                .OrderByDescending(i => i.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<FnePerformanceMetrics> GetPerformanceMetricsAsync()
        {
            await Task.Delay(10);
            
            return new FnePerformanceMetrics
            {
                SuccessRate = 95.0,
                AverageTime = TimeSpan.FromSeconds(2),
                CertificationsLastHour = 15,
                TotalCertificationsToday = 120,
                TotalErrorsToday = 5,
                TotalAmountCertifiedToday = 50000m
            };
        }

        public async Task<FneSystemHealthResult> PerformHealthCheckAsync()
        {
            try
            {
                var dbHealthy = await _context.Database.CanConnectAsync();
                var apiHealthy = await _dgiService.TestConnectionAsync();
                
                return new FneSystemHealthResult
                {
                    IsHealthy = dbHealthy && apiHealthy,
                    HasApiConfig = apiHealthy,
                    DatabaseHealthy = dbHealthy,
                    ApiConnectionHealthy = apiHealthy,
                    ConfigurationValid = true,
                    StatusMessage = dbHealthy && apiHealthy ? "Système opérationnel" : "Problèmes détectés"
                };
            }
            catch (Exception ex)
            {
                return new FneSystemHealthResult
                {
                    IsHealthy = false,
                    StatusMessage = ex.Message,
                    HealthIssues = new List<string> { ex.Message }
                };
            }
        }

        public async Task<string> GenerateQrCodeAsync(string verificationToken)
        {
            await Task.Delay(10);
            _logger.LogWarning("🔧 Génération de QR-Code simulée en mode développement");
            
            if (string.IsNullOrEmpty(verificationToken))
            {
                return string.Empty;
            }
            
            // Retourne une image QR-Code factice pour les tests
            return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
        }

        public string GetVerificationUrl(string verificationToken)
        {
            _logger.LogWarning("🔧 URL de vérification simulée en mode développement");
            
            if (string.IsNullOrEmpty(verificationToken))
            {
                return string.Empty;
            }
            
            // Retourne une URL de test factice
            return $"https://test.dgi.gouv.ci/verify/{verificationToken}";
        }

        public async Task<FneTokenValidationResult> ValidateVerificationTokenAsync(string verificationToken)
        {
            await Task.Delay(50);
            _logger.LogWarning("🔧 Validation de token simulée en mode développement");
            
            return new FneTokenValidationResult
            {
                IsValid = !string.IsNullOrEmpty(verificationToken),
                Message = string.IsNullOrEmpty(verificationToken) 
                    ? "Token vide en mode test" 
                    : "Token validé en mode test",
                InvoiceReference = $"TEST-{DateTime.Now:yyyyMMdd}-001",
                CertificationDate = DateTime.Now,
                CompanyNcc = "TEST123456",
                InvoiceAmount = 1000m,
                TokenUrl = GetVerificationUrl(verificationToken)
            };
        }

        /// <summary>
        /// Simulation de téléchargement de facture certifiée
        /// </summary>
        public async Task<FneCertifiedInvoiceDownloadResult> DownloadCertifiedInvoiceAsync(string invoiceId)
        {
            await Task.Delay(1000); // Simulation d'un téléchargement
            _logger.LogWarning("🔧 Téléchargement de facture simulé en mode développement");
            
            if (string.IsNullOrEmpty(invoiceId))
            {
                return new FneCertifiedInvoiceDownloadResult
                {
                    IsSuccess = false,
                    Message = "ID de facture requis",
                    Errors = new List<string> { "L'ID de facture ne peut pas être vide" }
                };
            }

            // Simulation d'un PDF minimal (Header PDF)
            byte[] mockPdfContent = {
                0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, // %PDF-1.4
                0x0A, 0x31, 0x20, 0x30, 0x20, 0x6F, 0x62, 0x6A, // Header
                0x0A, 0x3C, 0x3C, 0x2F, 0x54, 0x79, 0x70, 0x65, // Object start
                0x20, 0x2F, 0x43, 0x61, 0x74, 0x61, 0x6C, 0x6F, // Type Catalog
                0x67, 0x3E, 0x3E, 0x0A, 0x65, 0x6E, 0x64, 0x6F, // End
                0x62, 0x6A, 0x0A, 0x25, 0x25, 0x45, 0x4F, 0x46  // %%EOF
            };

            return new FneCertifiedInvoiceDownloadResult
            {
                IsSuccess = true,
                Message = "Facture de test téléchargée avec succès",
                PdfContent = mockPdfContent,
                FileName = $"Facture_TEST_{DateTime.Now:yyyyMMdd}.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = mockPdfContent.Length,
                InvoiceReference = $"TEST-INV-{invoiceId[..Math.Min(8, invoiceId.Length)]}",
                VerificationUrl = $"https://test.dgi.gouv.ci/verify/{Guid.NewGuid()}"
            };
        }

        /// <summary>
        /// Simulation de génération de PDF avec QR-code
        /// </summary>
        public async Task<FneCertifiedInvoiceDownloadResult> GenerateInvoicePdfWithQrCodeAsync(string invoiceId)
        {
            _logger.LogWarning("🔧 Génération PDF avec QR-code simulée en mode développement");
            
            var downloadResult = await DownloadCertifiedInvoiceAsync(invoiceId);
            if (downloadResult.IsSuccess)
            {
                downloadResult.Message += " (avec QR-code de test intégré)";
                // En mode réel, on intégrerait le QR-code dans le PDF ici
            }
            return downloadResult;
        }

        /// <summary>
        /// Simulation de vérification publique
        /// </summary>
        public async Task<FnePublicVerificationResult> GetPublicVerificationInfoAsync(string verificationUrl)
        {
            await Task.Delay(500); // Simulation d'appel API
            _logger.LogWarning("🔧 Vérification publique simulée en mode développement");

            if (string.IsNullOrEmpty(verificationUrl))
            {
                return new FnePublicVerificationResult
                {
                    IsValid = false,
                    Message = "URL de vérification requise",
                    Status = "Invalid",
                    ValidationDetails = new List<string> { "URL manquante pour la vérification" }
                };
            }

            var qrCode = await GenerateQrCodeAsync(verificationUrl);

            return new FnePublicVerificationResult
            {
                IsValid = true,
                Message = "Facture de test vérifiée avec succès",
                Status = "Valid",
                InvoiceReference = "TEST-PUBLIC-001",
                CertificationDate = DateTime.Now.AddDays(-1),
                CompanyName = "Entreprise Test SARL",
                CompanyNcc = "TEST123456789",
                InvoiceAmount = 1500.00m,
                VatAmount = 285.00m,
                PaymentMethod = "Espèces",
                ClientName = "Client de Test",
                QrCodeData = qrCode,
                ExpiryDate = DateTime.Now.AddYears(5),
                ValidationDetails = new List<string> 
                { 
                    "Vérification réussie en mode test",
                    "Facture émise par l'API de développement FNE",
                    $"URL testée: {verificationUrl}"
                }
            };
        }

        /// <summary>
        /// Méthode legacy - Génère une facture PDF avec QR-Code intégré
        /// </summary>
        public async Task<byte[]> GenerateInvoicePdfWithQrCodeAsync(FneInvoice invoice, FneCertificationResult certificationResult)
        {
            _logger.LogWarning("🔧 Méthode legacy PDF avec QR-code simulée en mode développement");
            
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
                _logger.LogError(ex, "Erreur dans la méthode legacy mock GenerateInvoicePdfWithQrCodeAsync");
                return Array.Empty<byte>();
            }
        }
    }
}