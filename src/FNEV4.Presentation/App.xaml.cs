using System.Windows;

namespace FNEV4.Presentation
{
    /// <summary>
    /// Application principale FNEV4
    /// Gestion du démarrage et configuration globale
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Configuration Material Design
            ConfigureMaterialDesign();
            
            // Configuration de l'application
            ConfigureApplication();
        }

        private void ConfigureMaterialDesign()
        {
            // Configuration du thème Material Design
            // Couleurs DGI : Bleu principal, Orange secondaire
        }

        private void ConfigureApplication()
        {
            // Configuration générale de l'application
            // Logging, DI, etc. (à implémenter plus tard)
        }
    }
}
