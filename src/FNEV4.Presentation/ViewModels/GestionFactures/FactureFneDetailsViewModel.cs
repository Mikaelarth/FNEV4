using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Text;
using FNEV4.Infrastructure.Services;
using BarcodeLib;
using FNEV4.Core.Entities;
using WpfMedia = System.Windows.Media;
using DrawingMedia = System.Drawing;
using System.Drawing.Imaging;

namespace FNEV4.Presentation.ViewModels.GestionFactures
{
    /// <summary>
    /// ViewModel pour le dialog de détails d'une facture FNE
    /// Basé sur la structure du Sage100FactureDetailsDialog
    /// Étendu pour exploiter toutes les données enrichies de l'API FNE
    /// </summary>
    public class FactureFneDetailsViewModel : INotifyPropertyChanged
    {
        private FneInvoice _facture;
        private ObservableCollection<FneInvoiceItem> _articles;
        private BitmapImage? _qrCodeImageSource;
        private BitmapImage? _barcodeImageSource;
        private readonly IDatabaseService? _databaseService;
        
        // Informations entreprise chargées depuis la base
        private string _companyName = "FNEV4 SARL";
        private string _companyAddress = "Zone Industrielle de Yopougon";
        private string _companyLocation = "Abidjan, Côte d'Ivoire";
        private string _companyPhone = "Tél: +225 27 23 45 67 89";
        private string _companyEmail = "Email: contact@fnev4.ci";

        public FactureFneDetailsViewModel(FneInvoice facture, IDatabaseService? databaseService = null)
        {
            _facture = facture ?? throw new ArgumentNullException(nameof(facture));
            _articles = new ObservableCollection<FneInvoiceItem>(facture.Items ?? new List<FneInvoiceItem>());
            _databaseService = databaseService;
            
            Debug.WriteLine($"FactureFneDetailsViewModel - DatabaseService injecté: {_databaseService != null}");
            
            InitializeCommands();
            CalculateProperties();
            GenerateQrCodeImage();
            GenerateBarcode();
            
            // Charger les informations d'entreprise depuis la base de données
            LoadCompanyInfoSync();
        }

        public FneInvoice Facture
        {
            get => _facture;
            set
            {
                _facture = value;
                OnPropertyChanged();
                CalculateProperties();
                GenerateQrCodeImage();
            }
        }

        public ObservableCollection<FneInvoiceItem> Articles
        {
            get => _articles;
            set
            {
                _articles = value;
                OnPropertyChanged();
            }
        }

        #region Propriétés enrichies FNE

        /// <summary>
        /// Image source pour l'affichage du QR Code
        /// </summary>
        public BitmapImage? QrCodeImageSource
        {
            get => _qrCodeImageSource;
            set
            {
                _qrCodeImageSource = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Image source pour l'affichage du code-barres de la facture
        /// </summary>
        public BitmapImage? BarcodeImageSource
        {
            get => _barcodeImageSource;
            set
            {
                _barcodeImageSource = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Hash de certification tronqué pour l'affichage
        /// </summary>
        public string CertificationHashShort
        {
            get
            {
                if (string.IsNullOrEmpty(Facture?.FneCertificationHash))
                    return "Non générée";
                
                var hash = Facture.FneCertificationHash;
                if (hash.Length > 16)
                    return $"{hash.Substring(0, 8)}...{hash.Substring(hash.Length - 8)}";
                return hash;
            }
        }

        /// <summary>
        /// Token de vérification public tronqué pour l'affichage
        /// Utilise FnePublicVerificationToken (juste l'ID) plutôt que VerificationToken (URL complète)
        /// </summary>
        public string VerificationTokenShort
        {
            get
            {
                if (!string.IsNullOrEmpty(Facture?.FnePublicVerificationToken))
                {
                    var token = Facture.FnePublicVerificationToken;
                    if (token.Length > 20)
                        return $"{token.Substring(0, 10)}...{token.Substring(token.Length - 10)}";
                    return token;
                }
                else if (!string.IsNullOrEmpty(Facture?.VerificationToken))
                {
                    // Fallback: extraire l'ID de l'URL complète
                    var url = Facture.VerificationToken;
                    var lastSlashIndex = url.LastIndexOf('/');
                    if (lastSlashIndex >= 0 && lastSlashIndex < url.Length - 1)
                    {
                        var tokenId = url.Substring(lastSlashIndex + 1);
                        if (tokenId.Length > 20)
                            return $"{tokenId.Substring(0, 10)}...{tokenId.Substring(tokenId.Length - 10)}";
                        return tokenId;
                    }
                }
                
                return "Non disponible";
            }
        }

        /// <summary>
        /// URL de téléchargement
        /// </summary>
        public string DownloadUrl
        {
            get
            {
                if (string.IsNullOrEmpty(Facture?.FneDownloadUrl))
                    return "Non disponible";
                return Facture.FneDownloadUrl;
            }
        }

        /// <summary>
        /// NCC de l'entreprise
        /// </summary>
        public string CompanyNcc
        {
            get
            {
                if (string.IsNullOrEmpty(Facture?.FneCompanyNcc))
                    return "Non défini";
                return Facture.FneCompanyNcc;
            }
        }

        /// <summary>
        /// Couleur de fond pour le statut de traitement
        /// </summary>
        public WpfMedia.Brush ProcessingStatusBackground
        {
            get
            {
                return Facture?.FneProcessingStatus?.ToLower() switch
                {
                    "success" or "completed" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(76, 175, 80)),   // Vert
                    "processing" or "in-progress" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(255, 152, 0)), // Orange
                    "pending" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(33, 150, 243)),  // Bleu
                    "error" or "failed" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(244, 67, 54)),       // Rouge
                    _ => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(158, 158, 158))      // Gris
                };
            }
        }

        /// <summary>
        /// Données QR Code formatées pour l'affichage avec saut de ligne si nécessaire
        /// </summary>
        public string QrCodeDataFormatted
        {
            get
            {
                if (string.IsNullOrEmpty(Facture?.FneQrCodeData))
                    return "Données QR Code non générées";
                
                // Si les données sont très longues, les formater pour une meilleure lisibilité
                var qrData = Facture.FneQrCodeData;
                if (qrData.Length > 60)
                {
                    // Essayer de diviser sur les délimiteurs logiques (pipe, virgule, etc.)
                    if (qrData.Contains('|'))
                        return qrData.Replace("|", "|\n");
                    if (qrData.Contains(','))
                        return qrData.Replace(",", ",\n");
                    
                    // Sinon, diviser par chunks de 60 caractères
                    var result = new StringBuilder();
                    for (int i = 0; i < qrData.Length; i += 60)
                    {
                        var length = Math.Min(60, qrData.Length - i);
                        result.AppendLine(qrData.Substring(i, length));
                    }
                    return result.ToString().TrimEnd();
                }
                
                return qrData;
            }
        }

        /// <summary>
        /// Données QR Code courtes pour les tooltips et affichages compacts
        /// </summary>
        public string QrCodeDataShort
        {
            get
            {
                if (string.IsNullOrEmpty(Facture?.FneQrCodeData))
                    return "Non généré";
                
                var qrData = Facture.FneQrCodeData;
                if (qrData.Length > 50)
                    return $"{qrData.Substring(0, 25)}...{qrData.Substring(qrData.Length - 20)}";
                
                return qrData;
            }
        }

        #endregion

        #region Propriétés de visibilité et contrôle d'état

        /// <summary>
        /// Indique si la facture est certifiée (pour conditionner l'affichage)
        /// </summary>
        public bool IsCertified => Facture?.Status?.ToLower() == "certified" || Facture?.IsCertified == true;

        /// <summary>
        /// Indique si la facture est un brouillon (peut être certifiée)
        /// </summary>
        public bool IsDraft => Facture?.Status?.ToLower() == "draft";

        /// <summary>
        /// Indique si la facture peut être certifiée (brouillon ou validée mais pas encore certifiée)
        /// </summary>
        public bool CanBeCertified => Facture?.Status?.ToLower() is "draft" or "validated" && !IsCertified;

        /// <summary>
        /// Visibilité des sections de certification FNE (uniquement si certifiée)
        /// </summary>
        public Visibility CertificationSectionVisibility => IsCertified ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Visibilité du bouton de certification (uniquement si peut être certifiée)
        /// </summary>
        public Visibility CertifyButtonVisibility => CanBeCertified ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Message d'état pour les factures non certifiées
        /// </summary>
        public string NonCertifiedMessage
        {
            get
            {
                return Facture?.Status?.ToLower() switch
                {
                    "draft" => "Cette facture est en brouillon. Vous pouvez la certifier via le bouton ci-dessous.",
                    "validated" => "Cette facture est validée mais pas encore certifiée. Cliquez sur 'Certifier' pour obtenir la certification FNE.",
                    "error" => "Une erreur s'est produite lors de la certification. Veuillez réessayer.",
                    _ => "Cette facture n'est pas encore certifiée."
                };
            }
        }

        #endregion

        #region Informations Entreprise

        /// <summary>
        /// Nom de l'entreprise
        /// </summary>
        public string CompanyName
        {
            get => _companyName;
            private set
            {
                _companyName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Adresse de l'entreprise
        /// </summary>
        public string CompanyAddress
        {
            get => _companyAddress;
            private set
            {
                _companyAddress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Ville et pays de l'entreprise
        /// </summary>
        public string CompanyLocation
        {
            get => _companyLocation;
            private set
            {
                _companyLocation = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Téléphone de l'entreprise
        /// </summary>
        public string CompanyPhone
        {
            get => _companyPhone;
            private set
            {
                _companyPhone = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Email de l'entreprise
        /// </summary>
        public string CompanyEmail
        {
            get => _companyEmail;
            private set
            {
                _companyEmail = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commandes

        public ICommand CopyQrDataCommand { get; private set; }
        public ICommand ShowQrDataCommand { get; private set; }
        public ICommand DownloadPdfCommand { get; private set; }
        public ICommand VerifyOnlineCommand { get; private set; }
        public ICommand ShowFullDetailsCommand { get; private set; }
        public ICommand CertifyInvoiceCommand { get; private set; }

        private void InitializeCommands()
        {
            CopyQrDataCommand = new RelayCommand(CopyQrData, CanCopyQrData);
            ShowQrDataCommand = new RelayCommand(ShowQrData, CanCopyQrData);
            DownloadPdfCommand = new RelayCommand(async () => await DownloadPdf(), CanDownloadPdf);
            VerifyOnlineCommand = new RelayCommand(VerifyOnline, CanVerifyOnline);
            ShowFullDetailsCommand = new RelayCommand(ShowFullDetails, CanShowFullDetails);
            CertifyInvoiceCommand = new RelayCommand(async () => await CertifyInvoice(), CanCertifyInvoice);
        }

        private bool CanCopyQrData()
        {
            return !string.IsNullOrEmpty(Facture?.FneQrCodeData);
        }

        private void CopyQrData()
        {
            try
            {
                if (!string.IsNullOrEmpty(Facture?.FneQrCodeData))
                {
                    Clipboard.SetText(Facture.FneQrCodeData);
                    MessageBox.Show("Données QR Code copiées dans le presse-papiers!", "Copie réussie", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la copie: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowQrData()
        {
            try
            {
                if (string.IsNullOrEmpty(Facture?.FneQrCodeData))
                {
                    MessageBox.Show("Aucune donnée QR Code disponible", "Information", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var qrDataWindow = new Window
                {
                    Title = $"Données QR Code - Facture {Facture.InvoiceNumber}",
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ResizeMode = ResizeMode.CanResize
                };

                var grid = new System.Windows.Controls.Grid();
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

                var scrollViewer = new System.Windows.Controls.ScrollViewer
                {
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    Padding = new Thickness(16)
                };

                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = Facture.FneQrCodeData,
                    FontFamily = new WpfMedia.FontFamily("Consolas"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                };

                scrollViewer.Content = textBlock;
                System.Windows.Controls.Grid.SetRow(scrollViewer, 0);
                grid.Children.Add(scrollViewer);

                // Boutons
                var buttonPanel = new System.Windows.Controls.StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(16)
                };

                var copyButton = new System.Windows.Controls.Button
                {
                    Content = "Copier",
                    Padding = new Thickness(16, 8, 16, 8),
                    Margin = new Thickness(0, 0, 8, 0)
                };
                copyButton.Click += (s, e) =>
                {
                    Clipboard.SetText(Facture.FneQrCodeData);
                    MessageBox.Show("Données QR copiées!", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                };

                var closeButton = new System.Windows.Controls.Button
                {
                    Content = "Fermer",
                    Padding = new Thickness(16, 8, 16, 8),
                    IsCancel = true
                };
                closeButton.Click += (s, e) => qrDataWindow.Close();

                buttonPanel.Children.Add(copyButton);
                buttonPanel.Children.Add(closeButton);
                System.Windows.Controls.Grid.SetRow(buttonPanel, 1);
                grid.Children.Add(buttonPanel);

                qrDataWindow.Content = grid;
                qrDataWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'affichage des données QR: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanDownloadPdf()
        {
            return !string.IsNullOrEmpty(Facture?.FneDownloadUrl);
        }

        private async Task DownloadPdf()
        {
            try
            {
                if (string.IsNullOrEmpty(Facture?.FneDownloadUrl))
                {
                    MessageBox.Show("URL de téléchargement non disponible", "Erreur", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Dialog de sauvegarde
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Enregistrer la facture certifiée",
                    FileName = $"Facture_FNE_{Facture.InvoiceNumber}_{DateTime.Now:yyyyMMdd}.pdf",
                    Filter = "Fichiers PDF (*.pdf)|*.pdf|Tous les fichiers (*.*)|*.*",
                    DefaultExt = "pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(Facture.FneDownloadUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(saveFileDialog.FileName, content);
                        
                        var result = MessageBox.Show(
                            $"Facture téléchargée avec succès !\n\nFichier: {saveFileDialog.FileName}\n\nVoulez-vous l'ouvrir maintenant ?",
                            "Téléchargement réussi", 
                            MessageBoxButton.YesNo, 
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Erreur lors du téléchargement: {response.StatusCode}", "Erreur de téléchargement", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du téléchargement: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanVerifyOnline()
        {
            return !string.IsNullOrEmpty(Facture?.VerificationToken);
        }

        private void VerifyOnline()
        {
            try
            {
                // Le VerificationToken contient déjà l'URL complète de vérification DGI
                // Format: "http://54.247.95.108/fr/verification/019465c1-3f61-766c-9652-706e32dfb436"
                if (string.IsNullOrEmpty(Facture?.VerificationToken))
                {
                    MessageBox.Show("Token de vérification non disponible", "Erreur", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var verificationUrl = Facture.VerificationToken;
                
                // Vérifier que c'est bien une URL valide
                if (!Uri.TryCreate(verificationUrl, UriKind.Absolute, out _))
                {
                    MessageBox.Show("L'URL de vérification n'est pas valide", "Erreur", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Process.Start(new ProcessStartInfo(verificationUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir l'URL de vérification: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanShowFullDetails()
        {
            return Facture != null;
        }

        private void ShowFullDetails()
        {
            var details = new StringBuilder();
            details.AppendLine("=== DÉTAILS COMPLETS DE CERTIFICATION FNE ===");
            details.AppendLine($"Facture N°: {Facture?.InvoiceNumber}");
            details.AppendLine($"Client: {Facture?.ClientDisplayName} ({Facture?.ClientCode})");
            details.AppendLine($"Date facture: {Facture?.InvoiceDate:dd/MM/yyyy}");
            details.AppendLine();
            
            details.AppendLine("--- Certification ---");
            details.AppendLine($"Référence FNE: {Facture?.FneReference ?? "Non certifiée"}");
            details.AppendLine($"Date certification: {Facture?.FneCertificationTimestamp?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Non certifiée"}");
            details.AppendLine($"Statut traitement: {Facture?.FneProcessingStatus ?? "Non défini"}");
            details.AppendLine($"Hash certification: {Facture?.FneCertificationHash ?? "Non généré"}");
            details.AppendLine();
            
            details.AppendLine("--- Informations DGI ---");
            details.AppendLine($"Balance Sticker: {Facture?.FneBalanceSticker ?? "Non disponible"}");
            details.AppendLine($"NCC Entreprise: {Facture?.FneCompanyNcc ?? "Non défini"}");
            details.AppendLine($"Token vérification: {Facture?.VerificationToken ?? "Non disponible"}");
            details.AppendLine();
            
            details.AppendLine("--- QR Code ---");
            details.AppendLine($"Données QR: {Facture?.FneQrCodeData ?? "Non généré"}");
            details.AppendLine();
            
            details.AppendLine("--- Téléchargement ---");
            details.AppendLine($"URL PDF: {Facture?.FneDownloadUrl ?? "Non disponible"}");

            var detailsWindow = new Window
            {
                Title = "Détails complets FNE",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var scrollViewer = new System.Windows.Controls.ScrollViewer
            {
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                Padding = new Thickness(16)
            };

            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = details.ToString(),
                FontFamily = new WpfMedia.FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };

            scrollViewer.Content = textBlock;
            detailsWindow.Content = scrollViewer;
            detailsWindow.ShowDialog();
        }

        private bool CanCertifyInvoice()
        {
            return CanBeCertified;
        }

        private async Task CertifyInvoice()
        {
            try
            {
                if (!CanBeCertified)
                {
                    MessageBox.Show("Cette facture ne peut pas être certifiée dans son état actuel.", "Certification impossible", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Voulez-vous certifier la facture {Facture?.InvoiceNumber} ?\n\n" +
                    "Cette action enverra la facture vers la DGI pour certification FNE.",
                    "Confirmation de certification", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // TODO: Implémenter l'appel au service de certification FNE
                // Ici on devrait appeler le service FneCertificationService
                await Task.Delay(100); // Placeholder pour éviter l'erreur async
                
                MessageBox.Show(
                    "Fonctionnalité de certification en cours d'implémentation.\n\n" +
                    "Cette fonction appellera le service FneCertificationService pour certifier la facture auprès de la DGI.",
                    "À implémenter", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la certification: {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Propriétés calculées pour l'affichage

        /// <summary>
        /// Titre principal du dialog
        /// </summary>
        public string FactureTitle => $"Facture FNE N° {Facture?.InvoiceNumber}";
        
        /// <summary>
        /// Sous-titre avec informations client
        /// </summary>
        public string FactureSubtitle => $"Client: {Facture?.ClientDisplayName} ({Facture?.ClientCode})";

        /// <summary>
        /// Texte du statut
        /// </summary>
        public string StatusText
        {
            get
            {
                return Facture?.Status?.ToLower() switch
                {
                    "certified" => "CERTIFIÉE",
                    "draft" => "BROUILLON",
                    "validated" => "VALIDÉE",
                    "error" => "ERREUR",
                    _ => "STATUT INCONNU"
                };
            }
        }

        /// <summary>
        /// Couleur de fond du badge de statut
        /// </summary>
        public WpfMedia.Brush StatusBackground
        {
            get
            {
                return Facture?.Status?.ToLower() switch
                {
                    "certified" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(76, 175, 80)),   // Vert
                    "draft" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(158, 158, 158)),     // Gris
                    "validated" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(33, 150, 243)),  // Bleu
                    "error" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(244, 67, 54)),       // Rouge
                    _ => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(96, 125, 139))             // Gris bleu
                };
            }
        }

        /// <summary>
        /// Couleur de fond du badge template
        /// </summary>
        public WpfMedia.Brush TemplateBackground
        {
            get
            {
                return Facture?.Template switch
                {
                    "B2B" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(33, 150, 243)),  // Bleu
                    "B2C" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(76, 175, 80)),   // Vert
                    "B2G" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(156, 39, 176)),  // Violet
                    "B2F" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(255, 152, 0)),   // Orange
                    _ => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(158, 158, 158))      // Gris
                };
            }
        }

        /// <summary>
        /// Nom d'affichage du template
        /// </summary>
        public string TemplateDisplayName
        {
            get
            {
                return Facture?.Template switch
                {
                    "B2B" => "Business to Business",
                    "B2C" => "Business to Consumer", 
                    "B2G" => "Business to Government",
                    "B2F" => "Business to Foreigner",
                    _ => Facture?.Template ?? "Non défini"
                };
            }
        }

        /// <summary>
        /// Nom d'affichage du moyen de paiement
        /// </summary>
        public string PaymentMethodDisplayName
        {
            get
            {
                return Facture?.PaymentMethod switch
                {
                    "cash" => "Espèces",
                    "card" => "Carte bancaire",
                    "mobile-money" => "Mobile Money",
                    "bank-transfer" => "Virement bancaire",
                    "check" => "Chèque",
                    _ => Facture?.PaymentMethod ?? "Non défini"
                };
            }
        }

        /// <summary>
        /// Nombre total d'articles
        /// </summary>
        public int NombreArticles => Articles?.Count ?? 0;

        /// <summary>
        /// Référence FNE formatée
        /// </summary>
        public string ReferenceFormatee
        {
            get
            {
                if (string.IsNullOrEmpty(Facture?.FneReference))
                    return "Pas encore certifiée";
                return Facture.FneReference;
            }
        }

        /// <summary>
        /// Date de certification formatée
        /// </summary>
        public string DateCertificationFormatee
        {
            get
            {
                if (Facture?.CertifiedAt == null)
                    return "Non certifiée";
                return Facture.CertifiedAt.Value.ToString("dd/MM/yyyy HH:mm");
            }
        }

        /// <summary>
        /// Message commercial tronqué pour l'affichage
        /// </summary>
        public string MessageCommercialCourt
        {
            get
            {
                if (string.IsNullOrEmpty(Facture?.CommercialMessage))
                    return "Aucun message";
                
                if (Facture.CommercialMessage.Length > 50)
                    return Facture.CommercialMessage.Substring(0, 50) + "...";
                
                return Facture.CommercialMessage;
            }
        }

        #endregion

        #region Méthodes utilitaires

        /// <summary>
        /// Obtient la couleur associée au type de TVA selon les spécifications FNE
        /// </summary>
        public static WpfMedia.Brush GetTvaColorBrush(string? codeTva)
        {
            return codeTva?.ToUpper() switch
            {
                "TVA" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(76, 175, 80)),   // Vert - TVA normal 18%
                "TVAB" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(255, 152, 0)),  // Orange - TVA réduit 9%
                "TVAC" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(158, 158, 158)), // Gris - TVA exec conv 0%
                "TVAD" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(96, 125, 139)),  // Gris bleu - TVA exec leg 0%
                null or "" => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(244, 67, 54)), // Rouge - Erreur
                _ => new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(158, 158, 158))       // Gris par défaut
            };
        }

        /// <summary>
        /// Obtient la description du type de TVA
        /// </summary>
        public static string GetTvaDescription(string? codeTva)
        {
            return codeTva?.ToUpper() switch
            {
                "TVA" => "TVA normal de 18%",
                "TVAB" => "TVA réduit de 9%",
                "TVAC" => "TVA exec conv de 0%",
                "TVAD" => "TVA exec leg de 0% pour TEE et RME",
                null or "" => "Code TVA manquant",
                _ => "Type inconnu"
            };
        }

        private void CalculateProperties()
        {
            OnPropertyChanged(nameof(FactureTitle));
            OnPropertyChanged(nameof(FactureSubtitle));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusBackground));
            OnPropertyChanged(nameof(TemplateBackground));
            OnPropertyChanged(nameof(TemplateDisplayName));
            OnPropertyChanged(nameof(PaymentMethodDisplayName));
            OnPropertyChanged(nameof(NombreArticles));
            OnPropertyChanged(nameof(ReferenceFormatee));
            OnPropertyChanged(nameof(DateCertificationFormatee));
            OnPropertyChanged(nameof(MessageCommercialCourt));
            
            // Propriétés enrichies FNE
            OnPropertyChanged(nameof(CertificationHashShort));
            OnPropertyChanged(nameof(VerificationTokenShort));
            OnPropertyChanged(nameof(QrCodeDataFormatted));
            OnPropertyChanged(nameof(QrCodeDataShort));
            OnPropertyChanged(nameof(DownloadUrl));
            OnPropertyChanged(nameof(CompanyNcc));
            OnPropertyChanged(nameof(ProcessingStatusBackground));
            
            // Propriétés de visibilité et état
            OnPropertyChanged(nameof(IsCertified));
            OnPropertyChanged(nameof(IsDraft));
            OnPropertyChanged(nameof(CanBeCertified));
            OnPropertyChanged(nameof(CertificationSectionVisibility));
            OnPropertyChanged(nameof(CertifyButtonVisibility));
            OnPropertyChanged(nameof(NonCertifiedMessage));
        }

        /// <summary>
        /// Génère l'image QR Code à partir des données FNE Base64
        /// </summary>
        private void GenerateQrCodeImage()
        {
            try
            {
                if (string.IsNullOrEmpty(Facture?.FneQrCodeData))
                {
                    QrCodeImageSource = null;
                    return;
                }

                // Convertir les données Base64 en BitmapImage
                var qrData = Facture.FneQrCodeData;
                
                // Si c'est déjà au format data:image/png;base64,xxx, extraire la partie Base64
                if (qrData.StartsWith("data:image/png;base64,"))
                {
                    qrData = qrData.Substring("data:image/png;base64,".Length);
                }

                var imageBytes = Convert.FromBase64String(qrData);
                
                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }
                
                QrCodeImageSource = bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur génération QR-code: {ex.Message}");
                // En cas d'erreur, utiliser le placeholder
                QrCodeImageSource = CreatePlaceholderImage();
            }
        }

        /// <summary>
        /// Crée une image placeholder en attendant l'implémentation du QR Code
        /// </summary>
        private BitmapImage? CreatePlaceholderImage()
        {
            try
            {
                // Créer une image simple avec le texte "QR CODE"
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                // Vous pouvez créer une image programmatiquement ou utiliser une ressource
                // Pour l'instant, on retourne null pour éviter les erreurs
                bitmap.EndInit();
                return null; // Retourner null pour l'instant
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Génère un code-barres pour la facture
        /// </summary>
        private void GenerateBarcode()
        {
            try
            {
                if (_facture?.InvoiceNumber == null)
                {
                    Debug.WriteLine("GenerateBarcode - Numéro de facture non disponible");
                    return;
                }

                var barcodeText = $"FNE-{_facture.InvoiceNumber}";
                Debug.WriteLine($"GenerateBarcode - Génération du code-barres pour: {barcodeText}");

                var barcode = new Barcode();
                var barcodeImage = barcode.Encode(TYPE.CODE128, barcodeText, 300, 60);

                if (barcodeImage != null)
                {
                    // Convertir System.Drawing.Image en System.Drawing.Bitmap puis en BitmapImage WPF
                    var bitmap = new DrawingMedia.Bitmap(barcodeImage);
                    BarcodeImageSource = ConvertBitmapToBitmapImage(bitmap);
                    Debug.WriteLine("GenerateBarcode - Code-barres généré avec succès");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la génération du code-barres: {ex.Message}");
                BarcodeImageSource = null;
            }
        }

        /// <summary>
        /// Convertit un System.Drawing.Bitmap en BitmapImage WPF
        /// </summary>
        private BitmapImage ConvertBitmapToBitmapImage(DrawingMedia.Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Permet l'utilisation thread-safe

                return bitmapImage;
            }
        }

        #endregion

        #region Chargement des informations d'entreprise

        /// <summary>
        /// Charge les informations d'entreprise depuis la base de données
        /// </summary>
        private async Task LoadCompanyInfoAsync()
        {
            Debug.WriteLine("LoadCompanyInfoAsync - Début");
            
            if (_databaseService == null)
            {
                Debug.WriteLine("LoadCompanyInfoAsync - DatabaseService est null");
                return;
            }

            try
            {
                Debug.WriteLine("LoadCompanyInfoAsync - Appel GetTableDataAsync");
                // Récupérer les données de la première entreprise active
                var companyData = await _databaseService.GetTableDataAsync("Companies", 1, 1, "IsActive = 1 AND IsDeleted = 0");
                
                Debug.WriteLine($"LoadCompanyInfoAsync - Données reçues: {companyData?.Rows.Count ?? 0} lignes");
                
                if (companyData != null && companyData.Rows.Count > 0)
                {
                    var row = companyData.Rows[0];
                    
                    // Utiliser TradeName si disponible, sinon CompanyName
                    var tradeName = row["TradeName"]?.ToString();
                    var companyName = row["CompanyName"]?.ToString();
                    CompanyName = !string.IsNullOrWhiteSpace(tradeName) ? tradeName : 
                                 !string.IsNullOrWhiteSpace(companyName) ? companyName : "FNEV4 SARL";
                    
                    Debug.WriteLine($"LoadCompanyInfoAsync - CompanyName défini: {CompanyName}");
                    
                    // Adresse
                    var address = row["Address"]?.ToString();
                    CompanyAddress = !string.IsNullOrWhiteSpace(address) ? address : "Zone Industrielle de Yopougon";
                    CompanyLocation = "Abidjan, Côte d'Ivoire"; // Valeur par défaut
                    
                    // Téléphone
                    var phone = row["Phone"]?.ToString();
                    CompanyPhone = !string.IsNullOrWhiteSpace(phone) ? $"Tél: {phone}" : "Tél: +225 27 23 45 67 89";
                    
                    // Email  
                    var email = row["Email"]?.ToString();
                    CompanyEmail = !string.IsNullOrWhiteSpace(email) ? $"Email: {email}" : "Email: contact@fnev4.ci";
                    
                    Debug.WriteLine($"Informations entreprise chargées: {CompanyName}");
                }
                else
                {
                    Debug.WriteLine("LoadCompanyInfoAsync - Aucune donnée d'entreprise trouvée");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors du chargement des informations d'entreprise: {ex.Message}");
                // Garder les valeurs par défaut
            }
        }

        /// <summary>
        /// Charge les informations de l'entreprise depuis la base de données de manière synchrone
        /// </summary>
        private void LoadCompanyInfoSync()
        {
            Debug.WriteLine("LoadCompanyInfoSync - Début");
            
            try
            {
                // Utiliser directement SQLite sans passer par IDatabaseService
                string dbPath = @"D:\PROJET\FNE\FNEV4\data\FNEV4.db";
                
                if (!System.IO.File.Exists(dbPath))
                {
                    Debug.WriteLine($"LoadCompanyInfoSync - Base de données non trouvée: {dbPath}");
                    return;
                }
                
                Debug.WriteLine($"LoadCompanyInfoSync - Connexion à la base: {dbPath}");
                
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    connection.Open();
                    
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT CompanyName, TradeName, Address, Phone, Email FROM Companies WHERE IsActive = 1 AND IsDeleted = 0 LIMIT 1";
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Debug.WriteLine("LoadCompanyInfoSync - Données trouvées");
                            
                            // Utiliser TradeName si disponible, sinon CompanyName
                            var tradeName = reader["TradeName"]?.ToString();
                            var companyName = reader["CompanyName"]?.ToString();
                            CompanyName = !string.IsNullOrWhiteSpace(tradeName) ? tradeName : 
                                         !string.IsNullOrWhiteSpace(companyName) ? companyName : "FNEV4 SARL";
                            
                            Debug.WriteLine($"LoadCompanyInfoSync - CompanyName: {CompanyName}");
                            
                            // Adresse
                            var address = reader["Address"]?.ToString();
                            CompanyAddress = !string.IsNullOrWhiteSpace(address) ? address : "Zone Industrielle de Yopougon";
                            
                            // Téléphone
                            var phone = reader["Phone"]?.ToString();
                            CompanyPhone = !string.IsNullOrWhiteSpace(phone) ? $"Tél: {phone}" : "Tél: +225 27 23 45 67 89";
                            
                            // Email  
                            var email = reader["Email"]?.ToString();
                            CompanyEmail = !string.IsNullOrWhiteSpace(email) ? $"Email: {email}" : "Email: contact@fnev4.ci";
                            
                            Debug.WriteLine($"LoadCompanyInfoSync - Informations chargées: {CompanyName}, {CompanyPhone}, {CompanyEmail}");
                        }
                        else
                        {
                            Debug.WriteLine("LoadCompanyInfoSync - Aucune donnée trouvée dans Companies");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadCompanyInfoSync - Erreur: {ex.Message}");
                // Garder les valeurs par défaut
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Implémentation simple de ICommand pour les commandes du ViewModel
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }
}