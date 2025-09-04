using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;

namespace FNEV4.Presentation.ViewModels.Maintenance
{
    /// <summary>
    /// ViewModel pour la fenêtre de paramètres de la base de données
    /// Gère la configuration des connexions, sauvegardes, alertes et affichage
    /// </summary>
    public class DatabaseSettingsViewModel : INotifyPropertyChanged
    {
        #region Propriétés de connexion

        private string _databasePath = string.Empty;
        public string DatabasePath
        {
            get => _databasePath;
            set => SetProperty(ref _databasePath, value);
        }

        private int _connectionTimeout = 30;
        public int ConnectionTimeout
        {
            get => _connectionTimeout;
            set => SetProperty(ref _connectionTimeout, value);
        }

        private int _cacheSize = 2048;
        public int CacheSize
        {
            get => _cacheSize;
            set => SetProperty(ref _cacheSize, value);
        }

        private bool _enableWalMode = true;
        public bool EnableWalMode
        {
            get => _enableWalMode;
            set => SetProperty(ref _enableWalMode, value);
        }

        private bool _enableAutoVacuum = true;
        public bool EnableAutoVacuum
        {
            get => _enableAutoVacuum;
            set => SetProperty(ref _enableAutoVacuum, value);
        }

        private int _pageSize = 4096;
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        private bool _forceSynchronous = false;
        public bool ForceSynchronous
        {
            get => _forceSynchronous;
            set => SetProperty(ref _forceSynchronous, value);
        }

        #endregion

        #region Propriétés de sauvegarde

        private bool _autoBackupEnabled = true;
        public bool AutoBackupEnabled
        {
            get => _autoBackupEnabled;
            set => SetProperty(ref _autoBackupEnabled, value);
        }

        private string _backupFrequency = "Daily";
        public string BackupFrequency
        {
            get => _backupFrequency;
            set => SetProperty(ref _backupFrequency, value);
        }

        private TimeSpan _backupTime = new TimeSpan(2, 0, 0); // 2h du matin
        public TimeSpan BackupTime
        {
            get => _backupTime;
            set => SetProperty(ref _backupTime, value);
        }

        private string _backupDirectory = @"C:\Backups\FNEV4";
        public string BackupDirectory
        {
            get => _backupDirectory;
            set => SetProperty(ref _backupDirectory, value);
        }

        private int _backupRetentionDays = 30;
        public int BackupRetentionDays
        {
            get => _backupRetentionDays;
            set => SetProperty(ref _backupRetentionDays, value);
        }

        private bool _compressBackups = true;
        public bool CompressBackups
        {
            get => _compressBackups;
            set => SetProperty(ref _compressBackups, value);
        }

        private string _compressionLevel = "Normal";
        public string CompressionLevel
        {
            get => _compressionLevel;
            set => SetProperty(ref _compressionLevel, value);
        }

        #endregion

        #region Propriétés d'alertes

        private double _maxDatabaseSizeMB = 1000;
        public double MaxDatabaseSizeMB
        {
            get => _maxDatabaseSizeMB;
            set => SetProperty(ref _maxDatabaseSizeMB, value);
        }

        private int _maxTableCount = 100;
        public int MaxTableCount
        {
            get => _maxTableCount;
            set => SetProperty(ref _maxTableCount, value);
        }

        private bool _emailAlertsEnabled = false;
        public bool EmailAlertsEnabled
        {
            get => _emailAlertsEnabled;
            set => SetProperty(ref _emailAlertsEnabled, value);
        }

        private string _alertEmailAddress = string.Empty;
        public string AlertEmailAddress
        {
            get => _alertEmailAddress;
            set => SetProperty(ref _alertEmailAddress, value);
        }

        #endregion

        #region Propriétés d'affichage

        private string _dateFormat = "dd/MM/yyyy HH:mm";
        public string DateFormat
        {
            get => _dateFormat;
            set => SetProperty(ref _dateFormat, value);
        }

        private string _sizeUnit = "Auto";
        public string SizeUnit
        {
            get => _sizeUnit;
            set => SetProperty(ref _sizeUnit, value);
        }

        private bool _showMilliseconds = false;
        public bool ShowMilliseconds
        {
            get => _showMilliseconds;
            set => SetProperty(ref _showMilliseconds, value);
        }

        private bool _autoRefreshEnabled = false;
        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set => SetProperty(ref _autoRefreshEnabled, value);
        }

        #endregion

        #region Commandes

        public ICommand TestConnectionCommand { get; }
        public ICommand ResetSettingsCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ApplySettingsCommand { get; }

        #endregion

        #region Propriétés de fenêtre

        private Window? _dialogWindow;
        public bool? DialogResult { get; private set; }

        #endregion

        public DatabaseSettingsViewModel()
        {
            // Initialisation des commandes
            TestConnectionCommand = new SimpleCommand(TestConnection);
            ResetSettingsCommand = new SimpleCommand(ResetSettings);
            CancelCommand = new SimpleCommand(Cancel);
            ApplySettingsCommand = new SimpleCommand(ApplySettings);

            // Charger les paramètres depuis la configuration
            LoadSettingsFromConfig();
        }

        #region Méthodes publiques

        public void SetDialogWindow(Window window)
        {
            _dialogWindow = window;
        }

        #endregion

        #region Méthodes privées

        private void TestConnection()
        {
            try
            {
                // TODO: Implémenter le test de connexion
                MessageBox.Show(
                    "Connexion testée avec succès !\n\n" +
                    $"Base de données : {DatabasePath}\n" +
                    $"Timeout : {ConnectionTimeout}s\n" +
                    $"Cache : {CacheSize} KB",
                    "Test de connexion",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors du test de connexion :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void ResetSettings()
        {
            var result = MessageBox.Show(
                "Êtes-vous sûr de vouloir réinitialiser tous les paramètres aux valeurs par défaut ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                LoadDefaultSettings();
            }
        }

        private void Cancel()
        {
            DialogResult = false;
            _dialogWindow?.Close();
        }

        private void ApplySettings()
        {
            try
            {
                // TODO: Sauvegarder les paramètres dans la configuration
                SaveSettingsToConfig();

                DialogResult = true;
                _dialogWindow?.Close();

                MessageBox.Show(
                    "Paramètres appliqués avec succès !",
                    "Confirmation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'application des paramètres :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void LoadSettingsFromConfig()
        {
            // TODO: Charger depuis la configuration réelle
            // Pour l'instant, on utilise les valeurs par défaut
            LoadDefaultSettings();
        }

        private void LoadDefaultSettings()
        {
            // Connexion
            DatabasePath = @"C:\wamp64\www\FNEV4\src\FNEV4.Presentation\Data\FNEV4.db";
            ConnectionTimeout = 30;
            CacheSize = 2048;
            EnableWalMode = true;
            EnableAutoVacuum = true;
            PageSize = 4096;
            ForceSynchronous = false;

            // Sauvegarde
            AutoBackupEnabled = true;
            BackupFrequency = "Daily";
            BackupTime = new TimeSpan(2, 0, 0);
            BackupDirectory = @"C:\Backups\FNEV4";
            BackupRetentionDays = 30;
            CompressBackups = true;
            CompressionLevel = "Normal";

            // Alertes
            MaxDatabaseSizeMB = 1000;
            MaxTableCount = 100;
            EmailAlertsEnabled = false;
            AlertEmailAddress = "";

            // Affichage
            DateFormat = "dd/MM/yyyy HH:mm";
            SizeUnit = "Auto";
            ShowMilliseconds = false;
            AutoRefreshEnabled = false;
        }

        private void SaveSettingsToConfig()
        {
            // TODO: Sauvegarder dans un fichier de configuration ou base de données
            // Pour l'instant, on simule la sauvegarde
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
