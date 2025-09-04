using System.ComponentModel.DataAnnotations;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Ligne de facture FNE selon structure Excel Sage 100
    /// Conforme aux spécifications colonnes B-H (ligne 20+)
    /// </summary>
    public class FneInvoiceItem : BaseEntity
    {
        /// <summary>
        /// Identifiant de la facture parente
        /// </summary>
        public Guid FneInvoiceId { get; set; }

        /// <summary>
        /// Code produit (Colonne B Excel)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>
        /// Désignation du produit (Colonne C Excel)
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Prix unitaire HT (Colonne D Excel)
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Quantité (Colonne E Excel)
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Unité/Emballage (Colonne F Excel)
        /// </summary>
        [MaxLength(50)]
        public string? MeasurementUnit { get; set; }

        /// <summary>
        /// Identifiant du type de TVA
        /// </summary>
        public Guid VatTypeId { get; set; }

        /// <summary>
        /// Code TVA (Colonne G Excel - TVA, TVAB, TVAC, TVAD)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string VatCode { get; set; } = string.Empty;

        /// <summary>
        /// Taux de TVA applicable (18%, 9%, 0%)
        /// </summary>
        [Range(0, 100)]
        public decimal VatRate { get; set; }

        /// <summary>
        /// Montant HT de la ligne (Colonne H Excel)
        /// Calculé : UnitPrice * Quantity * (1 - ItemDiscount/100)
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal LineAmountHT { get; set; }

        /// <summary>
        /// Montant TVA de la ligne
        /// Calculé : LineAmountHT * VatRate / 100
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal LineVatAmount { get; set; }

        /// <summary>
        /// Montant TTC de la ligne
        /// Calculé : LineAmountHT + LineVatAmount
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal LineAmountTTC { get; set; }

        /// <summary>
        /// Remise sur l'article (en pourcentage)
        /// </summary>
        [Range(0, 100)]
        public decimal ItemDiscount { get; set; }

        /// <summary>
        /// Référence article supplémentaire
        /// </summary>
        [MaxLength(100)]
        public string? Reference { get; set; }

        /// <summary>
        /// Position dans la facture (ordre d'affichage)
        /// </summary>
        public int LineOrder { get; set; }

        /// <summary>
        /// Identifiant FNE de la ligne (reçu de l'API après certification)
        /// </summary>
        [MaxLength(50)]
        public string? FneItemId { get; set; }

        /// <summary>
        /// Taxes personnalisées pour cette ligne (JSON)
        /// </summary>
        public string? CustomTaxes { get; set; }

        /// <summary>
        /// Navigation - Facture parente
        /// </summary>
        public virtual FneInvoice FneInvoice { get; set; } = null!;

        /// <summary>
        /// Navigation - Type de TVA
        /// </summary>
        public virtual VatType VatType { get; set; } = null!;
    }
}