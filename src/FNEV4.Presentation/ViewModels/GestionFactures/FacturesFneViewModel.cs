using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
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
        
        #endregion

        #region Properties

        [ObservableProperty]
        private ObservableCollection<FneInvoice> factures = new();

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

        // Options de filtrage par statut
        public List<string> StatusOptions { get; } = new()
        {
            "Tous",
            "Draft",
            "Validated", 
            "Certified",
            "Error"
        };

        #endregion

        #region Constructor

        public FacturesFneViewModel(IInvoiceRepository invoiceRepository, IDatabaseService databaseService)
        {
            _invoiceRepository = invoiceRepository;
            _databaseService = databaseService;
            
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
                
                Factures.Clear();
                foreach (var facture in allFactures.OrderByDescending(f => f.CreatedAt))
                {
                    Factures.Add(facture);
                }

                await UpdateStatisticsAsync();
                StatusMessage = $"Chargé {Factures.Count} factures";
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
        private async Task ApplyFiltersAsync()
        {
            await LoadFacturesAsync();
            // TODO: Implémenter le filtrage local pour optimiser les performances
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
        /// Met à jour les statistiques affichées
        /// </summary>
        private async Task UpdateStatisticsAsync()
        {
            try
            {
                var stats = await _invoiceRepository.GetStatisticsAsync();
                
                TotalFactures = Factures.Count;
                FacturesDraft = Factures.Count(f => f.Status == "draft");
                FacturesCertifiees = Factures.Count(f => f.Status == "certified");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur statistiques: {ex.Message}";
            }
        }

        #endregion
    }
}