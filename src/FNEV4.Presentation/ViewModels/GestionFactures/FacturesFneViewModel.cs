using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using FNEV4.Core.Interfaces.Services.Fne;
using Microsoft.Extensions.DependencyInjection;
using FNEV4.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FNEV4.Presentation.ViewModels.GestionFactures
{
    /// <summary>
    /// ViewModel pour le sous-menu Factures FNE
    /// Gère la liste simple des factures FNE importées
    /// </summary>
    public partial class FacturesFneViewModel : ObservableObject
    {
        #region Services
        
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IDatabaseService _databaseService;
        private readonly IFneCertificationService _certificationService;
        
        // Timer pour optimiser la recherche (debouncing)
        private readonly DispatcherTimer _searchTimer;
        
        #endregion

        #region Properties

        [ObservableProperty]
        private ObservableCollection<FneInvoice> factures = new();
        
        // Collection complète pour le filtrage local
        private List<FneInvoice> _allFactures = new();

        [ObservableProperty]
        private FneInvoice? selectedFacture;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string selectedStatus = "Tous";

        [ObservableProperty]
        private string statusMessage = "Prêt";

        [ObservableProperty]
        private int totalFactures;

        [ObservableProperty]
        private int facturesDraft;

        [ObservableProperty]
        private int facturesCertifiees;

        [ObservableProperty]
        private bool isCertifying;

        [ObservableProperty]
        private string certificationStatus = string.Empty;

        // Options de filtrage par statut (simplifiées selon les statuts réellement utilisés)
        public List<string> StatusOptions { get; } = new()
        {
            "Tous",
            "Draft", 
            "Certified"
        };

        // Propriétés pour les actions de masse
        [ObservableProperty]
        private ObservableCollection<FneInvoice> selectedFactures = new();

        [ObservableProperty]
        private bool hasMultipleSelection;

        [ObservableProperty]
        private int selectedCount;

        #endregion

        #region Property Change Handlers
        
        /// <summary>
        /// Applique les filtres quand le texte de recherche change (avec délai pour optimiser)
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            // Arrêter le timer précédent
            _searchTimer.Stop();
            
            // Si le texte est vide, filtrer immédiatement
            if (string.IsNullOrWhiteSpace(value))
            {
                ApplyLocalFilters();
            }
            else
            {
                // Sinon, attendre 300ms avant de filtrer (debouncing)
                _searchTimer.Start();
            }
        }
        
        /// <summary>
        /// Applique les filtres quand le statut sélectionné change
        /// </summary>
        partial void OnSelectedStatusChanged(string value)
        {
            ApplyLocalFilters();
        }

        #endregion

        #region Constructor

        public FacturesFneViewModel(IInvoiceRepository invoiceRepository, IDatabaseService databaseService, IFneCertificationService certificationService)
        {
            _invoiceRepository = invoiceRepository;
            _databaseService = databaseService;
            _certificationService = certificationService;
            
            // Initialiser le timer de recherche avec un délai de 300ms
            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                ApplyLocalFilters();
            };
            
            // Charger les factures au démarrage
            _ = LoadFacturesAsync();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Commande pour charger les factures
        /// </summary>
        [RelayCommand]
        private async Task LoadFacturesAsync()
        {
            IsLoading = true;
            StatusMessage = "Chargement des factures...";

            try
            {
                var allFactures = await _invoiceRepository.GetAllAsync();
                
                // Stocker toutes les factures pour le filtrage local
                _allFactures = allFactures.OrderByDescending(f => f.CreatedAt).ToList();
                
                // Appliquer les filtres
                ApplyLocalFilters();

                await UpdateStatisticsAsync();
                StatusMessage = $"Chargé {_allFactures.Count} factures";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur lors du chargement: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Commande pour actualiser la liste
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadFacturesAsync();
        }

        /// <summary>
        /// Commande pour voir les détails d'une facture
        /// </summary>
        [RelayCommand]
        private async Task ViewFacture(FneInvoice? facture)
        {
            if (facture == null) return;

            try
            {
                StatusMessage = $"Chargement détails facture {facture.InvoiceNumber}...";
                
                // Recharger la facture avec ses relations pour s'assurer que toutes les données sont disponibles
                var factureComplete = await _invoiceRepository.GetByIdWithDetailsAsync(facture.Id.ToString());
                if (factureComplete == null)
                {
                    StatusMessage = "Erreur: facture introuvable";
                    System.Windows.MessageBox.Show(
                        "Impossible de charger les détails de cette facture.",
                        "Erreur", 
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return;
                }
                
                StatusMessage = $"Ouverture détails facture {factureComplete.InvoiceNumber}";
                
                // Créer le ViewModel pour le dialogue avec la facture complètement chargée
                var dialogViewModel = new FactureFneDetailsViewModel(factureComplete, _databaseService);
                
                // Trouver la fenêtre principale actuelle
                var mainWindow = System.Windows.Application.Current.MainWindow;
                
                // Créer et afficher le dialogue avec le service provider
                var dialog = new Views.GestionFactures.FactureFneDetailsDialog(
                    serviceProvider: App.ServiceProvider, 
                    logger: App.ServiceProvider?.GetService<Microsoft.Extensions.Logging.ILogger<Views.GestionFactures.FactureFneDetailsDialog>>())
                {
                    DataContext = dialogViewModel,
                    Owner = mainWindow,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
                };

                // S'abonner à l'événement de certification pour mettre à jour la liste
                dialog.InvoiceCertified += OnInvoiceCertifiedInDialog;
                
                dialog.ShowDialog();

                // Se désabonner après fermeture du dialog
                dialog.InvoiceCertified -= OnInvoiceCertifiedInDialog;
                
                StatusMessage = "Prêt";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur ouverture détails: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'ouverture des détails: {ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Commande pour supprimer une facture
        /// </summary>
        [RelayCommand]
        private async Task DeleteFactureAsync(FneInvoice? facture)
        {
            if (facture == null) return;

            try
            {
                // Confirmation suppression (simplifié pour l'instant)
                var result = System.Windows.MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer la facture {facture.InvoiceNumber} ?",
                    "Confirmation suppression",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    await _invoiceRepository.DeleteAsync(facture.Id.ToString());
                    Factures.Remove(facture);
                    await UpdateStatisticsAsync();
                    StatusMessage = $"Facture {facture.InvoiceNumber} supprimée";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur suppression: {ex.Message}";
                System.Windows.MessageBox.Show($"Erreur lors de la suppression: {ex.Message}", "Erreur", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Commande pour appliquer les filtres
        /// </summary>
        [RelayCommand]
        private void ApplyFilters()
        {
            ApplyLocalFilters();
        }

        /// <summary>
        /// Commande pour certifier une facture via l'API FNE
        /// </summary>
        [RelayCommand]
        private async Task CertifyFacture(FneInvoice? facture)
        {
            if (facture == null) return;

            // Vérifier si la facture est déjà certifiée
            if (facture.Status?.ToLower() == "certified")
            {
                StatusMessage = $"La facture {facture.InvoiceNumber} est déjà certifiée";
                return;
            }

            // Dialog de confirmation
            var result = System.Windows.MessageBox.Show(
                $"Êtes-vous sûr de vouloir certifier la facture {facture.InvoiceNumber} ?\n\n" +
                $"Client: {facture.ClientDisplayName}\n" +
                $"Montant: {facture.TotalAmountTTC:C}\n" +
                $"Date: {facture.InvoiceDate:dd/MM/yyyy}\n\n" +
                "Cette action est irréversible.",
                "Confirmation de Certification",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            // Démarrer le processus de certification
            IsCertifying = true;
            CertificationStatus = "Préparation de la certification...";
            StatusMessage = $"Certification de la facture {facture.InvoiceNumber} en cours...";

            try
            {
                CertificationStatus = "Validation des données...";
                
                // Récupérer la configuration FNE active (même logique que CertificationManuelleViewModel)
                var configuration = await GetActiveFneConfigurationAsync();
                if (configuration == null)
                {
                    throw new InvalidOperationException("Aucune configuration FNE active trouvée.\nVeuillez configurer l'API FNE d'abord.");
                }

                CertificationStatus = "Envoi vers l'API FNE...";
                
                // Appeler le service de certification
                var certificationResult = await _certificationService.CertifyInvoiceAsync(facture, configuration);

                if (certificationResult.IsSuccess)
                {
                    // Mise à jour de la facture en cas de succès
                    facture.Status = "certified";
                    facture.FneCertificationDate = DateTime.Now;
                    
                    // Sauvegarder en base de données
                    await _invoiceRepository.UpdateAsync(facture);
                    
                    // Mettre à jour les statistiques
                    await UpdateStatisticsAsync();
                    
                    CertificationStatus = "Certification réussie !";
                    StatusMessage = $"Facture {facture.InvoiceNumber} certifiée avec succès";
                    
                    // Message de succès
                    System.Windows.MessageBox.Show(
                        $"La facture {facture.InvoiceNumber} a été certifiée avec succès !\n\n" +
                        $"Date de certification: {facture.FneCertificationDate:dd/MM/yyyy HH:mm}\n" +
                        $"Temps de traitement: {certificationResult.ProcessingTimeMs}ms",
                        "Certification Réussie",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    // Gestion des erreurs
                    var errorMessage = !string.IsNullOrEmpty(certificationResult.ErrorMessage) 
                        ? certificationResult.ErrorMessage 
                        : "Erreur inconnue lors de la certification";
                    
                    CertificationStatus = "Échec de la certification";
                    StatusMessage = $"Échec certification facture {facture.InvoiceNumber}: {errorMessage}";
                    
                    System.Windows.MessageBox.Show(
                        $"Échec de la certification de la facture {facture.InvoiceNumber}\n\n" +
                        $"Erreur: {errorMessage}\n\n" +
                        "Veuillez vérifier les données de la facture et réessayer.",
                        "Échec de la Certification",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                CertificationStatus = "Erreur lors de la certification";
                StatusMessage = $"Erreur certification: {ex.Message}";
                
                System.Windows.MessageBox.Show(
                    $"Une erreur s'est produite lors de la certification :\n\n{ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsCertifying = false;
                // Effacer le statut après quelques secondes
                await Task.Delay(3000);
                CertificationStatus = string.Empty;
            }
        }

        /// <summary>
        /// Récupère la configuration FNE active pour la certification
        /// </summary>
        private async Task<FneConfiguration?> GetActiveFneConfigurationAsync()
        {
            try
            {
                // Utiliser le même pattern que CertificationManuelleViewModel
                var dbContext = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                    .GetRequiredService<FNEV4.Infrastructure.Data.FNEV4DbContext>(
                        App.ServiceProvider);
                
                var activeConfig = await dbContext.FneConfigurations
                    .Where(c => c.IsActive && !c.IsDeleted)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();

                return activeConfig;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur configuration: {ex.Message}";
                return null;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Applique les filtres localement sur la collection complète (version optimisée)
        /// </summary>
        private void ApplyLocalFilters()
        {
            if (_allFactures == null || !_allFactures.Any())
            {
                Factures.Clear();
                StatusMessage = "Aucune facture chargée";
                return;
            }

            // Utiliser AsParallel pour améliorer les performances sur de grandes collections
            IEnumerable<FneInvoice> filteredFactures = _allFactures;

            // Filtrage par texte de recherche (optimisé)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filteredFactures = filteredFactures.Where(f =>
                    f != null &&
                    (f.InvoiceNumber?.ToLower().Contains(searchLower) == true ||
                     (f.ClientDisplayName ?? "").ToLower().Contains(searchLower) ||
                     (f.ClientCode ?? "").ToLower().Contains(searchLower) ||
                     f.TotalAmountTTC.ToString().Contains(searchLower)));
            }

            // Filtrage par statut (optimisé)
            if (SelectedStatus != "Tous")
            {
                filteredFactures = filteredFactures.Where(f => 
                    f != null && 
                    !string.IsNullOrEmpty(f.Status) &&
                    string.Equals(f.Status, SelectedStatus, StringComparison.OrdinalIgnoreCase));
            }

            // Convertir en liste une seule fois
            var resultList = filteredFactures.Where(f => f != null && !string.IsNullOrEmpty(f.InvoiceNumber)).ToList();
            
            // Mettre à jour la collection sur le thread UI de manière optimisée
            Factures.Clear();
            foreach (var facture in resultList)
            {
                Factures.Add(facture);
            }

            // Message informatif
            StatusMessage = $"Affichage: {Factures.Count} sur {_allFactures.Count} factures";
        }

        /// <summary>
        /// Met à jour les statistiques affichées
        /// </summary>
        private async Task UpdateStatisticsAsync()
        {
            try
            {
                // Calculer sur la collection complète, pas filtrée
                TotalFactures = _allFactures?.Count ?? 0;
                FacturesDraft = _allFactures?.Count(f => f.Status.Equals("draft", StringComparison.OrdinalIgnoreCase)) ?? 0;
                FacturesCertifiees = _allFactures?.Count(f => f.Status.Equals("certified", StringComparison.OrdinalIgnoreCase)) ?? 0;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur statistiques: {ex.Message}";
            }
        }

        /// <summary>
        /// Gestionnaire d'événement appelé quand une facture est certifiée depuis le dialog de détails
        /// </summary>
        private async void OnInvoiceCertifiedInDialog(object sender, Views.GestionFactures.InvoiceCertifiedEventArgs e)
        {
            try
            {
                StatusMessage = $"Mise à jour après certification de la facture {e.InvoiceNumber}...";

                // Recharger les données pour refléter les changements
                await LoadFacturesAsync();

                // Mettre à jour les statistiques
                await UpdateStatisticsAsync();

                StatusMessage = $"Facture {e.InvoiceNumber} certifiée avec succès - Référence FNE: {e.FneReference}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erreur lors de la mise à jour après certification: {ex.Message}";
            }
        }

        #endregion

        #region Actions de masse

        /// <summary>
        /// Certifie plusieurs factures sélectionnées en masse
        /// </summary>
        [RelayCommand]
        private async Task CertifyMultipleAsync()
        {
            if (SelectedFactures == null || SelectedFactures.Count == 0)
            {
                StatusMessage = "Aucune facture sélectionnée pour la certification.";
                return;
            }

            var facturesACertifier = SelectedFactures.Where(f => f.Status.Equals("draft", StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (facturesACertifier.Count == 0)
            {
                StatusMessage = "Aucune facture en statut 'draft' n'est sélectionnée.";
                return;
            }

            IsCertifying = true;
            CertificationStatus = $"Certification de {facturesACertifier.Count} factures en cours...";
            StatusMessage = $"Démarrage de la certification en masse de {facturesACertifier.Count} factures...";

            int successCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

            try
            {
                // Récupérer la configuration FNE active
                var configuration = await GetActiveFneConfigurationAsync();
                if (configuration == null)
                {
                    throw new InvalidOperationException("Aucune configuration FNE active trouvée.\nVeuillez configurer l'API FNE d'abord.");
                }

                // Traiter chaque facture sélectionnée
                for (int i = 0; i < facturesACertifier.Count; i++)
                {
                    var facture = facturesACertifier[i];
                    CertificationStatus = $"Certification {i + 1}/{facturesACertifier.Count}: {facture.InvoiceNumber}";
                    
                    try
                    {
                        var certificationResult = await _certificationService.CertifyInvoiceAsync(facture, configuration);

                        if (certificationResult.IsSuccess)
                        {
                            // Mise à jour de la facture en cas de succès
                            facture.Status = "certified";
                            facture.FneCertificationDate = DateTime.Now;
                            
                            // Sauvegarder en base de données
                            await _invoiceRepository.UpdateAsync(facture);
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"Facture {facture.InvoiceNumber}: {certificationResult.ErrorMessage}");
                            errorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Facture {facture.InvoiceNumber}: {ex.Message}");
                        errorCount++;
                    }
                }

                // Mettre à jour les statistiques
                await UpdateStatisticsAsync();

                // Message de résumé
                if (errorCount == 0)
                {
                    StatusMessage = $"✅ Certification en masse réussie: {successCount} factures certifiées";
                }
                else if (successCount > 0)
                {
                    StatusMessage = $"⚠️ Certification partielle: {successCount} réussies, {errorCount} erreurs";
                }
                else
                {
                    StatusMessage = $"❌ Échec de la certification: {errorCount} erreurs";
                }

                // Afficher les erreurs si nécessaire
                if (errors.Count > 0 && errors.Count <= 5)
                {
                    StatusMessage += $" - {string.Join("; ", errors)}";
                }
                else if (errors.Count > 5)
                {
                    StatusMessage += $" - Voir les logs pour le détail des erreurs";
                }

                // Vider la sélection après l'opération
                SelectedFactures.Clear();
                UpdateMultipleSelection();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur lors de la certification en masse: {ex.Message}";
            }
            finally
            {
                IsCertifying = false;
                CertificationStatus = "";
            }
        }

        /// <summary>
        /// Supprime plusieurs factures sélectionnées en masse
        /// </summary>
        [RelayCommand]
        private async Task DeleteMultipleAsync()
        {
            if (SelectedFactures == null || SelectedFactures.Count == 0)
            {
                StatusMessage = "Aucune facture sélectionnée pour la suppression.";
                return;
            }

            var facturesASupprimer = SelectedFactures.ToList();

            // Confirmation de suppression
            var messageBoxResult = System.Windows.MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer {facturesASupprimer.Count} factures ?\n\nCette action est irréversible.",
                "Confirmation de suppression",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (messageBoxResult != System.Windows.MessageBoxResult.Yes)
            {
                StatusMessage = "Suppression annulée par l'utilisateur.";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Suppression de {facturesASupprimer.Count} factures en cours...";

            int successCount = 0;
            int errorCount = 0;
            var errors = new List<string>();

            try
            {
                // Traiter chaque facture sélectionnée
                foreach (var facture in facturesASupprimer)
                {
                    try
                    {
                        await _invoiceRepository.DeleteAsync(facture.Id.ToString());
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Facture {facture.InvoiceNumber}: {ex.Message}");
                        errorCount++;
                    }
                }

                // Recharger les données après suppression
                await LoadFacturesAsync();

                // Message de résumé
                if (errorCount == 0)
                {
                    StatusMessage = $"✅ Suppression en masse réussie: {successCount} factures supprimées";
                }
                else if (successCount > 0)
                {
                    StatusMessage = $"⚠️ Suppression partielle: {successCount} supprimées, {errorCount} erreurs";
                }
                else
                {
                    StatusMessage = $"❌ Échec de la suppression: {errorCount} erreurs";
                }

                // Afficher les erreurs si nécessaire
                if (errors.Count > 0 && errors.Count <= 3)
                {
                    StatusMessage += $" - {string.Join("; ", errors)}";
                }
                else if (errors.Count > 3)
                {
                    StatusMessage += $" - Voir les logs pour le détail des erreurs";
                }

                // Vider la sélection après l'opération
                SelectedFactures.Clear();
                UpdateMultipleSelection();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur lors de la suppression en masse: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Met à jour les propriétés liées à la sélection multiple
        /// </summary>
        public void UpdateMultipleSelection()
        {
            SelectedCount = SelectedFactures?.Count ?? 0;
            HasMultipleSelection = SelectedCount > 0;
        }

        #endregion
    }
}