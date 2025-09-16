using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using FNEV4.Core.DTOs;

namespace FNEV4.Presentation.Views.CertificationFne
{
    /// <summary>
    /// Dialog pour afficher les résultats détaillés de certification
    /// </summary>
    public partial class CertificationResultDialog : Window
    {
        public class CertificationResultDialogViewModel
        {
            public List<CertificationResultDetail> Results { get; set; } = new();
            public DateTime ProcessedAt { get; set; } = DateTime.Now;
            
            public int TotalCount => Results.Count;
            public int SuccessCount => Results.Count(r => r.IsSuccess);
            public int ErrorCount => Results.Count(r => !r.IsSuccess);
            public double SuccessPercentage => TotalCount > 0 ? (double)SuccessCount / TotalCount * 100 : 0;
        }

        public CertificationResultDialog(List<CertificationResultDetail> results)
        {
            InitializeComponent();
            
            DataContext = new CertificationResultDialogViewModel
            {
                Results = results,
                ProcessedAt = DateTime.Now
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Fichiers CSV (*.csv)|*.csv|Tous les fichiers (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"certification_results_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToCsv(saveDialog.FileName);
                    MessageBox.Show($"Résultats exportés vers:\n{saveDialog.FileName}", 
                        "Export réussi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export:\n{ex.Message}", 
                    "Erreur d'export", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(string filePath)
        {
            if (DataContext is not CertificationResultDialogViewModel viewModel)
                return;

            var lines = new List<string>
            {
                "N° Facture;Client;Montant;Statut;Détails;Traité à"
            };

            foreach (var result in viewModel.Results)
            {
                var line = $"\"{result.InvoiceNumber}\";" +
                          $"\"{result.ClientName}\";" +
                          $"{result.Amount:F2};" +
                          $"\"{(result.IsSuccess ? "Succès" : "Échec")}\";" +
                          $"\"{result.Details.Replace("\"", "\"\"")}\";" +
                          $"\"{result.ProcessedAt:dd/MM/yyyy HH:mm:ss}\"";
                lines.Add(line);
            }

            File.WriteAllLines(filePath, lines, System.Text.Encoding.UTF8);
        }
    }
}