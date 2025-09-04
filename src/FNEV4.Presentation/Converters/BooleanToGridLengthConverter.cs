using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertisseur pour adapter la largeur du menu selon son état d'expansion
    /// </summary>
    public class BooleanToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isExpanded && parameter is string parameterString)
            {
                var parameters = parameterString.Split(',');
                if (parameters.Length == 2)
                {
                    if (double.TryParse(parameters[0], out double expandedWidth) &&
                        double.TryParse(parameters[1], out double collapsedWidth))
                    {
                        return new GridLength(isExpanded ? expandedWidth : collapsedWidth);
                    }
                }
            }

            return new GridLength(280); // Valeur par défaut
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
