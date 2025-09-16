
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FNEV4.Infrastructure.Data;
using FNEV4.Infrastructure.Repositories;

namespace TestRepository
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Test direct du FneInvoiceRepository ===");
            
            try
            {
                // Configuration du DbContext comme dans l'application
                var dbPath = @"d:\PROJET\FNE\FNEV4\data\FNEV4.db";
                var connectionString = $"Data Source={dbPath};Cache=Shared";
                
                var optionsBuilder = new DbContextOptionsBuilder<FNEV4DbContext>();
                optionsBuilder.UseSqlite(connectionString);
                
                using var context = new FNEV4DbContext(optionsBuilder.Options);
                var repository = new FneInvoiceRepository(context);
                
                Console.WriteLine("✅ Repository créé avec succès");
                
                // Test de la méthode GetAvailableForCertificationAsync
                Console.WriteLine("🔍 Appel GetAvailableForCertificationAsync...");
                var invoices = await repository.GetAvailableForCertificationAsync();
                
                Console.WriteLine($"✅ Méthode exécutée - {invoices.Count()} factures récupérées");
                
                foreach (var invoice in invoices.Take(3))
                {
                    Console.WriteLine($"  - {invoice.InvoiceNumber}: {invoice.TotalAmountTTC:C} - Client: {invoice.Client?.CompanyName ?? "N/A"}");
                }
                
                if (invoices.Any())
                {
                    Console.WriteLine("✅ SUCCÈS: Des factures sont disponibles pour certification");
                }
                else
                {
                    Console.WriteLine("❌ PROBLÈME: Aucune facture trouvée");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("\nAppuyez sur une touche pour quitter...");
            Console.ReadKey();
        }
    }
}
