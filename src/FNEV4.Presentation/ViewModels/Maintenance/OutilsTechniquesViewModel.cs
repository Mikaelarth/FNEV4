using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using FNEV4.Infrastructure.Services;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System;
using FNEV4.Core.Interfaces;
using CoreLogging = FNEV4.Core.Interfaces.ILoggingService;

namespace FNEV4.Presentation.ViewModels.Maintenance
{
    /// <summary>
    /// ViewModel pour la vue Outils Techniques
    /// Gère les tests de connectivité et les outils avancés
    /// </summary>
    public partial class OutilsTechniquesViewModel : ObservableObject
    {
        #region Services
        private readonly IDiagnosticService _diagnosticService;
        private readonly CoreLogging _loggingService;
        private readonly IPathConfigurationService _pathConfigurationService;
        #endregion

        #region Properties

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string systemVersion = "FNEV4 v1.0.0";

        [ObservableProperty]
        private string buildVersion = "2024.09.05.1200";

        [ObservableProperty]
        private string frameworkVersion = ".NET 8.0";

        [ObservableProperty]
        private string operatingSystem = "Windows 11 Pro";

        [ObservableProperty]
        private string applicationUptime = "0h 0m 0s";

        [ObservableProperty]
        private string processId = "PID 0";

        [ObservableProperty]
        private string memoryUsage = "0 MB / 0 GB";

        [ObservableProperty]
        private string cpuUsage = "0.0%";

        [ObservableProperty]
        private string applicationPath = string.Empty;

        [ObservableProperty]
        private string dataPath = string.Empty;

        [ObservableProperty]
        private string logsPath = string.Empty;

        [ObservableProperty]
        private string databasePath = string.Empty;

        [ObservableProperty]
        private string fneApiUrl = string.Empty;

        [ObservableProperty]
        private string apiTimeout = "30000 ms";

        [ObservableProperty]
        private string retryAttempts = "3 tentatives";

        #endregion

        #region Commands

        public ICommand TestApiFneCommand { get; }
        public ICommand TestNetworkCommand { get; }
        public ICommand RunCompleteTestCommand { get; }
        public ICommand TestPerformanceCommand { get; }
        public ICommand RefreshSystemInfoCommand { get; }
        public ICommand CopyPathCommand { get; }

        #endregion

        #region Constructor

        public OutilsTechniquesViewModel(IDiagnosticService diagnosticService, CoreLogging loggingService, IPathConfigurationService pathConfigurationService = null)
        {
            _diagnosticService = diagnosticService ?? throw new ArgumentNullException(nameof(diagnosticService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _pathConfigurationService = pathConfigurationService ?? App.GetService<IPathConfigurationService>();

            // Initialisation des commandes
            TestApiFneCommand = new AsyncRelayCommand(TestApiFneAsync);
            TestNetworkCommand = new AsyncRelayCommand(TestNetworkAsync);
            RunCompleteTestCommand = new AsyncRelayCommand(RunCompleteTestAsync);
            TestPerformanceCommand = new AsyncRelayCommand(TestPerformanceAsync);
            RefreshSystemInfoCommand = new AsyncRelayCommand(RefreshSystemInfoAsync);
            CopyPathCommand = new RelayCommand<string>(CopyPath);

            // Chargement initial des données
            _ = Task.Run(async () => await LoadInitialDataAsync());
        }

        #endregion

        #region Private Methods

        private async Task LoadInitialDataAsync()
        {
            await RefreshSystemInfoAsync();
            LoadStaticInfo();
        }

        private void LoadStaticInfo()
        {
            ApplicationPath = Environment.CurrentDirectory;
            
            // Utiliser le service centralisé pour les chemins
            DataPath = _pathConfigurationService.DataRootPath;
            LogsPath = _pathConfigurationService.LogsFolderPath;
            DatabasePath = _pathConfigurationService.DatabasePath;
            
            ProcessId = $"PID {Environment.ProcessId}";
            FrameworkVersion = $".NET {Environment.Version.Major}.{Environment.Version.Minor}";
        }

        private async Task RefreshSystemInfoAsync()
        {
            try
            {
                var systemInfo = await _diagnosticService.GetSystemInfoAsync();
                
                App.Current.Dispatcher.Invoke(() =>
                {
                    OperatingSystem = systemInfo.OperatingSystem;
                    ApplicationUptime = FormatUptime(systemInfo.Uptime);
                    MemoryUsage = systemInfo.MemoryUsageFormatted;
                    CpuUsage = $"{systemInfo.CpuUsagePercent:F1}%";
                });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur lors du chargement des informations système: {ex.Message}", ex, "OutilsTechniques");
            }
        }

        private static string FormatUptime(TimeSpan uptime)
        {
            if (uptime.TotalDays >= 1)
                return $"{uptime.Days}j {uptime.Hours}h {uptime.Minutes}m";
            else if (uptime.TotalHours >= 1)
                return $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
            else
                return $"{uptime.Minutes}m {uptime.Seconds}s";
        }

        #endregion

        #region Command Handlers

        private async Task TestApiFneAsync()
        {
            IsLoading = true;
            try
            {
                await _loggingService.LogInformationAsync("Début du test de connectivité API FNE", "TestAPI");
                
                var result = await _diagnosticService.TestApiConnectivityAsync();
                
                if (result.Success)
                {
                    await _loggingService.LogInformationAsync($"Test API FNE réussi: {result.Message} ({result.Duration.TotalMilliseconds:F0}ms)", "TestAPI");
                }
                else
                {
                    await _loggingService.LogWarningAsync($"Test API FNE échoué: {result.Message}", "TestAPI");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur lors du test API FNE: {ex.Message}", ex, "TestAPI");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task TestNetworkAsync()
        {
            IsLoading = true;
            try
            {
                await _loggingService.LogInformationAsync("Début du test de connectivité réseau", "TestNetwork");
                
                var result = await _diagnosticService.TestNetworkConnectivityAsync();
                
                if (result.Success)
                {
                    await _loggingService.LogInformationAsync($"Test réseau réussi: {result.Message} ({result.Duration.TotalMilliseconds:F0}ms)", "TestNetwork");
                }
                else
                {
                    await _loggingService.LogWarningAsync($"Test réseau échoué: {result.Message}", "TestNetwork");
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur lors du test réseau: {ex.Message}", ex, "TestNetwork");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RunCompleteTestAsync()
        {
            IsLoading = true;
            try
            {
                await _loggingService.LogInformationAsync("Début du diagnostic complet du système", "DiagnosticComplet");
                
                var diagnostic = await _diagnosticService.RunCompleteDiagnosticAsync();
                
                var status = diagnostic.OverallSuccess 
                    ? $"Diagnostic complet terminé - Système OK ({diagnostic.TotalDuration.TotalSeconds:F1}s)"
                    : $"Diagnostic complet terminé - {diagnostic.IssuesFound} problème(s) détecté(s) ({diagnostic.TotalDuration.TotalSeconds:F1}s)";

                await _loggingService.LogInformationAsync(status, "DiagnosticComplet");
                
                // Afficher un résumé
                ShowCompleteDiagnosticSummary(diagnostic);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur lors du diagnostic complet: {ex.Message}", ex, "DiagnosticComplet");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task TestPerformanceAsync()
        {
            IsLoading = true;
            try
            {
                await _loggingService.LogInformationAsync("Début du test de performance", "TestPerformance");
                
                // Test de performance CPU
                var stopwatch = Stopwatch.StartNew();
                var cpuTest = await TestCpuPerformanceAsync();
                stopwatch.Stop();
                
                await _loggingService.LogInformationAsync($"Test CPU terminé: {cpuTest} opérations en {stopwatch.ElapsedMilliseconds}ms", "TestPerformance");
                
                // Test de performance mémoire
                stopwatch.Restart();
                var memoryTest = TestMemoryPerformance();
                stopwatch.Stop();
                
                await _loggingService.LogInformationAsync($"Test mémoire terminé: {memoryTest:F2} MB alloués en {stopwatch.ElapsedMilliseconds}ms", "TestPerformance");
                
                // Test de performance I/O
                stopwatch.Restart();
                var ioTest = await TestIOPerformanceAsync();
                stopwatch.Stop();
                
                await _loggingService.LogInformationAsync($"Test I/O terminé: {ioTest} octets en {stopwatch.ElapsedMilliseconds}ms", "TestPerformance");
                
                await _loggingService.LogInformationAsync("Tests de performance terminés", "TestPerformance");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur lors du test de performance: {ex.Message}", ex, "TestPerformance");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<int> TestCpuPerformanceAsync()
        {
            return await Task.Run(() =>
            {
                int operations = 0;
                var endTime = DateTime.Now.AddSeconds(2);
                
                while (DateTime.Now < endTime)
                {
                    Math.Sqrt(operations);
                    operations++;
                }
                
                return operations;
            });
        }

        private double TestMemoryPerformance()
        {
            var startMemory = GC.GetTotalMemory(false);
            
            // Allouer de la mémoire
            var data = new List<byte[]>();
            for (int i = 0; i < 1000; i++)
            {
                data.Add(new byte[1024]); // 1KB par allocation
            }
            
            var endMemory = GC.GetTotalMemory(false);
            var allocatedBytes = endMemory - startMemory;
            
            // Nettoyer
            data.Clear();
            GC.Collect();
            
            return allocatedBytes / (1024.0 * 1024.0); // Conversion en MB
        }

        private async Task<long> TestIOPerformanceAsync()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var testData = new byte[1024 * 1024]; // 1MB
                new Random().NextBytes(testData);
                
                await File.WriteAllBytesAsync(tempFile, testData);
                var readData = await File.ReadAllBytesAsync(tempFile);
                
                return readData.Length;
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        private void CopyPath(string? path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                System.Windows.Clipboard.SetText(path);
                _ = Task.Run(async () => await _loggingService.LogInformationAsync($"Chemin copié: {path}", "OutilsTechniques"));
            }
        }

        private void ShowCompleteDiagnosticSummary(CompleteDiagnostic diagnostic)
        {
            var summary = $"=== DIAGNOSTIC COMPLET ===\n\n";
            summary += $"Durée totale: {diagnostic.TotalDuration.TotalSeconds:F1}s\n";
            summary += $"Problèmes détectés: {diagnostic.IssuesFound}\n\n";
            
            summary += $"• Base de données: {(diagnostic.DatabaseTest.Success ? "✓ OK" : "✗ Problème")}\n";
            summary += $"• Réseau: {(diagnostic.NetworkTest.Success ? "✓ OK" : "✗ Problème")}\n";
            summary += $"• API FNE: {(diagnostic.ApiTest.Success ? "✓ OK" : "✗ Problème")}\n";
            summary += $"• Intégrité: {(diagnostic.IntegrityCheck.Success ? "✓ OK" : "✗ Problème")}\n\n";
            
            if (diagnostic.Recommendations.Any())
            {
                summary += "RECOMMANDATIONS:\n";
                foreach (var recommendation in diagnostic.Recommendations)
                {
                    summary += $"• {recommendation}\n";
                }
            }

            System.Windows.MessageBox.Show(
                summary,
                "Résumé du diagnostic complet",
                System.Windows.MessageBoxButton.OK,
                diagnostic.OverallSuccess ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Warning);
        }

        #endregion
    }
}
