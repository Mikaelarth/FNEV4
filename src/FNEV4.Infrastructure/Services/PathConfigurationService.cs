using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FNEV4.Core.Interfaces;
using FNEV4.Core.Entities;
using FNEV4.Infrastructure.Data;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Service centralisé pour la gestion des chemins et dossiers de l'application
    /// </summary>
    public class PathConfigurationService : IPathConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly IDatabasePathProvider _databasePathProvider;
        private readonly FNEV4DbContext _context;
        private readonly ILogger<PathConfigurationService> _logger;
        private string _dataRootPath = string.Empty;
        private string _importFolderPath = string.Empty;
        private string _exportFolderPath = string.Empty;
        private string _archiveFolderPath = string.Empty;
        private string _logsFolderPath = string.Empty;
        private string _backupFolderPath = string.Empty;
        private string _databasePath = string.Empty;
        private string _databaseConfigPath = string.Empty;

        public PathConfigurationService(IConfiguration configuration, IDatabasePathProvider databasePathProvider, FNEV4DbContext context, ILogger<PathConfigurationService> logger)
        {
            _configuration = configuration;
            _databasePathProvider = databasePathProvider;
            _context = context;
            _logger = logger;
            InitializeDefaultPaths();
        }

        // Constructeur de fallback pour compatibilité (sera supprimé après migration)
        public PathConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
            _databasePathProvider = new DatabasePathProvider(); // Instance temporaire
            _context = null!; // Pas de base de données en mode fallback
            _logger = null!;
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
            
            // Déduire le répertoire data depuis le chemin de la base
            _dataRootPath = Path.GetDirectoryName(_databasePath) ?? Path.Combine(GetProjectRootPath(), "data");

            // Essayer de charger depuis la base de données d'abord
            if (_context != null)
            {
                LoadFromDatabaseAsync().GetAwaiter().GetResult();
            }

            // Configuration des sous-dossiers (relatifs au data fixe) - fallback si pas en base
            if (string.IsNullOrEmpty(_importFolderPath))
            {
                _importFolderPath = Path.Combine(_dataRootPath, "Import");
                _exportFolderPath = Path.Combine(_dataRootPath, "Export"); 
                _archiveFolderPath = Path.Combine(_dataRootPath, "Archive");
                _logsFolderPath = Path.Combine(_dataRootPath, "Logs");
                _backupFolderPath = Path.Combine(_dataRootPath, "Backup");
            }

            // Configuration du fichier de config de base
            _databaseConfigPath = Path.Combine(_dataRootPath, "database-config.json");
            
            // Log pour diagnostic
            System.Diagnostics.Debug.WriteLine($"[PathConfigurationService] Configuration centralisée:");
            System.Diagnostics.Debug.WriteLine($"[PathConfigurationService] Base de données: {_databasePath}");
            System.Diagnostics.Debug.WriteLine($"[PathConfigurationService] Dossier data: {_dataRootPath}");
        }

        /// <summary>
        /// Charge la configuration depuis la base de données
        /// </summary>
        private async Task LoadFromDatabaseAsync()
        {
            try
            {
                if (_context == null) return;

                _logger?.LogDebug("Chargement de la configuration des chemins depuis la base de données");
                
                var config = await _context.FolderConfigurations
                    .Where(f => f.IsActive && !f.IsDeleted)
                    .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
                    .FirstOrDefaultAsync();

                if (config != null)
                {
                    _importFolderPath = config.ImportFolderPath;
                    _exportFolderPath = config.ExportFolderPath;
                    _archiveFolderPath = config.ArchiveFolderPath;
                    _logsFolderPath = config.LogsFolderPath;
                    _backupFolderPath = config.BackupFolderPath;

                    _logger?.LogInformation("Configuration des chemins chargée depuis la base: {ConfigName}", config.Name);
                }
                else
                {
                    _logger?.LogInformation("Aucune configuration trouvée en base, utilisation des chemins par défaut");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur lors du chargement de la configuration depuis la base");
                // Continuer avec les chemins par défaut
            }
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
            
            // Persister en base de données si le contexte est disponible
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_context != null)
                    {
                        await SaveToDatabase();
                        _logger?.LogInformation("Configuration des chemins sauvegardée en base de données");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Erreur lors de la sauvegarde de la configuration en base");
                }
            });
        }

        /// <summary>
        /// Sauvegarde la configuration actuelle en base de données
        /// </summary>
        private async Task SaveToDatabase()
        {
            if (_context == null) return;

            try
            {
                // Chercher une configuration active existante
                var existingConfig = await _context.FolderConfigurations
                    .Where(f => f.IsActive && !f.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingConfig != null)
                {
                    // Mettre à jour la configuration existante
                    existingConfig.ImportFolderPath = _importFolderPath;
                    existingConfig.ExportFolderPath = _exportFolderPath;
                    existingConfig.ArchiveFolderPath = _archiveFolderPath;
                    existingConfig.LogsFolderPath = _logsFolderPath;
                    existingConfig.BackupFolderPath = _backupFolderPath;
                    existingConfig.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Créer une nouvelle configuration
                    var newConfig = new FolderConfiguration
                    {
                        Name = "Configuration Automatique",
                        Description = "Configuration créée automatiquement par PathConfigurationService",
                        ImportFolderPath = _importFolderPath,
                        ExportFolderPath = _exportFolderPath,
                        ArchiveFolderPath = _archiveFolderPath,
                        LogsFolderPath = _logsFolderPath,
                        BackupFolderPath = _backupFolderPath,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.FolderConfigurations.Add(newConfig);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur lors de la sauvegarde en base de données");
                throw;
            }
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
