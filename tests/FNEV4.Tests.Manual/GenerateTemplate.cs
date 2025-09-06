using FNEV4.Infrastructure.ExcelProcessing.Services;

namespace FNEV4.Tests.Manual
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var service = new ClientExcelImportService();
            var templatePath = @"C:\wamp64\www\FNEV4\data\templates\modele_import_clients_TEST.xlsx";
            
            Console.WriteLine("🚀 Génération du template Excel...");
            await service.ExportTemplateAsync(templatePath);
            Console.WriteLine($"✅ Template créé : {templatePath}");
        }
    }
}
