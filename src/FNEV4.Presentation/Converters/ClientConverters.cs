using System;
using System.Globalization;
using System.Windows.Data;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertisseur pour transformer un booléen en statut textuel
    /// </summary>
    public class BooleanToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Actif" : "Inactif";
            }
            return "Inconnu";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
