using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using FNEV4.Core.Interfaces;
using FNEV4.Infrastructure.Data;
using FNEV4.Infrastructure.Services;
using FNEV4.Presentation.Views.Maintenance;
using FNEV4.Presentation.ViewModels.GestionFactures;
using FNEV4.Presentation.Views.GestionFactures;

namespace FNEV4.Presentation.ViewModels
{
    /// <summary>
    /// ViewModel principal pour la fenêtre principale
    /// Gère la navigation entre les modules et l'état global
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        #region Services
        
        private readonly IDatabaseService? _databaseService;
        private readonly FNEV4DbContext? _dbContext;
        private readonly DispatcherTimer _statusRefreshTimer;
        
        #endregion
        #region Events
        
        /// <summary>
        /// Événement déclenché quand tous les sous-menus doivent être fermés
        /// </summary>
        public event Action? CloseSubMenusRequested;
        
        #endregion
        #region Properties

        [ObservableProperty]
        private string _currentModuleName = "Dashboard";

        [ObservableProperty]
        private UserControl? _currentView;

        [ObservableProperty]
        private string _applicationTitle = "FNEV4 - Application FNE Desktop";

        [ObservableProperty]
        private string _applicationVersion = "v1.0.0";

        [ObservableProperty]
        private string _companyName = "Non configuré";

        [ObservableProperty]
        private string _connectionStatus = "Déconnecté";

        [ObservableProperty]
        private string _stickerBalance = "0";

        [ObservableProperty]
        private string _currentEnvironment = "Non configuré";

        [ObservableProperty]
        private string _configurationStatus = "En attente";

        [ObservableProperty]
        private string _databaseStatus = "Non connecté";

        [ObservableProperty]
        private bool _isMenuExpanded = true;

        [ObservableProperty]
        private bool _areSectionExpanded = false;

        #endregion

        #region Commands

        /// <summary>
        /// Commandes pour naviguer vers le Dashboard
        /// </summary>
        [RelayCommand]
        private void NavigateToDashboard()
        {
            CurrentModuleName = "Dashboard - Vue d'ensemble";
            
            // Créer le ViewModel et la vue
            var dashboardViewModel = App.ServiceProvider.GetRequiredService<ViewModels.Dashboard.DashboardVueEnsembleViewModel>();
            var dashboardView = new Views.Dashboard.DashboardVueEnsembleView(dashboardViewModel);
            CurrentView = dashboardView;
        }

        [RelayCommand]
        private void NavigateToDashboardActions()
        {
            CurrentModuleName = "Dashboard - Actions rapides";
            
            // Créer le ViewModel et la vue pour Actions rapides
            var actionsViewModel = App.ServiceProvider.GetRequiredService<ViewModels.Dashboard.DashboardActionsRapidesViewModel>();
            var actionsView = new Views.Dashboard.DashboardActionsRapidesView(actionsViewModel);
            CurrentView = actionsView;
        }

        /// <summary>
        /// Commandes pour naviguer vers Import & Traitement
        /// </summary>
        [RelayCommand]
        private void NavigateToImportFichiers()
        {
            CurrentModuleName = "Import - Import de fichiers";
            
            // Créer le ViewModel et la vue
            var importViewModel = App.ServiceProvider.GetRequiredService<ViewModels.ImportTraitement.ImportFichiersViewModel>();
            var importView = new Views.ImportTraitement.ImportFichiersView(importViewModel);
            CurrentView = importView;
        }

        [RelayCommand]
        private void NavigateToParsingValidation()
        {
            CurrentModuleName = "Import - Parsing & Validation";
        }

        [RelayCommand]
        private void NavigateToHistoriqueImports()
        {
            CurrentModuleName = "Import - Historique des imports";
        }

        /// <summary>
        /// Commandes pour naviguer vers Gestion des Factures
        /// </summary>
        [RelayCommand]
        private void NavigateToListeFactures()
        {
            CurrentModuleName = "Factures - Liste des factures";
            
            // Créer le ViewModel et la vue
            var facturesViewModel = App.ServiceProvider.GetRequiredService<ViewModels.GestionFactures.FacturesListViewModel>();
            var facturesView = new Views.GestionFactures.FacturesListView { DataContext = facturesViewModel };
            CurrentView = facturesView;
        }

        [RelayCommand]
        private void NavigateToEditionFactures()
        {
            CurrentModuleName = "Factures - Édition de factures";
        }

        [RelayCommand]
        private void NavigateToDetailsFacture()
        {
            CurrentModuleName = "Factures - Détails de facture";
        }

        [RelayCommand]
        private void NavigateToFacturesAvoir()
        {
            CurrentModuleName = "Factures - Factures d'avoir";
        }

        /// <summary>
        /// Commandes pour naviguer vers Certification FNE
        /// </summary>
        [RelayCommand]
        private void NavigateToCertificationManuelle()
        {
            CurrentModuleName = "Certification - Certification manuelle";
        }

        [RelayCommand]
        private void NavigateToCertificationAutomatique()
        {
            CurrentModuleName = "Certification - Certification automatique";
        }

        [RelayCommand]
        private void NavigateToSuiviCertifications()
        {
            CurrentModuleName = "Certification - Suivi des certifications";
        }

        [RelayCommand]
        private void NavigateToRetryReprises()
        {
            CurrentModuleName = "Certification - Retry & Reprises";
        }

        /// <summary>
        /// Commandes pour naviguer vers Gestion Clients
        /// </summary>
        [RelayCommand]
        private void NavigateToListeClients()
        {
            CurrentModuleName = "Clients - Liste des clients";
            CurrentView = new Views.GestionClients.ListeClientsView();
        }

        [RelayCommand]
        private void NavigateToAjoutModificationClient()
        {
            CurrentModuleName = "Clients - Ajout/Modification";
        }

        [RelayCommand]
        private void NavigateToRechercheAvancee()
        {
            CurrentModuleName = "Clients - Recherche avancée";
        }

        /// <summary>
        /// Commandes pour naviguer vers Rapports & Analyses
        /// </summary>
        [RelayCommand]
        private void NavigateToRapportsStandards()
        {
            CurrentModuleName = "Rapports - Rapports standards";
        }

        [RelayCommand]
        private void NavigateToRapportsFne()
        {
            CurrentModuleName = "Rapports - Rapports FNE";
        }

        [RelayCommand]
        private void NavigateToAnalysesPersonnalisees()
        {
            CurrentModuleName = "Rapports - Analyses personnalisées";
        }

        /// <summary>
        /// Commandes pour naviguer vers Configuration
        /// </summary>
        [RelayCommand]
        private void NavigateToEntrepriseConfig()
        {
            CurrentModuleName = "Configuration - Entreprise";
            CurrentView = new Views.Configuration.EntrepriseConfigView();
        }

        [RelayCommand]
        private void NavigateToApiFneConfig()
        {
            CurrentModuleName = "Configuration - API FNE";
            CurrentView = new Views.Configuration.ApiFneConfigView();
        }

        [RelayCommand]
        private void NavigateToCheminsDossiers()
        {
            CurrentModuleName = "Configuration - Chemins & Dossiers";
            CurrentView = new Views.Configuration.CheminsDossiersConfigView();
        }

        [RelayCommand]
        private void NavigateToInterfaceUtilisateur()
        {
            CurrentModuleName = "Configuration - Interface utilisateur";
        }

        [RelayCommand]
        private void NavigateToPerformances()
        {
            CurrentModuleName = "Configuration - Performances";
        }

        /// <summary>
        /// Commandes pour naviguer vers Maintenance
        /// </summary>
        [RelayCommand]
        private void NavigateToLogsDiagnostics()
        {
            CurrentModuleName = "Maintenance - Logs & Diagnostics";
            CurrentView = new LogsDiagnosticsView();
        }

        [RelayCommand]
        private void NavigateToBaseDonnees()
        {
            CurrentModuleName = "Maintenance - Base de données";
            CurrentView = new BaseDonneesView();
        }

        [RelayCommand]
        private void NavigateToSynchronisation()
        {
            CurrentModuleName = "Maintenance - Synchronisation";
            // TODO: CurrentView = new SynchronisationView();
        }

        [RelayCommand]
        private void NavigateToOutilsTechniques()
        {
            CurrentModuleName = "Maintenance - Outils techniques";
            // TODO: CurrentView = new OutilsTechniquesView();
        }

        /// <summary>
        /// Commandes pour naviguer vers Aide & Support
        /// </summary>
        [RelayCommand]
        private void NavigateToDocumentation()
        {
            CurrentModuleName = "Aide - Documentation";
        }

        [RelayCommand]
        private void NavigateToSupport()
        {
            CurrentModuleName = "Aide - Support";
        }

        [RelayCommand]
        private void NavigateToAPropos()
        {
            CurrentModuleName = "Aide - À propos";
        }

        /// <summary>
        /// Commande pour basculer l'expansion du menu
        /// </summary>
        [RelayCommand]
        private void ToggleMenu()
        {
            IsMenuExpanded = !IsMenuExpanded;
            
            // Quand le menu se replie, fermer tous les sous-menus
            if (!IsMenuExpanded)
            {
                AreSectionExpanded = false;
                CloseSubMenusRequested?.Invoke();
            }
        }

        /// <summary>
        /// Commande pour rafraîchir l'état de connexion
        /// </summary>
        [RelayCommand]
        private async Task RefreshConnectionStatus()
        {
            ConnectionStatus = "Vérification...";
            
            try
            {
                // Vérifier l'état de la base de données
                await CheckDatabaseStatus();
                
                // Vérifier la configuration de l'entreprise
                await CheckCompanyConfiguration();
                
                // Vérifier l'environnement actuel
                await CheckCurrentEnvironment();
                
                // TODO: Vérifier la connexion API FNE quand le module sera implémenté
                ConnectionStatus = DatabaseStatus == "Connecté" ? "Prêt" : "Problème détecté";
                
                // TODO: Récupérer le vrai solde de vignettes depuis l'API
                StickerBalance = "Non disponible";
            }
            catch (Exception ex)
            {
                ConnectionStatus = "Erreur";
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification du statut: {ex.Message}");
            }
        }

        #endregion

        #region Constructor

        public MainViewModel()
        {
            // Récupérer les services via DI
            _databaseService = App.ServiceProvider?.GetService<IDatabaseService>();
            _dbContext = App.ServiceProvider?.GetService<FNEV4DbContext>();
            
            // Initialisation par défaut
            CurrentModuleName = "Dashboard - Vue d'ensemble";
            
            // Créer et configurer le timer pour le rafraîchissement automatique du statut
            _statusRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30) // Rafraîchir toutes les 30 secondes
            };
            _statusRefreshTimer.Tick += async (s, e) => await RefreshConnectionStatus();
            
            // Charger le Dashboard par défaut au démarrage
            LoadDefaultDashboard();
            
            // Initialiser le statut au démarrage
            _ = Task.Run(async () => await InitializeAsync());
            
            // Démarrer le timer de rafraîchissement
            _statusRefreshTimer.Start();
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            // Charger le statut de la configuration de l'entreprise
            await CheckCompanyConfiguration();
            
            // Vérifier l'état de connexion de la base de données
            await CheckDatabaseStatus();
            
            // Vérifier l'environnement actuel
            await CheckCurrentEnvironment();
            
            // Rafraîchir l'état de connexion général
            await RefreshConnectionStatus();
        }

        /// <summary>
        /// Vérifie l'état de la base de données
        /// </summary>
        private async Task CheckDatabaseStatus()
        {
            try
            {
                if (_databaseService != null)
                {
                    var dbInfo = await _databaseService.GetDatabaseInfoAsync();
                    DatabaseStatus = dbInfo?.ConnectionStatus == "Connectée" ? "Connecté" : "Déconnecté";
                }
                else
                {
                    DatabaseStatus = "Service non disponible";
                }
            }
            catch (Exception ex)
            {
                DatabaseStatus = "Erreur";
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification de la base de données: {ex.Message}");
            }
        }

        /// <summary>
        /// Vérifie la configuration de l'entreprise
        /// </summary>
        private async Task CheckCompanyConfiguration()
        {
            try
            {
                if (_dbContext != null)
                {
                    var activeCompany = _dbContext.Companies?.FirstOrDefault(c => c.IsActive);
                    
                    if (activeCompany != null)
                    {
                        CompanyName = activeCompany.CompanyName ?? "Entreprise sans nom";
                        ConfigurationStatus = "Configuré";
                        
                        // Déterminer l'environnement depuis la configuration
                        CurrentEnvironment = !string.IsNullOrEmpty(activeCompany.Environment) 
                            ? activeCompany.Environment 
                            : "Non spécifié";
                    }
                    else
                    {
                        CompanyName = "Non configuré";
                        ConfigurationStatus = "À configurer";
                        CurrentEnvironment = "Non configuré";
                    }
                }
                else
                {
                    CompanyName = "Service non disponible";
                    ConfigurationStatus = "Erreur";
                }
            }
            catch (Exception ex)
            {
                CompanyName = "Erreur de chargement";
                ConfigurationStatus = "Erreur";
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification de la configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Vérifie l'environnement de travail actuel
        /// </summary>
        private async Task CheckCurrentEnvironment()
        {
            try
            {
                if (_dbContext != null)
                {
                    var activeCompany = _dbContext.Companies?.FirstOrDefault(c => c.IsActive);
                    
                    if (activeCompany != null && !string.IsNullOrEmpty(activeCompany.Environment))
                    {
                        CurrentEnvironment = activeCompany.Environment switch
                        {
                            "Production" => "🟢 Production",
                            "Test" => "🟡 Test",
                            "Development" => "🔵 Développement",
                            _ => activeCompany.Environment
                        };
                    }
                    else
                    {
                        CurrentEnvironment = "⚪ Non configuré";
                    }
                }
            }
            catch (Exception ex)
            {
                CurrentEnvironment = "❌ Erreur";
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification de l'environnement: {ex.Message}");
            }
        }

        /// <summary>
        /// Charge le Dashboard par défaut au démarrage de l'application
        /// </summary>
        private void LoadDefaultDashboard()
        {
            try
            {
                // Créer le ViewModel et la vue par défaut
                var dashboardViewModel = App.ServiceProvider.GetRequiredService<ViewModels.Dashboard.DashboardVueEnsembleViewModel>();
                var dashboardView = new Views.Dashboard.DashboardVueEnsembleView(dashboardViewModel);
                CurrentView = dashboardView;
            }
            catch (Exception ex)
            {
                // Log de l'erreur en cas de problème
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement du Dashboard par défaut: {ex.Message}");
                // Ne pas faire planter l'application, l'utilisateur pourra naviguer manuellement
            }
        }

        /// <summary>
        /// Nettoyage des ressources
        /// </summary>
        public void Dispose()
        {
            _statusRefreshTimer?.Stop();
        }

        #endregion
    }
}
