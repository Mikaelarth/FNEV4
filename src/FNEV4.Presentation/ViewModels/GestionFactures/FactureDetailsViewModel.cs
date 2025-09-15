using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Entities;
using Microsoft.Extensions.Logging;

namespace FNEV4.Presentation.ViewModels.GestionFactures
{
    /// <summary>
    /// ViewModel pour l'affichage détaillé d'une facture
    /// </summary>
    public partial class FactureDetailsViewModel : ObservableObject
    {
        private readonly ILogger<FactureDetailsViewModel> _logger;

        #region Propriétés

        [ObservableProperty]
        private FneInvoice _facture = null!;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// Calcule le montant de la TVA (TTC - HT)
        /// </summary>
        public decimal TvaAmount => Facture?.TotalAmountTTC - Facture?.TotalAmountHT ?? 0;

        /// <summary>
        /// Couleur de fond pour la template du client
        /// </summary>
        public string TemplateBackground
        {
            get
            {
                return Facture?.Client?.DefaultTemplate?.ToUpper() switch
                {
                    "B2B" => "#2196F3", // Bleu
                    "B2C" => "#4CAF50", // Vert
                    "B2F" => "#FF9800", // Orange
                    "B2G" => "#9E9E9E", // Gris pour administration
                    _ => "#9E9E9E"      // Gris par défaut
                };
            }
        }

        #endregion

        #region Constructeur

        public FactureDetailsViewModel(ILogger<FactureDetailsViewModel> logger)
        {
            _logger = logger;
        }

        #endregion

        #region Commandes

        [RelayCommand]
        private void Print()
        {
            try
            {
                StatusMessage = "Impression en cours...";
                
                // TODO: Implémenter l'impression réelle
                MessageBox.Show(
                    $"Impression de la facture {Facture.InvoiceNumber}\n\nCette fonctionnalité sera bientôt disponible.",
                    "Impression",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                _logger.LogInformation("Impression demandée pour la facture {InvoiceNumber}", Facture.InvoiceNumber);
                StatusMessage = "Prêt";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'impression de la facture {InvoiceNumber}", Facture.InvoiceNumber);
                StatusMessage = $"Erreur d'impression : {ex.Message}";
                MessageBox.Show(
                    $"Impossible d'imprimer la facture :\n\n{ex.Message}",
                    "Erreur d'impression",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Close()
        {
            // Cette commande sera liée à la fermeture de la fenêtre via le code-behind
            StatusMessage = "Fermeture...";
            _logger.LogInformation("Fermeture de la vue détails pour la facture {InvoiceNumber}", Facture?.InvoiceNumber ?? "Unknown");
            
            // Déclencher un événement pour fermer la fenêtre
            CloseRequested?.Invoke();
        }
        
        /// <summary>
        /// Événement déclenché quand la fermeture est demandée
        /// </summary>
        public event Action? CloseRequested;

        #endregion

        #region Méthodes publiques

        /// <summary>
        /// Initialise le ViewModel avec une facture
        /// </summary>
        /// <param name="facture">La facture à afficher</param>
        public void Initialize(FneInvoice facture)
        {
            if (facture == null)
                throw new ArgumentNullException(nameof(facture));

            Facture = facture;
            StatusMessage = $"Affichage des détails de la facture {facture.InvoiceNumber}";
            
            _logger.LogInformation("Initialisation de la vue détails pour la facture {InvoiceNumber} avec {ItemsCount} articles", 
                facture.InvoiceNumber, facture.Items?.Count ?? 0);
                
            // Debug : Afficher les détails des articles
            if (facture.Items != null && facture.Items.Any())
            {
                foreach (var item in facture.Items)
                {
                    _logger.LogInformation("Article: {ProductCode} - {Description} - Qté: {Quantity} - Prix: {UnitPrice}", 
                        item.ProductCode, item.Description, item.Quantity, item.UnitPrice);
                }
            }
            else
            {
                _logger.LogWarning("Aucun article trouvé pour la facture {InvoiceNumber}", facture.InvoiceNumber);
            }
        }

        #endregion
    }
}