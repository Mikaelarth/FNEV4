using System.Windows;

namespace FNEV4.Presentation.Views.Maintenance
{
    /// <summary>
    /// Fenêtre de dialogue pour les paramètres de la base de données
    /// </summary>
    public partial class DatabaseSettingsDialog : Window
    {
        public DatabaseSettingsDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Affiche la fenêtre de dialogue avec une fenêtre parent
        /// </summary>
        /// <param name="owner">Fenêtre parent</param>
        /// <returns>Résultat de la boîte de dialogue</returns>
        public bool? ShowDialog(Window owner)
        {
            Owner = owner;
            return base.ShowDialog();
        }
    }
}
