using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Models.ImportTraitement;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FNEV4.Presentation.ViewModels.ImportTraitement
{
    public partial class Sage100PreviewViewModel : ObservableObject
    {
        #region Properties

        private readonly Sage100ImportViewModel? _parentViewModel;

        [ObservableProperty]
        private ObservableCollection<Sage100FacturePreview> _facturesImportees = new();

        [ObservableProperty]
        private ObservableCollection<Sage100FacturePreview> _filteredFactures = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private int _validFiles;

        [ObservableProperty]
        private int _invalidFiles;

        [ObservableProperty]
        private string _currentFilter = "Toutes";

        public int TotalFactures => FacturesImportees?.Count ?? 0;
        public int FacturesTraitees => ValidFiles;
        public int FacturesEnErreur => InvalidFiles;

        #endregion

        #region Constructor

        public Sage100PreviewViewModel(Sage100ImportViewModel? parentViewModel = null)
        {
            _parentViewModel = parentViewModel;
            FacturesImportees = new ObservableCollection<Sage100FacturePreview>();
            FilteredFactures = new ObservableCollection<Sage100FacturePreview>();
        }

        #endregion

        #region Filter Methods

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnFacturesImporteesChanged(ObservableCollection<Sage100FacturePreview> value)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (FacturesImportees == null)
                return;

            FilteredFactures.Clear();
            
            var filtered = FacturesImportees.AsEnumerable();

            // Filtre par statut
            switch (CurrentFilter)
            {
                case "Valides":
                    filtered = filtered.Where(f => f.EstValide);
                    break;
                case "Erreurs":
                    filtered = filtered.Where(f => !f.EstValide);
                    break;
                case "Toutes":
                default:
                    // Pas de filtre, afficher toutes les factures
                    break;
            }

            // Filtre de recherche rapide sur plusieurs champs
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(f =>
                    (f.NumeroFacture?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                    (f.NomClient?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                    (f.CodeClient?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                    (f.PointDeVente?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                    (f.MoyenPaiement?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                    (f.Produits?.Any(p => p.Designation?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) == true) ||
                    (f.Produits?.Any(p => p.CodeProduit?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) == true));
            }

            foreach (var facture in filtered)
            {
                FilteredFactures.Add(facture);
            }

            // Notifier les propriétés calculées
            OnPropertyChanged(nameof(TotalFactures));
        }

        #endregion

        #region Public Methods

        public void LoadPreviewData(Sage100PreviewResult previewResult)
        {
            if (previewResult == null) return;

            FacturesImportees.Clear();
            
            foreach (var facture in previewResult.Apercu)
            {
                FacturesImportees.Add(facture);
            }

            ValidFiles = previewResult.Apercu.Count(f => f.EstValide);
            InvalidFiles = previewResult.Apercu.Count(f => !f.EstValide);
            
            // Déclencher le filtrage pour remplir FilteredFactures
            ApplyFilters();
            
            // Notifier les propriétés calculées
            OnPropertyChanged(nameof(TotalFactures));
            OnPropertyChanged(nameof(FacturesTraitees));
            OnPropertyChanged(nameof(FacturesEnErreur));
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        [RelayCommand]
        private void ShowProductDetails(object? parameter)
        {
            if (parameter is not Sage100FacturePreview facture || facture == null) 
                return;
            
            var produitDetails = string.Empty;
            if (facture.Produits?.Any() == true)
            {
                produitDetails = string.Join("\n", facture.Produits.Select(p => 
                    $"• {p.Designation} - Qté: {p.Quantite} - Prix: {p.PrixUnitaire:N2} - Total: {p.MontantHt:N2}"));
            }
            else
            {
                produitDetails = "Aucun produit détaillé disponible";
            }
            
            System.Windows.MessageBox.Show(
                $"Détails de la facture {facture.NumeroFacture}:\n\n" +
                $"Client: {facture.NomClient}\n" +
                $"Date: {facture.DateFacture:dd/MM/yyyy}\n" +
                $"Nombre de produits: {facture.NombreProduits}\n" +
                $"Montant HT: {facture.MontantHT:N2}\n" +
                $"Montant TTC: {facture.MontantTTC:N2}\n\n" +
                $"Produits:\n{produitDetails}",
                "Détails de la facture",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task ImportInvoices()
        {
            try
            {
                if (_parentViewModel == null)
                {
                    System.Windows.MessageBox.Show(
                        "Erreur: Impossible d'accéder au processus d'import.", 
                        "Erreur", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                    return;
                }

                var validInvoices = FacturesImportees.Where(f => f.EstValide).Count();
                var totalInvoices = FacturesImportees.Count;

                var confirmMessage = $"📋 CONFIRMATION D'IMPORT\n\n" +
                                   $"✅ Factures à importer: {totalInvoices}\n" +
                                   $"✅ Factures valides: {validInvoices}\n\n" +
                                   $"Voulez-vous procéder à l'import ?";

                var result = System.Windows.MessageBox.Show(
                    confirmMessage,
                    "Confirmation d'import",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // Fermer la fenêtre d'aperçu
                    var currentWindow = System.Windows.Application.Current.Windows
                        .OfType<Views.ImportTraitement.Sage100PreviewWindow>()
                        .FirstOrDefault();
                    
                    // Lancer l'import via le ViewModel parent
                    await _parentViewModel.ProcessImportFromPreview();
                    
                    currentWindow?.Close();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'import :\n{ex.Message}", 
                    "Erreur", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ExportToExcel()
        {
            try
            {
                // TODO: Implémenter l'export Excel des factures prévisualisées
                System.Windows.MessageBox.Show(
                    "Fonctionnalité d'export Excel en cours de développement", 
                    "Information", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'export :\n{ex.Message}", 
                    "Erreur", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ImportFactures()
        {
            try
            {
                // Déléguer l'import au ViewModel parent s'il existe
                if (_parentViewModel != null)
                {
                    // Utiliser la méthode d'import publique du parent
                    await _parentViewModel.ProcessImportFromPreview();
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Impossible de démarrer l'import - Référence au module d'import non disponible", 
                        "Erreur", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors du démarrage de l'import :\n{ex.Message}", 
                    "Erreur", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ShowAllFactures()
        {
            CurrentFilter = "Toutes";
            ApplyFilters();
        }

        [RelayCommand]
        private void ShowValidFactures()
        {
            CurrentFilter = "Valides";
            ApplyFilters();
        }

        [RelayCommand]
        private void ShowErrorFactures()
        {
            CurrentFilter = "Erreurs";
            ApplyFilters();
        }

        #endregion
    }
}
