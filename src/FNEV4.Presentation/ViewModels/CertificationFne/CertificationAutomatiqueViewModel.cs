using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using FNEV4.Core.Interfaces.Services.Fne;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Windows.Threading;

namespace FNEV4.Presentation.ViewModels.CertificationFne
{
    /// <summary>
    /// ViewModel pour la certification automatique FNE
    /// Surveille automatiquement les factures à certifier et lance la certification en arrière-plan
    /// </summary>
    public partial class CertificationAutomatiqueViewModel : ObservableObject
    {
        #region Services injectés
        
        private readonly IFneCertificationService _certificationService;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPathConfigurationService _pathService;
        private readonly ILoggingService _loggingService;
        private readonly ILogger<CertificationAutomatiqueViewModel> _logger;
        private readonly DispatcherTimer _autoCheckTimer;
        private readonly FileSystemWatcher? _folderWatcher;
        
        #endregion

        #region Propriétés observables

        [ObservableProperty]
        private bool _isAutoModeEnabled = false;

        [ObservableProperty]
        private int _autoCheckIntervalMinutes = 5;

        [ObservableProperty]
        private bool _certifyOnImport = true;

        [ObservableProperty]
        private bool _certifyDraftInvoices = true;

        [ObservableProperty]
        private bool _certifyValidatedInvoices = true;

        [ObservableProperty]
        private bool _retryErrorInvoices = false;

        [ObservableProperty]
        private int _maxRetryAttempts = 3;

        [ObservableProperty]
        private string _currentStatus = "Mode automatique désactivé";

        [ObservableProperty]
        private DateTime? _lastCheckTime;

        [ObservableProperty]
        private DateTime? _nextCheckTime;

        [ObservableProperty]
        private bool _isCertificationRunning = false;

        [ObservableProperty]
        private int _pendingInvoicesCount = 0;

        [ObservableProperty]
        private int _totalCertifiedToday = 0;

        [ObservableProperty]
        private int _totalErrorsToday = 0;

        [ObservableProperty]
        private decimal _totalAmountCertifiedToday = 0;

        [ObservableProperty]
        private ObservableCollection<AutoCertificationLogEntry> _recentActivity = new();

        [ObservableProperty]
        private string _configurationSummary = "Configuration automatique prête";

        [ObservableProperty]
        private bool _hasActiveConfiguration = false;

        [ObservableProperty]
        private string _nextActionMessage = "En attente...";

        #endregion

        #region Collections

        public List<int> AvailableIntervals { get; } = new() { 1, 2, 5, 10, 15, 30, 60 };

        #endregion

        #region Constructor

        public CertificationAutomatiqueViewModel(
            IFneCertificationService certificationService,
            IInvoiceRepository invoiceRepository,
            IPathConfigurationService pathService,
            ILoggingService loggingService,
            ILogger<CertificationAutomatiqueViewModel> logger)
        {
            _certificationService = certificationService ?? throw new ArgumentNullException(nameof(certificationService));
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configuration du timer automatique
            _autoCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(AutoCheckIntervalMinutes)
            };
            _autoCheckTimer.Tick += OnAutoCheckTimer;

            // Configuration du surveillance de dossier si disponible
            try
            {
                if (!string.IsNullOrEmpty(_pathService.ImportFolderPath) && 
                    Directory.Exists(_pathService.ImportFolderPath))
                {
                    _folderWatcher = new FileSystemWatcher(_pathService.ImportFolderPath, "*.xlsx")
                    {
                        NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                        EnableRaisingEvents = false
                    };
                    _folderWatcher.Created += OnFileImported;
                    _folderWatcher.Changed += OnFileImported;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible d'initialiser la surveillance du dossier d'import");
            }

            // Initialisation des données
            _ = InitializeAsync();
            
            _logger.LogInformation("CertificationAutomatiqueViewModel initialisé");
        }

        #endregion

        #region Commandes

        [RelayCommand]
        private async Task ToggleAutoModeAsync()
        {
            if (IsAutoModeEnabled)
            {
                await StopAutoModeAsync();
            }
            else
            {
                await StartAutoModeAsync();
            }
        }

        [RelayCommand]
        private async Task StartAutoModeAsync()
        {
            try
            {
                _logger.LogInformation("Activation du mode certification automatique");

                // Vérifier la configuration
                var hasConfig = await CheckFneConfigurationAsync();
                if (!hasConfig)
                {
                    MessageBox.Show("Configuration FNE manquante ou inactive.\nVeuillez configurer l'API FNE avant d'activer le mode automatique.",
                        "Configuration manquante", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsAutoModeEnabled = true;
                CurrentStatus = "Mode automatique activé";
                
                // Démarrer le timer
                UpdateTimerInterval();
                _autoCheckTimer.Start();
                
                // Activer la surveillance de dossier si disponible
                if (_folderWatcher != null)
                {
                    _folderWatcher.EnableRaisingEvents = CertifyOnImport;
                }

                // Première vérification immédiate
                _ = PerformAutomaticCheckAsync();
                
                UpdateNextCheckTime();
                AddActivityLog("Mode automatique activé", "Info");
                
                _logger.LogInformation("Mode certification automatique activé avec intervalle de {Minutes} minutes", AutoCheckIntervalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'activation du mode automatique");
                CurrentStatus = "Erreur lors de l'activation";
                MessageBox.Show($"Erreur lors de l'activation: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task StopAutoModeAsync()
        {
            try
            {
                _logger.LogInformation("Désactivation du mode certification automatique");

                IsAutoModeEnabled = false;
                CurrentStatus = "Mode automatique désactivé";
                
                // Arrêter le timer
                _autoCheckTimer.Stop();
                
                // Désactiver la surveillance de dossier
                if (_folderWatcher != null)
                {
                    _folderWatcher.EnableRaisingEvents = false;
                }

                LastCheckTime = DateTime.Now;
                NextCheckTime = null;
                NextActionMessage = "Mode automatique arrêté";
                
                AddActivityLog("Mode automatique désactivé", "Info");
                
                _logger.LogInformation("Mode certification automatique désactivé");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la désactivation du mode automatique");
                MessageBox.Show($"Erreur lors de la désactivation: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task RunManualCheckAsync()
        {
            if (IsCertificationRunning)
            {
                MessageBox.Show("Une certification est déjà en cours.", "Certification en cours", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await PerformAutomaticCheckAsync();
        }

        [RelayCommand]
        private void SaveConfiguration()
        {
            try
            {
                UpdateTimerInterval();
                UpdateConfigurationSummary();
                AddActivityLog("Configuration sauvegardée", "Info");
                
                _logger.LogInformation("Configuration de certification automatique sauvegardée");
                
                MessageBox.Show("Configuration sauvegardée avec succès.", "Configuration", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde de configuration");
                MessageBox.Show($"Erreur lors de la sauvegarde: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ClearActivityLog()
        {
            RecentActivity.Clear();
            AddActivityLog("Journal d'activité vidé", "Info");
        }

        [RelayCommand]
        private async Task ViewPendingInvoicesAsync()
        {
            try
            {
                var pendingInvoices = await GetPendingInvoicesAsync();
                
                if (!pendingInvoices.Any())
                {
                    MessageBox.Show("Aucune facture en attente de certification.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Créer un dialogue simple pour afficher les factures en attente
                var message = $"Factures en attente de certification ({pendingInvoices.Count}):\n\n";
                message += string.Join("\n", pendingInvoices.Take(10).Select(i => $"• {i.InvoiceNumber} - {i.Client?.CompanyName} - {i.TotalAmountTTC:N2} TND"));
                
                if (pendingInvoices.Count > 10)
                {
                    message += $"\n... et {pendingInvoices.Count - 10} autres";
                }

                MessageBox.Show(message, "Factures en attente", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affichage des factures en attente");
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Méthodes privées

        private async Task InitializeAsync()
        {
            try
            {
                await CheckFneConfigurationAsync();
                await UpdatePendingCountAsync();
                await UpdateTodayStatisticsAsync();
                UpdateConfigurationSummary();
                
                AddActivityLog("Système de certification automatique initialisé", "Info");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation");
                CurrentStatus = "Erreur d'initialisation";
            }
        }

        private async Task<bool> CheckFneConfigurationAsync()
        {
            try
            {
                var dbContext = App.ServiceProvider.GetRequiredService<FNEV4.Infrastructure.Data.FNEV4DbContext>();
                
                var activeConfig = await dbContext.FneConfigurations
                    .Where(c => c.IsActive && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                HasActiveConfiguration = activeConfig != null;
                
                if (HasActiveConfiguration && activeConfig != null)
                {
                    var configInfo = $"Configuration: {activeConfig.ConfigurationName} ({activeConfig.Environment})";
                    if (activeConfig.IsValidatedByDgi)
                    {
                        configInfo += " ✓ Validée DGI";
                    }
                    ConfigurationSummary = configInfo;
                }
                else
                {
                    ConfigurationSummary = "❌ Aucune configuration FNE active";
                }

                return HasActiveConfiguration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de la configuration FNE");
                return false;
            }
        }

        private async Task<List<FneInvoice>> GetPendingInvoicesAsync()
        {
            var criteria = new List<string>();
            if (CertifyDraftInvoices) criteria.Add("Draft");
            if (CertifyValidatedInvoices) criteria.Add("Validated");
            if (RetryErrorInvoices) criteria.Add("Error");

            var allInvoices = await _invoiceRepository.GetAllAsync();
            
            return allInvoices
                .Where(i => criteria.Contains(i.Status) && 
                           (!RetryErrorInvoices || i.RetryCount < MaxRetryAttempts))
                .ToList();
        }

        private async Task PerformAutomaticCheckAsync()
        {
            if (IsCertificationRunning) return;

            try
            {
                IsCertificationRunning = true;
                CurrentStatus = "Vérification des factures à certifier...";
                LastCheckTime = DateTime.Now;

                var pendingInvoices = await GetPendingInvoicesAsync();
                PendingInvoicesCount = pendingInvoices.Count;

                if (!pendingInvoices.Any())
                {
                    CurrentStatus = "Aucune facture à certifier";
                    NextActionMessage = "Prochaine vérification programmée";
                    return;
                }

                CurrentStatus = $"Certification de {pendingInvoices.Count} facture(s) en cours...";
                
                var configuration = await GetActiveFneConfigurationAsync();
                if (configuration == null)
                {
                    CurrentStatus = "❌ Configuration FNE introuvable";
                    AddActivityLog("Configuration FNE introuvable", "Error");
                    return;
                }

                int successCount = 0;
                int errorCount = 0;

                foreach (var invoice in pendingInvoices)
                {
                    try
                    {
                        CurrentStatus = $"Certification {invoice.InvoiceNumber}...";
                        
                        var result = await _certificationService.CertifyInvoiceAsync(invoice, configuration);
                        
                        if (result.IsSuccess)
                        {
                            successCount++;
                            AddActivityLog($"✅ {invoice.InvoiceNumber} certifiée", "Success");
                        }
                        else
                        {
                            errorCount++;
                            AddActivityLog($"❌ {invoice.InvoiceNumber}: {result.ErrorMessage}", "Error");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        AddActivityLog($"❌ {invoice.InvoiceNumber}: Exception - {ex.Message}", "Error");
                        _logger.LogError(ex, "Erreur certification automatique facture {InvoiceNumber}", invoice.InvoiceNumber);
                    }
                    
                    // Petit délai pour éviter de surcharger l'API
                    await Task.Delay(200);
                }

                // Mise à jour des statistiques
                TotalCertifiedToday += successCount;
                TotalErrorsToday += errorCount;
                
                await UpdateTodayStatisticsAsync();
                await UpdatePendingCountAsync();

                CurrentStatus = $"✅ Certification automatique terminée: {successCount} succès, {errorCount} erreurs";
                AddActivityLog($"Certification automatique: {successCount} succès, {errorCount} erreurs", successCount > 0 ? "Success" : "Warning");

                _logger.LogInformation("Certification automatique terminée: {Success} succès, {Errors} erreurs", successCount, errorCount);
            }
            catch (Exception ex)
            {
                CurrentStatus = "❌ Erreur lors de la certification automatique";
                AddActivityLog($"Erreur certification automatique: {ex.Message}", "Error");
                _logger.LogError(ex, "Erreur lors de la certification automatique");
            }
            finally
            {
                IsCertificationRunning = false;
                UpdateNextCheckTime();
                NextActionMessage = IsAutoModeEnabled ? $"Prochaine vérification dans {AutoCheckIntervalMinutes} min" : "Mode automatique arrêté";
            }
        }

        private async Task<FneConfiguration?> GetActiveFneConfigurationAsync()
        {
            try
            {
                var dbContext = App.ServiceProvider.GetRequiredService<FNEV4.Infrastructure.Data.FNEV4DbContext>();
                
                return await dbContext.FneConfigurations
                    .Where(c => c.IsActive && !c.IsDeleted)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur récupération configuration FNE active");
                return null;
            }
        }

        private async Task UpdatePendingCountAsync()
        {
            try
            {
                var pending = await GetPendingInvoicesAsync();
                PendingInvoicesCount = pending.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur mise à jour compteur factures en attente");
            }
        }

        private async Task UpdateTodayStatisticsAsync()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                
                var allInvoices = await _invoiceRepository.GetAllAsync();
                
                var todayInvoices = allInvoices
                    .Where(i => i.CertifiedAt.HasValue && 
                               i.CertifiedAt.Value >= today && 
                               i.CertifiedAt.Value < tomorrow)
                    .ToList();

                TotalCertifiedToday = todayInvoices.Count(i => i.Status == "Certified");
                TotalAmountCertifiedToday = todayInvoices
                    .Where(i => i.Status == "Certified")
                    .Sum(i => i.TotalAmountTTC);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur mise à jour statistiques du jour");
            }
        }

        private void UpdateTimerInterval()
        {
            if (_autoCheckTimer != null)
            {
                _autoCheckTimer.Interval = TimeSpan.FromMinutes(AutoCheckIntervalMinutes);
                UpdateNextCheckTime();
            }
        }

        private void UpdateNextCheckTime()
        {
            if (IsAutoModeEnabled && !IsCertificationRunning)
            {
                NextCheckTime = DateTime.Now.AddMinutes(AutoCheckIntervalMinutes);
            }
            else
            {
                NextCheckTime = null;
            }
        }

        private void UpdateConfigurationSummary()
        {
            var parts = new List<string>();
            
            parts.Add($"Intervalle: {AutoCheckIntervalMinutes} min");
            
            if (CertifyOnImport) parts.Add("Import auto");
            if (CertifyDraftInvoices) parts.Add("Brouillons");
            if (CertifyValidatedInvoices) parts.Add("Validées");
            if (RetryErrorInvoices) parts.Add($"Retry (max {MaxRetryAttempts})");
            
            ConfigurationSummary = string.Join(" • ", parts);
        }

        private void AddActivityLog(string message, string level)
        {
            var entry = new AutoCertificationLogEntry
            {
                Timestamp = DateTime.Now,
                Message = message,
                Level = level
            };
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                RecentActivity.Insert(0, entry);
                
                // Garder seulement les 50 dernières entrées
                while (RecentActivity.Count > 50)
                {
                    RecentActivity.RemoveAt(RecentActivity.Count - 1);
                }
            });
        }

        private async void OnAutoCheckTimer(object? sender, EventArgs e)
        {
            if (!IsCertificationRunning)
            {
                await PerformAutomaticCheckAsync();
            }
        }

        private async void OnFileImported(object sender, FileSystemEventArgs e)
        {
            if (!CertifyOnImport || !IsAutoModeEnabled) return;

            try
            {
                // Attendre un peu pour s'assurer que le fichier est complètement écrit
                await Task.Delay(2000);
                
                AddActivityLog($"Fichier détecté: {Path.GetFileName(e.FullPath)}", "Info");
                
                // Déclencher une vérification
                _ = PerformAutomaticCheckAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement du fichier importé {FilePath}", e.FullPath);
            }
        }

        #endregion

        #region Property change handlers

        partial void OnAutoCheckIntervalMinutesChanged(int value)
        {
            UpdateTimerInterval();
            UpdateConfigurationSummary();
        }

        partial void OnCertifyOnImportChanged(bool value)
        {
            if (_folderWatcher != null && IsAutoModeEnabled)
            {
                _folderWatcher.EnableRaisingEvents = value;
            }
            UpdateConfigurationSummary();
        }

        partial void OnCertifyDraftInvoicesChanged(bool value)
        {
            UpdateConfigurationSummary();
            _ = UpdatePendingCountAsync();
        }

        partial void OnCertifyValidatedInvoicesChanged(bool value)
        {
            UpdateConfigurationSummary();
            _ = UpdatePendingCountAsync();
        }

        partial void OnRetryErrorInvoicesChanged(bool value)
        {
            UpdateConfigurationSummary();
            _ = UpdatePendingCountAsync();
        }

        partial void OnMaxRetryAttemptsChanged(int value)
        {
            UpdateConfigurationSummary();
            _ = UpdatePendingCountAsync();
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _autoCheckTimer?.Stop();
            _folderWatcher?.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Entrée du journal d'activité de certification automatique
    /// </summary>
    public class AutoCertificationLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Level { get; set; } = "Info"; // Info, Success, Warning, Error
        
        public string FormattedTime => Timestamp.ToString("HH:mm:ss");
        public string LevelIcon => Level switch
        {
            "Success" => "✅",
            "Warning" => "⚠️",
            "Error" => "❌",
            _ => "ℹ️"
        };
    }
}