using System;
using System.IO;
using FNEV4.Application.Services.GestionFactures;

// Test rapide du service d'import
var service = new InvoiceExcelImportService();
var testFile = @"C:\wamp64\www\FNEV4\test_sage100_20250907_124618.xlsx";

Console.WriteLine("=== Test Rapide Import Factures ===");

if (File.Exists(testFile))
{
    Console.WriteLine($"✅ Fichier trouvé : {Path.GetFileName(testFile)}");
    
    // Test validation
    var validation = service.ValidateExcelFile(testFile);
    Console.WriteLine($"📋 Validation : {validation.IsValid}");
    
    if (validation.IsValid)
    {
        // Test import
        var result = service.ImportInvoicesFromExcel(testFile);
        Console.WriteLine($"📊 Import réussi : {result.IsSuccess}");
        Console.WriteLine($"📄 Factures importées : {result.ValidInvoices}/{result.TotalInvoices}");
        
        foreach (var invoice in result.ImportedInvoices.Take(3))
        {
            Console.WriteLine($"  - {invoice.InvoiceNumber} ({invoice.PaymentMethod}) : {invoice.Items.Count} lignes");
        }
    }
}
else
{
    Console.WriteLine($"❌ Fichier non trouvé : {testFile}");
}

Console.WriteLine("✅ Test terminé");
