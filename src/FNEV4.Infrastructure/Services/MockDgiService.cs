using Microsoft.Extensions.Logging;
using FNEV4.Core.Interfaces.Services.Fne;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Implémentation mock du service DGI pour les tests et développement
    /// </summary>
    public class MockDgiService : IDgiService
    {
        private readonly ILogger<MockDgiService> _logger;

        public MockDgiService(ILogger<MockDgiService> logger)
        {
            _logger = logger;
        }

        public async Task<DgiVerificationResult> VerifyNccAsync(string nccNumber)
        {
            _logger.LogInformation("Simulation vérification NCC {NccNumber}", nccNumber);
            
            await Task.Delay(100);
            
            return new DgiVerificationResult
            {
                IsSuccess = true,
                IsValid = nccNumber?.Length == 8,
                Message = nccNumber?.Length == 8 ? "NCC valide (simulation)" : "Format NCC invalide",
                CompanyInfo = nccNumber?.Length == 8 ? new DgiCompanyInfo
                {
                    Ncc = nccNumber,
                    CompanyName = "Entreprise Test",
                    Status = "Actif"
                } : null
            };
        }

        public async Task<bool> TestConnectivityAsync()
        {
            await Task.Delay(50);
            _logger.LogInformation("Test connectivité DGI (simulation)");
            return true;
        }

        public async Task<bool> TestConnectionAsync()
        {
            return await TestConnectivityAsync();
        }

        public async Task<bool> IsConfiguredAsync()
        {
            await Task.Delay(10);
            return true; // Toujours configuré en simulation
        }

        public async Task<DgiCompanyInfo?> GetCompanyInfoAsync(string nccNumber)
        {
            await Task.Delay(100);
            
            if (nccNumber?.Length != 8)
                return null;
            
            return new DgiCompanyInfo
            {
                Ncc = nccNumber,
                CompanyName = "Entreprise Simulée",
                Status = "Actif",
                RegistrationDate = DateTime.Now.AddYears(-2),
                BusinessSector = "Services",
                Address = "Adresse de test"
            };
        }
    }
}