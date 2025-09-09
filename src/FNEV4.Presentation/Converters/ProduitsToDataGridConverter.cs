using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using FNEV4.Core.Models.ImportTraitement;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertisseur qui transforme une liste de produits en DataGrid détaillé pour tooltip
    /// </summary>
    public class ProduitsToDataGridConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not List<Sage100ProduitData> produits || !produits.Any())
            {
                return CreateEmptyTooltip();
            }

            return CreateProductTooltip(produits);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private ToolTip CreateEmptyTooltip()
        {
            var tooltip = new ToolTip
            {
                Background = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12),
                MaxWidth = 300
            };

            var textBlock = new TextBlock
            {
                Text = "Aucun produit détecté",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            tooltip.Content = textBlock;
            return tooltip;
        }

        private ToolTip CreateProductTooltip(List<Sage100ProduitData> produits)
        {
            var tooltip = new ToolTip
            {
                Background = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                MaxWidth = 600,
                MaxHeight = 400
            };

            var stackPanel = new StackPanel();

            // En-tête
            var headerText = new TextBlock
            {
                Text = $"Détail des produits ({produits.Count})",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackPanel.Children.Add(headerText);

            // DataGrid des produits
            var dataGrid = new DataGrid
            {
                ItemsSource = produits,
                AutoGenerateColumns = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                Background = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                Foreground = new SolidColorBrush(Colors.White),
                RowBackground = new SolidColorBrush(Color.FromRgb(52, 58, 64)),
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(73, 80, 87)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderThickness = new Thickness(2),
                CanUserSortColumns = true,
                CanUserReorderColumns = false,
                CanUserResizeColumns = true,
                SelectionMode = DataGridSelectionMode.Single,
                IsReadOnly = true,
                FontSize = 12,
                MaxHeight = 400,
                MinWidth = 750,
                ColumnHeaderHeight = 35,
                RowHeight = 32,
                Margin = new Thickness(8),
                Padding = new Thickness(6)
            };

            // Colonnes du DataGrid
            AddProductColumns(dataGrid);

            stackPanel.Children.Add(dataGrid);

            // Résumé en bas
            var totalHT = produits.Sum(p => p.MontantHt);
            var summaryText = new TextBlock
            {
                Text = $"Total HT: {totalHT:C2}",
                Foreground = new SolidColorBrush(Colors.LightGreen),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            stackPanel.Children.Add(summaryText);

            tooltip.Content = stackPanel;
            return tooltip;
        }

        private void AddProductColumns(DataGrid dataGrid)
        {
            // Code produit
            var codeColumn = new DataGridTextColumn
            {
                Header = "Code Produit",
                Binding = new Binding("CodeProduit"),
                Width = DataGridLength.Auto,
                MinWidth = 100
            };
            var codeStyle = new Style(typeof(TextBlock));
            codeStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            codeStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.LightBlue)));
            codeStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 4, 8, 4)));
            codeStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            codeColumn.ElementStyle = codeStyle;
            dataGrid.Columns.Add(codeColumn);

            // Désignation
            var designationColumn = new DataGridTextColumn
            {
                Header = "Désignation",
                Binding = new Binding("Designation"),
                Width = DataGridLength.Auto,
                MinWidth = 180,
                MaxWidth = 250
            };
            var designationStyle = new Style(typeof(TextBlock));
            designationStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            designationStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 4, 8, 4)));
            designationColumn.ElementStyle = designationStyle;
            dataGrid.Columns.Add(designationColumn);

            // Quantité
            var quantiteColumn = new DataGridTextColumn
            {
                Header = "Quantité",
                Binding = new Binding("Quantite"),
                Width = DataGridLength.Auto,
                MinWidth = 80
            };
            var quantiteStyle = new Style(typeof(TextBlock));
            quantiteStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            quantiteStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            quantiteStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 4, 8, 4)));
            quantiteColumn.ElementStyle = quantiteStyle;
            dataGrid.Columns.Add(quantiteColumn);

            // Prix unitaire
            var prixColumn = new DataGridTextColumn
            {
                Header = "Prix Unitaire",
                Binding = new Binding("PrixUnitaire") { StringFormat = "C2" },
                Width = DataGridLength.Auto,
                MinWidth = 100
            };
            var prixStyle = new Style(typeof(TextBlock));
            prixStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
            prixStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.LightGreen)));
            prixStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 4, 8, 4)));
            prixColumn.ElementStyle = prixStyle;
            dataGrid.Columns.Add(prixColumn);

            // Montant HT
            var montantColumn = new DataGridTextColumn
            {
                Header = "Montant HT",
                Binding = new Binding("MontantHt") { StringFormat = "C2" },
                Width = DataGridLength.Auto,
                MinWidth = 110
            };
            var montantStyle = new Style(typeof(TextBlock));
            montantStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
            montantStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            montantStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.Gold)));
            montantStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 4, 8, 4)));
            montantColumn.ElementStyle = montantStyle;
            dataGrid.Columns.Add(montantColumn);

            // TVA
            var tvaColumn = new DataGridTextColumn
            {
                Header = "Code TVA",
                Binding = new Binding("CodeTva"),
                Width = DataGridLength.Auto,
                MinWidth = 80
            };
            var tvaStyle = new Style(typeof(TextBlock));
            tvaStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            tvaStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.Orange)));
            tvaStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 4, 8, 4)));
            tvaColumn.ElementStyle = tvaStyle;
            dataGrid.Columns.Add(tvaColumn);
        }
    }
}
