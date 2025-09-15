using System.Windows;
using FNEV4.Presentation.ViewModels.ImportTraitement;

namespace FNEV4.Presentation.Views.ImportTraitement
{
    /// <summary>
    /// Logique d'interaction pour Sage100ImportWindow.xaml
    /// </summary>
    public partial class Sage100ImportWindow : Window
    {
        public Sage100ImportWindow()
        {
            InitializeComponent();
        }

        public Sage100ImportWindow(Sage100ImportViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}