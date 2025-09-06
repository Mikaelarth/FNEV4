using FNEV4.Core.Interfaces;
using System;

namespace FNEV4.Application.Extensions
{
    public static class LoggingServiceExtensions
    {
        public static void LogInfo(this ILoggingService loggingService, string message, string category = "General")
        {
            Task.Run(async () => await loggingService.LogInformationAsync(message, category));
        }

        public static void LogError(this ILoggingService loggingService, Exception exception, string message, string category = "General")
        {
            Task.Run(async () => await loggingService.LogErrorAsync(message, exception, category));
        }

        public static void LogError(this ILoggingService loggingService, string message, string category = "General")
        {
            Task.Run(async () => await loggingService.LogErrorAsync(message, null, category));
        }

        public static void LogWarning(this ILoggingService loggingService, string message, string category = "General")
        {
            Task.Run(async () => await loggingService.LogWarningAsync(message, category));
        }

        public static void LogDebug(this ILoggingService loggingService, string message, string category = "General")
        {
            Task.Run(async () => await loggingService.LogDebugAsync(message, category));
        }

        public static void LogCritical(this ILoggingService loggingService, string message, Exception? exception = null, string category = "General")
        {
            Task.Run(async () => await loggingService.LogCriticalAsync(message, exception, category));
        }
    }
}
