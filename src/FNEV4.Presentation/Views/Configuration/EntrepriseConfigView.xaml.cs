using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using FNEV4.Presentation.ViewModels.Configuration;

namespace FNEV4.Presentation.Views.Configuration
{
    /// <summary>
    /// Logique d'interaction pour EntrepriseConfigView.xaml
    /// </summary>
    public partial class EntrepriseConfigView : UserControl
    {
        public EntrepriseConfigView()
        {
            InitializeComponent();
            
            // Utiliser le pattern de BaseDonneesView avec fallback
            try
            {
                DataContext = App.ServiceProvider.GetRequiredService<EntrepriseConfigViewModel>();
            }
            catch
            {
                // Fallback - créer manuellement si l'injection échoue
                DataContext = new EntrepriseConfigViewModel();
            }
        }
    }
}
