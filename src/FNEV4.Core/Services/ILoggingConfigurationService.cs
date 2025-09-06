using FNEV4.Core.Entities;
using AppLogLevel = FNEV4.Core.Entities.LogLevel;

namespace FNEV4.Core.Services
{
    /// <summary>
    /// Service de configuration du logging
    /// </summary>
    public interface ILoggingConfigurationService
    {
        /// <summary>
        /// Niveau de log minimum configuré
        /// </summary>
        AppLogLevel MinimumLogLevel { get; }

        /// <summary>
        /// Indique si la rotation automatique est activée
        /// </summary>
        bool RotationEnabled { get; }

        /// <summary>
        /// Indique si le logging hybride est activé (DB + fichiers pour Error/Warning, fichiers seuls pour Info/Debug/Trace)
        /// </summary>
        bool HybridLoggingEnabled { get; }

        /// <summary>
        /// Met à jour le niveau de log minimum
        /// </summary>
        /// <param name="level">Nouveau niveau de log</param>
        Task SetMinimumLogLevelAsync(AppLogLevel level);

        /// <summary>
        /// Active ou désactive la rotation automatique des logs
        /// </summary>
        /// <param name="enabled">True pour activer, false pour désactiver</param>
        Task SetRotationEnabledAsync(bool enabled);

        /// <summary>
        /// Active ou désactive le logging hybride
        /// </summary>
        /// <param name="enabled">True pour activer le mode hybride, false pour logging complet en DB</param>
        Task SetHybridLoggingEnabledAsync(bool enabled);

        /// <summary>
        /// Convertit une chaîne de niveau vers l'enum LogLevel
        /// </summary>
        /// <param name="levelString">Niveau sous forme de chaîne</param>
        /// <returns>Niveau LogLevel correspondant</returns>
        AppLogLevel ParseLogLevel(string levelString);

        /// <summary>
        /// Convertit un LogLevel vers sa représentation chaîne
        /// </summary>
        /// <param name="level">Niveau LogLevel</param>
        /// <returns>Représentation chaîne du niveau</returns>
        string FormatLogLevel(AppLogLevel level);

        /// <summary>
        /// Effectue la rotation des fichiers de logs si nécessaire
        /// </summary>
        Task PerformRotationIfNeededAsync();
    }
}
