using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Application.UseCases.GestionClients;
using FNEV4.Application.Extensions;
using FNEV4.Core.DTOs;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace FNEV4.Presentation.ViewModels.GestionClients
{
    public partial class AjoutModificationClientViewModel : ObservableObject
    {
        private readonly AjoutClientUseCase _ajoutClientUseCase;
        private readonly ModificationClientUseCase _modificationClientUseCase;
        private readonly ILoggingService _loggingService;

        public AjoutModificationClientViewModel(
            AjoutClientUseCase ajoutClientUseCase,
            ModificationClientUseCase modificationClientUseCase,
            ILoggingService loggingService)
        {
            _ajoutClientUseCase = ajoutClientUseCase ?? throw new ArgumentNullException(nameof(ajoutClientUseCase));
            _modificationClientUseCase = modificationClientUseCase ?? throw new ArgumentNullException(nameof(modificationClientUseCase));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

            InitializeClientTypes();
            InitializeNewClient();
        }

        #region Properties

        [ObservableProperty]
        private ClientDto _client = new();

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _clientTypes = new();

        // Window properties
        public string WindowTitle => IsEditMode ? "Modification du client" : "Nouveau client";
        public string HeaderTitle => IsEditMode ? "Modification" : "Création";
        public string HeaderSubtitle => IsEditMode ? "Modifier les informations du client" : "Ajouter un nouveau client au système";
        public string HeaderIcon => IsEditMode ? "AccountEdit" : "AccountPlus";
        public string SaveButtonText => IsEditMode ? "Modifier" : "Créer";

        public bool CanSave => !IsSaving && IsFormValid();

        #endregion

        #region Commands

        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                IsSaving = true;
                HasError = false;
                ErrorMessage = string.Empty;

                // Validation finale
                if (!ValidateClient())
                {
                    return;
                }

                // Log de l'action
                _loggingService.LogInfo($"Début {(IsEditMode ? "modification" : "création")} client: {Client.Name}");

                // Sauvegarde
                var result = IsEditMode 
                    ? await _modificationClientUseCase.ExecuteAsync(Client)
                    : await _ajoutClientUseCase.ExecuteAsync(Client);

                if (result.IsSuccess)
                {
                    _loggingService.LogInfo($"Client {(IsEditMode ? "modifié" : "créé")} avec succès: {Client.Name}");
                    
                    // Fermer la fenêtre avec succès
                    if (WpfApp.Current.MainWindow is Window mainWindow)
                    {
                        foreach (Window window in WpfApp.Current.Windows)
                        {
                            if (window.DataContext == this)
                            {
                                window.DialogResult = true;
                                window.Close();
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Afficher l'erreur
                    HasError = true;
                    ErrorMessage = result.ErrorMessage ?? "Une erreur inattendue s'est produite.";
                    _loggingService.LogError($"Erreur lors de la sauvegarde du client: {ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = "Erreur technique lors de la sauvegarde.";
                _loggingService.LogError(ex, "Erreur dans SaveAsync");
            }
            finally
            {
                IsSaving = false;
                OnPropertyChanged(nameof(CanSave));
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            // Fermer la fenêtre sans sauvegarder
            if (WpfApp.Current.MainWindow is Window mainWindow)
            {
                foreach (Window window in WpfApp.Current.Windows)
                {
                    if (window.DataContext == this)
                    {
                        window.DialogResult = false;
                        window.Close();
                        break;
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Configure le ViewModel pour la création d'un nouveau client
        /// </summary>
        public void ConfigureForNewClient()
        {
            IsEditMode = false;
            InitializeNewClient();
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(HeaderSubtitle));
            OnPropertyChanged(nameof(HeaderIcon));
            OnPropertyChanged(nameof(SaveButtonText));
        }

        /// <summary>
        /// Configure le ViewModel pour la modification d'un client existant
        /// </summary>
        public void ConfigureForEditClient(ClientDto clientToEdit)
        {
            if (clientToEdit == null)
                throw new ArgumentNullException(nameof(clientToEdit));

            IsEditMode = true;
            Client = new ClientDto
            {
                Id = clientToEdit.Id,
                Code = clientToEdit.Code,
                Name = clientToEdit.Name,
                Type = clientToEdit.Type,
                Ncc = clientToEdit.Ncc,
                Email = clientToEdit.Email,
                Phone = clientToEdit.Phone,
                Address = clientToEdit.Address,
                City = clientToEdit.City,
                PostalCode = clientToEdit.PostalCode,
                Notes = clientToEdit.Notes,
                IsActive = clientToEdit.IsActive,
                CreatedDate = clientToEdit.CreatedDate
            };

            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(HeaderSubtitle));
            OnPropertyChanged(nameof(HeaderIcon));
            OnPropertyChanged(nameof(SaveButtonText));
        }

        #endregion

        #region Private Methods

        private void InitializeClientTypes()
        {
            ClientTypes.Clear();
            ClientTypes.Add("Particulier");
            ClientTypes.Add("Entreprise");
            ClientTypes.Add("Association");
            ClientTypes.Add("Administration");
        }

        private void InitializeNewClient()
        {
            Client = new ClientDto
            {
                Type = "Particulier", // Valeur par défaut
                IsActive = true,
                CreatedDate = DateTime.Now
            };
        }

        private bool IsFormValid()
        {
            return !string.IsNullOrWhiteSpace(Client.Code) &&
                   !string.IsNullOrWhiteSpace(Client.Name) &&
                   !string.IsNullOrWhiteSpace(Client.Type);
        }

        private bool ValidateClient()
        {
            var errors = new List<string>();

            // Validation du code client
            if (string.IsNullOrWhiteSpace(Client.Code))
            {
                errors.Add("Le code client est obligatoire.");
            }
            else if (Client.Code.Length < 2)
            {
                errors.Add("Le code client doit contenir au moins 2 caractères.");
            }

            // Validation du nom
            if (string.IsNullOrWhiteSpace(Client.Name))
            {
                errors.Add("Le nom/raison sociale est obligatoire.");
            }
            else if (Client.Name.Length < 2)
            {
                errors.Add("Le nom doit contenir au moins 2 caractères.");
            }

            // Validation du type
            if (string.IsNullOrWhiteSpace(Client.Type))
            {
                errors.Add("Le type de client est obligatoire.");
            }

            // Validation de l'email (si fourni)
            if (!string.IsNullOrWhiteSpace(Client.Email))
            {
                var emailValidator = new EmailAddressAttribute();
                if (!emailValidator.IsValid(Client.Email))
                {
                    errors.Add("L'adresse email n'est pas valide.");
                }
            }

            // Validation du NCC (si fourni)
            if (!string.IsNullOrWhiteSpace(Client.Ncc))
            {
                if (Client.Ncc.Length < 8 || Client.Ncc.Length > 15)
                {
                    errors.Add("Le NCC doit contenir entre 8 et 15 caractères.");
                }
            }

            if (errors.Any())
            {
                HasError = true;
                ErrorMessage = string.Join("\n", errors);
                return false;
            }

            HasError = false;
            ErrorMessage = string.Empty;
            return true;
        }

        #endregion

        #region Property Changed Overrides

        partial void OnClientChanged(ClientDto value)
        {
            OnPropertyChanged(nameof(CanSave));
        }

        partial void OnIsSavingChanged(bool value)
        {
            OnPropertyChanged(nameof(CanSave));
        }

        #endregion
    }
}
