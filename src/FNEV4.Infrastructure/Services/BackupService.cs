using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using FNEV4.Core.Interfaces;
using FNEV4.Infrastructure.Data;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Service de sauvegarde de la base de données SQLite.
    /// Gère la création, restauration et maintenance des sauvegardes.
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly ILogger<BackupService> _logger;
        private readonly FNEV4DbContext _context;
        private readonly IPathConfigurationService _pathConfigurationService;
        private readonly ILoggingService _loggingService;
        private bool _autoBackupActive = false;

        public BackupService(
            ILogger<BackupService> logger,
            FNEV4DbContext context,
            IPathConfigurationService pathConfigurationService,
            ILoggingService loggingService)
        {
            _logger = logger;
            _context = context;
            _pathConfigurationService = pathConfigurationService;
            _loggingService = loggingService;
        }

        #region Backup Operations

        /// <summary>
        /// Crée une sauvegarde de la base de données SQLite.
        /// </summary>
        public async Task<string> CreateBackupAsync(string backupFolderPath, string? backupName = null)
        {
            try
            {
                // Créer le dossier de sauvegarde s'il n'existe pas
                Directory.CreateDirectory(backupFolderPath);

                // Générer le nom du fichier de sauvegarde
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = backupName ?? $"FNEV4_backup_{timestamp}.db";
                if (!fileName.EndsWith(".db") && !fileName.EndsWith(".bak"))
                {
                    fileName += ".db";
                }

                var backupFilePath = Path.Combine(backupFolderPath, fileName);

                // Obtenir le chemin de la base de données depuis le service de configuration
                var sourceDbPath = _pathConfigurationService.DatabasePath;

                if (string.IsNullOrEmpty(sourceDbPath) || !File.Exists(sourceDbPath))
                {
                    throw new FileNotFoundException($"Base de données source non trouvée: {sourceDbPath}");
                }

                // Effectuer la sauvegarde avec SQLite VACUUM INTO
                await CreateSqliteBackupAsync(sourceDbPath, backupFilePath);

                // Log de la sauvegarde
                var fileInfo = new FileInfo(backupFilePath);
                await _loggingService.LogInfoAsync(
                    $"Sauvegarde créée: {fileName} ({FormatBytes(fileInfo.Length)})", 
                    "Backup");

                _logger.LogInformation("Sauvegarde créée avec succès: {BackupPath}", backupFilePath);

                return backupFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la sauvegarde");
                await _loggingService.LogErrorAsync($"Erreur sauvegarde: {ex.Message}", "Backup");
                throw;
            }
        }

        /// <summary>
        /// Crée une sauvegarde automatique selon la configuration.
        /// </summary>
        public async Task<string?> CreateAutoBackupAsync(BackupConfiguration configuration)
        {
            try
            {
                var lastBackup = await GetLastBackupInfoAsync(configuration.BackupFolderPath);
                
                // Vérifier si une sauvegarde est nécessaire
                if (ShouldCreateBackup(lastBackup, configuration.Frequency))
                {
                    var backupName = $"auto_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                    var backupPath = await CreateBackupAsync(configuration.BackupFolderPath, backupName);

                    // Compression si activée
                    if (configuration.EnableCompression)
                    {
                        backupPath = await CompressBackupAsync(backupPath);
                    }

                    // Nettoyage automatique si activé
                    if (configuration.EnableAutoCleanup)
                    {
                        await CleanupOldBackupsAsync(configuration.BackupFolderPath, configuration.RetentionPolicy);
                    }

                    return backupPath;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde automatique");
                await _loggingService.LogErrorAsync($"Erreur sauvegarde automatique: {ex.Message}", "Backup");
                return null;
            }
        }

        /// <summary>
        /// Restaure la base de données depuis une sauvegarde.
        /// </summary>
        public async Task<bool> RestoreBackupAsync(string backupFilePath, bool overwriteExisting = false)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                {
                    throw new FileNotFoundException($"Fichier de sauvegarde non trouvé: {backupFilePath}");
                }

                // Valider le fichier de sauvegarde
                var validation = await ValidateBackupAsync(backupFilePath);
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException($"Sauvegarde invalide: {string.Join(", ", validation.Errors)}");
                }

                var targetDbPath = _pathConfigurationService.DatabasePath;

                if (File.Exists(targetDbPath) && !overwriteExisting)
                {
                    throw new InvalidOperationException("La base de données existe déjà. Utilisez overwriteExisting=true pour la remplacer.");
                }

                // S'assurer que les connexions sont fermées en disposant temporairement le contexte
                // Le contexte sera réutilisé après la restauration

                // Sauvegarder l'actuelle base avant restauration
                if (File.Exists(targetDbPath))
                {
                    var backupCurrentPath = $"{targetDbPath}.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                    File.Copy(targetDbPath, backupCurrentPath);
                }

                // Décompresser si nécessaire
                var sourceFile = backupFilePath;
                if (backupFilePath.EndsWith(".gz"))
                {
                    sourceFile = await DecompressBackupAsync(backupFilePath);
                }

                // Copier le fichier de sauvegarde
                File.Copy(sourceFile, targetDbPath, true);

                await _loggingService.LogInfoAsync($"Base de données restaurée depuis: {Path.GetFileName(backupFilePath)}", "Backup");

                _logger.LogInformation("Restauration réussie depuis: {BackupPath}", backupFilePath);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la restauration");
                await _loggingService.LogErrorAsync($"Erreur restauration: {ex.Message}", "Backup");
                return false;
            }
        }

        /// <summary>
        /// Valide l'intégrité d'un fichier de sauvegarde.
        /// </summary>
        public async Task<BackupValidationResult> ValidateBackupAsync(string backupFilePath)
        {
            var result = new BackupValidationResult
            {
                ValidationMethod = "SQLite Integrity Check"
            };

            try
            {
                if (!File.Exists(backupFilePath))
                {
                    result.Errors.Add("Fichier de sauvegarde non trouvé");
                    return result;
                }

                var fileInfo = new FileInfo(backupFilePath);
                result.ActualSize = fileInfo.Length;

                if (fileInfo.Length == 0)
                {
                    result.Errors.Add("Fichier de sauvegarde vide");
                    return result;
                }

                // Décompresser temporairement si nécessaire
                var testFilePath = backupFilePath;
                var isCompressed = backupFilePath.EndsWith(".gz");

                if (isCompressed)
                {
                    testFilePath = Path.GetTempFileName();
                    await DecompressBackupToFileAsync(backupFilePath, testFilePath);
                }

                try
                {
                    // Tester l'ouverture de la base de données
                    using var connection = new SqliteConnection($"Data Source={testFilePath};Mode=ReadOnly");
                    await connection.OpenAsync();

                    // Vérifier l'intégrité
                    using var command = connection.CreateCommand();
                    command.CommandText = "PRAGMA integrity_check";
                    var checkResult = await command.ExecuteScalarAsync() as string;

                    if (checkResult != "ok")
                    {
                        result.Errors.Add($"Intégrité compromise: {checkResult}");
                    }
                    else
                    {
                        result.IsValid = true;
                    }
                }
                finally
                {
                    // Nettoyer le fichier temporaire
                    if (isCompressed && File.Exists(testFilePath))
                    {
                        File.Delete(testFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Erreur de validation: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region Backup Management

        /// <summary>
        /// Obtient la liste des sauvegardes disponibles.
        /// </summary>
        public Task<List<BackupInfo>> GetAvailableBackupsAsync(string backupFolderPath)
        {
            var backups = new List<BackupInfo>();

            try
            {
                if (!Directory.Exists(backupFolderPath))
                {
                    return Task.FromResult(backups);
                }

                var files = Directory.GetFiles(backupFolderPath, "*.*")
                    .Where(f => f.EndsWith(".db") || f.EndsWith(".bak") || f.EndsWith(".gz"))
                    .OrderByDescending(File.GetLastWriteTime);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    var backup = new BackupInfo
                    {
                        FilePath = file,
                        FileName = fileInfo.Name,
                        CreatedAt = fileInfo.CreationTime,
                        FileSize = fileInfo.Length,
                        IsCompressed = file.EndsWith(".gz"),
                        BackupType = file.Contains("auto_") ? "Auto" : "Manual"
                    };

                    // Validation basique
                    if (fileInfo.Length > 0)
                    {
                        backup.IsValid = true;
                    }

                    backups.Add(backup);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des sauvegardes");
            }

            return Task.FromResult(backups);
        }

        /// <summary>
        /// Supprime les anciennes sauvegardes selon la politique de rétention.
        /// </summary>
        public async Task<int> CleanupOldBackupsAsync(string backupFolderPath, BackupRetentionPolicy retentionPolicy)
        {
            var deletedCount = 0;

            try
            {
                var backups = await GetAvailableBackupsAsync(backupFolderPath);
                var now = DateTime.Now;

                var toDelete = new List<BackupInfo>();

                // Sauvegardes automatiques plus anciennes que la politique
                var autoBackups = backups.Where(b => b.BackupType == "Auto").OrderByDescending(b => b.CreatedAt);
                
                var dailyBackups = autoBackups.Where(b => (now - b.CreatedAt).TotalDays > retentionPolicy.KeepDailyBackups);
                toDelete.AddRange(dailyBackups);

                // Vérifier la taille totale
                var totalSize = backups.Sum(b => b.FileSize);
                if (totalSize > retentionPolicy.MaxTotalSize)
                {
                    var oldestBackups = backups.OrderBy(b => b.CreatedAt);
                    foreach (var backup in oldestBackups)
                    {
                        if (totalSize <= retentionPolicy.MaxTotalSize)
                            break;

                        if (!toDelete.Contains(backup))
                        {
                            toDelete.Add(backup);
                            totalSize -= backup.FileSize;
                        }
                    }
                }

                // Supprimer les fichiers
                foreach (var backup in toDelete)
                {
                    try
                    {
                        File.Delete(backup.FilePath);
                        deletedCount++;
                        
                        await _loggingService.LogInfoAsync(
                            $"Sauvegarde supprimée: {backup.FileName} ({FormatBytes(backup.FileSize)})", 
                            "Backup");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Impossible de supprimer la sauvegarde: {FilePath}", backup.FilePath);
                    }
                }

                if (deletedCount > 0)
                {
                    await _loggingService.LogInfoAsync($"Nettoyage terminé: {deletedCount} sauvegarde(s) supprimée(s)", "Backup");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage des sauvegardes");
                await _loggingService.LogErrorAsync($"Erreur nettoyage sauvegardes: {ex.Message}", "Backup");
            }

            return deletedCount;
        }

        /// <summary>
        /// Supprime une sauvegarde spécifique.
        /// </summary>
        public async Task<bool> DeleteBackupAsync(string backupFilePath)
        {
            try
            {
                if (File.Exists(backupFilePath))
                {
                    var fileInfo = new FileInfo(backupFilePath);
                    File.Delete(backupFilePath);
                    
                    await _loggingService.LogInfoAsync(
                        $"Sauvegarde supprimée: {fileInfo.Name} ({FormatBytes(fileInfo.Length)})", 
                        "Backup");
                    
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la sauvegarde");
                await _loggingService.LogErrorAsync($"Erreur suppression sauvegarde: {ex.Message}", "Backup");
                return false;
            }
        }

        /// <summary>
        /// Compresse une sauvegarde pour économiser l'espace.
        /// </summary>
        public async Task<string> CompressBackupAsync(string backupFilePath)
        {
            try
            {
                var compressedPath = backupFilePath + ".gz";

                using (var originalFileStream = File.OpenRead(backupFilePath))
                using (var compressedFileStream = File.Create(compressedPath))
                using (var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                {
                    await originalFileStream.CopyToAsync(compressionStream);
                }

                // Supprimer le fichier original et renommer le compressé
                File.Delete(backupFilePath);

                var originalSize = new FileInfo(backupFilePath).Length;
                var compressedSize = new FileInfo(compressedPath).Length;
                var compressionRatio = (1.0 - (double)compressedSize / originalSize) * 100;

                await _loggingService.LogInfoAsync(
                    $"Sauvegarde compressée: {Path.GetFileName(compressedPath)}, taux de compression: {compressionRatio:F1}%", 
                    "Backup");

                return compressedPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la compression");
                await _loggingService.LogErrorAsync($"Erreur compression: {ex.Message}", "Backup");
                throw;
            }
        }

        #endregion

        #region Information and Statistics

        /// <summary>
        /// Obtient les informations de la dernière sauvegarde.
        /// </summary>
        public async Task<BackupInfo?> GetLastBackupInfoAsync(string backupFolderPath)
        {
            var backups = await GetAvailableBackupsAsync(backupFolderPath);
            return backups.OrderByDescending(b => b.CreatedAt).FirstOrDefault();
        }

        /// <summary>
        /// Calcule l'espace disque utilisé par les sauvegardes.
        /// </summary>
        public async Task<long> GetBackupsSpaceUsageAsync(string backupFolderPath)
        {
            var backups = await GetAvailableBackupsAsync(backupFolderPath);
            return backups.Sum(b => b.FileSize);
        }

        /// <summary>
        /// Obtient les statistiques des sauvegardes.
        /// </summary>
        public async Task<BackupStatistics> GetBackupStatisticsAsync(string backupFolderPath)
        {
            var backups = await GetAvailableBackupsAsync(backupFolderPath);
            
            var statistics = new BackupStatistics
            {
                TotalBackups = backups.Count,
                ValidBackups = backups.Count(b => b.IsValid),
                CompressedBackups = backups.Count(b => b.IsCompressed),
                TotalSize = backups.Sum(b => b.FileSize),
                OldestBackup = backups.MinBy(b => b.CreatedAt)?.CreatedAt,
                NewestBackup = backups.MaxBy(b => b.CreatedAt)?.CreatedAt,
                LargestBackup = backups.MaxBy(b => b.FileSize),
                SmallestBackup = backups.MinBy(b => b.FileSize),
                AverageSize = backups.Count > 0 ? backups.Average(b => b.FileSize) : 0
            };

            // Grouper par type
            statistics.BackupsByType = backups.GroupBy(b => b.BackupType)
                .ToDictionary(g => g.Key, g => g.Count());

            // Grouper par date
            statistics.BackupsByDate = backups.GroupBy(b => b.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            return statistics;
        }

        /// <summary>
        /// Estime la taille d'une future sauvegarde.
        /// </summary>
        public Task<long> EstimateBackupSizeAsync()
        {
            try
            {
                var dbPath = _pathConfigurationService.DatabasePath;

                if (File.Exists(dbPath))
                {
                    return Task.FromResult(new FileInfo(dbPath).Length);
                }

                return Task.FromResult(0L);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'estimation de la taille");
                return Task.FromResult(0L);
            }
        }

        #endregion

        #region Scheduling (Implémentation basique)

        public async Task<bool> ConfigureAutoBackupAsync(BackupConfiguration configuration)
        {
            // Implémentation basique - dans un vrai projet, on utiliserait un scheduler
            await _loggingService.LogInfoAsync("Configuration de sauvegarde automatique mise à jour", "Backup");
            return true;
        }

        public async Task<bool> StartAutoBackupServiceAsync()
        {
            _autoBackupActive = true;
            await _loggingService.LogInfoAsync("Service de sauvegarde automatique démarré", "Backup");
            return true;
        }

        public async Task<bool> StopAutoBackupServiceAsync()
        {
            _autoBackupActive = false;
            await _loggingService.LogInfoAsync("Service de sauvegarde automatique arrêté", "Backup");
            return true;
        }

        public bool IsAutoBackupActive()
        {
            return _autoBackupActive;
        }

        #endregion

        #region Import/Export (Implémentation basique)

        public async Task<string> ExportBackupAsync(string backupFilePath, string destinationPath, bool includeMetadata = true)
        {
            var destinationFile = Path.Combine(destinationPath, Path.GetFileName(backupFilePath));
            File.Copy(backupFilePath, destinationFile, true);
            
            await _loggingService.LogInfoAsync($"Sauvegarde exportée vers: {destinationFile}", "Backup");
            return destinationFile;
        }

        public async Task<string> ImportBackupAsync(string sourceFilePath, string backupFolderPath)
        {
            var destinationFile = Path.Combine(backupFolderPath, Path.GetFileName(sourceFilePath));
            File.Copy(sourceFilePath, destinationFile, true);
            
            await _loggingService.LogInfoAsync($"Sauvegarde importée: {destinationFile}", "Backup");
            return destinationFile;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Extrait le chemin de la base de données depuis la chaîne de connexion.
        /// </summary>
        private string ExtractDatabasePathFromConnectionString(string connectionString)
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            return builder.DataSource;
        }

        /// <summary>
        /// Crée une sauvegarde SQLite en utilisant VACUUM INTO.
        /// </summary>
        private async Task CreateSqliteBackupAsync(string sourceDbPath, string backupDbPath)
        {
            using var connection = new SqliteConnection($"Data Source={sourceDbPath}");
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = $"VACUUM INTO '{backupDbPath}'";
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Détermine si une sauvegarde doit être créée selon la fréquence.
        /// </summary>
        private bool ShouldCreateBackup(BackupInfo? lastBackup, BackupFrequency frequency)
        {
            if (lastBackup == null) return true;

            var timeSinceLastBackup = DateTime.Now - lastBackup.CreatedAt;

            return frequency switch
            {
                BackupFrequency.Hourly => timeSinceLastBackup.TotalHours >= 1,
                BackupFrequency.Daily => timeSinceLastBackup.TotalDays >= 1,
                BackupFrequency.Weekly => timeSinceLastBackup.TotalDays >= 7,
                BackupFrequency.Monthly => timeSinceLastBackup.TotalDays >= 30,
                _ => false
            };
        }

        /// <summary>
        /// Décompresse un fichier de sauvegarde.
        /// </summary>
        private async Task<string> DecompressBackupAsync(string compressedFilePath)
        {
            var decompressedPath = Path.GetTempFileName();
            await DecompressBackupToFileAsync(compressedFilePath, decompressedPath);
            return decompressedPath;
        }

        /// <summary>
        /// Décompresse un fichier vers un fichier de destination.
        /// </summary>
        private async Task DecompressBackupToFileAsync(string compressedFilePath, string destinationPath)
        {
            using (var compressedFileStream = File.OpenRead(compressedFilePath))
            using (var decompressionStream = new GZipStream(compressedFileStream, CompressionMode.Decompress))
            using (var destinationFileStream = File.Create(destinationPath))
            {
                await decompressionStream.CopyToAsync(destinationFileStream);
            }
        }

        /// <summary>
        /// Formate la taille en bytes en format lisible.
        /// </summary>
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        #endregion
    }
}