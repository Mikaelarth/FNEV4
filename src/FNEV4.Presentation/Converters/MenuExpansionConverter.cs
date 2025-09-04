using System;
using System.Globalization;
using System.Windows.Data;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertisseur pour gérer l'expansion des sous-menus selon l'état du menu principal
    /// </summary>
    public class MenuExpansionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && 
                values[0] is bool isMenuExpanded && 
                values[1] is bool areSectionExpanded)
            {
                // Les sections ne peuvent être ouvertes que si le menu principal est ouvert
                return isMenuExpanded && areSectionExpanded;
            }
            
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
