
using System;
using System.Threading.Tasks;
using FNEV4.Application.Services.GestionFactures;
using FNEV4.Infrastructure.Data.Repositories;
using FNEV4.Infrastructure.Data;

namespace TestImportSystem
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== TEST SYSTÈME D'IMPORT AVEC RÉSOLUTION DES CONFLITS CLIENT ===");
            
            try
            {
                // Simuler le repository (en vrai on utiliserait la DI)
                var dbContext = new ApplicationDbContext(); // À adapter selon votre config
                var clientRepository = new ClientRepository(dbContext);
                
                // Créer le service d'import avec résolution des conflits
                var importService = new InvoiceExcelImportService(clientRepository);
                
                // Tester avec factures.xlsx
                string filePath = "factures.xlsx";
                if (System.IO.File.Exists(filePath))
                {
                    Console.WriteLine($"Test avec {filePath}...");
                    
                    var result = await importService.ImportInvoicesFromExcelAsync(filePath);
                    
                    Console.WriteLine($"Résultat import:");
                    Console.WriteLine($"- Fichier source: {result.SourceFile}");
                    Console.WriteLine($"- Succès: {result.IsSuccess}");
                    Console.WriteLine($"- Factures trouvées: {result.ImportedInvoices.Count}");
                    Console.WriteLine($"- Erreurs globales: {result.GlobalErrors.Count}");
                    
                    foreach (var invoice in result.ImportedInvoices)
                    {
                        Console.WriteLine($"\nFacture {invoice.InvoiceNumber}:");
                        Console.WriteLine($"  - Client: {invoice.CustomerCode} - {invoice.CustomerTitle}");
                        Console.WriteLine($"  - NCC: {invoice.Ncc}");
                        Console.WriteLine($"  - Moyen paiement: {invoice.PaymentMethod}");
                        Console.WriteLine($"  - Valide: {invoice.IsValid}");
                        Console.WriteLine($"  - Erreurs: {invoice.ValidationErrors.Count}");
                        
                        if (invoice.ValidationErrors.Count > 0)
                        {
                            foreach (var error in invoice.ValidationErrors)
                            {
                                Console.WriteLine($"    * {error}");
                            }
                        }
                    }
                    
                    if (result.GlobalErrors.Count > 0)
                    {
                        Console.WriteLine("\nErreurs globales:");
                        foreach (var error in result.GlobalErrors)
                        {
                            Console.WriteLine($"  - {error}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Fichier factures.xlsx non trouvé");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("\n=== FIN DU TEST ===");
        }
    }
}
