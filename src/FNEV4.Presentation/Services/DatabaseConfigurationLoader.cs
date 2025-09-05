using System;
using System.IO;
using System.Threading.Tasks;
using FNEV4.Infrastructure.Services;

namespace FNEV4.Presentation.Services
{
    /// <summary>
    /// Service pour charger et appliquer la configuration de base de données au démarrage
    /// </summary>
    public interface IDatabaseConfigurationLoader
    {
        Task LoadAndApplyConfigurationAsync();
    }

    public class DatabaseConfigurationLoader : IDatabaseConfigurationLoader
    {
        private readonly IDatabaseService _databaseService;

        public DatabaseConfigurationLoader(IDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        public async Task LoadAndApplyConfigurationAsync()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "database-settings.json");
                
                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                    
                    // Charger le chemin de la base de données
                    if (config.TryGetProperty("DatabasePath", out var dbPathElement) && 
                        !string.IsNullOrWhiteSpace(dbPathElement.GetString()))
                    {
                        var dbPath = dbPathElement.GetString()!;
                        
                        // Vérifier que le fichier existe
                        if (File.Exists(dbPath))
                        {
                            // Appliquer le chemin au service de base de données
                            await _databaseService.UpdateConnectionStringAsync(dbPath);
                            
                            System.Diagnostics.Debug.WriteLine($"Configuration chargée - Chemin DB: {dbPath}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Fichier de base de données non trouvé: {dbPath}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Aucun fichier de configuration trouvé");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement de la configuration: {ex.Message}");
                // Ne pas faire planter l'application, continuer avec les paramètres par défaut
            }
        }
    }
}
