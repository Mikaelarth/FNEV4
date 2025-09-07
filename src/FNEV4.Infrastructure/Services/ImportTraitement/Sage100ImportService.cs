using ClosedXML.Excel;
using FNEV4.Application.Services.ImportTraitement;
using FNEV4.Core.Models.ImportTraitement;
using FNEV4.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FNEV4.Infrastructure.Services.ImportTraitement
{
    /// <summary>
    /// Service d'import des factures Sage 100 v15
    /// Implémente la structure définie dans exemple_structure_excel.py
    /// </summary>
    public class Sage100ImportService : ISage100ImportService
    {
        private readonly IClientRepository _clientRepository;

        public Sage100ImportService(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }
        public async Task<Sage100ImportResult> ImportSage100FileAsync(string filePath)
        {
            var startTime = DateTime.Now;
            var result = new Sage100ImportResult();
            
            try
            {
                // Validation préalable
                var validation = await ValidateFileStructureAsync(filePath);
                if (!validation.IsValid)
                {
                    result.IsSuccess = false;
                    result.Message = "Fichier invalide";
                    result.Errors.AddRange(validation.Errors);
                    return result;
                }

                using var workbook = new XLWorkbook(filePath);
                
                foreach (var worksheet in workbook.Worksheets)
                {
                    try
                    {
                        var factureData = await ParseFactureFromWorksheetAsync(worksheet);
                        if (factureData != null)
                        {
                            // TODO: Intégrer en base de données
                            // await _factureService.CreateFactureAsync(factureData);
                            
                            result.FacturesImportees++;
                            result.FacturesDetaillees.Add(new Sage100FactureImportee
                            {
                                NumeroFacture = factureData.NumeroFacture,
                                NomFeuille = worksheet.Name,
                                EstImportee = true,
                                DateImport = DateTime.Now,
                                NombreProduits = factureData.Produits.Count,
                                MontantTotal = factureData.Produits.Sum(p => p.MontantHt)
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FacturesEchouees++;
                        result.Errors.Add($"Erreur feuille '{worksheet.Name}': {ex.Message}");
                        result.FacturesDetaillees.Add(new Sage100FactureImportee
                        {
                            NomFeuille = worksheet.Name,
                            EstImportee = false,
                            MessageErreur = ex.Message,
                            DateImport = DateTime.Now
                        });
                    }
                }

                result.IsSuccess = result.FacturesImportees > 0;
                result.Message = result.IsSuccess 
                    ? $"Import réussi: {result.FacturesImportees} facture(s) importée(s)"
                    : "Aucune facture importée";
                    
                if (result.FacturesEchouees > 0)
                {
                    result.Message += $", {result.FacturesEchouees} échec(s)";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Erreur import: {ex.Message}";
                result.Errors.Add(ex.Message);
            }
            finally
            {
                result.DureeTraitement = DateTime.Now - startTime;
            }

            return result;
        }

        public async Task<Sage100ValidationResult> ValidateFileStructureAsync(string filePath)
        {
            var result = new Sage100ValidationResult();
            
            try
            {
                if (!File.Exists(filePath))
                {
                    result.IsValid = false;
                    result.Message = "Fichier introuvable";
                    result.Errors.Add($"Le fichier '{filePath}' n'existe pas");
                    return result;
                }

                if (!Path.GetExtension(filePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Message = "Format invalide";
                    result.Errors.Add("Le fichier doit être au format .xlsx");
                    return result;
                }

                // Validation des feuilles Excel
                using var workbook = new XLWorkbook(filePath);
                result.NombreFeuilles = workbook.Worksheets.Count;

                if (result.NombreFeuilles == 0)
                {
                    result.IsValid = false;
                    result.Message = "Aucune feuille trouvée";
                    result.Errors.Add("Le fichier Excel ne contient aucune feuille");
                    return result;
                }

                foreach (var worksheet in workbook.Worksheets)
                {
                    var validation = await ValidateWorksheetStructureAsync(worksheet);
                    if (validation.IsValid)
                    {
                        result.NomsFeuillesValides.Add(worksheet.Name);
                    }
                    else
                    {
                        result.NomsFeuillesInvalides.Add(worksheet.Name);
                        result.Errors.AddRange(validation.Errors.Select(e => $"Feuille '{worksheet.Name}': {e}"));
                    }
                }

                result.IsValid = result.NomsFeuillesValides.Count > 0;
                result.Message = result.IsValid
                    ? $"Fichier valide: {result.NomsFeuillesValides.Count} feuille(s) valide(s)"
                    : "Aucune feuille valide trouvée";

                if (result.NomsFeuillesInvalides.Count > 0)
                {
                    result.Warnings.Add($"{result.NomsFeuillesInvalides.Count} feuille(s) invalide(s) ignorée(s)");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Erreur validation: {ex.Message}";
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        public async Task<Sage100PreviewResult> PreviewFileAsync(string filePath)
        {
            var result = new Sage100PreviewResult();
            
            try
            {
                var validation = await ValidateFileStructureAsync(filePath);
                if (!validation.IsValid)
                {
                    result.IsSuccess = false;
                    result.Message = "Fichier invalide pour aperçu";
                    return result;
                }

                using var workbook = new XLWorkbook(filePath);
                result.NombreFeuilles = workbook.Worksheets.Count;

                foreach (var worksheet in workbook.Worksheets)
                {
                    try
                    {
                        var preview = await CreateFacturePreviewAsync(worksheet);
                        if (preview != null)
                        {
                            result.Apercu.Add(preview);
                            if (preview.EstValide)
                            {
                                result.FacturesDetectees++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Apercu.Add(new Sage100FacturePreview
                        {
                            NomFeuille = worksheet.Name,
                            EstValide = false,
                            Erreurs = { ex.Message }
                        });
                    }
                }

                result.IsSuccess = true;
                result.Message = $"Aperçu: {result.FacturesDetectees} facture(s) valide(s) sur {result.NombreFeuilles} feuille(s)";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Erreur aperçu: {ex.Message}";
            }

            return result;
        }

        #region Méthodes privées

        /// <summary>
        /// Valide la structure d'une feuille et l'existence du client
        /// </summary>
        private async Task<Sage100ValidationResult> ValidateWorksheetStructureAsync(IXLWorksheet worksheet)
        {
            var result = new Sage100ValidationResult { IsValid = true };

            try
            {
                // Vérifier les cellules obligatoires selon exemple_structure_excel.py
                var numeroFacture = GetCellValue(worksheet, "A3");
                if (string.IsNullOrWhiteSpace(numeroFacture))
                {
                    result.Errors.Add("Numéro de facture manquant (cellule A3)");
                    result.IsValid = false;
                }

                var codeClient = GetCellValue(worksheet, "A5");
                if (string.IsNullOrWhiteSpace(codeClient))
                {
                    result.Errors.Add("Code client manquant (cellule A5)");
                    result.IsValid = false;
                }
                else
                {
                    // Vérifier l'existence du client (sauf code 1999 pour clients divers)
                    if (codeClient != "1999")
                    {
                        var clientExists = await _clientRepository.GetByClientCodeAsync(codeClient);
                        if (clientExists == null)
                        {
                            result.Errors.Add($"Client '{codeClient}' inexistant en base de données. Le client doit être créé avant import des factures.");
                            result.IsValid = false;
                        }
                    }
                }

                var dateFacture = GetCellValue(worksheet, "A8");
                if (string.IsNullOrWhiteSpace(dateFacture))
                {
                    result.Errors.Add("Date facture manquante (cellule A8)");
                    result.IsValid = false;
                }

                // A18 peut être vide (moyen de paiement par défaut du client)
                var moyenPaiement = GetCellValue(worksheet, "A18");
                if (!string.IsNullOrWhiteSpace(moyenPaiement))
                {
                    if (!MoyensPaiementA18.Valides.Contains(moyenPaiement.ToLower()))
                    {
                        result.Errors.Add($"Moyen de paiement invalide: '{moyenPaiement}'. Valides: {string.Join(", ", MoyensPaiementA18.Valides)}");
                        result.IsValid = false;
                    }
                }

                // Vérifier qu'il y a au moins un produit (ligne 20 et suivantes)
                var hasProduits = false;
                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 20;
                for (int row = 20; row <= lastRow; row++)
                {
                    var codeProduit = GetCellValue(worksheet, $"B{row}");
                    if (!string.IsNullOrWhiteSpace(codeProduit))
                    {
                        hasProduits = true;
                        break;
                    }
                }

                if (!hasProduits)
                {
                    result.Errors.Add("Aucun produit trouvé (à partir de la ligne 20)");
                    result.IsValid = false;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Erreur validation structure: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// Parse une facture depuis une feuille Excel avec validation métier
        /// </summary>
        private async Task<Sage100FactureData?> ParseFactureFromWorksheetAsync(IXLWorksheet worksheet)
        {
            var validation = await ValidateWorksheetStructureAsync(worksheet);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Structure invalide: {string.Join(", ", validation.Errors)}");
            }

            var facture = new Sage100FactureData
            {
                NomFeuille = worksheet.Name,
                NumeroFacture = GetCellValue(worksheet, "A3"),
                CodeClient = GetCellValue(worksheet, "A5"),
                NccClient = GetCellValue(worksheet, "A6"),
                PointDeVente = GetCellValue(worksheet, "A10"),
                IntituleClient = GetCellValue(worksheet, "A11"),
                NomReelClientDivers = GetCellValue(worksheet, "A13"),
                NccClientDivers = GetCellValue(worksheet, "A15"),
                NumeroFactureAvoir = GetCellValue(worksheet, "A17")
            };

            // Gestion du moyen de paiement avec logique métier
            var moyenPaiementExcel = GetCellValue(worksheet, "A18");
            if (!string.IsNullOrWhiteSpace(moyenPaiementExcel))
            {
                // Utiliser le moyen de paiement spécifié dans Excel
                facture.MoyenPaiement = moyenPaiementExcel.ToLower();
            }
            else
            {
                // A18 vide -> utiliser le moyen de paiement par défaut du client
                if (facture.CodeClient == "1999")
                {
                    // Client divers -> cash par défaut
                    facture.MoyenPaiement = "cash";
                }
                else
                {
                    // Client normal -> récupérer son moyen de paiement par défaut
                    var client = await _clientRepository.GetByClientCodeAsync(facture.CodeClient);
                    facture.MoyenPaiement = client?.DefaultPaymentMethod ?? "cash";
                }
            }

            // Parse date (peut être en format Excel ou texte)
            var dateText = GetCellValue(worksheet, "A8");
            if (DateTime.TryParse(dateText, out var dateTime))
            {
                facture.DateFacture = dateTime;
            }
            else if (double.TryParse(dateText, out var excelDate))
            {
                facture.DateFacture = DateTime.FromOADate(excelDate);
            }
            else
            {
                throw new FormatException($"Date invalide: '{dateText}'");
            }

            // Parse produits (à partir de ligne 20)
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 20;
            for (int row = 20; row <= lastRow; row++)
            {
                var codeProduit = GetCellValue(worksheet, $"B{row}");
                if (string.IsNullOrWhiteSpace(codeProduit)) continue;

                var produit = new Sage100ProduitData
                {
                    NumeroLigne = row,
                    CodeProduit = codeProduit,
                    Designation = GetCellValue(worksheet, $"C{row}"),
                    Emballage = GetCellValue(worksheet, $"F{row}"),
                    CodeTva = GetCellValue(worksheet, $"G{row}")
                };

                // Parse prix unitaire
                if (decimal.TryParse(GetCellValue(worksheet, $"D{row}"), NumberStyles.Number, CultureInfo.InvariantCulture, out var prix))
                {
                    produit.PrixUnitaire = prix;
                }

                // Parse quantité
                if (decimal.TryParse(GetCellValue(worksheet, $"E{row}"), NumberStyles.Number, CultureInfo.InvariantCulture, out var qte))
                {
                    produit.Quantite = qte;
                }

                // Parse montant HT
                if (decimal.TryParse(GetCellValue(worksheet, $"H{row}"), NumberStyles.Number, CultureInfo.InvariantCulture, out var montant))
                {
                    produit.MontantHt = montant;
                }

                facture.Produits.Add(produit);
            }

            return facture;
        }

        /// <summary>
        /// Crée un aperçu d'une facture avec validation métier
        /// </summary>
        private async Task<Sage100FacturePreview?> CreateFacturePreviewAsync(IXLWorksheet worksheet)
        {
            var preview = new Sage100FacturePreview
            {
                NomFeuille = worksheet.Name
            };

            try
            {
                preview.NumeroFacture = GetCellValue(worksheet, "A3");
                preview.CodeClient = GetCellValue(worksheet, "A5");

                // Déterminer le nom du client
                var codeClient = preview.CodeClient;
                if (codeClient == "1999")
                {
                    preview.NomClient = GetCellValue(worksheet, "A13"); // Nom réel client divers
                }
                else
                {
                    preview.NomClient = GetCellValue(worksheet, "A11"); // Intitulé client
                }

                // Gestion du moyen de paiement avec logique métier
                var moyenPaiementExcel = GetCellValue(worksheet, "A18");
                if (!string.IsNullOrWhiteSpace(moyenPaiementExcel))
                {
                    preview.MoyenPaiement = moyenPaiementExcel;
                }
                else
                {
                    // A18 vide -> utiliser le moyen de paiement par défaut
                    if (codeClient == "1999")
                    {
                        preview.MoyenPaiement = "cash (par défaut)";
                        preview.ClientTrouve = true; // Client 1999 toujours valide
                    }
                    else
                    {
                        var client = await _clientRepository.GetByClientCodeAsync(codeClient);
                        if (client != null)
                        {
                            preview.MoyenPaiement = $"{client.DefaultPaymentMethod} (par défaut client)";
                            preview.ClientTrouve = true;
                        }
                        else
                        {
                            preview.MoyenPaiement = "Client inexistant";
                            preview.ClientTrouve = false;
                        }
                    }
                }

                // Vérification supplémentaire pour les clients avec moyen de paiement Excel spécifié
                if (!string.IsNullOrWhiteSpace(moyenPaiementExcel) && codeClient != "1999")
                {
                    var client = await _clientRepository.GetByClientCodeAsync(codeClient);
                    preview.ClientTrouve = client != null;
                }
                else if (codeClient == "1999")
                {
                    preview.ClientTrouve = true; // Client 1999 toujours valide
                }

                // Parse date
                var dateText = GetCellValue(worksheet, "A8");
                if (DateTime.TryParse(dateText, out var dateTime))
                {
                    preview.DateFacture = dateTime;
                }
                else if (double.TryParse(dateText, out var excelDate))
                {
                    preview.DateFacture = DateTime.FromOADate(excelDate);
                }

                // Compter produits et calculer montant
                decimal montantTotal = 0;
                int nombreProduits = 0;
                
                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 20;
                for (int row = 20; row <= lastRow; row++)
                {
                    var codeProduit = GetCellValue(worksheet, $"B{row}");
                    if (!string.IsNullOrWhiteSpace(codeProduit))
                    {
                        nombreProduits++;
                        if (decimal.TryParse(GetCellValue(worksheet, $"H{row}"), NumberStyles.Number, CultureInfo.InvariantCulture, out var montant))
                        {
                            montantTotal += montant;
                        }
                    }
                }

                preview.NombreProduits = nombreProduits;
                preview.MontantEstime = montantTotal;

                // Analyse des informations pour certification FNE et détection des conflits
                await AnalyzerInformationsFNE(preview, worksheet, codeClient);

                // Validation avec vérification d'existence du client
                var validation = await ValidateWorksheetStructureAsync(worksheet);
                preview.EstValide = validation.IsValid;
                preview.Erreurs.AddRange(validation.Errors);
            }
            catch (Exception ex)
            {
                preview.EstValide = false;
                preview.Erreurs.Add(ex.Message);
            }

            return preview;
        }

        /// <summary>
        /// Analyse les informations pour la certification FNE et détecte les conflits de données
        /// </summary>
        private async Task AnalyzerInformationsFNE(Sage100FacturePreview preview, IXLWorksheet worksheet, string codeClient)
        {
            var conflits = new List<string>();
            
            // Récupération des données Excel
            preview.NomClientExcel = preview.NomClient;
            preview.TelephoneClientExcel = GetCellValue(worksheet, "A14"); // Exemple cellule téléphone
            preview.EmailClientExcel = GetCellValue(worksheet, "A15"); // Exemple cellule email
            
            // Détermination du type de facturation FNE basé sur le code client
            if (codeClient == "1999")
            {
                preview.TypeFacture = "B2C"; // Client particulier/divers
            }
            else if (codeClient.StartsWith("GOV") || codeClient.StartsWith("ETAT"))
            {
                preview.TypeFacture = "B2G"; // Gouvernement/État
            }
            else if (codeClient.StartsWith("INT") || codeClient.StartsWith("EXT"))
            {
                preview.TypeFacture = "B2F"; // International
            }
            else
            {
                preview.TypeFacture = "B2B"; // Entreprise (défaut)
            }
            
            // Détermination du type de TVA (à partir des produits ou configuration)
            preview.TypeTva = DeterminerTypeTVA(worksheet);
            
            // Comparaison avec les données de la base si client trouvé
            if (preview.ClientTrouve && codeClient != "1999")
            {
                var clientBDD = await _clientRepository.GetByClientCodeAsync(codeClient);
                if (clientBDD != null)
                {
                    preview.NomClientBDD = clientBDD.Name ?? string.Empty;
                    preview.TelephoneClientBDD = clientBDD.Phone ?? string.Empty;
                    preview.EmailClientBDD = clientBDD.Email ?? string.Empty;
                    
                    // Détection des conflits (Source de vérité : Excel prioritaire sauf moyens de paiement)
                    if (!string.IsNullOrEmpty(preview.NomClientExcel) && 
                        !string.IsNullOrEmpty(preview.NomClientBDD) &&
                        !preview.NomClientExcel.Equals(preview.NomClientBDD, StringComparison.OrdinalIgnoreCase))
                    {
                        conflits.Add($"Nom: Excel='{preview.NomClientExcel}' ≠ BDD='{preview.NomClientBDD}'");
                    }
                    
                    if (!string.IsNullOrEmpty(preview.TelephoneClientExcel) && 
                        !string.IsNullOrEmpty(preview.TelephoneClientBDD) &&
                        preview.TelephoneClientExcel != preview.TelephoneClientBDD)
                    {
                        conflits.Add($"Tél: Excel='{preview.TelephoneClientExcel}' ≠ BDD='{preview.TelephoneClientBDD}'");
                    }
                    
                    if (!string.IsNullOrEmpty(preview.EmailClientExcel) && 
                        !string.IsNullOrEmpty(preview.EmailClientBDD) &&
                        !preview.EmailClientExcel.Equals(preview.EmailClientBDD, StringComparison.OrdinalIgnoreCase))
                    {
                        conflits.Add($"Email: Excel='{preview.EmailClientExcel}' ≠ BDD='{preview.EmailClientBDD}'");
                    }
                }
            }
            
            // Formatage des conflits pour affichage
            if (conflits.Any())
            {
                preview.ConflitDonnees = "⚠️ Conflits détectés";
                preview.ConflitDetails = $"Conflits de données (Source de vérité = Excel):\n{string.Join("\n", conflits)}";
            }
            else
            {
                preview.ConflitDonnees = "✅ Cohérent";
                preview.ConflitDetails = "Aucun conflit détecté entre Excel et base de données";
            }
        }
        
        /// <summary>
        /// Détermine le type de TVA basé sur les produits de la facture
        /// </summary>
        private string DeterminerTypeTVA(IXLWorksheet worksheet)
        {
            // Analyse des produits pour déterminer le type de TVA prédominant
            var typesTva = new Dictionary<string, int>();
            
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 25;
            for (int row = 20; row <= lastRow; row++)
            {
                var codeProduit = GetCellValue(worksheet, $"B{row}");
                if (!string.IsNullOrWhiteSpace(codeProduit))
                {
                    // Logique basée sur le code produit ou configuration
                    var typeTva = "TVA"; // Défaut 18%
                    
                    // Exemples de règles métier
                    if (codeProduit.StartsWith("MED") || codeProduit.StartsWith("SANTE"))
                        typeTva = "TVAB"; // 9% pour médicaments
                    else if (codeProduit.StartsWith("EXPORT") || codeProduit.StartsWith("EXP"))
                        typeTva = "TVAC"; // 0% exonération conventionnelle
                    else if (codeProduit.StartsWith("AGRI") || codeProduit.StartsWith("RIZ"))
                        typeTva = "TVAD"; // 0% exonération légale
                    
                    typesTva[typeTva] = typesTva.GetValueOrDefault(typeTva, 0) + 1;
                }
            }
            
            // Retourne le type de TVA le plus fréquent
            return typesTva.OrderByDescending(x => x.Value).FirstOrDefault().Key ?? "TVA";
        }

        private string GetCellValue(IXLWorksheet worksheet, string cellAddress)
        {
            try
            {
                var cell = worksheet.Cell(cellAddress);
                return cell.IsEmpty() ? string.Empty : cell.GetString().Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion
    }
}
