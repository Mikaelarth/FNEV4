using System;
using System.Globalization;
using System.Windows.Data;

namespace FNEV4.Presentation.Views.ImportTraitement
{
    /// <summary>
    /// Converter pour afficher la description des types de TVA selon les spécifications FNE
    /// </summary>
    public class TvaDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string codeTva)
                return string.Empty;

            return codeTva.ToUpper() switch
            {
                "TVA" => "TVA normal (18%)",
                "TVAB" => "TVA réduit (9%)",
                "TVAC" => "TVA exec conv (0%)",
                "TVAD" => "TVA exec leg (0%)",
                _ => "Type inconnu"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
