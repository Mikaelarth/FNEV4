using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using FNEV4.Presentation.ViewModels.Configuration;

namespace FNEV4.Presentation.Views.Configuration
{
    /// <summary>
    /// Vue de configuration des chemins et dossiers pour l'application FNEV4.
    /// Gère la configuration des dossiers d'import, export, archivage, logs et backup.
    /// </summary>
    public partial class CheminsDossiersConfigView : UserControl
    {
        /// <summary>
        /// Initialise une nouvelle instance de CheminsDossiersConfigView.
        /// </summary>
        public CheminsDossiersConfigView()
        {
            InitializeComponent();
            
            // Récupérer le ViewModel via l'injection de dépendances (pattern LogsDiagnosticsView)
            if (App.ServiceProvider != null)
            {
                DataContext = App.ServiceProvider.GetService(typeof(CheminsDossiersConfigViewModel));
            }
        }
    }
}
