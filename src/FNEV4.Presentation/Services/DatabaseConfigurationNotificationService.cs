using System;
using System.Threading.Tasks;

namespace FNEV4.Presentation.Services
{
    /// <summary>
    /// Service de notification pour les changements de configuration de base de données
    /// </summary>
    public interface IDatabaseConfigurationNotificationService
    {
        event Func<Task>? DatabaseConfigurationChanged;
        Task NotifyConfigurationChangedAsync();
    }

    public class DatabaseConfigurationNotificationService : IDatabaseConfigurationNotificationService
    {
        public event Func<Task>? DatabaseConfigurationChanged;

        public async Task NotifyConfigurationChangedAsync()
        {
            try
            {
                if (DatabaseConfigurationChanged != null)
                {
                    await DatabaseConfigurationChanged.Invoke();
                }
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas faire planter l'application
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la notification de changement de configuration: {ex.Message}");
            }
        }
    }
}
