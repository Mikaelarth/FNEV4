using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FNEV4.Core.Entities;

namespace FNEV4.Core.Interfaces
{
    /// <summary>
    /// Interface pour la gestion de la configuration des dossiers et chemins.
    /// Fournit des services pour valider, sauvegarder et gérer les chemins de fichiers.
    /// </summary>
    public interface IFolderConfigurationService
    {
        #region Configuration Management

        /// <summary>
        /// Charge la configuration des dossiers depuis la base de données.
        /// </summary>
        /// <returns>Configuration des dossiers ou null si non trouvée</returns>
        Task<FolderConfiguration?> LoadConfigurationAsync();

        /// <summary>
        /// Sauvegarde la configuration des dossiers dans la base de données.
        /// </summary>
        /// <param name="configuration">Configuration à sauvegarder</param>
        Task SaveConfigurationAsync(FolderConfiguration configuration);

        /// <summary>
        /// Exporte la configuration vers un fichier JSON.
        /// </summary>
        /// <param name="configuration">Configuration à exporter</param>
        /// <param name="filePath">Chemin du fichier de destination</param>
        Task ExportConfigurationAsync(FolderConfiguration configuration, string filePath);

        /// <summary>
        /// Importe une configuration depuis un fichier JSON.
        /// </summary>
        /// <param name="filePath">Chemin du fichier source</param>
        /// <returns>Configuration importée</returns>
        Task<FolderConfiguration> ImportConfigurationAsync(string filePath);

        #endregion

        #region Folder Validation

        /// <summary>
        /// Teste un dossier et retourne des informations détaillées.
        /// </summary>
        /// <param name="folderPath">Chemin du dossier à tester</param>
        /// <returns>Résultat du test</returns>
        Task<FolderTestResult> TestFolderAsync(string folderPath);

        /// <summary>
        /// Vérifie si l'application a les permissions d'écriture sur un dossier.
        /// </summary>
        /// <param name="folderPath">Chemin du dossier</param>
        /// <returns>True si les permissions d'écriture sont accordées</returns>
        Task<bool> HasWritePermissionAsync(string folderPath);

        /// <summary>
        /// Vérifie si un dossier existe et est accessible.
        /// </summary>
        /// <param name="folderPath">Chemin du dossier</param>
        /// <returns>True si le dossier existe et est accessible</returns>
        Task<bool> IsFolderAccessibleAsync(string folderPath);

        /// <summary>
        /// Calcule l'espace disque utilisé par un dossier.
        /// </summary>
        /// <param name="folderPath">Chemin du dossier</param>
        /// <returns>Taille en bytes</returns>
        Task<long> CalculateFolderSizeAsync(string folderPath);

        #endregion

        #region Folder Operations

        /// <summary>
        /// Crée un dossier s'il n'existe pas.
        /// </summary>
        /// <param name="folderPath">Chemin du dossier à créer</param>
        /// <returns>True si le dossier a été créé ou existe déjà</returns>
        Task<bool> CreateFolderIfNotExistsAsync(string folderPath);

        /// <summary>
        /// Nettoie un dossier d'archivage selon la politique de rétention.
        /// </summary>
        /// <param name="archiveFolderPath">Chemin du dossier d'archivage</param>
        /// <param name="retentionPeriod">Période de rétention (ex: "30 jours")</param>
        /// <returns>Taille libérée en bytes</returns>
        Task<long> CleanArchiveFolderAsync(string archiveFolderPath, string retentionPeriod);

        /// <summary>
        /// Organise les fichiers d'export selon le modèle configuré.
        /// </summary>
        /// <param name="exportFolderPath">Chemin du dossier d'export</param>
        /// <param name="organizationPattern">Modèle d'organisation</param>
        /// <returns>Nombre de fichiers organisés</returns>
        Task<int> OrganizeExportFolderAsync(string exportFolderPath, string organizationPattern);

        #endregion

        #region File Operations

        /// <summary>
        /// Génère un nom de fichier unique basé sur un modèle.
        /// </summary>
        /// <param name="namingPattern">Modèle de nommage</param>
        /// <param name="invoiceNumber">Numéro de facture</param>
        /// <param name="clientName">Nom du client</param>
        /// <param name="date">Date de la facture</param>
        /// <returns>Nom de fichier généré</returns>
        string GenerateFileName(string namingPattern, string invoiceNumber, string clientName, DateTime date);

        /// <summary>
        /// Déplace un fichier vers le dossier d'archivage avec organisation automatique.
        /// </summary>
        /// <param name="sourceFilePath">Fichier source</param>
        /// <param name="archiveConfiguration">Configuration d'archivage</param>
        /// <param name="clientName">Nom du client</param>
        /// <param name="date">Date du fichier</param>
        /// <returns>Chemin de destination</returns>
        Task<string> MoveToArchiveAsync(string sourceFilePath, FolderConfiguration archiveConfiguration, 
            string clientName, DateTime date);

        #endregion

        #region Statistics

        /// <summary>
        /// Obtient les statistiques d'utilisation des dossiers.
        /// </summary>
        /// <param name="configuration">Configuration des dossiers</param>
        /// <returns>Statistiques détaillées</returns>
        Task<FolderUsageStatistics> GetUsageStatisticsAsync(FolderConfiguration configuration);

        /// <summary>
        /// Obtient la liste des fichiers récents dans un dossier.
        /// </summary>
        /// <param name="folderPath">Chemin du dossier</param>
        /// <param name="maxCount">Nombre maximum de fichiers</param>
        /// <param name="extension">Extension de fichier à filtrer (optionnel)</param>
        /// <returns>Liste des fichiers récents</returns>
        Task<List<FileInfo>> GetRecentFilesAsync(string folderPath, int maxCount = 10, string? extension = null);

        #endregion
    }

    /// <summary>
    /// Résultat d'un test de dossier.
    /// </summary>
    public class FolderTestResult
    {
        public bool IsValid { get; set; }
        public bool HasWarnings { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Details { get; set; } = new();
        public long AvailableSpace { get; set; }
        public bool HasWritePermission { get; set; }
        public bool HasReadPermission { get; set; }
        public int FileCount { get; set; }
        public long TotalSize { get; set; }
    }

    /// <summary>
    /// Statistiques d'utilisation des dossiers.
    /// </summary>
    public class FolderUsageStatistics
    {
        public int TotalFolders { get; set; }
        public int ValidFolders { get; set; }
        public long TotalSize { get; set; }
        public int TotalFiles { get; set; }
        public Dictionary<string, long> FolderSizes { get; set; } = new();
        public Dictionary<string, int> FileCounts { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
