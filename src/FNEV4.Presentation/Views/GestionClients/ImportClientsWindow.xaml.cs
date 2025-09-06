using System.Windows;
using FNEV4.Presentation.ViewModels.GestionClients;

namespace FNEV4.Presentation.Views.GestionClients
{
    /// <summary>
    /// Logique d'interaction pour ImportClientsWindow.xaml
    /// </summary>
    public partial class ImportClientsWindow : Window
    {
        public ImportClientsWindow()
        {
            InitializeComponent();
        }

        public ImportClientsWindow(ImportClientsViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
