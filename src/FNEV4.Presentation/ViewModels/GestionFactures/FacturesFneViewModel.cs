using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using FNEV4.Infrastructure.Services;

namespace FNEV4.Presentation.ViewModels.GestionFactures
{
    /// <summary>
    /// ViewModel pour le sous-menu Factures FNE
    /// Gère la liste simple des factures FNE importées
    /// </summary>
    public partial class FacturesFneViewModel : ObservableObject
    {
        #region Services
        
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IDatabaseService _databaseService;
        
        // Timer pour optimiser la recherche (debouncing)
        private readonly DispatcherTimer _searchTimer;
        
        #endregion

        #region Properties

        [ObservableProperty]
        private ObservableCollection<FneInvoice> factures = new();
        
        // Collection complète pour le filtrage local
        private List<FneInvoice> _allFactures = new();

        [ObservableProperty]
        private FneInvoice? selectedFacture;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string selectedStatus = "Tous";

        [ObservableProperty]
        private string statusMessage = "Prêt";

        [ObservableProperty]
        private int totalFactures;

        [ObservableProperty]
        private int facturesDraft;

        [ObservableProperty]
        private int facturesCertifiees;

        // Options de filtrage par statut (simplifiées selon les statuts réellement utilisés)
        public List<string> StatusOptions { get; } = new()
        {
            "Tous",
            "Draft", 
            "Certified"
        };

        #endregion

        #region Property Change Handlers
        
        /// <summary>
        /// Applique les filtres quand le texte de recherche change (avec délai pour optimiser)
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            // Arrêter le timer précédent
            _searchTimer.Stop();
            
            // Si le texte est vide, filtrer immédiatement
            if (string.IsNullOrWhiteSpace(value))
            {
                ApplyLocalFilters();
            }
            else
            {
                // Sinon, attendre 300ms avant de filtrer (debouncing)
                _searchTimer.Start();
            }
        }
        
        /// <summary>
        /// Applique les filtres quand le statut sélectionné change
        /// </summary>
        partial void OnSelectedStatusChanged(string value)
        {
            ApplyLocalFilters();
        }

        #endregion

        #region Constructor

        public FacturesFneViewModel(IInvoiceRepository invoiceRepository, IDatabaseService databaseService)
        {
            _invoiceRepository = invoiceRepository;
            _databaseService = databaseService;
            
            // Initialiser le timer de recherche avec un délai de 300ms
            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                ApplyLocalFilters();
            };
            
            // Charger les factures au démarrage
            _ = LoadFacturesAsync();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Commande pour charger les factures
        /// </summary>
        [RelayCommand]
        private async Task LoadFacturesAsync()
        {
            IsLoading = true;
            StatusMessage = "Chargement des factures...";

            try
            {
                var allFactures = await _invoiceRepository.GetAllAsync();
                
                // Stocker toutes les factures pour le filtrage local
                _allFactures = allFactures.OrderByDescending(f => f.CreatedAt).ToList();
                
                // Appliquer les filtres
                ApplyLocalFilters();

                await UpdateStatisticsAsync();
                StatusMessage = $"Chargé {_allFactures.Count} factures";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur lors du chargement: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Commande pour actualiser la liste
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadFacturesAsync();
        }

        /// <summary>
        /// Commande pour voir les détails d'une facture
        /// </summary>
        [RelayCommand]
        private async Task ViewFacture(FneInvoice? facture)
        {
            if (facture == null) return;

            try
            {
                StatusMessage = $"Chargement détails facture {facture.InvoiceNumber}...";
                
                // Recharger la facture avec ses relations pour s'assurer que toutes les données sont disponibles
                var factureComplete = await _invoiceRepository.GetByIdWithDetailsAsync(facture.Id.ToString());
                if (factureComplete == null)
                {
                    StatusMessage = "Erreur: facture introuvable";
                    System.Windows.MessageBox.Show(
                        "Impossible de charger les détails de cette facture.",
                        "Erreur", 
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return;
                }
                
                StatusMessage = $"Ouverture détails facture {factureComplete.InvoiceNumber}";
                
                // Créer le ViewModel pour le dialogue avec la facture complètement chargée
                var dialogViewModel = new FactureFneDetailsViewModel(factureComplete, _databaseService);
                
                // Trouver la fenêtre principale actuelle
                var mainWindow = System.Windows.Application.Current.MainWindow;
                
                // Créer et afficher le dialogue avec le service provider
                var dialog = new Views.GestionFactures.FactureFneDetailsDialog(
                    serviceProvider: App.ServiceProvider, 
                    logger: App.ServiceProvider?.GetService<Microsoft.Extensions.Logging.ILogger<Views.GestionFactures.FactureFneDetailsDialog>>())
                {
                    DataContext = dialogViewModel,
                    Owner = mainWindow,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
                };
                
                dialog.ShowDialog();
                
                StatusMessage = "Prêt";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur ouverture détails: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'ouverture des détails: {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Commande pour supprimer une facture
        /// </summary>
        [RelayCommand]
        private async Task DeleteFactureAsync(FneInvoice? facture)
        {
            if (facture == null) return;

            try
            {
                // Confirmation suppression (simplifié pour l'instant)
                var result = System.Windows.MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer la facture {facture.InvoiceNumber} ?",
                    "Confirmation suppression",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    await _invoiceRepository.DeleteAsync(facture.Id.ToString());
                    Factures.Remove(facture);
                    await UpdateStatisticsAsync();
                    StatusMessage = $"Facture {facture.InvoiceNumber} supprimée";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur suppression: {ex.Message}";
                System.Windows.MessageBox.Show($"Erreur lors de la suppression: {ex.Message}", "Erreur", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Commande pour appliquer les filtres
        /// </summary>
        [RelayCommand]
        private void ApplyFilters()
        {
            ApplyLocalFilters();
        }

        /// <summary>
        /// Commande pour certifier une facture (action future)
        /// </summary>
        [RelayCommand]
        private void CertifyFacture(FneInvoice? facture)
        {
            if (facture == null) return;

            StatusMessage = $"Certification de la facture {facture.InvoiceNumber}";
            System.Windows.MessageBox.Show("Fonctionnalité de certification à implémenter", "À venir", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Applique les filtres localement sur la collection complète (version optimisée)
        /// </summary>
        private void ApplyLocalFilters()
        {
            if (_allFactures == null || !_allFactures.Any())
            {
                Factures.Clear();
                StatusMessage = "Aucune facture chargée";
                return;
            }

            // Utiliser AsParallel pour améliorer les performances sur de grandes collections
            IEnumerable<FneInvoice> filteredFactures = _allFactures;

            // Filtrage par texte de recherche (optimisé)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filteredFactures = filteredFactures.Where(f =>
                    f != null &&
                    (f.InvoiceNumber?.ToLower().Contains(searchLower) == true ||
                     (f.ClientDisplayName ?? "").ToLower().Contains(searchLower) ||
                     (f.ClientCode ?? "").ToLower().Contains(searchLower) ||
                     f.TotalAmountTTC.ToString().Contains(searchLower)));
            }

            // Filtrage par statut (optimisé)
            if (SelectedStatus != "Tous")
            {
                filteredFactures = filteredFactures.Where(f => 
                    f != null && 
                    !string.IsNullOrEmpty(f.Status) &&
                    string.Equals(f.Status, SelectedStatus, StringComparison.OrdinalIgnoreCase));
            }

            // Convertir en liste une seule fois
            var resultList = filteredFactures.Where(f => f != null && !string.IsNullOrEmpty(f.InvoiceNumber)).ToList();
            
            // Mettre à jour la collection sur le thread UI de manière optimisée
            Factures.Clear();
            foreach (var facture in resultList)
            {
                Factures.Add(facture);
            }

            // Message informatif
            StatusMessage = $"Affichage: {Factures.Count} sur {_allFactures.Count} factures";
        }

        /// <summary>
        /// Met à jour les statistiques affichées
        /// </summary>
        private async Task UpdateStatisticsAsync()
        {
            try
            {
                // Calculer sur la collection complète, pas filtrée
                TotalFactures = _allFactures?.Count ?? 0;
                FacturesDraft = _allFactures?.Count(f => f.Status.Equals("draft", StringComparison.OrdinalIgnoreCase)) ?? 0;
                FacturesCertifiees = _allFactures?.Count(f => f.Status.Equals("certified", StringComparison.OrdinalIgnoreCase)) ?? 0;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur statistiques: {ex.Message}";
            }
        }

        #endregion
    }
}