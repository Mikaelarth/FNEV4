using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FNEV4.Presentation.Converters
{
    /// <summary>
    /// Convertisseur qui inverse la logique du BooleanToVisibilityConverter
    /// True -> Hidden/Collapsed, False -> Visible
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convertit un booléen vers une visibilité (inversée)
        /// </summary>
        /// <param name="value">Valeur booléenne</param>
        /// <param name="targetType">Type cible (Visibility)</param>
        /// <param name="parameter">Paramètre optionnel pour spécifier Collapsed ou Hidden</param>
        /// <param name="culture">Culture</param>
        /// <returns>Visibility.Visible si false, Visibility.Collapsed/Hidden si true</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Inverser la logique : false = Visible, true = Hidden/Collapsed
                if (!boolValue)
                {
                    return Visibility.Visible;
                }
                else
                {
                    // Vérifier si le paramètre spécifie le type de masquage
                    if (parameter?.ToString()?.ToLower() == "hidden")
                    {
                        return Visibility.Hidden;
                    }
                    else
                    {
                        return Visibility.Collapsed;
                    }
                }
            }

            // Par défaut, retourner Collapsed pour des valeurs non-booléennes
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Convertit une visibilité vers un booléen (inversé)
        /// </summary>
        /// <param name="value">Valeur Visibility</param>
        /// <param name="targetType">Type cible (bool)</param>
        /// <param name="parameter">Paramètre (non utilisé)</param>
        /// <param name="culture">Culture</param>
        /// <returns>false si Visible, true si Hidden/Collapsed</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // Inverser la logique : Visible = false, Hidden/Collapsed = true
                return visibility != Visibility.Visible;
            }

            return false;
        }
    }
}
