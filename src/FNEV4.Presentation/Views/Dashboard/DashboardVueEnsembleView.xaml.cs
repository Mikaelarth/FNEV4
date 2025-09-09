using System.ComponentModel;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;
using FNEV4.Presentation.ViewModels.Dashboard;

namespace FNEV4.Presentation.Views.Dashboard
{
    /// <summary>
    /// Vue pour le Dashboard - Vue d'ensemble
    /// Affiche les KPIs, graphiques et informations système selon la charte graphique établie
    /// </summary>
    public partial class DashboardVueEnsembleView : UserControl
    {
        public DashboardVueEnsembleView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        public DashboardVueEnsembleView(DashboardVueEnsembleViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is DashboardVueEnsembleViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (e.NewValue is DashboardVueEnsembleViewModel newViewModel)
            {
                newViewModel.PropertyChanged += OnViewModelPropertyChanged;
                UpdateStatusIcons();
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DashboardVueEnsembleViewModel.StatutConnexionApi) ||
                e.PropertyName == nameof(DashboardVueEnsembleViewModel.StatutBaseDonnees) ||
                e.PropertyName == nameof(DashboardVueEnsembleViewModel.SoldeVignettes))
            {
                UpdateStatusIcons();
            }
        }

        private void UpdateStatusIcons()
        {
            if (DataContext is not DashboardVueEnsembleViewModel viewModel) return;

            // Mise à jour de l'icône API
            UpdateStatusIcon(ApiStatusIcon, ApiStatusText, viewModel.StatutConnexionApi);
            
            // Mise à jour de l'icône Base de données
            UpdateStatusIcon(DbStatusIcon, DbStatusText, viewModel.StatutBaseDonnees);
            
            // Mise à jour de l'icône Vignettes
            UpdateStatusIcon(VignettesStatusIcon, VignettesStatusText, viewModel.SoldeVignettes);
        }

        private void UpdateStatusIcon(PackIcon icon, TextBlock textBlock, string statusText)
        {
            // Extraire le texte sans emoji
            string cleanText = statusText;
            if (statusText.Contains("🟢") || statusText.Contains("Opérationnelle") || statusText.Contains("Connectée"))
            {
                icon.Kind = PackIconKind.CheckCircle;
                icon.Foreground = new SolidColorBrush(Colors.Green);
                cleanText = statusText.Replace("🟢", "").Trim();
            }
            else if (statusText.Contains("🔴") || statusText.Contains("❌") || statusText.Contains("Erreur"))
            {
                icon.Kind = PackIconKind.AlertCircle;
                icon.Foreground = new SolidColorBrush(Colors.Red);
                cleanText = statusText.Replace("🔴", "").Replace("❌", "").Trim();
            }
            else if (statusText.Contains("🟡") || statusText.Contains("⚠️") || statusText.Contains("Non configuré") || statusText.Contains("Configuration"))
            {
                icon.Kind = PackIconKind.Warning;
                icon.Foreground = new SolidColorBrush(Colors.Orange);
                cleanText = statusText.Replace("🟡", "").Replace("⚠️", "").Trim();
            }
            else
            {
                icon.Kind = PackIconKind.HelpCircle;
                icon.Foreground = new SolidColorBrush(Colors.Gray);
            }

            textBlock.Text = cleanText;
        }
    }
}
