using System.Windows;

namespace FNEV4.Presentation.Views.ImportTraitement
{
    /// <summary>
    /// Interaction logic for Sage100FactureDetailsDialog.xaml
    /// </summary>
    public partial class Sage100FactureDetailsDialog : Window
    {
        public Sage100FactureDetailsDialog()
        {
            InitializeComponent();
        }

        private void FermerButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
