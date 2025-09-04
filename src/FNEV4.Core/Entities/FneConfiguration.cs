using System.ComponentModel.DataAnnotations;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Configuration API FNE selon FNE-procedureapi.md
    /// Gestion des environnements Test/Production et validation DGI
    /// </summary>
    public class FneConfiguration : BaseEntity
    {
        /// <summary>
        /// Nom de la configuration (ex: "Test", "Production", "Custom")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ConfigurationName { get; set; } = string.Empty;

        /// <summary>
        /// Environnement FNE (Test, Production)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Environment { get; set; } = "Test";

        /// <summary>
        /// URL de base API FNE
        /// Test: http://54.247.95.108/ws
        /// Production: URL transmise par DGI après validation
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string BaseUrl { get; set; } = "http://54.247.95.108/ws";

        /// <summary>
        /// URL de l'interface web FNE (pour inscription)
        /// Test: http://54.247.95.108
        /// </summary>
        [MaxLength(200)]
        public string? WebUrl { get; set; }

        /// <summary>
        /// Clé API fournie par la DGI (chiffrée)
        /// Visible uniquement après validation de l'intégration
        /// </summary>
        [MaxLength(500)]
        public string? ApiKey { get; set; }

        /// <summary>
        /// Token d'authentification Bearer (si différent de ApiKey)
        /// </summary>
        [MaxLength(1000)]
        public string? BearerToken { get; set; }

        /// <summary>
        /// Indique si cette configuration est active
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// Indique si l'intégration a été validée par la DGI
        /// </summary>
        public bool IsValidatedByDgi { get; set; } = false;

        /// <summary>
        /// Date de validation de l'intégration par la DGI
        /// </summary>
        public DateTime? ValidationDate { get; set; }

        /// <summary>
        /// E-mail de contact DGI pour cette configuration
        /// support.fne@dgi.gouv.ci
        /// </summary>
        [MaxLength(100)]
        public string? SupportEmail { get; set; } = "support.fne@dgi.gouv.ci";

        /// <summary>
        /// Timeout pour les requêtes API (en secondes)
        /// </summary>
        public int RequestTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Nombre maximum de tentatives en cas d'échec
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Délai entre les tentatives (en secondes)
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 5;

        /// <summary>
        /// Endpoints API configurés (JSON)
        /// Format: {"sign": "/external/invoices/sign", "refund": "/external/invoices/{id}/refund"}
        /// </summary>
        public string? ApiEndpoints { get; set; }

        /// <summary>
        /// Version de l'API FNE supportée
        /// </summary>
        [MaxLength(10)]
        public string? ApiVersion { get; set; } = "1.0";

        /// <summary>
        /// Certificats SSL (si requis pour l'environnement)
        /// </summary>
        public string? SslCertificates { get; set; }

        /// <summary>
        /// Dernière date de test de connectivité
        /// </summary>
        public DateTime? LastConnectivityTest { get; set; }

        /// <summary>
        /// Résultat du dernier test de connectivité
        /// </summary>
        public bool? LastConnectivityResult { get; set; }

        /// <summary>
        /// Messages d'erreur du dernier test
        /// </summary>
        public string? LastTestErrorMessages { get; set; }

        /// <summary>
        /// Spécimens de factures transmis à la DGI (JSON)
        /// Historique des validations
        /// </summary>
        public string? SubmittedSpecimens { get; set; }

        /// <summary>
        /// Notes de configuration et remarques
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date de création de la configuration
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }

        /// <summary>
        /// Utilisateur ayant créé la configuration
        /// </summary>
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Utilisateur ayant modifié la configuration
        /// </summary>
        [MaxLength(100)]
        public string? ModifiedBy { get; set; }
    }
}
