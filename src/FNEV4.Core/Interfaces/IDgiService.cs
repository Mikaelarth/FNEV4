using System;
using System.Threading.Tasks;

namespace FNEV4.Core.Interfaces.Services.Fne
{
    /// <summary>
    /// Interface pour les services d'intégration avec l'API DGI (Direction Générale des Impôts)
    /// Conformément à la documentation FNE-procedureapi.md
    /// </summary>
    public interface IDgiService
    {
        /// <summary>
        /// Vérifie la validité d'un NCC auprès de la base DGI
        /// </summary>
        /// <param name="nccNumber">Numéro NCC à vérifier (format: 7 chiffres + 1 lettre)</param>
        /// <returns>Résultat de la vérification avec détails</returns>
        Task<DgiVerificationResult> VerifyNccAsync(string nccNumber);

        /// <summary>
        /// Teste la connectivité avec l'API DGI
        /// </summary>
        /// <returns>True si l'API DGI est accessible</returns>
        Task<bool> TestConnectivityAsync();

        /// <summary>
        /// Teste la connexion à l'API DGI
        /// </summary>
        /// <returns>True si la connexion est établie</returns>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// Vérifie si l'API DGI est configurée
        /// </summary>
        /// <returns>True si la configuration est valide</returns>
        Task<bool> IsConfiguredAsync();

        /// <summary>
        /// Obtient les informations d'une entreprise via son NCC
        /// </summary>
        /// <param name="nccNumber">Numéro NCC de l'entreprise</param>
        /// <returns>Informations de l'entreprise ou null si non trouvée</returns>
        Task<DgiCompanyInfo?> GetCompanyInfoAsync(string nccNumber);
    }

    /// <summary>
    /// Résultat de la vérification DGI
    /// </summary>
    public class DgiVerificationResult
    {
        /// <summary>
        /// Indique si la vérification a réussi
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Indique si le NCC est valide dans la base DGI
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Message descriptif du résultat
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Détails techniques de l'erreur (si applicable)
        /// </summary>
        public string? ErrorDetails { get; set; }

        /// <summary>
        /// Code d'erreur HTTP (si applicable)
        /// </summary>
        public int? HttpStatusCode { get; set; }

        /// <summary>
        /// Durée de l'appel API
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Informations supplémentaires de l'entreprise (si trouvée)
        /// </summary>
        public DgiCompanyInfo? CompanyInfo { get; set; }
    }

    /// <summary>
    /// Informations d'entreprise retournées par la DGI
    /// </summary>
    public class DgiCompanyInfo
    {
        /// <summary>
        /// Numéro NCC de l'entreprise
        /// </summary>
        public string Ncc { get; set; } = string.Empty;

        /// <summary>
        /// Nom de l'entreprise
        /// </summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Statut de l'entreprise (Actif, Suspendu, etc.)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Date d'enregistrement
        /// </summary>
        public DateTime? RegistrationDate { get; set; }

        /// <summary>
        /// Secteur d'activité
        /// </summary>
        public string? BusinessSector { get; set; }

        /// <summary>
        /// Adresse de l'entreprise
        /// </summary>
        public string? Address { get; set; }
    }
}
