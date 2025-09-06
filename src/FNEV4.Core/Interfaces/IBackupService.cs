using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FNEV4.Core.Interfaces
{
    /// <summary>
    /// Interface pour le service de sauvegarde de la base de données.
    /// Gère la création, restauration et maintenance des sauvegardes.
    /// </summary>
    public interface IBackupService
    {
        #region Backup Operations

        /// <summary>
        /// Crée une sauvegarde de la base de données.
        /// </summary>
        /// <param name="backupFolderPath">Dossier de destination de la sauvegarde</param>
        /// <param name="backupName">Nom personnalisé de la sauvegarde (optionnel)</param>
        /// <returns>Chemin complet du fichier de sauvegarde créé</returns>
        Task<string> CreateBackupAsync(string backupFolderPath, string? backupName = null);

        /// <summary>
        /// Crée une sauvegarde automatique selon la configuration.
        /// </summary>
        /// <param name="configuration">Configuration de sauvegarde</param>
        /// <returns>Chemin du fichier créé ou null si pas nécessaire</returns>
        Task<string?> CreateAutoBackupAsync(BackupConfiguration configuration);

        /// <summary>
        /// Restaure la base de données depuis une sauvegarde.
        /// </summary>
        /// <param name="backupFilePath">Chemin du fichier de sauvegarde</param>
        /// <param name="overwriteExisting">Remplacer la base existante</param>
        /// <returns>True si la restauration a réussi</returns>
        Task<bool> RestoreBackupAsync(string backupFilePath, bool overwriteExisting = false);

        /// <summary>
        /// Valide l'intégrité d'un fichier de sauvegarde.
        /// </summary>
        /// <param name="backupFilePath">Chemin du fichier de sauvegarde</param>
        /// <returns>Résultat de la validation</returns>
        Task<BackupValidationResult> ValidateBackupAsync(string backupFilePath);

        #endregion

        #region Backup Management

        /// <summary>
        /// Obtient la liste des sauvegardes disponibles dans un dossier.
        /// </summary>
        /// <param name="backupFolderPath">Dossier des sauvegardes</param>
        /// <returns>Liste des informations de sauvegarde</returns>
        Task<List<BackupInfo>> GetAvailableBackupsAsync(string backupFolderPath);

        /// <summary>
        /// Supprime les anciennes sauvegardes selon la politique de rétention.
        /// </summary>
        /// <param name="backupFolderPath">Dossier des sauvegardes</param>
        /// <param name="retentionPolicy">Politique de rétention</param>
        /// <returns>Nombre de sauvegardes supprimées</returns>
        Task<int> CleanupOldBackupsAsync(string backupFolderPath, BackupRetentionPolicy retentionPolicy);

        /// <summary>
        /// Supprime une sauvegarde spécifique.
        /// </summary>
        /// <param name="backupFilePath">Chemin du fichier de sauvegarde</param>
        /// <returns>True si la suppression a réussi</returns>
        Task<bool> DeleteBackupAsync(string backupFilePath);

        /// <summary>
        /// Compresse une sauvegarde pour économiser l'espace.
        /// </summary>
        /// <param name="backupFilePath">Chemin du fichier de sauvegarde</param>
        /// <returns>Chemin du fichier compressé</returns>
        Task<string> CompressBackupAsync(string backupFilePath);

        #endregion

        #region Scheduling

        /// <summary>
        /// Configure la sauvegarde automatique.
        /// </summary>
        /// <param name="configuration">Configuration de sauvegarde automatique</param>
        /// <returns>True si la configuration a réussi</returns>
        Task<bool> ConfigureAutoBackupAsync(BackupConfiguration configuration);

        /// <summary>
        /// Démarre le service de sauvegarde automatique.
        /// </summary>
        /// <returns>True si le service a démarré</returns>
        Task<bool> StartAutoBackupServiceAsync();

        /// <summary>
        /// Arrête le service de sauvegarde automatique.
        /// </summary>
        /// <returns>True si le service a été arrêté</returns>
        Task<bool> StopAutoBackupServiceAsync();

        /// <summary>
        /// Vérifie si la sauvegarde automatique est active.
        /// </summary>
        /// <returns>True si la sauvegarde automatique est active</returns>
        bool IsAutoBackupActive();

        #endregion

        #region Information and Statistics

        /// <summary>
        /// Obtient les informations de la dernière sauvegarde.
        /// </summary>
        /// <param name="backupFolderPath">Dossier des sauvegardes</param>
        /// <returns>Informations de la dernière sauvegarde ou null</returns>
        Task<BackupInfo?> GetLastBackupInfoAsync(string backupFolderPath);

        /// <summary>
        /// Calcule l'espace disque utilisé par les sauvegardes.
        /// </summary>
        /// <param name="backupFolderPath">Dossier des sauvegardes</param>
        /// <returns>Taille totale en bytes</returns>
        Task<long> GetBackupsSpaceUsageAsync(string backupFolderPath);

        /// <summary>
        /// Obtient les statistiques des sauvegardes.
        /// </summary>
        /// <param name="backupFolderPath">Dossier des sauvegardes</param>
        /// <returns>Statistiques détaillées</returns>
        Task<BackupStatistics> GetBackupStatisticsAsync(string backupFolderPath);

        /// <summary>
        /// Estime la taille d'une future sauvegarde.
        /// </summary>
        /// <returns>Taille estimée en bytes</returns>
        Task<long> EstimateBackupSizeAsync();

        #endregion

        #region Import/Export

        /// <summary>
        /// Exporte une sauvegarde vers un emplacement externe.
        /// </summary>
        /// <param name="backupFilePath">Fichier de sauvegarde source</param>
        /// <param name="destinationPath">Destination d'export</param>
        /// <param name="includeMetadata">Inclure les métadonnées</param>
        /// <returns>Chemin du fichier exporté</returns>
        Task<string> ExportBackupAsync(string backupFilePath, string destinationPath, bool includeMetadata = true);

        /// <summary>
        /// Importe une sauvegarde depuis un emplacement externe.
        /// </summary>
        /// <param name="sourceFilePath">Fichier source</param>
        /// <param name="backupFolderPath">Dossier de destination</param>
        /// <returns>Chemin du fichier importé</returns>
        Task<string> ImportBackupAsync(string sourceFilePath, string backupFolderPath);

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Configuration pour les sauvegardes automatiques.
    /// </summary>
    public class BackupConfiguration
    {
        public string BackupFolderPath { get; set; } = string.Empty;
        public BackupFrequency Frequency { get; set; } = BackupFrequency.Daily;
        public TimeSpan BackupTime { get; set; } = TimeSpan.FromHours(2); // 2h du matin
        public bool EnableCompression { get; set; } = true;
        public BackupRetentionPolicy RetentionPolicy { get; set; } = new();
        public bool EnableAutoCleanup { get; set; } = true;
        public int MaxConcurrentBackups { get; set; } = 1;
        public bool NotifyOnSuccess { get; set; } = false;
        public bool NotifyOnError { get; set; } = true;
    }

    /// <summary>
    /// Fréquence des sauvegardes automatiques.
    /// </summary>
    public enum BackupFrequency
    {
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Manual
    }

    /// <summary>
    /// Politique de rétention des sauvegardes.
    /// </summary>
    public class BackupRetentionPolicy
    {
        public int KeepHourlyBackups { get; set; } = 24; // 24 heures
        public int KeepDailyBackups { get; set; } = 30; // 30 jours
        public int KeepWeeklyBackups { get; set; } = 12; // 12 semaines
        public int KeepMonthlyBackups { get; set; } = 12; // 12 mois
        public long MaxTotalSize { get; set; } = 5L * 1024 * 1024 * 1024; // 5 GB
    }

    /// <summary>
    /// Informations sur une sauvegarde.
    /// </summary>
    public class BackupInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public long FileSize { get; set; }
        public bool IsCompressed { get; set; }
        public string BackupType { get; set; } = string.Empty; // Manual, Auto
        public bool IsValid { get; set; } = true;
        public string? Description { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Résultat de validation d'une sauvegarde.
    /// </summary>
    public class BackupValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public long ExpectedSize { get; set; }
        public long ActualSize { get; set; }
        public DateTime ValidatedAt { get; set; } = DateTime.Now;
        public string ValidationMethod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Statistiques des sauvegardes.
    /// </summary>
    public class BackupStatistics
    {
        public int TotalBackups { get; set; }
        public int ValidBackups { get; set; }
        public int CompressedBackups { get; set; }
        public long TotalSize { get; set; }
        public DateTime? OldestBackup { get; set; }
        public DateTime? NewestBackup { get; set; }
        public BackupInfo? LargestBackup { get; set; }
        public BackupInfo? SmallestBackup { get; set; }
        public double AverageSize { get; set; }
        public Dictionary<string, int> BackupsByType { get; set; } = new();
        public Dictionary<DateTime, int> BackupsByDate { get; set; } = new();
    }

    #endregion
}
