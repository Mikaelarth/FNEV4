using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FNEV4.Core.Models.ImportTraitement;

namespace FNEV4.Presentation.Views.ImportTraitement
{
    public partial class Sage100ProduitsDetailDialog : Window
    {
        public Sage100ProduitsDetailDialog(IEnumerable<Sage100ProduitData> produits, string numeroFacture)
        {
            InitializeComponent();
            
            var produitsListe = produits.ToList();
            
            DataContext = new ProduitsDetailViewModel
            {
                Produits = produitsListe,
                TitreDialog = $"Détail des produits - Facture {numeroFacture}",
                ResumeProduits = $"{produitsListe.Count} produit(s)",
                TotalHT = produitsListe.Sum(p => p.MontantHt)
            };
        }

        private void FermerButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class ProduitsDetailViewModel
    {
        public List<Sage100ProduitData> Produits { get; set; } = new();
        public string TitreDialog { get; set; } = string.Empty;
        public string ResumeProduits { get; set; } = string.Empty;
        public decimal TotalHT { get; set; }
        
        // Pour l'export futur si nécessaire
        public ICommand? ExporterCommand { get; set; }
    }
}
