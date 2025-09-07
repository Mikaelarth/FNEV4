using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FNEV4.Application.Special;
using FNEV4.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace FNEV4.Presentation.Views.Special
{
    /// <summary>
    /// Fenêtre de dialogue pour l'import exceptionnel de clients
    /// SYSTÈME TEMPORAIRE - À supprimer après utilisation
    /// </summary>
    public partial class ImportExceptionnelDialog : Window
    {
        private readonly ImportSpecialExcelUseCase _importUseCase;
        private ImportSpecialExcelUseCase.ImportResult? _lastPreviewResult;
        
        public ImportExceptionnelDialog()
        {
            InitializeComponent();
            
            // Injection de dépendance via le ServiceProvider global
            _importUseCase = App.ServiceProvider.GetRequiredService<ImportSpecialExcelUseCase>();
            
            // Initialisation
            Title = "Import Exceptionnel - FNEV4";
            
            // Pré-remplir avec le fichier clients.xlsx s'il existe
            var defaultFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clients.xlsx");
            if (File.Exists(defaultFile))
            {
                FichierPathTextBox.Text = defaultFile;
                EnableButtons();
            }
        }
        
        private void SelectionnerFichierButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Sélectionner le fichier Excel d'import exceptionnel",
                Filter = "Fichiers Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Tous les fichiers (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };
            
            if (dialog.ShowDialog() == true)
            {
                FichierPathTextBox.Text = dialog.FileName;
                EnableButtons();
                ResetResults();
            }
        }
        
        private void EnableButtons()
        {
            bool hasFile = !string.IsNullOrEmpty(FichierPathTextBox.Text) && 
                          File.Exists(FichierPathTextBox.Text);
            
            PreviewButton.IsEnabled = hasFile;
            ImportButton.IsEnabled = hasFile && _lastPreviewResult != null;
        }
        
        private async void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecutePreview();
        }
        
        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastPreviewResult == null)
            {
                MessageBox.Show("Veuillez d'abord effectuer un aperçu avant d'importer.", 
                               "Aperçu requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir importer {_lastPreviewResult.PreviewClients.Count} clients en base de données ?\n\n" +
                "Cette action est irréversible.",
                "Confirmation d'import", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                await ExecuteImport();
            }
        }
        
        private async Task ExecutePreview()
        {
            try
            {
                ShowProgress("Analyse du fichier Excel en cours...");
                
                string filePath = FichierPathTextBox.Text;
                
                // Exécution en mode preview (isImportMode = false)
                _lastPreviewResult = await _importUseCase.ExecuteAsync(filePath, true); // previewOnly = true
                
                HideProgress();
                DisplayResults(_lastPreviewResult, false);
                
                ImportButton.IsEnabled = _lastPreviewResult.PreviewClients.Any();
                
            }
            catch (Exception ex)
            {
                HideProgress();
                MessageBox.Show($"Erreur lors de l'aperçu :\n{ex.Message}", 
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async Task ExecuteImport()
        {
            try
            {
                ShowProgress("Import des clients en base de données...");
                
                string filePath = FichierPathTextBox.Text;
                
                // Exécution en mode import (isImportMode = true)
                var importResult = await _importUseCase.ExecuteAsync(filePath, false); // previewOnly = false
                
                HideProgress();
                DisplayResults(importResult, true);
                
                // Désactiver le bouton d'import après succès
                ImportButton.IsEnabled = false;
                
                MessageBox.Show(
                    $"Import terminé avec succès !\n\n" +
                    $"• {importResult.SuccessfulImports} clients importés\n" +
                    $"• {importResult.Errors} erreurs\n" +
                    $"• {importResult.SkippedDuplicates} doublons ignorés",
                    "Import réussi", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                
            }
            catch (Exception ex)
            {
                HideProgress();
                MessageBox.Show($"Erreur lors de l'import :\n{ex.Message}", 
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void DisplayResults(ImportSpecialExcelUseCase.ImportResult result, bool wasImport)
        {
            // Afficher les statistiques
            ClientsExtraitsText.Text = result.TotalProcessed.ToString();
            ClientsValidesText.Text = result.SuccessfulImports.ToString();
            ErreursText.Text = result.Errors.ToString();
            DoublonsText.Text = result.SkippedDuplicates.ToString();
            
            // Afficher la carte des résultats
            ResultatsCard.Visibility = Visibility.Visible;
            
            // Afficher les clients (premiers 50)
            if (result.PreviewClients.Any())
            {
                // Convertir les ExceptionalClientModel en un format pour la DataGrid
                var clientsForDisplay = result.PreviewClients.Take(50).Select(c => new
                {
                    CodeClient = c.ClientCode,
                    NCC = c.ClientNcc,
                    Nom = c.Name,
                    TypeFacturation = c.InvoiceType, // Utiliser InvoiceType pour les types B2B, B2C, B2F, B2G
                    Email = c.Email
                }).ToList();
                
                ClientsDataGrid.ItemsSource = clientsForDisplay;
                ClientsCard.Visibility = Visibility.Visible;
            }
            
            // Afficher les erreurs s'il y en a
            if (result.Messages.Any())
            {
                ErreursListBox.ItemsSource = result.Messages.Take(20).ToList();
                ErreursCard.Visibility = Visibility.Visible;
            }
            else
            {
                ErreursCard.Visibility = Visibility.Collapsed;
            }
        }
        
        private void ShowProgress(string message)
        {
            ProgressText.Text = message;
            ProgressCard.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            
            // Désactiver les boutons pendant le traitement
            PreviewButton.IsEnabled = false;
            ImportButton.IsEnabled = false;
            SelectionnerFichierButton.IsEnabled = false;
        }
        
        private void HideProgress()
        {
            ProgressCard.Visibility = Visibility.Collapsed;
            
            // Réactiver les boutons
            EnableButtons();
            SelectionnerFichierButton.IsEnabled = true;
        }
        
        private void ResetResults()
        {
            _lastPreviewResult = null;
            ResultatsCard.Visibility = Visibility.Collapsed;
            ClientsCard.Visibility = Visibility.Collapsed;
            ErreursCard.Visibility = Visibility.Collapsed;
            ImportButton.IsEnabled = false;
        }
        
        private void FermerButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private void VoirDocumentationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ouvrir le fichier README avec l'application par défaut
                string readmePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SPECIAL-IMPORT-README.md");
                
                if (File.Exists(readmePath))
                {
                    Process.Start(new ProcessStartInfo(readmePath) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Fichier de documentation non trouvé.", 
                                   "Documentation", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir la documentation :\n{ex.Message}", 
                               "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
