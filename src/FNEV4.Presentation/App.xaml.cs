using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using FNEV4.Infrastructure.Data;
using FNEV4.Infrastructure.Services;
using FNEV4.Infrastructure.Repositories;
using FNEV4.Infrastructure.ExcelProcessing.Services;
using FNEV4.Presentation.ViewModels.Maintenance;
using FNEV4.Presentation.ViewModels.Configuration;
using FNEV4.Presentation.ViewModels.GestionClients;
using FNEV4.Presentation.Services;
using FNEV4.Core.Interfaces;
using FNEV4.Core.Services;
using FNEV4.Application.UseCases.GestionClients;
using FNEV4.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using CoreLogging = FNEV4.Core.Interfaces.ILoggingService;
using InfraLogging = FNEV4.Infrastructure.Services.ILoggingService;

namespace FNEV4.Presentation
{
    /// <summary>
    /// Application principale FNEV4
    /// Gestion du démarrage et configuration globale
    /// </summary>
    public partial class App : System.Windows.Application
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
                
                // Initialiser les dossiers du service centralisé
                await InitializePathConfiguration();
                
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

        private Task InitializePathConfiguration()
        {
            try
            {
                var pathService = ServiceProvider.GetRequiredService<IPathConfigurationService>();
                pathService.EnsureDirectoriesExist();
                System.Diagnostics.Debug.WriteLine("Dossiers initialisés avec succès");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation des dossiers: {ex.Message}");
                return Task.CompletedTask;
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
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // Configuration du service centralisé des chemins en premier
                    services.AddSingleton<IPathConfigurationService, PathConfigurationService>();
                    
                    // Configuration Entity Framework avec le service centralisé
                    services.AddDbContext<FNEV4DbContext>((serviceProvider, options) =>
                    {
                        var pathService = serviceProvider.GetRequiredService<IPathConfigurationService>();
                        var connectionString = $"Data Source={pathService.DatabasePath}";
                        options.UseSqlite(connectionString);
                    }, ServiceLifetime.Transient); // Force une nouvelle instance à chaque injection

                    // Services Infrastructure
                    services.AddScoped<IDatabaseService, DatabaseService>();
                    services.AddScoped<ILoggingConfigurationService, LoggingConfigurationService>();
                    services.AddScoped<InfraLogging, LoggingService>();
                    services.AddScoped<IDiagnosticService, DiagnosticService>();
                    services.AddScoped<IBackupService, BackupService>();

                    // Adaptateur pour ILoggingService (respecte l'architecture Clean)
                    services.AddScoped<FNEV4.Core.Interfaces.ILoggingService>(provider => 
                        new FNEV4.Presentation.Adapters.LoggingServiceAdapter(provider.GetRequiredService<InfraLogging>()));

                    // Repositories
                    services.AddScoped<IClientRepository, ClientRepository>();

                    // Use Cases
                    services.AddScoped<ListeClientsUseCase>();
                    services.AddScoped<AjoutClientUseCase>();
                    services.AddScoped<ModificationClientUseCase>();
                    services.AddScoped<ImportClientsExcelUseCase>();
                    
                    // Services Excel Processing
                    services.AddScoped<IClientExcelImportService, ClientExcelImportService>();
                    
                    // Services externes
                    services.AddSingleton<HttpClient>();
                    services.AddLogging();
                    services.AddScoped<IDgiService, DgiService>();

                    // Services de notification
                    services.AddSingleton<IDatabaseConfigurationNotificationService, DatabaseConfigurationNotificationService>();
                    
                    // Service de chargement de configuration
                    services.AddScoped<IDatabaseConfigurationLoader, DatabaseConfigurationLoader>();

                    // ViewModels avec injection
                    services.AddTransient<BaseDonneesViewModel>();
                    services.AddTransient<LogsDiagnosticsViewModel>();
                    services.AddTransient<EntrepriseConfigViewModel>(provider =>
                        new EntrepriseConfigViewModel(
                            provider.GetService<IDgiService>(),
                            provider.GetRequiredService<IDatabaseService>()));
                    services.AddTransient<ApiFneConfigViewModel>();
                    services.AddTransient<CheminsDossiersConfigViewModel>();
                    
                    // ViewModels Gestion Clients
                    services.AddTransient<ListeClientsViewModel>();
                    services.AddTransient<AjoutModificationClientViewModel>();

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
