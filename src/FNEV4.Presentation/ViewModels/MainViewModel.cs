using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FNEV4.Presentation.ViewModels
{
    /// <summary>
    /// ViewModel principal pour la fenêtre principale
    /// Gère la navigation entre les modules et l'état global
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        #region Events
        
        /// <summary>
        /// Événement déclenché quand tous les sous-menus doivent être fermés
        /// </summary>
        public event Action? CloseSubMenusRequested;
        
        #endregion
        #region Properties

        [ObservableProperty]
        private string _currentModuleName = "Dashboard";

        [ObservableProperty]
        private string _applicationTitle = "FNEV4 - Application FNE Desktop";

        [ObservableProperty]
        private string _applicationVersion = "v1.0.0";

        [ObservableProperty]
        private string _companyName = "Non configuré";

        [ObservableProperty]
        private string _connectionStatus = "Déconnecté";

        [ObservableProperty]
        private string _stickerBalance = "0";

        [ObservableProperty]
        private bool _isMenuExpanded = true;

        [ObservableProperty]
        private bool _areSectionExpanded = false;

        #endregion

        #region Commands

        /// <summary>
        /// Commandes pour naviguer vers le Dashboard
        /// </summary>
        [RelayCommand]
        private void NavigateToDashboard()
        {
            CurrentModuleName = "Dashboard - Vue d'ensemble";
        }

        [RelayCommand]
        private void NavigateToDashboardStatus()
        {
            CurrentModuleName = "Dashboard - Statut du système";
        }

        [RelayCommand]
        private void NavigateToDashboardActions()
        {
            CurrentModuleName = "Dashboard - Actions rapides";
        }

        /// <summary>
        /// Commandes pour naviguer vers Import & Traitement
        /// </summary>
        [RelayCommand]
        private void NavigateToImportFichiers()
        {
            CurrentModuleName = "Import - Import de fichiers";
        }

        [RelayCommand]
        private void NavigateToParsingValidation()
        {
            CurrentModuleName = "Import - Parsing & Validation";
        }

        [RelayCommand]
        private void NavigateToHistoriqueImports()
        {
            CurrentModuleName = "Import - Historique des imports";
        }

        /// <summary>
        /// Commandes pour naviguer vers Gestion des Factures
        /// </summary>
        [RelayCommand]
        private void NavigateToListeFactures()
        {
            CurrentModuleName = "Factures - Liste des factures";
        }

        [RelayCommand]
        private void NavigateToEditionFactures()
        {
            CurrentModuleName = "Factures - Édition de factures";
        }

        [RelayCommand]
        private void NavigateToDetailsFacture()
        {
            CurrentModuleName = "Factures - Détails de facture";
        }

        [RelayCommand]
        private void NavigateToFacturesAvoir()
        {
            CurrentModuleName = "Factures - Factures d'avoir";
        }

        /// <summary>
        /// Commandes pour naviguer vers Certification FNE
        /// </summary>
        [RelayCommand]
        private void NavigateToCertificationManuelle()
        {
            CurrentModuleName = "Certification - Certification manuelle";
        }

        [RelayCommand]
        private void NavigateToCertificationAutomatique()
        {
            CurrentModuleName = "Certification - Certification automatique";
        }

        [RelayCommand]
        private void NavigateToSuiviCertifications()
        {
            CurrentModuleName = "Certification - Suivi des certifications";
        }

        [RelayCommand]
        private void NavigateToRetryReprises()
        {
            CurrentModuleName = "Certification - Retry & Reprises";
        }

        /// <summary>
        /// Commandes pour naviguer vers Gestion Clients
        /// </summary>
        [RelayCommand]
        private void NavigateToListeClients()
        {
            CurrentModuleName = "Clients - Liste des clients";
        }

        [RelayCommand]
        private void NavigateToAjoutModificationClient()
        {
            CurrentModuleName = "Clients - Ajout/Modification";
        }

        [RelayCommand]
        private void NavigateToRechercheAvancee()
        {
            CurrentModuleName = "Clients - Recherche avancée";
        }

        /// <summary>
        /// Commandes pour naviguer vers Rapports & Analyses
        /// </summary>
        [RelayCommand]
        private void NavigateToRapportsStandards()
        {
            CurrentModuleName = "Rapports - Rapports standards";
        }

        [RelayCommand]
        private void NavigateToRapportsFne()
        {
            CurrentModuleName = "Rapports - Rapports FNE";
        }

        [RelayCommand]
        private void NavigateToAnalysesPersonnalisees()
        {
            CurrentModuleName = "Rapports - Analyses personnalisées";
        }

        /// <summary>
        /// Commandes pour naviguer vers Configuration
        /// </summary>
        [RelayCommand]
        private void NavigateToEntrepriseConfig()
        {
            CurrentModuleName = "Configuration - Entreprise";
        }

        [RelayCommand]
        private void NavigateToApiFneConfig()
        {
            CurrentModuleName = "Configuration - API FNE";
        }

        [RelayCommand]
        private void NavigateToCheminsDossiers()
        {
            CurrentModuleName = "Configuration - Chemins & Dossiers";
        }

        [RelayCommand]
        private void NavigateToInterfaceUtilisateur()
        {
            CurrentModuleName = "Configuration - Interface utilisateur";
        }

        [RelayCommand]
        private void NavigateToPerformances()
        {
            CurrentModuleName = "Configuration - Performances";
        }

        /// <summary>
        /// Commandes pour naviguer vers Maintenance
        /// </summary>
        [RelayCommand]
        private void NavigateToLogsDiagnostics()
        {
            CurrentModuleName = "Maintenance - Logs & Diagnostics";
        }

        [RelayCommand]
        private void NavigateToBaseDonnees()
        {
            CurrentModuleName = "Maintenance - Base de données";
        }

        [RelayCommand]
        private void NavigateToSynchronisation()
        {
            CurrentModuleName = "Maintenance - Synchronisation";
        }

        [RelayCommand]
        private void NavigateToOutilsTechniques()
        {
            CurrentModuleName = "Maintenance - Outils techniques";
        }

        /// <summary>
        /// Commandes pour naviguer vers Aide & Support
        /// </summary>
        [RelayCommand]
        private void NavigateToDocumentation()
        {
            CurrentModuleName = "Aide - Documentation";
        }

        [RelayCommand]
        private void NavigateToSupport()
        {
            CurrentModuleName = "Aide - Support";
        }

        [RelayCommand]
        private void NavigateToAPropos()
        {
            CurrentModuleName = "Aide - À propos";
        }

        /// <summary>
        /// Commande pour basculer l'expansion du menu
        /// </summary>
        [RelayCommand]
        private void ToggleMenu()
        {
            IsMenuExpanded = !IsMenuExpanded;
            
            // Quand le menu se replie, fermer tous les sous-menus
            if (!IsMenuExpanded)
            {
                AreSectionExpanded = false;
                CloseSubMenusRequested?.Invoke();
            }
        }

        /// <summary>
        /// Commande pour rafraîchir l'état de connexion
        /// </summary>
        [RelayCommand]
        private async Task RefreshConnectionStatus()
        {
            // TODO: Implémenter la vérification de connexion API FNE
            ConnectionStatus = "Vérification...";
            
            // Simulation d'un appel API
            await Task.Delay(1000);
            
            ConnectionStatus = "Connecté"; // ou "Déconnecté" selon le résultat
            StickerBalance = "1,245"; // Exemple de balance
        }

        #endregion

        #region Constructor

        public MainViewModel()
        {
            // Initialisation par défaut - nécessaire pour le fonctionnement
            CurrentModuleName = "Dashboard - Vue d'ensemble";
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            // TODO: Charger la configuration de l'entreprise
            // TODO: Vérifier l'état de connexion
            // TODO: Charger les données de base
            
            await RefreshConnectionStatus();
        }

        #endregion
    }
}
