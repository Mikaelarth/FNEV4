using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertisseur pour ajuster la luminosité d'une couleur
    /// </summary>
    public class ColorBrightnessConverter : IValueConverter
    {
        public static readonly ColorBrightnessConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color && parameter is string factorStr && double.TryParse(factorStr, out double factor))
            {
                // Appliquer un facteur de luminosité
                byte r = (byte)Math.Min(255, color.R * factor);
                byte g = (byte)Math.Min(255, color.G * factor);
                byte b = (byte)Math.Min(255, color.B * factor);
                
                return Color.FromArgb(color.A, r, g, b);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
