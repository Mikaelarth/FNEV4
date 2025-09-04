using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using FNEV4.Infrastructure.Data;
using FNEV4.Infrastructure.Services;

namespace FNEV4.Presentation.ViewModels.Maintenance
{
    /// <summary>
    /// ViewModel ultra-simple pour la gestion de la base de données
    /// </summary>
    public class BaseDonneesViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Propriétés avec notification

        private string _databasePath = string.Empty;
        public string DatabasePath
        {
            get => _databasePath;
            set { _databasePath = value; OnPropertyChanged(nameof(DatabasePath)); }
        }

        private string _databaseSize = "0 KB";
        public string DatabaseSize
        {
            get => _databaseSize;
            set { _databaseSize = value; OnPropertyChanged(nameof(DatabaseSize)); }
        }

        private string _databaseVersion = "SQLite";
        public string DatabaseVersion
        {
            get => _databaseVersion;
            set { _databaseVersion = value; OnPropertyChanged(nameof(DatabaseVersion)); }
        }

        private string _connectionStatus = "Déconnectée";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set { _connectionStatus = value; OnPropertyChanged(nameof(ConnectionStatus)); }
        }

        private Brush _connectionStatusColor = Brushes.Red;
        public Brush ConnectionStatusColor
        {
            get => _connectionStatusColor;
            set { _connectionStatusColor = value; OnPropertyChanged(nameof(ConnectionStatusColor)); }
        }

        private string _lastBackupDate = "Jamais";
        public string LastBackupDate
        {
            get => _lastBackupDate;
            set { _lastBackupDate = value; OnPropertyChanged(nameof(LastBackupDate)); }
        }

        private int _tableCount = 0;
        public int TableCount
        {
            get => _tableCount;
            set { _tableCount = value; OnPropertyChanged(nameof(TableCount)); }
        }

        private ObservableCollection<TableInfoViewModel> _tables = new();
        public ObservableCollection<TableInfoViewModel> Tables
        {
            get => _tables;
            set { _tables = value; OnPropertyChanged(nameof(Tables)); }
        }

        private string _tableSearchText = string.Empty;
        public string TableSearchText
        {
            get => _tableSearchText;
            set { _tableSearchText = value; OnPropertyChanged(nameof(TableSearchText)); }
        }

        private bool _autoBackupEnabled = true;
        public bool AutoBackupEnabled
        {
            get => _autoBackupEnabled;
            set { _autoBackupEnabled = value; OnPropertyChanged(nameof(AutoBackupEnabled)); }
        }

        private string _sqlQuery = string.Empty;
        public string SqlQuery
        {
            get => _sqlQuery;
            set { _sqlQuery = value; OnPropertyChanged(nameof(SqlQuery)); }
        }

        private string _sqlResults = string.Empty;
        public string SqlResults
        {
            get => _sqlResults;
            set { _sqlResults = value; OnPropertyChanged(nameof(SqlResults)); }
        }

        #endregion

        #region Commandes

        public ICommand RefreshCommand { get; private set; }
        public ICommand SettingsCommand { get; private set; }
        public ICommand RefreshTablesCommand { get; private set; }
        public ICommand ViewTableDataCommand { get; private set; }
        public ICommand ViewTableStructureCommand { get; private set; }
        public ICommand BackupDatabaseCommand { get; private set; }
        public ICommand RestoreDatabaseCommand { get; private set; }
        public ICommand OptimizeDatabaseCommand { get; private set; }
        public ICommand ReindexDatabaseCommand { get; private set; }
        public ICommand CheckIntegrityCommand { get; private set; }
        public ICommand ApplyMigrationsCommand { get; private set; }
        public ICommand InitializeDatabaseCommand { get; private set; }
        public ICommand ExecuteSqlCommand { get; private set; }
        public ICommand ClearSqlCommand { get; private set; }
        public ICommand TestCommand { get; private set; }

        #endregion

        // Constructeur par défaut pour test
        public BaseDonneesViewModel() 
        {
            // Créer manuellement le service pour test
            try
            {
                var connectionString = "Data Source=Data/FNEV4.db";
                var options = new DbContextOptionsBuilder<FNEV4DbContext>()
                    .UseSqlite(connectionString)
                    .Options;
                
                var context = new FNEV4DbContext(options);
                _databaseService = new DatabaseService(context);
            }
            catch
            {
                _databaseService = null!;
            }

            InitializeCommands();
        }

        public BaseDonneesViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            RefreshCommand = new RelayCommand(async () => await RefreshDatabaseInfoAsync());
            SettingsCommand = new RelayCommand(OpenDatabaseSettings);
            RefreshTablesCommand = new RelayCommand(async () => await RefreshTablesAsync());
            ViewTableDataCommand = new RelayCommand<TableInfoViewModel>(ViewTableData);
            ViewTableStructureCommand = new RelayCommand<TableInfoViewModel>(ViewTableStructure);
            BackupDatabaseCommand = new RelayCommand(async () => await BackupDatabaseAsync());
            RestoreDatabaseCommand = new RelayCommand(async () => await RestoreDatabaseAsync());
            OptimizeDatabaseCommand = new RelayCommand(async () => await OptimizeDatabaseAsync());
            ReindexDatabaseCommand = new RelayCommand(async () => await ReindexDatabaseAsync());
            CheckIntegrityCommand = new RelayCommand(async () => await CheckDatabaseIntegrityAsync());
            ApplyMigrationsCommand = new RelayCommand(async () => await ApplyMigrationsAsync());
            InitializeDatabaseCommand = new RelayCommand(async () => await InitializeDatabaseAsync());
            ExecuteSqlCommand = new RelayCommand(async () => await ExecuteSqlQueryAsync());
            ClearSqlCommand = new RelayCommand(ClearSqlQuery);
            TestCommand = new RelayCommand(TestMethod);

            // Chargement initial des données
            _ = Task.Run(async () => await LoadDataAsync());
        }

        #region Méthodes privées

        private async Task LoadDataAsync()
        {
            await RefreshDatabaseInfoAsync();
            await RefreshTablesAsync();
        }

        private async Task RefreshDatabaseInfoAsync()
        {
            try
            {
                // Actualiser les informations générales
                var info = await _databaseService.GetDatabaseInfoAsync();
                
                DatabasePath = info.Path;
                DatabaseSize = info.Size;
                DatabaseVersion = info.Version;
                ConnectionStatus = info.ConnectionStatus;
                ConnectionStatusColor = info.ConnectionStatus == "Connectée" ? Brushes.Green : Brushes.Red;
                TableCount = info.TableCount;
                
                if (info.LastModified.HasValue)
                {
                    LastBackupDate = info.LastModified.Value.ToString("dd/MM/yyyy HH:mm");
                }

                // Actualiser aussi la liste des tables pour une actualisation complète
                await RefreshTablesAsync();

                SqlResults = $"Actualisation complète terminée.\n" +
                           $"✓ Informations de la base de données mises à jour\n" +
                           $"✓ Liste des tables actualisée ({TableCount} tables)\n" +
                           $"✓ Statistiques des tables rafraîchies";
            }
            catch (Exception ex)
            {
                SqlResults = $"Erreur lors de l'actualisation: {ex.Message}";
                ConnectionStatus = "Erreur";
                ConnectionStatusColor = Brushes.Red;
            }
        }

        private void OpenDatabaseSettings()
        {
            try
            {
                var settingsDialog = new Views.Maintenance.DatabaseSettingsDialog();
                var settingsViewModel = new DatabaseSettingsViewModel();
                
                settingsDialog.DataContext = settingsViewModel;
                settingsViewModel.SetDialogWindow(settingsDialog);
                
                var result = settingsDialog.ShowDialog(Application.Current.MainWindow);
                
                if (result == true)
                {
                    // Les paramètres ont été appliqués, actualiser l'affichage
                    _ = RefreshDatabaseInfoAsync();
                    SqlResults = "✓ Paramètres de base de données mis à jour avec succès.";
                }
                else
                {
                    SqlResults = "Paramètres de base de données annulés.";
                }
            }
            catch (Exception ex)
            {
                SqlResults = $"❌ Erreur lors de l'ouverture des paramètres : {ex.Message}";
            }
        }

        private async Task RefreshTablesAsync()
        {
            try
            {
                var tablesInfo = await _databaseService.GetTablesInfoAsync();
                Tables.Clear();
                
                foreach (var table in tablesInfo)
                {
                    Tables.Add(new TableInfoViewModel
                    {
                        Name = table.Name,
                        RowCount = table.RowCount,
                        Size = table.Size,
                        LastModified = table.LastModified
                    });
                }

                SqlResults = $"Liste des tables actualisée. {Tables.Count} tables trouvées.";
                TableCount = Tables.Count;
            }
            catch (Exception ex)
            {
                SqlResults = $"Erreur lors de l'actualisation des tables: {ex.Message}";
            }
        }

        private void ViewTableData(TableInfoViewModel? table)
        {
            if (table == null) return;
            
            SqlResults = $"Affichage des données de la table '{table.Name}' ({table.RowCount} lignes).\n" +
                        "Fonctionnalité complète à implémenter...";
        }

        private void ViewTableStructure(TableInfoViewModel? table)
        {
            if (table == null) return;
            
            SqlResults = $"Structure de la table '{table.Name}' :\n" +
                        "Fonctionnalité complète à implémenter...";
        }

        private async Task BackupDatabaseAsync()
        {
            try
            {
                var backupPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"FNEV4_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

                var success = await _databaseService.BackupDatabaseAsync(backupPath);
                
                if (success)
                {
                    SqlResults = $"Sauvegarde créée avec succès :\n{backupPath}";
                    LastBackupDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                }
                else
                {
                    SqlResults = "Erreur lors de la création de la sauvegarde.";
                }
            }
            catch (Exception ex)
            {
                SqlResults = $"Erreur lors de la sauvegarde: {ex.Message}";
            }
        }

        private async Task RestoreDatabaseAsync()
        {
            SqlResults = "Sélection du fichier de restauration à implémenter...";
            await Task.CompletedTask;
        }

        private async Task OptimizeDatabaseAsync()
        {
            try
            {
                var success = await _databaseService.OptimizeDatabaseAsync();
                
                if (success)
                {
                    SqlResults = "Optimisation de la base de données terminée avec succès.";
                    await RefreshDatabaseInfoAsync();
                }
                else
                {
                    SqlResults = "Erreur lors de l'optimisation de la base de données.";
                }
            }
            catch (Exception ex)
            {
                SqlResults = $"Erreur lors de l'optimisation: {ex.Message}";
            }
        }

        private async Task ReindexDatabaseAsync()
        {
            SqlResults = "Réindexation terminée (fonctionnalité simulée).";
            await Task.CompletedTask;
        }

        private async Task CheckDatabaseIntegrityAsync()
        {
            try
            {
                var isIntegrityOk = await _databaseService.CheckIntegrityAsync();
                
                if (isIntegrityOk)
                {
                    SqlResults = "✓ Vérification d'intégrité terminée avec succès.\n" +
                               "✓ Aucune corruption détectée.";
                }
                else
                {
                    SqlResults = "⚠️ Problèmes d'intégrité détectés dans la base de données.";
                }
            }
            catch (Exception ex)
            {
                SqlResults = $"Erreur lors de la vérification d'intégrité: {ex.Message}";
            }
        }

        private async Task ApplyMigrationsAsync()
        {
            try
            {
                var success = await _databaseService.ApplyMigrationsAsync();
                
                if (success)
                {
                    SqlResults = "Migrations appliquées avec succès.";
                    await RefreshDatabaseInfoAsync();
                    await RefreshTablesAsync();
                }
                else
                {
                    SqlResults = "Erreur lors de l'application des migrations.";
                }
            }
            catch (Exception ex)
            {
                SqlResults = $"Erreur lors de l'application des migrations: {ex.Message}";
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                SqlResults = "⚠️ ATTENTION: Cette opération va supprimer toutes les données!\n" +
                           "Initialisation en cours...";
                
                var success = await _databaseService.InitializeDatabaseAsync();
                
                if (success)
                {
                    SqlResults += "\n✓ Base de données initialisée avec succès.";
                    await RefreshDatabaseInfoAsync();
                    await RefreshTablesAsync();
                }
                else
                {
                    SqlResults += "\n❌ Erreur lors de l'initialisation.";
                }
            }
            catch (Exception ex)
            {
                SqlResults = $"Erreur lors de l'initialisation: {ex.Message}";
            }
        }

        private async Task ExecuteSqlQueryAsync()
        {
            if (string.IsNullOrWhiteSpace(SqlQuery))
            {
                SqlResults = "Veuillez saisir une requête SQL.";
                return;
            }

            try
            {
                var result = await _databaseService.ExecuteQueryAsync(SqlQuery);
                SqlResults = result;
            }
            catch (Exception ex)
            {
                SqlResults = $"Erreur lors de l'exécution de la requête: {ex.Message}";
            }
        }

        private void ClearSqlQuery()
        {
            SqlQuery = string.Empty;
            SqlResults = string.Empty;
        }

        private void TestMethod()
        {
            SqlResults = $"🔥 TEST BUTTON CLICKED! 🔥\n" +
                        $"Timestamp: {DateTime.Now:HH:mm:ss}\n" +
                        $"DatabaseService: {(_databaseService != null ? "✓ OK" : "❌ NULL")}\n" +
                        $"DataContext working!";
            
            System.Windows.MessageBox.Show("Test button clicked!", "Debug", System.Windows.MessageBoxButton.OK);
        }

        #endregion
    }

    /// <summary>
    /// ViewModel pour les informations d'une table
    /// </summary>
    public class TableInfoViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public string Size { get; set; } = string.Empty;
        public string LastModified { get; set; } = string.Empty;
    }

    /// <summary>
    /// Commande simplifiée pour WPF
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }

    /// <summary>
    /// Commande simplifiée avec paramètre pour WPF
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke((T?)parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute((T?)parameter);
        }
    }
}
