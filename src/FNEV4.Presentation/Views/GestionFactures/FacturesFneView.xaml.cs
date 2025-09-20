using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using FNEV4.Core.Entities;
using FNEV4.Presentation.ViewModels.GestionFactures;

namespace FNEV4.Presentation.Views.GestionFactures
{
    /// <summary>
    /// Logique d'interaction pour FacturesFneView.xaml
    /// Vue simple pour afficher la liste des factures FNE
    /// </summary>
    public partial class FacturesFneView : UserControl
    {
        public FacturesFneView()
        {
            InitializeComponent();
            
            // Gérer les changements de sélection multiple dans la DataGrid
            FacturesDataGrid.SelectionChanged += FacturesDataGrid_SelectionChanged;
        }

        /// <summary>
        /// Synchronise la sélection de la DataGrid avec le ViewModel
        /// </summary>
        private void FacturesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is FacturesFneViewModel viewModel)
            {
                // Mettre à jour la collection des factures sélectionnées
                viewModel.SelectedFactures.Clear();
                foreach (FneInvoice facture in FacturesDataGrid.SelectedItems)
                {
                    viewModel.SelectedFactures.Add(facture);
                }
                
                // Mettre à jour les propriétés de sélection
                viewModel.UpdateMultipleSelection();
            }
        }
    }
}