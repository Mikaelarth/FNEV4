using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FNEV4.Infrastructure.Services;
using FNEV4.Infrastructure.Data;
using FNEV4.Presentation.Services;

namespace FNEV4.Tests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Test de synchronisation de configuration ===\n");

            // Configuration des services
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<AppDbContext>();
                    services.AddScoped<IDatabaseService, DatabaseService>();
                    services.AddSingleton<IDatabaseConfigurationNotificationService, DatabaseConfigurationNotificationService>();
                    services.AddScoped<IDatabaseConfigurationLoader, DatabaseConfigurationLoader>();
                })
                .Build();

            var databaseService = host.Services.GetRequiredService<IDatabaseService>();
            var configLoader = host.Services.GetRequiredService<IDatabaseConfigurationLoader>();

            try
            {
                Console.WriteLine("1. État initial de la base de données :");
                var initialInfo = await databaseService.GetDatabaseInfoAsync();
                Console.WriteLine($"   Chemin actuel : {initialInfo.Path}");
                
                Console.WriteLine("\n2. Chargement de la configuration sauvegardée...");
                await configLoader.LoadAndApplyConfigurationAsync();
                
                Console.WriteLine("\n3. État après chargement de configuration :");
                var updatedInfo = await databaseService.GetDatabaseInfoAsync();
                Console.WriteLine($"   Nouveau chemin : {updatedInfo.Path}");
                
                if (initialInfo.Path != updatedInfo.Path)
                {
                    Console.WriteLine("✅ SUCCESS: La configuration a été appliquée correctement !");
                }
                else
                {
                    Console.WriteLine("⚠️  WARNING: Aucun changement détecté dans la configuration");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
            
            Console.WriteLine("\nAppuyez sur une touche pour continuer...");
            Console.ReadKey();
        }
    }
}
