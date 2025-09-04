using System.Windows;

namespace FNEV4.Presentation.Views.Maintenance
{
    public partial class TableStructureDialog : Window
    {
        public TableStructureDialog()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
