using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using FNEV4.Presentation.ViewModels.GestionClients;

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
            
            // Utiliser le MÊME pattern que BaseDonneesView - injection directe simple
            DataContext = App.ServiceProvider.GetRequiredService<ListeClientsViewModel>();
        }
    }
}
