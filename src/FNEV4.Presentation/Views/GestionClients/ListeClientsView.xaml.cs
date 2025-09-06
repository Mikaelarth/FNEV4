using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using FNEV4.Presentation.Services;

namespace FNEV4.Presentation.Views.GestionClients
{
    /// <summary>
    /// Logique d'interaction pour ListeClientsView.xaml
    /// </summary>
    public partial class ListeClientsView : UserControl
    {
        public ListeClientsView()
        {
            InitializeComponent();
            
            // Configuration du DataContext via le ViewModelLocator
            if (App.ServiceProvider != null)
            {
                var locator = App.ServiceProvider.GetService(typeof(ViewModelLocator)) as ViewModelLocator;
                DataContext = locator?.ListeClientsViewModel;
            }
        }
    }
}
