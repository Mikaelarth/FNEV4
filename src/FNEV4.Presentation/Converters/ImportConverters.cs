using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using FNEV4.Core.Models.ImportTraitement;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertit un boolean en icône Material Design
    /// </summary>
    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "CheckCircle" : "AlertCircle";
            }
            return "Help";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un boolean en couleur
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "#2E7D32" : "#D32F2F"; // Dark Green or Dark Red for better visibility
            }
            return "#757575"; // Dark Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un boolean en état textuel
    /// </summary>
    public class BoolToStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Réussi" : "Échec";
            }
            return "Inconnu";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un boolean en texte de statut
    /// </summary>
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Valide" : "Erreur";
            }
            return "Inconnu";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un boolean en Oui/Non
    /// </summary>
    public class BoolToYesNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Oui" : "Non";
            }
            return "Inconnu";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un boolean en couleur de fond claire
    /// </summary>
    public class BoolToLightColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return new SolidColorBrush(boolValue ? 
                    Color.FromArgb(50, 46, 125, 50) :     // Light green with more opacity
                    Color.FromArgb(50, 211, 47, 47));     // Light red with more opacity
            }
            return new SolidColorBrush(Color.FromArgb(40, 117, 117, 117)); // Light gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit le statut de traitement en texte pour le bouton
    /// </summary>
    public class ProcessingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isProcessing)
            {
                return isProcessing ? "Validation..." : "Valider";
            }
            return "Valider";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverse un boolean
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit plusieurs boolean avec un ET logique
    /// </summary>
    public class BooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is bool boolValue && !boolValue)
                {
                    return false;
                }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une liste de produits en tooltip formaté
    /// </summary>
    public class ProduitsToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Debug: Vérifier le type de données reçu
                if (value == null)
                {
                    return "❌ Aucune donnée (null)";
                }

                // Gestion spécifique du type List<Sage100ProduitData>
                if (value is List<Sage100ProduitData> produitsSage100)
                {
                    if (!produitsSage100.Any())
                    {
                        return "📦 Aucun article détecté";
                    }

                    var tooltip = "📦 DÉTAIL DES ARTICLES\n\n";
                    
                    foreach (var produit in produitsSage100.Take(10)) // Limiter à 10 pour éviter un tooltip trop long
                    {
                        tooltip += $"• {produit.Designation}\n";
                        tooltip += $"  Code: {produit.CodeProduit} | Qté: {produit.Quantite:N0} {produit.Emballage}\n";
                        tooltip += $"  Prix: {produit.PrixUnitaire:N0} | TVA: {produit.CodeTva}\n";
                        tooltip += $"  Total: {produit.MontantHt:N0} FCFA\n\n";
                    }
                    
                    if (produitsSage100.Count > 10)
                    {
                        tooltip += $"... et {produitsSage100.Count - 10} autres articles";
                    }
                    
                    return tooltip.Trim();
                }

                // Gestion des autres types de listes de produits
                IEnumerable<object> produits = null;
                
                if (value is IEnumerable<object> produitsGeneric)
                {
                    produits = produitsGeneric;
                }
                else if (value is System.Collections.IEnumerable enumerable)
                {
                    produits = enumerable.Cast<object>();
                }
                else
                {
                    return $"❌ Type non supporté: {value.GetType().Name}";
                }

                if (produits == null || !produits.Any())
                {
                    return "📦 Aucun article détecté";
                }

                var tooltipGeneric = "📦 DÉTAIL DES ARTICLES\n\n";
                int count = 0;
                
                foreach (var item in produits.Take(10)) // Limiter à 10 pour éviter un tooltip trop long
                {
                    if (item == null) continue;
                    
                    count++;
                    
                    // Utiliser la réflexion pour accéder aux propriétés
                    var type = item.GetType();
                    var designation = type.GetProperty("Designation")?.GetValue(item)?.ToString() ?? "N/A";
                    var codeProduit = type.GetProperty("CodeProduit")?.GetValue(item)?.ToString() ?? "N/A";
                    var quantite = type.GetProperty("Quantite")?.GetValue(item) ?? 0;
                    var emballage = type.GetProperty("Emballage")?.GetValue(item)?.ToString() ?? "";
                    var prixUnitaire = type.GetProperty("PrixUnitaire")?.GetValue(item) ?? 0;
                    var codeTva = type.GetProperty("CodeTva")?.GetValue(item)?.ToString() ?? "N/A";
                    var montantHt = type.GetProperty("MontantHt")?.GetValue(item) ?? 0;
                    
                    tooltipGeneric += $"• {designation}\n";
                    tooltipGeneric += $"  Code: {codeProduit} | Qté: {quantite:N0} {emballage}\n";
                    tooltipGeneric += $"  Prix: {prixUnitaire:N0} | TVA: {codeTva}\n";
                    tooltipGeneric += $"  Total: {montantHt:N0} FCFA\n\n";
                }
                
                var totalCount = produits.Count();
                if (totalCount > 10)
                {
                    tooltipGeneric += $"... et {totalCount - 10} autres articles";
                }
                
                return tooltipGeneric.Trim();
            }
            catch (Exception ex)
            {
                return $"❌ Erreur tooltip: {ex.Message}";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
