using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Presentation.Views.ImportTraitement;
using FNEV4.Presentation.ViewModels.ImportTraitement;
using FNEV4.Application.Services.ImportTraitement;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Media;

namespace FNEV4.Presentation.ViewModels.ImportTraitement
{
    /// <summary>
    /// ViewModel pour le module "Import de fichiers"
    /// Gère l'import de différents types de fichiers Excel (clients, factures, etc.)
    /// </summary>
    public partial class ImportFichiersViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;

        #region Properties

        /// <summary>
        /// Indicateur de chargement global
        /// </summary>
        [ObservableProperty]
        private bool _isLoading = false;

        /// <summary>
        /// Message de statut global
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "Sélectionnez le type d'import à effectuer";

        /// <summary>
        /// Liste des types d'import disponibles pour l'affichage
        /// </summary>
        public ObservableCollection<ImportTypeInfo> AvailableImportTypes { get; } = new();

        /// <summary>
        /// Liste des types d'import pour l'interface
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ImportTypeInfo> importTypes = new();

        /// <summary>
        /// Texte de recherche pour filtrer les imports
        /// </summary>
        [ObservableProperty]
        private string searchText = string.Empty;

        /// <summary>
        /// Indique si des résultats sont disponibles
        /// </summary>
        public bool HasResults => ImportTypes?.Any() == true;

        #endregion

        #region UI Properties

        /// <summary>
        /// Titre de la fenêtre
        /// </summary>
        public string WindowTitle => "Import de fichiers";

        /// <summary>
        /// Titre principal
        /// </summary>
        public string HeaderTitle => "Import & Traitement";

        /// <summary>
        /// Sous-titre
        /// </summary>
        public string HeaderSubtitle => "Import de fichiers Excel - Clients, Factures et autres données";

        #endregion

        #region Constructor

        public ImportFichiersViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeImportTypes();
            
            // Status avec description claire des types d'import
            StatusMessage = $"✅ {AvailableImportTypes.Count} types d'import disponibles : Standard, Exceptionnel Sage v15";
        }

        #endregion

        #region Commands

        /// <summary>
        /// Commande pour rafraîchir la vue
        /// </summary>
        [RelayCommand]
        private async Task Refresh()
        {
            IsLoading = true;
            StatusMessage = "Actualisation...";
            
            try
            {
                // Simuler un rafraîchissement
                await Task.Delay(500);
                
                InitializeImportTypes();
                StatusMessage = "✅ Interface actualisée";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur actualisation : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Commande pour ouvrir un type d'import spécifique
        /// </summary>
        [RelayCommand]
        private void OpenImport(object parameter)
        {
            if (parameter is ImportTypeInfo importType)
            {
                OpenImport(importType.Type);
            }
            else if (parameter is string type)
            {
                OpenImport(type);
            }
        }

        /// <summary>
        /// Ouvre un type d'import spécialisé
        /// </summary>
        private void OpenImport(string type)
        {
            try
            {
                StatusMessage = $"Ouverture de l'import {type}...";
                
                switch (type)
                {
                    case "FacturesStandard":
                        StatusMessage = "🚧 Import Factures Standard - En cours de développement";
                        MessageBox.Show(
                            "Import de Factures Standard\n\n" +
                            "Fonctionnalité en cours de développement.\n" +
                            "Gérera l'import des factures depuis des fichiers Excel au format standard professionnel :\n" +
                            "• Structure classique avec en-têtes normalisés\n" +
                            "• Colonnes prédéfinies : N° facture, Date, Client, Montant HT, TVA, Total TTC\n" +
                            "• Format conventionnel adapté aux systèmes de gestion standard\n" +
                            "• Validation des données et contrôles de cohérence\n\n" +
                            "Sera disponible dans une prochaine mise à jour.",
                            "En développement",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        break;
                        
                    case "FacturesSage100":
                        StatusMessage = "🚀 Ouverture de l'import Sage 100 v15...";
                        OpenSage100ImportWindow();
                        break;
                        
                    default:
                        StatusMessage = $"❌ Type d'import de factures non reconnu : {type}";
                        MessageBox.Show(
                            $"Type d'import non reconnu : {type}\n\n" +
                            "Types supportés :\n" +
                            "• FacturesStandard : Import de factures au format standard professionnel\n" +
                            "• FacturesSage100 : Import spécialisé pour Sage 100 v15",
                            "Erreur",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur ouverture import {type} : {ex.Message}";
            }
        }

        /// <summary>
        /// Commande pour afficher l'aide
        /// </summary>
        [RelayCommand]
        private void ShowHelp()
        {
            StatusMessage = "💡 Affichage de l'aide...";
            
            MessageBox.Show(
                "Aide sur l'import de fichiers :\n\n" +
                "• Import Standard : Formats Excel normalisés avec en-têtes fixes\n" +
                "• Import Exceptionnel Sage v15 : Structure non-standard spécifique\n" +
                "• Import Configuration : Paramètres système et règles métier\n\n" +
                "Note : Pour l'import de clients standard, utilisez le menu\n" +
                "'Gestion Clients > Liste des clients > Importer'\n\n" +
                "Pour plus d'informations, consultez la documentation.",
                "Aide - Import de fichiers",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            StatusMessage = "Sélectionnez le type d'import à effectuer";
        }

        /// <summary>
        /// Commande pour afficher l'historique des imports
        /// </summary>
        [RelayCommand]
        private void ShowHistory()
        {
            StatusMessage = "📊 Ouverture de l'historique...";
            
            // TODO: Implémenter la fenêtre d'historique
            MessageBox.Show(
                "Historique des imports :\n\n" +
                "Cette fonctionnalité permettra de voir :\n" +
                "• Les imports récents\n" +
                "• Les statistiques de réussite\n" +
                "• Les logs d'erreurs\n\n" +
                "🚧 En cours de développement",
                "Historique - Import de fichiers",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            StatusMessage = "Sélectionnez le type d'import à effectuer";
        }

        /// <summary>
        /// Commande pour effectuer une recherche
        /// </summary>
        [RelayCommand]
        private void Search()
        {
            StatusMessage = "🔍 Recherche en cours...";
            
            // Filtrer les types d'import selon le texte de recherche
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Afficher tous les types
                ImportTypes.Clear();
                foreach (var type in AvailableImportTypes)
                {
                    ImportTypes.Add(type);
                }
            }
            else
            {
                // Filtrer selon le texte
                var filtered = AvailableImportTypes
                    .Where(t => t.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                               t.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                ImportTypes.Clear();
                foreach (var type in filtered)
                {
                    ImportTypes.Add(type);
                }
            }
            
            OnPropertyChanged(nameof(HasResults));
            StatusMessage = $"🔍 {ImportTypes.Count} résultat(s) trouvé(s)";
        }

        /// <summary>
        /// Commande pour revenir en arrière (optionnelle)
        /// </summary>
        [RelayCommand]
        private void GoBack()
        {
            StatusMessage = "Retour au menu principal...";
            
            // TODO: Implémenter la navigation retour si nécessaire
            // Cette commande peut être utilisée pour revenir au menu principal
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialise les types d'import de factures disponibles
        /// </summary>
        private void InitializeImportTypes()
        {
            AvailableImportTypes.Clear();
            
            // Import de Factures Standard - Structure professionnelle classique
            AvailableImportTypes.Add(new ImportTypeInfo
            {
                Type = "FacturesStandard",
                Title = "Import Factures Standard",
                Description = "Import des factures depuis des fichiers Excel au format standard professionnel. Structure classique avec en-têtes normalisés, colonnes prédéfinies et format conventionnel adapté aux systèmes de gestion standard.",
                Icon = "FileDocumentOutline", 
                IsEnabled = true,
                Status = "Standard",
                StatusColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
                SupportedFormats = ".xlsx, .xls (format standard avec en-têtes normalisés)",
                ButtonText = "IMPORTER STANDARD",
                Color = new SolidColorBrush(Color.FromRgb(67, 160, 71)) // Green moderne
            });
            
            // Import de Factures Sage 100 v15 - Structure spécifique selon exemple_structure_excel.py
            AvailableImportTypes.Add(new ImportTypeInfo
            {
                Type = "FacturesSage100",
                Title = "Import Exceptionnel Sage 100 v15",
                Description = "Import spécialisé pour les factures Sage 100 v15 selon la structure définie dans exemple_structure_excel.py. Gère les clients divers (code 1999), moyens de paiement A18, et la structure spécifique : 1 feuille = 1 facture.",
                Icon = "AlertCircleOutline", 
                IsEnabled = true,
                Status = "Sage 100 v15",
                StatusColor = new SolidColorBrush(Color.FromRgb(255, 193, 7)), // Amber
                SupportedFormats = ".xlsx spécifique Sage 100 v15 (selon exemple_structure_excel.py)",
                ButtonText = "IMPORTER SAGE 100",
                Color = new SolidColorBrush(Color.FromRgb(255, 87, 34)) // Deep Orange
            });

            // Copier vers la collection d'affichage
            ImportTypes.Clear();
            foreach (var type in AvailableImportTypes)
            {
                ImportTypes.Add(type);
            }
            
            OnPropertyChanged(nameof(HasResults));
        }

        /// <summary>
        /// Ouvre la fenêtre d'import Sage 100 v15
        /// </summary>
        private void OpenSage100ImportWindow()
        {
            try
            {
                var sage100ImportView = new Sage100ImportView();
                var sage100ImportViewModel = _serviceProvider.GetRequiredService<Sage100ImportViewModel>();
                sage100ImportView.DataContext = sage100ImportViewModel;
                
                var window = new Window
                {
                    Title = "Import Exceptionnel Sage 100 v15",
                    Content = sage100ImportView,
                    Width = 1400,
                    Height = 900,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowState = WindowState.Normal,
                    ShowInTaskbar = true,
                    ResizeMode = ResizeMode.CanResize
                };
                
                // Passer la référence de la fenêtre au ViewModel pour pouvoir la fermer
                sage100ImportViewModel.ParentWindow = window;
                
                window.ShowDialog();
                StatusMessage = "📋 Fenêtre d'import Sage 100 fermée";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur lors de l'ouverture : {ex.Message}";
                MessageBox.Show(
                    $"Impossible d'ouvrir la fenêtre d'import Sage 100 :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion
    }

    /// <summary>
    /// Information sur un type d'import
    /// </summary>
    public class ImportTypeInfo : ObservableObject
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "File";
        public bool IsEnabled { get; set; } = true;
        public string Status { get; set; } = "Disponible";
        public Brush StatusColor { get; set; } = new SolidColorBrush(Colors.Green);
        public string SupportedFormats { get; set; } = ".xlsx, .xls";
        public string ButtonText { get; set; } = "IMPORTER";
        public Brush Color { get; set; } = new SolidColorBrush(Colors.LightBlue);
    }
}
