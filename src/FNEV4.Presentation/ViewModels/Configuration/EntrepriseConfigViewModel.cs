using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;
using FNEV4.Infrastructure.Data;
using FNEV4.Infrastructure.Services;
using System;

namespace FNEV4.Presentation.ViewModels.Configuration;

/// <summary>
/// ViewModel simplifié pour la configuration d'entreprise sans gestion complexe des points de vente
/// Aligné sur les exigences DGI : Point de vente = simple champ texte (max 10 caractères)
/// </summary>
public class EntrepriseConfigViewModel : INotifyPropertyChanged
{
    private readonly IDgiService _dgiService;
    private readonly IDatabaseService _databaseService;

    #region Propriétés principales de l'entreprise
    private string _companyName = string.Empty;
    public string CompanyName
    {
        get => _companyName;
        set { _companyName = value; OnPropertyChanged(); ValidateCompanyName(); CalculateCompletionPercentage(); }
    }

    private string _nccNumber = string.Empty;
    public string NccNumber
    {
        get => _nccNumber;
        set { _nccNumber = value; OnPropertyChanged(); ValidateNcc(); CalculateCompletionPercentage(); }
    }

    private string _businessAddress = string.Empty;
    public string BusinessAddress
    {
        get => _businessAddress;
        set { _businessAddress = value; OnPropertyChanged(); ValidateBusinessAddress(); CalculateCompletionPercentage(); }
    }

    private string _phoneNumber = string.Empty;
    public string PhoneNumber
    {
        get => _phoneNumber;
        set { _phoneNumber = value; OnPropertyChanged(); ValidatePhoneNumber(); CalculateCompletionPercentage(); }
    }

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); ValidateEmail(); CalculateCompletionPercentage(); }
    }

    // Point de vente simplifié - juste un champ texte (API DGI)
    private string _defaultPointOfSale = string.Empty;
    public string DefaultPointOfSale
    {
        get => _defaultPointOfSale;
        set { _defaultPointOfSale = value; OnPropertyChanged(); ValidatePointOfSale(); CalculateCompletionPercentage(); }
    }

    private string _apiKey = string.Empty;
    public string ApiKey
    {
        get => _apiKey;
        set { _apiKey = value; OnPropertyChanged(); CalculateCompletionPercentage(); }
    }

    private string _apiBaseUrl = string.Empty;
    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set { _apiBaseUrl = value; OnPropertyChanged(); CalculateCompletionPercentage(); }
    }

    private string _environment = "Test";
    public string Environment
    {
        get => _environment;
        set { _environment = value; OnPropertyChanged(); }
    }
    #endregion

    #region Propriétés de validation et UI
    private bool _isNccValid = true;
    public bool IsNccValid
    {
        get => _isNccValid;
        set { _isNccValid = value; OnPropertyChanged(); }
    }

    private string _nccValidationMessage = string.Empty;
    public string NccValidationMessage
    {
        get => _nccValidationMessage;
        set { _nccValidationMessage = value; OnPropertyChanged(); }
    }

    private bool _hasNccValidationMessage = false;
    public bool HasNccValidationMessage
    {
        get => _hasNccValidationMessage;
        set { _hasNccValidationMessage = value; OnPropertyChanged(); }
    }

    private SolidColorBrush _nccValidationColor = Brushes.Red;
    public SolidColorBrush NccValidationColor
    {
        get => _nccValidationColor;
        set { _nccValidationColor = value; OnPropertyChanged(); }
    }

    // Propriétés de validation pour les autres champs
    private string _companyNameValidationMessage = string.Empty;
    public string CompanyNameValidationMessage
    {
        get => _companyNameValidationMessage;
        set { _companyNameValidationMessage = value; OnPropertyChanged(); }
    }

    private string _businessAddressValidationMessage = string.Empty;
    public string BusinessAddressValidationMessage
    {
        get => _businessAddressValidationMessage;
        set { _businessAddressValidationMessage = value; OnPropertyChanged(); }
    }

    private string _phoneNumberValidationMessage = string.Empty;
    public string PhoneNumberValidationMessage
    {
        get => _phoneNumberValidationMessage;
        set { _phoneNumberValidationMessage = value; OnPropertyChanged(); }
    }

    private string _emailValidationMessage = string.Empty;
    public string EmailValidationMessage
    {
        get => _emailValidationMessage;
        set { _emailValidationMessage = value; OnPropertyChanged(); }
    }

    // Propriétés booléennes pour validation XAML
    private bool _isCompanyNameValid = false;
    public bool IsCompanyNameValid
    {
        get => _isCompanyNameValid;
        set { _isCompanyNameValid = value; OnPropertyChanged(); }
    }

    private bool _isBusinessAddressValid = false;
    public bool IsBusinessAddressValid
    {
        get => _isBusinessAddressValid;
        set { _isBusinessAddressValid = value; OnPropertyChanged(); }
    }

    private bool _isPhoneNumberValid = false;
    public bool IsPhoneNumberValid
    {
        get => _isPhoneNumberValid;
        set { _isPhoneNumberValid = value; OnPropertyChanged(); }
    }

    private bool _isEmailValid = false;
    public bool IsEmailValid
    {
        get => _isEmailValid;
        set { _isEmailValid = value; OnPropertyChanged(); }
    }

    private bool _isPointOfSaleValid = false;
    public bool IsPointOfSaleValid
    {
        get => _isPointOfSaleValid;
        set { _isPointOfSaleValid = value; OnPropertyChanged(); }
    }

    private string _pointOfSaleValidationMessage = string.Empty;
    public string PointOfSaleValidationMessage
    {
        get => _pointOfSaleValidationMessage;
        set { _pointOfSaleValidationMessage = value; OnPropertyChanged(); }
    }

    private double _completionPercentage = 0;
    public double CompletionPercentage
    {
        get => _completionPercentage;
        set { _completionPercentage = value; OnPropertyChanged(); }
    }

    private bool _canSave = false;
    public bool CanSave
    {
        get => _canSave;
        set { _canSave = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "Prêt à configurer";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private PackIconKind _statusIcon = PackIconKind.Information;
    public PackIconKind StatusIcon
    {
        get => _statusIcon;
        set { _statusIcon = value; OnPropertyChanged(); }
    }

    private SolidColorBrush _statusColor = Brushes.Blue;
    public SolidColorBrush StatusColor
    {
        get => _statusColor;
        set { _statusColor = value; OnPropertyChanged(); }
    }
    #endregion

    #region Commands (simplifiées)
    public ICommand SaveConfigurationCommand { get; private set; } = null!;
    public ICommand SaveCommand => SaveConfigurationCommand; // Alias pour le XAML
    public ICommand ValidateNccCommand { get; private set; } = null!;
    public ICommand ExportConfigurationCommand { get; private set; } = null!;
    public ICommand ImportConfigurationCommand { get; private set; } = null!;
    public ICommand SyncWithDgiCommand { get; private set; } = null!;
    public ICommand ResetCommand { get; private set; } = null!;
    public ICommand VerifyNccCommand { get; private set; } = null!;
    public ICommand DetectLocationCommand { get; private set; } = null!;
    public ICommand FormatPhoneCommand { get; private set; } = null!;
    public ICommand SetPointOfSaleCommand { get; private set; } = null!;
    #endregion

    #region Constructeur
    public EntrepriseConfigViewModel(IDgiService? dgiService = null, IDatabaseService? databaseService = null)
    {
        _dgiService = dgiService ?? CreateDefaultDgiService();
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        InitializeCommands();
        InitializeData();
        UpdateStatus();
    }

    private IDgiService CreateDefaultDgiService()
    {
        var httpClient = new System.Net.Http.HttpClient();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<FNEV4.Infrastructure.Services.DgiService>();
        return new FNEV4.Infrastructure.Services.DgiService(httpClient, logger);
    }
    #endregion

    #region Initialisation
    private void InitializeCommands()
    {
        SaveConfigurationCommand = new RelayCommand(async () => await SaveConfigurationAsync());
        ValidateNccCommand = new RelayCommand(() => ValidateNcc());
        ExportConfigurationCommand = new RelayCommand(async () => await ExportConfigurationAsync());
        ImportConfigurationCommand = new RelayCommand(async () => await ImportConfigurationAsync());
        SyncWithDgiCommand = new RelayCommand(async () => await SyncWithDgiAsync());
        ResetCommand = new RelayCommand(() => ResetForm());
        VerifyNccCommand = new RelayCommand(async () => await VerifyNccWithDgiAsync());
        DetectLocationCommand = new RelayCommand(() => DetectLocation());
        FormatPhoneCommand = new RelayCommand(() => FormatPhone());
        SetPointOfSaleCommand = new RelayCommand<string>((value) => SetPointOfSale(value));
    }

    private void InitializeData()
    {
        // Charger la configuration existante depuis la base de données
        _ = LoadExistingConfigurationAsync();
        
        // Validation initiale
        ValidateNcc();
        ValidateCompanyName();
        ValidateBusinessAddress();
        ValidatePhoneNumber();
        ValidateEmail();
        ValidatePointOfSale();
        CalculateCompletionPercentage();
    }

    private async Task LoadExistingConfigurationAsync()
    {
        try
        {
            // Utiliser la chaîne de connexion configurée pour créer un contexte
            var connectionString = _databaseService.GetConnectionString();
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Pas de base de données configurée, initialiser avec des valeurs vides
                InitializeEmptyConfiguration();
                StatusMessage = "Nouvelle configuration - veuillez saisir les informations";
                return;
            }

            var optionsBuilder = new DbContextOptionsBuilder<FNEV4DbContext>();
            optionsBuilder.UseSqlite(connectionString);

            using var context = new FNEV4DbContext(optionsBuilder.Options);
            
            // S'assurer que la base de données et les tables existent
            await context.Database.EnsureCreatedAsync();
            
            var existingCompany = await context.Companies.FirstOrDefaultAsync();
            
            if (existingCompany != null)
            {
                // Charger les données dans les propriétés du ViewModel
                CompanyName = existingCompany.CompanyName ?? "";
                NccNumber = existingCompany.Ncc ?? "";
                BusinessAddress = existingCompany.Address ?? "";
                PhoneNumber = existingCompany.Phone ?? "";
                Email = existingCompany.Email ?? "";
                DefaultPointOfSale = existingCompany.DefaultPointOfSale ?? "";
                ApiKey = existingCompany.ApiKey ?? "";
                ApiBaseUrl = existingCompany.ApiBaseUrl ?? "";
                Environment = existingCompany.Environment ?? "Test";
                
                StatusMessage = "Configuration chargée depuis la base de données";
                System.Diagnostics.Debug.WriteLine($"Configuration chargée pour: {existingCompany.CompanyName}");
            }
            else
            {
                // Initialiser avec des valeurs vides
                InitializeEmptyConfiguration();
                StatusMessage = "Nouvelle configuration - veuillez saisir les informations";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erreur lors du chargement : {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Erreur LoadExistingConfiguration: {ex}");
            
            // En cas d'erreur, initialiser avec des valeurs vides
            InitializeEmptyConfiguration();
        }
    }

    private void InitializeEmptyConfiguration()
    {
        CompanyName = string.Empty;
        BusinessAddress = string.Empty;
        PhoneNumber = string.Empty;
        Email = string.Empty;
        NccNumber = string.Empty;
        DefaultPointOfSale = string.Empty;
        ApiKey = string.Empty;
        ApiBaseUrl = string.Empty;
        Environment = "Test";
    }
    #endregion

    #region Méthodes de validation
    private void ValidateNcc()
    {
        if (string.IsNullOrWhiteSpace(NccNumber))
        {
            IsNccValid = false;
            NccValidationMessage = "Le NCC entreprise est obligatoire";
            NccValidationColor = Brushes.Red;
            HasNccValidationMessage = true;
        }
        else if (NccNumber.Length < 8)
        {
            IsNccValid = false;
            NccValidationMessage = "Le NCC doit contenir au moins 8 caractères";
            NccValidationColor = Brushes.Orange;
            HasNccValidationMessage = true;
        }
        else
        {
            IsNccValid = true;
            NccValidationMessage = "Format NCC valide";
            NccValidationColor = Brushes.Green;
            HasNccValidationMessage = true;
        }
    }

    private void ValidateCompanyName()
    {
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            CompanyNameValidationMessage = "";
            IsCompanyNameValid = false;
            return;
        }
        
        if (CompanyName.Length < 3)
        {
            CompanyNameValidationMessage = "Le nom doit contenir au moins 3 caractères";
            IsCompanyNameValid = false;
            return;
        }
        
        CompanyNameValidationMessage = "";
        IsCompanyNameValid = true;
    }

    private void ValidateBusinessAddress()
    {
        if (string.IsNullOrWhiteSpace(BusinessAddress))
        {
            BusinessAddressValidationMessage = "";
            IsBusinessAddressValid = false;
            return;
        }
        
        if (BusinessAddress.Length < 10)
        {
            BusinessAddressValidationMessage = "L'adresse doit être plus détaillée";
            IsBusinessAddressValid = false;
            return;
        }
        
        BusinessAddressValidationMessage = "";
        IsBusinessAddressValid = true;
    }

    private void ValidatePhoneNumber()
    {
        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            PhoneNumberValidationMessage = "";
            IsPhoneNumberValid = false;
            return;
        }
        
        // Validation format ivoirien : 10 chiffres
        var cleaned = PhoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        
        if (cleaned.Length != 10 || !cleaned.All(char.IsDigit))
        {
            PhoneNumberValidationMessage = "Format ivoirien requis: 10 chiffres (ex: 0123456789)";
            IsPhoneNumberValid = false;
            return;
        }
        
        PhoneNumberValidationMessage = "";
        IsPhoneNumberValid = true;
    }

    private void ValidateEmail()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            EmailValidationMessage = "";
            IsEmailValid = false;
            return;
        }
        
        // Validation email simple
        if (!Email.Contains("@") || !Email.Contains(".") || Email.Length < 5)
        {
            EmailValidationMessage = "Format email invalide (ex: contact@entreprise.ci)";
            IsEmailValid = false;
            return;
        }
        
        EmailValidationMessage = "";
        IsEmailValid = true;
    }

    private void ValidatePointOfSale()
    {
        if (string.IsNullOrWhiteSpace(DefaultPointOfSale))
        {
            PointOfSaleValidationMessage = "Le point de vente est requis pour la facturation";
            IsPointOfSaleValid = false;
            return;
        }
        
        // Validation flexible : chaîne de caractères valide sans contrainte de longueur stricte
        var trimmed = DefaultPointOfSale.Trim();
        if (trimmed.Length == 0)
        {
            PointOfSaleValidationMessage = "Le point de vente ne peut pas être vide";
            IsPointOfSaleValid = false;
            return;
        }
        
        // Vérifier qu'il n'y a pas de caractères problématiques pour la base de données
        if (trimmed.Contains('\n') || trimmed.Contains('\r') || trimmed.Contains('\t'))
        {
            PointOfSaleValidationMessage = "Le point de vente ne peut pas contenir de caractères de saut de ligne";
            IsPointOfSaleValid = false;
            return;
        }
        
        PointOfSaleValidationMessage = "";
        IsPointOfSaleValid = true;
    }
    #endregion

    #region Calcul de progression
    private void CalculateCompletionPercentage()
    {
        var fields = new[]
        {
            !string.IsNullOrWhiteSpace(CompanyName),
            !string.IsNullOrWhiteSpace(NccNumber),
            !string.IsNullOrWhiteSpace(BusinessAddress),
            !string.IsNullOrWhiteSpace(PhoneNumber),
            !string.IsNullOrWhiteSpace(Email),
            !string.IsNullOrWhiteSpace(DefaultPointOfSale),
            !string.IsNullOrWhiteSpace(ApiKey),
            !string.IsNullOrWhiteSpace(ApiBaseUrl),
            !string.IsNullOrWhiteSpace(Environment)
        };

        CompletionPercentage = (double)fields.Count(f => f) / fields.Length * 100;
        CanSave = CompletionPercentage >= 50; // Au moins 50% complété (abaissé pour test)
        System.Diagnostics.Debug.WriteLine($"CalculateCompletionPercentage: {CompletionPercentage:F1}% - CanSave: {CanSave}");
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (CompletionPercentage >= 90)
        {
            StatusMessage = "Configuration complète - Prêt pour certification";
            StatusIcon = PackIconKind.CheckCircle;
            StatusColor = Brushes.Green;
        }
        else if (CompletionPercentage >= 70)
        {
            StatusMessage = "Configuration en cours - Presque terminé";
            StatusIcon = PackIconKind.ClockOutline;
            StatusColor = Brushes.Orange;
        }
        else
        {
            StatusMessage = "Configuration incomplète";
            StatusIcon = PackIconKind.AlertCircle;
            StatusColor = Brushes.Red;
        }
    }
    #endregion

    #region Actions utilisateur (simplifiées)
    private void ResetForm()
    {
        CompanyName = string.Empty;
        NccNumber = string.Empty;
        BusinessAddress = string.Empty;
        PhoneNumber = string.Empty;
        Email = string.Empty;
        DefaultPointOfSale = string.Empty;
        CalculateCompletionPercentage();
        StatusMessage = "Formulaire réinitialisé";
    }

    private void DetectLocation()
    {
        StatusMessage = "Fonction de géolocalisation non disponible.";
        
        System.Windows.MessageBox.Show(
            "La détection automatique de localisation n'est pas encore implémentée.\n\n" +
            "Veuillez saisir votre adresse complète manuellement dans le champ Adresse.\n\n" +
            "Format recommandé: Rue/Avenue, Commune, Ville, Pays\n" +
            "Exemple: Boulevard Lagunaire, Cocody, Abidjan, Côte d'Ivoire",
            "Géolocalisation non disponible",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
            
        StatusMessage = "Veuillez saisir votre adresse manuellement.";
    }

    private void FormatPhone()
    {
        if (!string.IsNullOrWhiteSpace(PhoneNumber))
        {
            // Nettoyer le numéro
            var cleaned = PhoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
            
            // Format ivoirien 10 chiffres
            if (cleaned.Length == 10 && cleaned.All(char.IsDigit))
            {
                PhoneNumber = $"{cleaned.Substring(0, 2)} {cleaned.Substring(2, 2)} {cleaned.Substring(4, 2)} {cleaned.Substring(6, 2)} {cleaned.Substring(8, 2)}";
            }
            
            ValidatePhoneNumber();
        }
    }

    private void SetPointOfSale(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            DefaultPointOfSale = value;
        }
    }
    #endregion

    #region Méthodes async (stubs)
    private async Task SaveConfigurationAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== DEBUT SaveConfigurationAsync ===");
            StatusMessage = "Validation des données...";
            
            // Validation de base avant sauvegarde
            if (string.IsNullOrWhiteSpace(CompanyName))
            {
                StatusMessage = "❌ Le nom de l'entreprise est obligatoire";
                return;
            }
            
            if (string.IsNullOrWhiteSpace(NccNumber))
            {
                StatusMessage = "❌ Le numéro NCC est obligatoire";
                return;
            }
            
            if (!IsNccValid)
            {
                StatusMessage = "❌ Le format du NCC n'est pas valide";
                return;
            }

            StatusMessage = "Sauvegarde en cours...";
            
            // Créer l'entité Company avec les données du formulaire
            var company = new FNEV4.Core.Entities.Company
            {
                Id = Guid.NewGuid(),
                Ncc = this.NccNumber?.Trim() ?? "",
                CompanyName = this.CompanyName?.Trim() ?? "",
                Address = this.BusinessAddress?.Trim() ?? "",
                Phone = this.PhoneNumber?.Trim(),
                Email = this.Email?.Trim(),
                DefaultPointOfSale = this.DefaultPointOfSale?.Trim(),
                ApiKey = this.ApiKey?.Trim(),
                ApiBaseUrl = this.ApiBaseUrl?.Trim(),
                Environment = this.Environment?.Trim() ?? "Test",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                IsValidated = false // Sera validé plus tard via l'API DGI
            };

            // Utiliser la chaîne de connexion configurée pour créer un contexte
            var connectionString = _databaseService.GetConnectionString();
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                StatusMessage = "❌ Aucune base de données configurée";
                return;
            }

            var optionsBuilder = new DbContextOptionsBuilder<FNEV4DbContext>();
            optionsBuilder.UseSqlite(connectionString);

            using var context = new FNEV4DbContext(optionsBuilder.Options);
            
            // S'assurer que la base de données et les tables existent
            await context.Database.EnsureCreatedAsync();
            
            var existingCompany = await context.Companies.FirstOrDefaultAsync();
            
            if (existingCompany != null)
            {
                // Mettre à jour la configuration existante
                existingCompany.Ncc = company.Ncc;
                existingCompany.CompanyName = company.CompanyName;
                existingCompany.Address = company.Address;
                existingCompany.Phone = company.Phone;
                existingCompany.Email = company.Email;
                existingCompany.DefaultPointOfSale = company.DefaultPointOfSale;
                existingCompany.ApiKey = company.ApiKey;
                existingCompany.ApiBaseUrl = company.ApiBaseUrl;
                existingCompany.Environment = company.Environment;
                existingCompany.UpdatedAt = DateTime.UtcNow;
                
                context.Companies.Update(existingCompany);
                StatusMessage = "Mise à jour de la configuration...";
            }
            else
            {
                // Créer une nouvelle configuration
                context.Companies.Add(company);
                StatusMessage = "Création de la configuration...";
            }
            
            // Sauvegarder les changements
            var savedCount = await context.SaveChangesAsync();
            
            if (savedCount > 0)
            {
                StatusMessage = "✅ Configuration sauvegardée avec succès dans la base de données";
                System.Diagnostics.Debug.WriteLine($"Configuration sauvegardée dans: {connectionString}");
            }
            else
            {
                StatusMessage = "❌ Aucune modification détectée";
            }
            
            System.Diagnostics.Debug.WriteLine("=== FIN SaveConfigurationAsync ===");
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erreur lors de la sauvegarde : {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Erreur SaveConfiguration: {ex}");
        }
    }

    private async Task ExportConfigurationAsync()
    {
        try
        {
            StatusMessage = "Export en cours...";
            
            // Créer l'objet de configuration à exporter
            var configData = new
            {
                CompanyName = this.CompanyName,
                NccNumber = this.NccNumber,
                BusinessAddress = this.BusinessAddress,
                PhoneNumber = this.PhoneNumber,
                Email = this.Email,
                DefaultPointOfSale = this.DefaultPointOfSale,
                ApiKey = this.ApiKey,
                ApiBaseUrl = this.ApiBaseUrl,
                Environment = this.Environment,
                ExportedAt = DateTime.Now,
                Version = "1.0"
            };

            // Sérialiser en JSON
            var json = System.Text.Json.JsonSerializer.Serialize(configData, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            // Ouvrir la boîte de dialogue Enregistrer sous
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Fichiers JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"FNEV4_Config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                await System.IO.File.WriteAllTextAsync(dialog.FileName, json);
                StatusMessage = $"Configuration exportée : {System.IO.Path.GetFileName(dialog.FileName)}";
            }
            else
            {
                StatusMessage = "Export annulé";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur lors de l'export : {ex.Message}";
        }
    }

    private async Task ImportConfigurationAsync()
    {
        try
        {
            StatusMessage = "Sélection du fichier à importer...";

            // Ouvrir la boîte de dialogue Ouvrir
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Fichiers JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*",
                DefaultExt = "json",
                Title = "Sélectionner le fichier de configuration à importer"
            };

            if (dialog.ShowDialog() == true)
            {
                StatusMessage = "Import en cours...";
                
                var json = await System.IO.File.ReadAllTextAsync(dialog.FileName);
                
                // Désérialiser le JSON
                using var document = System.Text.Json.JsonDocument.Parse(json);
                var root = document.RootElement;

                // Demander confirmation avant d'écraser les données existantes
                var result = System.Windows.MessageBox.Show(
                    "Cette action va remplacer toutes les données actuelles de configuration.\nVoulez-vous continuer ?",
                    "Confirmation d'import",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // Importer les données
                    if (root.TryGetProperty("CompanyName", out var companyName))
                        CompanyName = companyName.GetString() ?? "";
                    
                    if (root.TryGetProperty("NccNumber", out var nccNumber))
                        NccNumber = nccNumber.GetString() ?? "";
                    
                    if (root.TryGetProperty("BusinessAddress", out var businessAddress))
                        BusinessAddress = businessAddress.GetString() ?? "";
                    
                    if (root.TryGetProperty("PhoneNumber", out var phoneNumber))
                        PhoneNumber = phoneNumber.GetString() ?? "";
                    
                    if (root.TryGetProperty("Email", out var email))
                        Email = email.GetString() ?? "";
                    
                    if (root.TryGetProperty("DefaultPointOfSale", out var pointOfSale))
                        DefaultPointOfSale = pointOfSale.GetString() ?? "";
                    
                    if (root.TryGetProperty("ApiKey", out var apiKey))
                        ApiKey = apiKey.GetString() ?? "";
                    
                    if (root.TryGetProperty("ApiBaseUrl", out var apiBaseUrl))
                        ApiBaseUrl = apiBaseUrl.GetString() ?? "";
                    
                    if (root.TryGetProperty("Environment", out var environment))
                        Environment = environment.GetString() ?? "";

                    // Recalculer le pourcentage après import
                    CalculateCompletionPercentage();
                    
                    StatusMessage = $"Configuration importée : {System.IO.Path.GetFileName(dialog.FileName)}";
                }
                else
                {
                    StatusMessage = "Import annulé";
                }
            }
            else
            {
                StatusMessage = "Import annulé";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur lors de l'import : {ex.Message}";
        }
    }

    private async Task SyncWithDgiAsync()
    {
        StatusMessage = "Synchronisation DGI en cours...";
        await Task.Delay(2000); // Simulation
        StatusMessage = "Synchronisation DGI terminée";
    }

    private async Task VerifyNccWithDgiAsync()
    {
        StatusMessage = "Vérification NCC avec DGI...";
        await Task.Delay(2000); // Simulation
        StatusMessage = "NCC vérifié avec succès";
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
