using FNEV4.Core.Models.ImportTraitement;

namespace FNEV4.Core.Interfaces.Services
{
    /// <summary>
    /// Service de certification des factures via l'API FNE
    /// Implémente la procédure officielle selon FNE-procedureapi.md
    /// </summary>
    public interface IFneCertificationService
    {
        /// <summary>
        /// Certifie une facture de vente via l'API FNE
        /// </summary>
        /// <param name="factureData">Données de la facture Sage100</param>
        /// <returns>Réponse de l'API FNE avec référence et token</returns>
        Task<FneCertificationResult> CertifyInvoiceAsync(Sage100FactureData factureData);

        /// <summary>
        /// Convertit les données Sage100 vers le format JSON FNE
        /// </summary>
        /// <param name="factureData">Données de la facture</param>
        /// <returns>Objet JSON prêt pour l'API FNE</returns>
        Task<object> ConvertToFneApiJsonAsync(Sage100FactureData factureData);

        /// <summary>
        /// Valide que les données sont conformes pour la certification FNE
        /// </summary>
        /// <param name="factureData">Données à valider</param>
        /// <returns>Résultat de validation</returns>
        Task<FneValidationResult> ValidateForCertificationAsync(Sage100FactureData factureData);
    }

    /// <summary>
    /// Résultat de certification FNE
    /// </summary>
    public class FneCertificationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        
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
    }
}
