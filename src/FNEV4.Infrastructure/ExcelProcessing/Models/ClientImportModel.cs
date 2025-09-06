using System.ComponentModel.DataAnnotations;

namespace FNEV4.Infrastructure.ExcelProcessing.Models
{
    /// <summary>
    /// Modèle pour l'import Excel des clients
    /// Mapping des colonnes Excel vers entité Client
    /// </summary>
    public class ClientImportModel
    {
        /// <summary>
        /// Code client unique (Colonne A)
        /// </summary>
        [Required(ErrorMessage = "Le code client est obligatoire")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Le code client doit contenir entre 2 et 50 caractères")]
        public string ClientCode { get; set; } = string.Empty;

        /// <summary>
        /// Nom/Raison sociale (Colonne B)
        /// </summary>
        [Required(ErrorMessage = "Le nom/raison sociale est obligatoire")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caractères")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type de client (Colonne C)
        /// Valeurs: Particulier, Entreprise, Association, Administration
        /// </summary>
        [Required(ErrorMessage = "Le type de client est obligatoire")]
        public string ClientType { get; set; } = string.Empty;

        /// <summary>
        /// NCC - Numéro Carte Contribuable (Colonne D)
        /// </summary>
        [StringLength(20, MinimumLength = 8, ErrorMessage = "Le NCC doit contenir entre 8 et 20 caractères")]
        [RegularExpression(@"^[0-9]{7,15}[A-Z]?$", ErrorMessage = "Format NCC invalide")]
        public string? ClientNcc { get; set; }

        /// <summary>
        /// Nom commercial/Intitulé (Colonne E)
        /// </summary>
        [StringLength(200, ErrorMessage = "Le nom commercial ne peut pas dépasser 200 caractères")]
        public string? CompanyName { get; set; }

        /// <summary>
        /// Adresse complète (Colonne F)
        /// </summary>
        [StringLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
        public string? Address { get; set; }

        /// <summary>
        /// Ville (Colonne G)
        /// </summary>
        [StringLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères")]
        public string? City { get; set; }

        /// <summary>
        /// Code postal (Colonne H)
        /// </summary>
        [StringLength(10, ErrorMessage = "Le code postal ne peut pas dépasser 10 caractères")]
        public string? PostalCode { get; set; }

        /// <summary>
        /// Pays (Colonne I)
        /// </summary>
        [StringLength(50, ErrorMessage = "Le pays ne peut pas dépasser 50 caractères")]
        public string? Country { get; set; }

        /// <summary>
        /// Téléphone (Colonne J)
        /// </summary>
        [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        [RegularExpression(@"^[\+]?[0-9\s\-\(\)\.]{8,20}$", ErrorMessage = "Format de téléphone invalide")]
        public string? Phone { get; set; }

        /// <summary>
        /// Email professionnel (Colonne K)
        /// </summary>
        [StringLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string? Email { get; set; }

        /// <summary>
        /// Nom du représentant/vendeur (Colonne L)
        /// </summary>
        [StringLength(100, ErrorMessage = "Le nom du représentant ne peut pas dépasser 100 caractères")]
        public string? SellerName { get; set; }

        /// <summary>
        /// Numéro identification fiscale (Colonne M)
        /// </summary>
        [StringLength(50, ErrorMessage = "Le numéro fiscal ne peut pas dépasser 50 caractères")]
        public string? TaxIdentificationNumber { get; set; }

        /// <summary>
        /// Devise par défaut (Colonne N)
        /// </summary>
        [StringLength(3, MinimumLength = 3, ErrorMessage = "La devise doit contenir exactement 3 caractères")]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Format de devise invalide (ex: XOF, EUR, USD)")]
        public string? DefaultCurrency { get; set; }

        /// <summary>
        /// Client actif (Colonne O)
        /// Valeurs: Oui/Non, True/False, 1/0
        /// </summary>
        public string? IsActiveText { get; set; }

        /// <summary>
        /// Notes et commentaires (Colonne P)
        /// </summary>
        public string? Notes { get; set; }

        // Propriétés calculées pour validation
        public bool IsActive => ParseBooleanField(IsActiveText, defaultValue: true);
        public string DefaultTemplate => GetDefaultTemplate(ClientType);

        /// <summary>
        /// Numéro de ligne Excel (pour reporting d'erreurs)
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// Liste des erreurs de validation pour cette ligne
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new();

        #region Helper Methods

        private static bool ParseBooleanField(string? value, bool defaultValue = true)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            var normalized = value.Trim().ToLowerInvariant();
            
            return normalized switch
            {
                "oui" or "yes" or "true" or "1" or "actif" or "active" => true,
                "non" or "no" or "false" or "0" or "inactif" or "inactive" => false,
                _ => defaultValue
            };
        }

        private static string GetDefaultTemplate(string clientType)
        {
            return clientType?.ToLowerInvariant() switch
            {
                "particulier" or "individual" => "B2C",
                "entreprise" or "company" => "B2B",
                "administration" or "government" => "B2G",
                "association" or "ong" or "ngo" => "B2C",
                _ => "B2C"
            };
        }

        #endregion
    }
}
