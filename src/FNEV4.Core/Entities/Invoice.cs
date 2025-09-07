using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Entité représentant une facture
    /// Compatible avec Sage 100 v15 et API DGI
    /// </summary>
    [Table("Invoices")]
    public class Invoice : BaseEntity
    {
        #region Identifiants

        /// <summary>
        /// Identifiant unique de la facture (override pour utiliser int au lieu de Guid)
        /// </summary>
        [Key]
        public new int Id { get; set; }

        /// <summary>
        /// Numéro de facture (unique)
        /// Format : FAC-YYYY-NNNN
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string InvoiceNumber { get; set; } = string.Empty;

        #endregion

        #region Informations Client

        /// <summary>
        /// Code client Sage 100
        /// 1999 = client divers, autre = client référencé
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column(TypeName = "nvarchar(20)")]
        public string CustomerCode { get; set; } = string.Empty;

        /// <summary>
        /// NCC (Numéro de Compte Contribuable) du client
        /// 8-11 caractères alphanumériques
        /// </summary>
        [Required]
        [StringLength(11)]
        [Column(TypeName = "nvarchar(11)")]
        public string CustomerNcc { get; set; } = string.Empty;

        /// <summary>
        /// Nom affiché du client (générique ou réel)
        /// </summary>
        [Required]
        [StringLength(200)]
        [Column(TypeName = "nvarchar(200)")]
        public string CustomerTitle { get; set; } = string.Empty;

        /// <summary>
        /// Nom réel du client (pour clients divers)
        /// </summary>
        [StringLength(200)]
        [Column(TypeName = "nvarchar(200)")]
        public string? CustomerRealName { get; set; }

        #endregion

        #region Informations Facture

        /// <summary>
        /// Date de facturation
        /// </summary>
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime InvoiceDate { get; set; }

        /// <summary>
        /// Point de vente / Magasin
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string PointOfSale { get; set; } = string.Empty;

        /// <summary>
        /// Moyen de paiement API DGI
        /// cash, card, mobile-money, bank-transfer, check, credit
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column(TypeName = "nvarchar(20)")]
        public string PaymentMethod { get; set; } = "cash";

        /// <summary>
        /// Référence facture originale (pour avoirs)
        /// </summary>
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string? CreditNoteReference { get; set; }

        /// <summary>
        /// Indique si c'est une facture d'avoir
        /// </summary>
        [Column(TypeName = "bit")]
        public bool IsCreditNote => !string.IsNullOrWhiteSpace(CreditNoteReference);

        #endregion

        #region Montants

        /// <summary>
        /// Montant total hors taxes
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmountHT { get; set; }

        /// <summary>
        /// Montant total de la TVA
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVatAmount { get; set; }

        /// <summary>
        /// Montant total toutes taxes comprises
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmountTTC { get; set; }

        #endregion

        #region Certification FNE

        /// <summary>
        /// Numéro de transaction FNE (après certification)
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? FneTransactionNumber { get; set; }

        /// <summary>
        /// Date de certification FNE
        /// </summary>
        [Column(TypeName = "datetime2")]
        public DateTime? FneCertificationDate { get; set; }

        /// <summary>
        /// Statut de certification FNE
        /// </summary>
        [StringLength(20)]
        [Column(TypeName = "nvarchar(20)")]
        public string FneStatus { get; set; } = "Pending";

        /// <summary>
        /// Message de réponse de l'API FNE
        /// </summary>
        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? FneResponseMessage { get; set; }

        #endregion

        #region Métadonnées Import

        /// <summary>
        /// Nom du fichier Excel source
        /// </summary>
        [StringLength(255)]
        [Column(TypeName = "nvarchar(255)")]
        public string? SourceFileName { get; set; }

        /// <summary>
        /// Nom de la feuille Excel source
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? SourceSheetName { get; set; }

        /// <summary>
        /// Statut de traitement
        /// Imported, Validated, Certified, Error
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column(TypeName = "nvarchar(20)")]
        public string Status { get; set; } = "Imported";

        /// <summary>
        /// Notes et commentaires
        /// </summary>
        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string? Notes { get; set; }

        #endregion

        #region Relations

        /// <summary>
        /// Lignes de la facture
        /// </summary>
        public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

        #endregion

        #region Propriétés Calculées

        /// <summary>
        /// Nombre de lignes dans la facture
        /// </summary>
        [NotMapped]
        public int ItemCount => Items?.Count ?? 0;

        /// <summary>
        /// Indique si la facture est certifiée FNE
        /// </summary>
        [NotMapped]
        public bool IsFneCertified => FneStatus == "Certified" && !string.IsNullOrWhiteSpace(FneTransactionNumber);

        /// <summary>
        /// Type de client selon le code
        /// </summary>
        [NotMapped]
        public string ClientType => CustomerCode == "1999" ? "Divers" : "Normal";

        /// <summary>
        /// Résumé de la facture pour affichage
        /// </summary>
        [NotMapped]
        public string DisplaySummary => $"{InvoiceNumber} - {CustomerTitle} - {TotalAmountTTC:C} - {InvoiceDate:dd/MM/yyyy}";

        #endregion

        #region Validation

        /// <summary>
        /// Valide la cohérence des montants
        /// </summary>
        public bool ValidateAmounts()
        {
            if (Items == null || !Items.Any())
                return TotalAmountHT == 0 && TotalVatAmount == 0 && TotalAmountTTC == 0;

            var calculatedHT = Items.Sum(i => i.AmountHT);
            var calculatedVat = Items.Sum(i => i.VatAmount);
            var calculatedTTC = Items.Sum(i => i.AmountTTC);

            const decimal tolerance = 0.01m;

            return Math.Abs(TotalAmountHT - calculatedHT) <= tolerance &&
                   Math.Abs(TotalVatAmount - calculatedVat) <= tolerance &&
                   Math.Abs(TotalAmountTTC - calculatedTTC) <= tolerance;
        }

        /// <summary>
        /// Recalcule automatiquement les totaux depuis les lignes
        /// </summary>
        public void RecalculateTotals()
        {
            if (Items == null || !Items.Any())
            {
                TotalAmountHT = 0;
                TotalVatAmount = 0;
                TotalAmountTTC = 0;
                return;
            }

            TotalAmountHT = Items.Sum(i => i.AmountHT);
            TotalVatAmount = Items.Sum(i => i.VatAmount);
            TotalAmountTTC = Items.Sum(i => i.AmountTTC);
        }

        #endregion

        #region Méthodes Métier

        /// <summary>
        /// Marque la facture comme certifiée FNE
        /// </summary>
        public void MarkAsFneCertified(string transactionNumber, string? responseMessage = null)
        {
            FneTransactionNumber = transactionNumber;
            FneCertificationDate = DateTime.Now;
            FneStatus = "Certified";
            FneResponseMessage = responseMessage;
            Status = "Certified";
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Marque la facture en erreur FNE
        /// </summary>
        public void MarkAsFneError(string errorMessage)
        {
            FneStatus = "Error";
            FneResponseMessage = errorMessage;
            Status = "Error";
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Ajoute une ligne à la facture
        /// </summary>
        public void AddItem(InvoiceItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            
            item.Invoice = this;
            item.InvoiceId = Id;
            Items.Add(item);
            RecalculateTotals();
        }

        /// <summary>
        /// Supprime une ligne de la facture
        /// </summary>
        public void RemoveItem(InvoiceItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            
            Items.Remove(item);
            RecalculateTotals();
        }

        #endregion
    }
}
