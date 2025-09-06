using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertit le type de client en couleur pour l'affichage
    /// </summary>
    public class ClientTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string clientType)
            {
                return clientType switch
                {
                    "B2B" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),   // Bleu pour Business
                    "B2C" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Vert pour Consumer
                    "B2G" => new SolidColorBrush(Color.FromRgb(156, 39, 176)),   // Violet pour Government
                    "B2F" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),    // Orange pour Foreign/International
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))       // Gris par défaut
                };
            }

            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
