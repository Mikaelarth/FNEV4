using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Converter pour transformer un code TVA en couleur de fond
    /// </summary>
    public class TvaCodeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string codeTva)
                return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rouge pour erreur

            return codeTva.ToUpper() switch
            {
                "TVA" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Vert - TVA normal 18%
                "TVAB" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),  // Orange - TVA réduit 9%
                "TVAC" => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gris - TVA exec conv 0%
                "TVAD" => new SolidColorBrush(Color.FromRgb(96, 125, 139)),  // Gris bleu - TVA exec leg 0%
                "" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),      // Rouge - Code vide
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))       // Gris par défaut
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}