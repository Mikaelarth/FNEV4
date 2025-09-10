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
            
            // S'assurer que la fenêtre reste visible et accessible
            this.Loaded += OnLoaded;
            this.Activated += OnActivated;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // S'assurer que la fenêtre est au premier plan lors du chargement
            this.Activate();
            this.Focus();
        }

        private void OnActivated(object sender, System.EventArgs e)
        {
            // Maintenir le focus quand la fenêtre est activée
            this.Focus();
        }

        private void FermerButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
