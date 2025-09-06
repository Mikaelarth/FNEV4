using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FNEV4.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de logging avancé.
    /// Gère les logs applicatifs avec rotation et nettoyage automatique.
    /// </summary>
    public interface ILoggingService
    {
        #region Logging Operations

        /// <summary>
        /// Écrit un log au niveau Information.
        /// </summary>
        /// <param name="message">Message du log</param>
        /// <param name="category">Catégorie du log</param>
        Task LogInformationAsync(string message, string category = "General");

        /// <summary>
        /// Écrit un log au niveau Warning.
        /// </summary>
        /// <param name="message">Message du log</param>
        /// <param name="category">Catégorie du log</param>
        Task LogWarningAsync(string message, string category = "General");

        /// <summary>
        /// Écrit un log au niveau Error.
        /// </summary>
        /// <param name="message">Message d'erreur</param>
        /// <param name="exception">Exception associée</param>
        /// <param name="category">Catégorie du log</param>
        Task LogErrorAsync(string message, Exception? exception = null, string category = "General");

        /// <summary>
        /// Écrit un log au niveau Debug.
        /// </summary>
        /// <param name="message">Message de debug</param>
        /// <param name="category">Catégorie du log</param>
        Task LogDebugAsync(string message, string category = "General");

        /// <summary>
        /// Écrit un log au niveau Critical.
        /// </summary>
        /// <param name="message">Message critique</param>
        /// <param name="exception">Exception associée</param>
        /// <param name="category">Catégorie du log</param>
        Task LogCriticalAsync(string message, Exception? exception = null, string category = "General");

        #endregion

        #region Log Management

        /// <summary>
        /// Obtient le chemin du fichier de log actuel.
        /// </summary>
        /// <param name="logsFolderPath">Dossier des logs</param>
        /// <returns>Chemin du fichier de log actuel</returns>
        string GetLatestLogFile(string logsFolderPath);

        /// <summary>
        /// Nettoie les anciens fichiers de log.
        /// </summary>
        /// <param name="logsFolderPath">Dossier des logs</param>
        /// <param name="retentionDays">Nombre de jours de rétention</param>
        /// <returns>Taille libérée en bytes</returns>
        Task<long> CleanOldLogsAsync(string logsFolderPath, int retentionDays);

        /// <summary>
        /// Archive les logs anciens.
        /// </summary>
        /// <param name="logsFolderPath">Dossier des logs</param>
        /// <param name="archiveFolderPath">Dossier d'archivage</param>
        /// <returns>Nombre de fichiers archivés</returns>
        Task<int> ArchiveOldLogsAsync(string logsFolderPath, string archiveFolderPath);

        /// <summary>
        /// Obtient les statistiques des logs.
        /// </summary>
        /// <param name="logsFolderPath">Dossier des logs</param>
        /// <returns>Statistiques des logs</returns>
        Task<LogStatistics> GetLogStatisticsAsync(string logsFolderPath);

        #endregion

        #region Configuration

        /// <summary>
        /// Configure le niveau de log minimum.
        /// </summary>
        /// <param name="logLevel">Niveau de log</param>
        Task SetLogLevelAsync(LogLevel logLevel);

        /// <summary>
        /// Configure la rotation automatique des logs.
        /// </summary>
        /// <param name="enabled">Activer la rotation</param>
        /// <param name="maxFileSize">Taille maximale d'un fichier de log</param>
        /// <param name="maxFiles">Nombre maximum de fichiers</param>
        Task ConfigureLogRotationAsync(bool enabled, long maxFileSize = 10485760, int maxFiles = 10);

        /// <summary>
        /// Configure le formatage des logs.
        /// </summary>
        /// <param name="includeTimestamp">Inclure le timestamp</param>
        /// <param name="includeLevel">Inclure le niveau</param>
        /// <param name="includeCategory">Inclure la catégorie</param>
        Task ConfigureLogFormatAsync(bool includeTimestamp = true, bool includeLevel = true, bool includeCategory = true);

        #endregion
    }

    #region Enums and Data Classes

    /// <summary>
    /// Niveaux de log.
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Information = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    /// <summary>
    /// Statistiques des logs.
    /// </summary>
    public class LogStatistics
    {
        public int TotalLogFiles { get; set; }
        public long TotalSize { get; set; }
        public DateTime? OldestLog { get; set; }
        public DateTime? NewestLog { get; set; }
        public Dictionary<string, int> LogsByLevel { get; set; } = new();
        public Dictionary<string, int> LogsByCategory { get; set; } = new();
        public Dictionary<DateTime, int> LogsByDate { get; set; } = new();
    }

    #endregion
}
