using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FNEV4.Core.Interfaces;
using InfraLogging = FNEV4.Infrastructure.Services.ILoggingService;

namespace FNEV4.Presentation.Adapters
{
    /// <summary>
    /// Adaptateur permettant d'utiliser le service de logging de l'Infrastructure
    /// dans les couches qui dépendent de l'interface Core
    /// </summary>
    public class LoggingServiceAdapter : FNEV4.Core.Interfaces.ILoggingService
    {
        private readonly InfraLogging _infrastructureLoggingService;

        public LoggingServiceAdapter(InfraLogging infrastructureLoggingService)
        {
            _infrastructureLoggingService = infrastructureLoggingService ?? throw new ArgumentNullException(nameof(infrastructureLoggingService));
        }

        public async Task LogInformationAsync(string message, string category = "General")
        {
            await _infrastructureLoggingService.LogInfoAsync(message, category);
        }

        public async Task LogWarningAsync(string message, string category = "General")
        {
            await _infrastructureLoggingService.LogWarningAsync(message, category);
        }

        public async Task LogErrorAsync(string message, Exception? exception = null, string category = "General")
        {
            await _infrastructureLoggingService.LogErrorAsync(message, category, exception);
        }

        public async Task LogDebugAsync(string message, string category = "General")
        {
            await _infrastructureLoggingService.LogDebugAsync(message, category);
        }

        public async Task LogCriticalAsync(string message, Exception? exception = null, string category = "General")
        {
            // Infrastructure n'a pas de LogCritical, on utilise LogError avec marqueur CRITICAL
            await _infrastructureLoggingService.LogErrorAsync($"CRITICAL: {message}", category, exception);
        }

        public string GetLatestLogFile(string logsFolderPath)
        {
            // Cette méthode n'existe pas dans Infrastructure, on retourne un chemin par défaut
            return Path.Combine(logsFolderPath, $"fnev4-{DateTime.Now:yyyyMMdd}.log");
        }

        public async Task<long> CleanOldLogsAsync(string logsFolderPath, int retentionDays)
        {
            // Simulation du nettoyage - Infrastructure utilise ClearOldLogsAsync
            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            await _infrastructureLoggingService.ClearOldLogsAsync(cutoffDate);
            return 0; // Retourne 0 car on ne peut pas calculer la taille libérée
        }

        public async Task<int> ArchiveOldLogsAsync(string logsFolderPath, string archiveFolderPath)
        {
            // Cette fonctionnalité n'existe pas dans Infrastructure
            await Task.CompletedTask;
            return 0;
        }

        public async Task<FNEV4.Core.Interfaces.LogStatistics> GetLogStatisticsAsync(string logsFolderPath)
        {
            var infraStats = await _infrastructureLoggingService.GetLogStatisticsAsync();
            
            // Mapping des statistiques
            return new FNEV4.Core.Interfaces.LogStatistics
            {
                TotalLogFiles = 1, // Infrastructure ne track pas le nombre de fichiers
                TotalSize = 0, // Information non disponible
                OldestLog = infraStats.OldestEntryTime,
                NewestLog = infraStats.LastEntryTime,
                LogsByLevel = new Dictionary<string, int>
                {
                    ["Debug"] = infraStats.DebugCount,
                    ["Information"] = infraStats.InfoCount,
                    ["Warning"] = infraStats.WarningCount,
                    ["Error"] = infraStats.ErrorCount,
                    ["Critical"] = infraStats.CriticalCount
                },
                LogsByCategory = new Dictionary<string, int>(),
                LogsByDate = new Dictionary<DateTime, int>()
            };
        }

        public async Task SetLogLevelAsync(FNEV4.Core.Interfaces.LogLevel logLevel)
        {
            // Cette fonctionnalité n'existe pas dans Infrastructure
            await Task.CompletedTask;
        }

        public async Task ConfigureLogRotationAsync(bool enabled, long maxFileSize = 10485760, int maxFiles = 10)
        {
            // Cette fonctionnalité n'existe pas dans Infrastructure
            await Task.CompletedTask;
        }

        public async Task ConfigureLogFormatAsync(bool includeTimestamp = true, bool includeLevel = true, bool includeCategory = true)
        {
            // Cette fonctionnalité n'existe pas dans Infrastructure
            await Task.CompletedTask;
        }
    }
}