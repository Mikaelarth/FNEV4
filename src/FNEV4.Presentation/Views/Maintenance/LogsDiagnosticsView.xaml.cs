using System.Windows.Controls;
using FNEV4.Presentation.ViewModels.Maintenance;
using Microsoft.Extensions.DependencyInjection;

namespace FNEV4.Presentation.Views.Maintenance
{
    /// <summary>
    /// Logique d'interaction pour LogsDiagnosticsView.xaml
    /// Vue pour la consultation des logs système et outils de diagnostic
    /// </summary>
    public partial class LogsDiagnosticsView : UserControl
    {
        public LogsDiagnosticsView()
        {
            InitializeComponent();
            
            // Récupérer le ViewModel via l'injection de dépendances
            if (App.ServiceProvider != null)
            {
                DataContext = App.ServiceProvider.GetService(typeof(LogsDiagnosticsViewModel));
            }
        }
    }
}
