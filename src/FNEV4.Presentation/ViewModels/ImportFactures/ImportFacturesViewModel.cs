using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Application.Services.ImportTraitement;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace FNEV4.Presentation.ViewModels.ImportFactures
{
    /// <summary>
    /// Mode d'import des factures
    /// </summary>
    public enum ImportMode
    {
        Automatique,
        Manuel
    }

    /// <summary>
    /// ViewModel temporaire simplifié pour l'import de factures (pour compilation)
    /// </summary>
    public partial class ImportFacturesViewModel : ObservableObject
    {
        private readonly ISage100ImportService? _importService;

        #region Properties

        [ObservableProperty]
        private bool _isProcessing = false;

        [ObservableProperty]
        private string _statusMessage = "Interface d'import de factures initialisée (version temporaire)";

        [ObservableProperty]
        private int _progress = 0;

        [ObservableProperty]
        private string _progressMessage = string.Empty;

        [ObservableProperty]
        private string _importFolderPath = @"C:\temp\import";

        [ObservableProperty]
        private string _selectedFilePath = string.Empty;

        [ObservableProperty]
        private ImportMode _currentMode = ImportMode.Automatique;

        public ObservableCollection<object> DetectedFiles { get; } = new();
        public ObservableCollection<object> PreviewFactures { get; } = new();

        #endregion

        #region Constructor

        public ImportFacturesViewModel()
        {
            // TEMPORAIRE: Constructor sans DI
            StatusMessage = "ViewModel Import de Factures initialisé (mode temporaire)";
        }

        public ImportFacturesViewModel(ISage100ImportService importService)
        {
            _importService = importService;
            StatusMessage = "ViewModel Import de Factures initialisé avec service";
        }

        #endregion

        #region Commands

        [RelayCommand]
        private async Task ScanImportFolder()
        {
            if (IsProcessing) return;

            IsProcessing = true;
            StatusMessage = "Scan du dossier en cours...";
            
            try
            {
                // TEMPORAIRE: Simulation
                await Task.Delay(1000);
                StatusMessage = "Scan terminé (simulation)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur scan: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void SelectFile()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Sélectionner un fichier Excel",
                    Filter = "Fichiers Excel (*.xlsx)|*.xlsx|Tous les fichiers (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    SelectedFilePath = dialog.FileName;
                    StatusMessage = $"Fichier sélectionné: {System.IO.Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur sélection fichier: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task GeneratePreview()
        {
            if (string.IsNullOrEmpty(SelectedFilePath)) return;

            IsProcessing = true;
            StatusMessage = "Génération aperçu...";
            
            try
            {
                // TEMPORAIRE: Simulation
                await Task.Delay(500);
                StatusMessage = "Aperçu généré (simulation)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur aperçu: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private async Task ImportAutomatic()
        {
            IsProcessing = true;
            StatusMessage = "Import automatique...";
            
            try
            {
                await Task.Delay(1000);
                StatusMessage = "Import automatique terminé (simulation)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur import: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private async Task ImportManual()
        {
            IsProcessing = true;
            StatusMessage = "Import manuel...";
            
            try
            {
                await Task.Delay(1000);
                StatusMessage = "Import manuel terminé (simulation)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur import: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        #endregion
    }
}