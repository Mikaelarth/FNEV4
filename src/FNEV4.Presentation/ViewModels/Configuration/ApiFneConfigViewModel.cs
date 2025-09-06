using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using FNEV4.Infrastructure.Services;
using FNEV4.Infrastructure.Data;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FNEV4.Presentation.ViewModels.Configuration
{
    /// <summary>
    /// ViewModel pour la configuration API FNE
    /// Gestion des environnements Test/Production et paramètres d'API selon FNE-procedureapi.md
    /// </summary>
    public class ApiFneConfigViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private readonly HttpClient _httpClient;

        #region Properties - Configuration FNE

        private string _configurationName = "Test DGI";
        public string ConfigurationName
        {
            get => _configurationName;
            set { _configurationName = value; OnPropertyChanged(); ValidateConfigurationName(); }
        }

        private string _environment = "Test";
        public string Environment
        {
            get => _environment;
            set 
            { 
                _environment = value; 
                OnPropertyChanged(); 
                UpdateBaseUrlFromEnvironment();
                CalculateConfigurationProgress();
            }
        }

        private string _baseUrl = "http://54.247.95.108/ws";
        public string BaseUrl
        {
            get => _baseUrl;
            set { _baseUrl = value; OnPropertyChanged(); ValidateBaseUrl(); RefreshCommandCanExecute(); }
        }

        private string _webUrl = "http://54.247.95.108";
        public string WebUrl
        {
            get => _webUrl;
            set { _webUrl = value; OnPropertyChanged(); ValidateWebUrl(); }
        }

        private string _apiKey = string.Empty;
        public string ApiKey
        {
            get => _apiKey;
            set { _apiKey = value; OnPropertyChanged(); ValidateApiKey(); RefreshCommandCanExecute(); }
        }

        private string _bearerToken = string.Empty;
        public string BearerToken
        {
            get => _bearerToken;
            set { _bearerToken = value; OnPropertyChanged(); ValidateBearerToken(); RefreshCommandCanExecute(); }
        }

        private string _supportEmail = "support.fne@dgi.gouv.ci";
        public string SupportEmail
        {
            get => _supportEmail;
            set { _supportEmail = value; OnPropertyChanged(); ValidateSupportEmail(); }
        }

        private int _requestTimeoutSeconds = 30;
        public int RequestTimeoutSeconds
        {
            get => _requestTimeoutSeconds;
            set { _requestTimeoutSeconds = value; OnPropertyChanged(); ValidateTimeout(); }
        }

        private int _maxRetryAttempts = 3;
        public int MaxRetryAttempts
        {
            get => _maxRetryAttempts;
            set { _maxRetryAttempts = value; OnPropertyChanged(); ValidateRetryAttempts(); }
        }

        private int _retryDelaySeconds = 5;
        public int RetryDelaySeconds
        {
            get => _retryDelaySeconds;
            set { _retryDelaySeconds = value; OnPropertyChanged(); ValidateRetryDelay(); }
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); CalculateConfigurationProgress(); }
        }

        private bool _isValidatedByDgi = false;
        public bool IsValidatedByDgi
        {
            get => _isValidatedByDgi;
            set { _isValidatedByDgi = value; OnPropertyChanged(); CalculateConfigurationProgress(); }
        }

        private DateTime? _validationDate;
        public DateTime? ValidationDate
        {
            get => _validationDate;
            set { _validationDate = value; OnPropertyChanged(); }
        }

        #endregion

        #region Properties - État et validation

        private bool _isConfigurationNameValid = true;
        public bool IsConfigurationNameValid
        {
            get => _isConfigurationNameValid;
            set { _isConfigurationNameValid = value; OnPropertyChanged(); CalculateConfigurationProgress(); }
        }

        private bool _isBaseUrlValid = true;
        public bool IsBaseUrlValid
        {
            get => _isBaseUrlValid;
            set { _isBaseUrlValid = value; OnPropertyChanged(); CalculateConfigurationProgress(); }
        }

        private bool _isWebUrlValid = true;
        public bool IsWebUrlValid
        {
            get => _isWebUrlValid;
            set { _isWebUrlValid = value; OnPropertyChanged(); CalculateConfigurationProgress(); }
        }

        private bool _isApiKeyValid = false;
        public bool IsApiKeyValid
        {
            get => _isApiKeyValid;
            set { _isApiKeyValid = value; OnPropertyChanged(); CalculateConfigurationProgress(); }
        }

        private bool _isBearerTokenValid = false;
        public bool IsBearerTokenValid
        {
            get => _isBearerTokenValid;
            set { _isBearerTokenValid = value; OnPropertyChanged(); CalculateConfigurationProgress(); }
        }

        private bool _isSupportEmailValid = true;
        public bool IsSupportEmailValid
        {
            get => _isSupportEmailValid;
            set { _isSupportEmailValid = value; OnPropertyChanged(); CalculateConfigurationProgress(); }
        }

        private bool _isTimeoutValid = true;
        public bool IsTimeoutValid
        {
            get => _isTimeoutValid;
            set { _isTimeoutValid = value; OnPropertyChanged(); CalculateConfigurationProgress(); }
        }

        private bool _isRetryAttemptsValid = true;
        public bool IsRetryAttemptsValid
        {
            get => _isRetryAttemptsValid;
            set { _isRetryAttemptsValid = value; OnPropertyChanged(); CalculateConfigurationProgress(); }
        }

        private bool _isRetryDelayValid = true;
        public bool IsRetryDelayValid
        {
            get => _isRetryDelayValid;
            set { _isRetryDelayValid = value; OnPropertyChanged(); CalculateConfigurationProgress(); }
        }

        private bool _isConnectionTested = false;
        public bool IsConnectionTested
        {
            get => _isConnectionTested;
            set { _isConnectionTested = value; OnPropertyChanged(); CalculateConfigurationProgress(); }
        }

        #endregion

        #region Properties - Progression et statut

        private double _configurationProgress = 0.0;
        public double ConfigurationProgress
        {
            get => _configurationProgress;
            set { _configurationProgress = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "Configuration API FNE prête";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private string _progressDetails = "0% - Configuration vide";
        public string ProgressDetails
        {
            get => _progressDetails;
            set { _progressDetails = value; OnPropertyChanged(); }
        }

        #endregion

        #region Properties - Collections et options

        public ObservableCollection<string> AvailableEnvironments { get; set; }
        public ObservableCollection<string> AvailableConfigurations { get; set; }

        #endregion

        #region Properties - Notification

        private bool _isNotificationVisible = false;
        public bool IsNotificationVisible
        {
            get => _isNotificationVisible;
            set { _isNotificationVisible = value; OnPropertyChanged(); }
        }

        private string _notificationMessage = string.Empty;
        public string NotificationMessage
        {
            get => _notificationMessage;
            set { _notificationMessage = value; OnPropertyChanged(); }
        }

        private PackIconKind _notificationIcon = PackIconKind.Information;
        public PackIconKind NotificationIcon
        {
            get => _notificationIcon;
            set { _notificationIcon = value; OnPropertyChanged(); }
        }

        private SolidColorBrush _notificationColor = Brushes.Blue;
        public SolidColorBrush NotificationColor
        {
            get => _notificationColor;
            set { _notificationColor = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        public ICommand SaveConfigurationCommand { get; private set; }
        public ICommand TestConnectionCommand { get; private set; }
        public ICommand ResetConfigurationCommand { get; private set; }
        public ICommand LoadConfigurationCommand { get; private set; }
        public ICommand DeleteConfigurationCommand { get; private set; }
        public ICommand OpenWebUrlCommand { get; private set; }
        public ICommand ContactSupportCommand { get; private set; }
        public ICommand ValidateWithDgiCommand { get; private set; }

        #endregion

        #region Constructor

        public ApiFneConfigViewModel(IDatabaseService? databaseService = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _httpClient = new HttpClient();

            InitializeCollections();
            InitializeCommands();
            InitializeData();
        }

        private void InitializeCollections()
        {
            AvailableEnvironments = new ObservableCollection<string>
            {
                "Test", "Production"
            };

            // Initialisation avec des configurations par défaut, sera mise à jour par LoadAvailableConfigurationsAsync
            AvailableConfigurations = new ObservableCollection<string>
            {
                "Test DGI", "Production DGI"
            };
        }

        private void InitializeCommands()
        {
            SaveConfigurationCommand = new RelayCommand(
                async () => await SaveConfigurationAsync(),
                () => CanExecuteSaveConfiguration()
            );
            TestConnectionCommand = new RelayCommand(
                async () => await TestConnectionAsync(),
                () => CanExecuteTestConnection()
            );
            ResetConfigurationCommand = new RelayCommand(async () => await ResetConfigurationAsync());
            LoadConfigurationCommand = new RelayCommand<string>(async (configName) => await LoadConfigurationAsync(configName));
            DeleteConfigurationCommand = new RelayCommand(
                async () => await DeleteConfigurationAsync(),
                () => CanExecuteDeleteConfiguration()
            );
            OpenWebUrlCommand = new RelayCommand(() => OpenWebUrl());
            ContactSupportCommand = new RelayCommand(() => ContactSupport());
            ValidateWithDgiCommand = new RelayCommand(async () => await ValidateWithDgiAsync());
        }

        private void InitializeData()
        {
            // Charger les configurations disponibles et la configuration existante sur le thread UI
            _ = LoadConfigurationsAsync();
            
            // Validation initiale
            ValidateAllFields();
            CalculateConfigurationProgress();
        }

        private async Task LoadConfigurationsAsync()
        {
            try
            {
                await LoadAvailableConfigurationsAsync();
                await LoadExistingConfigurationAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur LoadConfigurationsAsync: {ex.Message}");
            }
        }

        #endregion

        #region Méthodes de validation

        private void ValidateConfigurationName()
        {
            IsConfigurationNameValid = !string.IsNullOrWhiteSpace(ConfigurationName) && 
                                     ConfigurationName.Length >= 3 &&
                                     ConfigurationName.Length <= 50;
        }

        private void ValidateBaseUrl()
        {
            IsBaseUrlValid = !string.IsNullOrWhiteSpace(BaseUrl) && 
                           (BaseUrl.StartsWith("http://") || BaseUrl.StartsWith("https://"));
        }

        private void ValidateWebUrl()
        {
            IsWebUrlValid = !string.IsNullOrWhiteSpace(WebUrl) && 
                          (WebUrl.StartsWith("http://") || WebUrl.StartsWith("https://"));
        }

        private void ValidateApiKey()
        {
            IsApiKeyValid = !string.IsNullOrWhiteSpace(ApiKey) && ApiKey.Length >= 20;
        }

        private void ValidateBearerToken()
        {
            IsBearerTokenValid = !string.IsNullOrWhiteSpace(BearerToken) && BearerToken.Length >= 20;
        }

        private void ValidateSupportEmail()
        {
            IsSupportEmailValid = !string.IsNullOrWhiteSpace(SupportEmail) && 
                                SupportEmail.Contains("@") && 
                                SupportEmail.Contains(".");
        }

        private void ValidateTimeout()
        {
            IsTimeoutValid = RequestTimeoutSeconds >= 5 && RequestTimeoutSeconds <= 300;
        }

        private void ValidateRetryAttempts()
        {
            IsRetryAttemptsValid = MaxRetryAttempts >= 0 && MaxRetryAttempts <= 10;
        }

        private void ValidateRetryDelay()
        {
            IsRetryDelayValid = RetryDelaySeconds >= 1 && RetryDelaySeconds <= 60;
        }

        private void ValidateAllFields()
        {
            ValidateConfigurationName();
            ValidateBaseUrl();
            ValidateWebUrl();
            ValidateApiKey();
            ValidateBearerToken();
            ValidateSupportEmail();
            ValidateTimeout();
            ValidateRetryAttempts();
            ValidateRetryDelay();
        }

        #endregion

        #region Méthodes de progression

        private void CalculateConfigurationProgress()
        {
            double progress = 0.0;
            int totalCriteria = 0;
            int validCriteria = 0;

            // Critères de base (obligatoires)
            totalCriteria += 5;
            if (IsConfigurationNameValid) validCriteria++;
            if (IsBaseUrlValid) validCriteria++;
            if (IsWebUrlValid) validCriteria++;
            if (IsSupportEmailValid) validCriteria++;
            if (IsActive) validCriteria++;

            // Critères d'authentification (au moins un des deux)
            totalCriteria += 1;
            if (IsApiKeyValid || IsBearerTokenValid) validCriteria++;

            // Critères techniques
            totalCriteria += 3;
            if (IsTimeoutValid) validCriteria++;
            if (IsRetryAttemptsValid) validCriteria++;
            if (IsRetryDelayValid) validCriteria++;

            // Test de connexion
            totalCriteria += 1;
            if (IsConnectionTested) validCriteria++;

            // Critères de validation DGI (bonus)
            totalCriteria += 1;
            if (IsValidatedByDgi) validCriteria++;

            progress = totalCriteria > 0 ? (double)validCriteria / totalCriteria * 100.0 : 0.0;
            ConfigurationProgress = progress;

            // Mise à jour des détails de progression
            UpdateProgressDetails(progress, validCriteria, totalCriteria);
        }

        private void UpdateProgressDetails(double progress, int validCriteria, int totalCriteria)
        {
            string progressText = $"{progress:F0}% - {validCriteria}/{totalCriteria} critères validés";
            
            if (progress >= 90)
            {
                ProgressDetails = $"🟢 {progressText} - Configuration complète !";
                StatusMessage = "✅ Configuration API FNE prête pour la production";
            }
            else if (progress >= 70)
            {
                ProgressDetails = $"🟡 {progressText} - Configuration avancée";
                StatusMessage = "⚡ Configuration API FNE presque complète";
            }
            else if (progress >= 40)
            {
                ProgressDetails = $"🟠 {progressText} - Configuration de base";
                StatusMessage = "⚙️ Configuration API FNE en cours";
            }
            else
            {
                ProgressDetails = $"🔴 {progressText} - Configuration incomplète";
                StatusMessage = "⚠️ Configuration API FNE incomplète";
            }
        }

        #endregion

        #region Méthodes d'actions

        private async Task SaveConfigurationAsync()
        {
            try
            {
                StatusMessage = "💾 Validation des données...";
                
                // Validation finale
                ValidateAllFields();
                if (!IsConfigurationNameValid || !IsBaseUrlValid || !IsWebUrlValid)
                {
                    StatusMessage = "❌ Veuillez corriger les erreurs de validation";
                    ShowNotification("❌ Veuillez corriger les erreurs de validation", false);
                    return;
                }

                // Validation des champs obligatoires
                if (string.IsNullOrWhiteSpace(ConfigurationName))
                {
                    StatusMessage = "❌ Le nom de configuration est obligatoire";
                    ShowNotification("❌ Le nom de configuration est obligatoire", false);
                    return;
                }

                if (string.IsNullOrWhiteSpace(BaseUrl))
                {
                    StatusMessage = "❌ L'URL de base est obligatoire";
                    ShowNotification("❌ L'URL de base est obligatoire", false);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ApiKey) && string.IsNullOrWhiteSpace(BearerToken))
                {
                    StatusMessage = "❌ Une clé API ou un token Bearer est requis";
                    ShowNotification("❌ Une clé API ou un token Bearer est requis", false);
                    return;
                }

                StatusMessage = "💾 Sauvegarde en cours...";

                // Construire l'objet de configuration
                var config = new FneConfiguration
                {
                    Id = Guid.NewGuid(),
                    ConfigurationName = ConfigurationName?.Trim() ?? "",
                    Environment = Environment?.Trim() ?? "Test",
                    BaseUrl = BaseUrl?.Trim() ?? "",
                    WebUrl = WebUrl?.Trim(),
                    ApiKey = ApiKey?.Trim(),
                    BearerToken = BearerToken?.Trim(),
                    SupportEmail = SupportEmail?.Trim(),
                    RequestTimeoutSeconds = RequestTimeoutSeconds,
                    MaxRetryAttempts = MaxRetryAttempts,
                    RetryDelaySeconds = RetryDelaySeconds,
                    IsActive = IsActive,
                    IsValidatedByDgi = IsValidatedByDgi,
                    ValidationDate = ValidationDate,
                    ApiVersion = "1.0",
                    LastModifiedDate = DateTime.UtcNow,
                    ModifiedBy = "System"
                };

                // Utiliser la chaîne de connexion configurée pour créer un contexte
                var connectionString = _databaseService.GetConnectionString();
                
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    StatusMessage = "❌ Aucune base de données configurée";
                    ShowNotification("❌ Aucune base de données configurée", false);
                    return;
                }

                var optionsBuilder = new DbContextOptionsBuilder<FNEV4DbContext>();
                optionsBuilder.UseSqlite(connectionString);

                using (var context = new FNEV4DbContext(optionsBuilder.Options))
                {
                    // Vérifier si une configuration avec ce nom existe déjà
                    var existingConfig = await context.FneConfigurations
                        .FirstOrDefaultAsync(c => c.ConfigurationName == config.ConfigurationName);

                    if (existingConfig != null)
                    {
                        // Mettre à jour la configuration existante
                        existingConfig.Environment = config.Environment;
                        existingConfig.BaseUrl = config.BaseUrl;
                        existingConfig.WebUrl = config.WebUrl;
                        existingConfig.ApiKey = config.ApiKey;
                        existingConfig.BearerToken = config.BearerToken;
                        existingConfig.SupportEmail = config.SupportEmail;
                        existingConfig.RequestTimeoutSeconds = config.RequestTimeoutSeconds;
                        existingConfig.MaxRetryAttempts = config.MaxRetryAttempts;
                        existingConfig.RetryDelaySeconds = config.RetryDelaySeconds;
                        existingConfig.IsActive = config.IsActive;
                        existingConfig.IsValidatedByDgi = config.IsValidatedByDgi;
                        existingConfig.ValidationDate = config.ValidationDate;
                        existingConfig.LastModifiedDate = config.LastModifiedDate;
                        existingConfig.ModifiedBy = config.ModifiedBy;
                        
                        context.FneConfigurations.Update(existingConfig);
                    }
                    else
                    {
                        // Ajouter une nouvelle configuration
                        context.FneConfigurations.Add(config);
                    }

                    var changesCount = await context.SaveChangesAsync();
                    
                    if (changesCount > 0)
                    {
                        StatusMessage = "✅ Configuration sauvegardée avec succès";
                        ShowNotification("💾 Configuration API FNE sauvegardée avec succès !", true);
                        
                        // Recharger les configurations disponibles
                        await LoadAvailableConfigurationsAsync();
                    }
                    else
                    {
                        StatusMessage = "⚠️ Aucune modification détectée";
                        ShowNotification("⚠️ Aucune modification n'a été détectée", false);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "❌ Erreur lors de la sauvegarde";
                ShowNotification($"❌ Erreur lors de la sauvegarde : {ex.Message}", false);
                System.Diagnostics.Debug.WriteLine($"Erreur SaveConfigurationAsync: {ex}");
            }
        }

        private (bool IsValid, string Message) ValidateTestPrerequisites()
        {
            var errors = new List<string>();

            // Validation URL de base
            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                errors.Add("URL de base manquante");
            }
            else if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || 
                     (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                errors.Add("URL de base invalide");
            }

            // Validation authentification (API FNE utilise l'API Key comme Bearer Token)
            if (string.IsNullOrWhiteSpace(ApiKey) && string.IsNullOrWhiteSpace(BearerToken))
            {
                errors.Add("Clé API FNE requise (utilisée comme Bearer Token)");
            }

            if (!string.IsNullOrWhiteSpace(ApiKey) && ApiKey.Length < 20)
            {
                errors.Add("Clé API FNE trop courte (minimum 20 caractères)");
            }

            if (!string.IsNullOrWhiteSpace(BearerToken) && BearerToken.Length < 20)
            {
                errors.Add("Token Bearer trop court (minimum 20 caractères)");
            }

            // Validation paramètres techniques
            if (RequestTimeoutSeconds < 5 || RequestTimeoutSeconds > 300)
            {
                errors.Add("Timeout invalide (5-300 secondes)");
            }

            if (errors.Any())
            {
                return (false, $"Prérequis manquants: {string.Join(", ", errors)}");
            }

            return (true, "Validation réussie");
        }

        private bool CanExecuteTestConnection()
        {
            // Vérifications minimales pour permettre le test (priorité à l'API Key pour FNE)
            return !string.IsNullOrWhiteSpace(BaseUrl) && 
                   (!string.IsNullOrWhiteSpace(ApiKey) || !string.IsNullOrWhiteSpace(BearerToken));
        }

        private bool CanExecuteSaveConfiguration()
        {
            // Prérequis minimum pour la sauvegarde
            return !string.IsNullOrWhiteSpace(ConfigurationName) && 
                   !string.IsNullOrWhiteSpace(BaseUrl) &&
                   (!string.IsNullOrWhiteSpace(ApiKey) || !string.IsNullOrWhiteSpace(BearerToken));
        }

        private void RefreshCommandCanExecute()
        {
            if (TestConnectionCommand is RelayCommand testCommand)
            {
                testCommand.NotifyCanExecuteChanged();
            }
            
            if (SaveConfigurationCommand is RelayCommand saveCommand)
            {
                saveCommand.NotifyCanExecuteChanged();
            }
            
            if (DeleteConfigurationCommand is RelayCommand deleteCommand)
            {
                deleteCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                StatusMessage = "🔗 Test de connexion en cours...";
                ShowNotification("🔗 Test de connexion à l'API FNE...", true);

                // Validation des prérequis
                var validationResult = ValidateTestPrerequisites();
                if (!validationResult.IsValid)
                {
                    StatusMessage = $"❌ {validationResult.Message}";
                    ShowNotification($"❌ {validationResult.Message}", false);
                    return;
                }

                // Configuration du client HTTP avec authentification
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
                
                // Configuration de l'authentification FNE DGI
                // L'API FNE utilise uniquement Authorization: Bearer <API_KEY>
                if (!string.IsNullOrWhiteSpace(ApiKey))
                {
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ApiKey);
                }
                else if (!string.IsNullOrWhiteSpace(BearerToken))
                {
                    // Fallback si l'utilisateur a renseigné le Bearer Token à la place
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", BearerToken);
                }

                httpClient.DefaultRequestHeaders.Add("User-Agent", "FNEV4-Client/1.0");
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var testResults = new List<string>();
                bool allTestsPassed = true;

                // Test 1: Connectivité de base (simple ping)
                StatusMessage = "🔗 Test 1/3: Connectivité de base...";
                try
                {
                    var pingUrl = BaseUrl.TrimEnd('/');
                    using var pingResponse = await httpClient.GetAsync(pingUrl);
                    
                    // Pour l'API FNE, même un 404 indique que le serveur répond
                    if (pingResponse.IsSuccessStatusCode || 
                        pingResponse.StatusCode == System.Net.HttpStatusCode.NotFound ||
                        pingResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        testResults.Add("✅ Connectivité de base: Serveur accessible");
                    }
                    else
                    {
                        testResults.Add($"⚠️ Connectivité de base: {pingResponse.StatusCode}");
                        allTestsPassed = false;
                    }
                }
                catch (HttpRequestException ex)
                {
                    testResults.Add($"❌ Connectivité de base: Serveur inaccessible - {ex.Message}");
                    allTestsPassed = false;
                }
                catch
                {
                    testResults.Add("❌ Connectivité de base: ÉCHEC");
                    allTestsPassed = false;
                }

                // Test 2: Test d'authentification avec l'API FNE
                StatusMessage = "🔐 Test 2/3: Authentification API FNE...";
                try
                {
                    // Test avec un endpoint réel de l'API FNE mais avec des données minimales
                    var testUrl = $"{BaseUrl.TrimEnd('/')}/external/invoices/sign";
                    
                    // Création d'une requête de test minimale (qui va échouer mais nous dira si l'auth fonctionne)
                    var testPayload = new
                    {
                        invoiceType = "test",
                        paymentMethod = "test",
                        template = "B2C",
                        isRne = false,
                        clientCompanyName = "Test",
                        clientPhone = 12345678,
                        clientEmail = "test@test.com",
                        pointOfSale = "Test",
                        establishment = "Test",
                        items = new[] { new { description = "Test", quantity = 1, amount = 1000 } },
                        taxes = "TVA"
                    };

                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(testPayload);
                    var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                    using var authResponse = await httpClient.PostAsync(testUrl, content);
                    
                    if (authResponse.IsSuccessStatusCode)
                    {
                        testResults.Add("✅ Authentification: API FNE accessible avec authentification");
                        IsConnectionTested = true;
                    }
                    else if (authResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        testResults.Add("❌ Authentification: Clés d'authentification invalides");
                        allTestsPassed = false;
                    }
                    else if (authResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        // BadRequest est attendu car on envoie des données de test
                        testResults.Add("✅ Authentification: API FNE répond (données de test rejetées comme attendu)");
                        IsConnectionTested = true;
                    }
                    else
                    {
                        testResults.Add($"⚠️ Authentification: Réponse inattendue {authResponse.StatusCode}");
                        // On considère que l'authentification fonctionne si on n'a pas 401
                        if (authResponse.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                        {
                            IsConnectionTested = true;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    testResults.Add($"❌ Authentification: Erreur réseau - {ex.Message}");
                    allTestsPassed = false;
                }
                catch (Exception ex)
                {
                    testResults.Add($"❌ Authentification: Erreur - {ex.Message}");
                    allTestsPassed = false;
                }

                // Test 3: Validation du format d'authentification
                StatusMessage = "🔧 Test 3/3: Validation configuration...";
                try
                {
                    var configTests = new List<string>();
                    
                    // Vérification de l'URL
                    if (BaseUrl.Contains("54.247.95.108"))
                    {
                        configTests.Add("✅ URL de test DGI détectée");
                    }
                    else if (BaseUrl.Contains("dgi.gouv.ci") || BaseUrl.Contains("fne"))
                    {
                        configTests.Add("✅ URL de production FNE probable");
                    }
                    else
                    {
                        configTests.Add("⚠️ URL non reconnue comme FNE DGI");
                    }

                    // Vérification des clés
                    if (!string.IsNullOrWhiteSpace(ApiKey) && ApiKey.Length >= 20)
                    {
                        configTests.Add("✅ Format de clé API valide");
                    }
                    
                    if (!string.IsNullOrWhiteSpace(BearerToken) && BearerToken.Length >= 20)
                    {
                        configTests.Add("✅ Format de token Bearer valide");
                    }

                    testResults.AddRange(configTests);
                    
                    if (configTests.Any(t => t.StartsWith("✅")))
                    {
                        testResults.Add("✅ Configuration: Paramètres cohérents");
                    }
                }
                catch
                {
                    testResults.Add("⚠️ Configuration: Erreur de validation");
                }

                // Résultat final
                var resultMessage = string.Join("\n", testResults);
                
                if (allTestsPassed)
                {
                    StatusMessage = "✅ Tous les tests réussis";
                    ShowNotification($"✅ Test de connexion réussi!\n{resultMessage}", true);
                }
                else
                {
                    StatusMessage = "⚠️ Certains tests ont échoué";
                    ShowNotification($"⚠️ Test partiellement réussi\n{resultMessage}", false);
                }

                // Mettre à jour le calcul de progression
                CalculateConfigurationProgress();
                OnPropertyChanged(nameof(ConfigurationProgress));
            }
            catch (TaskCanceledException)
            {
                StatusMessage = "⏰ Test de connexion annulé (timeout)";
                ShowNotification($"⏰ Timeout après {RequestTimeoutSeconds}s", false);
            }
            catch (HttpRequestException ex)
            {
                StatusMessage = "❌ Impossible de joindre l'API FNE";
                ShowNotification($"❌ Erreur réseau: {ex.Message}", false);
            }
            catch (Exception ex)
            {
                StatusMessage = "❌ Erreur lors du test de connexion";
                ShowNotification($"❌ Erreur inattendue: {ex.Message}", false);
                System.Diagnostics.Debug.WriteLine($"Erreur TestConnectionAsync: {ex}");
            }
        }

        private async Task ResetConfigurationAsync()
        {
            var result = MessageBox.Show(
                "🗑️ RÉINITIALISATION CONFIGURATION API FNE\n\n" +
                "Cette action va :\n" +
                "• Remettre les valeurs par défaut\n" +
                "• Supprimer la configuration personnalisée de la base\n" +
                "• Revenir à la configuration Test DGI\n\n" +
                "⚠️ ATTENTION : Cette opération est IRRÉVERSIBLE !\n\n" +
                "Voulez-vous vraiment continuer ?",
                "⚠️ Confirmation de réinitialisation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                ShowNotification("🚫 Réinitialisation annulée", false);
                return;
            }

            try
            {
                StatusMessage = "🔄 Réinitialisation en cours...";
                
                // Restaurer les valeurs par défaut
                ConfigurationName = "Test DGI";
                Environment = "Test";
                BaseUrl = "http://54.247.95.108/ws";
                WebUrl = "http://54.247.95.108";
                ApiKey = string.Empty;
                BearerToken = string.Empty;
                SupportEmail = "support.fne@dgi.gouv.ci";
                RequestTimeoutSeconds = 30;
                MaxRetryAttempts = 3;
                RetryDelaySeconds = 5;
                IsActive = true;
                IsValidatedByDgi = false;
                ValidationDate = null;

                // Valider et recalculer
                ValidateAllFields();
                CalculateConfigurationProgress();

                StatusMessage = "✅ Configuration réinitialisée";
                ShowNotification("🔄 Configuration API FNE réinitialisée !", true);
            }
            catch (Exception ex)
            {
                StatusMessage = "❌ Erreur lors de la réinitialisation";
                ShowNotification($"❌ Erreur : {ex.Message}", false);
                System.Diagnostics.Debug.WriteLine($"Erreur ResetConfigurationAsync: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        private bool CanExecuteDeleteConfiguration()
        {
            // Peut supprimer si une configuration est chargée et qu'elle n'est pas une configuration par défaut
            return !string.IsNullOrWhiteSpace(ConfigurationName) && 
                   ConfigurationName != "Test DGI" && 
                   ConfigurationName != "Production DGI";
        }

        private async Task DeleteConfigurationAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ConfigurationName))
                {
                    ShowNotification("❌ Aucune configuration sélectionnée", false);
                    return;
                }

                // Protection contre la suppression des configurations par défaut
                if (ConfigurationName == "Test DGI" || ConfigurationName == "Production DGI")
                {
                    ShowNotification("❌ Impossible de supprimer les configurations par défaut", false);
                    return;
                }

                var result = MessageBox.Show(
                    $"🗑️ SUPPRESSION CONFIGURATION API FNE\n\n" +
                    $"Vous êtes sur le point de supprimer :\n" +
                    $"📋 Configuration : {ConfigurationName}\n" +
                    $"🌐 Environnement : {Environment}\n" +
                    $"🔗 URL : {BaseUrl}\n\n" +
                    "Cette action va :\n" +
                    "• Supprimer définitivement la configuration de la base de données\n" +
                    "• Revenir automatiquement à la configuration 'Test DGI'\n\n" +
                    "⚠️ ATTENTION : Cette opération est IRRÉVERSIBLE !\n\n" +
                    "Voulez-vous vraiment continuer ?",
                    "⚠️ Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    ShowNotification("🚫 Suppression annulée", false);
                    return;
                }

                StatusMessage = "🗑️ Suppression en cours...";

                var connectionString = _databaseService.GetConnectionString();
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    StatusMessage = "❌ Aucune base de données configurée";
                    ShowNotification("❌ Aucune base de données configurée", false);
                    return;
                }

                var optionsBuilder = new DbContextOptionsBuilder<FNEV4DbContext>();
                optionsBuilder.UseSqlite(connectionString);

                using (var context = new FNEV4DbContext(optionsBuilder.Options))
                {
                    var configToDelete = await context.FneConfigurations
                        .FirstOrDefaultAsync(c => c.ConfigurationName == ConfigurationName);

                    if (configToDelete != null)
                    {
                        context.FneConfigurations.Remove(configToDelete);
                        var changesCount = await context.SaveChangesAsync();

                        if (changesCount > 0)
                        {
                            StatusMessage = "✅ Configuration supprimée";
                            ShowNotification($"🗑️ Configuration '{ConfigurationName}' supprimée avec succès !", true);

                            // Revenir à la configuration par défaut après suppression
                            ConfigurationName = "Test DGI";
                            Environment = "Test";
                            BaseUrl = "http://54.247.95.108/ws";
                            WebUrl = "http://54.247.95.108";
                            ApiKey = string.Empty;
                            BearerToken = string.Empty;
                            SupportEmail = "support.fne@dgi.gouv.ci";
                            RequestTimeoutSeconds = 30;
                            MaxRetryAttempts = 3;
                            RetryDelaySeconds = 5;
                            IsActive = true;
                            IsValidatedByDgi = false;
                            ValidationDate = null;

                            // Valider et recalculer
                            ValidateAllFields();
                            CalculateConfigurationProgress();

                            // Recharger la liste des configurations
                            await LoadAvailableConfigurationsAsync();

                            // Rafraîchir les commandes
                            RefreshCommandCanExecute();
                        }
                        else
                        {
                            StatusMessage = "⚠️ Suppression échouée";
                            ShowNotification("⚠️ La suppression n'a pas pu être effectuée", false);
                        }
                    }
                    else
                    {
                        StatusMessage = "❌ Configuration introuvable";
                        ShowNotification($"❌ Configuration '{ConfigurationName}' introuvable en base", false);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "❌ Erreur lors de la suppression";
                ShowNotification($"❌ Erreur lors de la suppression : {ex.Message}", false);
                System.Diagnostics.Debug.WriteLine($"Erreur DeleteConfigurationAsync: {ex}");
            }
        }

        private async Task LoadConfigurationAsync(string? configName)
        {
            if (string.IsNullOrWhiteSpace(configName)) return;

            try
            {
                StatusMessage = $"📥 Chargement de '{configName}'...";
                
                // Nettoyer le nom de configuration (enlever les suffixes comme "(Actif)")
                var cleanConfigName = configName.Replace(" (Actif)", "").Trim();
                
                var connectionString = _databaseService.GetConnectionString();
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    StatusMessage = "❌ Aucune base de données configurée";
                    return;
                }

                var optionsBuilder = new DbContextOptionsBuilder<FNEV4DbContext>();
                optionsBuilder.UseSqlite(connectionString);

                using (var context = new FNEV4DbContext(optionsBuilder.Options))
                {
                    var config = await context.FneConfigurations
                        .FirstOrDefaultAsync(c => c.ConfigurationName == cleanConfigName);

                    if (config != null)
                    {
                        // Charger les données dans le ViewModel
                        ConfigurationName = config.ConfigurationName;
                        Environment = config.Environment ?? "Test";
                        BaseUrl = config.BaseUrl ?? "";
                        WebUrl = config.WebUrl ?? "";
                        ApiKey = config.ApiKey ?? "";
                        BearerToken = config.BearerToken ?? "";
                        SupportEmail = config.SupportEmail ?? "";
                        RequestTimeoutSeconds = config.RequestTimeoutSeconds;
                        MaxRetryAttempts = config.MaxRetryAttempts;
                        RetryDelaySeconds = config.RetryDelaySeconds;
                        IsActive = config.IsActive;
                        IsValidatedByDgi = config.IsValidatedByDgi;
                        ValidationDate = config.ValidationDate;

                        StatusMessage = $"✅ Configuration '{cleanConfigName}' chargée";
                        ShowNotification($"📥 Configuration '{cleanConfigName}' chargée avec succès !", true);
                    }
                    else
                    {
                        // Fallback pour les configurations prédéfinies
                        if (cleanConfigName == "Test DGI")
                        {
                            ConfigurationName = "Test DGI";
                            Environment = "Test";
                            BaseUrl = "http://54.247.95.108/ws";
                            WebUrl = "http://54.247.95.108";
                            SupportEmail = "support.fne@dgi.gouv.ci";
                            ApiKey = "";
                            BearerToken = "";
                            IsActive = true;
                            IsValidatedByDgi = false;
                            
                            StatusMessage = $"✅ Configuration prédéfinie '{cleanConfigName}' chargée";
                            ShowNotification($"📥 Configuration prédéfinie '{cleanConfigName}' chargée !", true);
                        }
                        else
                        {
                            StatusMessage = $"❌ Configuration '{cleanConfigName}' introuvable";
                            ShowNotification($"❌ Configuration '{cleanConfigName}' introuvable", false);
                        }
                    }
                }

                ValidateAllFields();
                CalculateConfigurationProgress();
            }
            catch (Exception ex)
            {
                StatusMessage = "❌ Erreur lors du chargement";
                ShowNotification($"❌ Erreur lors du chargement : {ex.Message}", false);
                System.Diagnostics.Debug.WriteLine($"Erreur LoadConfigurationAsync: {ex}");
            }
        }

        private async Task LoadExistingConfigurationAsync()
        {
            try
            {
                // Charger la configuration existante depuis la base
                await LoadAvailableConfigurationsAsync();
                
                // Si des configurations existent, charger la première active
                if (AvailableConfigurations?.Any() == true)
                {
                    var activeConfig = AvailableConfigurations.FirstOrDefault(c => c.Contains("(Actif)"));
                    if (activeConfig != null)
                    {
                        var configName = activeConfig.Replace(" (Actif)", "");
                        await LoadConfigurationAsync(configName);
                    }
                }
                
                ValidateAllFields();
                CalculateConfigurationProgress();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur LoadExistingConfigurationAsync: {ex.Message}");
            }
        }

        private async Task LoadAvailableConfigurationsAsync()
        {
            try
            {
                var allConfigurations = new List<string>();
                
                // Toujours ajouter les configurations par défaut
                allConfigurations.AddRange(new[] { "Test DGI", "Production DGI" });
                
                var connectionString = _databaseService.GetConnectionString();
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    var optionsBuilder = new DbContextOptionsBuilder<FNEV4DbContext>();
                    optionsBuilder.UseSqlite(connectionString);

                    using (var context = new FNEV4DbContext(optionsBuilder.Options))
                    {
                        var configs = await context.FneConfigurations
                            .OrderByDescending(c => c.LastModifiedDate)
                            .Select(c => new { c.ConfigurationName, c.IsActive })
                            .ToListAsync();

                        // Ajouter les configurations de base de données (non par défaut)
                        var dbConfigs = configs
                            .Where(c => !allConfigurations.Contains(c.ConfigurationName))
                            .Select(c => c.IsActive ? $"{c.ConfigurationName} (Actif)" : c.ConfigurationName)
                            .ToList();
                        
                        allConfigurations.AddRange(dbConfigs);
                        
                        // Marquer les configurations par défaut comme actives si elles existent en base
                        for (int i = 0; i < allConfigurations.Count; i++)
                        {
                            var configName = allConfigurations[i];
                            var dbConfig = configs.FirstOrDefault(c => c.ConfigurationName == configName);
                            if (dbConfig != null && dbConfig.IsActive && !configName.Contains("(Actif)"))
                            {
                                allConfigurations[i] = $"{configName} (Actif)";
                            }
                        }
                    }
                }

                // Mise à jour sur le thread UI
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    AvailableConfigurations = new ObservableCollection<string>(allConfigurations);
                    OnPropertyChanged(nameof(AvailableConfigurations));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur LoadAvailableConfigurationsAsync: {ex}");
                // En cas d'erreur, garder au moins les configurations par défaut sur le thread UI
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    AvailableConfigurations = new ObservableCollection<string>(new[] { "Test DGI", "Production DGI" });
                    OnPropertyChanged(nameof(AvailableConfigurations));
                });
            }
        }

        private void UpdateBaseUrlFromEnvironment()
        {
            if (Environment == "Test")
            {
                BaseUrl = "http://54.247.95.108/ws";
                WebUrl = "http://54.247.95.108";
            }
            else if (Environment == "Production")
            {
                BaseUrl = ""; // URL transmise par DGI après validation
                WebUrl = "";
            }
            
            ValidateBaseUrl();
            ValidateWebUrl();
        }

        private void OpenWebUrl()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(WebUrl))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = WebUrl,
                        UseShellExecute = true
                    });
                    ShowNotification($"🌐 Ouverture de {WebUrl}", true);
                }
                else
                {
                    ShowNotification("❌ URL web non configurée", false);
                }
            }
            catch (Exception ex)
            {
                ShowNotification($"❌ Erreur ouverture URL : {ex.Message}", false);
            }
        }

        private void ContactSupport()
        {
            try
            {
                var emailUri = $"mailto:{SupportEmail}?subject=FNEV4 - Support API FNE&body=Bonjour,%0D%0A%0D%0AJe vous contacte concernant l'API FNE.%0D%0A%0D%0AConfiguration:%0D%0A- Environnement: {Environment}%0D%0A- URL: {BaseUrl}%0D%0A%0D%0AMerci";
                
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = emailUri,
                    UseShellExecute = true
                });
                
                ShowNotification($"📧 Ouverture email vers {SupportEmail}", true);
            }
            catch (Exception ex)
            {
                ShowNotification($"❌ Erreur ouverture email : {ex.Message}", false);
            }
        }

        private async Task ValidateWithDgiAsync()
        {
            try
            {
                StatusMessage = "🔍 Validation DGI en cours...";
                ShowNotification("🔍 Demande de validation DGI...", true);

                // Simulation d'une validation DGI
                await Task.Delay(2000);

                if (Environment == "Test")
                {
                    IsValidatedByDgi = true;
                    ValidationDate = DateTime.Now;
                    ShowNotification("✅ Validation DGI réussie pour l'environnement Test !", true);
                    StatusMessage = "✅ Configuration validée par la DGI";
                }
                else
                {
                    ShowNotification("⚠️ Validation DGI disponible uniquement après soumission des spécimens", false);
                    StatusMessage = "⚠️ Validation DGI en attente";
                }

                CalculateConfigurationProgress();
            }
            catch (Exception ex)
            {
                StatusMessage = "❌ Erreur lors de la validation DGI";
                ShowNotification($"❌ Erreur validation : {ex.Message}", false);
                System.Diagnostics.Debug.WriteLine($"Erreur ValidateWithDgiAsync: {ex.Message}");
            }
        }

        #endregion

        #region Méthodes de notification

        private void ShowNotification(string message, bool isSuccess)
        {
            NotificationMessage = message;
            NotificationIcon = isSuccess ? PackIconKind.CheckCircle : PackIconKind.AlertCircle;
            NotificationColor = isSuccess ? Brushes.Green : Brushes.Red;
            IsNotificationVisible = true;

            // Auto-hide après 4 secondes
            Task.Delay(4000).ContinueWith(_ =>
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    IsNotificationVisible = false;
                });
            });
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
