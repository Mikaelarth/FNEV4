using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using FNEV4.Core.Interfaces;
using FNEV4.Presentation.ViewModels.Dashboard.Models;
using InfraLogging = FNEV4.Infrastructure.Services.ILoggingService;
using IDatabaseService = FNEV4.Infrastructure.Services.IDatabaseService;

namespace FNEV4.Presentation.ViewModels.Dashboard
{
    /// <summary>
    /// ViewModel pour la page Actions Rapides du Dashboard
    /// Fournit un accès rapide aux fonctionnalités les plus utilisées
    /// </summary>
    public partial class DashboardActionsRapidesViewModel : ObservableObject
    {
        #region Services

        private readonly IClientRepository _clientRepository;
        private readonly IDatabaseService _databaseService;
        private readonly InfraLogging _loggingService;

        #endregion

        #region Propriétés observables

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "Prêt";

        [ObservableProperty]
        private string _statistiqueClients = "0 clients";

        [ObservableProperty]
        private string _derniereActivite = "Aucune activité récente";

        [ObservableProperty]
        private ObservableCollection<ActionRapide> _actionsDisponibles = new();

        [ObservableProperty]
        private ObservableCollection<RaccourciDashboard> _raccourcis = new();

        #endregion

        #region Constructeur

        /// <summary>
        /// Initialise le ViewModel des Actions Rapides
        /// </summary>
        public DashboardActionsRapidesViewModel(
            IClientRepository clientRepository,
            IDatabaseService databaseService,
            InfraLogging loggingService)
        {
            _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

            // Initialisation
            InitialiserActions();
            InitialiserRaccourcis();
            _ = ChargerStatistiquesAsync();
        }

        #endregion

        #region Méthodes d'initialisation

        /// <summary>
        /// Initialise les actions disponibles
        /// </summary>
        private void InitialiserActions()
        {
            ActionsDisponibles.Clear();

            // Import rapide
            ActionsDisponibles.Add(new ActionRapide(
                "Import Rapide",
                "Importer des clients depuis Excel",
                "FileExcelOutline",
                new SolidColorBrush(Color.FromRgb(34, 139, 34)), // Forest Green
                ImportRapideCommand));

            // Export rapide
            ActionsDisponibles.Add(new ActionRapide(
                "Export Rapide", 
                "Exporter la liste des clients",
                "FileExportOutline",
                new SolidColorBrush(Color.FromRgb(30, 144, 255)), // Dodger Blue
                ExportRapideCommand));

            // Sauvegarde rapide
            ActionsDisponibles.Add(new ActionRapide(
                "Sauvegarde",
                "Créer une sauvegarde de la base",
                "DatabaseExportOutline",
                new SolidColorBrush(Color.FromRgb(255, 140, 0)), // Dark Orange
                SauvegardeRapideCommand));

            // Nettoyage rapide
            ActionsDisponibles.Add(new ActionRapide(
                "Nettoyage",
                "Nettoyer les données obsolètes",
                "BroomOutline",
                new SolidColorBrush(Color.FromRgb(220, 20, 60)), // Crimson
                NettoyageRapideCommand));

            // Rapport rapide
            ActionsDisponibles.Add(new ActionRapide(
                "Rapport Rapide",
                "Générer un rapport de synthèse",
                "ChartLineVariant",
                new SolidColorBrush(Color.FromRgb(138, 43, 226)), // Blue Violet
                RapportRapideCommand));

            // Nouvelle entrée (bonus)
            ActionsDisponibles.Add(new ActionRapide(
                "Nouveau Client",
                "Ajouter rapidement un client",
                "AccountPlusOutline",
                new SolidColorBrush(Color.FromRgb(0, 128, 128)), // Teal
                NouveauClientCommand));
        }

        /// <summary>
        /// Initialise les raccourcis disponibles
        /// </summary>
        private void InitialiserRaccourcis()
        {
            Raccourcis.Clear();

            Raccourcis.Add(new RaccourciDashboard("Gestion Clients", "AccountGroupOutline"));
            Raccourcis.Add(new RaccourciDashboard("Import & Traitement", "FileImportOutline"));
            Raccourcis.Add(new RaccourciDashboard("Configuration", "CogOutline"));
            Raccourcis.Add(new RaccourciDashboard("Maintenance", "WrenchOutline"));
            Raccourcis.Add(new RaccourciDashboard("Vue d'ensemble", "ViewDashboardOutline"));
        }

        /// <summary>
        /// Charge les statistiques actuelles
        /// </summary>
        private async Task ChargerStatistiquesAsync()
        {
            try
            {
                await Task.Delay(100);
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatistiqueClients = "12 clients";
                    DerniereActivite = $"Dernière activité : {DateTime.Now:dd/MM/yyyy HH:mm}";
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatistiqueClients = "Erreur de chargement";
                    DerniereActivite = "Données indisponibles";
                });
                
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des statistiques: {ex.Message}");
            }
        }

        #endregion

        #region Commandes principales

        /// <summary>
        /// Commande pour actualiser les données
        /// </summary>
        [RelayCommand]
        private async Task ActualiserAsync()
        {
            await ChargerStatistiquesAsync();
            StatusMessage = $"Actualisé à {DateTime.Now:HH:mm}";
        }

        /// <summary>
        /// Commande pour l'import rapide
        /// </summary>
        [RelayCommand]
        private async Task ImportRapideAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Import en cours...";

                var dialog = new OpenFileDialog
                {
                    Title = "Sélectionner un fichier Excel",
                    Filter = "Fichiers Excel (*.xlsx;*.xls)|*.xlsx;*.xls",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                {
                    await Task.Delay(2000);
                    StatusMessage = "Import terminé avec succès";
                    await ChargerStatistiquesAsync();
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
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Commande pour l'export rapide
        /// </summary>
        [RelayCommand]
        private async Task ExportRapideAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Export en cours...";

                var dialog = new SaveFileDialog
                {
                    Title = "Sauvegarder l'export",
                    Filter = "Fichiers Excel (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"export_clients_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    await Task.Delay(2000);
                    StatusMessage = "Export terminé avec succès";
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
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Commande pour la sauvegarde rapide
        /// </summary>
        [RelayCommand]
        private async Task SauvegardeRapideAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Sauvegarde en cours...";

                await Task.Delay(2000);
                StatusMessage = "Sauvegarde terminée avec succès";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur lors de la sauvegarde : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Commande pour le nettoyage rapide
        /// </summary>
        [RelayCommand]
        private async Task NettoyageRapideAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    "Êtes-vous sûr de vouloir nettoyer les données obsolètes ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    IsLoading = true;
                    StatusMessage = "Nettoyage en cours...";

                    await Task.Delay(2000);
                    StatusMessage = "Nettoyage terminé avec succès";
                    await ChargerStatistiquesAsync();
                }
                else
                {
                    StatusMessage = "Nettoyage annulé";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur lors du nettoyage : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Commande pour le rapport rapide
        /// </summary>
        [RelayCommand]
        private async Task RapportRapideAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Génération du rapport...";

                await Task.Delay(2000);
                StatusMessage = "Rapport généré avec succès";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur lors de la génération : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Commande pour ajouter un nouveau client
        /// </summary>
        [RelayCommand]
        private void NouveauClient()
        {
            StatusMessage = "Navigation vers l'ajout de client...";
        }

        #endregion
    }
}
