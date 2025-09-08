using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertit un statut de dossier en Brush pour l'affichage visuel
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    // Valeurs du ViewModel
                    "Valid" => Brushes.Green,
                    "Warning" => Brushes.Orange,
                    "Invalid" => Brushes.Red,
                    "Unknown" => Brushes.LightGray,
                    
                    // Valeurs formatées héritées (pour compatibilité)
                    "✅ Configuré" => Brushes.Green,
                    "✅ Actif" => Brushes.LimeGreen,
                    "✅ Connecté" => Brushes.Green,
                    "⚠️ Attention" => Brushes.Orange,
                    "⚠️ Inactif" => Brushes.Orange,
                    "❌ Erreur" => Brushes.Red,
                    "❌ Non configuré" => Brushes.Red,
                    "⏳ En cours" => Brushes.Blue,
                    "🔄 Synchronisation" => Brushes.CornflowerBlue,
                    "⏸️ Suspendu" => Brushes.Gray,
                    _ => Brushes.LightGray
                };
            }
            return Brushes.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
