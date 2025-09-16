using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Interfaces.Services.Fne;
using FNEV4.Core.Interfaces;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace FNEV4.Presentation.ViewModels.CertificationFne
{
    /// <summary>
    /// ViewModel principal pour la section Certification FNE
    /// Gère la navigation entre les différentes vues de certification
    /// </summary>
    public partial class CertificationMainViewModel : ObservableObject
    {
        #region Services injectés
        
        private readonly IFneCertificationService _certificationService;
        private readonly ILoggingService _loggingService;
        
        #endregion

        #region Propriétés observables

        [ObservableProperty]
        private string _welcomeMessage = "Bienvenue dans la section Certification FNE";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _activeView = "Dashboard";

        [ObservableProperty]
        private bool _isSystemReady = false;

        [ObservableProperty]
        private string _systemStatusMessage = "Initialisation...";

        [ObservableProperty]
        private Brush _systemStatusColor = Brushes.Orange;

        [ObservableProperty]
        private string _consolidatedStatus = "Vérification en cours...";

        [ObservableProperty]
        private int _totalPendingInvoices = 0;

        [ObservableProperty]
        private bool _isAnyOperationRunning = false;

        // ViewModels des sous-vues
        [ObservableProperty]
        private CertificationDashboardViewModel? _dashboardViewModel;

        [ObservableProperty]
        private CertificationManuelleViewModel? _manualCertificationViewModel;

        [ObservableProperty]
        private CertificationAutomatiqueViewModel? _automatiqueViewModel;

        public ObservableCollection<NavigationItem> NavigationItems { get; }

        #endregion

        #region Constructeur

        public CertificationMainViewModel(IFneCertificationService certificationService, ILoggingService loggingService)
        {
            _certificationService = certificationService ?? throw new ArgumentNullException(nameof(certificationService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            
            // Initialiser les éléments de navigation
            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem 
                { 
                    Name = "Dashboard", 
                    Description = "Vue d'ensemble et statistiques", 
                    Icon = "ViewDashboard",
                    IsActive = true 
                },
                new NavigationItem 
                { 
                    Name = "Manual", 
                    Description = "Certification manuelle des factures", 
                    Icon = "FileDocument"
                },
                new NavigationItem 
                { 
                    Name = "Auto", 
                    Description = "Certification automatique", 
                    Icon = "AutoFix"
                },
                new NavigationItem 
                { 
                    Name = "Configuration", 
                    Description = "Paramètres de certification", 
                    Icon = "Cog"
                },
                new NavigationItem 
                { 
                    Name = "Logs", 
                    Description = "Journal d'activités", 
                    Icon = "TextBoxSearch"
                }
            };

            InitializeAsync();
        }

        #endregion

        #region Méthodes d'initialisation

        private async void InitializeAsync()
        {
            try
            {
                IsLoading = true;
                await LogInfoAsync("Initialisation de la vue principale Certification FNE");
                
                // Configuration initiale
                await LoadInitialDataAsync();
                await InitializeSubViewModelsAsync();
                
                await LogInfoAsync("Vue principale Certification FNE initialisée avec succès");
            }
            catch (Exception ex)
            {
                await LogErrorAsync($"Erreur lors de l'initialisation de la vue principale Certification FNE: {ex.Message}");
                WelcomeMessage = "Erreur lors du chargement de la certification FNE";
                SystemStatusMessage = "Erreur d'initialisation";
                SystemStatusColor = Brushes.Red;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadInitialDataAsync()
        {
            await Task.Delay(100); // Simulation de chargement
            WelcomeMessage = "Certification FNE - Prêt à utiliser";
            
            // Vérifier le statut du système
            await CheckSystemStatusAsync();
        }

        private async Task InitializeSubViewModelsAsync()
        {
            try
            {
                // Créer les ViewModels des sous-vues avec injection de dépendances
                var serviceProvider = App.ServiceProvider;
                
                DashboardViewModel = serviceProvider.GetService<CertificationDashboardViewModel>();
                ManualCertificationViewModel = serviceProvider.GetService<CertificationManuelleViewModel>();
                AutomatiqueViewModel = serviceProvider.GetService<CertificationAutomatiqueViewModel>();
                
                await LogInfoAsync("Sous-ViewModels initialisés");
            }
            catch (Exception ex)
            {
                await LogErrorAsync($"Erreur initialisation sous-ViewModels: {ex.Message}");
            }
        }

        private async Task CheckSystemStatusAsync()
        {
            try
            {
                IsAnyOperationRunning = true;
                SystemStatusMessage = "Vérification du statut du système...";
                
                // Vérifier la configuration FNE
                var hasConfig = await CheckFneConfigurationAsync();
                
                // Compter les factures en attente
                await UpdatePendingInvoicesCountAsync();
                
                if (hasConfig)
                {
                    IsSystemReady = true;
                    SystemStatusColor = Brushes.Green;
                    ConsolidatedStatus = "Système opérationnel";
                    SystemStatusMessage = "Système prêt";
                }
                else
                {
                    IsSystemReady = false;
                    SystemStatusColor = Brushes.Orange;
                    ConsolidatedStatus = "Configuration FNE requise";
                    SystemStatusMessage = "Configuration manquante";
                }
            }
            catch (Exception ex)
            {
                IsSystemReady = false;
                SystemStatusColor = Brushes.Red;
                ConsolidatedStatus = "Erreur système";
                SystemStatusMessage = "Erreur de vérification";
                await LogErrorAsync($"Erreur vérification statut système: {ex.Message}");
            }
            finally
            {
                IsAnyOperationRunning = false;
            }
        }

        private async Task<bool> CheckFneConfigurationAsync()
        {
            try
            {
                var dbContext = App.ServiceProvider.GetRequiredService<FNEV4.Infrastructure.Data.FNEV4DbContext>();
                var activeConfig = await dbContext.FneConfigurations
                    .Where(c => c.IsActive && !c.IsDeleted)
                    .AnyAsync();
                return activeConfig;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task UpdatePendingInvoicesCountAsync()
        {
            try
            {
                var invoiceRepository = App.ServiceProvider.GetRequiredService<IInvoiceRepository>();
                var allInvoices = await invoiceRepository.GetAllAsync();
                TotalPendingInvoices = allInvoices.Count(i => i.Status == "Draft" || i.Status == "Validated");
            }
            catch (Exception ex)
            {
                await LogErrorAsync($"Erreur comptage factures en attente: {ex.Message}");
            }
        }

        #endregion

        #region Commandes

        [RelayCommand]
        private async Task NavigateToView(string viewName)
        {
            if (string.IsNullOrEmpty(viewName)) return;

            try
            {
                await LogInfoAsync($"Navigation vers la vue: {viewName}");
                
                // Mettre à jour l'élément actif dans la navigation
                foreach (var item in NavigationItems)
                {
                    item.IsActive = item.Name == viewName;
                }

                ActiveView = viewName;
                await LogInfoAsync($"Navigation vers {viewName} terminée");
            }
            catch (Exception ex)
            {
                await LogErrorAsync($"Erreur navigation vers {viewName}: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task RefreshSystemStatus()
        {
            await CheckSystemStatusAsync();
        }

        [RelayCommand]
        private async Task TestSystemHealth()
        {
            try
            {
                await LogInfoAsync("Test de santé du système démarré");
                SystemStatusMessage = "Test en cours...";
                IsAnyOperationRunning = true;

                // Simuler des tests
                await Task.Delay(2000);
                
                var isHealthy = await CheckFneConfigurationAsync();
                
                if (isHealthy)
                {
                    SystemStatusMessage = "Test réussi";
                    await LogInfoAsync("Test de santé réussi");
                }
                else
                {
                    SystemStatusMessage = "Test échoué";
                    await LogErrorAsync("Test de santé échoué");
                }
            }
            catch (Exception ex)
            {
                SystemStatusMessage = "Erreur de test";
                await LogErrorAsync($"Erreur test de santé: {ex.Message}");
            }
            finally
            {
                IsAnyOperationRunning = false;
            }
        }

        [RelayCommand]
        private async Task QuickCertifyPending()
        {
            if (!IsSystemReady)
            {
                MessageBox.Show("Le système n'est pas prêt pour la certification.", 
                    "Système non prêt", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await LogInfoAsync("Certification rapide des factures en attente démarrée");
            
            // Basculer vers la vue de certification manuelle
            await NavigateToView("Manual");
        }

        #endregion

        #region Méthodes de logging centralisé

        private async Task LogInfoAsync(string message)
        {
            await _loggingService.LogInformationAsync("CertificationMain", message);
        }

        private async Task LogErrorAsync(string message)
        {
            await _loggingService.LogErrorAsync("CertificationMain", null, message);
        }

        private async Task LogDebugAsync(string message)
        {
            await _loggingService.LogDebugAsync("CertificationMain", message);
        }

        #endregion
    }

    /// <summary>
    /// Élément de navigation pour le menu latéral
    /// </summary>
    public partial class NavigationItem : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _icon = string.Empty;

        [ObservableProperty]
        private bool _isActive = false;

        [ObservableProperty]
        private bool _showBadge = false;

        [ObservableProperty]
        private int _badgeCount = 0;
    }
}