using ClosedXML.Excel;
using FNEV4.Application.Services.ImportTraitement;
using FNEV4.Core.Models.ImportTraitement;
using FNEV4.Core.Interfaces;
using FNEV4.Core.Entities;
using FNEV4.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
        private readonly FNEV4DbContext _context;
        private readonly ILoggingService _loggingService;

        public Sage100ImportService(
            IClientRepository clientRepository, 
            FNEV4DbContext context,
            ILoggingService loggingService)
        {
            _clientRepository = clientRepository;
            _context = context;
            _loggingService = loggingService;
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
                            // CORRECTION CRITIQUE: Sauvegarde réelle en base de données
                            var fneInvoice = await ConvertToFneInvoiceAsync(factureData, worksheet.Name);
                            if (fneInvoice != null)
                            {
                                await _context.FneInvoices.AddAsync(fneInvoice);
                                await _context.SaveChangesAsync();
                                
                                // Log de succès
                                await _loggingService.LogInfoAsync(
                                    $"Facture {factureData.NumeroFacture} sauvegardée avec succès (ID: {fneInvoice.Id})", 
                                    "Sage100Import");
                            }
                            
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
                NccClientNormal = GetCellValue(worksheet, "A6"),
                IntituleClient = GetCellValue(worksheet, "A11"),
                NomReelClientDivers = GetCellValue(worksheet, "A13"),
                NccClientDivers = GetCellValue(worksheet, "A15"),
                NumeroFactureAvoir = GetCellValue(worksheet, "A17")
            };

            // Gestion du point de vente avec logique de fallback
            var pointDeVenteExcel = GetCellValue(worksheet, "A10");
            if (!string.IsNullOrWhiteSpace(pointDeVenteExcel))
            {
                facture.PointDeVente = pointDeVenteExcel;
            }
            else
            {
                var company = await _context.Companies.FirstOrDefaultAsync();
                facture.PointDeVente = company?.DefaultPointOfSale ?? "01";
            }

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

                // Déterminer le nom du client avec DEBUG
                var codeClient = preview.CodeClient;
                Console.WriteLine($"[DEBUG] Feuille {worksheet.Name} - Code client A5: '{codeClient}'");
                
                if (codeClient == "1999")
                {
                    preview.NomClient = GetCellValue(worksheet, "A13"); // Nom réel client divers
                    var nomGenerique = GetCellValue(worksheet, "A11"); // Intitulé générique
                    Console.WriteLine($"[DEBUG] Client divers 1999 - A11 générique: '{nomGenerique}', A13 réel: '{preview.NomClient}'");
                }
                else
                {
                    preview.NomClient = GetCellValue(worksheet, "A11"); // Intitulé client
                    Console.WriteLine($"[DEBUG] Client normal {codeClient} - A11: '{preview.NomClient}'");
                }

                // Récupérer le point de vente avec logique de fallback ET DEBUG
                var pointDeVenteExcel = GetCellValue(worksheet, "A10");
                
                // DEBUG: Afficher les valeurs extraites pour diagnostic
                Console.WriteLine($"[DEBUG] Feuille {worksheet.Name} - A10 Point de Vente Excel: '{pointDeVenteExcel}'");
                
                if (!string.IsNullOrWhiteSpace(pointDeVenteExcel))
                {
                    // Priorité 1: Utiliser la valeur de A10 si présente
                    preview.PointDeVente = pointDeVenteExcel;
                    Console.WriteLine($"[DEBUG] Point de vente utilisé depuis Excel A10: '{pointDeVenteExcel}'");
                }
                else
                {
                    // Priorité 2: Chercher dans les attributs du client par code client
                    if (codeClient != "1999")
                    {
                        var client = await _clientRepository.GetByClientCodeAsync(codeClient);
                        if (client != null && !string.IsNullOrWhiteSpace(client.DefaultPointOfSale))
                        {
                            preview.PointDeVente = client.DefaultPointOfSale;
                            Console.WriteLine($"[DEBUG] Point de vente utilisé depuis client {codeClient}: '{client.DefaultPointOfSale}'");
                        }
                        else
                        {
                            // Priorité 3: Utiliser le point de vente par défaut de l'entreprise
                            var company = await _context.Companies.FirstOrDefaultAsync();
                            preview.PointDeVente = company?.DefaultPointOfSale ?? "01"; // "01" par défaut si aucune config
                            Console.WriteLine($"[DEBUG] Point de vente utilisé depuis Company par défaut: '{preview.PointDeVente}'");
                        }
                    }
                    else
                    {
                        // Pour client divers (1999), utiliser directement le point de vente de l'entreprise
                        var company = await _context.Companies.FirstOrDefaultAsync();
                        preview.PointDeVente = company?.DefaultPointOfSale ?? "01";
                        Console.WriteLine($"[DEBUG] Point de vente utilisé depuis Company pour client divers: '{preview.PointDeVente}'");
                    }
                }

                // Gestion du moyen de paiement avec logique métier ET DEBUG
                var moyenPaiementExcel = GetCellValue(worksheet, "A18");
                
                // DEBUG: Afficher les valeurs extraites pour diagnostic
                Console.WriteLine($"[DEBUG] Feuille {worksheet.Name} - A18 Moyen Paiement Excel: '{moyenPaiementExcel}'");
                
                if (!string.IsNullOrWhiteSpace(moyenPaiementExcel))
                {
                    preview.MoyenPaiement = moyenPaiementExcel;
                    Console.WriteLine($"[DEBUG] Moyen paiement utilisé depuis Excel A18: '{moyenPaiementExcel}'");
                }
                else
                {
                    // A18 vide -> utiliser le moyen de paiement par défaut
                    Console.WriteLine($"[DEBUG] A18 vide, utilisation fallback pour client {codeClient}");
                    if (codeClient == "1999")
                    {
                        preview.MoyenPaiement = "cash (par défaut)";
                        preview.ClientTrouve = true; // Client 1999 toujours valide
                        Console.WriteLine($"[DEBUG] Client divers 1999: moyen paiement = cash par défaut");
                    }
                    else
                    {
                        var client = await _clientRepository.GetByClientCodeAsync(codeClient);
                        if (client != null)
                        {
                            preview.MoyenPaiement = $"{client.DefaultPaymentMethod} (par défaut client)";
                            preview.ClientTrouve = true;
                            Console.WriteLine($"[DEBUG] Client {codeClient} trouvé: moyen paiement = {client.DefaultPaymentMethod}");
                        }
                        else
                        {
                            preview.MoyenPaiement = "Client inexistant";
                            preview.ClientTrouve = false;
                            Console.WriteLine($"[DEBUG] Client {codeClient} NON trouvé en base");
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
                var produits = new List<Sage100ProduitData>();
                
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
                        
                        // Créer l'objet produit pour le détail
                        var produit = new Sage100ProduitData
                        {
                            CodeProduit = codeProduit,
                            Designation = GetCellValue(worksheet, $"C{row}"),
                            PrixUnitaire = decimal.TryParse(GetCellValue(worksheet, $"D{row}"), NumberStyles.Number, CultureInfo.InvariantCulture, out var prix) ? prix : 0,
                            Quantite = decimal.TryParse(GetCellValue(worksheet, $"E{row}"), NumberStyles.Number, CultureInfo.InvariantCulture, out var qte) ? qte : 0,
                            Emballage = GetCellValue(worksheet, $"F{row}"),
                            CodeTva = GetCellValue(worksheet, $"G{row}"),
                            MontantHt = montant,
                            NumeroLigne = row
                        };
                        produits.Add(produit);
                    }
                }

                preview.NombreProduits = nombreProduits;
                preview.Produits = produits;
                
                // Calcul des montants (approximation 18% TVA)
                preview.MontantHT = montantTotal;
                preview.MontantTTC = montantTotal * 1.18m;

                // Analyse des informations pour certification FNE et détection des conflits
                await AnalyzerInformationsClientFNE(preview, codeClient);

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
                preview.TypeFacture = "B2B"; // Entreprise/B2B par défaut
            }
        }

        /// <summary>
        /// Convertit les données Sage100 vers le format JSON FNE pour certification
        /// Selon la documentation FNE-procedureapi.md et exemple_structure_excel.py
        /// </summary>
        public async Task<object> ConvertToFneApiJsonAsync(Sage100FactureData factureData)
        {
            try
            {
                // Récupérer la configuration entreprise pour l'establishment
                var company = await _context.Companies.FirstOrDefaultAsync();
                if (company == null)
                {
                    throw new InvalidOperationException("Configuration entreprise manquante");
                }

                // Déterminer le template et les données client selon le code client
                string template;
                string clientNcc;
                string clientCompanyName;

                if (factureData.CodeClient == "1999")
                {
                    // CLIENT DIVERS (B2C)
                    template = "B2C";
                    clientNcc = factureData.NccClientDivers ?? ""; // A15
                    clientCompanyName = factureData.NomReelClientDivers ?? ""; // A13
                }
                else
                {
                    // CLIENT NORMAL (B2B/B2G/B2F)
                    template = "B2B"; // Par défaut, peut être affiné selon la logique métier
                    clientNcc = factureData.NccClientNormal ?? ""; // A6
                    
                    // Récupérer le nom du client depuis la base de données
                    var client = await _clientRepository.GetByClientCodeAsync(factureData.CodeClient);
                    clientCompanyName = client?.Name ?? factureData.IntituleClient ?? "";
                }

                // Construire la structure JSON FNE
                var fneJson = new
                {
                    invoiceType = "sale",
                    paymentMethod = factureData.MoyenPaiement, // A18
                    template = template,
                    clientNcc = clientNcc,
                    clientCompanyName = clientCompanyName,
                    pointOfSale = factureData.PointDeVente, // A10 ou fallback
                    establishment = company.CompanyName,
                    commercialMessage = "", // Peut être ajouté si nécessaire
                    footer = "", // Peut être ajouté si nécessaire
                    foreignCurrency = "", // Vide par défaut (francs CFA)
                    foreignCurrencyRate = 0,
                    items = factureData.Produits.Select(p => new
                    {
                        taxes = new[] { p.CodeTva }, // G{row} - TVA, TVAB, TVAC, TVAD
                        customTaxes = new object[0], // Vide par défaut
                        reference = p.CodeProduit, // B{row}
                        description = p.Designation, // C{row}
                        quantity = p.Quantite, // E{row}
                        amount = p.PrixUnitaire, // D{row} - Prix unitaire HT
                        discount = 0, // Pas de remise par défaut
                        measurementUnit = p.Emballage ?? "pcs" // F{row}
                    }).ToArray(),
                    customTaxes = new object[0], // Taxes personnalisées au niveau facture (vide par défaut)
                    discount = 0 // Remise globale (vide par défaut)
                };

                return fneJson;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur conversion JSON FNE: {ex.Message}", "Sage100Import", ex);
                throw;
            }
        }

        /// <summary>
        /// Certifie une facture via l'API FNE
        /// </summary>
        public async Task<object> CertifyInvoiceAsync(Sage100FactureData factureData)
        {
            try
            {
                // Convertir vers le format JSON FNE
                var fneJson = await ConvertToFneApiJsonAsync(factureData);

                // TODO: Implémenter l'appel à l'API FNE
                // POST http://54.247.95.108/ws/external/invoices/sign
                // Headers: Authorization: Bearer {API_KEY}, Content-Type: application/json
                // Body: fneJson

                await _loggingService.LogInfoAsync(
                    $"JSON FNE généré pour facture {factureData.NumeroFacture}: {System.Text.Json.JsonSerializer.Serialize(fneJson)}", 
                    "Sage100Import");

                return fneJson;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur certification facture {factureData.NumeroFacture}: {ex.Message}", "Sage100Import", ex);
                throw;
            }
        }

        /// <summary>
        /// Analyse et valide les informations client pour la certification FNE
        /// </summary>
        private async Task AnalyzerInformationsClientFNE(Sage100FacturePreview preview, string codeClient)
        {
            // Détermination du type de facturation FNE basé sur le code client
            if (codeClient == "1999")
            {
                preview.TypeFacture = "B2C"; // Client divers/particulier
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
            
            // Validation client
            if (codeClient != "1999")
            {
                var clientBDD = await _clientRepository.GetByClientCodeAsync(codeClient);
                preview.ClientTrouve = clientBDD != null;
            }
            else
            {
                preview.ClientTrouve = true; // Client divers est toujours "trouvé"
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

        /// <summary>
        /// Convertit les données Sage 100 en entité FneInvoice
        /// </summary>
        private async Task<FneInvoice?> ConvertToFneInvoiceAsync(Sage100FactureData factureData, string worksheetName)
        {
            try
            {
                // Récupérer le client (ou créer client divers si nécessaire)
                var client = await GetOrCreateClientAsync(factureData);
                if (client == null)
                {
                    await _loggingService.LogErrorAsync($"Impossible de récupérer/créer le client {factureData.CodeClient}", "Sage100Import");
                    return null;
                }

                var fneInvoice = new FneInvoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = factureData.NumeroFacture,
                    InvoiceType = !string.IsNullOrWhiteSpace(factureData.NumeroFactureAvoir) ? "refund" : "sale",
                    InvoiceDate = factureData.DateFacture,
                    ClientId = client.Id,
                    ClientCode = factureData.CodeClient,
                    PointOfSale = factureData.PointDeVente,
                    PaymentMethod = factureData.MoyenPaiement,
                    Status = "draft",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Calculer les montants totaux
                decimal totalHT = factureData.Produits.Sum(p => p.MontantHt);
                decimal totalTVA = factureData.Produits.Sum(p => GetVatAmountFromProduct(p));
                
                fneInvoice.TotalAmountHT = totalHT;
                fneInvoice.TotalVatAmount = totalTVA;
                fneInvoice.TotalAmountTTC = totalHT + totalTVA;
                
                // Si c'est un avoir, les montants sont négatifs
                if (fneInvoice.InvoiceType == "refund")
                {
                    fneInvoice.TotalAmountHT = -Math.Abs(fneInvoice.TotalAmountHT);
                    fneInvoice.TotalVatAmount = -Math.Abs(fneInvoice.TotalVatAmount);
                    fneInvoice.TotalAmountTTC = -Math.Abs(fneInvoice.TotalAmountTTC);
                    // Note: RefundReference n'existe pas dans l'entité FneInvoice actuelle
                }

                return fneInvoice;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur conversion facture {factureData.NumeroFacture}: {ex.Message}", "Sage100Import");
                return null;
            }
        }

        /// <summary>
        /// Récupère un client existant ou crée un client divers
        /// </summary>
        private async Task<Client?> GetOrCreateClientAsync(Sage100FactureData factureData)
        {
            try
            {
                if (factureData.CodeClient == "1999")
                {
                    // Client divers - chercher par NCC ou créer
                    var clientDivers = await _clientRepository.GetByNccAsync(factureData.GetNccClient());
                    if (clientDivers == null)
                    {
                        // Créer nouveau client divers
                        clientDivers = new Client
                        {
                            Id = Guid.NewGuid(),
                            ClientCode = "1999",
                            CompanyName = factureData.NomReelClientDivers ?? factureData.IntituleClient,
                            ClientNcc = factureData.GetNccClient(),
                            ClientType = "divers",
                            DefaultPaymentMethod = factureData.MoyenPaiement,
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow,
                            LastModifiedDate = DateTime.UtcNow
                        };
                        
                        await _clientRepository.CreateAsync(clientDivers);
                        await _loggingService.LogInfoAsync($"Client divers créé: {clientDivers.CompanyName} (NCC: {clientDivers.ClientNcc})", "Sage100Import");
                    }
                    return clientDivers;
                }
                else
                {
                    // Client normal - doit exister
                    var client = await _clientRepository.GetByClientCodeAsync(factureData.CodeClient);
                    if (client == null)
                    {
                        await _loggingService.LogErrorAsync($"Client {factureData.CodeClient} introuvable", "Sage100Import");
                    }
                    return client;
                }
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Erreur récupération client {factureData.CodeClient}: {ex.Message}", "Sage100Import");
                return null;
            }
        }

        /// <summary>
        /// Calcule le montant TVA d'un produit basé sur son code TVA et montant HT
        /// </summary>
        private decimal GetVatAmountFromProduct(Sage100ProduitData produit)
        {
            var vatRate = GetVatRateFromCode(produit.CodeTva);
            return produit.MontantHt * (vatRate / 100);
        }

        /// <summary>
        /// Obtient le taux de TVA à partir du code TVA
        /// </summary>
        private decimal GetVatRateFromCode(string codeTva)
        {
            return codeTva?.ToUpper() switch
            {
                "TVA" => 18.0m,    // 18%
                "TVAB" => 9.0m,    // 9%
                "TVAC" => 0.0m,    // 0% (convention)
                "TVAD" => 0.0m,    // 0% (légale)
                _ => 18.0m         // Par défaut 18%
            };
        }

        #endregion
    }
}
