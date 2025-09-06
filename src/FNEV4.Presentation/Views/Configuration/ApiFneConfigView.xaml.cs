using System.Windows.Controls;
using FNEV4.Presentation.ViewModels.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FNEV4.Presentation.Views.Configuration
{
    /// <summary>
    /// Interaction logic for ApiFneConfigView.xaml
    /// Vue pour la configuration API FNE
    /// </summary>
    public partial class ApiFneConfigView : UserControl
    {
        public ApiFneConfigView()
        {
            InitializeComponent();
            
            // Configurer le DataContext avec l'injection de dépendances
            if (DataContext == null && App.ServiceProvider != null)
            {
                DataContext = App.ServiceProvider.GetRequiredService<ApiFneConfigViewModel>();
            }
        }
    }
}
