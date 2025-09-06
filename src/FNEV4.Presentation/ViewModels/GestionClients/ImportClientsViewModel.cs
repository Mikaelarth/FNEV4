using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FNEV4.Application.UseCases.GestionClients;
using FNEV4.Application.DTOs.GestionClients;
using FNEV4.Core.Interfaces;
using FNEV4.Presentation.Messages;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

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

        [ObservableProperty]
        private bool _canCancelImport;

        private CancellationTokenSource? _cancellationTokenSource;

        // Options d'import
        public bool IgnoreDuplicates
        {
            get => ImportOptions.IgnoreDuplicates;
            set 
            { 
                ImportOptions.IgnoreDuplicates = value; 
                OnPropertyChanged();
                
                // Validation logique: si on active "Ignorer doublons" et que "Mettre à jour" est actif
                if (value && ImportOptions.UpdateExisting)
                {
                    ImportOptions.UpdateExisting = false;
                    OnPropertyChanged(nameof(UpdateExisting));
                    StatusMessage = "ℹ️ Mise à jour désactivée (incompatible avec ignorer doublons)";
                }
            }
        }

        public bool UpdateExisting
        {
            get => ImportOptions.UpdateExisting;
            set 
            { 
                ImportOptions.UpdateExisting = value; 
                OnPropertyChanged();
                
                // Validation logique: si on active "Mettre à jour" et que "Ignorer doublons" est actif
                if (value && ImportOptions.IgnoreDuplicates)
                {
                    ImportOptions.IgnoreDuplicates = false;
                    OnPropertyChanged(nameof(IgnoreDuplicates));
                    StatusMessage = "ℹ️ Ignorer doublons désactivé (incompatible avec mise à jour)";
                }
            }
        }

        public bool ValidateOnly
        {
            get => ImportOptions.ValidateOnly;
            set 
            { 
                ImportOptions.ValidateOnly = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImportButtonText));
                OnPropertyChanged(nameof(ImportIconKind));
            }
        }

        public int MaxErrors
        {
            get => ImportOptions.MaxErrors;
            set 
            { 
                // Validation: entre 1 et 10000
                var validatedValue = Math.Max(1, Math.Min(10000, value));
                if (validatedValue != value)
                {
                    // Feedback utilisateur si valeur corrigée
                    StatusMessage = validatedValue == 1 
                        ? "⚠️ Nombre d'erreurs minimum: 1" 
                        : "⚠️ Nombre d'erreurs maximum: 10000";
                }
                ImportOptions.MaxErrors = validatedValue; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(MaxErrorsDisplayText));
            }
        }

        public string MaxErrorsDisplayText => $"{MaxErrors} (min: 1, max: 10000)";

        // UI Properties
        public string WindowTitle => "Import clients Excel";
        public string HeaderTitle => "Import en masse";
        public string HeaderSubtitle => "Importer des clients depuis un fichier Excel (.xlsx)";
        public bool CanImport => IsFileSelected && !IsProcessing;
        public bool CanRefreshPreview => IsFileSelected && !IsProcessing;
        public string ImportButtonText => ValidateOnly ? "Valider uniquement" : "Importer les clients";
        public string ImportIconKind => ValidateOnly ? "CheckCircle" : "Import";

        #endregion

        #region Commands

        [RelayCommand]
        private void SelectFile()
        {
            try
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
                    var selectedFile = openFileDialog.FileName;

                    // Validation préalable du fichier
                    if (!ValidateSelectedFile(selectedFile))
                    {
                        return;
                    }

                    SelectedFilePath = selectedFile;
                    IsFileSelected = true;
                    HasPreview = false;
                    HasResults = false;
                    StatusMessage = $"Fichier sélectionné: {Path.GetFileName(SelectedFilePath)}";
                    
                    // Générer automatiquement l'aperçu
                    _ = Task.Run(async () => await GeneratePreviewAsync());
                }
            }
            catch (Exception ex)
            {
                _ = Task.Run(async () => await _loggingService.LogErrorAsync("Erreur lors de la sélection du fichier", ex, "GestionClients"));
                StatusMessage = "❌ Erreur lors de la sélection du fichier";
                IsFileSelected = false;
                HasPreview = false;
                HasResults = false;
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
                ProgressText = "Actualisation de l'aperçu...";
                ProgressValue = 10;

                // Réinitialiser l'aperçu existant
                HasPreview = false;
                Preview = null;

                ProgressValue = 30;
                ProgressText = "Analyse du fichier Excel...";

                Preview = await _importUseCase.PreviewFileAsync(SelectedFilePath);
                HasPreview = true;

                ProgressValue = 90;
                ProgressText = $"Aperçu actualisé: {Preview.TotalRows} lignes détectées";

                await Task.Delay(1500); // Afficher le résultat plus longtemps
                ProgressValue = 100;
                ShowProgress = false;

                StatusMessage = $"✅ Aperçu actualisé: {Preview.ValidRows} lignes valides, {Preview.ErrorRows} erreurs, {Preview.DuplicateRows} doublons";
                
                // Log de l'action utilisateur
                await _loggingService.LogInformationAsync($"Aperçu actualisé manuellement - Fichier: {System.IO.Path.GetFileName(SelectedFilePath)}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur lors de l'actualisation: {ex.Message}";
                await _loggingService.LogErrorAsync($"Erreur actualisation aperçu: {ex.Message}", ex);
                ShowProgress = false;
                HasPreview = false;
                Preview = null;
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

            // Demander confirmation avant import (sauf pour validation seule)
            if (!ValidateOnly && !await ConfirmImportAsync())
            {
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            try
            {
                IsProcessing = true;
                CanCancelImport = true;
                ShowProgress = true;
                HasResults = false;
                ImportErrors.Clear();

                var actionText = ValidateOnly ? "Validation" : "Import";
                ProgressText = $"{actionText} en cours...";
                ProgressValue = 0;

                // Configuration des options
                ImportOptions.ImportedBy = Environment.UserName;

                // Log de début d'opération
                await _loggingService.LogInformationAsync($"Début {actionText.ToLower()} Excel - Fichier: {System.IO.Path.GetFileName(SelectedFilePath)}");

                // Progression réaliste avec vérification d'annulation
                await UpdateProgressWithCancellation(10, "Préparation...", cancellationToken);
                await UpdateProgressWithCancellation(25, "Lecture du fichier Excel...", cancellationToken);
                
                // Exécution de l'import/validation
                ImportResult = await _importUseCase.ExecuteImportAsync(SelectedFilePath, ImportOptions);
                
                cancellationToken.ThrowIfCancellationRequested();
                
                await UpdateProgressWithCancellation(90, "Finalisation...", cancellationToken);
                
                ProgressValue = 100;
                ProgressText = $"{actionText} terminé avec succès";

                // Afficher les résultats
                HasResults = true;
                
                // Ajouter les erreurs à la collection
                foreach (var error in ImportResult.RowErrors)
                {
                    ImportErrors.Add(error);
                }

                // Message de statut avec plus de détails
                if (ImportResult.IsSuccess)
                {
                    StatusMessage = $"✅ {actionText} réussi: {ImportResult.ProcessedCount} lignes traitées, {ImportResult.SuccessCount} réussies";
                    
                    // Notifier que des clients ont été importés pour rafraîchir la liste
                    if (!ValidateOnly && ImportResult.SuccessCount > 0)
                    {
                        WeakReferenceMessenger.Default.Send(new ClientsImportedMessage(ImportResult.SuccessCount));
                    }
                }
                else
                {
                    StatusMessage = $"⚠️ {actionText} avec erreurs: {ImportResult.ErrorCount} erreurs sur {ImportResult.ProcessedCount} lignes";
                }

                await Task.Delay(2000, cancellationToken);
                ShowProgress = false;

                await _loggingService.LogInformationAsync($"{actionText} Excel terminé: {ImportResult.GetSummary()}");
            }
            catch (OperationCanceledException)
            {
                StatusMessage = $"❌ {(ValidateOnly ? "Validation" : "Import")} annulé par l'utilisateur";
                await _loggingService.LogInformationAsync($"{(ValidateOnly ? "Validation" : "Import")} Excel annulé");
                ShowProgress = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur critique: {ex.Message}";
                await _loggingService.LogErrorAsync($"Erreur import Excel: {ex.Message}", ex);
                ShowProgress = false;
            }
            finally
            {
                IsProcessing = false;
                CanCancelImport = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        [RelayCommand]
        private void CancelImport()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                StatusMessage = "🛑 Annulation en cours...";
            }
            catch (Exception ex)
            {
                _ = Task.Run(async () => await _loggingService.LogErrorAsync("Erreur lors de l'annulation", ex));
            }
        }

        [RelayCommand]
        private async Task DownloadTemplate()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Enregistrer le modèle Excel DGI",
                Filter = "Fichiers Excel (*.xlsx)|*.xlsx",
                FileName = "modele_import_clients_dgi.xlsx",
                DefaultExt = "xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    IsProcessing = true;
                    StatusMessage = "Génération du modèle Excel...";

                    // Utiliser le service Excel pour créer un vrai fichier Excel
                    await _importUseCase.ExportTemplateAsync(saveFileDialog.FileName);
                    
                    StatusMessage = $"Modèle Excel créé: {Path.GetFileName(saveFileDialog.FileName)}";
                    
                    // Optionnel: ouvrir le dossier contenant le fichier
                    var directory = Path.GetDirectoryName(saveFileDialog.FileName);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{saveFileDialog.FileName}\"");
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Erreur création modèle: {ex.Message}";
                    _ = _loggingService.LogErrorAsync($"Erreur téléchargement modèle: {ex.Message}", ex);
                }
                finally
                {
                    IsProcessing = false;
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

        [RelayCommand]
        private async Task ShowDetailedReportAsync()
        {
            if (Preview == null)
            {
                StatusMessage = "❌ Aucun aperçu disponible pour générer un rapport";
                return;
            }

            try
            {
                var reportContent = GenerateDetailedReport();
                
                // Afficher dans une nouvelle fenêtre ou sauvegarder
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Sauvegarder le rapport d'analyse",
                    Filter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
                    FileName = $"Rapport_Analyse_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await System.IO.File.WriteAllTextAsync(saveDialog.FileName, reportContent);
                    StatusMessage = $"✅ Rapport sauvegardé: {System.IO.Path.GetFileName(saveDialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                _ = Task.Run(async () => await _loggingService.LogErrorAsync("Erreur lors de la génération du rapport", ex, "GestionClients"));
                StatusMessage = "❌ Erreur lors de la génération du rapport";
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

            // Validation des valeurs par défaut
            ValidateImportOptions();
        }

        /// <summary>
        /// Valide et corrige les options d'import si nécessaire
        /// </summary>
        private void ValidateImportOptions()
        {
            // Validation du nombre maximum d'erreurs
            if (ImportOptions.MaxErrors < 1)
            {
                ImportOptions.MaxErrors = 1;
                StatusMessage = "⚠️ Nombre d'erreurs minimum ajusté à 1";
            }
            else if (ImportOptions.MaxErrors > 10000)
            {
                ImportOptions.MaxErrors = 10000;
                StatusMessage = "⚠️ Nombre d'erreurs maximum ajusté à 10000";
            }

            // Validation logique des options
            if (ImportOptions.UpdateExisting && ImportOptions.IgnoreDuplicates)
            {
                // Si on met à jour, on ne peut pas ignorer les doublons en même temps
                StatusMessage = "ℹ️ Options configurées: mise à jour activée, ignorer doublons désactivé";
            }
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
            OnPropertyChanged(nameof(CanRefreshPreview));
        }

        partial void OnHasPreviewChanged(bool value)
        {
            OnPropertyChanged(nameof(CanImport));
        }

        partial void OnIsProcessingChanged(bool value)
        {
            OnPropertyChanged(nameof(CanImport));
            OnPropertyChanged(nameof(CanRefreshPreview));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Valide le fichier sélectionné avant traitement.
        /// </summary>
        /// <param name="filePath">Chemin du fichier à valider</param>
        /// <returns>True si le fichier est valide, False sinon</returns>
        private bool ValidateSelectedFile(string filePath)
        {
            try
            {
                // Vérification que le fichier existe
                if (!File.Exists(filePath))
                {
                    StatusMessage = "❌ Le fichier sélectionné n'existe pas";
                    return false;
                }

                // Vérification de l'extension
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    StatusMessage = "⚠️ Attention: Le fichier n'a pas une extension Excel standard";
                    // On continue quand même pour permettre la flexibilité
                }

                // Vérification de la taille du fichier (limite à 50 MB)
                var fileInfo = new FileInfo(filePath);
                const long maxSizeBytes = 50 * 1024 * 1024; // 50 MB
                if (fileInfo.Length > maxSizeBytes)
                {
                    StatusMessage = $"❌ Le fichier est trop volumineux ({fileInfo.Length / (1024 * 1024)} MB > 50 MB)";
                    return false;
                }

                // Vérification que le fichier n'est pas vide
                if (fileInfo.Length == 0)
                {
                    StatusMessage = "❌ Le fichier sélectionné est vide";
                    return false;
                }

                // Vérification des permissions de lecture
                try
                {
                    using var stream = File.OpenRead(filePath);
                    // Si on arrive ici, le fichier est accessible en lecture
                }
                catch (UnauthorizedAccessException)
                {
                    StatusMessage = "❌ Accès refusé au fichier. Vérifiez les permissions";
                    return false;
                }
                catch (IOException)
                {
                    StatusMessage = "❌ Le fichier est en cours d'utilisation par une autre application";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _ = Task.Run(async () => await _loggingService.LogErrorAsync("Erreur lors de la validation du fichier", ex, "GestionClients"));
                StatusMessage = "❌ Erreur lors de la validation du fichier";
                return false;
            }
        }

        /// <summary>
        /// Génère un rapport détaillé de l'analyse du fichier
        /// </summary>
        /// <returns>Contenu du rapport formaté</returns>
        private string GenerateDetailedReport()
        {
            if (Preview == null) return "Aucune donnée d'aperçu disponible.";

            var report = new StringBuilder();
            report.AppendLine("RAPPORT D'ANALYSE IMPORT CLIENTS");
            report.AppendLine("================================");
            report.AppendLine();
            
            // Informations générales
            report.AppendLine($"Fichier analysé: {Preview.FileName}");
            report.AppendLine($"Date d'analyse: {Preview.AnalyzedAt:dd/MM/yyyy HH:mm:ss}");
            report.AppendLine();
            
            // Statistiques
            report.AppendLine("STATISTIQUES:");
            report.AppendLine($"- Total lignes: {Preview.TotalRows}");
            report.AppendLine($"- Lignes valides: {Preview.ValidRows} ({GetPercentage(Preview.ValidRows, Preview.TotalRows)}%)");
            report.AppendLine($"- Lignes avec erreurs: {Preview.ErrorRows} ({GetPercentage(Preview.ErrorRows, Preview.TotalRows)}%)");
            report.AppendLine($"- Doublons détectés: {Preview.DuplicateRows} ({GetPercentage(Preview.DuplicateRows, Preview.TotalRows)}%)");
            report.AppendLine($"- Lignes vides: {Preview.EmptyRows} ({GetPercentage(Preview.EmptyRows, Preview.TotalRows)}%)");
            report.AppendLine();
            
            // Colonnes détectées
            if (Preview.DetectedColumns.Any())
            {
                report.AppendLine("COLONNES DÉTECTÉES:");
                foreach (var column in Preview.DetectedColumns)
                {
                    report.AppendLine($"- {column}");
                }
                report.AppendLine();
            }
            
            // Erreurs détaillées
            if (Preview.SampleErrors.Any())
            {
                report.AppendLine("EXEMPLES D'ERREURS:");
                foreach (var error in Preview.SampleErrors.Take(20)) // Limiter à 20 erreurs
                {
                    report.AppendLine($"• Ligne {error.RowNumber}: {error.ErrorMessage}");
                    if (!string.IsNullOrEmpty(error.RowData))
                    {
                        report.AppendLine($"  Données: {error.RowData}");
                    }
                    report.AppendLine();
                }
            }
            
            // Recommandations
            report.AppendLine("RECOMMANDATIONS:");
            if (Preview.ErrorRows > 0)
            {
                report.AppendLine("- Corriger les erreurs identifiées avant l'import");
            }
            if (Preview.DuplicateRows > 0)
            {
                report.AppendLine("- Vérifier les options de gestion des doublons");
            }
            if (Preview.ValidRows > 0)
            {
                report.AppendLine($"- {Preview.ValidRows} clients peuvent être importés avec succès");
            }
            
            return report.ToString();
        }

        /// <summary>
        /// Calcule le pourcentage avec une décimale
        /// </summary>
        private static double GetPercentage(int part, int total)
        {
            return total > 0 ? Math.Round((double)part / total * 100, 1) : 0;
        }

        /// <summary>
        /// Demande confirmation avant import
        /// </summary>
        private async Task<bool> ConfirmImportAsync()
        {
            try
            {
                var summary = Preview != null 
                    ? $"Vous allez importer {Preview.ValidRows} clients valides.\nIl y a {Preview.ErrorRows} erreurs et {Preview.DuplicateRows} doublons qui seront traités selon vos options.\n\nVoulez-vous continuer ?"
                    : "Vous allez procéder à l'import des clients.\n\nVoulez-vous continuer ?";

                var result = System.Windows.MessageBox.Show(
                    summary,
                    "Confirmation d'import",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question,
                    System.Windows.MessageBoxResult.No);

                return result == System.Windows.MessageBoxResult.Yes;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Erreur lors de la demande de confirmation", ex);
                return false;
            }
        }

        /// <summary>
        /// Met à jour la progression avec possibilité d'annulation
        /// </summary>
        private async Task UpdateProgressWithCancellation(int percentage, string message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProgressValue = percentage;
            ProgressText = message;
            await Task.Delay(200, cancellationToken); // Délai réaliste
        }

        #endregion
    }
}
