using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using FNEV4.Core.Interfaces.Services.Fne;
using FNEV4.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FNEV4.Presentation.Views.GestionFactures
{
    /// <summary>
    /// Dialog de détails d'une facture FNE
    /// Basé sur la structure de Sage100FactureDetailsDialog
    /// </summary>
    public partial class FactureFneDetailsDialog : Window
    {
        private readonly IServiceProvider? _serviceProvider;
        private readonly ILogger<FactureFneDetailsDialog>? _logger;

        public FactureFneDetailsDialog(IServiceProvider? serviceProvider = null, ILogger<FactureFneDetailsDialog>? logger = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            InitializeComponent();
            
            // S'assurer que la fenêtre reste visible et accessible
            this.Loaded += OnLoaded;
            this.Activated += OnActivated;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // S'assurer que la fenêtre est au premier plan lors du chargement
            this.Activate();
            this.Focus();
        }

        private void OnActivated(object sender, System.EventArgs e)
        {
            // Maintenir le focus quand la fenêtre est activée
            this.Focus();
        }

        private void FermerButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void CertifierButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serviceProvider == null)
            {
                MessageBox.Show("Service provider non disponible", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var viewModel = this.DataContext as FNEV4.Presentation.ViewModels.GestionFactures.FactureFneDetailsViewModel;
                if (viewModel?.Facture == null)
                {
                    MessageBox.Show("Aucune facture sélectionnée pour certification", "Erreur", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Afficher dialog de confirmation
                var result = MessageBox.Show(
                    $"Voulez-vous certifier la facture n° {viewModel.Facture.InvoiceNumber} auprès de l'API FNE ?\n\n" +
                    "Cette action va soumettre la facture à la Direction Générale des Impôts pour certification officielle.",
                    "Confirmation de certification FNE",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Désactiver le bouton pendant le traitement
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    button.Content = "🔄 CERTIFICATION...";
                }

                try
                {
                    // Obtenir les services nécessaires du système FNE existant
                    var fneCertificationService = _serviceProvider.GetRequiredService<IFneCertificationService>();
                    var context = _serviceProvider.GetRequiredService<FNEV4DbContext>();

                    _logger?.LogInformation("Début de la certification de la facture {InvoiceNumber}", viewModel.Facture.InvoiceNumber);

                    // Récupérer la facture complète avec toutes ses relations
                    var factureComplete = await context.FneInvoices
                        .Include(f => f.Client)
                        .Include(f => f.Items)
                        .ThenInclude(i => i.VatType)
                        .FirstOrDefaultAsync(f => f.Id == viewModel.Facture.Id);

                    if (factureComplete == null)
                    {
                        throw new InvalidOperationException("Impossible de récupérer les détails complets de la facture");
                    }

                    // Récupérer la configuration FNE active
                    var activeConfig = await context.FneConfigurations
                        .FirstOrDefaultAsync(c => c.IsActive && !c.IsDeleted);

                    if (activeConfig == null)
                    {
                        MessageBox.Show(
                            "Aucune configuration FNE active trouvée.\n\n" +
                            "Veuillez configurer l'API FNE dans le menu Configuration avant de procéder à la certification.",
                            "Configuration FNE manquante",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Envoyer la demande de certification avec le service existant
                    var certificationResult = await fneCertificationService.CertifyInvoiceAsync(factureComplete, activeConfig);

                    if (certificationResult.IsSuccess)
                    {
                        // Certification réussie - les données sont déjà sauvées par le service
                        // Recharger la facture pour s'assurer que nous avons les dernières données
                        var updatedFacture = await context.FneInvoices
                            .Include(f => f.Client)
                            .Include(f => f.Items)
                            .FirstOrDefaultAsync(f => f.Id == factureComplete.Id);
                        
                        if (updatedFacture != null)
                        {
                            viewModel.Facture = updatedFacture;
                        }

                        _logger?.LogInformation("Certification réussie pour la facture {InvoiceNumber} - Référence: {FneReference}", 
                            factureComplete.InvoiceNumber, certificationResult.FneReference);

                        // Afficher le succès avec détails
                        var successMessage = $"✅ Certification FNE réussie !\n\n" +
                                           $"Numéro de facture: {factureComplete.InvoiceNumber}\n" +
                                           $"Référence FNE: {certificationResult.FneReference}\n" +
                                           $"Date de traitement: {certificationResult.ProcessedAt:dd/MM/yyyy HH:mm}\n";

                        if (!string.IsNullOrEmpty(certificationResult.VerificationToken))
                        {
                            successMessage += $"Token de vérification: {certificationResult.VerificationToken}\n";
                        }

                        if (certificationResult.HasWarning && !string.IsNullOrEmpty(certificationResult.WarningMessage))
                        {
                            successMessage += $"\n⚠️ Avertissement: {certificationResult.WarningMessage}";
                        }

                        MessageBox.Show(successMessage, "Certification FNE réussie", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Erreur de certification
                        var errorMessage = $"❌ Échec de la certification FNE\n\n" +
                                         $"Message d'erreur: {certificationResult.ErrorMessage}";
                        
                        if (!string.IsNullOrEmpty(certificationResult.ErrorCode))
                        {
                            errorMessage += $"\nCode d'erreur: {certificationResult.ErrorCode}";
                        }

                        if (certificationResult.Errors?.Any() == true)
                        {
                            errorMessage += $"\nDétails:\n• {string.Join("\n• ", certificationResult.Errors)}";
                        }

                        _logger?.LogWarning("Échec de la certification pour la facture {InvoiceNumber}: {Error}", 
                            factureComplete.InvoiceNumber, certificationResult.ErrorMessage);

                        MessageBox.Show(errorMessage, "Erreur de certification FNE", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                finally
                {
                    // Réactiver le bouton
                    if (button != null)
                    {
                        button.IsEnabled = true;
                        button.Content = "📋 CERTIFIER";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur lors de la certification de la facture");
                
                MessageBox.Show(
                    $"❌ Erreur inattendue lors de la certification:\n\n{ex.Message}\n\n" +
                    "Vérifiez votre connexion Internet et la configuration de l'API FNE.",
                    "Erreur de certification",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Réactiver le bouton en cas d'erreur
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "📋 CERTIFIER";
                }
            }
        }

        private void ImprimerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Créer le dialogue d'impression
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Créer une version simplifiée pour l'impression
                    var printableContent = CreateSimplePrintableContent();
                    
                    // Imprimer le contenu
                    printDialog.PrintVisual(printableContent, "Facture FNE - Détails");
                    
                    MessageBox.Show("Impression lancée avec succès!", "Impression", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression: {ex.Message}", "Erreur d'impression", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FrameworkElement CreateSimplePrintableContent()
        {
            var viewModel = this.DataContext as FNEV4.Presentation.ViewModels.GestionFactures.FactureFneDetailsViewModel;
            if (viewModel?.Facture == null)
            {
                return new TextBlock 
                { 
                    Text = "Aucune donnée à imprimer", 
                    FontSize = 14, 
                    Margin = new Thickness(20) 
                };
            }

            // Créer un document simple pour l'impression
            var printGrid = new Grid
            {
                Background = Brushes.White,
                Margin = new Thickness(20)
            };
            
            printGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Titre
            printGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Infos facture
            printGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Headers articles
            printGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Articles
            
            // Titre de la facture
            var titre = new TextBlock
            {
                Text = $"FACTURE FNE N° {viewModel.Facture.InvoiceNumber}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(titre, 0);
            printGrid.Children.Add(titre);
            
            // Informations de la facture
            var infosStack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 20) };
            infosStack.Children.Add(new TextBlock { Text = $"Client: {viewModel.Facture.ClientDisplayName} ({viewModel.Facture.ClientCode})", FontSize = 12, Margin = new Thickness(0, 2, 0, 2) });
            infosStack.Children.Add(new TextBlock { Text = $"Date: {viewModel.Facture.InvoiceDate:dd/MM/yyyy}", FontSize = 12, Margin = new Thickness(0, 2, 0, 2) });
            infosStack.Children.Add(new TextBlock { Text = $"Montant HT: {viewModel.Facture.TotalAmountHT:N2} FCFA", FontSize = 12, Margin = new Thickness(0, 2, 0, 2) });
            infosStack.Children.Add(new TextBlock { Text = $"Montant TTC: {viewModel.Facture.TotalAmountTTC:N2} FCFA", FontSize = 12, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 2, 0, 2) });
            Grid.SetRow(infosStack, 1);
            printGrid.Children.Add(infosStack);
            
            // En-tête du tableau des articles
            var headerGrid = new Grid { Margin = new Thickness(0, 10, 0, 5) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // Code
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Désignation
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) }); // Qté
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // Prix Unit.
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) }); // TVA
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // Montant HT
            
            var headers = new[] { "Code Article", "Désignation", "Qté", "Prix Unit.", "TVA", "Montant HT" };
            for (int i = 0; i < headers.Length; i++)
            {
                var headerText = new TextBlock 
                { 
                    Text = headers[i], 
                    FontWeight = FontWeights.Bold, 
                    FontSize = 11,
                    Padding = new Thickness(5, 3, 5, 3),
                    Background = Brushes.LightGray
                };
                Grid.SetColumn(headerText, i);
                headerGrid.Children.Add(headerText);
            }
            Grid.SetRow(headerGrid, 2);
            printGrid.Children.Add(headerGrid);
            
            // Liste des articles
            var articlesStack = new StackPanel { Orientation = Orientation.Vertical };
            
            if (viewModel.Articles?.Any() == true)
            {
                foreach (var article in viewModel.Articles)
                {
                    var articleGrid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                    articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                    articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                    articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                    articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                    articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                    
                    var cells = new[]
                    {
                        article.ProductCode ?? "",
                        article.Description ?? "",
                        article.Quantity.ToString("N2"),
                        article.UnitPrice.ToString("N2"),
                        article.VatCode ?? "",
                        article.LineAmountHT.ToString("N2")
                    };
                    
                    for (int i = 0; i < cells.Length; i++)
                    {
                        var cellText = new TextBlock 
                        { 
                            Text = cells[i], 
                            FontSize = 10,
                            Padding = new Thickness(5, 2, 5, 2),
                            TextAlignment = i >= 2 ? TextAlignment.Right : TextAlignment.Left
                        };
                        Grid.SetColumn(cellText, i);
                        articleGrid.Children.Add(cellText);
                    }
                    
                    articlesStack.Children.Add(articleGrid);
                }
            }
            else
            {
                articlesStack.Children.Add(new TextBlock { Text = "Aucun article trouvé", FontSize = 12, Margin = new Thickness(5, 5, 5, 5) });
            }
            
            var scrollViewer = new ScrollViewer 
            { 
                Content = articlesStack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden
            };
            Grid.SetRow(scrollViewer, 3);
            printGrid.Children.Add(scrollViewer);
            
            return printGrid;
        }
    }
}