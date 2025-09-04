using System.Windows;

namespace FNEV4.Presentation.Views.Maintenance
{
    public partial class TableDataDialog : Window
    {
        public TableDataDialog()
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
