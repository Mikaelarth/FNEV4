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

        /// <summary>
        /// Génère le QR-Code à partir du token de vérification
        /// </summary>
        /// <param name="verificationToken">Token de vérification FNE</param>
        /// <returns>Données du QR-Code en base64</returns>
        Task<string> GenerateQrCodeAsync(string verificationToken);

        /// <summary>
        /// Obtient l'URL de téléchargement/vérification de la facture certifiée
        /// </summary>
        /// <param name="verificationToken">Token de vérification FNE</param>
        /// <returns>URL complète de vérification</returns>
        string GetVerificationUrl(string verificationToken);

        /// <summary>
        /// Valide un token de vérification FNE
        /// </summary>
        /// <param name="verificationToken">Token à valider</param>
        /// <returns>Résultat de validation</returns>
        Task<FneTokenValidationResult> ValidateVerificationTokenAsync(string verificationToken);

        /// <summary>
        /// Télécharge la facture certifiée depuis la DGI
        /// </summary>
        /// <param name="verificationToken">Token de vérification FNE</param>
        /// <returns>Contenu PDF de la facture certifiée</returns>
        Task<FneCertifiedInvoiceDownloadResult> DownloadCertifiedInvoiceAsync(string verificationToken);

        /// <summary>
        /// Génère un PDF de facture avec QR-code intégré
        /// </summary>
        /// <param name="invoiceId">Identifiant de la facture</param>
        /// <returns>Résultat avec le PDF et les détails</returns>
        Task<FneCertifiedInvoiceDownloadResult> GenerateInvoicePdfWithQrCodeAsync(string invoiceId);

        /// <summary>
        /// Génère une facture PDF avec QR-Code intégré (méthode legacy)
        /// </summary>
        /// <param name="invoice">Facture à convertir</param>
        /// <param name="certificationResult">Résultat de certification</param>
        /// <returns>PDF de la facture avec QR-Code</returns>
        Task<byte[]> GenerateInvoicePdfWithQrCodeAsync(FneInvoice invoice, FneCertificationResult certificationResult);

        /// <summary>
        /// Obtient les informations de vérification publique d'une facture
        /// </summary>
        /// <param name="verificationToken">Token de vérification</param>
        /// <returns>Informations publiques de vérification</returns>
        Task<FnePublicVerificationResult> GetPublicVerificationInfoAsync(string verificationToken);
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
        
        // Données retournées par l'API FNE selon FNE-procedureapi.md
        public string? FneReference { get; set; }        // Numéro facture FNE
        public string? VerificationToken { get; set; }   // Token QR Code
        public string? NccEntreprise { get; set; }       // Identifiant contribuable
        public int StickerBalance { get; set; }         // Balance sticker facture
        public string? InvoiceId { get; set; }          // ID facture pour avoir/annulation
        
        // Nouvelles données importantes selon documentation
        public string? QrCodeData { get; set; }         // Données du QR-Code à générer
        public string? DownloadUrl { get; set; }        // URL de téléchargement/vérification
        public bool HasWarning { get; set; }            // Indicateur d'alerte
        public string? WarningMessage { get; set; }     // Message d'alerte détaillé
        
        // Détails complets de la facture certifiée
        public CertifiedInvoiceInfo? CertifiedInvoiceDetails { get; set; }
        
        // Gestion d'erreurs
        public List<string> Errors { get; set; } = new();
        public string? ErrorCode { get; set; }
        public int HttpStatusCode { get; set; }
        public long ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Informations détaillées de la facture certifiée
    /// </summary>
    public class CertifiedInvoiceInfo
    {
        public string? Id { get; set; }                  // ID pour les avoirs/annulations
        public string? ParentId { get; set; }           // ID facture parent (pour avoirs)
        public string? ParentReference { get; set; }    // Référence facture parent
        public string? Reference { get; set; }          // Numéro facture FNE
        public string? Type { get; set; }               // Type (invoice, refund, etc.)
        public string? Subtype { get; set; }            // Sous-type (normal, exceptional)
        public DateTime CertificationDate { get; set; } // Date de certification
        public string? PaymentMethod { get; set; }      // Méthode de paiement
        public decimal Amount { get; set; }             // Montant HT
        public decimal VatAmount { get; set; }          // Montant TVA
        public decimal FiscalStamp { get; set; }        // Timbre fiscal
        public decimal Discount { get; set; }           // Remise
        public string? ClientNcc { get; set; }          // NCC du client
        public string? ClientName { get; set; }         // Nom du client
        public string? ClientPhone { get; set; }        // Téléphone client
        public string? ClientEmail { get; set; }        // Email client
        public string? PointOfSale { get; set; }        // Point de vente
        public string? Establishment { get; set; }      // Établissement
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

    /// <summary>
    /// Résultat de validation d'un token FNE
    /// </summary>
    public class FneTokenValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? InvoiceReference { get; set; }
        public DateTime? CertificationDate { get; set; }
        public string? CompanyNcc { get; set; }
        public decimal? InvoiceAmount { get; set; }
        public string? TokenUrl { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    /// <summary>
    /// Résultat de téléchargement de facture certifiée
    /// </summary>
    public class FneCertifiedInvoiceDownloadResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public byte[]? PdfContent { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; } = "application/pdf";
        public long FileSizeBytes { get; set; }
        public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
        public string? InvoiceReference { get; set; }
        public string? VerificationUrl { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Informations publiques de vérification d'une facture
    /// </summary>
    public class FnePublicVerificationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? InvoiceReference { get; set; }
        public DateTime? CertificationDate { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyNcc { get; set; }
        public decimal? InvoiceAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ClientName { get; set; }
        public string? Status { get; set; } // "Valid", "Revoked", "Expired"
        public string? QrCodeData { get; set; } // QR-Code pour affichage
        public DateTime? ExpiryDate { get; set; }
        public List<string> ValidationDetails { get; set; } = new();
    }

    #endregion
}
