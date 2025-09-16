using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using FNEV4.Presentation.ViewModels.CertificationFne;

namespace FNEV4.Presentation.Views.CertificationFne
{
    /// <summary>
    /// Vue pour la certification manuelle des factures FNE
    /// </summary>
    public partial class CertificationManuelleView : UserControl
    {
        public CertificationManuelleView()
        {
            InitializeComponent();
            
            // Utiliser le VRAI ViewModel avec injection de dépendances (comme BaseDonneesView)
            try
            {
                DataContext = App.ServiceProvider.GetRequiredService<CertificationManuelleViewModel>();
                System.Diagnostics.Debug.WriteLine("=== DEBUG: CertificationManuelleViewModel injecté avec succès ===");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== DEBUG: Erreur injection ViewModel: {ex.Message} ===");
                // Ne pas créer de fallback - si l'injection échoue, on veut le savoir
                throw;
            }
        }
    }
}