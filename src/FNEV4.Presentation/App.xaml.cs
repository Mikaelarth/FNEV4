using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using FNEV4.Infrastructure.Data;
using FNEV4.Infrastructure.Services;
using FNEV4.Infrastructure.Services.ImportTraitement;
using FNEV4.Infrastructure.Repositories;
using FNEV4.Infrastructure.ExcelProcessing.Services;
using FNEV4.Presentation.ViewModels.Maintenance;
using FNEV4.Presentation.ViewModels.Configuration;
using FNEV4.Presentation.ViewModels.GestionClients;
using FNEV4.Presentation.ViewModels.ImportTraitement;
using FNEV4.Presentation.Services;
using FNEV4.Core.Interfaces;
using FNEV4.Core.Services;
using FNEV4.Application.UseCases.GestionClients;
using FNEV4.Application.Services.ImportTraitement;
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
                
                // NOUVEAU: Initialiser et migrer la base de données unique
                await InitializeCentralizedDatabase();
                
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

        private async Task InitializeCentralizedDatabase()
        {
            try
            {
                // S'assurer que le répertoire de la base existe
                var databasePathProvider = ServiceProvider.GetRequiredService<IDatabasePathProvider>();
                databasePathProvider.EnsureDatabaseDirectoryExists();
                
                // Appliquer les migrations et initialiser la base
                using (var scope = ServiceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<FNEV4DbContext>();
                    
                    // Créer la base si elle n'existe pas et appliquer les migrations
                    await context.Database.EnsureCreatedAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"Base de données centralisée initialisée: {databasePathProvider.DatabasePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation de la base de données: {ex.Message}");
                throw; // Faire planter si la base de données ne peut pas être initialisée
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
                    // Configuration centralisée des chemins et base de données
                    services.AddSingleton<IDatabasePathProvider, DatabasePathProvider>();
                    services.AddSingleton<IPathConfigurationService>(provider =>
                        new PathConfigurationService(
                            provider.GetRequiredService<IConfiguration>(),
                            provider.GetRequiredService<IDatabasePathProvider>()));
                    
                    // Configuration Entity Framework avec SINGLETON pour éviter les bases multiples
                    services.AddDbContext<FNEV4DbContext>((serviceProvider, options) =>
                    {
                        var databasePathProvider = serviceProvider.GetRequiredService<IDatabasePathProvider>();
                        var connectionString = databasePathProvider.GetConnectionString();
                        options.UseSqlite(connectionString, sqliteOptions =>
                        {
                            // Configuration SQLite pour de meilleures performances
                            sqliteOptions.CommandTimeout(30);
                        });
                        
                        // Optimisations de performance Entity Framework
                        options.EnableSensitiveDataLogging(false);
                        options.EnableServiceProviderCaching(true);
                        options.EnableDetailedErrors(false);
                        
                        // Log pour diagnostic
                        System.Diagnostics.Debug.WriteLine($"[DbContext] Connexion DB: {connectionString}");
                    }, ServiceLifetime.Scoped); // CHANGÉ: Scoped au lieu de Transient

                    // Services Infrastructure
                    services.AddScoped<IDatabaseService, DatabaseService>();
                    services.AddScoped<ILoggingConfigurationService, LoggingConfigurationService>();
                    services.AddScoped<InfraLogging, LoggingService>();
                    services.AddScoped<IDiagnosticService, DiagnosticService>();
                    services.AddScoped<IBackupService, BackupService>();
                    
                    // Services Import Traitement
                    services.AddScoped<ISage100ImportService>(provider => 
                        new Sage100ImportService(
                            provider.GetRequiredService<IClientRepository>(),
                            provider.GetRequiredService<FNEV4DbContext>(),
                            provider.GetRequiredService<InfraLogging>()));

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
                    
                    // Import Exceptionnel (système temporaire)
                    services.AddScoped<FNEV4.Application.Special.ImportSpecialExcelUseCase>();
                    
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
                    services.AddTransient<BaseDonneesViewModel>(provider =>
                        new BaseDonneesViewModel(
                            provider.GetRequiredService<IDatabaseService>(),
                            provider.GetService<IDatabaseConfigurationNotificationService>(),
                            provider.GetRequiredService<IPathConfigurationService>(),
                            provider));
                    services.AddTransient<LogsDiagnosticsViewModel>();
                    services.AddTransient<DatabaseSettingsViewModel>(provider =>
                        new DatabaseSettingsViewModel(
                            provider.GetRequiredService<IDatabaseService>(),
                            provider.GetService<IDatabaseConfigurationNotificationService>()));
                    services.AddTransient<EntrepriseConfigViewModel>(provider =>
                        new EntrepriseConfigViewModel(
                            provider.GetService<IDgiService>(),
                            provider.GetRequiredService<IDatabaseService>(),
                            provider.GetRequiredService<FNEV4DbContext>()));
                    services.AddTransient<ApiFneConfigViewModel>(provider =>
                        new ApiFneConfigViewModel(
                            provider.GetRequiredService<IDatabaseService>(),
                            provider.GetRequiredService<FNEV4DbContext>()));
                    services.AddTransient<CheminsDossiersConfigViewModel>();
                    
                    // ViewModels Gestion Clients
                    services.AddTransient<ListeClientsViewModel>();
                    services.AddTransient<AjoutModificationClientViewModel>();
                    services.AddTransient<ImportClientsViewModel>();
                    
                    // ViewModels Import & Traitement
                    services.AddTransient<FNEV4.Presentation.ViewModels.ImportTraitement.ImportFichiersViewModel>();

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
