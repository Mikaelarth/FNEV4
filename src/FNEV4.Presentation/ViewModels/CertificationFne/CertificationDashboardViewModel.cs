using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces.Services.Fne;
using FNEV4.Core.Interfaces;

namespace FNEV4.Presentation.ViewModels.CertificationFne
{
    /// <summary>
    /// ViewModel pour le dashboard de certification FNE
    /// Affiche les statistiques et métriques de certification
    /// </summary>
    public partial class CertificationDashboardViewModel : ObservableObject
    {
        #region Services injectés
        
        private readonly IFneCertificationService _certificationService;
        private readonly ILoggingService _loggingService;
        
        #endregion

        #region Propriétés observables

        [ObservableProperty]
        private int _totalInvoicesCertified = 0;

        [ObservableProperty]
        private int _invoicesPendingCertification = 0;

        [ObservableProperty]
        private int _certificationErrorsToday = 0;

        [ObservableProperty]
        private decimal _successRate = 0;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _lastUpdateTime = string.Empty;

        public ObservableCollection<string> RecentMetrics { get; } = new();

        #endregion

        #region Constructeur

        public CertificationDashboardViewModel(IFneCertificationService certificationService, ILoggingService loggingService)
        {
            _certificationService = certificationService ?? throw new ArgumentNullException(nameof(certificationService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            
            InitializeAsync();
        }

        #endregion

        #region Méthodes d'initialisation

        private async void InitializeAsync()
        {
            await LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                IsLoading = true;
                await _loggingService.LogInformationAsync("CertificationDashboard", "Chargement des données du dashboard de certification");

                // Récupération des métriques via le service
                var metrics = await _certificationService.GetPerformanceMetricsAsync();
                
                if (metrics != null)
                {
                    TotalInvoicesCertified = metrics.TotalCertificationsToday;
                    InvoicesPendingCertification = 0; // Pas disponible dans les métriques actuelles
                    CertificationErrorsToday = metrics.TotalErrorsToday;
                    SuccessRate = (decimal)metrics.SuccessRate;
                    
                    RecentMetrics.Clear();
                    // Simulation d'activités récentes
                    RecentMetrics.Add($"{DateTime.Now.AddMinutes(-10):HH:mm} - Facture certificée avec succès");
                    RecentMetrics.Add($"{DateTime.Now.AddMinutes(-25):HH:mm} - Lot de 5 factures traité");
                    RecentMetrics.Add($"{DateTime.Now.AddHours(-1):HH:mm} - Synchronisation avec l'API FNE");
                }

                LastUpdateTime = $"Dernière mise à jour: {DateTime.Now:HH:mm:ss}";
                await _loggingService.LogInformationAsync("CertificationDashboard", "Données du dashboard chargées avec succès");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("CertificationDashboard", ex, "Erreur lors du chargement des données du dashboard");
                // Valeurs par défaut en cas d'erreur
                TotalInvoicesCertified = 0;
                InvoicesPendingCertification = 0;
                CertificationErrorsToday = 0;
                SuccessRate = 0;
                LastUpdateTime = "Erreur de chargement";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Commandes

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            await LoadDashboardDataAsync();
        }

        [RelayCommand]
        private void ViewDetailedReport()
        {
            _ = _loggingService.LogInformationAsync("CertificationDashboard", "Affichage du rapport détaillé de certification");
        }

        [RelayCommand]
        private void ExportMetrics()
        {
            _ = _loggingService.LogInformationAsync("CertificationDashboard", "Export des métriques de certification");
        }

        #endregion
    }
}