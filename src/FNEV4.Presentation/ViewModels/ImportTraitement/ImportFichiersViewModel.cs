using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        public ImportFichiersViewModel()
        {
            InitializeImportTypes();
            
            // Debug - Forcer le message de statut
            StatusMessage = $"✅ {AvailableImportTypes.Count} types d'import spécialisés chargés";
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
                    case "Factures":
                        StatusMessage = "🚧 Import de factures Sage 100 v15 - En cours de développement";
                        MessageBox.Show(
                            "Import de Factures Sage 100 v15\n\n" +
                            "Fonctionnalité en cours de développement.\n" +
                            "Support du nouveau champ A18 (moyens de paiement).\n\n" +
                            "Sera disponible dans une prochaine mise à jour.",
                            "En développement",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        break;
                        
                    case "Configuration":
                        StatusMessage = "🚧 Import de configuration système - En cours de développement";
                        MessageBox.Show(
                            "Import de Configuration Système\n\n" +
                            "Fonctionnalité en cours de développement.\n" +
                            "Permettra l'import de paramètres, chemins et règles métier.\n\n" +
                            "Sera disponible dans une prochaine mise à jour.",
                            "En développement",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        break;
                        
                    default:
                        StatusMessage = $"❌ Type d'import non reconnu : {type}";
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
                "Aide sur l'import de fichiers spécialisés :\n\n" +
                "• Factures : Import des factures Sage 100 v15 avec support A18\n" +
                "• Configuration : Import des paramètres système et règles métier\n\n" +
                "Note : Pour l'import de clients, utilisez le menu\n" +
                "'Gestion Clients > Liste des clients > Importer'\n\n" +
                "Pour plus d'informations, consultez la documentation.",
                "Aide - Import de fichiers spécialisés",
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
        /// Initialise les types d'import disponibles avec intégration au système de chemins
        /// </summary>
        private void InitializeImportTypes()
        {
            AvailableImportTypes.Clear();
            
            // Import de Factures Sage 100 - Nouveau avec support A18
            AvailableImportTypes.Add(new ImportTypeInfo
            {
                Type = "Factures",
                Title = "Import Factures Sage 100 v15",
                Description = "Import des factures depuis Sage 100 v15 avec support du nouveau champ A18 (moyens de paiement). Utilise le dossier d'import configuré avec surveillance automatique et archivage selon les règles définies.",
                Icon = "FileDocumentMultiple", 
                IsEnabled = true,
                Status = "Nouveau - Support A18",
                StatusColor = new SolidColorBrush(Colors.Orange),
                SupportedFormats = ".xlsx (Structure : 1 classeur = N factures, 1 feuille = 1 facture)",
                ButtonText = "IMPORTER FACTURES",
                Color = new SolidColorBrush(Color.FromRgb(255, 152, 0)) // Orange Material
            });
            
            // Import de Données de Configuration
            AvailableImportTypes.Add(new ImportTypeInfo
            {
                Type = "Configuration",
                Title = "Import Configuration Système",
                Description = "Import des paramètres de configuration, chemins de dossiers et règles métier depuis un fichier de sauvegarde. Permet la migration entre environnements.",
                Icon = "Cog", 
                IsEnabled = true,
                Status = "Système",
                StatusColor = new SolidColorBrush(Colors.Purple),
                SupportedFormats = ".json, .xml (fichiers de configuration)",
                ButtonText = "IMPORTER CONFIG",
                Color = new SolidColorBrush(Color.FromRgb(156, 39, 176)) // Purple Material
            });

            // Copier vers la collection d'affichage
            ImportTypes.Clear();
            foreach (var type in AvailableImportTypes)
            {
                ImportTypes.Add(type);
            }
            
            OnPropertyChanged(nameof(HasResults));
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
