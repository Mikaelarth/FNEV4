using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using FNEV4.Application.UseCases.GestionClients;
using InfraLogging = FNEV4.Infrastructure.Services.ILoggingService;

namespace FNEV4.Presentation.ViewModels.GestionClients
{
    /// <summary>
    /// ViewModel pour la liste des clients
    /// Module: Gestion Clients > Liste des clients
    /// </summary>
    public partial class ListeClientsViewModel : ObservableObject
    {
        #region Services

        private readonly ListeClientsUseCase _listeClientsUseCase;
        private readonly InfraLogging _loggingService;
        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<Client> _clients = new();

        [ObservableProperty]
        private Client? _selectedClient;

        [ObservableProperty]
        private string _searchTerm = string.Empty;

        [ObservableProperty]
        private string? _selectedClientType;

        [ObservableProperty]
        private bool? _isActiveFilter = true;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _pageSize = 50;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private int _totalPages;

        [ObservableProperty]
        private bool _hasNextPage;

        [ObservableProperty]
        private bool _hasPreviousPage;

        [ObservableProperty]
        private ClientStatistics? _statistics;

        #endregion

        #region Collections

        public ObservableCollection<string> ClientTypes { get; } = new()
        {
            "Tous",
            "Individual",
            "Company", 
            "Government",
            "International"
        };

        public ObservableCollection<int> PageSizes { get; } = new()
        {
            25, 50, 100, 200
        };

        #endregion

        #region Constructor

        public ListeClientsViewModel(
            ListeClientsUseCase listeClientsUseCase,
            InfraLogging loggingService,
            IServiceProvider serviceProvider)
        {
            _listeClientsUseCase = listeClientsUseCase;
            _loggingService = loggingService;
            _serviceProvider = serviceProvider;

            // Initialisation des valeurs par défaut
            SelectedClientType = "Tous";

            // Commandes
            LoadClientsCommand = new AsyncRelayCommand(LoadClientsAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => HasPreviousPage);
            DeleteClientCommand = new AsyncRelayCommand<Client>(DeleteClientAsync);
            ViewClientDetailsCommand = new RelayCommand<Client>(ViewClientDetails);
            EditClientCommand = new RelayCommand<Client>(EditClient);
            AddClientCommand = new RelayCommand(AddClient);
            ImportClientsCommand = new RelayCommand(ImportClients);

            // Charger les données initiales
            _ = Task.Run(InitializeAsync);
        }

        #endregion

        #region Commands

        public IAsyncRelayCommand LoadClientsCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearFiltersCommand { get; }
        public IAsyncRelayCommand NextPageCommand { get; }
        public IAsyncRelayCommand PreviousPageCommand { get; }
        public IAsyncRelayCommand<Client> DeleteClientCommand { get; }
        public IRelayCommand<Client> ViewClientDetailsCommand { get; }
        public IRelayCommand<Client> EditClientCommand { get; }
        public IRelayCommand AddClientCommand { get; }
        public IRelayCommand ImportClientsCommand { get; }

        #endregion

        #region Methods

        private async Task InitializeAsync()
        {
            try
            {
                await LoadStatisticsAsync();
                await LoadClientsAsync();
                
                await _loggingService.LogInfoAsync("Liste des clients initialisée", "GestionClients");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Erreur lors de l'initialisation", ex);
            }
        }

        private async Task LoadClientsAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;

                var request = new ListeClientsRequest
                {
                    PageNumber = CurrentPage,
                    PageSize = PageSize,
                    SearchTerm = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
                    ClientType = SelectedClientType,
                    IsActive = IsActiveFilter
                };

                var response = await _listeClientsUseCase.ExecuteAsync(request);

                if (response.Success)
                {
                    Clients.Clear();
                    foreach (var client in response.Clients)
                    {
                        Clients.Add(client);
                    }

                    TotalCount = response.TotalCount;
                    TotalPages = response.TotalPages;
                    HasNextPage = response.HasNextPage;
                    HasPreviousPage = response.HasPreviousPage;

                    // Mettre à jour l'état des commandes de navigation
                    NextPageCommand.NotifyCanExecuteChanged();
                    PreviousPageCommand.NotifyCanExecuteChanged();
                }
                else
                {
                    await HandleErrorAsync("Erreur lors du chargement des clients", response.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Erreur lors du chargement des clients", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                var response = await _listeClientsUseCase.GetStatisticsAsync();
                if (response.Success)
                {
                    Statistics = response.Statistics;
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur lors du chargement des statistiques: {ex.Message}", "GestionClients", ex);
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                
                await _loggingService.LogInfoAsync("Actualisation de la liste des clients demandée", "GestionClients");
                
                // Recharger les statistiques et les clients en parallèle pour améliorer les performances
                var statisticsTask = LoadStatisticsAsync();
                var clientsTask = LoadClientsAsync();
                
                await Task.WhenAll(statisticsTask, clientsTask);
                
                await _loggingService.LogInfoAsync("Actualisation de la liste des clients terminée", "GestionClients");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Erreur lors de l'actualisation", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchAsync()
        {
            CurrentPage = 1; // Retourner à la première page lors d'une recherche
            await LoadClientsAsync();
        }

        private void ClearFilters()
        {
            SearchTerm = string.Empty;
            SelectedClientType = null;
            IsActiveFilter = true;
            CurrentPage = 1;
            
            _ = Task.Run(LoadClientsAsync);
        }

        private async Task NextPageAsync()
        {
            if (HasNextPage)
            {
                CurrentPage++;
                await LoadClientsAsync();
            }
        }

        private async Task PreviousPageAsync()
        {
            if (HasPreviousPage)
            {
                CurrentPage--;
                await LoadClientsAsync();
            }
        }

        private async Task DeleteClientAsync(Client? client)
        {
            if (client == null) return;

            try
            {
                // Demander confirmation (TODO: implémenter dialog de confirmation)
                // Pour l'instant, on procède directement
                
                var response = await _listeClientsUseCase.DeleteClientAsync(client.Id);
                
                if (response.Success)
                {
                    await _loggingService.LogInfoAsync($"Client {client.Name} supprimé avec succès", "GestionClients");
                    await RefreshAsync(); // Recharger la liste
                }
                else
                {
                    await HandleErrorAsync("Erreur lors de la suppression", response.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Erreur lors de la suppression du client", ex);
            }
        }

        private void ViewClientDetails(Client? client)
        {
            if (client == null) return;
            
            // TODO: Naviguer vers la vue détails du client
            // Navigation.NavigateToClientDetails(client.Id);
        }

        private void EditClient(Client? client)
        {
            if (client == null) return;
            
            // TODO: Naviguer vers l'édition du client
            // Navigation.NavigateToEditClient(client.Id);
        }

        #endregion

        #region Property Changed Handlers

        partial void OnIsLoadingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsListVisible));
        }

        partial void OnPageSizeChanged(int value)
        {
            CurrentPage = 1; // Retourner à la première page quand on change la taille
            OnPropertyChanged(nameof(PaginationInfo));
            _ = Task.Run(LoadClientsAsync);
        }

        partial void OnSelectedClientTypeChanged(string? value)
        {
            CurrentPage = 1;
            OnPropertyChanged(nameof(ActiveFiltersInfo));
            _ = Task.Run(LoadClientsAsync);
        }

        partial void OnIsActiveFilterChanged(bool? value)
        {
            CurrentPage = 1;
            OnPropertyChanged(nameof(ActiveFiltersInfo));
            _ = Task.Run(LoadClientsAsync);
        }

        partial void OnSearchTermChanged(string value)
        {
            OnPropertyChanged(nameof(ActiveFiltersInfo));
        }

        partial void OnCurrentPageChanged(int value)
        {
            OnPropertyChanged(nameof(PaginationInfo));
        }

        partial void OnTotalCountChanged(int value)
        {
            OnPropertyChanged(nameof(PaginationInfo));
        }

        partial void OnTotalPagesChanged(int value)
        {
            OnPropertyChanged(nameof(PaginationInfo));
        }

        #endregion

        #region Helper Methods

        private async Task HandleErrorAsync(string message, Exception? ex = null)
        {
            var fullMessage = ex != null ? $"{message}: {ex.Message}" : message;
            
            ErrorMessage = fullMessage;
            HasError = true;
            
            await _loggingService.LogErrorAsync(fullMessage, "GestionClients", ex);
        }

        private async Task HandleErrorAsync(string message, string? errorDetails)
        {
            var fullMessage = !string.IsNullOrEmpty(errorDetails) ? $"{message}: {errorDetails}" : message;
            
            ErrorMessage = fullMessage;
            HasError = true;
            
            await _loggingService.LogErrorAsync(fullMessage, "GestionClients");
        }

        #endregion

        #region Public Properties for Binding

        /// <summary>
        /// Information de pagination formatée
        /// </summary>
        public string PaginationInfo => 
            $"Page {CurrentPage} sur {TotalPages} ({TotalCount} client(s) au total)";

        /// <summary>
        /// Indique si la liste doit être visible (inverse de IsLoading)
        /// </summary>
        public bool IsListVisible => !IsLoading;

        /// <summary>
        /// Texte du résumé des filtres actifs
        /// </summary>
        public string ActiveFiltersInfo
        {
            get
            {
                var filters = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                    filters.Add($"Recherche: '{SearchTerm}'");
                
                if (!string.IsNullOrWhiteSpace(SelectedClientType))
                    filters.Add($"Type: {SelectedClientType}");
                
                if (IsActiveFilter.HasValue)
                    filters.Add($"Statut: {(IsActiveFilter.Value ? "Actif" : "Inactif")}");

                return filters.Count > 0 ? string.Join(" | ", filters) : "Aucun filtre actif";
            }
        }

        #endregion

        #region Navigation Methods

        private void AddClient()
        {
            try
            {
                _loggingService.LogInfoAsync("Ouverture du formulaire d'ajout de client", "GestionClients");

                // Créer le ViewModel pour l'ajout
                var coreLoggingService = _serviceProvider.GetService<FNEV4.Core.Interfaces.ILoggingService>()!;
                var ajoutViewModel = new AjoutModificationClientViewModel(
                    _serviceProvider.GetService<AjoutClientUseCase>()!,
                    _serviceProvider.GetService<ModificationClientUseCase>()!,
                    coreLoggingService);

                // Configurer pour nouveau client
                ajoutViewModel.ConfigureForNewClient();

                // Créer et afficher la fenêtre
                var ajoutView = new Views.GestionClients.AjoutModificationClientView(ajoutViewModel);
                ajoutView.Owner = System.Windows.Application.Current.MainWindow;

                var result = ajoutView.ShowDialog();

                // Si le client a été créé avec succès, actualiser la liste
                if (result == true)
                {
                    _loggingService.LogInfoAsync("Client ajouté avec succès, actualisation de la liste", "GestionClients");
                    _ = Task.Run(RefreshAsync);
                    
                    // Afficher un message de succès
                    System.Windows.MessageBox.Show(
                        "Client créé avec succès !",
                        "Succès",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogErrorAsync($"Erreur lors de l'ouverture du formulaire d'ajout: {ex.Message}", "GestionClients", ex);
            }
        }

        private void ImportClients()
        {
            try
            {
                _ = _loggingService.LogInfoAsync("Ouverture de l'interface d'import Excel", "GestionClients");

                // Créer le ViewModel pour l'import
                var coreLoggingService = _serviceProvider.GetService<FNEV4.Core.Interfaces.ILoggingService>()!;
                var importUseCase = _serviceProvider.GetService<ImportClientsExcelUseCase>()!;
                var importViewModel = new ImportClientsViewModel(importUseCase, coreLoggingService);

                // Créer et afficher la fenêtre d'import
                var importWindow = new Views.GestionClients.ImportClientsWindow(importViewModel);
                importWindow.Owner = System.Windows.Application.Current.MainWindow;

                var result = importWindow.ShowDialog();

                // Si l'import a été réalisé avec succès, actualiser la liste
                if (result == true)
                {
                    _ = _loggingService.LogInfoAsync("Import Excel terminé, actualisation de la liste", "GestionClients");
                    _ = Task.Run(RefreshAsync);
                    
                    // Afficher un message de succès
                    System.Windows.MessageBox.Show(
                        "Import terminé avec succès ! Consultez les résultats dans la fenêtre d'import.",
                        "Import terminé",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = _loggingService.LogErrorAsync($"Erreur lors de l'ouverture de l'interface d'import: {ex.Message}", "GestionClients", ex);
                
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'ouverture de l'interface d'import:\n{ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
