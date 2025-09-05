using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using FNEV4.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using FNEV4.Infrastructure.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Linq;

namespace FNEV4.Presentation.ViewModels.Maintenance
{
    /// <summary>
    /// ViewModel pour la fenêtre de paramètres de la base de données
    /// Gère la configuration des connexions, sauvegardes, alertes et affichage
    /// </summary>
    public class DatabaseSettingsViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;

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
            set 
            { 
                if (SetProperty(ref _dateFormat, value))
                {
                    RefreshPreview();
                }
            }
        }

        private string _sizeUnit = "Auto";
        public string SizeUnit
        {
            get => _sizeUnit;
            set 
            { 
                if (SetProperty(ref _sizeUnit, value))
                {
                    RefreshPreview();
                }
            }
        }

        private bool _showMilliseconds = false;
        public bool ShowMilliseconds
        {
            get => _showMilliseconds;
            set 
            { 
                if (SetProperty(ref _showMilliseconds, value))
                {
                    RefreshPreview();
                }
            }
        }

        private bool _autoRefreshEnabled = false;
        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set => SetProperty(ref _autoRefreshEnabled, value);
        }

        private string _currentDatePreview = string.Empty;
        public string CurrentDatePreview
        {
            get => _currentDatePreview;
            set => SetProperty(ref _currentDatePreview, value);
        }

        private string _sizePreview = string.Empty;
        public string SizePreview
        {
            get => _sizePreview;
            set => SetProperty(ref _sizePreview, value);
        }

        #endregion

        #region Commandes

        public ICommand TestConnectionCommand { get; }
        public ICommand ResetSettingsCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ApplySettingsCommand { get; }
        public ICommand BrowseDatabasePathCommand { get; }
        public ICommand ResetDatabasePathCommand { get; }
        public ICommand BrowseBackupDirectoryCommand { get; }
        public ICommand TestBackupCommand { get; }
        public ICommand TestAlertsCommand { get; }
        public ICommand CheckAlertsNowCommand { get; }
        public ICommand RefreshPreviewCommand { get; }
        public ICommand TestAutoRefreshCommand { get; }

        #endregion

        #region Propriétés de fenêtre

        private Window? _dialogWindow;
        public bool? DialogResult { get; private set; }

        #endregion

        public DatabaseSettingsViewModel() : this(CreateDefaultDatabaseService())
        {
        }

        public DatabaseSettingsViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            // Initialisation des commandes
            TestConnectionCommand = new SimpleCommand(TestConnection);
            ResetSettingsCommand = new SimpleCommand(ResetSettings);
            CancelCommand = new SimpleCommand(Cancel);
            ApplySettingsCommand = new SimpleCommand(ApplySettings);
            BrowseDatabasePathCommand = new SimpleCommand(BrowseDatabasePath);
            ResetDatabasePathCommand = new SimpleCommand(ResetDatabasePath);
            BrowseBackupDirectoryCommand = new SimpleCommand(BrowseBackupDirectory);
            TestBackupCommand = new SimpleCommand(TestBackup);
            TestAlertsCommand = new SimpleCommand(TestAlerts);
            CheckAlertsNowCommand = new SimpleCommand(CheckAlertsNow);
            RefreshPreviewCommand = new SimpleCommand(RefreshPreview);
            TestAutoRefreshCommand = new SimpleCommand(TestAutoRefresh);

            // Charger les paramètres depuis la configuration
            LoadSettingsFromConfig();
            
            // Initialiser l'aperçu
            RefreshPreview();
        }

        private static IDatabaseService CreateDefaultDatabaseService()
        {
            try
            {
                var connectionString = "Data Source=Data/FNEV4.db";
                var options = new DbContextOptionsBuilder<FNEV4DbContext>()
                    .UseSqlite(connectionString)
                    .Options;
                
                var context = new FNEV4DbContext(options);
                return new DatabaseService(context);
            }
            catch
            {
                return null!;
            }
        }

        #region Méthodes publiques

        public void SetDialogWindow(Window window)
        {
            _dialogWindow = window;
        }

        #endregion

        #region Méthodes privées

        private void BrowseDatabasePath()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Sélectionner une base de données SQLite",
                    Filter = "Fichiers de base de données SQLite (*.db)|*.db|Tous les fichiers (*.*)|*.*",
                    DefaultExt = ".db",
                    CheckFileExists = false, // Permet de créer un nouveau fichier
                    InitialDirectory = Path.GetDirectoryName(DatabasePath) ?? Path.GetFullPath("Data"),
                    FileName = Path.GetFileName(DatabasePath) ?? "FNEV4.db"
                };

                // Si l'utilisateur a sélectionné un fichier
                if (openFileDialog.ShowDialog() == true)
                {
                    var selectedPath = openFileDialog.FileName;
                    
                    // Vérifier que l'extension est .db
                    if (!selectedPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                    {
                        selectedPath += ".db";
                    }
                    
                    DatabasePath = selectedPath;
                    
                    // Créer le répertoire s'il n'existe pas
                    var directory = Path.GetDirectoryName(DatabasePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // Informer l'utilisateur si le fichier n'existe pas encore
                    if (!File.Exists(DatabasePath))
                    {
                        MessageBox.Show(
                            $"Le fichier de base de données sera créé à :\n\n{DatabasePath}\n\n" +
                            "Cliquez sur 'Tester la connexion' pour créer et valider la base de données.",
                            "Nouveau fichier de base de données",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la sélection du fichier :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void BrowseBackupDirectory()
        {
            try
            {
                var result = MessageBox.Show(
                    $"Répertoire de sauvegarde actuel :\n{BackupDirectory}\n\n" +
                    "Voulez-vous utiliser un répertoire par défaut recommandé ?\n\n" +
                    "• Oui : Utiliser le répertoire Documents/FNEV4_Backups\n" +
                    "• Non : Conserver le répertoire actuel\n" +
                    "• Annuler : Modifier manuellement dans le champ texte",
                    "Répertoire de sauvegarde",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    // Utiliser le répertoire par défaut
                    var defaultBackupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FNEV4_Backups");
                    BackupDirectory = defaultBackupDir;
                    
                    // Créer le répertoire s'il n'existe pas
                    if (!Directory.Exists(BackupDirectory))
                    {
                        Directory.CreateDirectory(BackupDirectory);
                    }
                    
                    MessageBox.Show(
                        $"✅ Répertoire de sauvegarde mis à jour :\n\n{BackupDirectory}\n\n" +
                        "Le répertoire a été créé s'il n'existait pas.",
                        "Répertoire mis à jour",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    MessageBox.Show(
                        "💡 Conseil : Vous pouvez modifier le chemin directement dans le champ texte ci-dessus.\n\n" +
                        "Exemple : C:\\Sauvegardes\\FNEV4",
                        "Modification manuelle",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la modification du répertoire :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async void TestBackup()
        {
            try
            {
                // Valider les paramètres de sauvegarde
                if (string.IsNullOrWhiteSpace(BackupDirectory))
                {
                    MessageBox.Show(
                        "❌ Le répertoire de sauvegarde ne peut pas être vide.",
                        "Test de sauvegarde",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // Créer le répertoire s'il n'existe pas
                if (!Directory.Exists(BackupDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(BackupDirectory);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"❌ Impossible de créer le répertoire de sauvegarde :\n\n{ex.Message}",
                            "Erreur de création du répertoire",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        return;
                    }
                }

                // Effectuer une sauvegarde de test
                var startTime = DateTime.Now;
                var success = await _databaseService.CreateAutomaticBackupAsync(BackupDirectory, CompressBackups);
                var duration = (DateTime.Now - startTime).TotalMilliseconds;

                if (success)
                {
                    // Compter les fichiers de sauvegarde dans le répertoire
                    var backupFiles = Directory.GetFiles(BackupDirectory, "FNEV4_AutoBackup_*.db");
                    var totalSize = backupFiles.Sum(f => new FileInfo(f).Length);
                    var sizeKB = Math.Round(totalSize / 1024.0, 2);

                    MessageBox.Show(
                        "✅ Test de sauvegarde réussi !\n\n" +
                        $"📁 Répertoire : {BackupDirectory}\n" +
                        $"⏱️ Durée : {duration:F2} ms\n" +
                        $"📊 Nombre de sauvegardes : {backupFiles.Length}\n" +
                        $"💾 Taille totale : {sizeKB} KB\n" +
                        $"🔧 Compression : {(CompressBackups ? "Activée (" + CompressionLevel + ")" : "Désactivée")}\n" +
                        $"🔄 Fréquence configurée : {BackupFrequency}\n" +
                        $"⏰ Heure configurée : {BackupTime:hh\\:mm}\n" +
                        $"📅 Rétention : {BackupRetentionDays} jours",
                        "Test de sauvegarde réussi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        $"❌ Échec du test de sauvegarde.\n\n" +
                        $"📁 Répertoire testé : {BackupDirectory}\n\n" +
                        "Vérifiez que le répertoire est accessible en écriture.",
                        "Échec du test de sauvegarde",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Erreur lors du test de sauvegarde :\n\n{ex.Message}",
                    "Erreur de test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async void TestAlerts()
        {
            try
            {
                // Valider la configuration d'alertes
                if (EmailAlertsEnabled && string.IsNullOrWhiteSpace(AlertEmailAddress))
                {
                    MessageBox.Show(
                        "❌ Veuillez spécifier une adresse email pour les alertes.",
                        "Test des alertes",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // Obtenir les informations de surveillance actuelles
                var monitoringInfo = await _databaseService.GetDatabaseMonitoringInfoAsync();
                
                // Afficher les informations de surveillance
                var message = "📊 ÉTAT ACTUEL DE LA BASE DE DONNÉES\n\n" +
                             $"💾 Taille actuelle : {monitoringInfo.SizeMB:F2} MB\n" +
                             $"🗃️ Nombre de tables : {monitoringInfo.TableCount}\n" +
                             $"⏰ Dernière vérification : {monitoringInfo.LastCheck:dd/MM/yyyy HH:mm:ss}\n\n" +
                             "⚙️ CONFIGURATION DES ALERTES\n\n" +
                             $"🚨 Seuil taille max : {MaxDatabaseSizeMB} MB\n" +
                             $"📊 Seuil tables max : {MaxTableCount} tables\n" +
                             $"📧 Alertes email : {(EmailAlertsEnabled ? "✅ Activées" : "❌ Désactivées")}\n";

                if (EmailAlertsEnabled)
                {
                    message += $"📬 Adresse email : {AlertEmailAddress}\n";
                }

                message += "\n🔍 ÉVALUATION DES SEUILS\n\n";

                // Vérifier les seuils
                if (monitoringInfo.SizeMB > MaxDatabaseSizeMB)
                {
                    message += $"🚨 ALERTE : Taille dépassée ({monitoringInfo.SizeMB:F2} > {MaxDatabaseSizeMB} MB)\n";
                }
                else
                {
                    message += $"✅ Taille OK ({monitoringInfo.SizeMB:F2} < {MaxDatabaseSizeMB} MB)\n";
                }

                if (monitoringInfo.TableCount > MaxTableCount)
                {
                    message += $"🚨 ALERTE : Nombre de tables dépassé ({monitoringInfo.TableCount} > {MaxTableCount})\n";
                }
                else
                {
                    message += $"✅ Nombre de tables OK ({monitoringInfo.TableCount} < {MaxTableCount})\n";
                }

                MessageBox.Show(
                    message,
                    "Test des alertes - État de la surveillance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Proposer de tester l'envoi d'email si activé
                if (EmailAlertsEnabled && !string.IsNullOrWhiteSpace(AlertEmailAddress))
                {
                    var result = MessageBox.Show(
                        "Voulez-vous envoyer un email de test pour vérifier la configuration ?",
                        "Test d'envoi d'email",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        var success = await _databaseService.SendEmailAlertAsync(
                            AlertEmailAddress,
                            "FNEV4 - Test des alertes email",
                            $"Test d'envoi d'email réussi depuis FNEV4.\n\n" +
                            $"Configuration testée le : {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                            $"Adresse de destination : {AlertEmailAddress}\n\n" +
                            $"Si vous recevez ce message, la configuration des alertes email fonctionne correctement."
                        );

                        if (success)
                        {
                            MessageBox.Show(
                                "✅ Email de test envoyé avec succès !\n\n" +
                                "Vérifiez votre boîte de réception (et dossier spam si nécessaire).",
                                "Email de test envoyé",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );
                        }
                        else
                        {
                            MessageBox.Show(
                                "❌ Erreur lors de l'envoi de l'email de test.\n\n" +
                                "Vérifiez la configuration de votre serveur email.",
                                "Erreur d'envoi",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Erreur lors du test des alertes :\n\n{ex.Message}",
                    "Erreur de test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async void CheckAlertsNow()
        {
            try
            {
                var startTime = DateTime.Now;
                var alertsTriggered = await _databaseService.CheckDatabaseAlertsAsync(
                    MaxDatabaseSizeMB, 
                    MaxTableCount, 
                    EmailAlertsEnabled, 
                    AlertEmailAddress
                );
                var duration = (DateTime.Now - startTime).TotalMilliseconds;

                var monitoringInfo = await _databaseService.GetDatabaseMonitoringInfoAsync();
                
                if (alertsTriggered)
                {
                    var alertMessage = "🚨 ALERTES DÉTECTÉES !\n\n" +
                                      $"⏱️ Vérification effectuée en {duration:F2} ms\n" +
                                      $"📊 État actuel :\n" +
                                      $"  • Taille : {monitoringInfo.SizeMB:F2} MB (max: {MaxDatabaseSizeMB} MB)\n" +
                                      $"  • Tables : {monitoringInfo.TableCount} (max: {MaxTableCount})\n\n";

                    if (monitoringInfo.AlertMessages.Any())
                    {
                        alertMessage += "📋 Messages d'alerte :\n";
                        foreach (var alert in monitoringInfo.AlertMessages)
                        {
                            alertMessage += $"  • {alert}\n";
                        }
                    }

                    if (EmailAlertsEnabled && !string.IsNullOrWhiteSpace(AlertEmailAddress))
                    {
                        alertMessage += $"\n📧 Email d'alerte envoyé à : {AlertEmailAddress}";
                    }

                    MessageBox.Show(
                        alertMessage,
                        "Alertes détectées",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
                else
                {
                    MessageBox.Show(
                        "✅ AUCUNE ALERTE DÉTECTÉE\n\n" +
                        $"⏱️ Vérification effectuée en {duration:F2} ms\n" +
                        $"📊 État actuel :\n" +
                        $"  • Taille : {monitoringInfo.SizeMB:F2} MB (max: {MaxDatabaseSizeMB} MB)\n" +
                        $"  • Tables : {monitoringInfo.TableCount} (max: {MaxTableCount})\n\n" +
                        "🔍 Tous les seuils sont respectés.",
                        "Surveillance OK",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Erreur lors de la vérification des alertes :\n\n{ex.Message}",
                    "Erreur de surveillance",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void RefreshPreview()
        {
            try
            {
                // Aperçu de la date
                var now = DateTime.Now;
                var dateFormat = ShowMilliseconds ? DateFormat + ".fff" : DateFormat;
                CurrentDatePreview = now.ToString(dateFormat);

                // Aperçu de la taille (exemple avec 2.5 MB)
                const long exampleBytes = 2621440; // 2.5 MB
                SizePreview = FormatSizeWithUnit(exampleBytes, SizeUnit);
            }
            catch (Exception ex)
            {
                CurrentDatePreview = $"Erreur format date: {ex.Message}";
                SizePreview = $"Erreur format taille: {ex.Message}";
            }
        }

        private async void TestAutoRefresh()
        {
            try
            {
                if (!AutoRefreshEnabled)
                {
                    MessageBox.Show(
                        "ℹ️ L'actualisation automatique est désactivée.\n\n" +
                        "Activez l'option 'Actualisation automatique toutes les 30 secondes' pour tester cette fonctionnalité.",
                        "Actualisation désactivée",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    return;
                }

                MessageBox.Show(
                    "🔄 Test d'actualisation automatique démarré !\n\n" +
                    "L'aperçu va se mettre à jour toutes les 2 secondes pendant 10 secondes pour la démonstration.\n\n" +
                    "Dans l'application réelle, l'actualisation se ferait toutes les 30 secondes.",
                    "Test d'actualisation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Démonstration d'actualisation rapide
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(2000); // Attendre 2 secondes
                    RefreshPreview();
                }

                MessageBox.Show(
                    "✅ Test d'actualisation automatique terminé !\n\n" +
                    "L'aperçu a été mis à jour 5 fois en 10 secondes.\n\n" +
                    "En mode normal, les données seraient actualisées toutes les 30 secondes.",
                    "Test terminé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Erreur lors du test d'actualisation :\n\n{ex.Message}",
                    "Erreur de test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void ResetDatabasePath()
        {
            try
            {
                var defaultPath = Path.GetFullPath(@"Data\FNEV4.db");
                
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir réinitialiser le chemin de la base de données au chemin par défaut ?\n\n" +
                    $"Chemin par défaut : {defaultPath}",
                    "Réinitialiser le chemin",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                
                if (result == MessageBoxResult.Yes)
                {
                    DatabasePath = defaultPath;
                    
                    MessageBox.Show(
                        "✅ Chemin de la base de données réinitialisé au chemin par défaut.",
                        "Chemin réinitialisé",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la réinitialisation du chemin :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async void TestConnection()
        {
            try
            {
                // Vérifier que le chemin de base de données existe
                if (string.IsNullOrWhiteSpace(DatabasePath))
                {
                    MessageBox.Show(
                        "❌ Veuillez spécifier un chemin de base de données.",
                        "Test de connexion",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                if (!File.Exists(DatabasePath))
                {
                    MessageBox.Show(
                        $"❌ Le fichier de base de données n'existe pas :\n{DatabasePath}",
                        "Test de connexion",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // Test de connexion réel
                var startTime = DateTime.Now;
                
                // Tester la connexion en exécutant une requête simple
                var result = await _databaseService.ExecuteQueryAsync("SELECT sqlite_version();");
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;

                // Obtenir des informations sur la base de données
                var dbInfo = await _databaseService.GetDatabaseInfoAsync();
                var fileInfo = new FileInfo(DatabasePath);
                var sizeKB = Math.Round(fileInfo.Length / 1024.0, 2);

                MessageBox.Show(
                    "✅ Connexion testée avec succès !\n\n" +
                    $"📁 Base de données : {Path.GetFileName(DatabasePath)}\n" +
                    $"📍 Chemin : {DatabasePath}\n" +
                    $"💾 Taille : {sizeKB} KB\n" +
                    $"⏱️ Temps de connexion : {duration:F2} ms\n" +
                    $"📊 Version SQLite : {result?.Replace("sqlite_version()", "").Trim()}\n" +
                    $"🗃️ Nombre de tables : {dbInfo?.TableCount ?? 0}\n" +
                    $"⚙️ Mode WAL : {(EnableWalMode ? "Activé" : "Désactivé")}\n" +
                    $"🔧 Cache : {CacheSize} KB\n" +
                    $"⏰ Timeout : {ConnectionTimeout}s",
                    "Test de connexion réussi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Erreur lors du test de connexion :\n\n{ex.Message}\n\n" +
                    $"📍 Chemin testé : {DatabasePath}",
                    "Erreur de connexion",
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

        private async void ApplySettings()
        {
            try
            {
                // Valider les paramètres avant application
                if (string.IsNullOrWhiteSpace(DatabasePath))
                {
                    MessageBox.Show(
                        "❌ Le chemin de la base de données ne peut pas être vide.",
                        "Validation des paramètres",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                if (ConnectionTimeout <= 0 || ConnectionTimeout > 300)
                {
                    MessageBox.Show(
                        "❌ Le timeout de connexion doit être entre 1 et 300 secondes.",
                        "Validation des paramètres",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                if (CacheSize < 512 || CacheSize > 65536)
                {
                    MessageBox.Show(
                        "❌ La taille du cache doit être entre 512 et 65536 KB.",
                        "Validation des paramètres",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // Appliquer les paramètres réels
                await SaveSettingsToConfig();

                // Mettre à jour la connexion de base de données si le chemin a changé
                var currentDbInfo = await _databaseService.GetDatabaseInfoAsync();
                if (currentDbInfo?.Path != DatabasePath)
                {
                    var updateSuccess = await _databaseService.UpdateConnectionStringAsync(DatabasePath);
                    if (!updateSuccess)
                    {
                        MessageBox.Show(
                            "⚠️ Les paramètres ont été sauvegardés mais la mise à jour de la connexion a échoué.\n" +
                            "Redémarrez l'application pour appliquer le nouveau chemin de base de données.",
                            "Avertissement",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                }

                DialogResult = true;
                _dialogWindow?.Close();

                MessageBox.Show(
                    "✅ Paramètres appliqués avec succès !\n\n" +
                    $"📁 Base de données : {Path.GetFileName(DatabasePath)}\n" +
                    $"⏱️ Timeout : {ConnectionTimeout}s\n" +
                    $"💾 Cache : {CacheSize} KB\n" +
                    $"⚙️ Mode WAL : {(EnableWalMode ? "Activé" : "Désactivé")}\n" +
                    $"🔄 Auto-vacuum : {(EnableAutoVacuum ? "Activé" : "Désactivé")}\n\n" +
                    "ℹ️ Redémarrez l'application pour que tous les changements prennent effet.",
                    "Paramètres appliqués",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Erreur lors de l'application des paramètres :\n\n{ex.Message}",
                    "Erreur d'application",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async void LoadSettingsFromConfig()
        {
            try
            {
                // Tenter de charger depuis le fichier de configuration
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "database-settings.json");
                
                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                    
                    if (config.TryGetProperty("DatabasePath", out var dbPath) && !string.IsNullOrWhiteSpace(dbPath.GetString()))
                        DatabasePath = dbPath.GetString()!;
                    
                    if (config.TryGetProperty("ConnectionTimeout", out var timeout))
                        ConnectionTimeout = timeout.GetInt32();
                    
                    if (config.TryGetProperty("CacheSize", out var cache))
                        CacheSize = cache.GetInt32();
                    
                    if (config.TryGetProperty("EnableWalMode", out var wal))
                        EnableWalMode = wal.GetBoolean();
                    
                    if (config.TryGetProperty("EnableAutoVacuum", out var vacuum))
                        EnableAutoVacuum = vacuum.GetBoolean();
                    
                    if (config.TryGetProperty("PageSize", out var page))
                        PageSize = page.GetInt32();
                    
                    if (config.TryGetProperty("ForceSynchronous", out var sync))
                        ForceSynchronous = sync.GetBoolean();
                    
                    // Charger les autres paramètres...
                    if (config.TryGetProperty("AutoBackupEnabled", out var backup))
                        AutoBackupEnabled = backup.GetBoolean();
                    
                    if (config.TryGetProperty("BackupDirectory", out var backupDir))
                        BackupDirectory = backupDir.GetString() ?? string.Empty;
                    
                    System.Diagnostics.Debug.WriteLine($"Configuration chargée depuis : {configPath}");
                }
                else
                {
                    // Pas de fichier de configuration, charger depuis la base de données ou valeurs par défaut
                    var dbInfo = await _databaseService.GetDatabaseInfoAsync();
                    
                    if (dbInfo != null)
                    {
                        // Utiliser le vrai chemin de la base de données
                        DatabasePath = dbInfo.Path ?? Path.GetFullPath(@"Data\FNEV4.db");
                        
                        if (File.Exists(DatabasePath))
                        {
                            var fileInfo = new FileInfo(DatabasePath);
                            System.Diagnostics.Debug.WriteLine($"Base de données trouvée : {DatabasePath} ({fileInfo.Length} bytes)");
                        }
                    }
                    else
                    {
                        LoadDefaultSettings();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement de la configuration : {ex.Message}");
                LoadDefaultSettings();
            }
        }

        private void LoadDefaultSettings()
        {
            // Connexion - Utiliser le vrai chemin de la base de données depuis le service
            try
            {
                var dbInfo = _databaseService.GetDatabaseInfoAsync().Result;
                DatabasePath = dbInfo?.Path ?? Path.GetFullPath(@"Data\FNEV4.db");
            }
            catch
            {
                DatabasePath = Path.GetFullPath(@"Data\FNEV4.db");
            }
            
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
            BackupDirectory = Path.GetFullPath(@"Backups");
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

        private async Task SaveSettingsToConfig()
        {
            try
            {
                // Sauvegarder dans un fichier de configuration JSON réel
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "database-settings.json");
                var configDir = Path.GetDirectoryName(configPath);
                
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir!);
                }

                var settings = new
                {
                    DatabasePath,
                    ConnectionTimeout,
                    CacheSize,
                    EnableWalMode,
                    EnableAutoVacuum,
                    PageSize,
                    ForceSynchronous,
                    AutoBackupEnabled,
                    BackupFrequency,
                    BackupTime = BackupTime.ToString(@"hh\:mm"),
                    BackupDirectory,
                    BackupRetentionDays,
                    CompressBackups,
                    CompressionLevel,
                    MaxDatabaseSizeMB,
                    MaxTableCount,
                    EmailAlertsEnabled,
                    AlertEmailAddress,
                    DateFormat,
                    SizeUnit,
                    ShowMilliseconds,
                    AutoRefreshEnabled,
                    LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                await File.WriteAllTextAsync(configPath, json);

                // Créer le répertoire de sauvegarde s'il est spécifié
                if (!string.IsNullOrWhiteSpace(BackupDirectory) && !Directory.Exists(BackupDirectory))
                {
                    Directory.CreateDirectory(BackupDirectory);
                }

                // Log de la configuration appliquée
                System.Diagnostics.Debug.WriteLine($"Configuration sauvegardée dans : {configPath}");
                System.Diagnostics.Debug.WriteLine($"  DatabasePath: {DatabasePath}");
                System.Diagnostics.Debug.WriteLine($"  ConnectionTimeout: {ConnectionTimeout}");
                System.Diagnostics.Debug.WriteLine($"  CacheSize: {CacheSize}");
                System.Diagnostics.Debug.WriteLine($"  EnableWalMode: {EnableWalMode}");
                System.Diagnostics.Debug.WriteLine($"  EnableAutoVacuum: {EnableAutoVacuum}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur lors de la sauvegarde des paramètres: {ex.Message}", ex);
            }
        }

        #endregion

        #region Méthodes utilitaires

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }

        private string FormatSizeWithUnit(long bytes, string unit)
        {
            try
            {
                switch (unit.ToLower())
                {
                    case "bytes":
                        return $"{bytes} B";
                    case "kb":
                        return $"{Math.Round(bytes / 1024.0, 2)} KB";
                    case "mb":
                        return $"{Math.Round(bytes / (1024.0 * 1024.0), 2)} MB";
                    case "auto":
                    default:
                        return FormatFileSize(bytes);
                }
            }
            catch
            {
                return $"{bytes} B";
            }
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
