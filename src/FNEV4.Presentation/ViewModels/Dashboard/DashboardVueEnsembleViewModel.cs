using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using Microsoft.Extensions.Logging;
using InfraLogging = FNEV4.Infrastructure.Services.ILoggingService;
using InfraDatabase = FNEV4.Infrastructure.Services.IDatabaseService;

namespace FNEV4.Presentation.ViewModels.Dashboard
{
    /// <summary>
    /// ViewModel pour la vue d'ensemble du Dashboard
    /// Affiche les KPIs, graphiques et alertes selon la charte existante
    /// </summary>
    public partial class DashboardVueEnsembleViewModel : ObservableObject
    {
        #region Fields

        private readonly IClientRepository _clientRepository;
        private readonly InfraLogging _loggingService;
        private readonly InfraDatabase _databaseService;
        private readonly ILogger<DashboardVueEnsembleViewModel> _logger;
        private readonly DispatcherTimer _refreshTimer;

        #endregion

        #region Properties - KPIs Généraux

        [ObservableProperty]
        private int _totalClients;

        [ObservableProperty]
        private int _totalFactures;

        [ObservableProperty]
        private decimal _montantTotalFactures;

        [ObservableProperty]
        private int _facturesCertifiees;

        [ObservableProperty]
        private int _facturesEnAttente;

        [ObservableProperty]
        private decimal _pourcentageCertification;

        [ObservableProperty]
        private string _derniereMiseAJour = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        #endregion

        #region Properties - Système

        [ObservableProperty]
        private string _statutConnexionApi = "Non connecté";

        [ObservableProperty]
        private string _statutBaseDonnees = "Opérationnelle";

        [ObservableProperty]
        private string _soldeVignettes = "Non configuré";

        [ObservableProperty]
        private int _nombreLogs;

        [ObservableProperty]
        private string _espaceDisqueUtilise = "0 MB";

        #endregion

        #region Properties - Graphiques et Listes

        [ObservableProperty]
        private ObservableCollection<KpiCard> _kpiCards = new();

        [ObservableProperty]
        private ObservableCollection<ClientsRecentItem> _clientsRecents = new();

        [ObservableProperty]
        private ObservableCollection<AlerteItem> _alertes = new();

        [ObservableProperty]
        private ObservableCollection<StatistiquesMensuelle> _statistiquesMensuelles = new();

        #endregion

        #region Properties - États d'interface

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private string _statusMessage = "Chargement des données...";

        [ObservableProperty]
        private bool _hasErrors = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        #endregion

        #region Constructor

        public DashboardVueEnsembleViewModel(
            IClientRepository clientRepository,
            InfraLogging loggingService,
            InfraDatabase databaseService,
            ILogger<DashboardVueEnsembleViewModel> logger)
        {
            _clientRepository = clientRepository;
            _loggingService = loggingService;
            _databaseService = databaseService;
            _logger = logger;

            // Configuration du timer de rafraîchissement automatique
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5) // Rafraîchir toutes les 5 minutes
            };
            _refreshTimer.Tick += OnRefreshTimer;

            // Initialiser les données sur le thread UI
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
            {
                await InitializeAsync();
            }));
        }

        #endregion

        #region Commands

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            IsLoading = true;
            StatusMessage = "Rafraîchissement des données...";
            HasErrors = false;

            try
            {
                await LoadKpisAsync();
                await LoadClientsRecentsAsync();
                await LoadAlertesAsync();
                await LoadStatistiquesSystemeAsync();
                // Les statistiques mensuelles seront chargées quand le module de facturation sera implémenté

                StatusMessage = "Données mises à jour avec succès";
                DerniereMiseAJour = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                
                // Mettre à jour l'état global basé sur les données chargées
                UpdateGlobalStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du rafraîchissement des données du dashboard");
                HasErrors = true;
                ErrorMessage = "Erreur lors du chargement des données";
                StatusMessage = "Erreur de chargement";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ToggleAutoRefresh()
        {
            if (_refreshTimer.IsEnabled)
            {
                _refreshTimer.Stop();
                StatusMessage = "Rafraîchissement automatique désactivé";
            }
            else
            {
                _refreshTimer.Start();
                StatusMessage = "Rafraîchissement automatique activé";
            }
        }

        [RelayCommand]
        private async Task ExportKpisAsync()
        {
            // TODO: Implémenter l'export des KPIs vers Excel
            StatusMessage = "Export des KPIs en cours...";
            await Task.Delay(1000); // Simulation
            StatusMessage = "Export terminé avec succès";
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Initialisation du dashboard...";
                
                await RefreshDataAsync();
                _refreshTimer.Start();
                
                StatusMessage = "Dashboard initialisé avec succès";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation du dashboard");
                HasErrors = true;
                ErrorMessage = $"Erreur d'initialisation: {ex.Message}";
                StatusMessage = "Erreur lors de l'initialisation";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadKpisAsync()
        {
            // Charger UNIQUEMENT les données clients réelles
            var clientsData = await _clientRepository.GetAllAsync();
            TotalClients = clientsData.TotalCount;

            // TODO: Ces données seront disponibles quand les modules correspondants seront implémentés
            // En attendant, on affiche 0 ou des valeurs par défaut claires
            TotalFactures = 0; // Pas encore de module factures implémenté
            MontantTotalFactures = 0m; // Pas encore de module factures implémenté
            FacturesCertifiees = 0; // Pas encore de module certification implémenté
            FacturesEnAttente = 0; // Pas encore de module certification implémenté
            PourcentageCertification = 0; // Calculé à partir des vraies données quand elles existeront

            // Créer les cartes KPI avec VRAIES données uniquement sur le thread UI
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                KpiCards.Clear();
                KpiCards.Add(new KpiCard
                {
                    Titre = "Clients actifs",
                    Valeur = TotalClients.ToString(),
                    Icone = "AccountGroup",
                    Couleur = "#2E7D32",
                    Evolution = TotalClients > 0 ? $"{TotalClients} clients enregistrés" : "Aucun client",
                    EstPositive = TotalClients > 0
                });

                KpiCards.Add(new KpiCard
                {
                    Titre = "Factures totales",
                    Valeur = "N/A",
                    Icone = "FileDocument",
                    Couleur = "#757575",
                    Evolution = "Module en cours d'implémentation",
                    EstPositive = false
                });

                KpiCards.Add(new KpiCard
                {
                    Titre = "Montant total",
                    Valeur = "N/A",
                    Icone = "CurrencyFra",
                    Couleur = "#757575",
                    Evolution = "Module en cours d'implémentation",
                    EstPositive = false
                });

                KpiCards.Add(new KpiCard
                {
                    Titre = "Certification",
                    Valeur = "N/A",
                    Icone = "CheckCircle",
                    Couleur = "#757575",
                    Evolution = "Module en cours d'implémentation",
                    EstPositive = false
                });
            });
        }

        private async Task LoadClientsRecentsAsync()
        {
            var clientsData = await _clientRepository.GetAllAsync();
            var clientsRecents = clientsData.Clients
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToList();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ClientsRecents.Clear();
                foreach (var client in clientsRecents)
                {
                    ClientsRecents.Add(new ClientsRecentItem
                    {
                        NomComplet = $"{client.Name}",
                        TypeClient = client.ClientType,
                        DateCreation = client.CreatedAt,
                        EstActif = client.IsActive
                    });
                }
            });
        }

        private async Task LoadAlertesAsync()
        {
            try
            {
                // Vérifier les VRAIES erreurs dans les logs des dernières 24h
                var logs = await _loggingService.GetLogsAsync(FNEV4.Core.Entities.LogLevel.Error, DateTime.Now.AddDays(-1), 100);
                var logsErreur = logs.Count();

                var alertesToAdd = new List<AlerteItem>();

                if (logsErreur > 0)
                {
                    alertesToAdd.Add(new AlerteItem
                    {
                        Type = TypeAlerte.Erreur,
                        Message = $"{logsErreur} erreur(s) détectée(s) dans les logs des dernières 24h",
                        DateCreation = DateTime.Now,
                        EstCritique = logsErreur > 10
                    });
                }

                // Vérifier les avertissements dans les logs
                var logsWarning = await _loggingService.GetLogsAsync(FNEV4.Core.Entities.LogLevel.Warning, DateTime.Now.AddDays(-1), 100);
                var warningCount = logsWarning.Count();

                if (warningCount > 5) // Seulement si beaucoup d'avertissements
                {
                    alertesToAdd.Add(new AlerteItem
                    {
                        Type = TypeAlerte.Avertissement,
                        Message = $"{warningCount} avertissement(s) dans les logs des dernières 24h",
                        DateCreation = DateTime.Now,
                        EstCritique = false
                    });
                }

                // Vérifier l'état de la base de données
                var dbInfo = await _databaseService.GetDatabaseInfoAsync();
                if (dbInfo.ConnectionStatus != "Connectée")
                {
                    alertesToAdd.Add(new AlerteItem
                    {
                        Type = TypeAlerte.Erreur,
                        Message = "Problème de connexion à la base de données",
                        DateCreation = DateTime.Now,
                        EstCritique = true
                    });
                }

                // Mettre à jour la collection sur le thread UI
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Alertes.Clear();
                    
                    // Ajouter une alerte informative si aucun problème détecté
                    if (alertesToAdd.Count == 0)
                    {
                        Alertes.Add(new AlerteItem
                        {
                            Type = TypeAlerte.Information,
                            Message = "Système opérationnel - Aucune alerte active",
                            DateCreation = DateTime.Now,
                            EstCritique = false
                        });
                    }
                    else
                    {
                        foreach (var alerte in alertesToAdd)
                        {
                            Alertes.Add(alerte);
                        }
                    }
                });

                // PAS D'ALERTES FICTIVES - seulement des vraies basées sur l'état du système
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Alertes.Clear();
                    Alertes.Add(new AlerteItem
                    {
                        Type = TypeAlerte.Erreur,
                        Message = $"Erreur lors de la vérification des alertes: {ex.Message}",
                        DateCreation = DateTime.Now,
                        EstCritique = true
                    });
                });
            }
        }

        private async Task LoadStatistiquesSystemeAsync()
        {
            try
            {
                // Statistiques de la base de données
                var dbInfo = await _databaseService.GetDatabaseInfoAsync();
                EspaceDisqueUtilise = dbInfo.Size ?? "Inconnu";

                // Nombre de logs via le service de logging
                var logs = await _loggingService.GetLogsAsync();
                NombreLogs = logs.Count();

                // Statuts système avec vraies vérifications
                StatutBaseDonnees = dbInfo.ConnectionStatus == "Connectée" ? "🟢 Opérationnelle" : "� Problème détecté";
                
                // Vérification de l'API FNE (simulation réaliste)
                try 
                {
                    // TODO: Remplacer par une vraie vérification d'API quand le module sera implémenté
                    StatutConnexionApi = "Non configurée - Module FNE à implémenter";
                }
                catch
                {
                    StatutConnexionApi = "� Erreur de connexion";
                }
                
                // Solde vignettes - état réaliste
                SoldeVignettes = "Configuration requise - Module FNE à implémenter";
                
                StatusMessage = "Statistiques système chargées";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des statistiques système");
                StatutBaseDonnees = "Erreur";
                StatutConnexionApi = "Erreur";
                SoldeVignettes = "Erreur";
                StatusMessage = "Erreur lors du chargement des statistiques système";
            }
        }

        private void GenerateStatistiquesMensuelles()
        {
            StatistiquesMensuelles.Clear();

            // PAS DE DONNÉES FICTIVES
            // Les statistiques mensuelles seront affichées quand le module de facturation sera implémenté
            // Pour l'instant, nous laissons cette collection vide pour éviter les fausses données

            // TODO: Implémenter le chargement des vraies statistiques depuis la base de données
            // quand le module de facturation sera développé
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _refreshTimer?.Stop();
            if (_refreshTimer != null)
            {
                _refreshTimer.Tick -= OnRefreshTimer;
            }
        }

        private async void OnRefreshTimer(object? sender, EventArgs e)
        {
            await RefreshDataAsync();
        }

        private void UpdateGlobalStatus()
        {
            try
            {
                // Calculer l'état global basé sur les vraies données
                var hasErrors = StatutBaseDonnees.Contains("Erreur") || StatutConnexionApi.Contains("Erreur");
                var hasWarnings = StatutConnexionApi.Contains("Non configurée") || SoldeVignettes.Contains("Configuration");
                var alertesActives = Alertes.Count(a => a.Type == TypeAlerte.Erreur);
                
                if (hasErrors || alertesActives > 0)
                {
                    StatusMessage = $"⚠️ {alertesActives} problème(s) détecté(s) - Vérifiez les alertes";
                }
                else if (hasWarnings)
                {
                    StatusMessage = "🟡 Configuration incomplète - Certains modules nécessitent une configuration";
                }
                else
                {
                    StatusMessage = $"✅ Système opérationnel - {TotalClients} clients, {NombreLogs} logs";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur lors de l'évaluation du statut: {ex.Message}";
            }
        }

        #endregion
    }

    #region Data Models

    public class KpiCard
    {
        public string Titre { get; set; } = string.Empty;
        public string Valeur { get; set; } = string.Empty;
        public string Icone { get; set; } = string.Empty;
        public string Couleur { get; set; } = "#1976D2";
        public string Evolution { get; set; } = string.Empty;
        public bool EstPositive { get; set; } = true;
    }

    public class ClientsRecentItem
    {
        public string NomComplet { get; set; } = string.Empty;
        public string TypeClient { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public bool EstActif { get; set; }
    }

    public class AlerteItem
    {
        public TypeAlerte Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public bool EstCritique { get; set; }
    }

    public class StatistiquesMensuelle
    {
        public string Mois { get; set; } = string.Empty;
        public int NombreFactures { get; set; }
        public decimal MontantTotal { get; set; }
    }

    public enum TypeAlerte
    {
        Information,
        Avertissement,
        Erreur
    }

    #endregion
}
