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
            // Configuration des chemins de base : CHEMIN FIXE POUR ÉVITER LA DISPERSION
            var usePortableMode = bool.Parse(_configuration["PathSettings:UsePortableMode"] ?? "true");
            
            // SOLUTION: Toujours utiliser le répertoire racine du projet
            // Cela évite la création de bases multiples selon le répertoire de travail
            var projectRoot = GetProjectRootPath();
            _dataRootPath = Path.Combine(projectRoot, "data");

            // Configuration des sous-dossiers (relatifs au data fixe)
            _importFolderPath = Path.Combine(_dataRootPath, "Import");
            _exportFolderPath = Path.Combine(_dataRootPath, "Export"); 
            _archiveFolderPath = Path.Combine(_dataRootPath, "Archive");
            _logsFolderPath = Path.Combine(_dataRootPath, "Logs");
            _backupFolderPath = Path.Combine(_dataRootPath, "Backup");

            // Configuration de la base de données : CHEMIN ABSOLU FIXE
            _databasePath = Path.Combine(_dataRootPath, "FNEV4.db");
            _databaseConfigPath = Path.Combine(_dataRootPath, "database-config.json");
        }

        /// <summary>
        /// Trouve le répertoire racine du projet en remontant l'arborescence
        /// </summary>
        private string GetProjectRootPath()
        {
            // Chercher le dossier contenant FNEV4.sln
            var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            
            while (currentDir != null)
            {
                if (File.Exists(Path.Combine(currentDir.FullName, "FNEV4.sln")))
                {
                    return currentDir.FullName;
                }
                currentDir = currentDir.Parent;
            }
            
            // Méthode 1: Variable d'environnement personnalisée
            var customPath = Environment.GetEnvironmentVariable("FNEV4_ROOT");
            if (!string.IsNullOrEmpty(customPath) && Directory.Exists(customPath))
            {
                return customPath;
            }
            
            // Méthode 2: Chemin fixe local de développement
            var devPath = @"C:\wamp64\www\FNEV4";
            if (Directory.Exists(devPath))
            {
                return devPath;
            }
            
            // Méthode 3: Dernier recours - répertoire courant
            return AppDomain.CurrentDomain.BaseDirectory;
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

            EnsureDirectoriesExist();
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

            try
            {
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
            }
            catch
            {
                // Ignorer les erreurs d'accès
            }

            return size;
        }
    }
}
