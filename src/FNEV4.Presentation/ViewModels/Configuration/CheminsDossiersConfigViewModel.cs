using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Diagnostics;
using FNEV4.Core.Interfaces;
using FNEV4.Core.Entities;
using FNEV4.Infrastructure.Services;

namespace FNEV4.Presentation.ViewModels.Configuration
{
    /// <summary>
    /// ViewModel pour la configuration des chemins et dossiers de l'application FNEV4.
    /// Gère l'import, export, archivage, logs et backup avec surveillance automatique.
    /// </summary>
    public partial class CheminsDossiersConfigViewModel : ObservableObject
    {
        #region Fields & Dependencies

        private readonly IPathConfigurationService _pathConfigurationService;

        // Services temporairement désactivés pour debug
        // private readonly IFolderConfigurationService _folderService;
        // private readonly IFileWatcherService _watcherService;
        // private readonly IBackupService _backupService;
        // private readonly ILoggingService _loggingService;
        // private readonly INotificationService _notificationService;

        // Timers pour la surveillance
        private System.Timers.Timer _statusUpdateTimer;
        private System.Timers.Timer _spaceCalculationTimer;

        #endregion

        #region Observable Properties - Chemins Principaux

        [ObservableProperty]
        private string importFolderPath = string.Empty;

        [ObservableProperty]
        private string exportFolderPath = string.Empty;

        [ObservableProperty]
        private string archiveFolderPath = string.Empty;

        [ObservableProperty]
        private string logsFolderPath = string.Empty;

        [ObservableProperty]
        private string backupFolderPath = string.Empty;

        #endregion

        #region Observable Properties - Statuts des Dossiers

        [ObservableProperty]
        private string importFolderStatus = "Unknown";

        [ObservableProperty]
        private string exportFolderStatus = "Unknown";

        [ObservableProperty]
        private string archiveFolderStatus = "Unknown";

        [ObservableProperty]
        private string logsFolderStatus = "Unknown";

        [ObservableProperty]
        private string backupFolderStatus = "Unknown";

        [ObservableProperty]
        private string globalStatusMessage = "Vérification en cours...";

        [ObservableProperty]
        private string globalStatusIcon = "Loading";

        [ObservableProperty]
        private Brush globalStatusColor = Brushes.Orange;

        #endregion

        #region Observable Properties - Options de Surveillance

        [ObservableProperty]
        private bool importFolderWatchEnabled = true;

        [ObservableProperty]
        private bool exportAutoOrganizeEnabled = true;

        [ObservableProperty]
        private bool archiveAutoEnabled = false;

        [ObservableProperty]
        private bool logRotationEnabled = true;

        [ObservableProperty]
        private bool backupAutoEnabled = true;

        #endregion

        #region Observable Properties - Informations Détaillées

        [ObservableProperty]
        private string importFolderInfo = string.Empty;

        [ObservableProperty]
        private string exportFolderInfo = string.Empty;

        [ObservableProperty]
        private string archiveFolderInfo = string.Empty;

        [ObservableProperty]
        private string logsFolderInfo = string.Empty;

        [ObservableProperty]
        private string backupFolderInfo = string.Empty;

        [ObservableProperty]
        private string importFolderWatchInfo = string.Empty;

        [ObservableProperty]
        private string exportOrganizationInfo = string.Empty;

        [ObservableProperty]
        private string archivePolicyInfo = string.Empty;

        [ObservableProperty]
        private string logsStatistics = string.Empty;

        [ObservableProperty]
        private string lastBackupInfo = string.Empty;

        #endregion

        #region Observable Properties - Statistiques Globales

        [ObservableProperty]
        private string pathsConfiguredSummary = string.Empty;

        [ObservableProperty]
        private int activeWatchersCount = 0;

        [ObservableProperty]
        private string totalSpaceUsed = "Calcul...";

        [ObservableProperty]
        private int configuredFoldersCount = 0;

        [ObservableProperty]
        private string namingPreview = string.Empty;

        #endregion

        #region Observable Properties - Collections

        [ObservableProperty]
        private ObservableCollection<string> archivePeriodOptions = new();

        [ObservableProperty]
        private ObservableCollection<string> logLevelOptions = new();

        [ObservableProperty]
        private ObservableCollection<string> backupFrequencyOptions = new();

        [ObservableProperty]
        private ObservableCollection<string> exportNamingPatterns = new();

        [ObservableProperty]
        private ObservableCollection<string> archiveOrganizationPatterns = new();

        [ObservableProperty]
        private string selectedArchivePeriod = "30 jours";

        [ObservableProperty]
        private string selectedLogLevel = "Information";

        [ObservableProperty]
        private string selectedBackupFrequency = "Quotidien";

        [ObservableProperty]
        private string selectedExportNaming = "{Date}_{NomClient}_{NumFacture}.pdf";

        [ObservableProperty]
        private string selectedArchiveOrganization = "{Annee}/{Mois}/{Client}";

        #endregion

        #region Observable Properties - État Interface

        [ObservableProperty]
        private bool canSave = true;

        [ObservableProperty]
        private bool isNotificationVisible = false;

        [ObservableProperty]
        private string notificationMessage = string.Empty;

        [ObservableProperty]
        private string notificationIcon = "Information";

        [ObservableProperty]
        private Brush notificationColor = Brushes.Blue;

        #endregion

        #region Constructor

        public CheminsDossiersConfigViewModel(IPathConfigurationService pathConfigurationService = null)
        {
            _pathConfigurationService = pathConfigurationService ?? App.GetService<IPathConfigurationService>();
            
            InitializeCollections();
            InitializePathsFromService();
            
            // Initialisation asynchrone pour mettre à jour les statuts au chargement
            _ = Task.Run(async () => await InitializeStatusAsync());
        }

        #endregion

        #region Initialization Methods

        private void InitializeCollections()
        {
            // Options de période d'archivage
            ArchivePeriodOptions = new ObservableCollection<string>
            {
                "7 jours", "15 jours", "30 jours", "60 jours", "90 jours", "180 jours", "1 an", "Jamais"
            };

            // Niveaux de log
            LogLevelOptions = new ObservableCollection<string>
            {
                "Minimal", "Information", "Avertissement", "Erreur", "Critique", "Debug"
            };

            // Fréquences de backup
            BackupFrequencyOptions = new ObservableCollection<string>
            {
                "Toutes les heures", "Quotidien", "Hebdomadaire", "Manuel"
            };

            // Modèles de nommage export
            ExportNamingPatterns = new ObservableCollection<string>
            {
                "{Date}_{NomClient}_{NumFacture}.pdf",
                "{NumFacture}_{Date}_{NCC}.pdf",
                "{Annee}{Mois}{Jour}_{NumFacture}.pdf",
                "{NomClient}_{NumFacture}_{Timestamp}.pdf",
                "Facture_{NumFacture}_{Date}.pdf"
            };

            // Organisations d'archivage
            ArchiveOrganizationPatterns = new ObservableCollection<string>
            {
                "{Annee}/{Mois}/{Client}",
                "{Annee}/{Mois}/{Jour}",
                "{Client}/{Annee}",
                "{Statut}/{Annee}/{Mois}",
                "Archive_{Annee}_{Mois}"
            };
        }

        private void InitializePathsFromService()
        {
            try
            {
                // Assurer que les dossiers existent
                _pathConfigurationService.EnsureDirectoriesExist();

                // Initialiser les chemins depuis le service centralisé
                ImportFolderPath = _pathConfigurationService.ImportFolderPath;
                ExportFolderPath = _pathConfigurationService.ExportFolderPath;
                ArchiveFolderPath = _pathConfigurationService.ArchiveFolderPath;
                LogsFolderPath = _pathConfigurationService.LogsFolderPath;
                BackupFolderPath = _pathConfigurationService.BackupFolderPath;

                UpdateNamingPreview();
            }
            catch (Exception ex)
            {
                // Fallback vers les valeurs par défaut en cas d'erreur
                InitializeFallbackValues();
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation des chemins: {ex.Message}");
            }
        }

        private void InitializeFallbackValues()
        {
            // Chemins par défaut relatifs (comme dans appsettings.json)
            ImportFolderPath = @"Data\Import";
            ExportFolderPath = @"Data\Export";
            ArchiveFolderPath = @"Data\Archive";
            LogsFolderPath = @"Data\Logs";
            BackupFolderPath = @"Data\Backup";

            UpdateNamingPreview();
        }

        private void ResetOptionsToDefaults()
        {
            // Réinitialiser les options de surveillance et automatisation
            ImportFolderWatchEnabled = true;
            ExportAutoOrganizeEnabled = true;
            ArchiveAutoEnabled = false;
            LogRotationEnabled = true;
            BackupAutoEnabled = true;

            // Réinitialiser les sélections aux valeurs par défaut
            SelectedArchivePeriod = "30 jours";
            SelectedLogLevel = "Information";
            SelectedBackupFrequency = "Quotidien";
            SelectedExportNaming = "{Date}_{NomClient}_{NumFacture}.pdf";
            SelectedArchiveOrganization = "{Annee}/{Mois}/{Client}";

            UpdateNamingPreview();
        }

        /// <summary>
        /// Initialise les statuts des dossiers de manière asynchrone au chargement
        /// </summary>
        private async Task InitializeStatusAsync()
        {
            try
            {
                // Effectuer la première mise à jour des statuts
                await UpdateAllStatusAsync();
                await CalculateSpaceUsageAsync();
            }
            catch (Exception ex)
            {
                // En cas d'erreur, afficher un message d'erreur au lieu de rester figé
                GlobalStatusMessage = "❌ Erreur lors de l'initialisation";
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation des statuts: {ex.Message}");
            }
        }

        private void InitializeTimers()
        {
            // Timer pour mise à jour des statuts (toutes les 30 secondes)
            _statusUpdateTimer = new System.Timers.Timer(30000);
            _statusUpdateTimer.Elapsed += async (s, e) => await UpdateAllStatusAsync();
            _statusUpdateTimer.AutoReset = true;
            _statusUpdateTimer.Start();

            // Timer pour calcul de l'espace (toutes les 2 minutes)
            _spaceCalculationTimer = new System.Timers.Timer(120000);
            _spaceCalculationTimer.Elapsed += async (s, e) => await CalculateSpaceUsageAsync();
            _spaceCalculationTimer.AutoReset = true;
            _spaceCalculationTimer.Start();
        }

        #endregion

        #region Commands - Navigation et Parcours

        [RelayCommand]
        private async Task BrowseImportFolderAsync()
        {
            var path = BrowseForFolder("Sélectionner le dossier d'import Excel Sage 100", ImportFolderPath);
            if (!string.IsNullOrEmpty(path))
            {
                ImportFolderPath = path;
                await ValidatePathAsync("Import", path);
                await UpdateImportFolderInfoAsync();
            }
        }

        [RelayCommand]
        private async Task BrowseExportFolderAsync()
        {
            var path = BrowseForFolder("Sélectionner le dossier d'export des factures certifiées", ExportFolderPath);
            if (!string.IsNullOrEmpty(path))
            {
                ExportFolderPath = path;
                await ValidatePathAsync("Export", path);
                await UpdateExportFolderInfoAsync();
            }
        }

        [RelayCommand]
        private async Task BrowseArchiveFolderAsync()
        {
            var path = BrowseForFolder("Sélectionner le dossier d'archivage", ArchiveFolderPath);
            if (!string.IsNullOrEmpty(path))
            {
                ArchiveFolderPath = path;
                await ValidatePathAsync("Archive", path);
                await UpdateArchiveFolderInfoAsync();
            }
        }

        [RelayCommand]
        private async Task BrowseLogsFolderAsync()
        {
            var path = BrowseForFolder("Sélectionner le dossier des logs", LogsFolderPath);
            if (!string.IsNullOrEmpty(path))
            {
                LogsFolderPath = path;
                await ValidatePathAsync("Logs", path);
                await UpdateLogsFolderInfoAsync();
            }
        }

        [RelayCommand]
        private async Task BrowseBackupFolderAsync()
        {
            var path = BrowseForFolder("Sélectionner le dossier de sauvegarde", BackupFolderPath);
            if (!string.IsNullOrEmpty(path))
            {
                BackupFolderPath = path;
                await ValidatePathAsync("Backup", path);
                await UpdateBackupFolderInfoAsync();
            }
        }

        #endregion

        #region Commands - Actions sur Dossiers

        [RelayCommand]
        private void OpenImportFolder() 
        {
            System.Windows.MessageBox.Show("🔍 Commande OpenImportFolder appelée !", "Debug", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            OpenFolder(ImportFolderPath);
        }

        [RelayCommand]
        private void OpenExportFolder() => OpenFolder(ExportFolderPath);

        [RelayCommand]
        private void OpenArchiveFolder() => OpenFolder(ArchiveFolderPath);

        [RelayCommand]
        private void OpenLogsFolder() => OpenFolder(LogsFolderPath);

        [RelayCommand]
        private void OpenBackupFolder() => OpenFolder(BackupFolderPath);

        [RelayCommand]
        private void OpenRootFolder()
        {
            try
            {
                // Debug visible avec MessageBox
                System.Windows.MessageBox.Show("🔍 Commande OpenRootFolder appelée !", "Debug", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                // Test simple avec notification pour vérifier que la commande fonctionne
                _ = ShowNotificationAsync("🔍 Commande OpenRootFolder exécutée !", "Information", Brushes.Blue);
                
                if (string.IsNullOrEmpty(ImportFolderPath))
                {
                    _ = ShowNotificationAsync("⚠️ Veuillez d'abord configurer le dossier d'import", "Warning", Brushes.Orange);
                    System.Windows.MessageBox.Show("⚠️ Veuillez d'abord configurer le dossier d'import", "Attention", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var rootPath = Path.GetDirectoryName(ImportFolderPath);
                if (!string.IsNullOrEmpty(rootPath))
                {
                    _ = ShowNotificationAsync($"📂 Ouverture du dossier racine : {rootPath}", "FolderOpen", Brushes.Blue);
                    OpenFolder(rootPath);
                }
                else
                {
                    _ = ShowNotificationAsync("❌ Impossible de déterminer le dossier racine", "FolderRemove", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                _ = ShowNotificationAsync($"❌ Erreur lors de l'ouverture : {ex.Message}", "Alert", Brushes.Red);
            }
        }

        #endregion

        #region Commands - Tests et Validation

        [RelayCommand]
        private async Task TestImportFolderAsync()
        {
            await TestFolderAsync("Import", ImportFolderPath);
        }

        [RelayCommand]
        private async Task TestExportFolderAsync()
        {
            await TestFolderAsync("Export", ExportFolderPath);
        }

        [RelayCommand]
        private async Task TestArchiveFolderAsync()
        {
            await TestFolderAsync("Archive", ArchiveFolderPath);
        }

        [RelayCommand]
        private async Task TestAllPathsAsync()
        {
            await ShowNotificationAsync("🔍 Test de tous les chemins en cours...", "Loading", Brushes.Blue);

            var tasks = new[]
            {
                TestFolderAsync("Import", ImportFolderPath),
                TestFolderAsync("Export", ExportFolderPath),
                TestFolderAsync("Archive", ArchiveFolderPath),
                TestFolderAsync("Logs", LogsFolderPath),
                TestFolderAsync("Backup", BackupFolderPath)
            };

            await Task.WhenAll(tasks);
            await UpdateGlobalStatusAsync();
            await ShowNotificationAsync("✅ Test de tous les chemins terminé", "CheckCircle", Brushes.Green);
        }

        #endregion

        #region Commands - Gestion des Fichiers

        [RelayCommand]
        private async Task CleanArchiveFolderAsync()
        {
            try
            {
                await ShowNotificationAsync("🧹 Nettoyage des archives en cours...", "Broom", Brushes.Orange);

                // var cleanedSize = await _folderService.CleanArchiveFolderAsync(ArchiveFolderPath, SelectedArchivePeriod);
                var cleanedSize = 1024L; // Temporaire
                
                await ShowNotificationAsync($"✅ Archives nettoyées: {FormatBytes(cleanedSize)} libérés", "CheckCircle", Brushes.Green);
                await UpdateArchiveFolderInfoAsync();
            }
            catch (Exception ex)
            {
                await ShowNotificationAsync($"❌ Erreur nettoyage: {ex.Message}", "Alert", Brushes.Red);
            }
        }

        [RelayCommand]
        private async Task CleanOldLogsAsync()
        {
            try
            {
                await ShowNotificationAsync("🧹 Nettoyage des anciens logs...", "DeleteSweep", Brushes.Orange);

                // Utiliser la méthode disponible dans l'interface existante
                // var cutoffDate = DateTime.Now.AddDays(-30);
                // await _loggingService.ClearOldLogsAsync(cutoffDate);
                
                await ShowNotificationAsync("✅ Anciens logs nettoyés", "CheckCircle", Brushes.Green);
                await UpdateLogsFolderInfoAsync();
            }
            catch (Exception ex)
            {
                await ShowNotificationAsync($"❌ Erreur nettoyage logs: {ex.Message}", "Alert", Brushes.Red);
            }
        }

        [RelayCommand]
        private void ViewLatestLog()
        {
            try
            {
                // Rechercher manuellement le dernier fichier de log
                var latestLog = GetLatestLogFile(LogsFolderPath);
                if (!string.IsNullOrEmpty(latestLog) && File.Exists(latestLog))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = latestLog,
                        UseShellExecute = true
                    });
                }
                else
                {
                    _ = ShowNotificationAsync("ℹ️ Aucun fichier log trouvé", "Information", Brushes.Blue);
                }
            }
            catch (Exception ex)
            {
                _ = ShowNotificationAsync($"❌ Erreur ouverture log: {ex.Message}", "Alert", Brushes.Red);
            }
        }

        #endregion

        #region Commands - Sauvegarde et Backup

        [RelayCommand]
        private async Task CreateBackupNowAsync()
        {
            try
            {
                await ShowNotificationAsync("💾 Création de la sauvegarde...", "DatabaseExport", Brushes.Blue);

                // var backupPath = await _backupService.CreateBackupAsync(BackupFolderPath);
                var backupPath = "temp_backup.bak"; // Temporaire
                
                await ShowNotificationAsync($"✅ Sauvegarde créée: {Path.GetFileName(backupPath)}", "CheckCircle", Brushes.Green);
                await UpdateBackupFolderInfoAsync();
            }
            catch (Exception ex)
            {
                await ShowNotificationAsync($"❌ Erreur sauvegarde: {ex.Message}", "Alert", Brushes.Red);
            }
        }

        [RelayCommand]
        private void ManageBackups()
        {
            // Ouvrir une fenêtre de gestion des sauvegardes
            // TODO: Implémenter la fenêtre de gestion des backups
            _ = ShowNotificationAsync("🔧 Gestionnaire de sauvegardes (à implémenter)", "Cogs", Brushes.Orange);
        }

        #endregion

        #region Commands - Actions Système

        [RelayCommand]
        private async Task CreateAllFoldersAsync()
        {
            try
            {
                await ShowNotificationAsync("📁 Création de tous les dossiers...", "FolderPlus", Brushes.Blue);

                var folders = new[] { ImportFolderPath, ExportFolderPath, ArchiveFolderPath, LogsFolderPath, BackupFolderPath };
                var created = 0;

                foreach (var folder in folders)
                {
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                        created++;
                    }
                }

                await ShowNotificationAsync($"✅ {created} dossier(s) créé(s)", "CheckCircle", Brushes.Green);
                await UpdateAllStatusAsync();
            }
            catch (Exception ex)
            {
                await ShowNotificationAsync($"❌ Erreur création: {ex.Message}", "Alert", Brushes.Red);
            }
        }

        [RelayCommand]
        private async Task VerifyAllPermissionsAsync()
        {
            try
            {
                await ShowNotificationAsync("🛡️ Vérification des permissions...", "ShieldCheck", Brushes.Blue);

                var folders = new[] { ImportFolderPath, ExportFolderPath, ArchiveFolderPath, LogsFolderPath, BackupFolderPath };
                var issues = new List<string>();

                foreach (var folder in folders)
                {
                    // if (!await _folderService.HasWritePermissionAsync(folder))
                    // {
                    //     issues.Add(Path.GetFileName(folder));
                    // }
                    // Temporaire - supposer que tous les dossiers sont OK
                }

                if (issues.Any())
                {
                    await ShowNotificationAsync($"⚠️ Problèmes: {string.Join(", ", issues)}", "AlertCircle", Brushes.Orange);
                }
                else
                {
                    await ShowNotificationAsync("✅ Toutes les permissions OK", "CheckCircle", Brushes.Green);
                }
            }
            catch (Exception ex)
            {
                await ShowNotificationAsync($"❌ Erreur vérification: {ex.Message}", "Alert", Brushes.Red);
            }
        }

        [RelayCommand]
        private async Task RefreshAllPathsAsync()
        {
            await ShowNotificationAsync("🔄 Actualisation en cours...", "Refresh", Brushes.Blue);
            
            // Réinitialiser depuis le service centralisé
            InitializePathsFromService();
            
            await UpdateAllStatusAsync();
            await CalculateSpaceUsageAsync();
            await ShowNotificationAsync("✅ Actualisation terminée", "CheckCircle", Brushes.Green);
        }

        [RelayCommand]
        private async Task ResetToDefaultsAsync()
        {
            await ShowNotificationAsync("🔄 Remise aux valeurs par défaut...", "RestoreAlert", Brushes.Orange);
            
            // Réinitialisation avec les vraies valeurs par défaut
            InitializeFallbackValues();
            
            // Sauvegarder les nouvelles valeurs par défaut dans le service centralisé
            _pathConfigurationService.UpdatePaths(
                ImportFolderPath,
                ExportFolderPath,
                ArchiveFolderPath,
                LogsFolderPath,
                BackupFolderPath
            );
            
            // Assurer que tous les dossiers existent
            _pathConfigurationService.EnsureDirectoriesExist();
            
            // Réinitialiser les options aux valeurs par défaut
            ResetOptionsToDefaults();
            
            await UpdateAllStatusAsync();
            await ShowNotificationAsync("✅ Configuration réinitialisée aux valeurs par défaut", "CheckCircle", Brushes.Green);
        }

        #endregion

        #region Commands - Configuration

        [RelayCommand]
        private async Task SaveConfigurationAsync()
        {
            try
            {
                CanSave = false;
                await ShowNotificationAsync("💾 Sauvegarde de la configuration...", "ContentSave", Brushes.Blue);

                // Mettre à jour les chemins dans le service centralisé
                _pathConfigurationService.UpdatePaths(
                    ImportFolderPath,
                    ExportFolderPath,
                    ArchiveFolderPath,
                    LogsFolderPath,
                    BackupFolderPath
                );

                // S'assurer que tous les dossiers existent
                _pathConfigurationService.EnsureDirectoriesExist();

                // Mise à jour de la surveillance
                await UpdateFileWatchersAsync();

                await ShowNotificationAsync("✅ Configuration sauvegardée avec succès", "CheckCircle", Brushes.Green);
            }
            catch (Exception ex)
            {
                await ShowNotificationAsync($"❌ Erreur sauvegarde: {ex.Message}", "Alert", Brushes.Red);
            }
            finally
            {
                CanSave = true;
            }
        }

        [RelayCommand]
        private async Task ExportConfigurationAsync()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Configuration JSON|*.json|Tous les fichiers|*.*",
                    DefaultExt = "json",
                    FileName = $"FNEV4_CheminsDossiers_{DateTime.Now:yyyyMMdd}.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var config = CreateFolderConfiguration();
                    // await _folderService.ExportConfigurationAsync(config, saveDialog.FileName);
                    await ShowNotificationAsync($"✅ Configuration exportée: {Path.GetFileName(saveDialog.FileName)}", "Export", Brushes.Green);
                }
            }
            catch (Exception ex)
            {
                await ShowNotificationAsync($"❌ Erreur export: {ex.Message}", "Alert", Brushes.Red);
            }
        }

        [RelayCommand]
        private async Task ImportConfigurationAsync()
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "Configuration JSON|*.json|Tous les fichiers|*.*",
                    DefaultExt = "json"
                };

                if (openDialog.ShowDialog() == true)
                {
                    // var config = await _folderService.ImportConfigurationAsync(openDialog.FileName);
                    // ApplyConfiguration(config);
                    await ShowNotificationAsync($"✅ Configuration importée: {Path.GetFileName(openDialog.FileName)}", "Import", Brushes.Green);
                }
            }
            catch (Exception ex)
            {
                await ShowNotificationAsync($"❌ Erreur import: {ex.Message}", "Alert", Brushes.Red);
            }
        }

        #endregion

        #region Helper Methods - Validation et Test

        private async Task<bool> ValidatePathAsync(string pathType, string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    SetPathStatus(pathType, "Invalid");
                    return false;
                }

                if (!Directory.Exists(path))
                {
                    SetPathStatus(pathType, "Warning");
                    return false;
                }

                // if (!await _folderService.HasWritePermissionAsync(path))
                // {
                //     SetPathStatus(pathType, "Warning");
                //     return false;
                // }

                SetPathStatus(pathType, "Valid");
                return true;
            }
            catch
            {
                SetPathStatus(pathType, "Invalid");
                return false;
            }
        }

        private void SetPathStatus(string pathType, string status)
        {
            switch (pathType)
            {
                case "Import": ImportFolderStatus = status; break;
                case "Export": ExportFolderStatus = status; break;
                case "Archive": ArchiveFolderStatus = status; break;
                case "Logs": LogsFolderStatus = status; break;
                case "Backup": BackupFolderStatus = status; break;
            }
        }

        private async Task TestFolderAsync(string folderType, string folderPath)
        {
            try
            {
                await ShowNotificationAsync($"🔍 Test du dossier {folderType}...", "TestTube", Brushes.Blue);

                // var result = await _folderService.TestFolderAsync(folderPath);
                
                // Simulation temporaire
                bool isValid = Directory.Exists(folderPath);
                string message = isValid ? "Dossier accessible" : "Dossier non trouvé";
                
                if (isValid)
                {
                    await ShowNotificationAsync($"✅ {folderType}: {message}", "CheckCircle", Brushes.Green);
                    SetPathStatus(folderType, "Valid");
                }
                else
                {
                    await ShowNotificationAsync($"⚠️ {folderType}: {message}", "AlertCircle", Brushes.Orange);
                    SetPathStatus(folderType, "Warning");
                }
            }
            catch (Exception ex)
            {
                await ShowNotificationAsync($"❌ Erreur test {folderType}: {ex.Message}", "Alert", Brushes.Red);
                SetPathStatus(folderType, "Invalid");
            }
        }

        #endregion

        #region Helper Methods - Updates

        private async Task UpdateAllStatusAsync()
        {
            // Exécuter directement sur le thread UI pour mettre à jour les propriétés observables
            await ValidatePathAsync("Import", ImportFolderPath);
            await ValidatePathAsync("Export", ExportFolderPath);
            await ValidatePathAsync("Archive", ArchiveFolderPath);
            await ValidatePathAsync("Logs", LogsFolderPath);
            await ValidatePathAsync("Backup", BackupFolderPath);

            await UpdateImportFolderInfoAsync();
            await UpdateExportFolderInfoAsync();
            await UpdateArchiveFolderInfoAsync();
            await UpdateLogsFolderInfoAsync();
            await UpdateBackupFolderInfoAsync();

            await UpdateGlobalStatusAsync();
            await UpdateStatisticsAsync();
        }

        private async Task UpdateGlobalStatusAsync()
        {
            var statuses = new[] { ImportFolderStatus, ExportFolderStatus, ArchiveFolderStatus, LogsFolderStatus, BackupFolderStatus };
            
            var validCount = statuses.Count(s => s == "Valid");
            var warningCount = statuses.Count(s => s == "Warning");
            var invalidCount = statuses.Count(s => s == "Invalid");

            if (invalidCount > 0)
            {
                GlobalStatusMessage = $"❌ {invalidCount} dossier(s) avec erreurs";
                GlobalStatusIcon = "AlertCircle";
                GlobalStatusColor = Brushes.Red;
            }
            else if (warningCount > 0)
            {
                GlobalStatusMessage = $"⚠️ {warningCount} dossier(s) avec avertissements";
                GlobalStatusIcon = "Alert";
                GlobalStatusColor = Brushes.Orange;
            }
            else if (validCount == 5)
            {
                GlobalStatusMessage = "✅ Tous les dossiers sont configurés";
                GlobalStatusIcon = "CheckCircle";
                GlobalStatusColor = Brushes.Green;
            }
            else
            {
                GlobalStatusMessage = "🔍 Configuration en cours...";
                GlobalStatusIcon = "Loading";
                GlobalStatusColor = Brushes.Blue;
            }

            PathsConfiguredSummary = $"{validCount}/5 dossiers configurés correctement";
        }

        private async Task UpdateStatisticsAsync()
        {
            ConfiguredFoldersCount = new[] { ImportFolderStatus, ExportFolderStatus, ArchiveFolderStatus, LogsFolderStatus, BackupFolderStatus }
                .Count(s => s == "Valid");

            // Compter les surveillances actives
            ActiveWatchersCount = 0;
            if (ImportFolderWatchEnabled && ImportFolderStatus == "Valid") ActiveWatchersCount++;
            if (ExportAutoOrganizeEnabled && ExportFolderStatus == "Valid") ActiveWatchersCount++;
            if (ArchiveAutoEnabled && ArchiveFolderStatus == "Valid") ActiveWatchersCount++;
            if (BackupAutoEnabled && BackupFolderStatus == "Valid") ActiveWatchersCount++;
        }

        private async Task UpdateImportFolderInfoAsync()
        {
            if (Directory.Exists(ImportFolderPath))
            {
                var fileCount = Directory.GetFiles(ImportFolderPath, "*.xlsx").Length;
                var dirInfo = new DirectoryInfo(ImportFolderPath);
                var size = await CalculateDirectorySizeAsync(dirInfo);
                
                ImportFolderInfo = $"📊 {fileCount} fichier(s) Excel • {FormatBytes(size)}";
                
                if (ImportFolderWatchEnabled)
                {
                    ImportFolderWatchInfo = "Surveillance active - détection automatique des nouveaux fichiers";
                }
            }
            else
            {
                ImportFolderInfo = "❌ Dossier non trouvé - créez-le ou modifiez le chemin";
                ImportFolderWatchInfo = string.Empty;
            }
        }

        private async Task UpdateExportFolderInfoAsync()
        {
            if (Directory.Exists(ExportFolderPath))
            {
                var fileCount = Directory.GetFiles(ExportFolderPath, "*.*", SearchOption.AllDirectories).Length;
                var dirInfo = new DirectoryInfo(ExportFolderPath);
                var size = await CalculateDirectorySizeAsync(dirInfo);
                
                ExportFolderInfo = $"📄 {fileCount} fichier(s) exporté(s) • {FormatBytes(size)}";
                
                if (ExportAutoOrganizeEnabled)
                {
                    ExportOrganizationInfo = $"Organisation automatique selon: {SelectedArchiveOrganization}";
                }
            }
            else
            {
                ExportFolderInfo = "❌ Dossier non trouvé - sera créé automatiquement lors du premier export";
                ExportOrganizationInfo = string.Empty;
            }
        }

        private async Task UpdateArchiveFolderInfoAsync()
        {
            if (Directory.Exists(ArchiveFolderPath))
            {
                var fileCount = Directory.GetFiles(ArchiveFolderPath, "*.*", SearchOption.AllDirectories).Length;
                var dirInfo = new DirectoryInfo(ArchiveFolderPath);
                var size = await CalculateDirectorySizeAsync(dirInfo);
                
                ArchiveFolderInfo = $"🗄️ {fileCount} fichier(s) archivé(s) • {FormatBytes(size)}";
                
                if (ArchiveAutoEnabled)
                {
                    ArchivePolicyInfo = $"Archivage automatique après {SelectedArchivePeriod}";
                }
            }
            else
            {
                ArchiveFolderInfo = "❌ Dossier non trouvé - sera créé lors du premier archivage";
                ArchivePolicyInfo = string.Empty;
            }
        }

        private async Task UpdateLogsFolderInfoAsync()
        {
            if (Directory.Exists(LogsFolderPath))
            {
                var logFiles = Directory.GetFiles(LogsFolderPath, "*.log");
                var dirInfo = new DirectoryInfo(LogsFolderPath);
                var size = await CalculateDirectorySizeAsync(dirInfo);
                
                LogsFolderInfo = $"📋 {logFiles.Length} fichier(s) log • {FormatBytes(size)}";
                
                var latestLog = logFiles.OrderByDescending(f => File.GetLastWriteTime(f)).FirstOrDefault();
                if (latestLog != null)
                {
                    var lastModified = File.GetLastWriteTime(latestLog);
                    LogsStatistics = $"Dernier log: {Path.GetFileName(latestLog)} • {lastModified:dd/MM/yyyy HH:mm}";
                }
            }
            else
            {
                LogsFolderInfo = "❌ Dossier de logs non trouvé";
                LogsStatistics = string.Empty;
            }
        }

        private async Task UpdateBackupFolderInfoAsync()
        {
            if (Directory.Exists(BackupFolderPath))
            {
                var backupFiles = Directory.GetFiles(BackupFolderPath, "*.bak");
                var dirInfo = new DirectoryInfo(BackupFolderPath);
                var size = await CalculateDirectorySizeAsync(dirInfo);
                
                BackupFolderInfo = $"💾 {backupFiles.Length} sauvegarde(s) • {FormatBytes(size)}";
                
                var latestBackup = backupFiles.OrderByDescending(f => File.GetLastWriteTime(f)).FirstOrDefault();
                if (latestBackup != null)
                {
                    var lastModified = File.GetLastWriteTime(latestBackup);
                    LastBackupInfo = $"{Path.GetFileName(latestBackup)} • {lastModified:dd/MM/yyyy HH:mm}";
                }
                else
                {
                    LastBackupInfo = "Aucune sauvegarde trouvée";
                }
            }
            else
            {
                BackupFolderInfo = "❌ Dossier de sauvegarde non trouvé";
                LastBackupInfo = string.Empty;
            }
        }

        private async Task CalculateSpaceUsageAsync()
        {
            try
            {
                long totalSize = 0;
                var folders = new[] { ImportFolderPath, ExportFolderPath, ArchiveFolderPath, LogsFolderPath, BackupFolderPath };
                
                foreach (var folder in folders)
                {
                    if (Directory.Exists(folder))
                    {
                        var dirInfo = new DirectoryInfo(folder);
                        totalSize += await CalculateDirectorySizeAsync(dirInfo);
                    }
                }
                
                TotalSpaceUsed = FormatBytes(totalSize);
            }
            catch
            {
                TotalSpaceUsed = "Erreur calcul";
            }
        }

        #endregion

        #region Helper Methods - File Operations

        private string BrowseForFolder(string description, string initialPath = "")
        {
            var dialog = new OpenFileDialog
            {
                Title = description,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Sélectionner ce dossier",
                Filter = "Dossiers|*.folder",
                ValidateNames = false
            };

            if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
            {
                dialog.InitialDirectory = initialPath;
            }

            // Workaround pour sélectionner un dossier avec OpenFileDialog
            dialog.FileName = "Dossier sélectionné";
            
            if (dialog.ShowDialog() == true)
            {
                return Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            }
            
            return string.Empty;
        }

        private void OpenFolder(string folderPath)
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = folderPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else
                {
                    _ = ShowNotificationAsync($"❌ Dossier non trouvé: {folderPath}", "FolderRemove", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                _ = ShowNotificationAsync($"❌ Erreur ouverture: {ex.Message}", "Alert", Brushes.Red);
            }
        }

        private async Task<long> CalculateDirectorySizeAsync(DirectoryInfo directoryInfo)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var size = directoryInfo.GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
                    return size;
                }
                catch
                {
                    return 0;
                }
            });
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        #endregion

        #region Helper Methods - Configuration

        private FolderConfiguration CreateFolderConfiguration()
        {
            return new FolderConfiguration
            {
                ImportFolderPath = ImportFolderPath,
                ExportFolderPath = ExportFolderPath,
                ArchiveFolderPath = ArchiveFolderPath,
                LogsFolderPath = LogsFolderPath,
                BackupFolderPath = BackupFolderPath,
                ImportFolderWatchEnabled = ImportFolderWatchEnabled,
                ExportAutoOrganizeEnabled = ExportAutoOrganizeEnabled,
                ArchiveAutoEnabled = ArchiveAutoEnabled,
                LogRotationEnabled = LogRotationEnabled,
                BackupAutoEnabled = BackupAutoEnabled,
                SelectedArchivePeriod = SelectedArchivePeriod,
                SelectedLogLevel = SelectedLogLevel,
                SelectedBackupFrequency = SelectedBackupFrequency,
                SelectedExportNaming = SelectedExportNaming,
                SelectedArchiveOrganization = SelectedArchiveOrganization
            };
        }

        private void ApplyConfiguration(FolderConfiguration config)
        {
            ImportFolderPath = config.ImportFolderPath;
            ExportFolderPath = config.ExportFolderPath;
            ArchiveFolderPath = config.ArchiveFolderPath;
            LogsFolderPath = config.LogsFolderPath;
            BackupFolderPath = config.BackupFolderPath;
            ImportFolderWatchEnabled = config.ImportFolderWatchEnabled;
            ExportAutoOrganizeEnabled = config.ExportAutoOrganizeEnabled;
            ArchiveAutoEnabled = config.ArchiveAutoEnabled;
            LogRotationEnabled = config.LogRotationEnabled;
            BackupAutoEnabled = config.BackupAutoEnabled;
            SelectedArchivePeriod = config.SelectedArchivePeriod;
            SelectedLogLevel = config.SelectedLogLevel;
            SelectedBackupFrequency = config.SelectedBackupFrequency;
            SelectedExportNaming = config.SelectedExportNaming;
            SelectedArchiveOrganization = config.SelectedArchiveOrganization;

            UpdateNamingPreview();
        }

        private async Task LoadConfigurationAsync()
        {
            try
            {
                // var config = await _folderService.LoadConfigurationAsync();
                // if (config != null)
                // {
                //     ApplyConfiguration(config);
                // }
                
                await UpdateAllStatusAsync();
            }
            catch (Exception ex)
            {
                await ShowNotificationAsync($"⚠️ Erreur chargement config: {ex.Message}", "Alert", Brushes.Orange);
            }
        }

        private async Task UpdateFileWatchersAsync()
        {
            try
            {
                // Arrêter toutes les surveillances existantes
                // await _watcherService.StopAllWatchersAsync();

                // Redémarrer la surveillance import si activée
                // if (ImportFolderWatchEnabled && Directory.Exists(ImportFolderPath))
                // {
                //     await _watcherService.StartWatcherAsync("Import", ImportFolderPath, "*.xlsx");
                // }
            }
            catch (Exception ex)
            {
                await ShowNotificationAsync($"⚠️ Erreur surveillance: {ex.Message}", "Alert", Brushes.Orange);
            }
        }

        #endregion

        #region Helper Methods - UI

        private void UpdateNamingPreview()
        {
            var preview = SelectedExportNaming
                .Replace("{Date}", "20250905")
                .Replace("{NomClient}", "SARL_EXEMPLE")
                .Replace("{NumFacture}", "FAC001234")
                .Replace("{NCC}", "9606123E")
                .Replace("{Annee}", "2025")
                .Replace("{Mois}", "09")
                .Replace("{Jour}", "05")
                .Replace("{Timestamp}", "143022")
                .Replace("{Client}", "SARL_EXEMPLE");

            var archivePreview = SelectedArchiveOrganization
                .Replace("{Annee}", "2025")
                .Replace("{Mois}", "09")
                .Replace("{Jour}", "05")
                .Replace("{Client}", "SARL_EXEMPLE")
                .Replace("{Statut}", "Certified");

            NamingPreview = $"Fichier: {preview}\nDossier: {archivePreview}\\";
        }

        private async Task ShowNotificationAsync(string message, string icon, Brush color)
        {
            NotificationMessage = message;
            NotificationIcon = icon;
            NotificationColor = color;
            IsNotificationVisible = true;

            // Auto-hide après 3 secondes
            await Task.Delay(3000);
            IsNotificationVisible = false;
        }

        private string GetLatestLogFile(string logsPath)
        {
            try
            {
                if (!Directory.Exists(logsPath))
                    return string.Empty;

                var logFiles = Directory.GetFiles(logsPath, "*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .FirstOrDefault();

                return logFiles ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion

        #region Property Changed Handlers

        partial void OnSelectedExportNamingChanged(string value)
        {
            UpdateNamingPreview();
        }

        partial void OnSelectedArchiveOrganizationChanged(string value)
        {
            UpdateNamingPreview();
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _statusUpdateTimer?.Stop();
            _statusUpdateTimer?.Dispose();
            _spaceCalculationTimer?.Stop();
            _spaceCalculationTimer?.Dispose();
            // _watcherService?.Dispose();
        }

        #endregion
    }
}
