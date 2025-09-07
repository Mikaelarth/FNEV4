using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Application.Services.ImportTraitement;
using FNEV4.Core.Models.ImportTraitement;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FNEV4.Presentation.ViewModels.ImportTraitement
{
    /// <summary>
    /// ViewModel pour l'import Sage 100 v15
    /// </summary>
    public partial class Sage100ImportViewModel : ObservableObject
    {
        private readonly ISage100ImportService _sage100ImportService;

        // Référence à la fenêtre pour pouvoir la fermer
        public Window? ParentWindow { get; set; }

        #region Properties

        [ObservableProperty]
        private string _selectedFilePath = string.Empty;

        [ObservableProperty]
        private bool _isProcessing = false;

        [ObservableProperty]
        private bool _hasValidationResult = false;

        [ObservableProperty]
        private bool _hasPreviewData = false;

        [ObservableProperty]
        private bool _hasImportResult = false;

        [ObservableProperty]
        private bool _hasDetailedResults = false;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        [ObservableProperty]
        private string _validationDetails = string.Empty;

        [ObservableProperty]
        private string _validationIcon = "Information";

        [ObservableProperty]
        private Brush _validationColor = new SolidColorBrush(Colors.Blue);

        [ObservableProperty]
        private string _importResultMessage = string.Empty;

        [ObservableProperty]
        private string _importResultDetails = string.Empty;

        [ObservableProperty]
        private string _importResultIcon = "CheckCircle";

        [ObservableProperty]
        private Brush _importResultColor = new SolidColorBrush(Colors.Green);

        public ObservableCollection<Sage100FacturePreview> PreviewFactures { get; } = new();
        public ObservableCollection<Sage100FactureImportee> ImportedFactures { get; } = new();

        // Collection filtrée pour la vue
        private ICollectionView? _previewFacturesView;
        public ICollectionView PreviewFacturesView
        {
            get
            {
                if (_previewFacturesView == null)
                {
                    _previewFacturesView = CollectionViewSource.GetDefaultView(PreviewFactures);
                    _previewFacturesView.Filter = FilterFactures;
                }
                return _previewFacturesView;
            }
        }

        // Propriétés pour la recherche et le filtrage
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    PreviewFacturesView?.Refresh();
                }
            }
        }

        private string _filterEtat = "All";
        public string FilterEtat
        {
            get => _filterEtat;
            set
            {
                if (SetProperty(ref _filterEtat, value))
                {
                    PreviewFacturesView?.Refresh();
                }
            }
        }

        private string _filterPaiement = "All";
        public string FilterPaiement
        {
            get => _filterPaiement;
            set
            {
                if (SetProperty(ref _filterPaiement, value))
                {
                    PreviewFacturesView?.Refresh();
                }
            }
        }

        public bool HasSelectedFile => !string.IsNullOrEmpty(SelectedFilePath);
        public bool CanImport => HasValidationResult && PreviewFactures.Any(f => f.EstValide);
        public bool CanExecuteImport => CanImport && !IsProcessing;

        private Sage100ValidationResult? _lastValidation;
        private Sage100ImportResult? _lastImportResult;

        #endregion

        #region Constructor

        public Sage100ImportViewModel(ISage100ImportService sage100ImportService)
        {
            _sage100ImportService = sage100ImportService;
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void SelectFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Sélectionner un fichier Excel Sage 100 v15",
                Filter = "Fichiers Excel (*.xlsx)|*.xlsx|Tous les fichiers (*.*)|*.*",
                FilterIndex = 1,
                CheckFileExists = true,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedFilePath = openFileDialog.FileName;
                
                // Reset des résultats précédents
                ResetResults();
                
                OnPropertyChanged(nameof(HasSelectedFile));
                OnPropertyChanged(nameof(CanImport));
                OnPropertyChanged(nameof(CanExecuteImport));
            }
        }

        [RelayCommand]
        private async Task ValidateFile()
        {
            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                MessageBox.Show("Veuillez sélectionner un fichier Excel.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Reset des résultats précédents
            ResetResults();
            IsProcessing = true;
            
            try
            {
                // Affichage du message de début de validation
                ValidationMessage = "Validation en cours...";
                ValidationDetails = "Vérification de la structure du fichier";
                ValidationIcon = "Loading";
                ValidationColor = new SolidColorBrush(Colors.Orange);
                HasValidationResult = true;
                
                // Validation de la structure avec un petit délai pour permettre à l'UI de se mettre à jour
                await Task.Delay(100);
                _lastValidation = await _sage100ImportService.ValidateFileStructureAsync(SelectedFilePath);
                
                UpdateValidationUI(_lastValidation);
                
                if (_lastValidation.IsValid)
                {
                    // Mise à jour du message pour la génération de l'aperçu
                    ValidationMessage = "Génération de l'aperçu...";
                    ValidationDetails = "Analyse des factures et validation des données";
                    
                    // Générer l'aperçu
                    var preview = await _sage100ImportService.PreviewFileAsync(SelectedFilePath);
                    
                    PreviewFactures.Clear();
                    foreach (var facture in preview.Apercu)
                    {
                        PreviewFactures.Add(facture);
                    }
                    
                    // Mettre à jour la vue filtrée
                    PreviewFacturesView?.Refresh();
                    
                    HasPreviewData = PreviewFactures.Count > 0;
                    
                    if (HasPreviewData)
                    {
                        var validFactures = PreviewFactures.Count(f => f.EstValide);
                        var invalidFactures = PreviewFactures.Count - validFactures;
                        
                        // Message de succès détaillé
                        ValidationMessage = "Validation terminée";
                        ValidationDetails = $"✅ {validFactures} facture(s) valide(s)";
                        if (invalidFactures > 0)
                        {
                            ValidationDetails += $" | ⚠️ {invalidFactures} facture(s) avec erreurs";
                        }
                        ValidationDetails += $" | 📄 Total: {PreviewFactures.Count} facture(s)";
                        ValidationIcon = "CheckCircle";
                        ValidationColor = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        ValidationMessage = "Aucune facture détectée";
                        ValidationDetails = "Le fichier ne contient aucune donnée de facture valide";
                        ValidationIcon = "AlertCircle";
                        ValidationColor = new SolidColorBrush(Colors.Orange);
                    }
                }
                
                HasValidationResult = true;
                OnPropertyChanged(nameof(CanImport));
                OnPropertyChanged(nameof(CanExecuteImport));
            }
            catch (Exception ex)
            {
                // Gestion d'erreur améliorée avec plus de détails
                var errorMessage = $"Erreur lors de la validation du fichier :\n\n{ex.Message}";
                
                // Ajouter des suggestions selon le type d'erreur
                if (ex.Message.Contains("fichier") || ex.Message.Contains("access") || ex.Message.Contains("file"))
                {
                    errorMessage += "\n\n💡 Suggestions :\n• Vérifiez que le fichier n'est pas ouvert dans Excel\n• Assurez-vous d'avoir les droits de lecture sur le fichier\n• Vérifiez que le fichier n'est pas corrompu";
                }
                else if (ex.Message.Contains("structure") || ex.Message.Contains("format"))
                {
                    errorMessage += "\n\n💡 Suggestions :\n• Vérifiez que le fichier respecte le format Sage 100 v15\n• Assurez-vous que la structure '1 feuille = 1 facture' est respectée\n• Vérifiez que les colonnes requises sont présentes";
                }
                
                var result = MessageBox.Show($"{errorMessage}\n\nVoulez-vous réessayer avec un autre fichier ?", 
                                           "Erreur de validation", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Error);
                
                ValidationMessage = "❌ Erreur de validation";
                ValidationDetails = $"Échec : {ex.Message}";
                ValidationIcon = "AlertCircle";
                ValidationColor = new SolidColorBrush(Colors.Red);
                HasValidationResult = true;
                
                // Si l'utilisateur veut réessayer, ouvrir la sélection de fichier
                if (result == MessageBoxResult.Yes)
                {
                    await Task.Delay(100); // Petit délai pour l'UI
                    SelectFile();
                }
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private async Task Import()
        {
            if (!CanExecuteImport)
            {
                MessageBox.Show("Impossible d'effectuer l'import. Vérifiez que le fichier est valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var validFactures = PreviewFactures.Count(f => f.EstValide);
            var result = MessageBox.Show(
                $"Confirmer l'import de {validFactures} facture(s) Sage 100 v15 ?\n\n" +
                "Cette opération va :\n" +
                "• Traiter chaque feuille Excel comme une facture\n" +
                "• Gérer les clients divers (code 1999) et normaux\n" +
                "• Valider les moyens de paiement A18\n" +
                "• Intégrer les données en base\n\n" +
                "Continuer ?",
                "Confirmation d'import",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsProcessing = true;
            try
            {
                _lastImportResult = await _sage100ImportService.ImportSage100FileAsync(SelectedFilePath);
                
                UpdateImportResultUI(_lastImportResult);
                
                ImportedFactures.Clear();
                foreach (var facture in _lastImportResult.FacturesDetaillees)
                {
                    ImportedFactures.Add(facture);
                }
                
                HasDetailedResults = ImportedFactures.Count > 0;
                HasImportResult = true;
                
                // Notification selon le résultat
                if (_lastImportResult.IsSuccess)
                {
                    if (_lastImportResult.FacturesEchouees == 0)
                    {
                        MessageBox.Show(
                            $"Import réussi !\n\n" +
                            $"✅ {_lastImportResult.FacturesImportees} facture(s) importée(s)\n" +
                            $"⏱️ Durée : {_lastImportResult.DureeTraitement.TotalSeconds:F1}s",
                            "Import terminé",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Import partiellement réussi\n\n" +
                            $"✅ {_lastImportResult.FacturesImportees} facture(s) importée(s)\n" +
                            $"❌ {_lastImportResult.FacturesEchouees} facture(s) échouée(s)\n" +
                            $"⏱️ Durée : {_lastImportResult.DureeTraitement.TotalSeconds:F1}s\n\n" +
                            "Consultez les détails pour plus d'informations.",
                            "Import terminé",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show(
                        $"Échec de l'import\n\n" +
                        $"❌ {_lastImportResult.Message}\n\n" +
                        "Consultez les détails pour plus d'informations.",
                        "Échec d'import",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'import :\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                
                ImportResultMessage = "Erreur d'import";
                ImportResultDetails = ex.Message;
                ImportResultIcon = "AlertCircle";
                ImportResultColor = new SolidColorBrush(Colors.Red);
                HasImportResult = true;
            }
            finally
            {
                IsProcessing = false;
                OnPropertyChanged(nameof(CanExecuteImport));
            }
        }

        [RelayCommand]
        private void GoBack()
        {
            // Fermer la fenêtre pour retourner au menu principal
            try
            {
                ParentWindow?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la fermeture : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void CloseWindow()
        {
            // Commande explicite pour fermer la fenêtre
            try
            {
                ParentWindow?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la fermeture : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void ShowHelp()
        {
            MessageBox.Show(
                "Aide - Import Sage 100 v15\n\n" +
                "Structure attendue :\n" +
                "• 1 feuille Excel = 1 facture\n" +
                "• En-tête en colonne A (lignes fixes)\n" +
                "• Produits à partir de ligne 20\n\n" +
                "Cellules obligatoires :\n" +
                "• A3 : Numéro de facture\n" +
                "• A5 : Code client (1999 = divers)\n" +
                "• A8 : Date facture\n" +
                "• A18 : Moyen de paiement A18\n\n" +
                "Moyens de paiement A18 :\n" +
                "cash, card, mobile-money, bank-transfer, check, credit\n\n" +
                "Clients divers (code 1999) :\n" +
                "• A13 : Nom réel du client\n" +
                "• A15 : NCC spécifique",
                "Aide - Import Sage 100 v15",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion

        #region Private Methods

        private void ResetResults()
        {
            HasValidationResult = false;
            HasPreviewData = false;
            HasImportResult = false;
            HasDetailedResults = false;
            
            PreviewFactures.Clear();
            ImportedFactures.Clear();
            
            _lastValidation = null;
            _lastImportResult = null;
        }

        private void UpdateValidationUI(Sage100ValidationResult validation)
        {
            if (validation.IsValid)
            {
                ValidationMessage = "Fichier valide";
                ValidationIcon = "CheckCircle";
                ValidationColor = new SolidColorBrush(Colors.Green);
                ValidationDetails = $"{validation.NomsFeuillesValides.Count} feuille(s) valide(s) détectée(s)";
            }
            else
            {
                ValidationMessage = "Fichier invalide";
                ValidationIcon = "AlertCircle";
                ValidationColor = new SolidColorBrush(Colors.Red);
                ValidationDetails = string.Join(", ", validation.Errors.Take(3));
                if (validation.Errors.Count > 3)
                {
                    ValidationDetails += $" et {validation.Errors.Count - 3} autre(s) erreur(s)";
                }
            }
        }

        private void UpdateImportResultUI(Sage100ImportResult result)
        {
            if (result.IsSuccess)
            {
                if (result.FacturesEchouees == 0)
                {
                    ImportResultMessage = "Import réussi";
                    ImportResultIcon = "CheckCircle";
                    ImportResultColor = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    ImportResultMessage = "Import partiellement réussi";
                    ImportResultIcon = "AlertCircle";
                    ImportResultColor = new SolidColorBrush(Colors.Orange);
                }
                
                ImportResultDetails = $"{result.FacturesImportees} facture(s) importée(s)";
                if (result.FacturesEchouees > 0)
                {
                    ImportResultDetails += $", {result.FacturesEchouees} échec(s)";
                }
                ImportResultDetails += $" en {result.DureeTraitement.TotalSeconds:F1}s";
            }
            else
            {
                ImportResultMessage = "Échec d'import";
                ImportResultIcon = "CloseCircle";
                ImportResultColor = new SolidColorBrush(Colors.Red);
                ImportResultDetails = result.Message;
            }
        }



        private void ApplyFilters()
        {
            // Rafraîchit le filtre de la vue
            _previewFacturesView?.Refresh();
        }

        private bool FilterFactures(object item)
        {
            if (item is not Sage100FacturePreview facture) return false;

            // Filtre par recherche
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                if (!facture.NumeroFacture.ToLower().Contains(searchLower) &&
                    !facture.NomClient.ToLower().Contains(searchLower) &&
                    !facture.MontantEstime.ToString().Contains(searchLower) &&
                    !facture.MoyenPaiement.ToLower().Contains(searchLower))
                {
                    return false;
                }
            }

            // Filtre par état
            if (FilterEtat != "All")
            {
                switch (FilterEtat)
                {
                    case "Valid":
                        if (!facture.EstValide) return false;
                        break;
                    case "Error":
                        if (facture.EstValide) return false;
                        break;
                }
            }

            // Filtre par paiement
            if (FilterPaiement != "All")
            {
                switch (FilterPaiement)
                {
                    case "cash":
                        if (!facture.MoyenPaiement.ToLower().Contains("cash")) return false;
                        break;
                    case "default":
                        if (!facture.MoyenPaiement.ToLower().Contains("défaut")) return false;
                        break;
                    case "missing":
                        if (!facture.MoyenPaiement.ToLower().Contains("inexistant")) return false;
                        break;
                }
            }

            return true;
        }

        partial void OnSelectedFilePathChanged(string value)
        {
            OnPropertyChanged(nameof(HasSelectedFile));
            OnPropertyChanged(nameof(CanImport));
            OnPropertyChanged(nameof(CanExecuteImport));
        }

        #endregion
    }
}
