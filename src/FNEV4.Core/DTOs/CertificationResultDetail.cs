using System;

namespace FNEV4.Core.DTOs
{
    /// <summary>
    /// DTO contenant les détails d'un résultat de certification
    /// </summary>
    public class CertificationResultDetail
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string FneReference { get; set; } = string.Empty;
        public string VerificationToken { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        
        /// <summary>
        /// Statut pour affichage
        /// </summary>
        public string Status => IsSuccess ? "✅ Succès" : "❌ Échec";
        
        /// <summary>
        /// Détails pour affichage
        /// </summary>
        public string Details
        {
            get
            {
                if (IsSuccess)
                {
                    return string.IsNullOrEmpty(FneReference) 
                        ? "🚫 ERREUR: Pas de référence FNE - Service non configuré" 
                        : $"Ref FNE: {FneReference}";
                }
                return ErrorMessage;
            }
        }
    }
}