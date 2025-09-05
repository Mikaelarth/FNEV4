using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using FNEV4.Infrastructure.Data;
using FNEV4.Infrastructure.Services;
using FNEV4.Presentation.ViewModels.Maintenance;
using FNEV4.Presentation.Services;

namespace FNEV4.Presentation
{
    /// <summary>
    /// Application principale FNEV4
    /// Gestion du démarrage et configuration globale
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                System.Diagnostics.Debug.WriteLine("Démarrage de l'application...");
                
                // Configuration de l'hôte avec DI
                _host = CreateHostBuilder().Build();
                await _host.StartAsync();
                
                // Exposer le ServiceProvider globalement
                ServiceProvider = _host.Services;
                
                System.Diagnostics.Debug.WriteLine("ServiceProvider configuré avec succès");
                
                // Test de l'injection de dépendances
                TestDependencyInjection();
                
                // Configuration Material Design
                ConfigureMaterialDesign();
                
                // Configuration de l'application
                ConfigureApplication();
                
                // Charger et appliquer la configuration de base de données sauvegardée
                await LoadDatabaseConfiguration();
                
                System.Diagnostics.Debug.WriteLine("Application démarrée avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du démarrage: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task LoadDatabaseConfiguration()
        {
            try
            {
                var configLoader = ServiceProvider.GetRequiredService<IDatabaseConfigurationLoader>();
                await configLoader.LoadAndApplyConfigurationAsync();
                System.Diagnostics.Debug.WriteLine("Configuration de base de données chargée avec succès");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement de la configuration: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }

        private IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Configuration Entity Framework
                    services.AddDbContext<FNEV4DbContext>(options =>
                        options.UseSqlite("Data Source=Data/FNEV4.db"));

                    // Services Infrastructure
                    services.AddScoped<IDatabaseService, DatabaseService>();
                    services.AddScoped<ILoggingService, LoggingService>();
                    services.AddScoped<IDiagnosticService, DiagnosticService>();

                    // Services de notification
                    services.AddSingleton<IDatabaseConfigurationNotificationService, DatabaseConfigurationNotificationService>();
                    
                    // Service de chargement de configuration
                    services.AddScoped<IDatabaseConfigurationLoader, DatabaseConfigurationLoader>();

                    // ViewModels avec injection
                    services.AddTransient<BaseDonneesViewModel>();
                    services.AddTransient<LogsDiagnosticsViewModel>();

                    // Service locator pour les ViewModels
                    services.AddSingleton<ViewModelLocator>();

                    // Services Application (à ajouter plus tard)
                });
        }

        private void ConfigureMaterialDesign()
        {
            // Configuration du thème Material Design
            // Couleurs DGI : Bleu principal, Orange secondaire
        }

        private void ConfigureApplication()
        {
            // Configuration générale de l'application
            // Logging, DI, etc.
        }

        private void TestDependencyInjection()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Test de l'injection de dépendances...");
                
                // Test du DatabaseService
                var dbService = ServiceProvider.GetService<IDatabaseService>();
                System.Diagnostics.Debug.WriteLine($"DatabaseService: {(dbService != null ? "OK" : "ECHEC")}");
                
                // Test du ViewModelLocator
                var viewModelLocator = ServiceProvider.GetService<ViewModelLocator>();
                System.Diagnostics.Debug.WriteLine($"ViewModelLocator: {(viewModelLocator != null ? "OK" : "ECHEC")}");
                
                // Test du ViewModel
                var viewModel = ServiceProvider.GetService<BaseDonneesViewModel>();
                System.Diagnostics.Debug.WriteLine($"BaseDonneesViewModel: {(viewModel != null ? "OK" : "ECHEC")}");
                
                System.Diagnostics.Debug.WriteLine("Test d'injection terminé");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du test DI: {ex.Message}");
            }
        }

        /// <summary>
        /// Méthode statique pour accéder aux services enregistrés
        /// </summary>
        /// <typeparam name="T">Type du service</typeparam>
        /// <returns>Instance du service</returns>
        public static T GetService<T>() where T : class
        {
            return ServiceProvider.GetRequiredService<T>();
        }
    }
}
