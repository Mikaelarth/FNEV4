using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Application.Services.ImportTraitement;
using FNEV4.Core.Models.ImportTraitement;
using FNEV4.Core.Interfaces;
using FNEV4.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
using System.Collections.Generic;

namespace FNEV4.Presentation.ViewModels.ImportTraitement
{
    /// <summary>
    /// ViewModel pour l'import Sage 100 v15
    /// </summary>
    public partial class Sage100ImportViewModel : ObservableObject
    {
        private readonly ISage100ImportService _sage100ImportService;
        private readonly IPathConfigurationService _pathService;
        private readonly FNEV4DbContext _context;

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
        private bool _hasScanResults = false;

        // Statistiques du dernier scan
        private int _lastScanTotalFiles = 0;
        private List<string> _lastScanErrors = new();

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

        // Intégration avec la configuration des dossiers
        public string ImportFolderPath => _pathService?.ImportFolderPath ?? "Non configuré";
        public string ExportFolderPath => _pathService?.ExportFolderPath ?? "Non configuré";
        public string ArchiveFolderPath => _pathService?.ArchiveFolderPath ?? "Non configuré";

        [ObservableProperty]
        private bool _autoArchiveEnabled = true;

        [ObservableProperty]
        private bool _hasConfiguredFolders = false;

        private Sage100ValidationResult? _lastValidation;
        private Sage100ImportResult? _lastImportResult;

        #endregion

        #region Constructor

        public Sage100ImportViewModel(ISage100ImportService sage100ImportService, IPathConfigurationService pathService, FNEV4DbContext context)
        {
            _sage100ImportService = sage100ImportService;
            _pathService = pathService;
            _context = context;
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
            System.Diagnostics.Debug.WriteLine("🚨 DÉBUT DE L'IMPORT - Les fichiers vont être déplacés !");
            
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
                
                // Post-traitement avec intégration des dossiers configurés
                if (_lastImportResult.IsSuccess && _lastImportResult.FacturesImportees > 0 && AutoArchiveEnabled)
                {
                    await ArchiveProcessedFile(SelectedFilePath, _lastImportResult);
                }
                
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
            // Fermer la fenêtre d'import si elle est ouverte
            var window = System.Windows.Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.Content is Views.ImportTraitement.Sage100ImportView);
            
            if (window != null)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        [RelayCommand]
        private void ShowHelp()
        {
            MessageBox.Show(
                "🔧 AIDE - IMPORT SAGE 100 V15\n\n" +
                "📋 STRUCTURE REQUISE :\n" +
                "• Format : 1 classeur Excel = N factures, 1 feuille = 1 facture\n" +
                "• Extension : .xlsx uniquement\n" +
                "• Encodage : UTF-8 recommandé\n\n" +
                "📊 DONNÉES PAR FEUILLE :\n" +
                "• A3 : Numéro de facture (obligatoire)\n" +
                "• A5 : Code client (1999 = divers, autre = normal)\n" +
                "• A6 : NCC client normal (si code ≠ 1999)\n" +
                "• A8 : Date facture (format Excel)\n" +
                "• A10 : Point de vente\n" +
                "• A11 : Intitulé client\n" +
                "• A13 : Nom réel client divers (si code = 1999)\n" +
                "• A15 : NCC client divers (si code = 1999)\n" +
                "• A17 : Numéro facture avoir (si applicable)\n" +
                "• A18 : Moyen de paiement (cash, card, mobile-money, etc.)\n\n" +
                "🛍️ PRODUITS (À partir de la ligne 20) :\n" +
                "• B : Code produit\n" +
                "• C : Désignation\n" +
                "• D : Prix unitaire\n" +
                "• E : Quantité\n" +
                "• F : Unité/Emballage\n" +
                "• G : Code TVA\n" +
                "• H : Montant HT\n\n" +
                "⚡ FONCTIONNALITÉS AVANCÉES :\n" +
                "• Validation automatique de la structure\n" +
                "• Aperçu détaillé avant import\n" +
                "• Gestion des erreurs par facture\n" +
                "• Support clients divers et normaux\n" +
                "• Calculs automatiques de TVA\n" +
                "• Archivage automatique des fichiers traités\n\n" +
                "📁 DOSSIERS CONFIGURÉS :\n" +
                "• Import : Fichiers source à traiter\n" +
                "• Archive : Fichiers traités avec succès\n" +
                "• Logs : Journaux détaillés des opérations\n\n" +
                "🚨 EN CAS DE PROBLÈME :\n" +
                "• Vérifiez la structure du fichier Excel\n" +
                "• Consultez les messages d'erreur détaillés\n" +
                "• Assurez-vous que les clients existent en base\n" +
                "• Vérifiez la configuration des dossiers\n\n" +
                "Pour plus d'aide, consultez la documentation technique.",
                "Aide - Import Sage 100 v15",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task ScanImportFolder()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 DÉBUT DU SCAN - Aucun traitement ne devrait être effectué");
                
                var importPath = _pathService.ImportFolderPath;
                
                if (!Directory.Exists(importPath))
                {
                    MessageBox.Show($"Le dossier d'import n'existe pas :\n{importPath}\n\nVeuillez configurer les chemins dans 'Configuration > Chemins & Dossiers'", 
                                  "Dossier introuvable", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                    return;
                }

                var excelFiles = Directory.GetFiles(importPath, "*.xlsx");
                System.Diagnostics.Debug.WriteLine($"🔍 SCAN: {excelFiles.Length} fichier(s) Excel trouvé(s)");
                
                // Stocker le nombre de fichiers trouvés pour l'aperçu
                _lastScanTotalFiles = excelFiles.Length;
                _lastScanErrors.Clear();
                
                if (excelFiles.Length == 0)
                {
                    MessageBox.Show($"Aucun fichier Excel trouvé dans :\n{importPath}", 
                                  "Dossier vide", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    return;
                }

                // AMÉLIORATION 1: Génération d'un aperçu détaillé des factures trouvées
                IsProcessing = true;
                ValidationMessage = "🔍 Analyse des fichiers en cours...";
                HasValidationResult = true;
                
                var allPreviews = new List<Sage100FacturePreview>();
                var validFilesCount = 0;
                var invalidFilesCount = 0;
                var totalInvoicesFound = 0;
                var validInvoicesFound = 0;
                var invalidInvoicesFound = 0;
                
                foreach (var file in excelFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(file);
                        ValidationDetails = $"📄 Analyse de {fileName}...";
                        System.Diagnostics.Debug.WriteLine($"🔍 SCAN: Analyse du fichier {fileName} (LECTURE SEULE)");
                        
                        var preview = await _sage100ImportService.PreviewFileAsync(file);
                        
                        if (preview.IsSuccess && preview.FacturesDetectees > 0)
                        {
                            validFilesCount++;
                            totalInvoicesFound += preview.FacturesDetectees;
                            
                            // Ajouter chaque facture à la liste d'aperçu avec nom du fichier
                            foreach (var facturePreview in preview.Apercu)
                            {
                                facturePreview.NomFichierSource = fileName;
                                allPreviews.Add(facturePreview);
                                
                                if (facturePreview.EstValide)
                                    validInvoicesFound++;
                                else
                                    invalidInvoicesFound++;
                            }
                        }
                        else
                        {
                            invalidFilesCount++;
                            // Ajouter une entrée d'erreur pour les fichiers non analysables
                            var errorMessage = preview.Errors?.Any() == true ? string.Join(", ", preview.Errors) : "Structure invalide ou fichier corrompu";
                            _lastScanErrors.Add($"{fileName}: {errorMessage}");
                            
                            allPreviews.Add(new Sage100FacturePreview
                            {
                                NomFeuille = "❌ ERREUR FICHIER",
                                NomFichierSource = fileName,
                                NumeroFacture = "N/A",
                                NomClient = "Fichier non analysable",
                                EstValide = false,
                                Erreurs = preview.Errors?.Any() == true ? preview.Errors : new List<string> { "Structure invalide ou fichier corrompu" }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        invalidFilesCount++;
                        var errorMessage = ex.Message.Length > 50 ? ex.Message.Substring(0, 50) + "..." : ex.Message;
                        _lastScanErrors.Add($"{Path.GetFileName(file)}: Exception - {ex.Message}");
                        
                        // Créer une entrée d'erreur pour les fichiers en exception
                        allPreviews.Add(new Sage100FacturePreview
                        {
                            NomFeuille = "🚨 EXCEPTION",
                            NomFichierSource = Path.GetFileName(file),
                            NumeroFacture = "ERROR",
                            NomClient = errorMessage,
                            EstValide = false,
                            Erreurs = { $"Exception: {ex.Message}" }
                        });
                    }
                }
                
                IsProcessing = false;
                
                // AMÉLIORATION 2: Affichage de l'aperçu enrichi avec statistiques détaillées
                PreviewFactures.Clear();
                foreach (var preview in allPreviews)
                {
                    PreviewFactures.Add(preview);
                }
                
                HasPreviewData = allPreviews.Count > 0;
                
                // AMÉLIORATION 3: Message de confirmation enrichi avec diagnostic complet
                var diagnosticMessage = $"📁 DIAGNOSTIC COMPLET - Scan du dossier d'import\n\n" +
                                      $"� Dossier: {importPath}\n\n" +
                                      $"📊 FICHIERS ANALYSÉS:\n" +
                                      $"└─ � Total fichiers Excel: {excelFiles.Length}\n" +
                                      $"└─ ✅ Fichiers valides: {validFilesCount}\n" +
                                      $"└─ ❌ Fichiers en erreur: {invalidFilesCount}\n\n" +
                                      $"📋 FACTURES DÉTECTÉES:\n" +
                                      $"└─ 🎯 Total factures trouvées: {totalInvoicesFound}\n" +
                                      $"└─ ✅ Factures valides: {validInvoicesFound}\n" +
                                      $"└─ ⚠️ Factures avec erreurs: {invalidInvoicesFound}\n\n";

                if (totalInvoicesFound == 0)
                {
                    ValidationMessage = "❌ Aucune facture valide détectée";
                    ValidationDetails = $"Aucune facture valide trouvée dans les {excelFiles.Length} fichier(s) Excel";
                    ValidationIcon = "AlertCircle";
                    ValidationColor = new SolidColorBrush(Colors.Orange);
                    HasScanResults = true; // Marquer que nous avons scanné même si aucune facture trouvée
                    
                    MessageBox.Show(diagnosticMessage + 
                                  "🚨 PROBLÈME DÉTECTÉ:\n" +
                                  "Aucune facture valide n'a été trouvée.\n\n" +
                                  "💡 VÉRIFICATIONS RECOMMANDÉES:\n" +
                                  "• Structure des fichiers Excel (1 feuille = 1 facture)\n" +
                                  "• Format Sage 100 v15 respecté\n" +
                                  "• Données obligatoires présentes (A3, A5, A8, etc.)\n" +
                                  "• Fichiers non corrompus\n\n" +
                                  "Consultez l'aperçu ci-dessous pour les détails des erreurs.", 
                                  "Aucune facture détectée", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                    return;
                }
                
                // Message de réussite avec options
                ValidationMessage = "✅ Scan terminé avec succès";
                ValidationDetails = $"{validInvoicesFound} facture(s) prête(s) pour import, {invalidInvoicesFound} avec erreur(s)";
                ValidationIcon = "CheckCircle";
                ValidationColor = new SolidColorBrush(Colors.Green);
                
                // AMÉLIORATION: Pas de popup - l'utilisateur clique maintenant sur "Aperçu" pour voir et importer
                HasValidationResult = true;
                HasScanResults = true; // Activer le bouton Aperçu
                
                // Message informatif dans l'interface
                ValidationDetails = $"📊 SCAN TERMINÉ: {validInvoicesFound} facture(s) valide(s) détectée(s)\n" +
                                  $"Cliquez sur 'Aperçu' pour examiner les détails et procéder à l'import.";
                
                System.Diagnostics.Debug.WriteLine($"🔍 FIN DU SCAN - Aucun fichier n'a été déplacé. {validInvoicesFound} facture(s) prête(s) pour import");
            }
            catch (Exception ex)
            {
                IsProcessing = false;
                
                ValidationMessage = "🚨 Erreur critique du scan";
                ValidationDetails = ex.Message;
                ValidationIcon = "AlertCircle";
                ValidationColor = new SolidColorBrush(Colors.Red);
                HasValidationResult = true;
                
                MessageBox.Show($"❌ ERREUR CRITIQUE lors du scan du dossier d'import:\n\n" +
                              $"Erreur: {ex.Message}\n\n" +
                              $"💡 ACTIONS RECOMMANDÉES:\n" +
                              $"• Vérifiez les permissions sur le dossier d'import\n" +
                              $"• Assurez-vous qu'aucun fichier n'est ouvert dans Excel\n" +
                              $"• Vérifiez l'espace disque disponible\n" +
                              $"• Consultez les logs système pour plus de détails", 
                              "Erreur Critique", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OpenConfiguredFolders()
        {
            try
            {
                var importPath = _pathService.ImportFolderPath;
                if (Directory.Exists(importPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", importPath);
                }
                else
                {
                    MessageBox.Show($"Le dossier d'import n'existe pas :\n{importPath}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir le dossier :\n{ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ShowPreview()
        {
            try
            {
                if (!HasScanResults || PreviewFactures.Count == 0)
                {
                    MessageBox.Show("Aucune donnée d'aperçu disponible. Veuillez d'abord scanner le dossier d'import.", 
                                  "Aperçu", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Création de la fenêtre d'aperçu
                var previewWindow = new Views.ImportTraitement.Sage100PreviewWindow();
                var previewViewModel = new Sage100PreviewViewModel(this); // Passer une référence de ce ViewModel
                
                // Chargement des données
                var previewResult = new Sage100PreviewResult
                {
                    IsSuccess = true,
                    FacturesDetectees = PreviewFactures.Count,
                    Apercu = PreviewFactures.ToList(),
                    Errors = _lastScanErrors
                };
                
                previewViewModel.LoadPreviewData(previewResult);
                
                previewWindow.DataContext = previewViewModel;
                previewWindow.Owner = System.Windows.Application.Current.MainWindow;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'affichage de l'aperçu :\n{ex.Message}", 
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Commande fusionnée pour scanner et afficher l'aperçu en une seule action
        /// Améliore l'UX en réduisant le nombre d'étapes pour l'utilisateur
        /// </summary>
        [RelayCommand]
        private async Task ScanAndPreview()
        {
            try
            {
                var importPath = _pathService.ImportFolderPath;
                
                if (!Directory.Exists(importPath))
                {
                    MessageBox.Show($"Le dossier d'import n'existe pas :\n{importPath}\n\nVeuillez configurer les chemins dans 'Configuration > Chemins & Dossiers'", 
                                  "Dossier introuvable", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                    return;
                }

                var excelFiles = Directory.GetFiles(importPath, "*.xlsx");
                
                if (excelFiles.Length == 0)
                {
                    MessageBox.Show($"Aucun fichier Excel trouvé dans :\n{importPath}", 
                                  "Dossier vide", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    return;
                }

                // Phase 1: Scanner (même logique que ScanImportFolder mais silencieux)
                IsProcessing = true;
                ValidationMessage = "🔍 Analyse des fichiers en cours...";
                HasValidationResult = true;
                
                var allPreviews = new List<Sage100FacturePreview>();
                var validFilesCount = 0;
                var invalidFilesCount = 0;
                var totalInvoicesFound = 0;
                var validInvoicesFound = 0;
                var invalidInvoicesFound = 0;
                
                _lastScanTotalFiles = excelFiles.Length;
                _lastScanErrors.Clear();
                
                foreach (var file in excelFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(file);
                        ValidationDetails = $"📄 Analyse de {fileName}...";
                        
                        var preview = await _sage100ImportService.PreviewFileAsync(file);
                        
                        if (preview.IsSuccess && preview.FacturesDetectees > 0)
                        {
                            validFilesCount++;
                            totalInvoicesFound += preview.FacturesDetectees;
                            
                            // Ajouter chaque facture à la liste d'aperçu avec nom du fichier
                            foreach (var facturePreview in preview.Apercu)
                            {
                                facturePreview.NomFichierSource = fileName;
                                allPreviews.Add(facturePreview);
                                
                                if (facturePreview.EstValide)
                                    validInvoicesFound++;
                                else
                                    invalidInvoicesFound++;
                            }
                        }
                        else
                        {
                            invalidFilesCount++;
                            var errorMessage = preview.Errors?.Any() == true ? string.Join(", ", preview.Errors) : "Structure invalide ou fichier corrompu";
                            _lastScanErrors.Add($"{fileName}: {errorMessage}");
                            
                            allPreviews.Add(new Sage100FacturePreview
                            {
                                NomFeuille = "❌ ERREUR FICHIER",
                                NomFichierSource = fileName,
                                NumeroFacture = "N/A",
                                NomClient = "Fichier non analysable",
                                EstValide = false,
                                Erreurs = preview.Errors?.Any() == true ? preview.Errors : new List<string> { "Structure invalide ou fichier corrompu" }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        invalidFilesCount++;
                        var errorMessage = ex.Message.Length > 50 ? ex.Message.Substring(0, 50) + "..." : ex.Message;
                        _lastScanErrors.Add($"{Path.GetFileName(file)}: Exception - {ex.Message}");
                        
                        allPreviews.Add(new Sage100FacturePreview
                        {
                            NomFeuille = "🚨 EXCEPTION",
                            NomFichierSource = Path.GetFileName(file),
                            NumeroFacture = "ERROR",
                            NomClient = errorMessage,
                            EstValide = false,
                            Erreurs = { $"Exception: {ex.Message}" }
                        });
                    }
                }
                
                IsProcessing = false;
                
                // Charger les données d'aperçu
                PreviewFactures.Clear();
                foreach (var preview in allPreviews)
                {
                    PreviewFactures.Add(preview);
                }
                
                HasPreviewData = allPreviews.Count > 0;
                HasScanResults = true;
                
                if (totalInvoicesFound == 0)
                {
                    ValidationMessage = "❌ Aucune facture valide détectée";
                    ValidationDetails = $"Aucune facture valide trouvée dans les {excelFiles.Length} fichier(s) Excel";
                    ValidationIcon = "AlertCircle";
                    ValidationColor = new SolidColorBrush(Colors.Orange);
                    
                    // Toujours afficher l'aperçu pour montrer les erreurs détaillées
                    // L'utilisateur peut voir pourquoi les fichiers n'ont pas été traités
                }
                
                // Phase 2: Afficher directement l'aperçu dans tous les cas
                if (totalInvoicesFound > 0)
                {
                    ValidationMessage = "✅ Analyse terminée - Ouverture de l'aperçu";
                    ValidationDetails = $"{validInvoicesFound} facture(s) prête(s) pour import";
                    ValidationIcon = "CheckCircle";
                    ValidationColor = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    ValidationMessage = "⚠️ Analyse terminée - Aperçu des erreurs";
                    ValidationDetails = $"Aucune facture valide - Consultez l'aperçu pour voir les erreurs détaillées";
                    ValidationIcon = "AlertCircle";
                    ValidationColor = new SolidColorBrush(Colors.Orange);
                }
                
                // Créer et afficher la fenêtre d'aperçu dans tous les cas
                var previewWindow = new Views.ImportTraitement.Sage100PreviewWindow();
                var previewViewModel = new Sage100PreviewViewModel(this);
                
                var previewResult = new Sage100PreviewResult
                {
                    IsSuccess = totalInvoicesFound > 0,
                    FacturesDetectees = PreviewFactures.Count,
                    Apercu = PreviewFactures.ToList(),
                    Errors = _lastScanErrors
                };
                
                previewViewModel.LoadPreviewData(previewResult);
                
                previewWindow.DataContext = previewViewModel;
                previewWindow.Owner = System.Windows.Application.Current.MainWindow;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                IsProcessing = false;
                
                ValidationMessage = "🚨 Erreur lors de l'analyse";
                ValidationDetails = ex.Message;
                ValidationIcon = "AlertCircle";
                ValidationColor = new SolidColorBrush(Colors.Red);
                HasValidationResult = true;
                
                MessageBox.Show($"❌ ERREUR lors de l'analyse et affichage de l'aperçu:\n\n" +
                              $"Erreur: {ex.Message}\n\n" +
                              $"💡 Vérifiez les permissions et la configuration des dossiers.", 
                              "Erreur", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
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
                    !facture.MontantTTC.ToString().Contains(searchLower) &&
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

        private async Task ProcessAllFilesInImportFolderWithDiagnostic(string[] files)
        {
            IsProcessing = true;
            var startTime = DateTime.Now;
            var totalFiles = files.Length;
            var successCount = 0;
            var failureCount = 0;
            var totalInvoicesProcessed = 0;
            var totalInvoicesSaved = 0;
            var totalInvoicesFailed = 0;
            var detailedResults = new List<string>();
            var dbVerificationResults = new List<string>();

            try
            {
                ValidationMessage = "🚀 Import en cours...";
                ValidationIcon = "Loading";
                ValidationColor = new SolidColorBrush(Colors.Orange);
                
                for (int i = 0; i < files.Length; i++)
                {
                    var file = files[i];
                    var fileName = Path.GetFileName(file);
                    var fileIndex = i + 1;
                    
                    try
                    {
                        ValidationDetails = $"📄 Traitement {fileIndex}/{totalFiles}: {fileName}";
                        await Task.Delay(100); // Permettre mise à jour UI
                        
                        // Étape 1: Import du fichier
                        var result = await _sage100ImportService.ImportSage100FileAsync(file);
                        
                        if (result.IsSuccess && result.FacturesImportees > 0)
                        {
                            successCount++;
                            totalInvoicesProcessed += result.FacturesImportees;
                            totalInvoicesSaved += result.FacturesImportees;
                            
                            detailedResults.Add($"✅ {fileName}: {result.FacturesImportees} facture(s) importée(s)");
                            
                            // Étape 2: Vérification en base de données
                            ValidationDetails = $"🔍 Vérification BDD pour {fileName}...";
                            var dbCheck = await VerifyInvoicesInDatabase(result.FacturesDetaillees);
                            dbVerificationResults.Add($"📊 {fileName}: {dbCheck}");
                            
                            // Étape 3: Archivage
                            if (AutoArchiveEnabled)
                            {
                                ValidationDetails = $"📦 Archivage de {fileName}...";
                                await ArchiveProcessedFile(file, result);
                                detailedResults[detailedResults.Count - 1] += " (archivé)";
                            }
                        }
                        else
                        {
                            failureCount++;
                            totalInvoicesFailed += result.FacturesEchouees;
                            
                            var errorSummary = result.Errors.Count > 0 ? result.Errors[0] : "Erreur inconnue";
                            detailedResults.Add($"❌ {fileName}: ÉCHEC ({errorSummary})");
                            
                            await MoveToErrorFolder(file, result.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        totalInvoicesFailed++;
                        
                        detailedResults.Add($"🚨 {fileName}: EXCEPTION ({ex.Message})");
                        await MoveToErrorFolder(file, ex.Message);
                    }
                }

                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                
                // DIAGNOSTIC FINAL COMPLET
                var finalMessage = GenerateCompleteDiagnosticReport(
                    totalFiles, successCount, failureCount, 
                    totalInvoicesProcessed, totalInvoicesSaved, totalInvoicesFailed,
                    detailedResults, dbVerificationResults, duration);

                // Affichage selon le résultat
                if (successCount > 0)
                {
                    ValidationMessage = totalInvoicesFailed == 0 ? "✅ Import terminé avec succès" : "⚠️ Import partiellement réussi";
                    ValidationIcon = totalInvoicesFailed == 0 ? "CheckCircle" : "AlertCircle";
                    ValidationColor = new SolidColorBrush(totalInvoicesFailed == 0 ? Colors.Green : Colors.Orange);
                    ValidationDetails = $"{totalInvoicesSaved} facture(s) sauvegardée(s) en base de données";
                }
                else
                {
                    ValidationMessage = "❌ Échec complet de l'import";
                    ValidationIcon = "CloseCircle";
                    ValidationColor = new SolidColorBrush(Colors.Red);
                    ValidationDetails = "Aucune facture n'a pu être importée";
                }

                MessageBox.Show(finalMessage, 
                              "Rapport d'Import Détaillé", 
                              MessageBoxButton.OK, 
                              successCount > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            finally
            {
                IsProcessing = false;
                HasValidationResult = true;
                
                // Rafraîchir l'aperçu après traitement
                if (successCount > 0)
                {
                    await RefreshPreviewAfterImport();
                }
            }
        }

        private async Task<string> VerifyInvoicesInDatabase(List<Sage100FactureImportee> importedInvoices)
        {
            try
            {
                var verifiedCount = 0;
                var notFoundCount = 0;
                
                foreach (var invoice in importedInvoices.Where(i => i.EstImportee))
                {
                    // Recherche par numéro de facture dans la base
                    var dbInvoice = await _context.FneInvoices
                        .FirstOrDefaultAsync(f => f.InvoiceNumber == invoice.NumeroFacture);
                    
                    if (dbInvoice != null)
                        verifiedCount++;
                    else
                        notFoundCount++;
                }
                
                return $"{verifiedCount} trouvée(s) en BDD, {notFoundCount} introuvable(s)";
            }
            catch (Exception ex)
            {
                return $"Erreur vérification BDD: {ex.Message}";
            }
        }

        private string GenerateCompleteDiagnosticReport(
            int totalFiles, int successCount, int failureCount,
            int totalInvoicesProcessed, int totalInvoicesSaved, int totalInvoicesFailed,
            List<string> detailedResults, List<string> dbVerificationResults, TimeSpan duration)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("📋 RAPPORT D'IMPORT COMPLET");
            report.AppendLine($"⏱️ Durée totale: {duration.TotalSeconds:F1} secondes");
            report.AppendLine();
            
            report.AppendLine("📊 STATISTIQUES GLOBALES:");
            report.AppendLine($"└─ 📁 Fichiers traités: {totalFiles}");
            report.AppendLine($"└─ ✅ Succès: {successCount}");
            report.AppendLine($"└─ ❌ Échecs: {failureCount}");
            report.AppendLine($"└─ 📄 Factures traitées: {totalInvoicesProcessed}");
            report.AppendLine($"└─ 💾 Sauvegardées en BDD: {totalInvoicesSaved}");
            report.AppendLine($"└─ ⚠️ Échecs: {totalInvoicesFailed}");
            report.AppendLine();
            
            if (detailedResults.Count > 0)
            {
                report.AppendLine("🔍 DÉTAIL PAR FICHIER:");
                foreach (var result in detailedResults)
                {
                    report.AppendLine($"└─ {result}");
                }
                report.AppendLine();
            }
            
            if (dbVerificationResults.Count > 0)
            {
                report.AppendLine("💾 VÉRIFICATION BASE DE DONNÉES:");
                foreach (var verification in dbVerificationResults)
                {
                    report.AppendLine($"└─ {verification}");
                }
                report.AppendLine();
            }
            
            // Recommandations finales
            if (failureCount > 0)
            {
                report.AppendLine("💡 RECOMMANDATIONS:");
                report.AppendLine("└─ Consultez les fichiers d'erreur dans le dossier Archive/Erreurs");
                report.AppendLine("└─ Vérifiez les logs détaillés pour diagnostiquer les problèmes");
                report.AppendLine("└─ Corrigez les fichiers en erreur et relancez l'import");
            }
            
            if (totalInvoicesSaved > 0)
            {
                report.AppendLine("🎯 PROCHAINES ÉTAPES:");
                report.AppendLine("└─ Vérifiez les factures dans la base de données");
                report.AppendLine("└─ Procédez à la certification FNE si nécessaire");
                report.AppendLine("└─ Consultez les archives pour traçabilité");
            }
            
            return report.ToString();
        }

        private async Task RefreshPreviewAfterImport()
        {
            try
            {
                // Marquer les factures importées comme traitées dans l'aperçu
                foreach (var preview in PreviewFactures.Where(p => p.EstValide))
                {
                    preview.NomClient += " (✅ Importé)";
                }
                
                // Rafraîchir la vue
                PreviewFacturesView?.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur rafraîchissement aperçu: {ex.Message}");
            }
        }

        private async Task ArchiveProcessedFile(string filePath, Sage100ImportResult result)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var archiveFileName = $"{timestamp}_{result.FacturesImportees}factures_{fileName}";
                var archivePath = Path.Combine(_pathService.ArchiveFolderPath, archiveFileName);

                // Créer le dossier d'archive s'il n'existe pas
                Directory.CreateDirectory(_pathService.ArchiveFolderPath);

                // Déplacer le fichier vers l'archive
                File.Move(filePath, archivePath);

                // Créer un fichier de log associé
                var logFileName = Path.ChangeExtension(archiveFileName, ".log");
                var logPath = Path.Combine(_pathService.LogsFolderPath, logFileName);
                
                Directory.CreateDirectory(_pathService.LogsFolderPath);
                
                var logContent = $"Import automatique - {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                               $"Fichier source: {fileName}\n" +
                               $"Factures importées: {result.FacturesImportees}\n" +
                               $"Factures échouées: {result.FacturesEchouees}\n" +
                               $"Durée: {result.DureeTraitement.TotalSeconds:F1}s\n" +
                               $"Archivé vers: {archivePath}\n";

                if (result.Errors.Any())
                {
                    logContent += $"\nErreurs:\n{string.Join("\n", result.Errors)}";
                }

                await File.WriteAllTextAsync(logPath, logContent);
            }
            catch (Exception ex)
            {
                // Log silencieux - ne pas perturber le processus principal
                System.Diagnostics.Debug.WriteLine($"Erreur archivage : {ex.Message}");
            }
        }

        private async Task MoveToErrorFolder(string filePath, string errorMessage)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var errorFileName = $"{timestamp}_ERREUR_{fileName}";
                var errorFolderPath = Path.Combine(_pathService.ArchiveFolderPath, "Erreurs");
                var errorFilePath = Path.Combine(errorFolderPath, errorFileName);

                // Créer le dossier d'erreur
                Directory.CreateDirectory(errorFolderPath);

                // Déplacer le fichier en erreur
                File.Move(filePath, errorFilePath);

                // Créer un fichier d'erreur détaillé
                var errorLogPath = Path.ChangeExtension(errorFilePath, ".error.log");
                var errorLogContent = $"Erreur d'import - {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                    $"Fichier: {fileName}\n" +
                                    $"Erreur: {errorMessage}\n" +
                                    $"Fichier déplacé vers: {errorFilePath}\n";

                await File.WriteAllTextAsync(errorLogPath, errorLogContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur déplacement fichier erreur : {ex.Message}");
            }
        }

        /// <summary>
        /// Traite l'import depuis la fenêtre de prévisualisation
        /// </summary>
        public async Task ProcessImportFromPreview()
        {
            try
            {
                await Import();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'import :\n{ex.Message}", 
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
