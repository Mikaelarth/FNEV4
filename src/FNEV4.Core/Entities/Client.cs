using System.ComponentModel.DataAnnotations;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Client conforme aux spécifications Excel Sage 100
    /// Référentiel clients avec NCC et classification
    /// </summary>
    public class Client : BaseEntity
    {
        /// <summary>
        /// Code client Sage (1999 pour client divers)
        /// Ligne A5 Excel
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ClientCode { get; set; } = string.Empty;

        /// <summary>
        /// NCC du client (pour B2B) - Ligne A15 Excel
        /// </summary>
        [MaxLength(20)]
        public string? ClientNcc { get; set; }

        /// <summary>
        /// Nom/Raison sociale du client - Ligne A13 Excel
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Intitulé client (nom commercial) - Ligne A11 Excel
        /// </summary>
        [MaxLength(200)]
        public string? CompanyName { get; set; }

        /// <summary>
        /// Adresse complète
        /// </summary>
        [MaxLength(500)]
        public string? Address { get; set; }

        /// <summary>
        /// Numéro de téléphone
        /// </summary>
        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>
        /// Adresse e-mail
        /// </summary>
        [MaxLength(100)]
        public string? Email { get; set; }

        /// <summary>
        /// Type de client (Individual, Company, Government, International)
        /// Détermine le template de facturation (B2C, B2B, B2G, B2F)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string ClientType { get; set; } = "Individual";

        /// <summary>
        /// Template de facturation par défaut (B2C, B2B, B2G, B2F)
        /// </summary>
        [Required]
        [MaxLength(5)]
        public string DefaultTemplate { get; set; } = "B2C";

        /// <summary>
        /// Moyen de paiement par défaut du client (cash, card, mobile-money, bank-transfer, etc.)
        /// Obligatoire pour la certification des factures selon l'API DGI
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string DefaultPaymentMethod { get; set; } = "cash";

        /// <summary>
        /// Indique si le client est actif
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Pays (pour clients internationaux B2F)
        /// </summary>
        [MaxLength(50)]
        public string? Country { get; set; }

        /// <summary>
        /// Devise par défaut (pour B2F)
        /// </summary>
        [MaxLength(3)]
        public string? DefaultCurrency { get; set; }

        /// <summary>
        /// Nom du représentant/vendeur
        /// </summary>
        [MaxLength(100)]
        public string? SellerName { get; set; }

        /// <summary>
        /// Numéro d'identification fiscale (autre que NCC)
        /// </summary>
        [MaxLength(50)]
        public string? TaxIdentificationNumber { get; set; }

        /// <summary>
        /// Notes et commentaires
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date de création du client
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }

        /// <summary>
        /// Navigation - Factures du client
        /// </summary>
        public virtual ICollection<FneInvoice> Invoices { get; set; } = new List<FneInvoice>();
    }
}
