using System;
using System.IO;
using ClosedXML.Excel;
using FNEV4.Application.Services.GestionFactures;

namespace FNEV4
{
    /// <summary>
    /// Test du service d'import des factures Sage 100 v15
    /// Crée un fichier Excel de test et teste l'import
    /// </summary>
    class TestInvoiceImport
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Test Import Factures Sage 100 v15 ===\n");

            try
            {
                // 1. Créer un fichier Excel de test
                var testFilePath = CreateTestExcelFile();
                Console.WriteLine($"✅ Fichier de test créé : {testFilePath}\n");

                // 2. Tester la validation du fichier
                var importService = new InvoiceExcelImportService();
                var validationResult = importService.ValidateExcelFile(testFilePath);
                
                Console.WriteLine("📋 Résultat validation :");
                Console.WriteLine($"   Valide : {validationResult.IsValid}");
                foreach (var error in validationResult.Errors)
                {
                    Console.WriteLine($"   ❌ {error}");
                }
                foreach (var message in validationResult.Messages)
                {
                    Console.WriteLine($"   ✅ {message}");
                }
                Console.WriteLine();

                // 3. Tester l'import
                if (validationResult.IsValid)
                {
                    Console.WriteLine("🔄 Lancement de l'import...");
                    var importResult = importService.ImportInvoicesFromExcel(testFilePath);
                    
                    Console.WriteLine("\n📊 Résultat d'import :");
                    Console.WriteLine($"   Fichier : {importResult.SourceFile}");
                    Console.WriteLine($"   Durée : {importResult.Duration.TotalSeconds:F2}s");
                    Console.WriteLine($"   Succès global : {importResult.IsSuccess}");
                    Console.WriteLine($"   Factures trouvées : {importResult.TotalInvoices}");
                    Console.WriteLine($"   Factures valides : {importResult.ValidInvoices}");
                    Console.WriteLine($"   Factures invalides : {importResult.InvalidInvoices}");
                    
                    // Erreurs globales
                    if (importResult.GlobalErrors.Any())
                    {
                        Console.WriteLine("\n❌ Erreurs globales :");
                        foreach (var error in importResult.GlobalErrors)
                        {
                            Console.WriteLine($"   - {error}");
                        }
                    }

                    // Détail des factures
                    Console.WriteLine("\n📝 Détail des factures :");
                    foreach (var invoice in importResult.ImportedInvoices)
                    {
                        Console.WriteLine($"\n   📄 Facture {invoice.InvoiceNumber}");
                        Console.WriteLine($"      Client : {invoice.CustomerCode} - {invoice.CustomerTitle}");
                        Console.WriteLine($"      Date : {invoice.InvoiceDate:dd/MM/yyyy}");
                        Console.WriteLine($"      Paiement : {invoice.PaymentMethod}");
                        Console.WriteLine($"      Lignes : {invoice.Items.Count}");
                        Console.WriteLine($"      Valide : {(invoice.IsValid ? "✅" : "❌")}");
                        
                        if (!invoice.IsValid)
                        {
                            Console.WriteLine("      Erreurs :");
                            foreach (var error in invoice.ValidationErrors)
                            {
                                Console.WriteLine($"        - {error}");
                            }
                        }
                        else
                        {
                            // Calculer les totaux
                            var totalHT = invoice.Items.Sum(i => i.AmountHT);
                            var totalTTC = invoice.Items.Sum(i => i.AmountTTC);
                            Console.WriteLine($"      Total HT : {totalHT:C}");
                            Console.WriteLine($"      Total TTC : {totalTTC:C}");
                        }
                    }
                }

                // 4. Nettoyer
                File.Delete(testFilePath);
                Console.WriteLine($"\n🧹 Fichier de test supprimé");

                Console.WriteLine("\n✅ Test terminé avec succès !");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Erreur critique : {ex.Message}");
                Console.WriteLine($"Stack trace : {ex.StackTrace}");
            }

            Console.WriteLine("\nAppuyez sur une touche pour terminer...");
            Console.ReadKey();
        }

        /// <summary>
        /// Crée un fichier Excel de test avec structure Sage 100 v15
        /// </summary>
        static string CreateTestExcelFile()
        {
            var fileName = Path.Combine(Path.GetTempPath(), $"test_sage100_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            
            using var workbook = new XLWorkbook();

            // Facture 1 : Client normal avec carte
            CreateTestInvoiceSheet(workbook, "FAC001", new
            {
                InvoiceNumber = "FAC-2024-001",
                CustomerCode = "CLI001", 
                CustomerNcc = "12345678A",
                CustomerTitle = "ENTREPRISE ABC SARL",
                Date = DateTime.Today,
                PointOfSale = "MAGASIN-01",
                PaymentMethod = "card",
                Items = new[]
                {
                    new { Code = "PROD001", Description = "Ordinateur portable", Price = 850000m, Qty = 1m, Unit = "pcs", VatType = "TVA" },
                    new { Code = "PROD002", Description = "Souris sans fil", Price = 25000m, Qty = 2m, Unit = "pcs", VatType = "TVA" },
                    new { Code = "SERV001", Description = "Installation logiciel", Price = 150000m, Qty = 1m, Unit = "heure", VatType = "TVAB" }
                }
            });

            // Facture 2 : Client divers avec espèces
            CreateTestInvoiceSheet(workbook, "FAC002", new
            {
                InvoiceNumber = "FAC-2024-002",
                CustomerCode = "1999", // Client divers
                CustomerNcc = "",
                CustomerTitle = "CLIENT DIVERS",
                CustomerRealName = "MARTIN Jean",
                CustomerDiversNcc = "98765432B",
                Date = DateTime.Today.AddDays(-1),
                PointOfSale = "MAGASIN-02", 
                PaymentMethod = "cash",
                Items = new[]
                {
                    new { Code = "PROD003", Description = "Clavier mécanique", Price = 75000m, Qty = 1m, Unit = "pcs", VatType = "TVA" },
                    new { Code = "PROD004", Description = "Écran 24 pouces", Price = 320000m, Qty = 1m, Unit = "pcs", VatType = "TVA" }
                }
            });

            // Facture 3 : Avoir avec mobile money
            CreateTestInvoiceSheet(workbook, "AVO001", new
            {
                InvoiceNumber = "AVO-2024-001",
                CustomerCode = "CLI002",
                CustomerNcc = "11111111C", 
                CustomerTitle = "TECH SOLUTIONS",
                Date = DateTime.Today,
                PointOfSale = "MAGASIN-01",
                PaymentMethod = "mobile-money",
                CreditNoteRef = "FAC-2024-003",
                Items = new[]
                {
                    new { Code = "PROD001", Description = "Ordinateur portable (retour)", Price = -850000m, Qty = 1m, Unit = "pcs", VatType = "TVA" }
                }
            });

            workbook.SaveAs(fileName);
            return fileName;
        }

        /// <summary>
        /// Crée une feuille de facture avec la structure Sage 100 v15
        /// </summary>
        static void CreateTestInvoiceSheet(XLWorkbook workbook, string sheetName, dynamic invoiceData)
        {
            var worksheet = workbook.Worksheets.Add(sheetName);

            // En-tête facture (Colonne A)
            worksheet.Cell("A3").Value = invoiceData.InvoiceNumber;  // Numéro facture
            worksheet.Cell("A5").Value = invoiceData.CustomerCode;   // Code client
            worksheet.Cell("A6").Value = invoiceData.CustomerNcc;    // NCC client normal
            worksheet.Cell("A8").Value = invoiceData.Date;           // Date facture
            worksheet.Cell("A10").Value = invoiceData.PointOfSale;   // Point de vente
            worksheet.Cell("A11").Value = invoiceData.CustomerTitle; // Intitulé client
            
            // Client divers
            if (invoiceData.CustomerCode == "1999")
            {
                worksheet.Cell("A13").Value = invoiceData.CustomerRealName;   // Nom réel client divers
                worksheet.Cell("A15").Value = invoiceData.CustomerDiversNcc;  // NCC client divers
            }

            // Avoir
            if (invoiceData.CreditNoteRef != null)
            {
                worksheet.Cell("A17").Value = invoiceData.CreditNoteRef; // Référence facture originale
            }

            // Moyen de paiement (NOUVEAU)
            worksheet.Cell("A18").Value = invoiceData.PaymentMethod;

            // Lignes produits (à partir ligne 20)
            int row = 20;
            foreach (var item in invoiceData.Items)
            {
                worksheet.Cell($"B{row}").Value = item.Code;           // Code produit
                worksheet.Cell($"C{row}").Value = item.Description;    // Désignation
                worksheet.Cell($"D{row}").Value = item.Price;          // Prix unitaire
                worksheet.Cell($"E{row}").Value = item.Qty;            // Quantité
                worksheet.Cell($"F{row}").Value = item.Unit;           // Unité
                worksheet.Cell($"G{row}").Value = item.VatType;        // Type TVA
                worksheet.Cell($"H{row}").Value = item.Price * item.Qty; // Montant HT
                row++;
            }

            // Formatage pour lisibilité
            worksheet.Range("A1:H50").Style.Font.FontName = "Calibri";
            worksheet.Range("A1:H50").Style.Font.FontSize = 11;
            worksheet.Column("C").Width = 30; // Description plus large
        }
    }
}
