using System.Windows;
using FNEV4.Presentation.ViewModels.GestionFactures;

namespace FNEV4.Presentation.Views.GestionFactures
{
    /// <summary>
    /// Interaction logic for FactureDetailsView.xaml
    /// </summary>
    public partial class FactureDetailsView : Window
    {
        public FactureDetailsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is FactureDetailsViewModel viewModel)
            {
                viewModel.CloseRequested += () => Close();
            }
        }
    }
}