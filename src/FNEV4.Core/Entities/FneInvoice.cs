using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Facture FNE selon les spécifications DGI
    /// Conforme au cahier des charges et API FNE
    /// </summary>
    public class FneInvoice : BaseEntity
    {
        /// <summary>
        /// Numéro de facture (issu de Sage ou généré)
        /// Exemple: FAC001, A9606123E2500000006 (pour avoir)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Référence FNE après certification (ex: 9606123E25000000019)
        /// </summary>
        [MaxLength(50)]
        public string? FneReference { get; set; }

        /// <summary>
        /// Type de facture (sale, purchase, refund)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string InvoiceType { get; set; } = "sale";

        /// <summary>
        /// Date de la facture
        /// </summary>
        public DateTime InvoiceDate { get; set; }

        /// <summary>
        /// Identifiant client (référence étrangère)
        /// </summary>
        public Guid ClientId { get; set; }

        /// <summary>
        /// Code client Sage (1999 pour divers)
        /// </summary>
        [MaxLength(50)]
        public string ClientCode { get; set; } = string.Empty;

        /// <summary>
        /// Point de vente (ex: 01, 23)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string PointOfSale { get; set; } = string.Empty;

        /// <summary>
        /// Nom de l'établissement
        /// </summary>
        [MaxLength(200)]
        public string? Establishment { get; set; }

        /// <summary>
        /// Méthode de paiement (cash, card, mobile-money, etc.)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = "cash";

        /// <summary>
        /// Template de facturation (B2B, B2C, B2G, B2F)
        /// </summary>
        [Required]
        [MaxLength(5)]
        public string Template { get; set; } = "B2C";

        /// <summary>
        /// Montant total HT de la facture
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal TotalAmountHT { get; set; }

        /// <summary>
        /// Montant total TVA
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal TotalVatAmount { get; set; }

        /// <summary>
        /// Montant total TTC
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal TotalAmountTTC { get; set; }

        /// <summary>
        /// Remise globale sur la facture
        /// </summary>
        [Range(0, 100)]
        public decimal GlobalDiscount { get; set; }

        /// <summary>
        /// Statut de la facture (Draft, Validated, Certified, Error)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Draft";

        /// <summary>
        /// Token de vérification FNE (pour QR Code)
        /// </summary>
        [MaxLength(500)]
        public string? VerificationToken { get; set; }

        /// <summary>
        /// URL complète de vérification avec token
        /// </summary>
        [MaxLength(1000)]
        public string? VerificationUrl { get; set; }

        /// <summary>
        /// Identifiant de la facture parente (pour les avoirs)
        /// </summary>
        public Guid? ParentInvoiceId { get; set; }

        /// <summary>
        /// Numéro de reçu normalisé associé (RNE)
        /// </summary>
        [MaxLength(50)]
        public string? RneNumber { get; set; }

        /// <summary>
        /// Indique si la facture est liée à un RNE
        /// </summary>
        public bool IsRne { get; set; } = false;

        /// <summary>
        /// Message commercial personnalisé
        /// </summary>
        [MaxLength(500)]
        public string? CommercialMessage { get; set; }

        /// <summary>
        /// Footer personnalisé
        /// </summary>
        [MaxLength(500)]
        public string? Footer { get; set; }

        /// <summary>
        /// Devise étrangère (pour B2F)
        /// </summary>
        [MaxLength(3)]
        public string? ForeignCurrency { get; set; }

        /// <summary>
        /// Taux de change devise étrangère
        /// </summary>
        public decimal? ForeignCurrencyRate { get; set; }

        /// <summary>
        /// Identifiant session d'import
        /// </summary>
        public Guid? ImportSessionId { get; set; }

        /// <summary>
        /// Date de certification FNE
        /// </summary>
        public DateTime? CertifiedAt { get; set; }

        /// <summary>
        /// Numéro de certificat FNE
        /// </summary>
        [MaxLength(100)]
        public string? FneCertificationNumber { get; set; }

        /// <summary>
        /// Date de certification FNE retournée par l'API
        /// </summary>
        public DateTime? FneCertificationDate { get; set; }

        /// <summary>
        /// Code QR de certification FNE
        /// </summary>
        [MaxLength(2000)]
        public string? FneQrCode { get; set; }

        /// <summary>
        /// Signature numérique FNE
        /// </summary>
        [MaxLength(2000)]
        public string? FneDigitalSignature { get; set; }

        /// <summary>
        /// URL de validation FNE
        /// </summary>
        [MaxLength(500)]
        public string? FneValidationUrl { get; set; }

        /// <summary>
        /// Balance de stickers après certification (de l'API FNE)
        /// </summary>
        public int? FneStickerBalance { get; set; }

        /// <summary>
        /// ID de la facture dans le système FNE (pour avoirs et références)
        /// </summary>
        [MaxLength(100)]
        public string? FneInvoiceId { get; set; }

        /// <summary>
        /// URL de téléchargement de la facture certifiée PDF
        /// </summary>
        [MaxLength(500)]
        public string? FneDownloadUrl { get; set; }

        /// <summary>
        /// Données détaillées de la facture certifiée (JSON)
        /// </summary>
        [MaxLength(4000)]
        public string? FneCertifiedInvoiceDetails { get; set; }

        /// <summary>
        /// Warning retourné par l'API FNE
        /// </summary>
        public bool FneHasWarning { get; set; }

        /// <summary>
        /// Message de warning de l'API FNE
        /// </summary>
        [MaxLength(500)]
        public string? FneWarningMessage { get; set; }

        /// <summary>
        /// NCC de l'entreprise retourné par l'API
        /// </summary>
        [MaxLength(50)]
        public string? FneCompanyNcc { get; set; }

        /// <summary>
        /// Token de vérification publique FNE (peut être différent de VerificationToken)
        /// </summary>
        [MaxLength(500)]
        public string? FnePublicVerificationToken { get; set; }

        /// <summary>
        /// Indique si la facture est certifiée FNE
        /// </summary>
        public bool IsCertified { get; set; }

        /// <summary>
        /// Messages d'erreur de certification
        /// </summary>
        public string? ErrorMessages { get; set; }

        /// <summary>
        /// Nombre de tentatives de certification
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Données brutes du QR Code pour génération/affichage
        /// </summary>
        [MaxLength(1000)]
        public string? FneQrCodeData { get; set; }

        /// <summary>
        /// Balance sticker - étiquette de validation officielle DGI
        /// </summary>
        [MaxLength(200)]
        public string? FneBalanceSticker { get; set; }

        /// <summary>
        /// Timestamp de la certification FNE côté DGI
        /// </summary>
        public DateTime? FneCertificationTimestamp { get; set; }

        /// <summary>
        /// Statut de traitement FNE (PENDING, PROCESSED, ERROR, etc.)
        /// </summary>
        [MaxLength(50)]
        public string? FneProcessingStatus { get; set; }

        /// <summary>
        /// Hash/empreinte de la facture certifiée pour vérification d'intégrité
        /// </summary>
        [MaxLength(256)]
        public string? FneCertificationHash { get; set; }

        /// <summary>
        /// Navigation - Client associé
        /// </summary>
        public virtual Client Client { get; set; } = null!;

        /// <summary>
        /// Navigation - Facture parente (pour les avoirs)
        /// </summary>
        public virtual FneInvoice? ParentInvoice { get; set; }

        /// <summary>
        /// Navigation - Factures filles (avoirs)
        /// </summary>
        public virtual ICollection<FneInvoice> ChildInvoices { get; set; } = new List<FneInvoice>();

        /// <summary>
        /// Navigation - Lignes de la facture
        /// </summary>
        public virtual ICollection<FneInvoiceItem> Items { get; set; } = new List<FneInvoiceItem>();

        /// <summary>
        /// Navigation - Session d'import
        /// </summary>
        public virtual ImportSession? ImportSession { get; set; }

        /// <summary>
        /// Navigation - Logs API associés
        /// </summary>
        public virtual ICollection<FneApiLog> ApiLogs { get; set; } = new List<FneApiLog>();

        #region Propriétés calculées pour l'affichage

        /// <summary>
        /// Retourne le nom d'affichage du client : nom réel pour clients divers, nom du client sinon
        /// Pour les clients divers (1999), extrait le nom depuis CommercialMessage
        /// </summary>
        [NotMapped]
        public string ClientDisplayName
        {
            get
            {
                // Pour les clients divers (code 1999)
                if (ClientCode == "1999" && !string.IsNullOrEmpty(CommercialMessage))
                {
                    // Format dans CommercialMessage: "Client: NOM_REEL_CLIENT"
                    if (CommercialMessage.StartsWith("Client: "))
                    {
                        var realName = CommercialMessage.Substring("Client: ".Length).Trim();
                        if (!string.IsNullOrEmpty(realName) && realName != "DIVERS CLIENTS CARBURANTS")
                        {
                            return realName; // Nom réel du client divers
                        }
                    }
                }
                
                // Pour les clients normaux ou si pas d'info dans CommercialMessage
                return Client?.Name ?? "Client inconnu";
            }
        }

        /// <summary>
        /// Indique si c'est un client divers (code 1999)
        /// </summary>
        [NotMapped]
        public bool IsClientDivers => ClientCode == "1999";

        #endregion
    }
}