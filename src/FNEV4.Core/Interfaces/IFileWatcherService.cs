using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FNEV4.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de surveillance des fichiers et dossiers.
    /// Permet de surveiller automatiquement les dossiers pour détecter les changements.
    /// </summary>
    public interface IFileWatcherService : IDisposable
    {
        #region Events

        /// <summary>
        /// Événement déclenché quand un nouveau fichier est détecté.
        /// </summary>
        event EventHandler<FileDetectedEventArgs> FileDetected;

        /// <summary>
        /// Événement déclenché quand un fichier est modifié.
        /// </summary>
        event EventHandler<FileChangedEventArgs> FileChanged;

        /// <summary>
        /// Événement déclenché quand un fichier est supprimé.
        /// </summary>
        event EventHandler<FileDeletedEventArgs> FileDeleted;

        /// <summary>
        /// Événement déclenché en cas d'erreur de surveillance.
        /// </summary>
        event EventHandler<WatcherErrorEventArgs> WatcherError;

        #endregion

        #region Watcher Management

        /// <summary>
        /// Démarre la surveillance d'un dossier.
        /// </summary>
        /// <param name="watcherId">Identifiant unique de la surveillance</param>
        /// <param name="folderPath">Chemin du dossier à surveiller</param>
        /// <param name="filter">Filtre de fichiers (ex: "*.xlsx")</param>
        /// <param name="includeSubdirectories">Inclure les sous-dossiers</param>
        /// <returns>True si la surveillance a été démarrée avec succès</returns>
        Task<bool> StartWatcherAsync(string watcherId, string folderPath, string filter = "*.*", 
            bool includeSubdirectories = false);

        /// <summary>
        /// Arrête la surveillance d'un dossier spécifique.
        /// </summary>
        /// <param name="watcherId">Identifiant de la surveillance</param>
        /// <returns>True si la surveillance a été arrêtée</returns>
        Task<bool> StopWatcherAsync(string watcherId);

        /// <summary>
        /// Arrête toutes les surveillances en cours.
        /// </summary>
        Task StopAllWatchersAsync();

        /// <summary>
        /// Redémarre une surveillance.
        /// </summary>
        /// <param name="watcherId">Identifiant de la surveillance</param>
        /// <returns>True si la surveillance a été redémarrée</returns>
        Task<bool> RestartWatcherAsync(string watcherId);

        #endregion

        #region Status and Information

        /// <summary>
        /// Vérifie si une surveillance est active.
        /// </summary>
        /// <param name="watcherId">Identifiant de la surveillance</param>
        /// <returns>True si la surveillance est active</returns>
        bool IsWatcherActive(string watcherId);

        /// <summary>
        /// Obtient la liste des surveillances actives.
        /// </summary>
        /// <returns>Liste des identifiants de surveillance actifs</returns>
        List<string> GetActiveWatchers();

        /// <summary>
        /// Obtient le nombre total de surveillances actives.
        /// </summary>
        /// <returns>Nombre de surveillances actives</returns>
        int GetActiveWatchersCount();

        /// <summary>
        /// Obtient les informations détaillées d'une surveillance.
        /// </summary>
        /// <param name="watcherId">Identifiant de la surveillance</param>
        /// <returns>Informations de surveillance ou null si non trouvée</returns>
        WatcherInfo? GetWatcherInfo(string watcherId);

        /// <summary>
        /// Obtient les statistiques de toutes les surveillances.
        /// </summary>
        /// <returns>Statistiques globales</returns>
        WatcherStatistics GetWatcherStatistics();

        #endregion

        #region Configuration

        /// <summary>
        /// Configure les options globales de surveillance.
        /// </summary>
        /// <param name="options">Options de surveillance</param>
        Task ConfigureWatcherOptionsAsync(WatcherOptions options);

        /// <summary>
        /// Met à jour le filtre d'une surveillance existante.
        /// </summary>
        /// <param name="watcherId">Identifiant de la surveillance</param>
        /// <param name="newFilter">Nouveau filtre</param>
        /// <returns>True si le filtre a été mis à jour</returns>
        Task<bool> UpdateWatcherFilterAsync(string watcherId, string newFilter);

        #endregion
    }

    #region Event Args

    /// <summary>
    /// Arguments d'événement pour un fichier détecté.
    /// </summary>
    public class FileDetectedEventArgs : EventArgs
    {
        public string WatcherId { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.Now;
        public string FolderPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Arguments d'événement pour un fichier modifié.
    /// </summary>
    public class FileChangedEventArgs : EventArgs
    {
        public string WatcherId { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.Now;
        public string ChangeType { get; set; } = string.Empty; // Created, Modified, Renamed
    }

    /// <summary>
    /// Arguments d'événement pour un fichier supprimé.
    /// </summary>
    public class FileDeletedEventArgs : EventArgs
    {
        public string WatcherId { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Arguments d'événement pour une erreur de surveillance.
    /// </summary>
    public class WatcherErrorEventArgs : EventArgs
    {
        public string WatcherId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public DateTime ErrorTime { get; set; } = DateTime.Now;
        public bool IsRecoverable { get; set; }
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Informations sur une surveillance active.
    /// </summary>
    public class WatcherInfo
    {
        public string WatcherId { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public bool IncludeSubdirectories { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartedAt { get; set; }
        public int FilesDetected { get; set; }
        public int ErrorsCount { get; set; }
        public DateTime? LastActivity { get; set; }
    }

    /// <summary>
    /// Statistiques globales des surveillances.
    /// </summary>
    public class WatcherStatistics
    {
        public int TotalWatchers { get; set; }
        public int ActiveWatchers { get; set; }
        public int TotalFilesDetected { get; set; }
        public int TotalErrors { get; set; }
        public DateTime StatisticsGeneratedAt { get; set; } = DateTime.Now;
        public Dictionary<string, int> WatcherFileCounts { get; set; } = new();
        public Dictionary<string, int> WatcherErrorCounts { get; set; } = new();
    }

    /// <summary>
    /// Options de configuration pour les surveillances.
    /// </summary>
    public class WatcherOptions
    {
        /// <summary>
        /// Délai d'attente avant de déclencher un événement de fichier (en millisecondes).
        /// Évite les événements multiples pour le même fichier.
        /// </summary>
        public int FileEventDelay { get; set; } = 500;

        /// <summary>
        /// Taille maximale de fichier à surveiller (en bytes).
        /// Les fichiers plus gros seront ignorés.
        /// </summary>
        public long MaxFileSize { get; set; } = 100 * 1024 * 1024; // 100 MB

        /// <summary>
        /// Nombre maximum de fichiers à traiter simultanément.
        /// </summary>
        public int MaxConcurrentFiles { get; set; } = 5;

        /// <summary>
        /// Extensions de fichiers à ignorer.
        /// </summary>
        public List<string> IgnoredExtensions { get; set; } = new() { ".tmp", ".temp", ".log" };

        /// <summary>
        /// Activer le logging détaillé des surveillances.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// Redémarrage automatique en cas d'erreur.
        /// </summary>
        public bool AutoRestartOnError { get; set; } = true;

        /// <summary>
        /// Délai avant redémarrage automatique (en secondes).
        /// </summary>
        public int AutoRestartDelay { get; set; } = 30;
    }

    #endregion
}
