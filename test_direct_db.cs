using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FNEV4.Infrastructure.Data;
using FNEV4.Core.Entities;
using FNEV4.Infrastructure.Services;

namespace FNEV4.Tests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🧪 TEST DIRECT - Création FneInvoice en base de données");
            Console.WriteLine("=" * 65);
            
            try
            {
                // 1. Configuration et DbContext comme dans l'app
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();
                
                var pathService = new PathConfigurationService(configuration);
                pathService.EnsureDirectoriesExist();
                
                Console.WriteLine($"📁 Chemin de la base : {pathService.DatabasePath}");
                Console.WriteLine($"   Existe avant test : {File.Exists(pathService.DatabasePath)}");
                
                // 2. Créer le DbContext comme dans l'application
                var optionsBuilder = new DbContextOptionsBuilder<FNEV4DbContext>();
                var connectionString = $"Data Source={pathService.DatabasePath}";
                optionsBuilder.UseSqlite(connectionString);
                
                Console.WriteLine($"🔗 Connection String : {connectionString}");
                
                // 3. Test avec DbContext
                using var context = new FNEV4DbContext(optionsBuilder.Options);
                
                // Assurer que la DB existe
                await context.Database.EnsureCreatedAsync();
                Console.WriteLine("✅ Base de données créée/vérifiée");
                
                // 4. Créer une ImportSession de test
                var importSession = new ImportSession
                {
                    Id = Guid.NewGuid(),
                    FileName = "test_manual.xlsx",
                    FilePath = "C:\\test\\test_manual.xlsx",
                    FileSize = 1024,
                    StartedAt = DateTime.Now,
                    Status = "InProgress",
                    UserName = "TestUser"
                };
                
                context.ImportSessions.Add(importSession);
                await context.SaveChangesAsync();
                Console.WriteLine($"✅ ImportSession créée : {importSession.Id}");
                
                // 5. Créer une facture de test
                var fneInvoice = new FneInvoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = "TEST-001",
                    InvoiceDate = DateTime.Today,
                    DueDate = DateTime.Today.AddDays(30),
                    ClientCode = "TEST",
                    ClientName = "Client Test",
                    TotalAmountExclTax = 100.00m,
                    TotalAmountInclTax = 120.00m,
                    TotalTaxAmount = 20.00m,
                    PaymentMethod = "VIREMENT",
                    Currency = "EUR",
                    InvoiceType = "B2B",
                    VatType = "TVA",
                    Status = "Draft",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    ImportSessionId = importSession.Id,
                    Items = new List<FneInvoiceItem>
                    {
                        new FneInvoiceItem
                        {
                            Id = Guid.NewGuid(),
                            ProductCode = "PROD001",
                            ProductName = "Produit Test",
                            Quantity = 1,
                            UnitPrice = 100.00m,
                            TotalAmountExclTax = 100.00m,
                            TaxRate = 20.00m,
                            TaxAmount = 20.00m,
                            TotalAmountInclTax = 120.00m
                        }
                    }
                };
                
                Console.WriteLine($"💼 Création facture : {fneInvoice.InvoiceNumber}");
                context.FneInvoices.Add(fneInvoice);
                await context.SaveChangesAsync();
                
                Console.WriteLine($"✅ Facture sauvée : {fneInvoice.Id}");
                
                // 6. Finaliser la session
                importSession.CompletedAt = DateTime.Now;
                importSession.Status = "Completed";
                importSession.InvoicesImported = 1;
                importSession.ErrorsCount = 0;
                importSession.TotalInvoicesFound = 1;
                
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Session finalisée");
                
                // 7. Vérification
                var countInvoices = await context.FneInvoices.CountAsync();
                var countSessions = await context.ImportSessions.CountAsync();
                var countItems = await context.FneInvoiceItems.CountAsync();
                
                Console.WriteLine($"\n📊 RÉSULTATS :");
                Console.WriteLine($"   FneInvoices : {countInvoices}");
                Console.WriteLine($"   ImportSessions : {countSessions}");
                Console.WriteLine($"   FneInvoiceItems : {countItems}");
                
                // 8. Lister les factures
                var invoices = await context.FneInvoices
                    .Include(i => i.Items)
                    .ToListAsync();
                    
                Console.WriteLine($"\n📋 DÉTAIL FACTURES :");
                foreach (var inv in invoices)
                {
                    Console.WriteLine($"   {inv.InvoiceNumber} - {inv.ClientName} - {inv.TotalAmountInclTax:C} - {inv.Items.Count} items");
                }
                
                Console.WriteLine("\n🎉 TEST RÉUSSI - La persistence fonctionne !");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR : {ex.Message}");
                Console.WriteLine($"   Stack : {ex.StackTrace}");
            }
            
            Console.WriteLine("\nAppuyez sur une touche pour continuer...");
            Console.ReadKey();
        }
    }
}
