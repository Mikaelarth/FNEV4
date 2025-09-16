using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using FNEV4.Core.Interfaces.Services.Fne;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace FNEV4.Presentation.ViewModels.CertificationFne
{
    /// <summary>
    /// ViewModel pour la certification manuelle FNE
    /// Suit l'architecture centralisée de FNEV4 avec injection de dépendances
    /// </summary>
    public partial class CertificationManuelleViewModel : ObservableObject
    {
        #region Services injectés
        
        private readonly IFneCertificationService _certificationService;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly FNEV4.Core.Interfaces.ILoggingService _loggingService;
        
        #endregion

        #region Propriétés observables

        [ObservableProperty]
        private ObservableCollection<FneInvoice> _availableInvoices = new();

        [ObservableProperty]
        private ObservableCollection<FneInvoice> _selectedInvoices = new();

        [ObservableProperty]
        private ICollectionView? _invoicesView;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedStatusFilter = "draft"; // Utiliser la vraie valeur de la base

        [ObservableProperty]
        private DateTime? _dateDebutFilter;

        [ObservableProperty]
        private DateTime? _dateFinFilter;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isCertifying = false;

        [ObservableProperty]
        private string _statusMessage = "Prêt pour certification";

        [ObservableProperty]
        private int _totalInvoicesCount = 0;

        [ObservableProperty]
        private int _selectedInvoicesCount = 0;

        [ObservableProperty]
        private int _certificationProgress = 0;

        [ObservableProperty]
        private int _certificationMaximum = 1; // Éviter division par 0 et barre visible

        [ObservableProperty]
        private string _currentCertificationInvoice = string.Empty;

        [ObservableProperty]
        private bool _hasAvailableInvoices = false;

        [ObservableProperty]
        private bool _hasSelectedInvoices = false;

        [ObservableProperty]
        private string _certificationSummary = string.Empty;

        #endregion

        #region Collections et Filtres

        public List<string> StatusFilters { get; } = new()
        {
            "Tous",
            "draft",
            "validated", 
            "error"
        };

        #endregion

        #region Logging Helpers

        private async Task LogInfoAsync(string message, string category = "CertificationManuelle")
        {
            await _loggingService.LogInformationAsync(message, category);
        }

        private async Task LogWarningAsync(string message, string category = "CertificationManuelle")
        {
            await _loggingService.LogWarningAsync(message, category);
        }

        private async Task LogErrorAsync(string message, string category = "CertificationManuelle", Exception? exception = null)
        {
            await _loggingService.LogErrorAsync(message, exception, category);
        }

        private async Task LogDebugAsync(string message, string category = "CertificationManuelle")
        {
            await _loggingService.LogDebugAsync(message, category);
        }

        #endregion

        #region Constructor

        public CertificationManuelleViewModel(
            IFneCertificationService certificationService,
            IInvoiceRepository invoiceRepository,
            FNEV4.Core.Interfaces.ILoggingService loggingService)
        {
            _certificationService = certificationService ?? throw new ArgumentNullException(nameof(certificationService));
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

            // Log de débogage immédiat sans async pour voir si le constructeur s'exécute
            try
            {
                // Utiliser Task.Run pour log synchrone sans bloquer
                Task.Run(() => _loggingService.LogInformationAsync("Certification", "=== CONSTRUCTEUR CertificationManuelleViewModel DÉMARRÉ ==="));
                System.Diagnostics.Debug.WriteLine("=== DEBUG: CertificationManuelleViewModel Constructor Started ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== DEBUG: Erreur log dans constructeur: {ex.Message} ===");
            }

            // Configuration de la vue filtrée
            InvoicesView = CollectionViewSource.GetDefaultView(AvailableInvoices);
            if (InvoicesView != null)
            {
                InvoicesView.Filter = FilterInvoices;
            }

            // Chargement automatique des données au démarrage
            StatusMessage = "Initialisation...";
            
            // Approche simplifiée : chargement direct sans Dispatcher complexe
            Task.Run(async () =>
            {
                try
                {
                    await LogInfoAsync("=== INITIALISATION CertificationManuelleViewModel ===");
                    await LogInfoAsync($"CertificationService: {_certificationService.GetType().Name}");
                    await LogInfoAsync($"InvoiceRepository: {_invoiceRepository.GetType().Name}");
                    
                    // Attendre un peu pour que l'UI soit prête
                    await Task.Delay(100);
                    
                    await LogInfoAsync("=== DÉMARRAGE CHARGEMENT AUTOMATIQUE ===");
                    await LoadAvailableInvoicesAsync();
                    await LogInfoAsync("=== FIN INITIALISATION CertificationManuelleViewModel ===");
                }
                catch (Exception ex)
                {
                    await LogErrorAsync("Erreur lors de l'initialisation du ViewModel", exception: ex);
                    
                    // En cas d'erreur, mettre à jour l'UI sur le thread principal
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsLoading = false;
                        StatusMessage = $"Erreur d'initialisation: {ex.Message}";
                    });
                }
            });
        }

        #endregion

        #region Commandes

        [RelayCommand]
        private async Task LoadAvailableInvoicesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== DEBUG: LoadAvailableInvoicesAsync DÉMARRÉ ===");
                
                IsLoading = true;
                StatusMessage = "Chargement des factures...";

                await LogInfoAsync("=== DÉBUT LoadAvailableInvoicesAsync ===");
                System.Diagnostics.Debug.WriteLine("=== DEBUG: Après LogInfoAsync ===");

                await LogInfoAsync("=== DÉBUT CHARGEMENT FACTURES CERTIFICATION ===");
                await LogInfoAsync($"Repository type: {_invoiceRepository.GetType().Name}");

                // Utiliser la méthode spécifique pour les factures disponibles pour certification
                await LogInfoAsync("Appel GetAvailableForCertificationAsync...");
                var invoices = await _invoiceRepository.GetAvailableForCertificationAsync();
                await LogInfoAsync($"Récupéré {invoices.Count()} factures disponibles pour certification");

                await LogInfoAsync("Effacement de la collection AvailableInvoices...");
                
                // Mise à jour de l'UI sur le thread principal
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableInvoices.Clear();
                });
                
                await LogInfoAsync($"Ajout des {invoices.Count()} factures à la collection...");
                
                // Ajouter les factures sur le thread principal
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var invoice in invoices)
                    {
                        // Log pour diagnostiquer les données client
                        var clientInfo = invoice.Client?.CompanyName ?? invoice.Client?.Name ?? $"Code: {invoice.ClientCode}";
                        System.Diagnostics.Debug.WriteLine($"Facture {invoice.InvoiceNumber}: Client = {clientInfo}");
                        
                        AvailableInvoices.Add(invoice);
                    }
                });

                await LogInfoAsync($"Collection AvailableInvoices mise à jour - Count: {AvailableInvoices.Count}");

                // Mise à jour des propriétés sur le thread principal
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    TotalInvoicesCount = AvailableInvoices.Count;
                    HasAvailableInvoices = TotalInvoicesCount > 0;
                    StatusMessage = HasAvailableInvoices 
                        ? $"{TotalInvoicesCount} facture(s) disponible(s) pour certification"
                        : "Aucune facture disponible pour certification";
                });

                await LogInfoAsync($"Propriétés mises à jour - TotalCount: {TotalInvoicesCount}, HasAvailable: {HasAvailableInvoices}, StatusMessage: {StatusMessage}");

                // Forcer la mise à jour de la vue sur le thread principal
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    InvoicesView?.Refresh();
                });
                await LogInfoAsync("InvoicesView.Refresh() appelé");

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ApplyFilters();
                });

                await LogInfoAsync($"=== FIN CHARGEMENT - SUCCESS: {TotalInvoicesCount} factures disponibles ===");
            }
            catch (Exception ex)
            {
                await LogErrorAsync("=== ERREUR CRITIQUE LORS DU CHARGEMENT ===", exception: ex);
                await LogErrorAsync($"Type d'erreur: {ex.GetType().Name}");
                await LogErrorAsync($"Message: {ex.Message}");
                await LogErrorAsync($"StackTrace: {ex.StackTrace}");
                
                StatusMessage = "Erreur lors du chargement: " + ex.Message;
                HasAvailableInvoices = false;
                TotalInvoicesCount = 0;
                
                // Afficher l'erreur à l'utilisateur aussi
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Erreur lors du chargement des factures:\n\n{ex.Message}\n\nType: {ex.GetType().Name}", 
                        "Erreur Certification", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                // Mise à jour finale sur le thread principal
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
                await LogInfoAsync("=== CHARGEMENT TERMINÉ - IsLoading = false ===");
            }
        }

        [RelayCommand]
        private void ApplyFilters()
        {
            InvoicesView?.Refresh();
            StatusMessage = $"Filtres appliqués - {InvoicesView?.Cast<object>().Count() ?? 0} facture(s) affichée(s)";
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedStatusFilter = "Draft";
            DateDebutFilter = null;
            DateFinFilter = null;
            
            ApplyFilters();
            StatusMessage = "Filtres effacés";
        }

        [RelayCommand]
        private void AddToSelection(FneInvoice? invoice)
        {
            if (invoice == null || SelectedInvoices.Contains(invoice)) return;

            SelectedInvoices.Add(invoice);
            SelectedInvoicesCount = SelectedInvoices.Count;
            HasSelectedInvoices = SelectedInvoicesCount > 0;
            UpdateCertificationSummary();
        }

        [RelayCommand]
        private void RemoveFromSelection(FneInvoice? invoice)
        {
            if (invoice == null) return;

            SelectedInvoices.Remove(invoice);
            SelectedInvoicesCount = SelectedInvoices.Count;
            HasSelectedInvoices = SelectedInvoicesCount > 0;
            UpdateCertificationSummary();        }

        [RelayCommand]
        private void AddAllToSelection()
        {
            var filteredInvoices = InvoicesView?.Cast<FneInvoice>().ToList() ?? new List<FneInvoice>();
            
            foreach (var invoice in filteredInvoices)
            {
                if (!SelectedInvoices.Contains(invoice))
                {
                    SelectedInvoices.Add(invoice);
                }
            }

            SelectedInvoicesCount = SelectedInvoices.Count;
            HasSelectedInvoices = SelectedInvoicesCount > 0;
            UpdateCertificationSummary();        }

        [RelayCommand]
        private void ClearSelection()
        {
            SelectedInvoices.Clear();
            SelectedInvoicesCount = 0;
            HasSelectedInvoices = false;
            CertificationSummary = string.Empty;        }

        [RelayCommand]
        private async Task CertifySelectedInvoicesAsync()
        {
            if (!HasSelectedInvoices)
            {
                MessageBox.Show("Veuillez sélectionner au moins une facture à certifier.", 
                    "Sélection vide", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir certifier {SelectedInvoicesCount} facture(s) ?\n\nCette action ne peut pas être annulée.",
                "Confirmation de certification",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await PerformCertificationAsync();
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            await LoadAvailableInvoicesAsync();
        }

        [RelayCommand]
        private async Task ViewInvoiceDetails(FneInvoice? invoice)
        {
            if (invoice == null) return;

            try
            {
                // Suivre le pattern existant de FacturesListViewModel pour maintenir la compatibilité
                var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { });
                var detailsViewModel = new ViewModels.GestionFactures.FactureDetailsViewModel(
                    loggerFactory.CreateLogger<ViewModels.GestionFactures.FactureDetailsViewModel>());
                detailsViewModel.Initialize(invoice);
                
                var detailsView = new Views.GestionFactures.FactureDetailsView();
                detailsView.DataContext = detailsViewModel;
                detailsView.Owner = System.Windows.Application.Current.MainWindow;
                detailsView.ShowDialog();
            }
            catch (Exception ex)
            {
                await LogErrorAsync($"Erreur lors de l'affichage des détails de la facture {invoice?.InvoiceNumber}: {ex.Message}");
                MessageBox.Show("Impossible d'ouvrir les détails de la facture.", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Méthodes privées

        private bool FilterInvoices(object item)
        {
            if (item is not FneInvoice invoice) return false;

            // Filtre par texte
            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLower();
                var matches = invoice.InvoiceNumber.ToLower().Contains(searchLower) ||
                             (invoice.Client?.CompanyName?.ToLower().Contains(searchLower) ?? false);
                if (!matches) return false;
            }

            // Filtre par statut
            if (SelectedStatusFilter != "Tous" && invoice.Status.ToString() != SelectedStatusFilter)
                return false;

            // Filtre par date
            if (DateDebutFilter.HasValue && invoice.InvoiceDate.Date < DateDebutFilter.Value.Date)
                return false;

            if (DateFinFilter.HasValue && invoice.InvoiceDate.Date > DateFinFilter.Value.Date)
                return false;

            return true;
        }

        private void UpdateCertificationSummary()
        {
            if (!HasSelectedInvoices)
            {
                CertificationSummary = string.Empty;
                return;
            }

            var totalAmount = SelectedInvoices.Sum(i => i.TotalAmountTTC);
            CertificationSummary = $"Sélection: {SelectedInvoicesCount} factures • Total: {totalAmount:N2} TND";
        }

        private async Task PerformCertificationAsync()
        {
            try
            {
                IsCertifying = true;
                var invoicesToCertify = SelectedInvoices.ToList();
                CertificationMaximum = invoicesToCertify.Count;
                CertificationProgress = 0;

                await LogInfoAsync($"Début certification manuelle de {invoicesToCertify.Count} factures");

                // Récupérer la configuration FNE active
                var configuration = await GetActiveFneConfigurationAsync();
                if (configuration == null)
                {
                    MessageBox.Show("Aucune configuration FNE active trouvée.\nVeuillez configurer l'API FNE d'abord.",
                        "Configuration manquante", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int successCount = 0;
                int errorCount = 0;

                foreach (var invoice in invoicesToCertify)
                {
                    CurrentCertificationInvoice = $"Certification {invoice.InvoiceNumber}...";
                    StatusMessage = CurrentCertificationInvoice;

                    try
                    {
                        var certificationResult = await _certificationService.CertifyInvoiceAsync(invoice, configuration);
                        
                        if (certificationResult.IsSuccess)
                        {
                            successCount++;
                            await LogInfoAsync($"Facture {invoice.InvoiceNumber} certifiée avec succès");
                        }
                        else
                        {
                            errorCount++;
                            await LogWarningAsync($"Échec certification facture {invoice.InvoiceNumber}: {certificationResult.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        await LogErrorAsync($"Exception lors de la certification facture {invoice.InvoiceNumber}", exception: ex);
                    }

                    CertificationProgress++;
                    
                    // Petite pause pour éviter de surcharger l'API
                    await Task.Delay(100);
                }

                // Résumé final
                var message = $"Certification terminée:\n\n✅ Succès: {successCount}\n❌ Échecs: {errorCount}";
                MessageBox.Show(message, "Résultat certification", MessageBoxButton.OK, 
                    errorCount == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);

                // Rafraîchir les données et vider la sélection
                ClearSelection();
                await LoadAvailableInvoicesAsync();

                await LogInfoAsync($"Certification manuelle terminée: {successCount} succès, {errorCount} erreurs");
            }
            catch (Exception ex)
            {
                await LogErrorAsync("Erreur générale lors de la certification manuelle", exception: ex);
                MessageBox.Show("Erreur lors de la certification: " + ex.Message, 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsCertifying = false;
                CurrentCertificationInvoice = string.Empty;
                StatusMessage = "Certification terminée";
                CertificationProgress = 0;
            }
        }

        private async Task<FneConfiguration?> GetActiveFneConfigurationAsync()
        {
            try
            {
                // Utiliser le même pattern que les autres ViewModels pour accéder au DbContext
                var dbContext = App.ServiceProvider.GetRequiredService<FNEV4.Infrastructure.Data.FNEV4DbContext>();
                
                var activeConfig = await dbContext.FneConfigurations
                    .Where(c => c.IsActive && !c.IsDeleted)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();

                return activeConfig;
            }
            catch (Exception ex)
            {
                await LogErrorAsync("Erreur lors de la récupération de la configuration FNE active", exception: ex);
                return null;
            }
        }

        #endregion

        #region Property change handlers

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSelectedStatusFilterChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnDateDebutFilterChanged(DateTime? value)
        {
            ApplyFilters();
        }

        partial void OnDateFinFilterChanged(DateTime? value)
        {
            ApplyFilters();
        }

        #endregion
    }
}