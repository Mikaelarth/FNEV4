using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Entité FneCustomTax - Taxes personnalisées au niveau facture
    /// Selon API FNE: "customTaxes": [{"name": "DTD", "amount": 5}]
    /// </summary>
    public class FneCustomTax : BaseEntity
    {
        [Required]
        [MaxLength(36)]
        public string FneInvoiceId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // ex: "DTD", "GRA"

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Rate { get; set; } // Taux en pourcentage

        [Column(TypeName = "decimal(15,2)")]
        public decimal CalculatedAmount { get; set; } // Montant calculé

        [MaxLength(200)]
        public string? Description { get; set; }

        // === RELATIONS ===
        
        [ForeignKey("FneInvoiceId")]
        public virtual FneInvoice FneInvoice { get; set; } = null!;

        // === MÉTHODES ===
        
        public void CalculateAmount(decimal baseAmount)
        {
            CalculatedAmount = baseAmount * (Rate / 100);
        }
    }

    /// <summary>
    /// Entité FneItemCustomTax - Taxes personnalisées au niveau item
    /// Selon API FNE: items[].customTaxes: [{"name": "GRA", "amount": 5}]
    /// </summary>
    public class FneItemCustomTax : BaseEntity
    {
        [Required]
        [MaxLength(36)]
        public string FneInvoiceItemId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // ex: "GRA", "AIRSI"

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Rate { get; set; } // Taux en pourcentage

        [Column(TypeName = "decimal(15,2)")]
        public decimal CalculatedAmount { get; set; } // Montant calculé

        [MaxLength(200)]
        public string? Description { get; set; }

        // === RELATIONS ===
        
        [ForeignKey("FneInvoiceItemId")]
        public virtual FneInvoiceItem FneInvoiceItem { get; set; } = null!;

        // === MÉTHODES ===
        
        public void CalculateAmount(decimal baseAmount)
        {
            CalculatedAmount = baseAmount * (Rate / 100);
        }
    }

    /// <summary>
    /// Entité FneStickerBalance - Gestion du stock de stickers
    /// CRUCIAL selon API FNE: "balance_sticker": 179
    /// </summary>
    public class FneStickerBalance : BaseEntity
    {
        [Required]
        public int CompanyId { get; set; }

        [Required]
        public int CurrentBalance { get; set; } // Solde actuel

        [Required]
        public int InitialBalance { get; set; } // Solde initial

        [Required]
        public int UsedStickers { get; set; } // Stickers utilisés

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public DateTime? LastRefillDate { get; set; } // Dernière recharge

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Seuils d'alerte
        public int WarningThreshold { get; set; } = 50;
        public int CriticalThreshold { get; set; } = 10;

        // === RELATIONS ===
        
        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;

        // === MÉTHODES ===
        
        public bool IsWarningLevel => CurrentBalance <= WarningThreshold && CurrentBalance > CriticalThreshold;
        public bool IsCriticalLevel => CurrentBalance <= CriticalThreshold;
        
        public void ConsumeSticker()
        {
            if (CurrentBalance > 0)
            {
                CurrentBalance--;
                UsedStickers++;
                LastUpdated = DateTime.UtcNow;
            }
        }

        public void RefillStickers(int quantity, string? notes = null)
        {
            CurrentBalance += quantity;
            InitialBalance += quantity;
            LastRefillDate = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(notes))
            {
                Notes = notes;
            }
        }
    }


}
