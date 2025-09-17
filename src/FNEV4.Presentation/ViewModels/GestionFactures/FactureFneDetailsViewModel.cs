using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using FNEV4.Core.Entities;
using System.Linq;

namespace FNEV4.Presentation.ViewModels.GestionFactures
{
    /// <summary>
    /// ViewModel pour le dialog de détails d'une facture FNE
    /// Basé sur la structure du Sage100FactureDetailsDialog
    /// </summary>
    public class FactureFneDetailsViewModel : INotifyPropertyChanged
    {
        private FneInvoice _facture;
        private ObservableCollection<FneInvoiceItem> _articles;

        public FactureFneDetailsViewModel(FneInvoice facture)
        {
            _facture = facture ?? throw new ArgumentNullException(nameof(facture));
            _articles = new ObservableCollection<FneInvoiceItem>(facture.Items ?? new List<FneInvoiceItem>());
            
            CalculateProperties();
        }

        public FneInvoice Facture
        {
            get => _facture;
            set
            {
                _facture = value;
                OnPropertyChanged();
                CalculateProperties();
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
        public Brush StatusBackground
        {
            get
            {
                return Facture?.Status?.ToLower() switch
                {
                    "certified" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Vert
                    "draft" => new SolidColorBrush(Color.FromRgb(158, 158, 158)),     // Gris
                    "validated" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),  // Bleu
                    "error" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),       // Rouge
                    _ => new SolidColorBrush(Color.FromRgb(96, 125, 139))             // Gris bleu
                };
            }
        }

        /// <summary>
        /// Couleur de fond du badge template
        /// </summary>
        public Brush TemplateBackground
        {
            get
            {
                return Facture?.Template switch
                {
                    "B2B" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),  // Bleu
                    "B2C" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Vert
                    "B2G" => new SolidColorBrush(Color.FromRgb(156, 39, 176)),  // Violet
                    "B2F" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),   // Orange
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))      // Gris
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
        public static Brush GetTvaColorBrush(string? codeTva)
        {
            return codeTva?.ToUpper() switch
            {
                "TVA" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Vert - TVA normal 18%
                "TVAB" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),  // Orange - TVA réduit 9%
                "TVAC" => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gris - TVA exec conv 0%
                "TVAD" => new SolidColorBrush(Color.FromRgb(96, 125, 139)),  // Gris bleu - TVA exec leg 0%
                null or "" => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Rouge - Erreur
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))       // Gris par défaut
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
}