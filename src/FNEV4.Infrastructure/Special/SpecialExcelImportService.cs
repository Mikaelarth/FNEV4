using ClosedXML.Excel;
using FNEV4.Core.Entities;

namespace FNEV4.Infrastructure.Special
{
    /// <summary>
    /// Service d'importation Excel EXCEPTIONNEL pour format spécial clients.xlsx
    /// 
    /// ⚠️ SYSTÈME TEMPORAIRE - FACILEMENT SUPPRIMABLE ⚠️
    /// 
    /// Structure du fichier:
    /// - Colonnes fixes: A=CODE, B=NCC, E=NOM, G=EMAIL, I=TEL, K=REGLEMENT, M=FACTURATION, O=DEVISE
    /// - L13: Données test (ignorées)
    /// - L16, L19, L22... : Clients réels (espacés de 3 lignes)
    /// 
    /// Pour supprimer ce système:
    /// 1. Supprimer le dossier /Special/
    /// 2. Retirer les références dans DI
    /// 3. Retirer le bouton d'import exceptionnel de l'UI
    /// </summary>
    public class SpecialExcelImportService
    {
        /// <summary>
        /// Modèle de données pour l'import exceptionnel
        /// </summary>
        public class ExceptionalClientModel
        {
            public string ClientCode { get; set; } = "";
            public string ClientNcc { get; set; } = "";
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public string PaymentMode { get; set; } = ""; // MODE DE REGLEMENT (non mappé en DB)
            public string InvoiceType { get; set; } = ""; // TYPE DE FACTURATION (non mappé en DB)
            public string Currency { get; set; } = ""; // DEVISE (souvent vide)
            
            public int SourceLine { get; set; } // Ligne d'origine dans Excel
            
            // Champs calculés pour la DB
            public string DetectedClientType { get; set; } = "Individual";
            public string FinalCurrency { get; set; } = "XOF";
            public string FinalTemplate { get; set; } = "DGI1";
        }

        /// <summary>
        /// Résultat de l'import exceptionnel
        /// </summary>
        public class ExceptionalImportResult
        {
            public List<ExceptionalClientModel> Clients { get; set; } = new();
            public int TotalFound { get; set; }
            public int ValidClients { get; set; }
            public List<string> Warnings { get; set; } = new();
            public List<string> Errors { get; set; } = new();
        }

        private readonly Dictionary<string, string> _columnMapping = new()
        {
            { "A", "ClientCode" },
            { "B", "ClientNcc" },
            { "E", "Name" },
            { "G", "Email" },
            { "I", "Phone" },
            { "K", "PaymentMode" },
            { "M", "InvoiceType" },
            { "O", "Currency" }
        };

        /// <summary>
        /// Importe les clients depuis le format Excel exceptionnel
        /// </summary>
        public async Task<ExceptionalImportResult> ImportFromSpecialExcelAsync(string filePath)
        {
            var result = new ExceptionalImportResult();
            
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);
                
                // Détecter tous les clients (à partir de L16, espacés de 3)
                for (int row = 16; row <= worksheet.LastRowUsed().RowNumber(); row += 3)
                {
                    var client = ExtractClientFromRow(worksheet, row);
                    
                    if (IsValidClient(client))
                    {
                        // Déduire le type de client
                        client.DetectedClientType = DetectClientType(client.Name);
                        
                        // Appliquer les valeurs par défaut
                        client.FinalCurrency = string.IsNullOrEmpty(client.Currency) ? "XOF" : client.Currency;
                        client.FinalTemplate = string.IsNullOrEmpty(client.InvoiceType) ? "DGI1" : client.InvoiceType;
                        
                        result.Clients.Add(client);
                        result.ValidClients++;
                    }
                    
                    result.TotalFound++;
                }
                
                // Validation et warnings
                ValidateClients(result);
                
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Erreur lors de l'import: {ex.Message}");
            }
            
            return result;
        }

        /// <summary>
        /// Extrait les données d'un client depuis une ligne Excel
        /// </summary>
        private ExceptionalClientModel ExtractClientFromRow(IXLWorksheet worksheet, int row)
        {
            var client = new ExceptionalClientModel
            {
                SourceLine = row
            };

            // Extraction selon le mapping des colonnes
            client.ClientCode = GetCellValue(worksheet, row, "A") ?? "";
            client.ClientNcc = GetCellValue(worksheet, row, "B") ?? "";
            client.Name = GetCellValue(worksheet, row, "E") ?? "";
            client.Email = GetCellValue(worksheet, row, "G") ?? "";
            client.Phone = GetCellValue(worksheet, row, "I") ?? "";
            client.PaymentMode = GetCellValue(worksheet, row, "K") ?? "";
            client.InvoiceType = GetCellValue(worksheet, row, "M") ?? "";
            client.Currency = GetCellValue(worksheet, row, "O") ?? "";

            return client;
        }

        /// <summary>
        /// Obtient la valeur d'une cellule
        /// </summary>
        private string? GetCellValue(IXLWorksheet worksheet, int row, string column)
        {
            try
            {
                var cell = worksheet.Cell($"{column}{row}");
                var value = cell.GetString()?.Trim();
                return string.IsNullOrEmpty(value) ? null : value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Vérifie si un client est valide (a au minimum un code et un nom)
        /// </summary>
        private bool IsValidClient(ExceptionalClientModel client)
        {
            return !string.IsNullOrEmpty(client.ClientCode) && !string.IsNullOrEmpty(client.Name);
        }

        /// <summary>
        /// Détecte le type de client selon des mots-clés dans le nom
        /// </summary>
        private string DetectClientType(string name)
        {
            var nameLower = name.ToLower();
            
            // Mots-clés pour entreprises
            string[] companyKeywords = { "sarl", "sa", "ci", "ste", "societe", "société", "eurl", "sas", "ets", "etablissement" };
            
            // Mots-clés pour gouvernement
            string[] governmentKeywords = { "ministere", "ministère", "prefecture", "préfecture", "mairie", "conseil", "état", "etat", "public" };
            
            if (governmentKeywords.Any(keyword => nameLower.Contains(keyword)))
                return "Government";
                
            if (companyKeywords.Any(keyword => nameLower.Contains(keyword)))
                return "Company";
                
            return "Individual";
        }

        /// <summary>
        /// Valide les clients et génère des warnings
        /// </summary>
        private void ValidateClients(ExceptionalImportResult result)
        {
            var duplicateCodes = result.Clients
                .GroupBy(c => c.ClientCode)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
                
            foreach (var code in duplicateCodes)
            {
                result.Warnings.Add($"Code client dupliqué: {code}");
            }
            
            var noEmail = result.Clients.Count(c => string.IsNullOrEmpty(c.Email));
            if (noEmail > 0)
            {
                result.Warnings.Add($"{noEmail} clients sans email");
            }
            
            var noPhone = result.Clients.Count(c => string.IsNullOrEmpty(c.Phone));
            if (noPhone > 0)
            {
                result.Warnings.Add($"{noPhone} clients sans téléphone");
            }
        }

        /// <summary>
        /// Convertit un modèle exceptionnel vers l'entité Client
        /// </summary>
        public Client ToClientEntity(ExceptionalClientModel model)
        {
            return new Client
            {
                Id = Guid.NewGuid(),
                ClientCode = model.ClientCode,
                ClientNcc = string.IsNullOrEmpty(model.ClientNcc) ? null : model.ClientNcc,
                Name = model.Name,
                CompanyName = model.DetectedClientType == "Company" ? model.Name : null,
                Email = string.IsNullOrEmpty(model.Email) ? null : model.Email,
                Phone = string.IsNullOrEmpty(model.Phone) ? null : model.Phone,
                ClientType = model.DetectedClientType,
                DefaultTemplate = model.FinalTemplate,
                DefaultCurrency = model.FinalCurrency,
                IsActive = true,
                Country = "Côte d'Ivoire",
                Address = null,
                TaxIdentificationNumber = null,
                SellerName = null,
                Notes = $"Import exceptionnel - Ligne {model.SourceLine} - Mode règlement: {model.PaymentMode}",
                CreatedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
