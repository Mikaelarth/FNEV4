using System.Diagnostics;
using Microsoft.Extensions.Logging;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces.Services.Fne;
using FNEV4.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Implémentation mock du service de certification FNE pour les tests et développement
    /// </summary>
    public class MockFneCertificationService : IFneCertificationService
    {
        private readonly FNEV4DbContext _context;
        private readonly ILogger<MockFneCertificationService> _logger;
        private readonly IDgiService _dgiService;

        public MockFneCertificationService(
            FNEV4DbContext context,
            ILogger<MockFneCertificationService> logger,
            IDgiService dgiService)
        {
            _context = context;
            _logger = logger;
            _dgiService = dgiService;
        }

        public async Task<FneCertificationResult> CertifyInvoiceAsync(FneInvoice invoice, FneConfiguration configuration)
        {
            _logger.LogInformation("Simulation certification facture {InvoiceNumber}", invoice.InvoiceNumber);
            
            // Simulation
            await Task.Delay(100);
            
            return new FneCertificationResult
            {
                IsSuccess = true,
                ErrorMessage = string.Empty,
                FneReference = $"FNE{invoice.InvoiceNumber}",
                VerificationToken = Guid.NewGuid().ToString()
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
    }
}