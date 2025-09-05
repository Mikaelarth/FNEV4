using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNEV4.Core.Entities;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Interface pour le service de logging professionnel
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Récupère les logs avec filtrage optionnel
        /// </summary>
        Task<IEnumerable<LogEntry>> GetLogsAsync(LogLevel? minLevel = null, DateTime? since = null, int maxCount = 1000);

        /// <summary>
        /// Enregistre un log avec niveau spécifique
        /// </summary>
        Task LogAsync(LogLevel level, string message, string category = "Application", Exception? exception = null);

        /// <summary>
        /// Enregistre un log de débogage
        /// </summary>
        Task LogDebugAsync(string message, string category = "Debug");

        /// <summary>
        /// Enregistre un log d'information
        /// </summary>
        Task LogInfoAsync(string message, string category = "Info");

        /// <summary>
        /// Enregistre un log d'avertissement
        /// </summary>
        Task LogWarningAsync(string message, string category = "Warning");

        /// <summary>
        /// Enregistre un log d'erreur
        /// </summary>
        Task LogErrorAsync(string message, string category = "Error", Exception? exception = null);

        /// <summary>
        /// Supprime les logs anciens
        /// </summary>
        Task ClearOldLogsAsync(DateTime olderThan);

        /// <summary>
        /// Exporte les logs vers un fichier
        /// </summary>
        Task<bool> ExportLogsAsync(string filePath, LogLevel? minLevel = null, DateTime? since = null);

        /// <summary>
        /// Obtient les statistiques des logs
        /// </summary>
        Task<LogStatistics> GetLogStatisticsAsync();

        /// <summary>
        /// Événement déclenché lors de l'ajout d'un nouveau log
        /// </summary>
        event EventHandler<LogEntry> LogAdded;
    }

    /// <summary>
    /// Statistiques des logs
    /// </summary>
    public class LogStatistics
    {
        public int TotalEntries { get; set; }
        public int DebugCount { get; set; }
        public int InfoCount { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public int CriticalCount { get; set; }
        public DateTime? LastEntryTime { get; set; }
        public DateTime? OldestEntryTime { get; set; }
        public double AverageEntriesPerHour { get; set; }
    }
}
