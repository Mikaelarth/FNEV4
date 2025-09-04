using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using FNEV4.Presentation.ViewModels.Maintenance;

namespace FNEV4.Presentation.Views.Maintenance
{
    /// <summary>
    /// Logique d'interaction pour BaseDonneesView.xaml
    /// </summary>
    public partial class BaseDonneesView : UserControl
    {
        public BaseDonneesView()
        {
            InitializeComponent();
            
            // Utiliser le VRAI ViewModel avec injection de dépendances
            try
            {
                DataContext = App.ServiceProvider.GetRequiredService<BaseDonneesViewModel>();
            }
            catch
            {
                // Fallback - créer manuellement si l'injection échoue
                DataContext = new BaseDonneesViewModel();
            }
        }
    }
}
