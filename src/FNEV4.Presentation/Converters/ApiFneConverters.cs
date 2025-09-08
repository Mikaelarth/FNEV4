using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertisseur Boolean vers PackIconKind pour la validation DGI
    /// </summary>
    public class BooleanToPackIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isValidated)
            {
                return isValidated ? PackIconKind.CheckDecagram : PackIconKind.ClockOutline;
            }
            return PackIconKind.ClockOutline;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur Boolean vers couleur pour la validation DGI
    /// </summary>
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isValidated)
            {
                return isValidated ? Brushes.Green : Brushes.Orange;
            }
            return Brushes.Orange;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur Boolean vers texte de validation DGI
    /// </summary>
    public class BooleanToValidationTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isValidated)
            {
                return isValidated ? "✅ Intégration validée par la DGI" : "⏳ En attente de validation DGI";
            }
            return "⏳ En attente de validation DGI";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertisseur null vers Visibility
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
