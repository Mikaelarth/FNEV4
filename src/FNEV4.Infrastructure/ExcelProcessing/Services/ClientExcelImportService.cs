using ClosedXML.Excel;
using FNEV4.Application.DTOs.GestionClients;
using FNEV4.Application.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace FNEV4.Infrastructure.ExcelProcessing.Services
{
    /// <summary>
    /// Service d'import Excel pour les clients
    /// Implémentation dans Infrastructure respectant l'architecture Clean
    /// </summary>
    public class ClientExcelImportService : IClientExcelImportService
    {
        private readonly Dictionary<string, string> _columnMapping = new()
        {
            // En-têtes du template DGI avec underscores
            { "code_client", nameof(ClientImportModelDgi.ClientCode) },
            { "nom_raison_sociale", nameof(ClientImportModelDgi.Name) },
            { "template_facturation", nameof(ClientImportModelDgi.Template) },
            { "ncc_client", nameof(ClientImportModelDgi.ClientNcc) },
            { "nom_commercial", nameof(ClientImportModelDgi.CommercialName) },
            { "adresse", nameof(ClientImportModelDgi.Address) },
            { "ville", nameof(ClientImportModelDgi.City) },
            { "code_postal", nameof(ClientImportModelDgi.PostalCode) },
            { "pays", nameof(ClientImportModelDgi.Country) },
            { "telephone", nameof(ClientImportModelDgi.Phone) },
            { "email", nameof(ClientImportModelDgi.Email) },
            { "representant", nameof(ClientImportModelDgi.Representative) },
            { "numero_fiscal", nameof(ClientImportModelDgi.TaxNumber) },
            { "devise", nameof(ClientImportModelDgi.Currency) },
            { "actif", nameof(ClientImportModelDgi.IsActive) },
            { "notes", nameof(ClientImportModelDgi.Notes) },
            
            // Anciens formats pour compatibilité
            { "code client", nameof(ClientImportModelDgi.ClientCode) },
            { "code", nameof(ClientImportModelDgi.ClientCode) },
            { "nom/raison sociale", nameof(ClientImportModelDgi.Name) },
            { "nom", nameof(ClientImportModelDgi.Name) },
            { "raison sociale", nameof(ClientImportModelDgi.Name) },
            { "template", nameof(ClientImportModelDgi.Template) },
            { "type template", nameof(ClientImportModelDgi.Template) },
            { "template dgi", nameof(ClientImportModelDgi.Template) },
            { "ncc", nameof(ClientImportModelDgi.ClientNcc) },
            { "ncc client", nameof(ClientImportModelDgi.ClientNcc) },
            { "nom commercial", nameof(ClientImportModelDgi.CommercialName) },
            { "denomination commerciale", nameof(ClientImportModelDgi.CommercialName) },
            { "code postal", nameof(ClientImportModelDgi.PostalCode) },
            { "téléphone", nameof(ClientImportModelDgi.Phone) },
            { "phone", nameof(ClientImportModelDgi.Phone) },
            { "représentant", nameof(ClientImportModelDgi.Representative) },
            { "n° fiscal", nameof(ClientImportModelDgi.TaxNumber) },
            { "numéro fiscal", nameof(ClientImportModelDgi.TaxNumber) }
        };

        /// <summary>
        /// Génère un aperçu du fichier Excel
        /// </summary>
        public async Task<ClientImportPreviewDto> PreviewFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var preview = new ClientImportPreviewDto
                {
                    FileName = Path.GetFileName(filePath),
                    AnalyzedAt = DateTime.Now
                };

                try
                {
                    using var workbook = new XLWorkbook(filePath);
                    var worksheet = workbook.Worksheets.First();

                    // Détecter les colonnes
                    var firstRow = worksheet.Row(1);
                    preview.DetectedColumns = firstRow.CellsUsed()
                        .Select(cell => cell.GetString())
                        .ToList();

                    var totalRows = worksheet.RangeUsed().RowCount() - 1; // Exclure l'en-tête
                    preview.TotalRows = totalRows;

                    // Lecture rapide pour analyse
                    var clients = ReadClientsFromWorksheet(worksheet);
                    
                    preview.ValidRows = clients.Count(c => c.IsValid);
                    preview.ErrorRows = clients.Count(c => !c.IsValid);
                    preview.EmptyRows = totalRows - clients.Count;

                    // Détecter les doublons
                    var duplicates = clients
                        .GroupBy(c => new { c.ClientCode, c.ClientNcc })
                        .Where(g => g.Count() > 1)
                        .SelectMany(g => g)
                        .ToList();
                    preview.DuplicateRows = duplicates.Count();

                    // Créer l'aperçu des clients avec statuts
                    preview.PreviewClients = clients.Take(100).Select(c => // Limiter à 100 pour performance
                    {
                        var previewClient = new ClientPreviewDto
                        {
                            RowNumber = c.RowNumber,
                            Code = c.ClientCode ?? "",
                            Name = c.Name ?? "",
                            Ncc = c.ClientNcc,
                            Type = c.Template ?? "",
                            Email = c.Email,
                            Phone = c.Phone,
                            Address = c.Address,
                            City = c.City,
                            PostalCode = c.PostalCode,
                            Country = c.Country,
                            Representative = c.Representative,
                            Currency = c.Currency,
                            Active = c.IsActive ? "Oui" : "Non",
                            Notes = c.Notes,
                            ValidationIssues = c.ValidationErrors.ToList()
                        };

                        // Déterminer le statut
                        if (!c.IsValid)
                        {
                            previewClient.Status = "Erreur";
                        }
                        else if (duplicates.Any(d => d.RowNumber == c.RowNumber))
                        {
                            previewClient.Status = "Doublon";
                        }
                        else
                        {
                            previewClient.Status = "Nouveau";
                        }

                        return previewClient;
                    }).ToList();

                    // Échantillon d'erreurs (max 10)
                    preview.SampleErrors = clients
                        .Where(c => !c.IsValid)
                        .Take(10)
                        .SelectMany(c => c.ValidationErrors.Select(error => new ClientImportErrorDto
                        {
                            RowNumber = c.RowNumber,
                            ErrorMessage = error,
                            RowData = $"{c.ClientCode} - {c.Name}",
                            ErrorType = "Validation"
                        }))
                        .ToList();

                    return preview;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Erreur lors de l'analyse du fichier: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// Lit et parse le fichier Excel
        /// </summary>
        public async Task<List<ClientImportModelDgi>> ReadExcelFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var workbook = new XLWorkbook(filePath);
                    var worksheet = workbook.Worksheets.First();
                    
                    return ReadClientsFromWorksheet(worksheet);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Erreur lors de la lecture du fichier Excel: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// Valide les données des clients selon les règles DGI
        /// </summary>
        public async Task<List<ClientImportModelDgi>> ValidateDataAsync(List<ClientImportModelDgi> clients)
        {
            return await Task.Run(() =>
            {
                foreach (var client in clients)
                {
                    client.ValidateBusinessRules();
                }
                return clients;
            });
        }

        /// <summary>
        /// Exporte un modèle Excel DGI complet avec documentation
        /// </summary>
        public async Task ExportTemplateAsync(string filePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Chemin vers notre template DGI complet
                    var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "data", "templates", "modele_import_clients_dgi.xlsx");
                    templatePath = Path.GetFullPath(templatePath);

                    // Si le template DGI n'existe pas, utiliser le chemin relatif depuis le répertoire de travail
                    if (!File.Exists(templatePath))
                    {
                        templatePath = Path.Combine("data", "templates", "modele_import_clients_dgi.xlsx");
                    }

                    if (File.Exists(templatePath))
                    {
                        // Copier le template DGI complet
                        File.Copy(templatePath, filePath, true);
                    }
                    else
                    {
                        // Fallback: générer un template basique si le template DGI n'est pas trouvé
                        using var workbook = new XLWorkbook();
                        var worksheet = workbook.Worksheets.Add("Template");

                        // En-têtes conformes API DGI
                        var headers = new[]
                        {
                            "CodeClient", "NomRaisonSociale", "Template", "ClientNcc", "NomCommercial",
                            "Adresse", "Ville", "CodePostal", "Pays", "Telephone", "Email",
                            "Representant", "NumeroFiscal", "Devise", "Actif", "Notes"
                        };

                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = worksheet.Cell(1, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                        }

                        // Données d'exemple conformes API DGI
                        var sampleData = new[]
                        {
                            new[] { "CLI001", "ARTHUR LE GRAND SARL", "B2B", "1234567890", "Arthur Le Grand", "123 Boulevard de la Paix", "Abidjan", "01001", "Côte d'Ivoire", "+225 01 02 03 04", "arthur@legrand.ci", "Jean KOUAME", "TIN123456", "XOF", "Oui", "Client VIP" },
                            new[] { "CLI002", "MARIE KOUASSI", "B2C", "", "Marie Kouassi Boutique", "45 Rue du Commerce", "Bouaké", "02001", "Côte d'Ivoire", "+225 05 06 07 08", "marie.kouassi@gmail.com", "Paul DIALLO", "", "XOF", "Oui", "Particulier" },
                            new[] { "CLI003", "MINISTERE SANTE", "B2G", "9876543210", "Min. Santé et Hygiène", "Plateau Tour C", "Abidjan", "01000", "Côte d'Ivoire", "+225 20 21 22 23", "contact@sante.gouv.ci", "Dr. BAMBA", "GOV789", "XOF", "Oui", "Client gouvernemental" }
                        };

                        for (int row = 0; row < sampleData.Length; row++)
                        {
                            for (int col = 0; col < sampleData[row].Length; col++)
                            {
                                worksheet.Cell(row + 2, col + 1).Value = sampleData[row][col];
                            }
                        }

                        // Auto-ajuster les colonnes
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(filePath);
                    }
                }
                catch (Exception ex)
                {
                    // En cas d'erreur, créer un template basique
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Template");
                    
                    var headers = new[] { "CodeClient", "NomRaisonSociale", "Template", "ClientNcc", "Email" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cell(1, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                    }
                    
                    workbook.SaveAs(filePath);
                }
            });
        }

        #region Méthodes privées

        /// <summary>
        /// Lit les clients depuis la feuille Excel avec modèle DGI
        /// </summary>
        private List<ClientImportModelDgi> ReadClientsFromWorksheet(IXLWorksheet worksheet)
        {
            var clients = new List<ClientImportModelDgi>();
            
            // Mapper les colonnes
            var columnMap = MapColumns(worksheet);
            
            var usedRange = worksheet.RangeUsed();
            if (usedRange == null || usedRange.RowCount() <= 1)
                return clients;

            // Commencer à la ligne 2 (après l'en-tête)
            for (int row = 2; row <= usedRange.RowCount(); row++)
            {
                var client = new ClientImportModelDgi { RowNumber = row };
                
                try
                {
                    // Lire les valeurs selon le mapping des colonnes
                    client.ClientCode = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.ClientCode)) ?? "";
                    client.Name = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.Name)) ?? "";
                    client.Template = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.Template)) ?? "";
                    client.ClientNcc = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.ClientNcc)) ?? "";
                    client.CommercialName = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.CommercialName)) ?? "";
                    client.Address = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.Address)) ?? "";
                    client.City = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.City)) ?? "";
                    client.PostalCode = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.PostalCode)) ?? "";
                    client.Country = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.Country)) ?? "Côte d'Ivoire";
                    client.Phone = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.Phone)) ?? "";
                    client.Email = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.Email)) ?? "";
                    client.Representative = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.Representative)) ?? "";
                    client.TaxNumber = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.TaxNumber)) ?? "";
                    client.Currency = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.Currency)) ?? "XOF";
                    client.Notes = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.Notes)) ?? "";

                    // Traitement spécial pour IsActive avec gestion des espaces
                    var activeValue = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModelDgi.IsActive))?.ToLower().Trim();
                    client.IsActive = activeValue != "non" && activeValue != "false" && activeValue != "0";

                    // Ignorer les lignes vides
                    if (string.IsNullOrWhiteSpace(client.ClientCode) && string.IsNullOrWhiteSpace(client.Name))
                        continue;

                    clients.Add(client);
                }
                catch (Exception ex)
                {
                    client.ValidationErrors.Add($"Erreur lecture ligne: {ex.Message}");
                    clients.Add(client);
                }
            }

            return clients;
        }

        /// <summary>
        /// Mappe les colonnes Excel aux propriétés du modèle
        /// </summary>
        private Dictionary<string, int> MapColumns(IXLWorksheet worksheet)
        {
            var columnMap = new Dictionary<string, int>();
            var headerRow = worksheet.Row(1);
            
            for (int col = 1; col <= headerRow.CellsUsed().Count(); col++)
            {
                var headerValue = headerRow.Cell(col).GetString().ToLower().Trim();
                
                if (_columnMapping.TryGetValue(headerValue, out var propertyName))
                {
                    columnMap[propertyName] = col;
                }
            }

            return columnMap;
        }

        /// <summary>
        /// Obtient la valeur d'une cellule selon le mapping avec traitement spécial pour les champs numériques
        /// </summary>
        private string? GetCellValue(IXLWorksheet worksheet, int row, Dictionary<string, int> columnMap, string propertyName)
        {
            if (columnMap.TryGetValue(propertyName, out var col))
            {
                var cell = worksheet.Cell(row, col);
                
                // Traitement spécial pour NCC et autres champs numériques qui doivent rester des chaînes
                if (propertyName == nameof(ClientImportModelDgi.ClientNcc) || 
                    propertyName == nameof(ClientImportModelDgi.TaxNumber) ||
                    propertyName == nameof(ClientImportModelDgi.PostalCode))
                {
                    // Si c'est un nombre, le convertir en chaîne sans décimales
                    if (cell.DataType == XLDataType.Number)
                    {
                        var numValue = cell.GetDouble();
                        return ((long)numValue).ToString();
                    }
                }
                
                return cell.GetString();
            }
            return null;
        }

        #endregion
    }
}
