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
            { "code client", nameof(ClientImportModel.ClientCode) },
            { "code", nameof(ClientImportModel.ClientCode) },
            { "nom/raison sociale", nameof(ClientImportModel.Name) },
            { "nom", nameof(ClientImportModel.Name) },
            { "raison sociale", nameof(ClientImportModel.Name) },
            { "type client", nameof(ClientImportModel.ClientType) },
            { "type", nameof(ClientImportModel.ClientType) },
            { "ncc", nameof(ClientImportModel.ClientNcc) },
            { "nom commercial", nameof(ClientImportModel.CommercialName) },
            { "adresse", nameof(ClientImportModel.Address) },
            { "ville", nameof(ClientImportModel.City) },
            { "code postal", nameof(ClientImportModel.PostalCode) },
            { "pays", nameof(ClientImportModel.Country) },
            { "téléphone", nameof(ClientImportModel.Phone) },
            { "phone", nameof(ClientImportModel.Phone) },
            { "email", nameof(ClientImportModel.Email) },
            { "représentant", nameof(ClientImportModel.Representative) },
            { "n° fiscal", nameof(ClientImportModel.TaxNumber) },
            { "numéro fiscal", nameof(ClientImportModel.TaxNumber) },
            { "devise", nameof(ClientImportModel.Currency) },
            { "actif", nameof(ClientImportModel.IsActive) },
            { "notes", nameof(ClientImportModel.Notes) }
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
        public async Task<List<ClientImportModel>> ReadExcelFileAsync(string filePath)
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
        /// Valide les données des clients
        /// </summary>
        public async Task<List<ClientImportModel>> ValidateDataAsync(List<ClientImportModel> clients)
        {
            return await Task.Run(() =>
            {
                foreach (var client in clients)
                {
                    ValidateClient(client);
                }
                return clients;
            });
        }

        /// <summary>
        /// Exporte un modèle Excel vierge
        /// </summary>
        public async Task ExportTemplateAsync(string filePath)
        {
            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Clients");

                // En-têtes
                var headers = new[]
                {
                    "Code Client", "Nom/Raison Sociale", "Type Client", "NCC", "Nom Commercial",
                    "Adresse", "Ville", "Code Postal", "Pays", "Téléphone", "Email",
                    "Représentant", "N° Fiscal", "Devise", "Actif", "Notes"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                }

                // Données d'exemple
                var sampleData = new[]
                {
                    new[] { "CLI001", "ARTHUR LE GRAND SARL", "Entreprise", "1234567890A", "Arthur Le Grand", "123 Boulevard de la Paix", "Abidjan", "01001", "Côte d'Ivoire", "+225 01 02 03 04", "arthur@legrand.ci", "Jean KOUAME", "TIN123456", "XOF", "Oui", "Client VIP" },
                    new[] { "CLI002", "MARIE KOUASSI", "Particulier", "", "Marie Kouassi Boutique", "45 Rue du Commerce", "Bouaké", "02001", "Côte d'Ivoire", "+225 05 06 07 08", "marie.kouassi@gmail.com", "Paul DIALLO", "", "XOF", "Oui", "" }
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
            });
        }

        #region Méthodes privées

        /// <summary>
        /// Lit les clients depuis la feuille Excel
        /// </summary>
        private List<ClientImportModel> ReadClientsFromWorksheet(IXLWorksheet worksheet)
        {
            var clients = new List<ClientImportModel>();
            
            // Mapper les colonnes
            var columnMap = MapColumns(worksheet);
            
            var usedRange = worksheet.RangeUsed();
            if (usedRange == null || usedRange.RowCount() <= 1)
                return clients;

            // Commencer à la ligne 2 (après l'en-tête)
            for (int row = 2; row <= usedRange.RowCount(); row++)
            {
                var client = new ClientImportModel { RowNumber = row };
                
                try
                {
                    // Lire les valeurs selon le mapping des colonnes
                    client.ClientCode = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.ClientCode));
                    client.Name = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.Name));
                    client.ClientType = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.ClientType));
                    client.ClientNcc = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.ClientNcc));
                    client.CommercialName = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.CommercialName));
                    client.Address = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.Address));
                    client.City = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.City));
                    client.PostalCode = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.PostalCode));
                    client.Country = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.Country)) ?? "Côte d'Ivoire";
                    client.Phone = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.Phone));
                    client.Email = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.Email));
                    client.Representative = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.Representative));
                    client.TaxNumber = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.TaxNumber));
                    client.Currency = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.Currency)) ?? "XOF";
                    client.Notes = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.Notes));

                    // Traitement spécial pour IsActive
                    var activeValue = GetCellValue(worksheet, row, columnMap, nameof(ClientImportModel.IsActive))?.ToLower();
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
        /// Obtient la valeur d'une cellule selon le mapping
        /// </summary>
        private string? GetCellValue(IXLWorksheet worksheet, int row, Dictionary<string, int> columnMap, string propertyName)
        {
            if (columnMap.TryGetValue(propertyName, out var col))
            {
                return worksheet.Cell(row, col).GetString();
            }
            return null;
        }

        /// <summary>
        /// Valide un client selon les attributs de validation
        /// </summary>
        private void ValidateClient(ClientImportModel client)
        {
            var context = new ValidationContext(client);
            var results = new List<ValidationResult>();
            
            if (!Validator.TryValidateObject(client, context, results, true))
            {
                client.ValidationErrors.AddRange(results.Select(r => r.ErrorMessage ?? "Erreur de validation"));
            }

            // Validations métier supplémentaires
            if (!string.IsNullOrEmpty(client.ClientNcc) && client.ClientNcc.Length != 10)
            {
                client.ValidationErrors.Add("Le NCC doit faire exactement 10 caractères");
            }

            if (!string.IsNullOrEmpty(client.Email) && !IsValidEmail(client.Email))
            {
                client.ValidationErrors.Add("Format d'email invalide");
            }
        }

        /// <summary>
        /// Validation simple de l'email
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
