using FNEV4.Presentation.ViewModels.GestionClients;
using System.Windows;

namespace FNEV4.Presentation.Views.GestionClients
{
    public partial class AjoutModificationClientView : Window
    {
        public AjoutModificationClientView()
        {
            InitializeComponent();
        }

        public AjoutModificationClientView(AjoutModificationClientViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus sur le premier champ lors du chargement
            if (DataContext is AjoutModificationClientViewModel viewModel)
            {
                // Trouver le premier TextBox et lui donner le focus
                var firstTextBox = FindName("CodeClientTextBox") as System.Windows.Controls.TextBox;
                firstTextBox?.Focus();
            }
        }
    }
}
