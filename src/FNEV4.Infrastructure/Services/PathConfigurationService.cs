using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using FNEV4.Core.Interfaces;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Service centralisé pour la gestion des chemins et dossiers de l'application
    /// </summary>
    public class PathConfigurationService : IPathConfigurationService
    {
        private readonly IConfiguration _configuration;
        private string _dataRootPath = string.Empty;
        private string _importFolderPath = string.Empty;
        private string _exportFolderPath = string.Empty;
        private string _archiveFolderPath = string.Empty;
        private string _logsFolderPath = string.Empty;
        private string _backupFolderPath = string.Empty;
        private string _databasePath = string.Empty;
        private string _databaseConfigPath = string.Empty;

        public PathConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
            InitializeDefaultPaths();
        }

        public string DataRootPath => _dataRootPath;
        public string ImportFolderPath => _importFolderPath;
        public string ExportFolderPath => _exportFolderPath;
        public string ArchiveFolderPath => _archiveFolderPath;
        public string LogsFolderPath => _logsFolderPath;
        public string BackupFolderPath => _backupFolderPath;
        public string DatabasePath => _databasePath;
        public string DatabaseConfigPath => _databaseConfigPath;

        private void InitializeDefaultPaths()
        {
            // Configuration des chemins de base : utiliser un point central
            var usePortableMode = bool.Parse(_configuration["PathSettings:UsePortableMode"] ?? "true");
            
            if (usePortableMode)
            {
                // Mode portable : relatif à l'application
                var appPath = AppDomain.CurrentDomain.BaseDirectory;
                _dataRootPath = Path.Combine(appPath, "Data");
            }
            else
            {
                // Mode installation : données dans ProgramData
                _dataRootPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                    "FNEV4");
            }

            // Configuration des sous-dossiers
            _importFolderPath = _configuration["PathSettings:ImportFolder"] ?? 
                Path.Combine(_dataRootPath, "Import");
            
            _exportFolderPath = _configuration["PathSettings:ExportFolder"] ?? 
                Path.Combine(_dataRootPath, "Export");
            
            _archiveFolderPath = _configuration["PathSettings:ArchiveFolder"] ?? 
                Path.Combine(_dataRootPath, "Archive");
            
            _logsFolderPath = _configuration["PathSettings:LogsFolder"] ?? 
                Path.Combine(_dataRootPath, "Logs");
            
            _backupFolderPath = _configuration["PathSettings:BackupFolder"] ?? 
                Path.Combine(_dataRootPath, "Backup");

            // Configuration de la base de données
            _databasePath = _configuration.GetConnectionString("DefaultConnection") ?? 
                           Path.Combine(_dataRootPath, "fnev4.db");
            
            _databaseConfigPath = Path.Combine(_dataRootPath, "database-config.json");
        }

        public void EnsureDirectoriesExist()
        {
            CreateDirectoryIfNotExists(_dataRootPath);
            CreateDirectoryIfNotExists(_importFolderPath);
            CreateDirectoryIfNotExists(_exportFolderPath);
            CreateDirectoryIfNotExists(_archiveFolderPath);
            CreateDirectoryIfNotExists(_logsFolderPath);
            CreateDirectoryIfNotExists(_backupFolderPath);
            
            // Créer le dossier parent de la base de données
            var dbDirectory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(dbDirectory))
            {
                CreateDirectoryIfNotExists(dbDirectory);
            }
        }

        public void UpdatePaths(string importPath, string exportPath, string archivePath, string logsPath, string backupPath)
        {
            if (!string.IsNullOrWhiteSpace(importPath)) _importFolderPath = importPath;
            if (!string.IsNullOrWhiteSpace(exportPath)) _exportFolderPath = exportPath;
            if (!string.IsNullOrWhiteSpace(archivePath)) _archiveFolderPath = archivePath;
            if (!string.IsNullOrWhiteSpace(logsPath)) _logsFolderPath = logsPath;
            if (!string.IsNullOrWhiteSpace(backupPath)) _backupFolderPath = backupPath;

            // Sauvegarder la configuration mise à jour
            SaveConfiguration();
        }

        public bool ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                // Vérifier si le chemin existe
                if (Directory.Exists(path))
                    return true;

                // Vérifier si on peut créer le dossier
                var parentDirectory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parentDirectory) && Directory.Exists(parentDirectory))
                {
                    return true; // Le parent existe, on pourra créer le dossier
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public long CalculateDirectorySize(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return 0;

            try
            {
                var directoryInfo = new DirectoryInfo(folderPath);
                return CalculateDirectorySizeRecursive(directoryInfo);
            }
            catch
            {
                return 0;
            }
        }

        private void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private long CalculateDirectorySizeRecursive(DirectoryInfo directory)
        {
            long size = 0;

            // Taille des fichiers dans ce dossier
            foreach (var file in directory.GetFiles())
            {
                size += file.Length;
            }

            // Taille des sous-dossiers
            foreach (var subdirectory in directory.GetDirectories())
            {
                size += CalculateDirectorySizeRecursive(subdirectory);
            }

            return size;
        }

        private void SaveConfiguration()
        {
            // TODO: Implémenter la sauvegarde des chemins personnalisés
            // Cela pourrait être dans un fichier JSON local ou dans la base de données
        }
    }
}
