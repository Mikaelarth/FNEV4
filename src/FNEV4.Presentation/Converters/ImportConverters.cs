using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

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
                return boolValue ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Green or Red
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
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
                    Color.FromArgb(30, 76, 175, 80) :    // Light green
                    Color.FromArgb(30, 244, 67, 54));    // Light red
            }
            return new SolidColorBrush(Color.FromArgb(30, 158, 158, 158)); // Light gray
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
            if (value is List<object> produits && produits.Any())
            {
                var tooltip = "📦 DÉTAIL DES ARTICLES\n\n";
                foreach (var item in produits.Take(10)) // Limiter à 10 pour éviter un tooltip trop long
                {
                    // Utiliser la réflexion pour accéder aux propriétés
                    var type = item.GetType();
                    var designation = type.GetProperty("Designation")?.GetValue(item)?.ToString() ?? "N/A";
                    var codeProduit = type.GetProperty("CodeProduit")?.GetValue(item)?.ToString() ?? "N/A";
                    var quantite = type.GetProperty("Quantite")?.GetValue(item) ?? 0;
                    var emballage = type.GetProperty("Emballage")?.GetValue(item)?.ToString() ?? "";
                    var prixUnitaire = type.GetProperty("PrixUnitaire")?.GetValue(item) ?? 0;
                    var codeTva = type.GetProperty("CodeTva")?.GetValue(item)?.ToString() ?? "N/A";
                    var montantHt = type.GetProperty("MontantHt")?.GetValue(item) ?? 0;
                    
                    tooltip += $"• {designation}\n";
                    tooltip += $"  Code: {codeProduit} | Qté: {quantite:N0} {emballage}\n";
                    tooltip += $"  Prix: {prixUnitaire:N0} | TVA: {codeTva}\n";
                    tooltip += $"  Total: {montantHt:N0} FCFA\n\n";
                }
                
                if (produits.Count > 10)
                {
                    tooltip += $"... et {produits.Count - 10} autres articles";
                }
                
                return tooltip.Trim();
            }
            return "Aucun détail disponible";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
