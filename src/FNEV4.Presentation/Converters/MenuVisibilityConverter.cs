using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertisseur pour contrôler la visibilité des sous-menus
    /// Visible seulement si le menu principal est étendu ET la section est ouverte
    /// </summary>
    public class MenuVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && 
                values[0] is bool isMenuExpanded && 
                values[1] is bool isSectionExpanded)
            {
                return (isMenuExpanded && isSectionExpanded) ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
