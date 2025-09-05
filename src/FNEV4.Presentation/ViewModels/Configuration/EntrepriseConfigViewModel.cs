using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Windows.Media;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using FNEV4.Infrastructure.Services;
using FNEV4.Core.Entities;
using FNEV4.Core.DTOs;
using MaterialDesignThemes.Wpf;

namespace FNEV4.Presentation.ViewModels.Configuration
{
    public class EntrepriseConfigViewModel : INotifyPropertyChanged
    {
        private readonly ILoggingService? _loggingService;

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Entity Properties
        private string _companyName = string.Empty;
        public string CompanyName
        {
            get => _companyName;
            set { _companyName = value; OnPropertyChanged(nameof(CompanyName)); CalculateCompletionPercentage(); }
        }

        private string _nccNumber = string.Empty;
        public string NccNumber
        {
            get => _nccNumber;
            set { _nccNumber = value; OnPropertyChanged(nameof(NccNumber)); CalculateCompletionPercentage(); ValidateNcc(); }
        }

        private string _dgiNumber = string.Empty;
        public string DgiNumber
        {
            get => _dgiNumber;
            set { _dgiNumber = value; OnPropertyChanged(nameof(DgiNumber)); CalculateCompletionPercentage(); }
        }

        private string _businessSector = string.Empty;
        public string BusinessSector
        {
            get => _businessSector;
            set { _businessSector = value; OnPropertyChanged(nameof(BusinessSector)); CalculateCompletionPercentage(); }
        }

        private string _businessAddress = string.Empty;
        public string BusinessAddress
        {
            get => _businessAddress;
            set { _businessAddress = value; OnPropertyChanged(nameof(BusinessAddress)); CalculateCompletionPercentage(); }
        }

        private string _phoneNumber = string.Empty;
        public string PhoneNumber
        {
            get => _phoneNumber;
            set { _phoneNumber = value; OnPropertyChanged(nameof(PhoneNumber)); CalculateCompletionPercentage(); }
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); CalculateCompletionPercentage(); }
        }
        #endregion

        #region UI State Properties
        private bool _isNccValid = true;
        public bool IsNccValid
        {
            get => _isNccValid;
            set { _isNccValid = value; OnPropertyChanged(nameof(IsNccValid)); }
        }

        private bool _canSave = false;
        public bool CanSave
        {
            get => _canSave;
            set { _canSave = value; OnPropertyChanged(nameof(CanSave)); }
        }

        private string _statusMessage = "Entreprise: Non configurée";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        private PackIconKind _statusIcon = PackIconKind.AlertCircle;
        public PackIconKind StatusIcon
        {
            get => _statusIcon;
            set { _statusIcon = value; OnPropertyChanged(nameof(StatusIcon)); }
        }

        private Brush _statusColor = Brushes.Red;
        public Brush StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(nameof(StatusColor)); }
        }

        private double _completionPercentage = 0;
        public double CompletionPercentage
        {
            get => _completionPercentage;
            set { _completionPercentage = value; OnPropertyChanged(nameof(CompletionPercentage)); }
        }

        private string _nccValidationMessage = string.Empty;
        public string NccValidationMessage
        {
            get => _nccValidationMessage;
            set { _nccValidationMessage = value; OnPropertyChanged(nameof(NccValidationMessage)); }
        }

        private Brush _nccValidationColor = Brushes.Gray;
        public Brush NccValidationColor
        {
            get => _nccValidationColor;
            set { _nccValidationColor = value; OnPropertyChanged(nameof(NccValidationColor)); }
        }

        private bool _hasNccValidationMessage = false;
        public bool HasNccValidationMessage
        {
            get => _hasNccValidationMessage;
            set { _hasNccValidationMessage = value; OnPropertyChanged(nameof(HasNccValidationMessage)); }
        }

        private ObservableCollection<string> _companyNameSuggestions = new()
        {
            "SARL", "SAS", "SA", "EURL", "SNC", "SCI", "Auto-entrepreneur"
        };
        public ObservableCollection<string> CompanyNameSuggestions
        {
            get => _companyNameSuggestions;
            set { _companyNameSuggestions = value; OnPropertyChanged(nameof(CompanyNameSuggestions)); }
        }

        private bool _autoSaveEnabled = true;
        public bool AutoSaveEnabled
        {
            get => _autoSaveEnabled;
            set { _autoSaveEnabled = value; OnPropertyChanged(nameof(AutoSaveEnabled)); }
        }

        private bool _autoValidateNcc = true;
        public bool AutoValidateNcc
        {
            get => _autoValidateNcc;
            set { _autoValidateNcc = value; OnPropertyChanged(nameof(AutoValidateNcc)); }
        }

        private bool _syncWithDgi = false;
        public bool SyncWithDgi
        {
            get => _syncWithDgi;
            set { _syncWithDgi = value; OnPropertyChanged(nameof(SyncWithDgi)); }
        }

        private ObservableCollection<PointOfSaleViewModel> _pointsOfSale = new();
        public ObservableCollection<PointOfSaleViewModel> PointsOfSale
        {
            get => _pointsOfSale;
            set { _pointsOfSale = value; OnPropertyChanged(nameof(PointsOfSale)); }
        }

        private bool _hasPointsOfSale = true;
        public bool HasPointsOfSale
        {
            get => _hasPointsOfSale;
            set { _hasPointsOfSale = value; OnPropertyChanged(nameof(HasPointsOfSale)); }
        }
        #endregion

        #region Commands
        public ICommand SaveConfigurationCommand { get; private set; }
        public ICommand ValidateNccCommand { get; private set; }
        public ICommand AddPointOfSaleCommand { get; private set; }
        public ICommand RemovePointOfSaleCommand { get; private set; }
        public ICommand ExportConfigurationCommand { get; private set; }
        public ICommand ImportConfigurationCommand { get; private set; }
        public ICommand SyncWithDgiCommand { get; private set; }
        
        // Commandes supplémentaires pour l'interface avancée
        public ICommand ResetCommand { get; private set; }
        public ICommand VerifyNccCommand { get; private set; }
        public ICommand DetectLocationCommand { get; private set; }
        public ICommand FormatPhoneCommand { get; private set; }
        public ICommand LoadTemplatesCommand { get; private set; }
        public ICommand DuplicatePointOfSaleCommand { get; private set; }
        public ICommand AddHeadOfficeCommand { get; private set; }
        public ICommand AddStoreCommand { get; private set; }
        public ICommand AddWarehouseCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand ExportConfigCommand { get; private set; }
        public ICommand ImportConfigCommand { get; private set; }
        public ICommand SyncNowCommand { get; private set; }
        public ICommand OpenCertificationGuideCommand { get; private set; }
        public ICommand TestInvoicingCommand { get; private set; }
        public ICommand ImportFromSageCommand { get; private set; }
        public ICommand VerifyMappingCommand { get; private set; }
        public ICommand ContactSupportCommand { get; private set; }
        #endregion

        // Constructeur par défaut pour fallback (aligné sur BaseDonneesViewModel)
        public EntrepriseConfigViewModel()
        {
            InitializeCommands();
            InitializeData();
            UpdateStatus();
        }

        private void InitializeCommands()
        {
            SaveConfigurationCommand = new RelayCommand(async () => await SaveConfigurationAsync());
            ValidateNccCommand = new RelayCommand(() => ValidateNcc());
            AddPointOfSaleCommand = new RelayCommand(() => AddPointOfSale());
            RemovePointOfSaleCommand = new RelayCommand<PointOfSaleViewModel>(RemovePointOfSale);
            ExportConfigurationCommand = new RelayCommand(async () => await ExportConfigurationAsync());
            ImportConfigurationCommand = new RelayCommand(async () => await ImportConfigurationAsync());
            SyncWithDgiCommand = new RelayCommand(async () => await SyncWithDgiAsync());
            
            // Commandes supplémentaires avec actions simples
            ResetCommand = new RelayCommand(() => ResetForm());
            VerifyNccCommand = new RelayCommand(() => ValidateNcc());
            DetectLocationCommand = new RelayCommand(() => DetectLocation());
            FormatPhoneCommand = new RelayCommand(() => FormatPhone());
            LoadTemplatesCommand = new RelayCommand(() => LoadTemplates());
            DuplicatePointOfSaleCommand = new RelayCommand<PointOfSaleViewModel>(DuplicatePointOfSale);
            AddHeadOfficeCommand = new RelayCommand(() => AddPointOfSale("Siège Social", "Adresse du siège"));
            AddStoreCommand = new RelayCommand(() => AddPointOfSale("Magasin", "Adresse du magasin"));
            AddWarehouseCommand = new RelayCommand(() => AddPointOfSale("Entrepôt", "Adresse de l'entrepôt"));
            SaveCommand = new RelayCommand(async () => await SaveConfigurationAsync());
            ExportConfigCommand = new RelayCommand(async () => await ExportConfigurationAsync());
            ImportConfigCommand = new RelayCommand(async () => await ImportConfigurationAsync());
            SyncNowCommand = new RelayCommand(async () => await SyncWithDgiAsync());
            OpenCertificationGuideCommand = new RelayCommand(() => OpenCertificationGuide());
            TestInvoicingCommand = new RelayCommand(() => TestInvoicing());
            ImportFromSageCommand = new RelayCommand(() => ImportFromSage());
            VerifyMappingCommand = new RelayCommand(() => VerifyMapping());
            ContactSupportCommand = new RelayCommand(() => ContactSupport());
        }

        private void InitializeData()
        {
            // Données de démonstration
            AddPointOfSale("Magasin Principal", "123 Rue de la République, Tunis", "71234567");
            AddPointOfSale("Succursale Nord", "456 Avenue Bourguiba, Ariana", "71987654");
            
            CompanyName = "Exemple SARL";
            BusinessSector = "Commerce de détail";
            BusinessAddress = "123 Rue de la République, Tunis";
            PhoneNumber = "71234567";
            Email = "contact@exemple.tn";
            
            CalculateCompletionPercentage();
        }

        private void CalculateCompletionPercentage()
        {
            var fields = new[]
            {
                !string.IsNullOrWhiteSpace(CompanyName),
                !string.IsNullOrWhiteSpace(NccNumber),
                !string.IsNullOrWhiteSpace(DgiNumber),
                !string.IsNullOrWhiteSpace(BusinessSector),
                !string.IsNullOrWhiteSpace(BusinessAddress),
                !string.IsNullOrWhiteSpace(PhoneNumber),
                !string.IsNullOrWhiteSpace(Email)
            };

            CompletionPercentage = (double)fields.Count(f => f) / fields.Length * 100;
            CanSave = CompletionPercentage >= 70; // Au moins 70% complété
        }

        private void ValidateNcc()
        {
            if (string.IsNullOrWhiteSpace(NccNumber))
            {
                IsNccValid = true;
                NccValidationMessage = string.Empty;
                HasNccValidationMessage = false;
                return;
            }

            // Validation basique du format NCC (8 chiffres)
            if (NccNumber.Length == 8 && NccNumber.All(char.IsDigit))
            {
                IsNccValid = true;
                NccValidationMessage = "Numéro NCC valide";
                NccValidationColor = Brushes.Green;
                HasNccValidationMessage = true;
            }
            else
            {
                IsNccValid = false;
                NccValidationMessage = "Format invalide (8 chiffres requis)";
                NccValidationColor = Brushes.Red;
                HasNccValidationMessage = true;
            }
        }

        private void UpdateStatus()
        {
            if (CompletionPercentage >= 90)
            {
                StatusMessage = "Configuration complète";
                StatusIcon = PackIconKind.CheckCircle;
                StatusColor = Brushes.Green;
            }
            else if (CompletionPercentage >= 50)
            {
                StatusMessage = "Configuration en cours";
                StatusIcon = PackIconKind.ProgressClock;
                StatusColor = Brushes.Orange;
            }
            else
            {
                StatusMessage = "Configuration incomplète";
                StatusIcon = PackIconKind.AlertCircle;
                StatusColor = Brushes.Red;
            }
        }

        private void AddPointOfSale(string name = "", string address = "", string phone = "")
        {
            var item = new PointOfSaleItem(
                string.IsNullOrEmpty(name) ? $"Point de vente {PointsOfSale.Count + 1}" : name,
                string.IsNullOrEmpty(address) ? "Adresse à définir" : address,
                phone,
                true
            );
            
            PointsOfSale.Add(new PointOfSaleViewModel(item));
            HasPointsOfSale = PointsOfSale.Any();
        }

        private void RemovePointOfSale(PointOfSaleViewModel? viewModel)
        {
            if (viewModel != null && PointsOfSale.Contains(viewModel))
            {
                PointsOfSale.Remove(viewModel);
                HasPointsOfSale = PointsOfSale.Any();
            }
        }

        private async Task SaveConfigurationAsync()
        {
            try
            {
                StatusMessage = "Sauvegarde en cours...";
                StatusIcon = PackIconKind.ContentSave;
                StatusColor = Brushes.Blue;

                // Simulation de sauvegarde
                await Task.Delay(1000);

                StatusMessage = "Configuration sauvegardée";
                StatusIcon = PackIconKind.CheckCircle;
                StatusColor = Brushes.Green;

                // Reset après 3 secondes
                await Task.Delay(3000);
                UpdateStatus();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur de sauvegarde: {ex.Message}";
                StatusIcon = PackIconKind.AlertCircle;
                StatusColor = Brushes.Red;
            }
        }

        private async Task ExportConfigurationAsync()
        {
            // Placeholder pour export
            StatusMessage = "Export en cours...";
            await Task.Delay(1000);
            StatusMessage = "Export terminé";
        }

        private async Task ImportConfigurationAsync()
        {
            // Placeholder pour import
            StatusMessage = "Import en cours...";
            await Task.Delay(1000);
            StatusMessage = "Import terminé";
        }

        private async Task SyncWithDgiAsync()
        {
            // Placeholder pour synchronisation DGI
            StatusMessage = "Synchronisation DGI en cours...";
            await Task.Delay(2000);
            StatusMessage = "Synchronisation terminée";
        }

        #region Méthodes supplémentaires pour les commandes avancées
        
        private void ResetForm()
        {
            CompanyName = string.Empty;
            NccNumber = string.Empty;
            DgiNumber = string.Empty;
            BusinessSector = string.Empty;
            BusinessAddress = string.Empty;
            PhoneNumber = string.Empty;
            Email = string.Empty;
            PointsOfSale.Clear();
            CalculateCompletionPercentage();
            StatusMessage = "Formulaire réinitialisé";
        }

        private void DetectLocation()
        {
            StatusMessage = "Détection de l'emplacement...";
            // Simulation de détection
            BusinessAddress = "Adresse détectée automatiquement";
            StatusMessage = "Emplacement détecté";
        }

        private void FormatPhone()
        {
            if (!string.IsNullOrWhiteSpace(PhoneNumber))
            {
                // Format tunisien simple
                var cleaned = PhoneNumber.Replace(" ", "").Replace("-", "");
                if (cleaned.Length == 8 && cleaned.All(char.IsDigit))
                {
                    PhoneNumber = $"{cleaned.Substring(0, 2)} {cleaned.Substring(2, 3)} {cleaned.Substring(5, 3)}";
                }
            }
        }

        private void LoadTemplates()
        {
            PointsOfSale.Clear();
            AddPointOfSale("Siège Social", "Avenue Habib Bourguiba, Tunis", "71234567");
            AddPointOfSale("Magasin Principal", "Rue de la République, Tunis", "71234568");
            AddPointOfSale("Succursale Nord", "Zone Industrielle, Ariana", "71234569");
            StatusMessage = "Templates de points de vente chargés";
        }

        private void DuplicatePointOfSale(PointOfSaleViewModel? original)
        {
            if (original != null)
            {
                var duplicate = new PointOfSaleViewModel(new PointOfSaleItem(
                    $"{original.Name} (Copie)",
                    original.Address,
                    original.PhoneNumber,
                    original.IsActive
                ));
                PointsOfSale.Add(duplicate);
                StatusMessage = "Point de vente dupliqué";
            }
        }

        private void OpenCertificationGuide()
        {
            StatusMessage = "Ouverture du guide de certification...";
            // Ici on pourrait ouvrir un navigateur ou une fenêtre d'aide
        }

        private void TestInvoicing()
        {
            StatusMessage = "Test de facturation en cours...";
            // Simulation de test
        }

        private void ImportFromSage()
        {
            StatusMessage = "Import depuis Sage 100 en cours...";
            // Simulation d'import
        }

        private void VerifyMapping()
        {
            StatusMessage = "Vérification de correspondance en cours...";
            // Simulation de vérification
        }

        private void ContactSupport()
        {
            StatusMessage = "Ouverture de l'assistance...";
            // Ici on pourrait ouvrir un email ou chat
        }

        #endregion
    }

    // RelayCommand simple pour les commandes
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
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
    }

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
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}