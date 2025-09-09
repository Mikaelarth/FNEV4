using System.Windows;

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
    }
}
