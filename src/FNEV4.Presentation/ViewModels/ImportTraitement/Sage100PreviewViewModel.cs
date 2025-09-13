using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Models.ImportTraitement;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;
using Microsoft.Win32;

namespace FNEV4.Presentation.ViewModels.ImportTraitement
{
    public partial class Sage100PreviewViewModel : ObservableObject
    {
        #region Properties

        private readonly Sage100ImportViewModel? _parentViewModel;
        private readonly string _sourceFilePath;

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
        public int FacturesDoublons => FacturesImportees?.Count(f => f.EstDoublon) ?? 0;

        #endregion

        #region Constructor

        public Sage100PreviewViewModel(Sage100ImportViewModel? parentViewModel = null, string sourceFilePath = "")
        {
            _parentViewModel = parentViewModel;
            _sourceFilePath = sourceFilePath;
            FacturesImportees = new ObservableCollection<Sage100FacturePreview>();
            FilteredFactures = new ObservableCollection<Sage100FacturePreview>();
            
            // Debug : Vérifier le chemin reçu et le parent
            System.Diagnostics.Debug.WriteLine($"🔍 Sage100PreviewViewModel créé avec:");
            System.Diagnostics.Debug.WriteLine($"   - SourceFilePath: '{sourceFilePath ?? "null"}'");
            System.Diagnostics.Debug.WriteLine($"   - Parent SelectedFilePath: '{parentViewModel?.SelectedFilePath ?? "null"}'");
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
                    filtered = filtered.Where(f => f.EstValide && !f.EstDoublon);
                    break;
                case "Erreurs":
                    filtered = filtered.Where(f => !f.EstValide && !f.EstDoublon);
                    break;
                case "Doublons":
                    filtered = filtered.Where(f => f.EstDoublon);
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
            OnPropertyChanged(nameof(FacturesDoublons));
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
            OnPropertyChanged(nameof(FacturesDoublons));
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
            
            try
            {
                // Créer le ViewModel pour le dialogue
                var dialogViewModel = new Sage100FactureDetailsViewModel(facture);
                
                // Trouver la fenêtre d'aperçu actuelle
                var previewWindow = System.Windows.Application.Current.Windows
                    .OfType<Views.ImportTraitement.Sage100PreviewWindow>()
                    .FirstOrDefault();
                
                // Créer et afficher le dialogue
                var dialog = new Views.ImportTraitement.Sage100FactureDetailsDialog
                {
                    DataContext = dialogViewModel,
                    Owner = previewWindow ?? System.Windows.Application.Current.MainWindow,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
                };
                
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'affichage des détails de la facture: {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
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
                    
                    // Debug : Vérifier le chemin avant l'appel
                    System.Diagnostics.Debug.WriteLine($"🔍 ImportFactures - Appel avec SourceFilePath: '{_sourceFilePath ?? "null"}'");
                    
                    // Lancer l'import via le ViewModel parent avec données pré-validées et chemin explicite
                    await _parentViewModel.ProcessImportFromPreviewWithData(FacturesImportees, _sourceFilePath ?? "");
                    
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
                // Vérifier qu'il y a des factures à exporter
                if (FilteredFactures?.Count == 0)
                {
                    System.Windows.MessageBox.Show(
                        "Aucune facture à exporter avec les filtres actuels.", 
                        "Aucune donnée", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Information);
                    return;
                }

                // Dialog pour choisir le fichier de destination
                var saveDialog = new SaveFileDialog
                {
                    Title = "Exporter les factures filtrées",
                    Filter = "Fichiers Excel (*.xlsx)|*.xlsx|Tous les fichiers (*.*)|*.*",
                    DefaultExt = "xlsx",
                    FileName = $"Factures_Sage100_{CurrentFilter}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() == true && !string.IsNullOrEmpty(saveDialog.FileName))
                {
                    ExportFacturesToExcel(saveDialog.FileName);
                    
                    System.Windows.MessageBox.Show(
                        $"Export terminé avec succès !\n\n" +
                        $"Fichier : {saveDialog.FileName}\n" +
                        $"Factures exportées : {FilteredFactures?.Count ?? 0} ({CurrentFilter})", 
                        "Export réussi", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Information);
                }
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

        private void ExportFacturesToExcel(string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Factures Sage 100");

            // En-têtes de colonnes
            var headers = new[]
            {
                "Fichier Source", "N° Facture", "Date", "Code Client", "Nom Client", 
                "Template", "Point de Vente", "Moyen Paiement", "Nb Produits", 
                "Montant HT (FCFA)", "Montant TTC (FCFA)", "Montant TVA (FCFA)", 
                "Statut", "Client Trouvé", "Erreurs"
            };

            // Ajouter les en-têtes avec style
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
            }

            // Ajouter les données des factures filtrées
            int row = 2;
            foreach (var facture in FilteredFactures)
            {
                worksheet.Cell(row, 1).Value = facture.NomFichierSource;
                worksheet.Cell(row, 2).Value = facture.NumeroFacture;
                worksheet.Cell(row, 3).Value = facture.DateFacture;
                worksheet.Cell(row, 4).Value = facture.CodeClient;
                worksheet.Cell(row, 5).Value = facture.NomClient;
                worksheet.Cell(row, 6).Value = facture.Template;
                worksheet.Cell(row, 7).Value = facture.PointDeVente;
                worksheet.Cell(row, 8).Value = facture.MoyenPaiement;
                worksheet.Cell(row, 9).Value = facture.NombreProduits;
                worksheet.Cell(row, 10).Value = facture.MontantHT;
                worksheet.Cell(row, 11).Value = facture.MontantTTC;
                worksheet.Cell(row, 12).Value = facture.MontantTVA;
                worksheet.Cell(row, 13).Value = facture.Statut;
                worksheet.Cell(row, 14).Value = facture.ClientTrouve ? "Oui" : "Non";
                worksheet.Cell(row, 15).Value = string.Join("; ", facture.Erreurs);

                // Coloration selon le statut
                if (!facture.EstValide)
                {
                    worksheet.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.LightPink;
                }
                else if (!facture.ClientTrouve)
                {
                    worksheet.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.LightYellow;
                }

                row++;
            }

            // Ajustement automatique des colonnes
            worksheet.Columns().AdjustToContents();

            // Ajout d'une feuille de résumé
            var summarySheet = workbook.Worksheets.Add("Résumé");
            summarySheet.Cell("A1").Value = "Résumé de l'export";
            summarySheet.Cell("A1").Style.Font.Bold = true;
            summarySheet.Cell("A1").Style.Font.FontSize = 14;

            summarySheet.Cell("A3").Value = "Date d'export :";
            summarySheet.Cell("B3").Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            summarySheet.Cell("A4").Value = "Filtre appliqué :";
            summarySheet.Cell("B4").Value = CurrentFilter;

            summarySheet.Cell("A5").Value = "Recherche appliquée :";
            summarySheet.Cell("B5").Value = string.IsNullOrEmpty(SearchText) ? "Aucune" : SearchText;

            summarySheet.Cell("A7").Value = "Total factures exportées :";
            summarySheet.Cell("B7").Value = FilteredFactures.Count;

            summarySheet.Cell("A8").Value = "Factures valides :";
            summarySheet.Cell("B8").Value = FilteredFactures.Count(f => f.EstValide);

            summarySheet.Cell("A9").Value = "Factures en erreur :";
            summarySheet.Cell("B9").Value = FilteredFactures.Count(f => !f.EstValide);

            summarySheet.Cell("A10").Value = "Clients non trouvés :";
            summarySheet.Cell("B10").Value = FilteredFactures.Count(f => !f.ClientTrouve);

            var totalHT = FilteredFactures.Sum(f => f.MontantHT);
            var totalTTC = FilteredFactures.Sum(f => f.MontantTTC);

            summarySheet.Cell("A12").Value = "Montant total HT (FCFA) :";
            summarySheet.Cell("B12").Value = totalHT;
            summarySheet.Cell("B12").Style.NumberFormat.Format = "#,##0.00";

            summarySheet.Cell("A13").Value = "Montant total TTC (FCFA) :";
            summarySheet.Cell("B13").Value = totalTTC;
            summarySheet.Cell("B13").Style.NumberFormat.Format = "#,##0.00";

            summarySheet.Columns().AdjustToContents();

            // Sauvegarde du fichier
            workbook.SaveAs(filePath);
        }

        [RelayCommand]
        private async Task ImportFactures()
        {
            try
            {
                // Debug : Vérifier les valeurs au moment de l'import
                System.Diagnostics.Debug.WriteLine($"🔍 ImportFactures appelé - _sourceFilePath: '{_sourceFilePath ?? "null"}', Factures: {FacturesImportees?.Count ?? 0}");
                
                // Déléguer l'import au ViewModel parent s'il existe
                if (_parentViewModel != null)
                {
                    // Passer les factures filtrées et le chemin du fichier source
                    await _parentViewModel.ProcessImportFromPreviewWithData(FacturesImportees ?? new ObservableCollection<Sage100FacturePreview>(), _sourceFilePath ?? "");
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

        [RelayCommand]
        private void ShowDoublonFactures()
        {
            CurrentFilter = "Doublons";
            ApplyFilters();
        }

        #endregion
    }
}
