using ClosedXML.Excel;
using System;
using System.IO;

namespace FNEV4.SpecialExcelAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                AnalyzeSpecialExcelFile();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur: {ex.Message}");
            }
        }

        static void AnalyzeSpecialExcelFile()
        {
            string filePath = @"c:\wamp64\www\FNEV4\clients.xlsx";
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ Fichier non trouvé: {filePath}");
                return;
            }

            Console.WriteLine("=== ANALYSE DU FICHIER EXCEL EXCEPTIONNEL ===");
            Console.WriteLine($"Fichier: {filePath}");
            Console.WriteLine();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);

            Console.WriteLine($"Nom de la feuille: {worksheet.Name}");
            Console.WriteLine($"Plage utilisée: {worksheet.RangeUsed()?.RangeAddress}");
            Console.WriteLine();

            // Mapping des colonnes selon les specs
            var columnMapping = new Dictionary<string, string>
            {
                ["A"] = "CODE CLIENT",
                ["B"] = "NCC", 
                ["E"] = "NOM",
                ["G"] = "EMAIL",
                ["I"] = "TELEPHONE",
                ["K"] = "MODE DE REGLEMENT",
                ["M"] = "TYPE DE FACTURATION",
                ["O"] = "DEVISE"
            };

            Console.WriteLine("=== STRUCTURE DES COLONNES ===");
            foreach (var col in columnMapping)
            {
                Console.WriteLine($"Colonne {col.Key}: {col.Value}");
            }
            Console.WriteLine();

            // Analyser les en-têtes dans les premières lignes
            Console.WriteLine("=== ANALYSE DES EN-TÊTES (lignes 1-15) ===");
            for (int row = 1; row <= 15; row++)
            {
                var values = new List<string>();
                foreach (var col in columnMapping.Keys)
                {
                    var cellValue = worksheet.Cell($"{col}{row}").GetString();
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        values.Add($"{col}: {cellValue}");
                    }
                }
                if (values.Any())
                {
                    Console.WriteLine($"Ligne {row}: {string.Join(" | ", values)}");
                }
            }
            Console.WriteLine();

            // Analyser la zone de données
            Console.WriteLine("=== ANALYSE DES DONNÉES (L13, L16, L19, L22...) ===");
            var testRows = new[] { 13, 16, 19, 22, 25, 28 };

            foreach (var rowNum in testRows)
            {
                if (rowNum <= worksheet.RangeUsed()?.RowCount())
                {
                    Console.WriteLine($"--- Ligne {rowNum} ---");
                    var clientData = new Dictionary<string, string>();
                    bool hasData = false;

                    foreach (var col in columnMapping)
                    {
                        var cellValue = worksheet.Cell($"{col.Key}{rowNum}").GetString();
                        if (!string.IsNullOrWhiteSpace(cellValue))
                        {
                            clientData[col.Value] = cellValue.Trim();
                            hasData = true;
                        }
                    }

                    if (hasData)
                    {
                        foreach (var data in clientData)
                        {
                            Console.WriteLine($"  {data.Key}: {data.Value}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("  (ligne vide)");
                    }
                    Console.WriteLine();
                }
            }

            // Détecter automatiquement tous les clients
            Console.WriteLine("=== DÉTECTION AUTOMATIQUE DES CLIENTS ===");
            var clientsFound = new List<(int ligne, Dictionary<string, string> data)>();

            var maxRow = worksheet.RangeUsed()?.RowCount() ?? 0;
            
            // Commencer à partir de la ligne 16 (premier client réel)
            for (int row = 16; row <= maxRow; row += 3) // Incrément de 3 (client + 2 lignes vides)
            {
                var clientData = new Dictionary<string, string>();
                bool hasSignificantData = false;

                foreach (var col in columnMapping)
                {
                    var cellValue = worksheet.Cell($"{col.Key}{row}").GetString();
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        clientData[col.Value] = cellValue.Trim();
                    }
                }

                // Vérifier si on a au moins un CODE CLIENT ou un NOM
                if (clientData.ContainsKey("CODE CLIENT") || clientData.ContainsKey("NOM"))
                {
                    hasSignificantData = true;
                }

                if (hasSignificantData)
                {
                    clientsFound.Add((row, clientData));
                }
            }

            Console.WriteLine($"Nombre de clients détectés: {clientsFound.Count}");
            Console.WriteLine();

            for (int i = 0; i < clientsFound.Count; i++)
            {
                var client = clientsFound[i];
                Console.WriteLine($"Client {i + 1} (ligne {client.ligne}):");
                foreach (var data in client.data)
                {
                    Console.WriteLine($"  {data.Key}: {data.Value}");
                }
                Console.WriteLine();
            }

            Console.WriteLine($"✅ Analyse terminée. {clientsFound.Count} clients trouvés.");
        }
    }
}
