using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using FNEV4.Application.UseCases.GestionClients;
using FNEV4.Presentation.Messages;
using FNEV4.Presentation.Views.Special;
using InfraLogging = FNEV4.Infrastructure.Services.ILoggingService;

namespace FNEV4.Presentation.ViewModels.GestionClients
{
    /// <summary>
    /// ViewModel pour la liste des clients
    /// Module: Gestion Clients > Liste des clients
    /// </summary>
    public partial class ListeClientsViewModel : ObservableObject, IRecipient<ClientsImportedMessage>, IDisposable
    {
        #region Static Members

        // Verrou statique pour éviter la concurrence DbContext
        private static readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);

        #endregion

        #region Services

        private readonly ListeClientsUseCase _listeClientsUseCase;
        private readonly InfraLogging _loggingService;
        private readonly IServiceProvider _serviceProvider;
        
        // Pour la recherche en temps réel avec debounce
        private CancellationTokenSource? _searchCancellationTokenSource;
        private const int SearchDebounceDelayMs = 300; // 300ms de délai

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
        private StatusOption? _selectedStatusOption;

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
            "B2C",  // Individual -> B2C
            "B2B",  // Company -> B2B 
            "B2G",  // Government -> B2G
            "B2F"   // International -> B2F
        };

        public ObservableCollection<StatusOption> StatusOptions { get; } = new()
        {
            new StatusOption { Display = "Tous", Value = null },
            new StatusOption { Display = "Actif", Value = true },
            new StatusOption { Display = "Inactif", Value = false }
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

            // Commandes
            LoadClientsCommand = new AsyncRelayCommand(LoadClientsAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            SearchCommand = new AsyncRelayCommand(ClearFiltersAsync);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => HasPreviousPage);
            DeleteClientCommand = new AsyncRelayCommand<Client>(DeleteClientAsync);
            ViewClientDetailsCommand = new RelayCommand<Client>(ViewClientDetails);
            EditClientCommand = new RelayCommand<Client>(EditClient);
            AddClientCommand = new RelayCommand(AddClient);
            ImportClientsCommand = new RelayCommand(ImportClients);
            ImportExceptionnelCommand = new RelayCommand(ImportExceptionnel);

            // Enregistrement pour recevoir les notifications d'import
            WeakReferenceMessenger.Default.Register<ClientsImportedMessage>(this);

            // INITIALISATION AUTOMATIQUE comme BaseDonneesViewModel
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
        public IRelayCommand ImportExceptionnelCommand { get; }

        #endregion

        #region Methods

        private async Task InitializeAsync()
        {
            try
            {
                // Initialisation différée pour éviter les conflits de binding
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedClientType = "Tous"; // Initialisation sécurisée
                    SelectedStatusOption = StatusOptions.FirstOrDefault(s => s.Value == null); // Sélectionner "Tous" par défaut
                });

                // Charger les clients et les statistiques
                await LoadClientsAsync();
                await Task.Delay(100); // Petit délai pour éviter la concurrence
                await LoadStatisticsAsync();
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

                // Utiliser le verrou pour éviter la concurrence DbContext
                await _dbLock.WaitAsync();

                try
                {
                    var request = new ListeClientsRequest
                    {
                        PageNumber = CurrentPage,
                        PageSize = PageSize,
                        SearchTerm = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
                        ClientType = SelectedClientType == "Tous" ? null : SelectedClientType,
                        IsActive = IsActiveFilter
                    };

                    var response = await _listeClientsUseCase.ExecuteAsync(request);

                    if (response.Success)
                    {
                        // Mettre à jour la collection sur le thread UI
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            Clients.Clear();
                            foreach (var client in response.Clients)
                            {
                                Clients.Add(client);
                            }
                        });

                        TotalCount = response.TotalCount;
                        TotalPages = response.TotalPages;
                        HasNextPage = response.HasNextPage;
                        HasPreviousPage = response.HasPreviousPage;

                        // Mettre à jour l'état des commandes de navigation sur le thread UI
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            NextPageCommand.NotifyCanExecuteChanged();
                            PreviousPageCommand.NotifyCanExecuteChanged();
                        });
                    }
                    else
                    {
                        await HandleErrorAsync("Erreur lors du chargement des clients", response.ErrorMessage);
                    }
                }
                finally
                {
                    _dbLock.Release();
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
                // Utiliser le même verrou pour éviter la concurrence
                await _dbLock.WaitAsync();

                try
                {
                    var response = await _listeClientsUseCase.GetStatisticsAsync();
                    if (response.Success)
                    {
                        Statistics = response.Statistics;
                    }
                }
                finally
                {
                    _dbLock.Release();
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
                ErrorMessage = string.Empty;

                // Utiliser le verrou une seule fois pour toute l'opération d'actualisation
                await _dbLock.WaitAsync();

                try
                {
                    // Charger les statistiques et les clients en parallèle pour améliorer les performances
                    var statisticsTask = _listeClientsUseCase.GetStatisticsAsync();
                    
                    var clientsRequest = new ListeClientsRequest
                    {
                        PageNumber = CurrentPage,
                        PageSize = PageSize,
                        SearchTerm = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
                        ClientType = SelectedClientType == "Tous" ? null : SelectedClientType,
                        IsActive = IsActiveFilter
                    };
                    var clientsTask = _listeClientsUseCase.ExecuteAsync(clientsRequest);

                    // Attendre les deux opérations en parallèle
                    await Task.WhenAll(statisticsTask, clientsTask);

                    var statisticsResponse = await statisticsTask;
                    var clientsResponse = await clientsTask;

                    // Traitement des résultats
                    if (statisticsResponse.Success && clientsResponse.Success)
                    {
                        // Mettre à jour les statistiques
                        Statistics = statisticsResponse.Statistics;

                        // Mettre à jour la collection sur le thread UI
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Clients.Clear();
                            foreach (var client in clientsResponse.Clients)
                            {
                                Clients.Add(client);
                            }
                        });

                        // Mettre à jour les données de pagination
                        TotalCount = clientsResponse.TotalCount;
                        TotalPages = clientsResponse.TotalPages;
                        HasNextPage = clientsResponse.HasNextPage;
                        HasPreviousPage = clientsResponse.HasPreviousPage;

                        // Mettre à jour l'état des commandes de navigation
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            NextPageCommand.NotifyCanExecuteChanged();
                            PreviousPageCommand.NotifyCanExecuteChanged();
                        });

                        // Log de succès  
                        await _loggingService.LogInfoAsync(
                            $"Actualisation réussie: {clientsResponse.Clients.Count()} clients chargés, statistiques mises à jour", 
                            "GestionClients");
                    }
                    else
                    {
                        var errors = new List<string>();
                        if (!statisticsResponse.Success) 
                            errors.Add($"Statistiques: {statisticsResponse.ErrorMessage}");
                        if (!clientsResponse.Success) 
                            errors.Add($"Clients: {clientsResponse.ErrorMessage}");
                            
                        await HandleErrorAsync("Erreur lors de l'actualisation", string.Join("; ", errors));
                    }
                }
                finally
                {
                    _dbLock.Release();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Erreur critique lors de l'actualisation", ex);
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

        /// <summary>
        /// Recherche avec debounce pour éviter trop d'appels à la base de données
        /// </summary>
        private async Task PerformDebouncedSearchAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Attendre le délai de debounce
                await Task.Delay(SearchDebounceDelayMs, cancellationToken);
                
                // Si la tâche n'a pas été annulée, effectuer la recherche
                if (!cancellationToken.IsCancellationRequested)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        CurrentPage = 1; // Retourner à la première page lors d'une recherche
                        await LoadClientsAsync();
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // L'opération a été annulée, c'est normal
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Erreur lors de la recherche automatique", ex);
            }
        }

        /// <summary>
        /// Réinitialise tous les filtres et recharge la liste
        /// </summary>
        private async Task ClearFiltersAsync()
        {
            // Annuler toute recherche en cours
            _searchCancellationTokenSource?.Cancel();
            
            SearchTerm = string.Empty;
            SelectedClientType = null;
            IsActiveFilter = true;
            CurrentPage = 1;
            
            await LoadClientsAsync();
        }

        private void ClearFilters()
        {
            SearchTerm = string.Empty;
            SelectedClientType = null;
            IsActiveFilter = true;
            CurrentPage = 1;
            
            LoadClientsCommand.ExecuteAsync(null);
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
            if (IsLoading) return; // Éviter les rechargements inutiles
            
            CurrentPage = 1; // Retourner à la première page quand on change la taille
            OnPropertyChanged(nameof(PaginationInfo));
            LoadClientsCommand.ExecuteAsync(null);
        }

        partial void OnSelectedClientTypeChanged(string? value)
        {
            if (IsLoading) return; // Éviter les rechargements inutiles
            
            CurrentPage = 1;
            OnPropertyChanged(nameof(ActiveFiltersInfo));
            LoadClientsCommand.ExecuteAsync(null);
        }

        partial void OnSelectedStatusOptionChanged(StatusOption? value)
        {
            if (value != null)
            {
                IsActiveFilter = value.Value;
            }
        }

        partial void OnIsActiveFilterChanged(bool? value)
        {
            CurrentPage = 1;
            OnPropertyChanged(nameof(ActiveFiltersInfo));
            LoadClientsCommand.ExecuteAsync(null);
        }

        partial void OnSearchTermChanged(string value)
        {
            OnPropertyChanged(nameof(ActiveFiltersInfo));
            
            // Annuler la recherche précédente si elle existe
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();
            
            // Démarrer une nouvelle recherche avec debounce
            _ = Task.Run(async () => await PerformDebouncedSearchAsync(_searchCancellationTokenSource.Token));
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

        /// <summary>
        /// Ouvre la fenêtre d'import exceptionnel
        /// </summary>
        private void ImportExceptionnel()
        {
            try
            {
                _ = _loggingService.LogInfoAsync("Ouverture de l'interface d'import exceptionnel", "GestionClients");

                // Créer et afficher la fenêtre d'import exceptionnel
                var importDialog = new ImportExceptionnelDialog();
                importDialog.Owner = System.Windows.Application.Current.MainWindow;

                var result = importDialog.ShowDialog();

                // Si l'import a été réalisé avec succès, actualiser la liste
                if (result == true)
                {
                    _ = _loggingService.LogInfoAsync("Import exceptionnel terminé, actualisation de la liste", "GestionClients");
                    _ = Task.Run(RefreshAsync);
                    
                    // Afficher un message de succès
                    System.Windows.MessageBox.Show(
                        "Import exceptionnel terminé avec succès !",
                        "Import terminé",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _ = _loggingService.LogErrorAsync($"Erreur lors de l'ouverture de l'interface d'import exceptionnel: {ex.Message}", "GestionClients", ex);
                
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'ouverture de l'interface d'import exceptionnel:\n{ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion

        #region IRecipient Implementation

        /// <summary>
        /// Reçoit les notifications d'import de clients et rafraîchit la liste
        /// </summary>
        public void Receive(ClientsImportedMessage message)
        {
            // Rafraîchir la liste des clients en arrière-plan
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadClientsAsync();
                }
                catch (Exception ex)
                {
                    await _loggingService.LogErrorAsync($"Erreur lors du rafraîchissement après import: {ex.Message}", "GestionClients", ex);
                }
            });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            // Annuler toute recherche en cours
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();
            
            // Désenregistrer du messenger
            WeakReferenceMessenger.Default.Unregister<ClientsImportedMessage>(this);
            
            // Nettoyer les ressources
            _dbLock?.Dispose();
        }

        #endregion
    }

    public class StatusOption
    {
        public string Display { get; set; } = string.Empty;
        public bool? Value { get; set; }
    }
}
