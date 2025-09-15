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
            
            // Status avec description claire et informative des types d'import
            StatusMessage = $"✅ {AvailableImportTypes.Count} types d'import disponibles : " +
                           "Standard (en développement), Sage 100 v15 (opérationnel)";
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
                "📊 TYPES D'IMPORT DISPONIBLES :\n\n" +
                "• Import Standard : Formats Excel normalisés avec en-têtes fixes\n" +
                "  - Structure classique : N° facture, Date, Client, Montant HT, TVA\n" +
                "  - Validation automatique des données\n" +
                "  - Compatible avec la plupart des systèmes de gestion\n\n" +
                "• Import Exceptionnel Sage 100 v15 : Structure spécialisée\n" +
                "  - Format spécifique : 1 feuille = 1 facture\n" +
                "  - Gestion clients divers (code 1999) et normaux\n" +
                "  - Support moyen de paiement A18 (cash, card, mobile-money, etc.)\n" +
                "  - Validation métier avancée et gestion d'erreurs détaillée\n\n" +
                "• Import Configuration : Paramètres système et règles métier\n\n" +
                "📋 NOTES IMPORTANTES :\n" +
                "• Pour l'import de clients standard : 'Gestion Clients > Importer'\n" +
                "• Tous les imports génèrent des logs détaillés\n" +
                "• Les fichiers traités sont automatiquement archivés\n" +
                "• Support complet de la certification FNE Côte d'Ivoire\n\n" +
                "Pour plus d'informations, consultez la documentation technique.",
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
            
            // TODO: Implémenter la fenêtre d'historique complète
            MessageBox.Show(
                "📈 Historique des imports :\n\n" +
                "Cette fonctionnalité permettra de consulter :\n\n" +
                "📊 STATISTIQUES :\n" +
                "• Imports récents par type (Standard, Sage 100 v15)\n" +
                "• Taux de réussite et d'échec par période\n" +
                "• Volumes traités (nombre de factures, clients)\n\n" +
                "📋 LOGS DÉTAILLÉS :\n" +
                "• Journaux d'activité horodatés\n" +
                "• Détails des erreurs et avertissements\n" +
                "• Traçabilité complète des opérations\n\n" +
                "📁 FICHIERS TRAITÉS :\n" +
                "• Liste des fichiers importés\n" +
                "• Statuts de traitement et archivage\n" +
                "• Liens vers les dossiers d'archive\n\n" +
                "🚧 Fonctionnalité en cours de développement\n" +
                "Sera disponible dans la prochaine version.",
                "Historique des imports",
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
                Description = "Import spécialisé pour les factures Sage 100 v15 selon la structure définie. Gère les clients divers (code 1999), moyens de paiement A18, et la structure spécifique : 1 feuille = 1 facture. Supporte la validation avancée et la gestion des erreurs.",
                Icon = "FileExcelOutline", 
                IsEnabled = true,
                Status = "Opérationnel",
                StatusColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green pour indiquer que c'est fonctionnel
                SupportedFormats = ".xlsx Sage 100 v15 (structure validée automatiquement)",
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
                // Obtenir le ViewModel via l'injection de dépendance
                var sage100ViewModel = _serviceProvider.GetRequiredService<Sage100ImportViewModel>();
                
                // Créer et configurer la fenêtre d'import Sage 100 avec notre nouvelle interface modale
                var sage100Window = new Views.ImportTraitement.Sage100ImportWindow(sage100ViewModel)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                
                // Afficher la fenêtre de manière modale
                var dialogResult = sage100Window.ShowDialog();
                
                StatusMessage = dialogResult == true 
                    ? "✅ Import Sage 100 v15 terminé avec succès" 
                    : "ℹ️ Import Sage 100 v15 fermé";
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Sage100ImportViewModel"))
            {
                StatusMessage = "❌ Service Sage 100 non configuré";
                MessageBox.Show(
                    "Le service d'import Sage 100 n'est pas correctement configuré.\n\n" +
                    "Erreur : Service Sage100ImportViewModel introuvable dans l'injection de dépendance.\n\n" +
                    "Veuillez vérifier la configuration des services dans App.xaml.cs",
                    "Service manquant",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erreur ouverture import Sage 100 : {ex.Message}";
                MessageBox.Show(
                    $"Impossible d'ouvrir la fenêtre d'import Sage 100 :\n\n{ex.Message}\n\n" +
                    "Détails techniques :\n" +
                    $"Type d'erreur : {ex.GetType().Name}\n" +
                    "Vérifiez que tous les composants nécessaires sont présents et correctement configurés.",
                    "Erreur d'ouverture",
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
