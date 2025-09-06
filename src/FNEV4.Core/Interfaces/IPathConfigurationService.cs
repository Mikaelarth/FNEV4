using System;

namespace FNEV4.Core.Interfaces
{
    /// <summary>
    /// Service centralisé pour la gestion des chemins et dossiers de l'application
    /// </summary>
    public interface IPathConfigurationService
    {
        /// <summary>
        /// Dossier racine de données de l'application
        /// </summary>
        string DataRootPath { get; }

        /// <summary>
        /// Dossier d'import des fichiers Excel Sage
        /// </summary>
        string ImportFolderPath { get; }

        /// <summary>
        /// Dossier d'export des factures certifiées
        /// </summary>
        string ExportFolderPath { get; }

        /// <summary>
        /// Dossier d'archivage des factures traitées
        /// </summary>
        string ArchiveFolderPath { get; }

        /// <summary>
        /// Dossier des logs de l'application
        /// </summary>
        string LogsFolderPath { get; }

        /// <summary>
        /// Dossier des sauvegardes
        /// </summary>
        string BackupFolderPath { get; }

        /// <summary>
        /// Chemin complet vers la base de données
        /// </summary>
        string DatabasePath { get; }

        /// <summary>
        /// Chemin vers le fichier de configuration de la base de données
        /// </summary>
        string DatabaseConfigPath { get; }

        /// <summary>
        /// Initialise tous les dossiers nécessaires
        /// </summary>
        void EnsureDirectoriesExist();

        /// <summary>
        /// Met à jour la configuration des chemins
        /// </summary>
        /// <param name="importPath">Nouveau chemin d'import</param>
        /// <param name="exportPath">Nouveau chemin d'export</param>
        /// <param name="archivePath">Nouveau chemin d'archive</param>
        /// <param name="logsPath">Nouveau chemin des logs</param>
        /// <param name="backupPath">Nouveau chemin de backup</param>
        void UpdatePaths(string importPath, string exportPath, string archivePath, string logsPath, string backupPath);

        /// <summary>
        /// Valide qu'un chemin existe et est accessible
        /// </summary>
        /// <param name="path">Chemin à valider</param>
        /// <returns>True si le chemin est valide</returns>
        bool ValidatePath(string path);

        /// <summary>
        /// Calcule la taille d'un dossier
        /// </summary>
        /// <param name="folderPath">Chemin du dossier</param>
        /// <returns>Taille en octets</returns>
        long CalculateDirectorySize(string folderPath);
    }
}
