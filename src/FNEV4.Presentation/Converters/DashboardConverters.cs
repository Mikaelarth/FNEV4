using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using FNEV4.Presentation.ViewModels.Dashboard;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertisseur pour transformer le type d'alerte en couleur appropriée
    /// </summary>
    public class AlerteTypeToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || values[0] is not TypeAlerte type || values[1] is not bool estCritique)
                return new SolidColorBrush(Colors.Gray);

            return type switch
            {
                TypeAlerte.Information => new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)), // Bleu
                TypeAlerte.Avertissement => estCritique 
                    ? new SolidColorBrush(Color.FromRgb(0xFF, 0x57, 0x22)) // Orange foncé
                    : new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)), // Orange
                TypeAlerte.Erreur => estCritique 
                    ? new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)) // Rouge foncé
                    : new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)), // Rouge
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur pour transformer le type d'alerte en icône appropriée
    /// </summary>
    public class AlerteTypeToIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || values[0] is not TypeAlerte type || values[1] is not bool estCritique)
                return PackIconKind.Information;

            return type switch
            {
                TypeAlerte.Information => PackIconKind.Information,
                TypeAlerte.Avertissement => estCritique 
                    ? PackIconKind.AlertCircle 
                    : PackIconKind.Alert,
                TypeAlerte.Erreur => estCritique 
                    ? PackIconKind.AlertOctagon 
                    : PackIconKind.AlertCircle,
                _ => PackIconKind.Information
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur pour transformer une valeur booléenne en couleur (positif/négatif)
    /// Spécifique au Dashboard pour éviter les conflits
    /// </summary>
    public class DashboardBooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPositive)
            {
                return isPositive 
                    ? Color.FromRgb(0x2E, 0x7D, 0x32) // Vert pour positif
                    : Color.FromRgb(0xD3, 0x2F, 0x2F); // Rouge pour négatif
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur pour transformer un null en visibilité
    /// Spécifique au Dashboard pour éviter les conflits
    /// </summary>
    public class DashboardNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
