using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using FNEV4.Core.Services;
using FNEV4.Infrastructure.Services;
using AppLogLevel = FNEV4.Core.Entities.LogLevel;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Implémentation du service de configuration du logging
    /// </summary>
    public class LoggingConfigurationService : ILoggingConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly IPathConfigurationService _pathConfigurationService;
        private readonly ILogger<LoggingConfigurationService> _logger;
        private AppLogLevel _minimumLogLevel = AppLogLevel.Info;
        private bool _rotationEnabled = true;
        private bool _hybridLoggingEnabled = true; // Mode hybride activé par défaut

        public LoggingConfigurationService(
            IConfiguration configuration,
            IPathConfigurationService pathConfigurationService,
            ILogger<LoggingConfigurationService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _pathConfigurationService = pathConfigurationService ?? throw new ArgumentNullException(nameof(pathConfigurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            LoadConfiguration();
        }

        public AppLogLevel MinimumLogLevel => _minimumLogLevel;
        public bool RotationEnabled => _rotationEnabled;
        public bool HybridLoggingEnabled => _hybridLoggingEnabled;

        public async Task SetMinimumLogLevelAsync(AppLogLevel level)
        {
            _minimumLogLevel = level;
            await SaveConfigurationAsync();
            _logger.LogInformation($"Niveau de log minimum configuré à : {FormatLogLevel(level)}");
        }

        public async Task SetRotationEnabledAsync(bool enabled)
        {
            _rotationEnabled = enabled;
            await SaveConfigurationAsync();
            _logger.LogInformation($"Rotation automatique des logs : {(enabled ? "activée" : "désactivée")}");
            
            if (enabled)
            {
                await PerformRotationIfNeededAsync();
            }
        }

        public async Task SetHybridLoggingEnabledAsync(bool enabled)
        {
            _hybridLoggingEnabled = enabled;
            await SaveConfigurationAsync();
            _logger.LogInformation($"Logging hybride : {(enabled ? "activé (Error/Warning → DB+Files, Info/Debug/Trace → Files)" : "désactivé (tout → DB)")}");
        }

        public AppLogLevel ParseLogLevel(string levelString)
        {
            return levelString?.ToLowerInvariant() switch
            {
                "minimal" => AppLogLevel.Debug,
                "debug" => AppLogLevel.Debug,
                "information" => AppLogLevel.Info,
                "info" => AppLogLevel.Info,
                "avertissement" => AppLogLevel.Warning,
                "warning" => AppLogLevel.Warning,
                "erreur" => AppLogLevel.Error,
                "error" => AppLogLevel.Error,
                "critique" => AppLogLevel.Critical,
                "critical" => AppLogLevel.Critical,
                _ => AppLogLevel.Info
            };
        }

        public string FormatLogLevel(AppLogLevel level)
        {
            return level switch
            {
                AppLogLevel.Debug => "Debug",
                AppLogLevel.Info => "Information",
                AppLogLevel.Warning => "Avertissement",
                AppLogLevel.Error => "Erreur",
                AppLogLevel.Critical => "Critique",
                _ => "Information"
            };
        }

        public async Task PerformRotationIfNeededAsync()
        {
            if (!_rotationEnabled)
                return;

            try
            {
                var logsPath = _pathConfigurationService.LogsFolderPath;
                if (!Directory.Exists(logsPath))
                    return;

                var logFiles = Directory.GetFiles(logsPath, "*.log");
                var rotationDate = DateTime.Now.AddDays(-7); // Archiver les logs de plus de 7 jours

                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.LastWriteTime < rotationDate)
                    {
                        await ArchiveLogFileAsync(logFile);
                    }
                }

                _logger.LogInformation("Rotation des logs effectuée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la rotation des logs");
            }
        }

        private void LoadConfiguration()
        {
            // Charger depuis la configuration
            var logLevelString = _configuration["Logging:LogLevel:Default"] ?? "Information";
            _minimumLogLevel = ParseLogLevel(logLevelString);

            var rotationEnabledString = _configuration["Logging:RotationEnabled"] ?? "true";
            _rotationEnabled = bool.Parse(rotationEnabledString);

            var hybridLoggingString = _configuration["Logging:HybridEnabled"] ?? "true";
            _hybridLoggingEnabled = bool.Parse(hybridLoggingString);
        }

        private async Task SaveConfigurationAsync()
        {
            // Dans une vraie implémentation, on sauvegarderait dans appsettings.json
            // ou dans une base de données de configuration
            // Pour l'instant, on garde juste en mémoire
            await Task.CompletedTask;
        }

        private async Task ArchiveLogFileAsync(string logFilePath)
        {
            try
            {
                var fileName = Path.GetFileName(logFilePath);
                var archivePath = Path.Combine(_pathConfigurationService.LogsFolderPath, "Archive");
                
                Directory.CreateDirectory(archivePath);
                
                var archiveFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now:yyyyMMdd}{Path.GetExtension(fileName)}";
                var archiveFullPath = Path.Combine(archivePath, archiveFileName);
                
                File.Move(logFilePath, archiveFullPath);
                
                _logger.LogInformation($"Fichier de log archivé : {fileName} -> {archiveFileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'archivage du fichier de log : {logFilePath}");
            }
        }
    }
}
