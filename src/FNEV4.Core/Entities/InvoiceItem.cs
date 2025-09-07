using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Entité représentant une ligne de facture
    /// Compatible avec Sage 100 v15 et API DGI
    /// </summary>
    [Table("InvoiceItems")]
    public class InvoiceItem : BaseEntity
    {
        #region Identifiants

        /// <summary>
        /// Identifiant unique de la ligne
        /// </summary>
        [Key]
        public new int Id { get; set; }

        /// <summary>
        /// Identifiant de la facture parente
        /// </summary>
        [Required]
        [ForeignKey(nameof(Invoice))]
        public int InvoiceId { get; set; }

        /// <summary>
        /// Numéro de ligne dans la facture (ordre)
        /// </summary>
        [Required]
        public int LineNumber { get; set; }

        #endregion

        #region Informations Produit

        /// <summary>
        /// Code produit Sage 100
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>
        /// Désignation/Description du produit
        /// </summary>
        [Required]
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Unité de mesure/Emballage
        /// </summary>
        [StringLength(20)]
        [Column(TypeName = "nvarchar(20)")]
        public string Unit { get; set; } = "pcs";

        #endregion

        #region Quantités et Prix

        /// <summary>
        /// Prix unitaire hors taxes
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Quantité
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Montant hors taxes (UnitPrice × Quantity)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountHT { get; set; }

        #endregion

        #region TVA

        /// <summary>
        /// Type de TVA Sage 100
        /// TVA = 18%, TVAB = 9%, TVAC = 0%, TVAD = 0%
        /// </summary>
        [Required]
        [StringLength(10)]
        [Column(TypeName = "nvarchar(10)")]
        public string VatType { get; set; } = "TVA";

        /// <summary>
        /// Taux de TVA (pourcentage décimal)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(5,4)")]
        public decimal VatRate { get; set; }

        /// <summary>
        /// Montant de la TVA
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        /// <summary>
        /// Montant toutes taxes comprises
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountTTC { get; set; }

        #endregion

        #region Relations

        /// <summary>
        /// Facture parente
        /// </summary>
        public virtual Invoice? Invoice { get; set; }

        #endregion

        #region Propriétés Calculées

        /// <summary>
        /// Montant HT calculé (UnitPrice × Quantity)
        /// </summary>
        [NotMapped]
        public decimal CalculatedAmountHT => UnitPrice * Quantity;

        /// <summary>
        /// Montant TVA calculé (AmountHT × VatRate)
        /// </summary>
        [NotMapped]
        public decimal CalculatedVatAmount => AmountHT * VatRate;

        /// <summary>
        /// Montant TTC calculé (AmountHT + VatAmount)
        /// </summary>
        [NotMapped]
        public decimal CalculatedAmountTTC => AmountHT + VatAmount;

        /// <summary>
        /// Taux de TVA selon le type Sage 100
        /// </summary>
        [NotMapped]
        public decimal StandardVatRateByType => VatType?.ToUpper() switch
        {
            "TVA" => 0.18m,    // 18%
            "TVAB" => 0.09m,   // 9%
            "TVAC" => 0.00m,   // 0% (convention)
            "TVAD" => 0.00m,   // 0% (légale)
            _ => 0.18m         // Par défaut 18%
        };

        /// <summary>
        /// Résumé de la ligne pour affichage
        /// </summary>
        [NotMapped]
        public string DisplaySummary => $"{ProductCode} - {Description} - {Quantity} × {UnitPrice:C} = {AmountTTC:C}";

        #endregion

        #region Validation et Calculs

        /// <summary>
        /// Valide la cohérence des calculs
        /// </summary>
        public bool ValidateCalculations()
        {
            const decimal tolerance = 0.01m;

            // Vérifier UnitPrice × Quantity = AmountHT
            if (Math.Abs(AmountHT - CalculatedAmountHT) > tolerance)
                return false;

            // Vérifier AmountHT × VatRate = VatAmount
            if (Math.Abs(VatAmount - CalculatedVatAmount) > tolerance)
                return false;

            // Vérifier AmountHT + VatAmount = AmountTTC
            if (Math.Abs(AmountTTC - CalculatedAmountTTC) > tolerance)
                return false;

            return true;
        }

        /// <summary>
        /// Recalcule automatiquement tous les montants
        /// </summary>
        public void RecalculateAmounts()
        {
            // 1. Calculer montant HT
            AmountHT = Math.Round(UnitPrice * Quantity, 2);

            // 2. Appliquer le taux de TVA standard si pas déjà défini
            if (VatRate == 0 && !string.IsNullOrWhiteSpace(VatType))
            {
                VatRate = StandardVatRateByType;
            }

            // 3. Calculer montant TVA
            VatAmount = Math.Round(AmountHT * VatRate, 2);

            // 4. Calculer montant TTC
            AmountTTC = AmountHT + VatAmount;

            // 5. Mettre à jour la date de modification
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Définit le taux de TVA selon le type Sage 100
        /// </summary>
        public void SetVatRateByType()
        {
            VatRate = StandardVatRateByType;
            RecalculateAmounts();
        }

        /// <summary>
        /// Applique une remise en pourcentage
        /// </summary>
        public void ApplyDiscount(decimal discountPercent)
        {
            if (discountPercent < 0 || discountPercent > 100)
                throw new ArgumentException("Le pourcentage de remise doit être entre 0 et 100");

            var discountAmount = UnitPrice * (discountPercent / 100);
            UnitPrice -= discountAmount;
            RecalculateAmounts();
        }

        #endregion

        #region Méthodes d'Initialisation

        /// <summary>
        /// Initialise une ligne de facture avec calculs automatiques
        /// </summary>
        public static InvoiceItem Create(
            string productCode, 
            string description, 
            decimal unitPrice, 
            decimal quantity, 
            string vatType = "TVA",
            string unit = "pcs")
        {
            var item = new InvoiceItem
            {
                ProductCode = productCode,
                Description = description,
                UnitPrice = unitPrice,
                Quantity = quantity,
                VatType = vatType,
                Unit = unit,
                CreatedAt = DateTime.Now
            };

            item.SetVatRateByType();
            return item;
        }

        /// <summary>
        /// Clone la ligne de facture
        /// </summary>
        public InvoiceItem Clone()
        {
            return new InvoiceItem
            {
                ProductCode = ProductCode,
                Description = Description,
                UnitPrice = UnitPrice,
                Quantity = Quantity,
                Unit = Unit,
                VatType = VatType,
                VatRate = VatRate,
                AmountHT = AmountHT,
                VatAmount = VatAmount,
                AmountTTC = AmountTTC,
                CreatedAt = DateTime.Now
            };
        }

        #endregion
    }
}
