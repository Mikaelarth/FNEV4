using System.ComponentModel.DataAnnotations;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Configuration de l'entreprise utilisatrice
    /// Paramètres DGI et points de vente selon ARCHITECTURE.md
    /// </summary>
    public class Company : BaseEntity
    {
        /// <summary>
        /// NCC (Numéro de Compte Contribuable) - Ligne A6 Excel
        /// Identifiant fiscal principal DGI
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Ncc { get; set; } = string.Empty;

        /// <summary>
        /// Raison sociale de l'entreprise
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Nom commercial
        /// </summary>
        [MaxLength(200)]
        public string? TradeName { get; set; }

        /// <summary>
        /// Adresse principale du siège
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Numéro de téléphone principal
        /// </summary>
        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>
        /// Adresse e-mail de contact
        /// </summary>
        [MaxLength(100)]
        public string? Email { get; set; }

        /// <summary>
        /// Site web de l'entreprise
        /// </summary>
        [MaxLength(200)]
        public string? Website { get; set; }

        /// <summary>
        /// Secteur d'activité
        /// </summary>
        [MaxLength(100)]
        public string? Industry { get; set; }

        /// <summary>
        /// Capital social
        /// </summary>
        public decimal? ShareCapital { get; set; }

        /// <summary>
        /// Numéro RCCM (Registre de Commerce)
        /// </summary>
        [MaxLength(50)]
        public string? RccmNumber { get; set; }

        /// <summary>
        /// Points de vente configurés (JSON)
        /// Format: [{"Code":"01","Name":"Siege","Address":"..."},...]
        /// </summary>
        public string? PointsOfSale { get; set; }

        /// <summary>
        /// Point de vente par défaut (ex: 01, 23)
        /// </summary>
        [MaxLength(10)]
        public string? DefaultPointOfSale { get; set; }

        /// <summary>
        /// Clé API FNE (chiffrée)
        /// </summary>
        [MaxLength(500)]
        public string? ApiKey { get; set; }

        /// <summary>
        /// URL environnement FNE (test/production)
        /// </summary>
        [MaxLength(200)]
        public string? ApiBaseUrl { get; set; }

        /// <summary>
        /// Environnement API actuel (Test/Production)
        /// </summary>
        [MaxLength(20)]
        public string Environment { get; set; } = "Test";

        /// <summary>
        /// Configuration de l'entreprise est validée par la DGI
        /// </summary>
        public bool IsValidated { get; set; } = false;

        /// <summary>
        /// Date de validation DGI
        /// </summary>
        public DateTime? ValidatedAt { get; set; }

        /// <summary>
        /// Solde de stickers électroniques
        /// </summary>
        public int StickerBalance { get; set; } = 0;

        /// <summary>
        /// Seuil d'alerte pour les stickers
        /// </summary>
        public int StickerAlertThreshold { get; set; } = 50;

        /// <summary>
        /// Paramètres de facturation par défaut (JSON)
        /// Messages commerciaux, footer, etc.
        /// </summary>
        public string? DefaultInvoiceSettings { get; set; }

        /// <summary>
        /// Logo de l'entreprise (Base64 ou chemin)
        /// </summary>
        public string? Logo { get; set; }

        /// <summary>
        /// Signature électronique (certificat)
        /// </summary>
        public string? ElectronicSignature { get; set; }

        /// <summary>
        /// Date de création du profil
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date de dernière synchronisation avec la DGI
        /// </summary>
        public DateTime? LastSyncDate { get; set; }

        /// <summary>
        /// Indique si l'entreprise est active
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}