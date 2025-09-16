using FNEV4.Core.Entities;

namespace FNEV4.Core.Interfaces.Services.Fne
{
    /// <summary>
    /// Service de certification des factures via l'API FNE
    /// Implémente la procédure officielle selon FNE-procedureapi.md
    /// </summary>
    public interface IFneCertificationService
    {
        /// <summary>
        /// Certifie une facture FNE via l'API DGI
        /// </summary>
        /// <param name="invoice">Facture à certifier</param>
        /// <param name="configuration">Configuration FNE</param>
        /// <returns>Résultat de la certification</returns>
        Task<FneCertificationResult> CertifyInvoiceAsync(FneInvoice invoice, FneConfiguration configuration);

        /// <summary>
        /// Certifie plusieurs factures en lot
        /// </summary>
        /// <param name="invoices">Liste des factures à certifier</param>
        /// <param name="configuration">Configuration FNE</param>
        /// <returns>Résultat de la certification en lot</returns>
        Task<FneBatchCertificationResult> CertifyInvoicesBatchAsync(List<FneInvoice> invoices, FneConfiguration configuration);

        /// <summary>
        /// Valide qu'une facture est prête pour la certification FNE
        /// </summary>
        /// <param name="invoice">Facture à valider</param>
        /// <returns>Résultat de validation</returns>
        Task<FneValidationResult> ValidateForCertificationAsync(FneInvoice invoice);

        /// <summary>
        /// Obtient les factures en attente de certification
        /// </summary>
        /// <returns>Liste des factures en attente</returns>
        Task<List<FneInvoice>> GetPendingInvoicesAsync();

        /// <summary>
        /// Obtient le nombre de factures en attente
        /// </summary>
        /// <returns>Nombre de factures en attente</returns>
        Task<int> GetPendingInvoicesCountAsync();

        /// <summary>
        /// Obtient les factures pour une période donnée
        /// </summary>
        /// <param name="startDate">Date de début</param>
        /// <param name="endDate">Date de fin</param>
        /// <returns>Liste des factures pour la période</returns>
        Task<List<FneInvoice>> GetInvoicesForPeriodAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Certifie les factures en attente (pour mode automatique)
        /// </summary>
        /// <param name="maxCount">Nombre maximum de factures à traiter</param>
        /// <returns>Résultat de la certification automatique</returns>
        Task<FneBatchCertificationResult> CertifyPendingInvoicesAsync(int maxCount = 50);

        /// <summary>
        /// Obtient les activités récentes de certification
        /// </summary>
        /// <param name="count">Nombre d'activités à récupérer</param>
        /// <returns>Liste des activités récentes</returns>
        Task<List<FneActivity>> GetRecentActivitiesAsync(int count = 10);

        /// <summary>
        /// Obtient les certifications récentes
        /// </summary>
        /// <param name="count">Nombre de certifications à récupérer</param>
        /// <returns>Liste des certifications récentes</returns>
        Task<List<FneInvoice>> GetRecentCertificationsAsync(int count = 5);

        /// <summary>
        /// Obtient les métriques de performance
        /// </summary>
        /// <returns>Métriques de performance</returns>
        Task<FnePerformanceMetrics> GetPerformanceMetricsAsync();

        /// <summary>
        /// Effectue un test de santé du système
        /// </summary>
        /// <returns>Résultat du test de santé</returns>
        Task<FneSystemHealthResult> PerformHealthCheckAsync();
    }

    #region Classes de résultat

    /// <summary>
    /// Résultat de certification FNE
    /// </summary>
    public class FneCertificationResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        
        // Données retournées par l'API FNE
        public string? FneReference { get; set; } // Numéro facture FNE
        public string? VerificationToken { get; set; } // Token QR Code
        public string? NccEntreprise { get; set; }
        public int StickerBalance { get; set; }
        public string? InvoiceId { get; set; } // ID facture pour avoir/annulation
        
        // Gestion d'erreurs
        public List<string> Errors { get; set; } = new();
        public string? ErrorCode { get; set; }
        public int HttpStatusCode { get; set; }
        public long ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Résultat de certification en lot
    /// </summary>
    public class FneBatchCertificationResult
    {
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public long TotalProcessingTimeMs { get; set; }
        
        public List<FneCertificationResult> Results { get; set; } = new();
        public List<string> GlobalErrors { get; set; } = new();
        
        public bool IsPartialSuccess => SuccessCount > 0 && ErrorCount > 0;
        public bool IsCompleteSuccess => SuccessCount == TotalCount && ErrorCount == 0;
        public double SuccessRate => TotalCount > 0 ? (double)SuccessCount / TotalCount * 100 : 0;
    }

    /// <summary>
    /// Résultat de validation FNE
    /// </summary>
    public class FneValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        
        // Détails de validation
        public bool HasValidClient { get; set; }
        public bool HasValidPaymentMethod { get; set; }
        public bool HasValidItems { get; set; }
        public bool HasValidTaxCodes { get; set; }
        public bool HasValidAmounts { get; set; }
        public bool HasValidDates { get; set; }
    }

    /// <summary>
    /// Activité FNE pour le journal
    /// </summary>
    public class FneActivity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ActivityType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Level { get; set; } = "Info"; // Info, Warning, Error, Success
        public string? InvoiceNumber { get; set; }
        public string? Details { get; set; }
        public long? ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Métriques de performance FNE
    /// </summary>
    public class FnePerformanceMetrics
    {
        public double SuccessRate { get; set; }
        public TimeSpan AverageTime { get; set; }
        public int CertificationsLastHour { get; set; }
        public int TotalCertificationsToday { get; set; }
        public int TotalErrorsToday { get; set; }
        public decimal TotalAmountCertifiedToday { get; set; }
        public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Résultat du test de santé du système
    /// </summary>
    public class FneSystemHealthResult
    {
        public bool IsHealthy { get; set; }
        public bool HasApiConfig { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        
        public bool DatabaseHealthy { get; set; }
        public bool ApiConnectionHealthy { get; set; }
        public bool ConfigurationValid { get; set; }
        
        public List<string> HealthIssues { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    #endregion
}
