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
        private readonly IDatabasePathProvider _databasePathProvider;
        private string _dataRootPath = string.Empty;
        private string _importFolderPath = string.Empty;
        private string _exportFolderPath = string.Empty;
        private string _archiveFolderPath = string.Empty;
        private string _logsFolderPath = string.Empty;
        private string _backupFolderPath = string.Empty;
        private string _databasePath = string.Empty;
        private string _databaseConfigPath = string.Empty;

        public PathConfigurationService(IConfiguration configuration, IDatabasePathProvider databasePathProvider)
        {
            _configuration = configuration;
            _databasePathProvider = databasePathProvider;
            InitializeDefaultPaths();
        }

        // Constructeur de fallback pour compatibilité (sera supprimé après migration)
        public PathConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
            _databasePathProvider = new DatabasePathProvider(); // Instance temporaire
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
            // SOLUTION CENTRALISÉE: Utiliser le provider centralisé pour la base de données
            _databasePath = _databasePathProvider.DatabasePath;
            
            // Détermine le mode de chemin depuis la configuration
            var pathMode = _configuration["Environment:PathMode"] ?? "Auto";
            var environmentType = _configuration["Environment:Type"] ?? "Development";
            
            // Déduire le répertoire data selon le mode
            _dataRootPath = GetDataRootPath(pathMode, environmentType);

            // Configuration des sous-dossiers
            InitializeFolderPaths(pathMode);

            // Configuration du fichier de config de base
            _databaseConfigPath = Path.Combine(_dataRootPath, "database-config.json");
            
            // Log pour diagnostic
            System.Diagnostics.Debug.WriteLine($"[PathConfigurationService] Configuration centralisée:");
            System.Diagnostics.Debug.WriteLine($"[PathConfigurationService] Mode: {pathMode}");
            System.Diagnostics.Debug.WriteLine($"[PathConfigurationService] Base de données: {_databasePath}");
            System.Diagnostics.Debug.WriteLine($"[PathConfigurationService] Dossier data: {_dataRootPath}");
        }

        private string GetDataRootPath(string pathMode, string environmentType)
        {
            // 1. Chemin explicite dans la configuration
            var configDataRoot = _configuration["PathSettings:DataRootPath"];
            if (!string.IsNullOrEmpty(configDataRoot))
            {
                return ExpandEnvironmentVariables(configDataRoot);
            }
            
            // 2. Basé sur le mode de chemin
            switch (pathMode.ToLower())
            {
                case "appdata":
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "FNEV4");
                
                case "project":
                    return Path.Combine(GetProjectRootPath(), "data");
                
                case "custom":
                    // Utilise le répertoire de la base de données
                    return Path.GetDirectoryName(_databasePath) ?? Path.Combine(GetProjectRootPath(), "data");
                
                case "auto":
                default:
                    // Auto: Basé sur l'environnement
                    if (environmentType.ToLower() == "production")
                    {
                        return Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "FNEV4");
                    }
                    else
                    {
                        return Path.Combine(GetProjectRootPath(), "data");
                    }
            }
        }

        private void InitializeFolderPaths(string pathMode)
        {
            // Chemins par défaut (relatifs au DataRoot)
            var defaultImport = _configuration["PathSettings:ImportFolder"] ?? "Import";
            var defaultExport = _configuration["PathSettings:ExportFolder"] ?? "Export";
            var defaultArchive = _configuration["PathSettings:ArchiveFolder"] ?? "Archive";
            var defaultLogs = _configuration["PathSettings:LogsFolder"] ?? "Logs";
            var defaultBackup = _configuration["PathSettings:BackupFolder"] ?? "Backup";
            
            // Chemins personnalisés (absolus)
            var customImport = _configuration["PathSettings:CustomPaths:ImportFolder"];
            var customExport = _configuration["PathSettings:CustomPaths:ExportFolder"];
            var customArchive = _configuration["PathSettings:CustomPaths:ArchiveFolder"];
            var customLogs = _configuration["PathSettings:CustomPaths:LogsFolder"];
            var customBackup = _configuration["PathSettings:CustomPaths:BackupFolder"];

            // Utiliser le chemin personnalisé s'il existe, sinon le chemin par défaut
            _importFolderPath = GetFolderPath(customImport, defaultImport);
            _exportFolderPath = GetFolderPath(customExport, defaultExport);
            _archiveFolderPath = GetFolderPath(customArchive, defaultArchive);
            _logsFolderPath = GetFolderPath(customLogs, defaultLogs);
            _backupFolderPath = GetFolderPath(customBackup, defaultBackup);
        }

        private string GetFolderPath(string? customPath, string defaultPath)
        {
            if (!string.IsNullOrEmpty(customPath))
            {
                var expandedPath = ExpandEnvironmentVariables(customPath);
                return Path.IsPathRooted(expandedPath) ? expandedPath : Path.Combine(_dataRootPath, expandedPath);
            }
            
            return Path.Combine(_dataRootPath, defaultPath);
        }

        private string ExpandEnvironmentVariables(string? path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            
            // Expand des variables d'environnement Windows comme %LocalAppData%
            var expanded = Environment.ExpandEnvironmentVariables(path);
            
            // Support pour des variables personnalisées
            expanded = expanded.Replace("{UserProfile}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            expanded = expanded.Replace("{AppData}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            expanded = expanded.Replace("{LocalAppData}", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            expanded = expanded.Replace("{Documents}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            
            return expanded;
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
