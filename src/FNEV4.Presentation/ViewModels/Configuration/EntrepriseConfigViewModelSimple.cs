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

namespace FNEV4.Presentation.ViewModels.Configuration;

/// <summary>
/// ViewModel simplifié pour la configuration d'entreprise sans gestion complexe des points de vente
/// Aligné sur les exigences DGI : Point de vente = simple champ texte (max 10 caractères)
/// </summary>
public class EntrepriseConfigViewModelSimple : INotifyPropertyChanged
{
    private readonly IDgiService _dgiService;

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

    private string _businessSector = string.Empty;
    public string BusinessSector
    {
        get => _businessSector;
        set { _businessSector = value; OnPropertyChanged(); CalculateCompletionPercentage(); }
    }

    // Point de vente simplifié - juste un champ texte (API DGI)
    private string _defaultPointOfSale = string.Empty;
    public string DefaultPointOfSale
    {
        get => _defaultPointOfSale;
        set { _defaultPointOfSale = value; OnPropertyChanged(); CalculateCompletionPercentage(); }
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
    public ICommand ValidateNccCommand { get; private set; } = null!;
    public ICommand ExportConfigurationCommand { get; private set; } = null!;
    public ICommand ImportConfigurationCommand { get; private set; } = null!;
    public ICommand SyncWithDgiCommand { get; private set; } = null!;
    public ICommand ResetCommand { get; private set; } = null!;
    public ICommand VerifyNccCommand { get; private set; } = null!;
    public ICommand DetectLocationCommand { get; private set; } = null!;
    public ICommand FormatPhoneCommand { get; private set; } = null!;
    #endregion

    #region Constructeur
    public EntrepriseConfigViewModelSimple(IDgiService? dgiService = null)
    {
        _dgiService = dgiService ?? CreateDefaultDgiService();
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
    }

    private void InitializeData()
    {
        // Aucune valeur par défaut - tous les champs doivent être saisis par l'utilisateur
        CompanyName = string.Empty;
        BusinessSector = string.Empty; 
        BusinessAddress = string.Empty;
        PhoneNumber = string.Empty;
        Email = string.Empty;
        NccNumber = string.Empty;
        DefaultPointOfSale = string.Empty;
        
        // Validation initiale (tous les champs vides = invalides)
        ValidateNcc();
        ValidateCompanyName();
        ValidateBusinessAddress();
        CalculateCompletionPercentage();
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
        // Validation simple du nom d'entreprise
    }

    private void ValidateBusinessAddress()
    {
        // Validation simple de l'adresse
    }

    private void ValidatePhoneNumber()
    {
        // Validation format ivoirien 10 chiffres
    }

    private void ValidateEmail()
    {
        // Validation format email
    }
    #endregion

    #region Calcul de progression
    private void CalculateCompletionPercentage()
    {
        var fields = new[]
        {
            !string.IsNullOrWhiteSpace(CompanyName),
            !string.IsNullOrWhiteSpace(NccNumber),
            !string.IsNullOrWhiteSpace(BusinessSector),
            !string.IsNullOrWhiteSpace(BusinessAddress),
            !string.IsNullOrWhiteSpace(PhoneNumber),
            !string.IsNullOrWhiteSpace(Email),
            !string.IsNullOrWhiteSpace(ApiKey),
            !string.IsNullOrWhiteSpace(ApiBaseUrl),
            !string.IsNullOrWhiteSpace(Environment)
        };

        CompletionPercentage = (double)fields.Count(f => f) / fields.Length * 100;
        CanSave = CompletionPercentage >= 70; // Au moins 70% complété
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
        BusinessSector = string.Empty;
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
    #endregion

    #region Méthodes async (stubs)
    private async Task SaveConfigurationAsync()
    {
        StatusMessage = "Sauvegarde en cours...";
        await Task.Delay(1000); // Simulation
        StatusMessage = "Configuration sauvegardée";
    }

    private async Task ExportConfigurationAsync()
    {
        StatusMessage = "Export en cours...";
        await Task.Delay(1000); // Simulation
        StatusMessage = "Configuration exportée";
    }

    private async Task ImportConfigurationAsync()
    {
        StatusMessage = "Import en cours...";
        await Task.Delay(1000); // Simulation
        StatusMessage = "Configuration importée";
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
