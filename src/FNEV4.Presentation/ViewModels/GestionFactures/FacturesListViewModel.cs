using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FNEV4.Presentation.ViewModels.GestionFactures
{
    /// <summary>
    /// ViewModel pour la gestion de la liste des factures FNE
    /// Utilise le système centralisé avec les entités FneInvoice existantes
    /// </summary>
    public partial class FacturesListViewModel : ObservableObject
    {
        #region Services
        private readonly ILogger<FacturesListViewModel> _logger;
        private readonly IInvoiceRepository _invoiceRepository;
        #endregion

        #region Propriétés observables
        
        [ObservableProperty]
        private string _searchText = string.Empty;
        
        [ObservableProperty]
        private string _selectedStatutFilter = "Tous";
        
        [ObservableProperty]
        private DateTime? _dateDebutFilter;
        
        [ObservableProperty]
        private DateTime? _dateFinFilter;
        
        [ObservableProperty]
        private bool _isLoading = false;
        
        [ObservableProperty]
        private bool _hasFactures = false;
        
        [ObservableProperty]
        private string _statusMessage = "Chargement des factures...";
        
        [ObservableProperty]
        private FneInvoice? _selectedFacture;
        
        [ObservableProperty]
        private int _totalFactures = 0;
        
        [ObservableProperty]
        private int _facturesCertifiees = 0;
        
        [ObservableProperty]
        private int _facturesEnAttente = 0;
        
        [ObservableProperty]
        private int _facturesEnErreur = 0;
        
        [ObservableProperty]
        private decimal _chiffreAffaireMois = 0;
        
        #endregion

        #region Collections
        
        /// <summary>
        /// Collection des factures FNE affichées
        /// </summary>
        public ObservableCollection<FneInvoice> Factures { get; } = new();
        
        /// <summary>
        /// Vue filtrée des factures pour la DataGrid
        /// </summary>
        public ICollectionView FacturesView { get; private set; }
        
        /// <summary>
        /// Options de filtre par statut
        /// </summary>
        public ObservableCollection<string> StatutFilterOptions { get; } = new()
        {
            "Tous",
            "Imported", 
            "Validated",
            "En cours certification",
            "Certified",
            "Error"
        };
        
        #endregion

        #region Constructeur
        
        public FacturesListViewModel(
            ILogger<FacturesListViewModel> logger,
            IInvoiceRepository invoiceRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            
            FacturesView = CollectionViewSource.GetDefaultView(Factures);
            FacturesView.Filter = FilterFactures;
            
            // Auto-chargement initial
            _ = LoadFactures();
            
            _logger.LogInformation("FacturesListViewModel initialisé");
        }
        
        #endregion

        #region Commandes
        
        [RelayCommand]
        private async Task LoadFactures()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Chargement des factures...";
                
                var factures = await _invoiceRepository.GetAllAsync();
                
                Factures.Clear();
                foreach (var facture in factures)
                {
                    Factures.Add(facture);
                }
                
                await UpdateStatistics();
                ApplyFilters();
                
                HasFactures = Factures.Count > 0;
                StatusMessage = HasFactures 
                    ? $"{Factures.Count} facture(s) chargée(s)" 
                    : "Aucune facture trouvée";
                    
                _logger.LogInformation("Chargement de {Count} factures terminé", Factures.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des factures");
                StatusMessage = $"Erreur de chargement : {ex.Message}";
                MessageBox.Show(
                    $"Impossible de charger les factures :\n\n{ex.Message}",
                    "Erreur de chargement",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        private void ApplyFilters()
        {
            FacturesView.Refresh();
            StatusMessage = HasFactures 
                ? $"{FacturesView.Cast<object>().Count()} / {Factures.Count} facture(s) affichée(s)"
                : "Aucune facture trouvée";
        }
        
        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedStatutFilter = "Tous";
            DateDebutFilter = null;
            DateFinFilter = null;
            ApplyFilters();
        }
        
        [RelayCommand]
        private async Task DeleteFacture(FneInvoice? facture)
        {
            if (facture == null) return;
            
            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer la facture {facture.InvoiceNumber} ?\n\nCette action est irréversible.",
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    StatusMessage = "Suppression en cours...";
                    
                    await _invoiceRepository.DeleteAsync(facture.Id.ToString());
                    Factures.Remove(facture);
                    await UpdateStatistics();
                    ApplyFilters();
                    
                    _logger.LogInformation("Facture {InvoiceNumber} supprimée avec succès", facture.InvoiceNumber);
                    StatusMessage = "Facture supprimée avec succès";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la suppression de la facture {InvoiceNumber}", facture.InvoiceNumber);
                    StatusMessage = $"Erreur lors de la suppression : {ex.Message}";
                    MessageBox.Show(
                        $"Impossible de supprimer la facture :\n\n{ex.Message}",
                        "Erreur de suppression",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
        
        [RelayCommand]
        private void EditFacture(FneInvoice? facture)
        {
            if (facture == null) return;
            
            // TODO: Ouvrir la fenêtre d'édition de facture
            MessageBox.Show(
                $"Édition de la facture {facture.InvoiceNumber}\n\nCette fonctionnalité sera bientôt disponible.",
                "En développement",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        
        [RelayCommand]
        private async Task ViewFacture(FneInvoice? facture)
        {
            if (facture == null) return;
            
            try
            {
                _logger.LogInformation("Ouverture des détails pour la facture {InvoiceNumber}", facture.InvoiceNumber);
                
                // Recharger la facture avec ses articles et client pour avoir toutes les données
                var factureComplete = await _invoiceRepository.GetByIdWithDetailsAsync(facture.Id.ToString());
                
                if (factureComplete == null)
                {
                    MessageBox.Show("Impossible de charger les détails de la facture.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Créer et configurer le ViewModel avec un logger null pour simplifier
                var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { });
                var detailsViewModel = new FactureDetailsViewModel(loggerFactory.CreateLogger<FactureDetailsViewModel>());
                detailsViewModel.Initialize(factureComplete);
                
                // Créer et afficher la fenêtre
                var detailsWindow = new Views.GestionFactures.FactureDetailsView
                {
                    DataContext = detailsViewModel,
                    Owner = System.Windows.Application.Current.MainWindow
                };
                
                detailsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ouverture des détails de la facture {InvoiceNumber}", facture.InvoiceNumber);
                MessageBox.Show(
                    $"Impossible d'ouvrir les détails de la facture :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        [RelayCommand]
        private void CreateNewFacture()
        {
            // TODO: Ouvrir la fenêtre de création de facture
            MessageBox.Show(
                "Création d'une nouvelle facture\n\nCette fonctionnalité sera bientôt disponible.",
                "En développement",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        
        #endregion

        #region Méthodes privées
        
        /// <summary>
        /// Filtre les factures selon les critères sélectionnés
        /// </summary>
        private bool FilterFactures(object item)
        {
            if (item is not FneInvoice facture) return false;
            
            // Filtre par texte de recherche
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTextLower = SearchText.ToLowerInvariant();
                var matchesSearch = 
                    facture.InvoiceNumber.ToLowerInvariant().Contains(searchTextLower) ||
                    (facture.Client?.Name?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (facture.Client?.ClientNcc?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    facture.ClientCode.ToLowerInvariant().Contains(searchTextLower);
                    
                if (!matchesSearch) return false;
            }
            
            // Filtre par statut
            if (!string.IsNullOrWhiteSpace(SelectedStatutFilter) && SelectedStatutFilter != "Tous")
            {
                if (facture.Status != SelectedStatutFilter) return false;
            }
            
            // Filtre par date de début
            if (DateDebutFilter.HasValue)
            {
                if (facture.InvoiceDate < DateDebutFilter.Value) return false;
            }
            
            // Filtre par date de fin
            if (DateFinFilter.HasValue)
            {
                if (facture.InvoiceDate > DateFinFilter.Value) return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Met à jour les statistiques affichées
        /// </summary>
        private async Task UpdateStatistics()
        {
            try
            {
                var stats = await _invoiceRepository.GetStatisticsAsync();
                
                TotalFactures = stats.TotalInvoices;
                FacturesCertifiees = stats.CertifiedInvoices;
                FacturesEnAttente = stats.PendingInvoices;
                FacturesEnErreur = TotalFactures - FacturesCertifiees - FacturesEnAttente;
                ChiffreAffaireMois = stats.MonthlyRevenue;
                
                _logger.LogDebug("Statistiques mises à jour : {Total} total, {Certified} certifiées, {Pending} en attente", 
                    TotalFactures, FacturesCertifiees, FacturesEnAttente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des statistiques");
            }
        }
        
        #endregion

        #region Event Handlers
        
        /// <summary>
        /// Gère les changements de propriétés pour déclencher les filtres automatiquement
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }
        
        partial void OnSelectedStatutFilterChanged(string value)
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