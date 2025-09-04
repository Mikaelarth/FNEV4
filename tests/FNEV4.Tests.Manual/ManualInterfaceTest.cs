using System;
using System.Threading;
using System.Windows;
using FNEV4.Presentation;
using FNEV4.Presentation.Views;

namespace FNEV4.Tests.Manual
{
    /// <summary>
    /// Application de test manuel pour valider l'interface utilisateur
    /// Ce test lance l'application et permet une validation visuelle
    /// </summary>
    public static class ManualInterfaceTest
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("=== FNEV4 - Test Manuel de l'Interface ===");
            Console.WriteLine("Lancement de l'application pour validation visuelle...");
            Console.WriteLine();
            
            try
            {
                // Configuration de l'application WPF pour le test
                var app = new Application();
                
                // Création de la fenêtre principale
                var mainWindow = new MainWindow();
                
                Console.WriteLine("✅ Fenêtre principale créée avec succès");
                Console.WriteLine("📋 Points à vérifier :");
                Console.WriteLine("   • Menu de navigation avec 9 modules");
                Console.WriteLine("   • Sous-menus pour chaque module");
                Console.WriteLine("   • Barre de titre avec informations");
                Console.WriteLine("   • Barre de statut");
                Console.WriteLine("   • Bouton toggle du menu");
                Console.WriteLine("   • Navigation fonctionnelle");
                Console.WriteLine();
                Console.WriteLine("🔍 Test en cours... Fermez la fenêtre pour terminer.");
                
                // Lancement de l'application
                app.Run(mainWindow);
                
                Console.WriteLine("✅ Test terminé avec succès");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors du test : {ex.Message}");
                Console.WriteLine($"   Stack trace : {ex.StackTrace}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Appuyez sur une touche pour continuer...");
            Console.ReadKey();
        }
    }
}
