using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using FNEV4.Presentation.ViewModels.ImportTraitement;

namespace FNEV4.Presentation.Views.ImportTraitement
{
    /// <summary>
    /// Interaction logic for ImportFichiersView.xaml
    /// Vue pour la gestion des imports de fichiers dans FNE V4
    /// </summary>
    public partial class ImportFichiersView : UserControl
    {
        /// <summary>
        /// Initialise une nouvelle instance de ImportFichiersView
        /// </summary>
        public ImportFichiersView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructeur avec ViewModel
        /// </summary>
        /// <param name="viewModel">ViewModel à associer à la vue</param>
        public ImportFichiersView(ImportFichiersViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        /// <summary>
        /// Gestion de l'effet hover sur les cartes d'import
        /// </summary>
        private void Card_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Card card)
            {
                // Animation d'élévation au survol
                ElevationAssist.SetElevation(card, Elevation.Dp4);
                
                // Effet de mise à l'échelle subtil
                card.RenderTransform = new ScaleTransform(1.02, 1.02);
                card.RenderTransformOrigin = new Point(0.5, 0.5);
            }
        }

        /// <summary>
        /// Gestion de la sortie du survol sur les cartes d'import
        /// </summary>
        private void Card_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Card card)
            {
                // Retour à l'élévation normale
                ElevationAssist.SetElevation(card, Elevation.Dp2);
                
                // Retour à la taille normale
                card.RenderTransform = new ScaleTransform(1.0, 1.0);
            }
        }
    }
}
