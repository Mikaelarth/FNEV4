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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
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

            // Document professionnel pour l'impression - style moderne et élégant
            var mainGrid = new Grid
            {
                Background = Brushes.White,
                Margin = new Thickness(30)
            };
            
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // En-tête société
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) }); // Espacement
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Titre facture
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(15) }); // Espacement
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Infos client et date
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) }); // Espacement
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Tableau articles
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) }); // Espacement
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Totaux
            
            // 1. En-tête société moderne
            var companyHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Bleu moderne
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(25, 15, 25, 15)
            };
            var companyInfo = new StackPanel { Orientation = Orientation.Vertical };
            companyInfo.Children.Add(new TextBlock 
            { 
                Text = "SOCIÉTÉ FNE", 
                FontSize = 20, 
                FontWeight = FontWeights.Bold, 
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left
            });
            companyInfo.Children.Add(new TextBlock 
            { 
                Text = "Gestion Comptable & Facturation", 
                FontSize = 12, 
                Foreground = Brushes.White,
                Opacity = 0.9,
                Margin = new Thickness(0, 3, 0, 0)
            });
            companyHeader.Child = companyInfo;
            Grid.SetRow(companyHeader, 0);
            mainGrid.Children.Add(companyHeader);
            
            // 2. Titre de la facture avec style professionnel
            var titleBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(25, 12, 25, 12)
            };
            var titleText = new TextBlock
            {
                Text = $"FACTURE N° {viewModel.Facture.InvoiceNumber}",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            titleBorder.Child = titleText;
            Grid.SetRow(titleBorder, 2);
            mainGrid.Children.Add(titleBorder);
            
            // 3. Informations client et date - disposition moderne
            var clientDateGrid = new Grid { Margin = new Thickness(25, 0, 25, 0) };
            clientDateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            clientDateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Bloc client
            var clientBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(222, 226, 230)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(0, 0, 10, 0)
            };
            var clientStack = new StackPanel { Orientation = Orientation.Vertical };
            clientStack.Children.Add(new TextBlock 
            { 
                Text = "CLIENT", 
                FontSize = 11, 
                FontWeight = FontWeights.Bold, 
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Margin = new Thickness(0, 0, 0, 6)
            });
            clientStack.Children.Add(new TextBlock 
            { 
                Text = viewModel.Facture.ClientDisplayName, 
                FontSize = 14, 
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                Margin = new Thickness(0, 0, 0, 3)
            });
            clientStack.Children.Add(new TextBlock 
            { 
                Text = $"Code: {viewModel.Facture.ClientCode}", 
                FontSize = 11, 
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125))
            });
            clientBorder.Child = clientStack;
            Grid.SetColumn(clientBorder, 0);
            clientDateGrid.Children.Add(clientBorder);
            
            // Bloc date et statut
            var dateBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(222, 226, 230)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(10, 0, 0, 0)
            };
            var dateStack = new StackPanel { Orientation = Orientation.Vertical };
            dateStack.Children.Add(new TextBlock 
            { 
                Text = "INFORMATIONS", 
                FontSize = 11, 
                FontWeight = FontWeights.Bold, 
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Margin = new Thickness(0, 0, 0, 6)
            });
            dateStack.Children.Add(new TextBlock 
            { 
                Text = $"Date: {viewModel.Facture.InvoiceDate:dd/MM/yyyy}", 
                FontSize = 12, 
                Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                Margin = new Thickness(0, 0, 0, 3)
            });
            dateStack.Children.Add(new TextBlock 
            { 
                Text = $"Statut: {viewModel.Facture.Status ?? "Émise"}", 
                FontSize = 12, 
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125))
            });
            dateBorder.Child = dateStack;
            Grid.SetColumn(dateBorder, 1);
            clientDateGrid.Children.Add(dateBorder);
            
            Grid.SetRow(clientDateGrid, 4);
            mainGrid.Children.Add(clientDateGrid);
            
            // 4. Tableau des articles - style professionnel moderne
            var tableContainer = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(222, 226, 230)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(25, 0, 25, 0),
                Background = Brushes.White
            };
            
            var tableGrid = new Grid();
            tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Articles
            
            // Header du tableau avec style moderne
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                Padding = new Thickness(0, 12, 0, 12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(222, 226, 230)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // Code
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // Désignation
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) }); // Qté
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // Prix Unit.
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) }); // TVA
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // Total
            
            var headers = new[] { "CODE", "DÉSIGNATION", "QTÉ", "PRIX UNIT.", "TVA", "TOTAL TTC" };
            for (int i = 0; i < headers.Length; i++)
            {
                var headerText = new TextBlock 
                { 
                    Text = headers[i], 
                    FontWeight = FontWeights.Bold, 
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(73, 80, 87)),
                    Padding = new Thickness(12, 0, 12, 0),
                    TextAlignment = i >= 2 ? TextAlignment.Right : TextAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(headerText, i);
                headerGrid.Children.Add(headerText);
            }
            
            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            tableGrid.Children.Add(headerBorder);
            
            // Articles avec alternance de couleurs
            var articlesContainer = new StackPanel { Orientation = Orientation.Vertical };
            
            if (viewModel.Articles?.Any() == true)
            {
                bool isEven = true;
                foreach (var article in viewModel.Articles)
                {
                    var articleBorder = new Border
                    {
                        Background = isEven ? 
                            Brushes.White : 
                            new SolidColorBrush(Color.FromRgb(252, 253, 253)),
                        Padding = new Thickness(0, 10, 0, 10),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                        BorderThickness = new Thickness(0, 0, 0, 0.5)
                    };
                    
                    var articleGrid = new Grid();
                    articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                    articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                    articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                    articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                    articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
                    articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                    
                    var cells = new[]
                    {
                        article.ProductCode ?? "",
                        article.Description ?? "",
                        article.Quantity.ToString("N2"),
                        article.UnitPrice.ToString("N0") + " FCFA",
                        $"{article.VatCode}-{article.VatRate:0}%",
                        article.LineAmountTTC.ToString("N0") + " FCFA"
                    };
                    
                    for (int i = 0; i < cells.Length; i++)
                    {
                        var cellText = new TextBlock 
                        { 
                            Text = cells[i], 
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                            Padding = new Thickness(12, 0, 12, 0),
                            TextAlignment = i >= 2 ? TextAlignment.Right : TextAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontWeight = i == 1 ? FontWeights.Medium : FontWeights.Normal
                        };
                        Grid.SetColumn(cellText, i);
                        articleGrid.Children.Add(cellText);
                    }
                    
                    articleBorder.Child = articleGrid;
                    articlesContainer.Children.Add(articleBorder);
                    isEven = !isEven;
                }
            }
            else
            {
                var emptyBorder = new Border
                {
                    Padding = new Thickness(20),
                    Background = new SolidColorBrush(Color.FromRgb(248, 249, 250))
                };
                emptyBorder.Child = new TextBlock 
                { 
                    Text = "Aucun article", 
                    FontSize = 12, 
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125))
                };
                articlesContainer.Children.Add(emptyBorder);
            }
            
            Grid.SetRow(articlesContainer, 1);
            tableGrid.Children.Add(articlesContainer);
            
            tableContainer.Child = tableGrid;
            Grid.SetRow(tableContainer, 6);
            mainGrid.Children.Add(tableContainer);
            
            // 5. Section totaux - style moderne et élégant
            var totalsContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(25, 0, 25, 0),
                Padding = new Thickness(25, 20, 25, 20)
            };
            
            var totalsGrid = new Grid();
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            
            var totalsStack = new StackPanel 
            { 
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            totalsStack.Children.Add(CreateTotalLine("Sous-total HT:", $"{viewModel.Facture.TotalAmountHT:N0} FCFA", 12, false));
            totalsStack.Children.Add(CreateTotalLine("TVA:", $"{viewModel.Facture.TotalVatAmount:N0} FCFA", 12, false));
            totalsStack.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), Margin = new Thickness(0, 8, 0, 8) });
            totalsStack.Children.Add(CreateTotalLine("TOTAL TTC:", $"{viewModel.Facture.TotalAmountTTC:N0} FCFA", 16, true));
            
            Grid.SetColumn(totalsStack, 1);
            totalsGrid.Children.Add(totalsStack);
            
            totalsContainer.Child = totalsGrid;
            Grid.SetRow(totalsContainer, 8);
            mainGrid.Children.Add(totalsContainer);
            
            return mainGrid;
        }
        
        private FrameworkElement CreateTotalLine(string label, string amount, int fontSize, bool isBold)
        {
            var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var labelText = new TextBlock
            {
                Text = label,
                FontSize = fontSize,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Medium,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 15, 0)
            };
            Grid.SetColumn(labelText, 0);
            grid.Children.Add(labelText);
            
            var amountText = new TextBlock
            {
                Text = amount,
                FontSize = fontSize,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Medium,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(amountText, 1);
            grid.Children.Add(amountText);
            
            return grid;
        }
    }
}