using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Application.UseCases.GestionClients;
using FNEV4.Application.DTOs.GestionClients;
using FNEV4.Core.Interfaces;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace FNEV4.Presentation.ViewModels.GestionClients
{
    public partial class ImportClientsViewModel : ObservableObject
    {
        private readonly ImportClientsExcelUseCase _importUseCase;
        private readonly ILoggingService _loggingService;

        public ImportClientsViewModel(
            ImportClientsExcelUseCase importUseCase,
            ILoggingService loggingService)
        {
            _importUseCase = importUseCase ?? throw new ArgumentNullException(nameof(importUseCase));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

            InitializeViewModel();
        }

        #region Properties

        [ObservableProperty]
        private string _selectedFilePath = string.Empty;

        [ObservableProperty]
        private bool _isFileSelected;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private bool _hasPreview;

        [ObservableProperty]
        private bool _hasResults;

        [ObservableProperty]
        private ClientImportPreviewDto? _preview;

        [ObservableProperty]
        private ClientImportResultDto? _importResult;

        [ObservableProperty]
        private ClientImportOptionsDto _importOptions = new();

        [ObservableProperty]
        private ObservableCollection<ClientImportErrorDto> _importErrors = new();

        [ObservableProperty]
        private string _statusMessage = "Sélectionnez un fichier Excel (.xlsx) contenant les clients à importer";

        [ObservableProperty]
        private string _progressText = string.Empty;

        [ObservableProperty]
        private int _progressValue;

        [ObservableProperty]
        private bool _showProgress;

        // Options d'import
        public bool IgnoreDuplicates
        {
            get => ImportOptions.IgnoreDuplicates;
            set { ImportOptions.IgnoreDuplicates = value; OnPropertyChanged(); }
        }

        public bool UpdateExisting
        {
            get => ImportOptions.UpdateExisting;
            set { ImportOptions.UpdateExisting = value; OnPropertyChanged(); }
        }

        public bool ValidateOnly
        {
            get => ImportOptions.ValidateOnly;
            set { ImportOptions.ValidateOnly = value; OnPropertyChanged(); }
        }

        public int MaxErrors
        {
            get => ImportOptions.MaxErrors;
            set { ImportOptions.MaxErrors = value; OnPropertyChanged(); }
        }

        // UI Properties
        public string WindowTitle => "Import clients Excel";
        public string HeaderTitle => "Import en masse";
        public string HeaderSubtitle => "Importer des clients depuis un fichier Excel (.xlsx)";
        public bool CanImport => IsFileSelected && HasPreview && !IsProcessing;
        public string ImportButtonText => ValidateOnly ? "Valider uniquement" : "Importer les clients";

        #endregion

        #region Commands

        [RelayCommand]
        private void SelectFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Sélectionner le fichier Excel des clients",
                Filter = "Fichiers Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Tous les fichiers (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedFilePath = openFileDialog.FileName;
                IsFileSelected = true;
                HasPreview = false;
                HasResults = false;
                StatusMessage = $"Fichier sélectionné: {Path.GetFileName(SelectedFilePath)}";
                
                // Générer automatiquement l'aperçu
                _ = Task.Run(async () => await GeneratePreviewAsync());
            }
        }

        [RelayCommand]
        private async Task GeneratePreviewAsync()
        {
            if (!IsFileSelected) return;

            try
            {
                IsProcessing = true;
                ShowProgress = true;
                ProgressText = "Analyse du fichier Excel...";
                ProgressValue = 30;

                Preview = await _importUseCase.PreviewFileAsync(SelectedFilePath);
                HasPreview = true;

                ProgressValue = 100;
                ProgressText = $"Aperçu généré: {Preview.TotalRows} lignes détectées";

                await Task.Delay(1000); // Afficher le résultat brièvement
                ShowProgress = false;

                StatusMessage = $"Aperçu: {Preview.ValidRows} lignes valides, {Preview.ErrorRows} erreurs, {Preview.DuplicateRows} doublons";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur lors de l'analyse: {ex.Message}";
                await _loggingService.LogErrorAsync($"Erreur aperçu import: {ex.Message}", ex);
                ShowProgress = false;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private async Task ExecuteImportAsync()
        {
            if (!CanImport) return;

            try
            {
                IsProcessing = true;
                ShowProgress = true;
                HasResults = false;
                ImportErrors.Clear();

                ProgressText = ValidateOnly ? "Validation en cours..." : "Import en cours...";
                ProgressValue = 0;

                // Configuration des options
                ImportOptions.ImportedBy = Environment.UserName;

                // Progression simulation
                for (int i = 0; i <= 50; i += 10)
                {
                    ProgressValue = i;
                    await Task.Delay(100);
                }

                // Exécution de l'import
                ImportResult = await _importUseCase.ExecuteImportAsync(SelectedFilePath, ImportOptions);
                
                ProgressValue = 100;
                ProgressText = ValidateOnly ? "Validation terminée" : "Import terminé";

                // Afficher les résultats
                HasResults = true;
                
                // Ajouter les erreurs à la collection
                foreach (var error in ImportResult.RowErrors)
                {
                    ImportErrors.Add(error);
                }

                // Message de statut
                StatusMessage = ImportResult.IsSuccess 
                    ? $"Succès: {ImportResult.GetSummary()}"
                    : $"Échec: {ImportResult.ErrorCount} erreurs détectées";

                await Task.Delay(2000);
                ShowProgress = false;

                await _loggingService.LogInformationAsync($"Import Excel terminé: {ImportResult.GetSummary()}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur critique: {ex.Message}";
                await _loggingService.LogErrorAsync($"Erreur import Excel: {ex.Message}", ex);
                ShowProgress = false;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void DownloadTemplate()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Enregistrer le modèle Excel",
                Filter = "Fichiers Excel (*.xlsx)|*.xlsx",
                FileName = "modele_import_clients.xlsx",
                DefaultExt = "xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Copier le modèle depuis les ressources
                    var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "templates", "modele_import_clients.csv");
                    
                    if (File.Exists(templatePath))
                    {
                        File.Copy(templatePath, saveFileDialog.FileName, true);
                        StatusMessage = $"Modèle sauvegardé: {Path.GetFileName(saveFileDialog.FileName)}";
                    }
                    else
                    {
                        // Créer un modèle à la volée si le fichier n'existe pas
                        CreateTemplateFile(saveFileDialog.FileName);
                        StatusMessage = $"Modèle créé: {Path.GetFileName(saveFileDialog.FileName)}";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Erreur création modèle: {ex.Message}";
                    _ = _loggingService.LogErrorAsync($"Erreur téléchargement modèle: {ex.Message}", ex);
                }
            }
        }

        [RelayCommand]
        private void Reset()
        {
            SelectedFilePath = string.Empty;
            IsFileSelected = false;
            HasPreview = false;
            HasResults = false;
            Preview = null;
            ImportResult = null;
            ImportErrors.Clear();
            ShowProgress = false;
            StatusMessage = "Sélectionnez un fichier Excel (.xlsx) contenant les clients à importer";
            
            // Reset options
            ImportOptions = new ClientImportOptionsDto();
            OnPropertyChanged(nameof(IgnoreDuplicates));
            OnPropertyChanged(nameof(UpdateExisting));
            OnPropertyChanged(nameof(ValidateOnly));
            OnPropertyChanged(nameof(MaxErrors));
        }

        [RelayCommand]
        private void Close()
        {
            // Fermer la fenêtre
            if (System.Windows.Application.Current.MainWindow is System.Windows.Window mainWindow)
            {
                foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                {
                    if (window.DataContext == this)
                    {
                        window.Close();
                        break;
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void InitializeViewModel()
        {
            ImportOptions = new ClientImportOptionsDto
            {
                IgnoreDuplicates = true,
                UpdateExisting = false,
                ValidateOnly = false,
                MaxErrors = 100,
                ImportedBy = Environment.UserName
            };
        }

        private void CreateTemplateFile(string filePath)
        {
            var headers = new[]
            {
                "Code Client", "Nom/Raison Sociale", "Type Client", "NCC", "Nom Commercial",
                "Adresse", "Ville", "Code Postal", "Pays", "Téléphone", "Email", 
                "Représentant", "N° Fiscal", "Devise", "Actif", "Notes"
            };

            var sampleData = new[]
            {
                "CLI001,ARTHUR LE GRAND SARL,Entreprise,1234567890A,Arthur Le Grand,123 Boulevard de la Paix,Abidjan,01001,Côte d'Ivoire,+225 01 02 03 04,arthur@legrand.ci,Jean KOUAME,TIN123456,XOF,Oui,Client VIP",
                "CLI002,MARIE KOUASSI,Particulier,,Marie Kouassi Boutique,45 Rue du Commerce,Bouaké,02001,Côte d'Ivoire,+225 05 06 07 08,marie.kouassi@gmail.com,Paul DIALLO,,XOF,Oui,"
            };

            var csvContent = string.Join(",", headers) + Environment.NewLine + 
                           string.Join(Environment.NewLine, sampleData);

            File.WriteAllText(filePath, csvContent, System.Text.Encoding.UTF8);
        }

        #endregion

        #region Property Changed Handlers

        partial void OnIsFileSelectedChanged(bool value)
        {
            OnPropertyChanged(nameof(CanImport));
        }

        partial void OnHasPreviewChanged(bool value)
        {
            OnPropertyChanged(nameof(CanImport));
        }

        partial void OnIsProcessingChanged(bool value)
        {
            OnPropertyChanged(nameof(CanImport));
        }

        #endregion
    }
}
