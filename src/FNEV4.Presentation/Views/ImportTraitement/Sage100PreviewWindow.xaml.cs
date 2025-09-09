using System.Windows;
using System.Windows.Controls;
using FNEV4.Presentation.ViewModels.ImportTraitement;
using FNEV4.Core.Models.ImportTraitement;

namespace FNEV4.Presentation.Views.ImportTraitement
{
    /// <summary>
    /// Logique d'interaction pour Sage100PreviewWindow.xaml
    /// </summary>
    public partial class Sage100PreviewWindow : Window
    {
        public Sage100PreviewWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void VoirProduitsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Sage100FacturePreview invoicePreview)
            {
                var dialog = new Sage100ProduitsDetailDialog(
                    invoicePreview.Produits, 
                    invoicePreview.NumeroFacture
                );
                dialog.Owner = this;
                dialog.ShowDialog();
            }
        }
    }
}
