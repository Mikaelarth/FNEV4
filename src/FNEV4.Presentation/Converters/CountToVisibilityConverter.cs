using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertit un count (nombre) en Visibility
    /// Retourne Visible si le count > 0, sinon Collapsed
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Visibility.Collapsed;

            // Si c'est une collection
            if (value is ICollection collection)
            {
                return collection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            // Si c'est un nombre
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            // Si c'est convertible en int
            if (int.TryParse(value.ToString(), out int parsedCount))
            {
                return parsedCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
