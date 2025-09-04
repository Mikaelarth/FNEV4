using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FNEV4.Presentation.ViewModels;
using MaterialDesignThemes.Wpf;

namespace FNEV4.Presentation.Views
{
    /// <summary>
    /// Fenêtre principale de l'application FNEV4
    /// Contient le menu de navigation et la zone de contenu principal
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Configuration initiale
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            // Configuration de la fenêtre
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // Configuration du DataContext si nécessaire
            if (DataContext is MainViewModel viewModel)
            {
                // Écouter l'événement pour fermer les sous-menus
                viewModel.CloseSubMenusRequested += CloseAllSubMenus;
            }
        }

        /// <summary>
        /// Gère l'expansion/contraction de la section Dashboard
        /// </summary>
        private void ToggleDashboardSection(object sender, RoutedEventArgs e)
        {
            ToggleMenuSection("DashboardSubMenu", "DashboardChevron");
        }

        /// <summary>
        /// Gère l'expansion/contraction de la section Import
        /// </summary>
        private void ToggleImportSection(object sender, RoutedEventArgs e)
        {
            ToggleMenuSection("ImportSubMenu", "ImportChevron");
        }

        /// <summary>
        /// Gère l'expansion/contraction de la section Factures
        /// </summary>
        private void ToggleFacturesSection(object sender, RoutedEventArgs e)
        {
            ToggleMenuSection("FacturesSubMenu", "FacturesChevron");
        }

        /// <summary>
        /// Gère l'expansion/contraction de la section Certification
        /// </summary>
        private void ToggleCertificationSection(object sender, RoutedEventArgs e)
        {
            ToggleMenuSection("CertificationSubMenu", "CertificationChevron");
        }

        /// <summary>
        /// Gère l'expansion/contraction de la section Clients
        /// </summary>
        private void ToggleClientsSection(object sender, RoutedEventArgs e)
        {
            ToggleMenuSection("ClientsSubMenu", "ClientsChevron");
        }

        /// <summary>
        /// Gère l'expansion/contraction de la section Rapports
        /// </summary>
        private void ToggleRapportsSection(object sender, RoutedEventArgs e)
        {
            ToggleMenuSection("RapportsSubMenu", "RapportsChevron");
        }

        /// <summary>
        /// Gère l'expansion/contraction de la section Configuration
        /// </summary>
        private void ToggleConfigurationSection(object sender, RoutedEventArgs e)
        {
            ToggleMenuSection("ConfigurationSubMenu", "ConfigurationChevron");
        }

        /// <summary>
        /// Gère l'expansion/contraction de la section Maintenance
        /// </summary>
        private void ToggleMaintenanceSection(object sender, RoutedEventArgs e)
        {
            ToggleMenuSection("MaintenanceSubMenu", "MaintenanceChevron");
        }

        /// <summary>
        /// Méthode générique pour basculer l'état d'une section de menu
        /// </summary>
        private void ToggleMenuSection(string subMenuName, string chevronName)
        {
            // Ne pas permettre l'expansion si le menu principal est réduit
            if (DataContext is MainViewModel viewModel && !viewModel.IsMenuExpanded)
                return;

            var subMenu = FindName(subMenuName) as StackPanel;
            var chevron = FindName(chevronName) as PackIcon;
            
            if (subMenu != null)
            {
                bool isVisible = subMenu.Visibility == Visibility.Visible;
                subMenu.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
                
                // Rotation du chevron
                if (chevron != null)
                {
                    chevron.Kind = isVisible ? PackIconKind.ChevronDown : PackIconKind.ChevronUp;
                }
            }
        }

        /// <summary>
        /// Ferme tous les sous-menus quand le menu principal se réduit
        /// </summary>
        public void CloseAllSubMenus()
        {
            var subMenuNames = new[] { "DashboardSubMenu", "ImportSubMenu", "FacturesSubMenu", 
                                     "CertificationSubMenu", "ClientsSubMenu", "RapportsSubMenu", 
                                     "ConfigurationSubMenu", "MaintenanceSubMenu", "AideSubMenu" };
            
            var chevronNames = new[] { "DashboardChevron", "ImportChevron", "FacturesChevron", 
                                     "CertificationChevron", "ClientsChevron", "RapportsChevron", 
                                     "ConfigurationChevron", "MaintenanceChevron", "AideChevron" };

            for (int i = 0; i < subMenuNames.Length; i++)
            {
                var subMenu = FindName(subMenuNames[i]) as StackPanel;
                var chevron = FindName(chevronNames[i]) as PackIcon;
                
                if (subMenu != null)
                {
                    subMenu.Visibility = Visibility.Collapsed;
                }
                
                if (chevron != null)
                {
                    chevron.Kind = PackIconKind.ChevronDown;
                }
            }
        }
    }
}
