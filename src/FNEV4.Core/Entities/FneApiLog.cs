using System.ComponentModel.DataAnnotations;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Journalisation complète des échanges avec l'API FNE
    /// Traçabilité conformément aux spécifications DGI
    /// </summary>
    public class FneApiLog : BaseEntity
    {
        /// <summary>
        /// Identifiant de la facture concernée (si applicable)
        /// </summary>
        public Guid? FneInvoiceId { get; set; }

        /// <summary>
        /// Type d'opération API (Certification, Avoir, Bordereau, Test)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string OperationType { get; set; } = string.Empty;

        /// <summary>
        /// Endpoint API appelé
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Méthode HTTP (POST, GET, PUT, DELETE)
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string HttpMethod { get; set; } = "POST";

        /// <summary>
        /// Corps de la requête envoyée (JSON)
        /// </summary>
        public string? RequestBody { get; set; }

        /// <summary>
        /// En-têtes de la requête (JSON)
        /// </summary>
        public string? RequestHeaders { get; set; }

        /// <summary>
        /// Code de statut HTTP de la réponse
        /// </summary>
        public int ResponseStatusCode { get; set; }

        /// <summary>
        /// Corps de la réponse reçue (JSON)
        /// </summary>
        public string? ResponseBody { get; set; }

        /// <summary>
        /// En-têtes de la réponse (JSON)
        /// </summary>
        public string? ResponseHeaders { get; set; }

        /// <summary>
        /// Temps de traitement en millisecondes
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Indique si l'appel API a réussi
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Message d'erreur détaillé (si échec)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Pile d'erreur complète (pour debugging)
        /// </summary>
        public string? ErrorStackTrace { get; set; }

        /// <summary>
        /// Numéro de tentative (pour retry automatique)
        /// </summary>
        public int AttemptNumber { get; set; } = 1;

        /// <summary>
        /// Type d'erreur (Network, Authentication, Validation, Server, etc.)
        /// </summary>
        [MaxLength(50)]
        public string? ErrorType { get; set; }

        /// <summary>
        /// Référence FNE retournée (si succès)
        /// </summary>
        [MaxLength(50)]
        public string? FneReference { get; set; }

        /// <summary>
        /// Token de vérification retourné (si succès)
        /// </summary>
        [MaxLength(500)]
        public string? VerificationToken { get; set; }

        /// <summary>
        /// Solde de stickers après l'opération
        /// </summary>
        public int? StickerBalance { get; set; }

        /// <summary>
        /// Environnement utilisé (Test/Production)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Environment { get; set; } = "Test";

        /// <summary>
        /// Adresse IP du serveur contacté
        /// </summary>
        [MaxLength(50)]
        public string? ServerIpAddress { get; set; }

        /// <summary>
        /// Nom d'utilisateur associé à l'opération
        /// </summary>
        [MaxLength(100)]
        public string? UserName { get; set; }

        /// <summary>
        /// Session ou contexte d'exécution
        /// </summary>
        [MaxLength(100)]
        public string? SessionId { get; set; }

        /// <summary>
        /// Niveau de log (Info, Warning, Error, Critical)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string LogLevel { get; set; } = "Info";

        /// <summary>
        /// Date et heure de l'appel API
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation - Facture associée
        /// </summary>
        public virtual FneInvoice? FneInvoice { get; set; }
    }
}
