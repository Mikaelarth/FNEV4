using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FNEV4.Core.Interfaces.Services.Fne;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Service d'intégration avec l'API DGI (Direction Générale des Impôts)
    /// Implémente la vérification NCC selon FNE-procedureapi.md
    /// </summary>
    public class DgiService : IDgiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DgiService> _logger;
        private readonly string _baseUrl;
        private readonly string? _apiKey;

        public DgiService(HttpClient httpClient, ILogger<DgiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            // URL de test DGI selon la documentation
            _baseUrl = "http://54.247.95.108/ws";
            
            // TODO: Récupérer la clé API depuis la configuration
            // _apiKey = configuration.GetValue<string>("Dgi:ApiKey");
            
            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        /// <summary>
        /// Vérifie la validité d'un NCC auprès de la base DGI
        /// Note: L'API DGI n'a pas d'endpoint spécifique pour vérifier un NCC seul.
        /// Cette méthode utilise l'endpoint de certification pour tester la validité.
        /// </summary>
        public async Task<DgiVerificationResult> VerifyNccAsync(string nccNumber)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("Début vérification NCC {NccNumber} auprès de la DGI", nccNumber);

                // Validation du format NCC
                if (!IsValidNccFormat(nccNumber))
                {
                    stopwatch.Stop();
                    return new DgiVerificationResult
                    {
                        IsSuccess = false,
                        IsValid = false,
                        Message = "Format NCC invalide (requis: 7 chiffres + 1 lettre)",
                        Duration = stopwatch.Elapsed
                    };
                }

                // Vérification de la disponibilité de l'API Key
                if (string.IsNullOrWhiteSpace(_apiKey))
                {
                    stopwatch.Stop();
                    _logger.LogWarning("Clé API DGI non configurée");
                    
                    // Simulation de vérification en mode test
                    return await SimulateNccVerificationAsync(nccNumber, stopwatch);
                }

                // Appel réel à l'API DGI
                return await CallDgiApiAsync(nccNumber, stopwatch);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Erreur lors de la vérification NCC {NccNumber}", nccNumber);
                
                return new DgiVerificationResult
                {
                    IsSuccess = false,
                    IsValid = false,
                    Message = "Erreur technique lors de la vérification DGI",
                    ErrorDetails = ex.Message,
                    Duration = stopwatch.Elapsed
                };
            }
        }

        private async Task<DgiVerificationResult> CallDgiApiAsync(string nccNumber, Stopwatch stopwatch)
        {
            try
            {
                // Création d'une facture de test minimale pour vérifier le NCC
                var testInvoiceRequest = CreateTestInvoiceRequest(nccNumber);
                var jsonContent = JsonSerializer.Serialize(testInvoiceRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug("Envoi requête test à l'API DGI pour NCC {NccNumber}", nccNumber);

                var response = await _httpClient.PostAsync("/external/invoices/sign", content);
                stopwatch.Stop();

                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("NCC {NccNumber} vérifié avec succès auprès de la DGI", nccNumber);
                    
                    return new DgiVerificationResult
                    {
                        IsSuccess = true,
                        IsValid = true,
                        Message = $"NCC {nccNumber} vérifié et valide dans la base DGI",
                        Duration = stopwatch.Elapsed,
                        CompanyInfo = ExtractCompanyInfoFromResponse(responseContent)
                    };
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    // Analyser la réponse d'erreur pour déterminer si c'est un problème de NCC
                    var isNccError = responseContent.Contains("NCC") || responseContent.Contains("clientNcc");
                    
                    return new DgiVerificationResult
                    {
                        IsSuccess = true,
                        IsValid = !isNccError,
                        Message = isNccError ? $"NCC {nccNumber} non trouvé dans la base DGI" : "Erreur de validation des données",
                        HttpStatusCode = (int)response.StatusCode,
                        ErrorDetails = responseContent,
                        Duration = stopwatch.Elapsed
                    };
                }
                else
                {
                    _logger.LogWarning("Erreur API DGI {StatusCode}: {Response}", response.StatusCode, responseContent);
                    
                    return new DgiVerificationResult
                    {
                        IsSuccess = false,
                        IsValid = false,
                        Message = "Erreur lors de la communication avec la DGI",
                        HttpStatusCode = (int)response.StatusCode,
                        ErrorDetails = responseContent,
                        Duration = stopwatch.Elapsed
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Erreur réseau lors de l'appel API DGI");
                
                return new DgiVerificationResult
                {
                    IsSuccess = false,
                    IsValid = false,
                    Message = "Impossible de contacter la DGI - Vérifiez votre connexion internet",
                    ErrorDetails = ex.Message,
                    Duration = stopwatch.Elapsed
                };
            }
        }

        private async Task<DgiVerificationResult> SimulateNccVerificationAsync(string nccNumber, Stopwatch stopwatch)
        {
            // Simulation d'un délai réseau
            await Task.Delay(1500);
            stopwatch.Stop();

            // Simulation basée sur le format et quelques exemples connus
            var knownValidNccs = new[] { "9606123E", "9502363N", "1234567A", "9876543Z" };
            var isSimulatedValid = Array.Exists(knownValidNccs, ncc => ncc.Equals(nccNumber, StringComparison.OrdinalIgnoreCase));

            if (!isSimulatedValid)
            {
                // Pour la simulation, on considère que les NCC avec un format valide sont potentiellement valides
                isSimulatedValid = IsValidNccFormat(nccNumber);
            }

            return new DgiVerificationResult
            {
                IsSuccess = true,
                IsValid = isSimulatedValid,
                Message = isSimulatedValid 
                    ? $"✅ NCC {nccNumber} vérifié et valide (mode simulation)" 
                    : $"❌ NCC {nccNumber} non trouvé (mode simulation)",
                Duration = stopwatch.Elapsed,
                CompanyInfo = isSimulatedValid ? CreateSimulatedCompanyInfo(nccNumber) : null
            };
        }

        private static object CreateTestInvoiceRequest(string nccNumber)
        {
            return new
            {
                invoiceType = "sale",
                paymentMethod = "cash",
                template = "B2B",
                clientNcc = nccNumber,
                clientCompanyName = "Test Company",
                clientPhone = "0709080765",
                clientEmail = "test@company.ci",
                pointOfSale = "Test",
                establishment = "Test Establishment",
                items = new[]
                {
                    new
                    {
                        taxes = new[] { "TVA" },
                        reference = "TEST001",
                        description = "Article de test",
                        quantity = 1,
                        amount = 1000,
                        measurementUnit = "pcs"
                    }
                }
            };
        }

        private static DgiCompanyInfo? ExtractCompanyInfoFromResponse(string responseContent)
        {
            try
            {
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;

                if (root.TryGetProperty("ncc", out var nccElement))
                {
                    return new DgiCompanyInfo
                    {
                        Ncc = nccElement.GetString() ?? string.Empty,
                        CompanyName = "Entreprise vérifiée DGI",
                        Status = "Actif",
                        RegistrationDate = DateTime.Now
                    };
                }
            }
            catch (Exception)
            {
                // Ignore les erreurs de parsing
            }

            return null;
        }

        private static DgiCompanyInfo CreateSimulatedCompanyInfo(string nccNumber)
        {
            return new DgiCompanyInfo
            {
                Ncc = nccNumber,
                CompanyName = $"Entreprise {nccNumber}",
                Status = "Actif",
                RegistrationDate = DateTime.Now.AddYears(-2),
                BusinessSector = "Commerce",
                Address = "Abidjan, Côte d'Ivoire"
            };
        }

        public async Task<bool> TestConnectivityAsync()
        {
            try
            {
                _logger.LogDebug("Test de connectivité avec l'API DGI");
                
                // Simple test de connectivité HTTP
                var response = await _httpClient.GetAsync("/");
                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Test de connectivité DGI échoué");
                return false;
            }
        }

        public async Task<DgiCompanyInfo?> GetCompanyInfoAsync(string nccNumber)
        {
            var verificationResult = await VerifyNccAsync(nccNumber);
            return verificationResult.CompanyInfo;
        }

        /// <summary>
        /// Teste la connexion à l'API DGI
        /// </summary>
        /// <returns>True si la connexion est établie</returns>
        public async Task<bool> TestConnectionAsync()
        {
            return await TestConnectivityAsync();
        }

        /// <summary>
        /// Vérifie si l'API DGI est configurée
        /// </summary>
        /// <returns>True si la configuration est valide</returns>
        public async Task<bool> IsConfiguredAsync()
        {
            try
            {
                // Vérifier que l'URL de base est configurée
                if (string.IsNullOrEmpty(_baseUrl))
                    return false;

                // Test de connectivité simple
                return await TestConnectivityAsync();
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidNccFormat(string nccNumber)
        {
            if (string.IsNullOrWhiteSpace(nccNumber) || nccNumber.Length != 8)
                return false;

            // Vérifier que les 7 premiers caractères sont des chiffres
            for (int i = 0; i < 7; i++)
            {
                if (!char.IsDigit(nccNumber[i]))
                    return false;
            }

            // Vérifier que le dernier caractère est une lettre
            return char.IsLetter(nccNumber[7]);
        }
    }
}
