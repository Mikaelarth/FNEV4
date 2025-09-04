using System.ComponentModel.DataAnnotations;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Types de TVA supportés par le système FNE
    /// Conforme aux spécifications API DGI (TVA, TVAB, TVAC, TVAD)
    /// </summary>
    public class VatType : BaseEntity
    {
        /// <summary>
        /// Code TVA (TVA, TVAB, TVAC, TVAD)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Description du type de TVA
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Taux de TVA en pourcentage
        /// TVA = 18%, TVAB = 9%, TVAC = 0%, TVAD = 0%
        /// </summary>
        [Range(0, 100)]
        public decimal Rate { get; set; }

        /// <summary>
        /// Indique si ce type de TVA est actif
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Lignes de facture utilisant ce type de TVA
        /// </summary>
        public virtual ICollection<FneInvoiceItem> InvoiceItems { get; set; } = new List<FneInvoiceItem>();
    }
}
