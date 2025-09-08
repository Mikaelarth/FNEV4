using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using FNEV4.Infrastructure.Data;
using FNEV4.Infrastructure.Services;
using FNEV4.Presentation.Services;
using FNEV4.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FNEV4.Presentation.ViewModels.Maintenance
{
    /// <summary>
    /// ViewModel ultra-simple pour la gestion de la base de données
    /// </summary>
    public class BaseDonneesViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private readonly IDatabaseConfigurationNotificationService? _notificationService;
        private readonly IPathConfigurationService _pathConfigurationService = null!;
        private readonly IServiceProvider? _serviceProvider;

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
        private ObservableCollection<TableInfoViewModel> _allTables = new(); // Liste complète des tables
        
        public ObservableCollection<TableInfoViewModel> Tables
        {
            get => _tables;
            set { _tables = value; OnPropertyChanged(nameof(Tables)); }
        }

        private string _tableSearchText = string.Empty;
        public string TableSearchText
        {
            get => _tableSearchText;
            set 
            { 
                _tableSearchText = value; 
                OnPropertyChanged(nameof(TableSearchText));
                FilterTables(); // Filtrer automatiquement
            }
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

        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand SettingsCommand { get; private set; } = null!;
        public ICommand RefreshTablesCommand { get; private set; } = null!;
        public ICommand ViewTableDataCommand { get; private set; } = null!;
        public ICommand ViewTableStructureCommand { get; private set; } = null!;
        public ICommand BackupDatabaseCommand { get; private set; } = null!;
        public ICommand RestoreDatabaseCommand { get; private set; } = null!;
        public ICommand OptimizeDatabaseCommand { get; private set; } = null!;
        public ICommand ReindexDatabaseCommand { get; private set; } = null!;
        public ICommand CheckIntegrityCommand { get; private set; } = null!;
        public ICommand ApplyMigrationsCommand { get; private set; } = null!;
        public ICommand InitializeDatabaseCommand { get; private set; } = null!;
        public ICommand ExecuteSqlCommand { get; private set; } = null!;
        public ICommand ClearSqlCommand { get; private set; } = null!;

        #endregion

        // Constructeur par défaut pour test
        public BaseDonneesViewModel() 
        {
            // Créer manuellement le service pour test
            try
            {
                // Utiliser le service centralisé des chemins
                _pathConfigurationService = App.GetService<IPathConfigurationService>();
                var connectionString = $"Data Source={_pathConfigurationService.DatabasePath}";
                var options = new DbContextOptionsBuilder<FNEV4DbContext>()
                    .UseSqlite(connectionString)
                    .Options;
                
                var context = new FNEV4DbContext(options);
                _databaseService = new DatabaseService(context);
                
                // Initialiser le chemin de base de données depuis le service centralisé
                DatabasePath = _pathConfigurationService.DatabasePath;
            }
            catch
            {
                _databaseService = null!;
            }

            SqlResults = "🔧 Interface de maintenance - Prêt pour les opérations...";
            InitializeCommands();
        }

        public BaseDonneesViewModel(IDatabaseService databaseService, IDatabaseConfigurationNotificationService? notificationService = null, IPathConfigurationService pathConfigurationService = null, IServiceProvider? serviceProvider = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _notificationService = notificationService;
            _pathConfigurationService = pathConfigurationService ?? App.GetService<IPathConfigurationService>();
            _serviceProvider = serviceProvider ?? App.ServiceProvider;
            
            // Initialiser le chemin de base de données depuis le service centralisé
            DatabasePath = _pathConfigurationService.DatabasePath;
            
            // S'abonner aux notifications de changement de configuration
            if (_notificationService != null)
            {
                _notificationService.DatabaseConfigurationChanged += OnConfigurationChanged;
            }
            
            SqlResults = "🔧 Interface de maintenance - Prêt pour les opérations...";
            // Initialiser la Console SQL avec un message d'accueil
            ClearSqlQuery();
            InitializeCommands();
        }

        private async Task OnConfigurationChanged()
        {
            try
            {
                // Rafraîchir les données quand la configuration change
                await RefreshDatabaseInfoAsync();
                
                // Ajouter un message de notification dans la console
                SqlResults = $"✅ Configuration mise à jour automatiquement - {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                SqlResults = $"❌ Erreur lors de la mise à jour automatique: {ex.Message}";
            }
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

            // Chargement initial des données
            _ = Task.Run(async () => await LoadDataAsync());
        }

        #region Méthodes privées

        /// <summary>
        /// Affiche un résultat d'opération dans un popup avec icône appropriée
        /// </summary>
        private void ShowOperationResult(string title, string message, bool isSuccess)
        {
            var icon = isSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning;
            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            
            // Mettre aussi à jour la zone de résultats pour traçabilité
            SqlResults = $"[{DateTime.Now:HH:mm:ss}] {message}";
        }

        private async Task LoadDataAsync()
        {
            await RefreshDatabaseInfoAsync();
            await RefreshTablesAsync();
        }

        /// <summary>
        /// Méthode publique pour rafraîchir les informations de la base de données
        /// Utilisée pour synchroniser après des modifications de configuration
        /// </summary>
        public async Task RefreshDatabaseDataAsync()
        {
            await RefreshDatabaseInfoAsync();
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

                // Message temporaire qui s'effacera automatiquement
                SqlResults = $"✓ Actualisation terminée - {TableCount} tables trouvées";
                
                // Effacer le message après 3 secondes
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (SqlResults?.Contains("Actualisation terminée") == true)
                        {
                            SqlResults = "Prêt pour les opérations de maintenance...";
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                SqlResults = $"Erreur lors de l'actualisation: {ex.Message}";
                ConnectionStatus = "Erreur";
                ConnectionStatusColor = Brushes.Red;
            }
        }

        private async void OpenDatabaseSettings()
        {
            try
            {
                var settingsDialog = new Views.Maintenance.DatabaseSettingsDialog();
                
                // Utiliser l'injection de dépendances au lieu de créer directement l'instance
                var settingsViewModel = _serviceProvider?.GetService<DatabaseSettingsViewModel>() 
                                     ?? new DatabaseSettingsViewModel(_databaseService);
                
                settingsDialog.DataContext = settingsViewModel;
                settingsViewModel.SetDialogWindow(settingsDialog);
                
                var result = settingsDialog.ShowDialog(System.Windows.Application.Current.MainWindow);
                
                if (result == true)
                {
                    // Les paramètres ont été appliqués, actualiser complètement l'affichage
                    await RefreshDatabaseInfoAsync();
                    await RefreshTablesAsync();
                    SqlResults = "✓ Paramètres de base de données mis à jour avec succès. Interface actualisée.";
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
                _allTables.Clear();
                
                foreach (var table in tablesInfo)
                {
                    _allTables.Add(new TableInfoViewModel
                    {
                        Name = table.Name,
                        RowCount = table.RowCount,
                        Size = table.Size,
                        LastModified = table.LastModified
                    });
                }

                FilterTables(); // Appliquer le filtre actuel
                SqlResults = $"Liste des tables actualisée. {_allTables.Count} tables trouvées.";
                TableCount = _allTables.Count;
            }
            catch (Exception ex)
            {
                SqlResults = $"Erreur lors de l'actualisation des tables: {ex.Message}";
            }
        }

        private void FilterTables()
        {
            Tables.Clear();
            
            var filteredTables = string.IsNullOrWhiteSpace(TableSearchText) 
                ? _allTables
                : _allTables.Where(t => t.Name.ToLowerInvariant().Contains(TableSearchText.ToLowerInvariant()));

            foreach (var table in filteredTables)
            {
                Tables.Add(table);
            }
        }

        private void ViewTableData(TableInfoViewModel? table)
        {
            if (table == null) return;

            try
            {
                var dialog = new Views.Maintenance.TableDataDialog();
                var viewModel = new TableDataViewModel(_databaseService);
                
                // Charger les données de la table
                viewModel.LoadTableData(table.Name);
                
                dialog.DataContext = viewModel;
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                
                dialog.ShowDialog();
                
                SqlResults = $"Fenêtre de données ouverte pour la table '{table.Name}'.";
            }
            catch (Exception ex)
            {
                SqlResults = $"Erreur lors de l'ouverture des données de la table '{table.Name}': {ex.Message}";
            }
        }

        private async void ViewTableStructure(TableInfoViewModel? table)
        {
            if (table == null) return;

            try
            {
                var dialog = new Views.Maintenance.TableStructureDialog();
                var viewModel = new TableStructureViewModel(_databaseService);
                
                dialog.DataContext = viewModel;
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                
                // Charger la vraie structure depuis la base
                await viewModel.LoadTableStructureAsync(table.Name);
                
                dialog.ShowDialog();
                
                SqlResults = $"Fenêtre de structure ouverte pour la table '{table.Name}'.";
            }
            catch (Exception ex)
            {
                SqlResults = $"Erreur lors de l'ouverture de la structure de la table '{table.Name}': {ex.Message}";
            }
        }

        private async Task BackupDatabaseAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Fichiers de base de données SQLite (*.db)|*.db|Tous les fichiers (*.*)|*.*",
                    DefaultExt = "db",
                    FileName = $"FNEV4_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
                    Title = "Sauvegarder la base de données"
                };

                if (dialog.ShowDialog() == true)
                {
                    SqlResults = "Sauvegarde en cours...";
                    
                    System.Diagnostics.Debug.WriteLine($"Début de sauvegarde vers : {dialog.FileName}");
                    
                    var success = await _databaseService.BackupDatabaseAsync(dialog.FileName);
                    
                    System.Diagnostics.Debug.WriteLine($"Résultat de sauvegarde : {success}");
                    
                    if (success)
                    {
                        // Vérifier que le fichier existe vraiment
                        if (File.Exists(dialog.FileName))
                        {
                            var fileInfo = new FileInfo(dialog.FileName);
                            var sizeKB = Math.Round(fileInfo.Length / 1024.0, 2);
                            LastBackupDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                            
                            // Actualiser les informations de la base
                            await RefreshDatabaseInfoAsync();
                            
                            ShowOperationResult(
                                "Sauvegarde réussie", 
                                $"✅ Sauvegarde créée avec succès !\n\nFichier : {dialog.FileName}\nTaille : {sizeKB} KB", 
                                true);
                        }
                        else
                        {
                            ShowOperationResult(
                                "Erreur de sauvegarde", 
                                $"❌ Le fichier de sauvegarde n'a pas été créé.\nChemin : {dialog.FileName}", 
                                false);
                        }
                    }
                    else
                    {
                        ShowOperationResult(
                            "Erreur de sauvegarde", 
                            "❌ Erreur lors de la création de la sauvegarde.", 
                            false);
                    }
                }
                else
                {
                    SqlResults = "Sauvegarde annulée par l'utilisateur.";
                }
            }
            catch (Exception ex)
            {
                ShowOperationResult(
                    "Erreur de sauvegarde", 
                    $"❌ Erreur lors de la sauvegarde: {ex.Message}", 
                    false);
            }
        }

        private async Task RestoreDatabaseAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Fichiers de base de données SQLite (*.db)|*.db|Tous les fichiers (*.*)|*.*",
                    Title = "Sélectionner une sauvegarde à restaurer",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"⚠️ ATTENTION ⚠️\n\n" +
                        $"Cette opération va remplacer complètement la base de données actuelle par :\n" +
                        $"{dialog.FileName}\n\n" +
                        $"Toutes les données non sauvegardées seront PERDUES.\n\n" +
                        $"Voulez-vous continuer ?",
                        "Confirmation de restauration",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);

                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        SqlResults = "Restauration en cours...";
                        
                        var success = await _databaseService.RestoreDatabaseAsync(dialog.FileName);
                        
                        if (success)
                        {
                            SqlResults = $"✅ Base de données restaurée avec succès depuis :\n{dialog.FileName}";
                            
                            // Actualiser toutes les informations après restauration
                            await RefreshDatabaseInfoAsync();
                            await RefreshTablesAsync();
                        }
                        else
                        {
                            SqlResults = "❌ Erreur lors de la restauration de la base de données.";
                        }
                    }
                    else
                    {
                        SqlResults = "Restauration annulée par l'utilisateur.";
                    }
                }
                else
                {
                    SqlResults = "Aucun fichier de sauvegarde sélectionné.";
                }
            }
            catch (Exception ex)
            {
                SqlResults = $"❌ Erreur lors de la restauration: {ex.Message}";
            }
        }

        private async Task OptimizeDatabaseAsync()
        {
            try
            {
                SqlResults = "Optimisation en cours (VACUUM + ANALYZE)...";
                
                var success = await _databaseService.OptimizeDatabaseAsync();
                
                if (success)
                {
                    await RefreshDatabaseInfoAsync();
                    ShowOperationResult(
                        "Optimisation réussie", 
                        "✅ Optimisation terminée avec succès.\n✅ Espace disque récupéré et statistiques mises à jour.", 
                        true);
                }
                else
                {
                    ShowOperationResult(
                        "Erreur d'optimisation", 
                        "❌ Erreur lors de l'optimisation de la base de données.", 
                        false);
                }
            }
            catch (Exception ex)
            {
                ShowOperationResult(
                    "Erreur d'optimisation", 
                    $"❌ Erreur lors de l'optimisation: {ex.Message}", 
                    false);
            }
        }

        private async Task ReindexDatabaseAsync()
        {
            try
            {
                SqlResults = "Réindexation en cours (REINDEX sur toutes les tables)...";
                
                var success = await _databaseService.ReindexDatabaseAsync();
                
                if (success)
                {
                    await RefreshTablesAsync();
                    ShowOperationResult(
                        "Réindexation réussie", 
                        "✅ Réindexation terminée avec succès.\n✅ Tous les index ont été reconstruits.", 
                        true);
                }
                else
                {
                    ShowOperationResult(
                        "Erreur de réindexation", 
                        "❌ Erreur lors de la réindexation de la base de données.", 
                        false);
                }
            }
            catch (Exception ex)
            {
                ShowOperationResult(
                    "Erreur de réindexation", 
                    $"❌ Erreur lors de la réindexation: {ex.Message}", 
                    false);
            }
        }

        private async Task CheckDatabaseIntegrityAsync()
        {
            try
            {
                SqlResults = "Analyse d'intégrité en cours (PRAGMA integrity_check)...";
                
                var isIntegrityOk = await _databaseService.CheckIntegrityAsync();
                
                if (isIntegrityOk)
                {
                    ShowOperationResult(
                        "Analyse d'intégrité réussie", 
                        "✅ Analyse terminée : Base de données intègre.\n✅ Aucune corruption détectée.", 
                        true);
                }
                else
                {
                    ShowOperationResult(
                        "Problèmes d'intégrité détectés", 
                        "⚠️ Analyse terminée : Problèmes d'intégrité détectés.\n❌ Vérifiez les logs ou contactez l'administrateur.", 
                        false);
                }
            }
            catch (Exception ex)
            {
                ShowOperationResult(
                    "Erreur d'analyse", 
                    $"❌ Erreur lors de l'analyse: {ex.Message}", 
                    false);
            }
        }

        private async Task ApplyMigrationsAsync()
        {
            try
            {
                SqlResults = "Application des migrations en cours...";
                
                var success = await _databaseService.ApplyMigrationsAsync();
                
                if (success)
                {
                    await RefreshDatabaseInfoAsync();
                    await RefreshTablesAsync();
                    ShowOperationResult(
                        "Migrations appliquées", 
                        "✅ Migrations appliquées avec succès.\n✅ Structure de la base de données mise à jour.", 
                        true);
                }
                else
                {
                    ShowOperationResult(
                        "Aucune migration", 
                        "❌ Aucune migration en attente ou échec de l'opération.", 
                        false);
                }
            }
            catch (Exception ex)
            {
                ShowOperationResult(
                    "Erreur de migration", 
                    $"❌ Erreur lors de l'application des migrations: {ex.Message}", 
                    false);
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                // Demander confirmation avant la réinitialisation
                var result = MessageBox.Show(
                    "⚠️ ATTENTION : Cette opération va supprimer TOUTES les données existantes !\n\n" +
                    "La base de données sera complètement réinitialisée.\n" +
                    "Cette action est IRRÉVERSIBLE.\n\n" +
                    "Voulez-vous vraiment continuer ?",
                    "Confirmation de réinitialisation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (result != MessageBoxResult.Yes)
                {
                    SqlResults = "❌ Réinitialisation annulée par l'utilisateur.";
                    return;
                }

                SqlResults = "⚠️ RÉINITIALISATION EN COURS...\n" +
                           "🔥 Suppression de toutes les données...";
                
                var success = await _databaseService.InitializeDatabaseAsync();
                
                if (success)
                {
                    await RefreshDatabaseInfoAsync();
                    await RefreshTablesAsync();
                    ShowOperationResult(
                        "Réinitialisation réussie", 
                        "✅ Réinitialisation terminée avec succès.\n✅ Base de données vide créée.\n✅ Structure initiale restaurée.", 
                        true);
                }
                else
                {
                    ShowOperationResult(
                        "Erreur de réinitialisation", 
                        "❌ Erreur lors de la réinitialisation.\n❌ La base de données pourrait être corrompue.", 
                        false);
                }
            }
            catch (Exception ex)
            {
                ShowOperationResult(
                    "Erreur critique", 
                    $"❌ Erreur critique lors de la réinitialisation: {ex.Message}", 
                    false);
            }
        }

        private async Task ExecuteSqlQueryAsync()
        {
            if (string.IsNullOrWhiteSpace(SqlQuery))
            {
                SqlResults = "⚠️ Veuillez saisir une requête SQL.";
                return;
            }

            try
            {
                SqlResults = $"⏳ Exécution en cours...\nRequête : {SqlQuery.Trim()}\n" + new string('─', 50);
                
                var startTime = DateTime.Now;
                var result = await _databaseService.ExecuteQueryAsync(SqlQuery);
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;
                
                // Formatage professionnel des résultats
                SqlResults = $"✅ Exécution réussie ({duration:F2} ms)\n" +
                           $"📝 Requête : {SqlQuery.Trim()}\n" +
                           $"📅 Exécutée le : {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                           new string('─', 60) + "\n" +
                           "📊 RÉSULTATS :\n" +
                           new string('─', 60) + "\n" +
                           result;
            }
            catch (Exception ex)
            {
                SqlResults = $"❌ ERREUR SQL\n" +
                           $"📝 Requête : {SqlQuery.Trim()}\n" +
                           $"📅 Tentative le : {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                           new string('─', 60) + "\n" +
                           $"🚫 Message d'erreur :\n{ex.Message}";
            }
        }

        private void ClearSqlQuery()
        {
            SqlQuery = string.Empty;
            SqlResults = "🔧 Console SQL - Prêt pour vos requêtes...\n\n" +
                        "💡 Exemples de requêtes :\n" +
                        "  • SELECT * FROM sqlite_master WHERE type='table';\n" +
                        "  • SELECT name FROM sqlite_master WHERE type='table';\n" +
                        "  • PRAGMA table_info(nom_table);\n" +
                        "  • SELECT COUNT(*) FROM nom_table;";
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
