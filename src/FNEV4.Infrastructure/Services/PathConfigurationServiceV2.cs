using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using FNEV4.Core.Interfaces;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Service centralisé pour la gestion des chemins et dossiers - Version Production-Ready
    /// Gère automatiquement les environnements Development/Production
    /// </summary>
    public class PathConfigurationServiceV2 : IPathConfigurationService
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

        public PathConfigurationServiceV2(IConfiguration configuration)
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
            // Obtenir le chemin racine selon l'environnement
            var dataRoot = GetEnvironmentSpecificDataPath();
            _dataRootPath = dataRoot;

            // Configuration des sous-dossiers
            _importFolderPath = Path.Combine(_dataRootPath, "Import");
            _exportFolderPath = Path.Combine(_dataRootPath, "Export"); 
            _archiveFolderPath = Path.Combine(_dataRootPath, "Archive");
            _logsFolderPath = Path.Combine(_dataRootPath, "Logs");
            _backupFolderPath = Path.Combine(_dataRootPath, "Backup");

            // Configuration de la base de données
            _databasePath = Path.Combine(_dataRootPath, "FNEV4.db");
            _databaseConfigPath = Path.Combine(_dataRootPath, "database-config.json");
        }

        /// <summary>
        /// Obtient le chemin des données selon l'environnement (Dev/Prod) et les contraintes système
        /// </summary>
        private string GetEnvironmentSpecificDataPath()
        {
            // 1. Variable d'environnement personnalisée (priorité max)
            var customPath = Environment.GetEnvironmentVariable("FNEV4_DATA_PATH");
            if (!string.IsNullOrEmpty(customPath))
            {
                var customDir = Path.GetDirectoryName(customPath) ?? customPath;
                if (HasWritePermission(customDir))
                {
                    return customPath;
                }
            }

            // 2. Détecter l'environnement
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
                             Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? 
                             "Production";
            
            var isDevelopment = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

            if (isDevelopment)
            {
                return GetDevelopmentDataPath();
            }
            else
            {
                return GetProductionDataPath();
            }
        }

        /// <summary>
        /// Chemin pour l'environnement de développement
        /// </summary>
        private string GetDevelopmentDataPath()
        {
            // Chercher le dossier contenant FNEV4.sln (méthode développement)
            var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            
            while (currentDir != null)
            {
                if (File.Exists(Path.Combine(currentDir.FullName, "FNEV4.sln")))
                {
                    return Path.Combine(currentDir.FullName, "data");
                }
                currentDir = currentDir.Parent;
            }
            
            // Fallback développement (chemin fixe local)
            var devFallback = Path.Combine(@"C:\wamp64\www\FNEV4", "data");
            if (Directory.Exists(Path.GetDirectoryName(devFallback)))
            {
                return devFallback;
            }

            // Si même le fallback dev échoue, utiliser la stratégie production
            return GetProductionDataPath();
        }

        /// <summary>
        /// Chemin pour l'environnement de production
        /// </summary>
        private string GetProductionDataPath()
        {
            // Option 1: Mode portable (à côté de l'exécutable)
            var usePortableMode = bool.Parse(_configuration["PathSettings:UsePortableMode"] ?? "false");
            if (usePortableMode)
            {
                var exeDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
                var portableDataPath = Path.Combine(exeDir ?? "", "Data");
                if (!string.IsNullOrEmpty(exeDir) && HasWritePermission(exeDir))
                {
                    return portableDataPath;
                }
            }

            // Option 2: Dossier utilisateur (AppData/Local) - Recommandé pour production
            var userDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDataPath = Path.Combine(userDataPath, "FNEV4");
            
            if (HasWritePermission(userDataPath))
            {
                return appDataPath;
            }

            // Option 3: Dossier utilisateur Documents (fallback)
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var documentsAppPath = Path.Combine(documentsPath, "FNEV4");
            
            if (HasWritePermission(documentsPath))
            {
                return documentsAppPath;
            }

            // Option 4: Dossier temporaire (dernier recours)
            var tempPath = Path.GetTempPath();
            return Path.Combine(tempPath, "FNEV4");
        }

        /// <summary>
        /// Vérifie si le dossier a les permissions d'écriture
        /// </summary>
        private bool HasWritePermission(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var testFile = Path.Combine(path, $"fnev4_write_test_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "write test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
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
            if (ValidatePath(importPath)) _importFolderPath = importPath;
            if (ValidatePath(exportPath)) _exportFolderPath = exportPath;
            if (ValidatePath(archivePath)) _archiveFolderPath = archivePath;
            if (ValidatePath(logsPath)) _logsFolderPath = logsPath;
            if (ValidatePath(backupPath)) _backupFolderPath = backupPath;
            
            EnsureDirectoriesExist();
        }

        public bool ValidatePath(string path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path) && 
                       Path.IsPathRooted(path) && 
                       HasWritePermission(Path.GetDirectoryName(path) ?? path);
            }
            catch
            {
                return false;
            }
        }

        public long CalculateDirectorySize(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                    return 0;

                return CalculateDirectorySizeRecursive(new DirectoryInfo(folderPath));
            }
            catch
            {
                return 0;
            }
        }

        private void CreateDirectoryIfNotExists(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - allow app to continue
                Console.WriteLine($"Warning: Could not create directory {path}: {ex.Message}");
            }
        }

        private long CalculateDirectorySizeRecursive(DirectoryInfo directory)
        {
            long size = 0;
            
            try
            {
                // Taille des fichiers dans ce dossier
                foreach (FileInfo file in directory.GetFiles())
                {
                    size += file.Length;
                }

                // Taille des sous-dossiers (récursif)
                foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                {
                    size += CalculateDirectorySizeRecursive(subDirectory);
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
