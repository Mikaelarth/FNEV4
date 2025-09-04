using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Entité PointDeVente - Points de vente de l'entreprise
    /// Liée à Company (pas Entreprise pour éviter redondance)
    /// </summary>
    public class PointDeVente : BaseEntity
    {
        [Required]
        public int CompanyId { get; set; } // Référence vers Company

        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Adresse { get; set; }

        [MaxLength(20)]
        public string? Telephone { get; set; }

        public bool EstActif { get; set; } = true;

        // === RELATIONS ===
        
        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;
    }

    /// <summary>
    /// Entité TypeTVA - Types de TVA supportés par l'API FNE
    /// </summary>
    public class TypeTVA : BaseEntity
    {
        [Required]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty; // TVA, TVAB, TVAC, TVAD

        [Required]
        [MaxLength(100)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(5,4)")]
        public decimal Taux { get; set; } // 0.18, 0.09, 0.00

        [MaxLength(200)]
        public string? Description { get; set; }

        public bool EstActif { get; set; } = true;

        // === DONNÉES PRÉDÉFINIES SELON API FNE ===
        
        public static readonly List<TypeTVA> DefaultTypes = new()
        {
            new TypeTVA { Code = "TVA", Libelle = "TVA normale", Taux = 0.18m, Description = "TVA normale de 18%" },
            new TypeTVA { Code = "TVAB", Libelle = "TVA réduite", Taux = 0.09m, Description = "TVA réduite de 9%" },
            new TypeTVA { Code = "TVAC", Libelle = "TVA exonérée convention", Taux = 0.00m, Description = "TVA exonérée convention 0%" },
            new TypeTVA { Code = "TVAD", Libelle = "TVA exonérée légale", Taux = 0.00m, Description = "TVA exonérée légale 0%" }
        };
    }

    /// <summary>
    /// Entité SessionImport - Historique des imports Excel
    /// </summary>
    public class SessionImport : BaseEntity
    {
        [Required]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(500)]
        public string NomFichier { get; set; } = string.Empty;

        [Required]
        public DateTime DateImport { get; set; } = DateTime.UtcNow;

        public int NombreFactures { get; set; } = 0;
        public int NombreFacturesTraitees { get; set; } = 0;
        public int NombreErreurs { get; set; } = 0;

        [MaxLength(20)]
        public string Statut { get; set; } = "EnCours"; // EnCours, Termine, Erreur

        [Column(TypeName = "text")]
        public string? LogImport { get; set; }

        // === RELATIONS ===
        
        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;

        public virtual ICollection<FneInvoice> Factures { get; set; } = new List<FneInvoice>();
    }

    /// <summary>
    /// Entité LogApplication - Logs système
    /// </summary>
    public class LogApplication : BaseEntity
    {
        [Required]
        [MaxLength(20)]
        public string Niveau { get; set; } = string.Empty; // Debug, Info, Warning, Error

        [Required]
        [MaxLength(100)]
        public string Source { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Details { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? Utilisateur { get; set; }

        public bool EstTraite { get; set; } = false;
    }

    /// <summary>
    /// Entité Utilisateur - Utilisateurs système
    /// </summary>
    public class Utilisateur : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string MotDePasseHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Role { get; set; } = "Utilisateur"; // Admin, Utilisateur

        public bool EstActif { get; set; } = true;

        public DateTime? DerniereConnexion { get; set; }

        // === RELATIONS ===
        
        public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    }

    /// <summary>
    /// Entité Session - Sessions utilisateur
    /// </summary>
    public class Session : BaseEntity
    {
        [Required]
        public int UtilisateurId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public new DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public DateTime? DateExpiration { get; set; }

        public bool EstActive { get; set; } = true;

        [MaxLength(200)]
        public string? AdresseIP { get; set; }

        // === RELATIONS ===
        
        [ForeignKey("UtilisateurId")]
        public virtual Utilisateur Utilisateur { get; set; } = null!;
    }

    /// <summary>
    /// Entité ParametreSysteme - Paramètres de configuration
    /// </summary>
    public class ParametreSysteme : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Cle { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Valeur { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string Type { get; set; } = "String"; // String, Integer, Boolean, Decimal

        [MaxLength(50)]
        public string Categorie { get; set; } = "General";

        public bool EstModifiable { get; set; } = true;
    }
}
