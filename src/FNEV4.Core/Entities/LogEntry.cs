using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Entité pour les logs système de l'application
    /// Stockage des messages de log avec métadonnées complètes
    /// </summary>
    [Table("LogEntries")]
    public class LogEntry : BaseEntity
    {
        /// <summary>
        /// Identifiant numérique séquentiel pour performance
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public new long Id { get; set; }

        /// <summary>
        /// Horodatage précis du log
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Niveau de gravité du log
        /// </summary>
        [Required]
        public LogLevel Level { get; set; }

        /// <summary>
        /// Catégorie/Module source du log
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Message principal du log
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Détails de l'exception (si applicable)
        /// </summary>
        [MaxLength(4000)]
        public string? ExceptionDetails { get; set; }

        /// <summary>
        /// Nom de la machine source
        /// </summary>
        [MaxLength(100)]
        public string MachineName { get; set; } = Environment.MachineName;

        /// <summary>
        /// Nom de l'utilisateur
        /// </summary>
        [MaxLength(100)]
        public string UserName { get; set; } = Environment.UserName;

        /// <summary>
        /// ID du processus
        /// </summary>
        public int ProcessId { get; set; } = Environment.ProcessId;

        /// <summary>
        /// ID du thread
        /// </summary>
        [MaxLength(50)]
        public string ThreadId { get; set; } = Thread.CurrentThread.ManagedThreadId.ToString();

        /// <summary>
        /// Constructeur par défaut
        /// </summary>
        public LogEntry()
        {
            // Override BaseEntity.Id with long type
            base.Id = Guid.NewGuid();
        }
    }

    /// <summary>
    /// Énumération des niveaux de log (identique à Microsoft.Extensions.Logging.LogLevel)
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Informations de débogage détaillées
        /// </summary>
        Debug = 0,

        /// <summary>
        /// Informations générales
        /// </summary>
        Info = 1,

        /// <summary>
        /// Avertissements non bloquants
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Erreurs récupérables
        /// </summary>
        Error = 3,

        /// <summary>
        /// Erreurs critiques nécessitant une intervention
        /// </summary>
        Critical = 4
    }
}
