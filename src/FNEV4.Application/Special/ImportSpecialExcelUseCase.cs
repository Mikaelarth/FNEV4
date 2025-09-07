using FNEV4.Core.Interfaces;
using FNEV4.Core.Entities;
using ClosedXML.Excel;

namespace FNEV4.Application.Special
{
    /// <summary>
    /// Use Case pour l'importation Excel EXCEPTIONNELLE
    /// 
    /// ⚠️ SYSTÈME TEMPORAIRE - FACILEMENT SUPPRIMABLE ⚠️
    /// 
    /// Ce use case orchestre l'import du format spécial clients.xlsx
    /// avec validation, transformation et persistance en base.
    /// 
    /// Structure du fichier:
    /// - Colonnes fixes: A=CODE, B=NCC, E=NOM, G=EMAIL, I=TEL, K=REGLEMENT, M=FACTURATION, O=DEVISE
    /// - L13: Données test (ignorées)
    /// - L16, L19, L22... : Clients réels (espacés de 3 lignes)
    /// 
    /// Pour supprimer ce système complet:
    /// 1. Supprimer le dossier /Application/Special/
    /// 2. Supprimer le dossier /Infrastructure/Special/
    /// 3. Retirer les références dans DI
    /// 4. Retirer le bouton d'import exceptionnel de l'UI
    /// </summary>
    public class ImportSpecialExcelUseCase
    {
        private readonly IClientRepository _clientRepository;

        public ImportSpecialExcelUseCase(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

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
        public class ImportResult
        {
            public int TotalProcessed { get; set; }
            public int SuccessfulImports { get; set; }
            public int SkippedDuplicates { get; set; }
            public int Errors { get; set; }
            public List<string> Messages { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
            public TimeSpan Duration { get; set; }
            public List<ExceptionalClientModel> PreviewClients { get; set; } = new();
        }

        /// <summary>
        /// Exécute l'import complet du fichier exceptionnel
        /// </summary>
        public async Task<ImportResult> ExecuteAsync(string filePath, bool previewOnly = false)
        {
            var startTime = DateTime.Now;
            var result = new ImportResult();

            try
            {
                // 1. Import depuis Excel
                var clients = await ExtractClientsFromExcelAsync(filePath);
                
                result.TotalProcessed = clients.Count;
                result.PreviewClients = clients;
                
                // Validation générale
                ValidateClients(result, clients);

                if (previewOnly)
                {
                    result.Messages.Add($"📋 Prévisualisation: {clients.Count} clients détectés");
                    result.Duration = DateTime.Now - startTime;
                    return result;
                }

                // 2. Import en base de données
                foreach (var clientModel in clients)
                {
                    try
                    {
                        // Vérifier les doublons existants
                        var existingByCode = await _clientRepository.GetByClientCodeAsync(clientModel.ClientCode);
                        if (existingByCode != null)
                        {
                            result.SkippedDuplicates++;
                            result.Warnings.Add($"Client {clientModel.ClientCode} déjà existant - ignoré");
                            continue;
                        }

                        // Vérifier NCC si présent
                        if (!string.IsNullOrEmpty(clientModel.ClientNcc))
                        {
                            var existingByNcc = await _clientRepository.GetByNccAsync(clientModel.ClientNcc);
                            if (existingByNcc != null)
                            {
                                result.SkippedDuplicates++;
                                result.Warnings.Add($"NCC {clientModel.ClientNcc} déjà existant - client {clientModel.ClientCode} ignoré");
                                continue;
                            }
                        }

                        // 3. Conversion et persistance
                        var clientEntity = ToClientEntity(clientModel);
                        await _clientRepository.CreateAsync(clientEntity);
                        
                        result.SuccessfulImports++;
                        result.Messages.Add($"✅ {clientModel.ClientCode} - {clientModel.Name}");
                    }
                    catch (Exception ex)
                    {
                        result.Errors++;
                        result.Messages.Add($"❌ Erreur {clientModel.ClientCode}: {ex.Message}");
                    }
                }

                // 4. Résumé final
                result.Duration = DateTime.Now - startTime;
                result.Messages.Insert(0, $"🎯 Import exceptionnel terminé en {result.Duration.TotalSeconds:F1}s");
                result.Messages.Insert(1, $"📊 {result.SuccessfulImports}/{result.TotalProcessed} clients importés");
                
                if (result.SkippedDuplicates > 0)
                    result.Messages.Insert(2, $"⚠️ {result.SkippedDuplicates} doublons ignorés");
                    
                if (result.Errors > 0)
                    result.Messages.Insert(3, $"❌ {result.Errors} erreurs");
            }
            catch (Exception ex)
            {
                result.Messages.Add($"💥 Erreur fatale: {ex.Message}");
                result.Duration = DateTime.Now - startTime;
            }

            return result;
        }

        /// <summary>
        /// Extrait les clients depuis le fichier Excel exceptionnel
        /// </summary>
        private async Task<List<ExceptionalClientModel>> ExtractClientsFromExcelAsync(string filePath)
        {
            var clients = new List<ExceptionalClientModel>();
            
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
                    
                    // Appliquer les valeurs par défaut avec le mapping correct
                    client.FinalCurrency = string.IsNullOrEmpty(client.Currency) ? "XOF" : client.Currency;
                    
                    // CORRECTION: Utiliser InvoiceType directement comme template (B2B, B2C, B2F, B2G)
                    // Si InvoiceType est vide, utiliser un template par défaut UNIQUE pour import exceptionnel
                    if (!string.IsNullOrEmpty(client.InvoiceType))
                    {
                        client.FinalTemplate = client.InvoiceType; // B2B, B2C, B2F, B2G
                    }
                    else
                    {
                        // Pour import exceptionnel: TOUS les clients ont le même template par défaut
                        // (au lieu d'analyser le nom du client qui donne des résultats différents)
                        client.FinalTemplate = "B2C"; // Template par défaut unique pour import exceptionnel
                    }
                    
                    clients.Add(client);
                }
            }
            
            return clients;
        }

        /// <summary>
        /// Extrait les données d'un client depuis une ligne Excel
        /// </summary>
        private ExceptionalClientModel ExtractClientFromRow(IXLWorksheet worksheet, int row)
        {
            var client = new ExceptionalClientModel
            {
                SourceLine = row,
                ClientCode = GetCellValue(worksheet, row, "A") ?? "",
                ClientNcc = GetCellValue(worksheet, row, "B") ?? "",
                Name = GetCellValue(worksheet, row, "E") ?? "",
                Email = GetCellValue(worksheet, row, "G") ?? "",
                Phone = GetCellValue(worksheet, row, "I") ?? "",
                PaymentMode = GetCellValue(worksheet, row, "K") ?? "",
                InvoiceType = GetCellValue(worksheet, row, "M") ?? "",
                Currency = GetCellValue(worksheet, row, "O") ?? ""
            };

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
        /// Obtient le template par défaut basé sur le type de client détecté
        /// Mapping aligné avec AjoutClientUseCase.cs pour cohérence
        /// </summary>
        private string GetDefaultTemplateFromClientType(string clientType)
        {
            return clientType switch
            {
                "Individual" => "B2C",     // Particulier
                "Company" => "B2B",        // Entreprise
                "Government" => "B2G",     // Gouvernement/Administration
                _ => "B2C"                 // Par défaut: Particulier
            };
        }

        /// <summary>
        /// Normalise le mode de paiement selon les standards API DGI
        /// </summary>
        private string GetDefaultPaymentMethod(string paymentMode)
        {
            if (string.IsNullOrEmpty(paymentMode))
                return "cash"; // Par défaut

            // Normalisation selon les standards API DGI
            return paymentMode.ToLower().Trim() switch
            {
                "espèces" or "especes" or "cash" or "liquide" => "cash",
                "carte" or "card" or "cb" or "carte bancaire" => "card",
                "mobile money" or "mobile-money" or "momo" or "orange money" or "mtn money" => "mobile-money",
                "virement" or "virement bancaire" or "bank transfer" or "transfer" => "bank-transfer",
                "chèque" or "cheque" or "check" => "check",
                "crédit" or "credit" or "compte client" => "credit",
                _ => "cash" // Par défaut si mode non reconnu
            };
        }

        /// <summary>
        /// Valide les clients et génère des warnings
        /// </summary>
        private void ValidateClients(ImportResult result, List<ExceptionalClientModel> clients)
        {
            var duplicateCodes = clients
                .GroupBy(c => c.ClientCode)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
                
            foreach (var code in duplicateCodes)
            {
                result.Warnings.Add($"Code client dupliqué dans le fichier: {code}");
            }
            
            var noEmail = clients.Count(c => string.IsNullOrEmpty(c.Email));
            if (noEmail > 0)
            {
                result.Warnings.Add($"{noEmail}/{clients.Count} clients sans email");
            }
            
            var noPhone = clients.Count(c => string.IsNullOrEmpty(c.Phone));
            if (noPhone > 0)
            {
                result.Warnings.Add($"{noPhone}/{clients.Count} clients sans téléphone");
            }

            var noNcc = clients.Count(c => string.IsNullOrEmpty(c.ClientNcc));
            if (noNcc > 0)
            {
                result.Warnings.Add($"{noNcc}/{clients.Count} clients sans NCC");
            }
        }

        /// <summary>
        /// Convertit un modèle exceptionnel vers l'entité Client
        /// </summary>
        private Client ToClientEntity(ExceptionalClientModel model)
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
                // CORRECTION: ClientType doit contenir le template API DGI (B2B, B2C, B2G, B2F)
                ClientType = model.FinalTemplate, // Utiliser le template plutôt que DetectedClientType
                DefaultTemplate = model.FinalTemplate,
                DefaultPaymentMethod = GetDefaultPaymentMethod(model.PaymentMode),
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

        /// <summary>
        /// Preview du fichier sans import en base
        /// </summary>
        public async Task<ImportResult> PreviewAsync(string filePath)
        {
            return await ExecuteAsync(filePath, previewOnly: true);
        }
    }
}
