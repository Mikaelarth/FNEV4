using ClosedXML.Excel;
using FNEV4.Application.DTOs.GestionFactures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FNEV4.Application.Services.GestionFactures
{
    /// <summary>
    /// Service d'import des factures depuis Excel Sage 100 v15
    /// Structure : 1 classeur = N factures, 1 feuille = 1 facture
    /// </summary>
    public class InvoiceExcelImportService
    {
        public InvoiceExcelImportService()
        {
        }

        #region Import Principal

        /// <summary>
        /// Importe toutes les factures depuis un fichier Excel Sage 100
        /// </summary>
        /// <param name="filePath">Chemin du fichier Excel</param>
        /// <returns>Liste des factures importées avec erreurs éventuelles</returns>
        public Task<InvoiceImportResult> ImportInvoicesFromExcelAsync(string filePath)
        {
            return Task.Run(() => ImportInvoicesFromExcel(filePath));
        }

        /// <summary>
        /// Version synchrone de l'import
        /// </summary>
        public InvoiceImportResult ImportInvoicesFromExcel(string filePath)
        {
            var result = new InvoiceImportResult
            {
                SourceFile = Path.GetFileName(filePath),
                StartTime = DateTime.Now
            };

            try
            {
                Console.WriteLine($"Début import factures depuis {filePath}");

                if (!File.Exists(filePath))
                {
                    result.AddGlobalError($"Fichier introuvable : {filePath}");
                    return result;
                }

                using var workbook = new XLWorkbook(filePath);
                
                Console.WriteLine($"Fichier Excel ouvert. Nombre de feuilles : {workbook.Worksheets.Count}");

                // Traiter chaque feuille = 1 facture
                foreach (var worksheet in workbook.Worksheets)
                {
                    try
                    {
                        var invoice = ParseInvoiceFromWorksheet(worksheet, result.SourceFile);
                        result.AddInvoice(invoice);
                        
                        Console.WriteLine($"Facture {invoice.InvoiceNumber} importée depuis feuille {worksheet.Name}");
                    }
                    catch (Exception ex)
                    {
                        var error = $"Erreur feuille '{worksheet.Name}': {ex.Message}";
                        result.AddGlobalError(error);
                        Console.WriteLine($"Erreur import feuille {worksheet.Name}: {ex.Message}");
                    }
                }

                result.EndTime = DateTime.Now;
                result.IsSuccess = result.GlobalErrors.Count == 0 && result.ImportedInvoices.Any(i => i.IsValid);

                Console.WriteLine($"Import terminé. Succès: {result.IsSuccess}, Factures: {result.ImportedInvoices.Count}, Erreurs: {result.GlobalErrors.Count}");

                return result;
            }
            catch (Exception ex)
            {
                result.AddGlobalError($"Erreur critique : {ex.Message}");
                result.EndTime = DateTime.Now;
                Console.WriteLine($"Erreur critique import {filePath}: {ex.Message}");
                return result;
            }
        }

        #endregion

        #region Parsing Feuille Excel

        /// <summary>
        /// Parse une feuille Excel en facture selon structure Sage 100 v15
        /// </summary>
        private InvoiceImportModelSage ParseInvoiceFromWorksheet(IXLWorksheet worksheet, string sourceFileName)
        {
            var invoice = new InvoiceImportModelSage
            {
                SourceFileName = sourceFileName,
                SourceSheetName = worksheet.Name
            };

            try
            {
                // 1. Parser l'en-tête facture (colonne A)
                ParseInvoiceHeader(worksheet, invoice);

                // 2. Parser les lignes produits (à partir ligne 20)
                ParseInvoiceItems(worksheet, invoice);

                // 3. Valider la cohérence
                invoice.ValidateBusinessRules();

                Console.WriteLine($"Facture {invoice.InvoiceNumber} parsée avec {invoice.Items.Count} lignes et {invoice.ValidationErrors.Count} erreurs");

                return invoice;
            }
            catch (Exception ex)
            {
                invoice.ValidationErrors.Add($"Erreur parsing : {ex.Message}");
                Console.WriteLine($"Erreur parsing feuille {worksheet.Name}: {ex.Message}");
                return invoice;
            }
        }

        /// <summary>
        /// Parse l'en-tête de la facture depuis la colonne A
        /// </summary>
        private void ParseInvoiceHeader(IXLWorksheet worksheet, InvoiceImportModelSage invoice)
        {
            // Numéro facture (A3)
            invoice.InvoiceNumber = GetCellValueAsString(worksheet, "A3");

            // Code client (A5)
            invoice.CustomerCode = GetCellValueAsString(worksheet, "A5");

            // NCC client normal (A6)
            invoice.CustomerNcc = GetCellValueAsString(worksheet, "A6");

            // Date facture (A8) - Excel stocke les dates comme nombre de jours depuis 1900-01-01
            var dateValue = GetCellValueAsString(worksheet, "A8");
            if (double.TryParse(dateValue, out double excelDate))
            {
                try
                {
                    invoice.InvoiceDate = DateTime.FromOADate(excelDate);
                }
                catch
                {
                    invoice.ValidationErrors.Add($"Date invalide en A8 : {dateValue}");
                }
            }
            else if (DateTime.TryParse(dateValue, out DateTime parsedDate))
            {
                invoice.InvoiceDate = parsedDate;
            }
            else
            {
                invoice.ValidationErrors.Add($"Format date invalide en A8 : {dateValue}");
            }

            // Point de vente (A10)
            invoice.PointOfSale = GetCellValueAsString(worksheet, "A10");

            // Intitulé client (A11)
            invoice.CustomerTitle = GetCellValueAsString(worksheet, "A11");

            // Nom réel client divers (A13)
            invoice.CustomerRealName = GetCellValueAsString(worksheet, "A13");

            // NCC client divers (A15)
            invoice.CustomerDiversNcc = GetCellValueAsString(worksheet, "A15");

            // Numéro facture avoir (A17)
            invoice.CreditNoteReference = GetCellValueAsString(worksheet, "A17");

            // Moyen de paiement (A18) - NOUVEAU
            var paymentMethod = GetCellValueAsString(worksheet, "A18");
            invoice.PaymentMethod = NormalizePaymentMethod(paymentMethod);

            Console.WriteLine($"En-tête parsé pour facture {invoice.InvoiceNumber}, client {invoice.CustomerCode}, paiement {invoice.PaymentMethod}");
        }

        /// <summary>
        /// Parse les lignes de produits à partir de la ligne 20
        /// </summary>
        private void ParseInvoiceItems(IXLWorksheet worksheet, InvoiceImportModelSage invoice)
        {
            const int START_ROW = 20;
            var currentRow = START_ROW;
            var itemsParsed = 0;

            while (currentRow <= worksheet.LastRowUsed()?.RowNumber())
            {
                // Vérifier si la ligne contient des données
                var productCode = GetCellValueAsString(worksheet, $"B{currentRow}");
                
                if (string.IsNullOrWhiteSpace(productCode))
                {
                    currentRow++;
                    continue; // Ligne vide, passer à la suivante
                }

                try
                {
                    var item = ParseInvoiceItem(worksheet, currentRow);
                    if (item != null)
                    {
                        invoice.Items.Add(item);
                        itemsParsed++;
                    }
                }
                catch (Exception ex)
                {
                    invoice.ValidationErrors.Add($"Erreur ligne {currentRow}: {ex.Message}");
                    Console.WriteLine($"Erreur parsing ligne {currentRow}: {ex.Message}");
                }

                currentRow++;
            }

            Console.WriteLine($"Lignes produits parsées: {itemsParsed} depuis ligne {START_ROW}");
        }

        /// <summary>
        /// Parse une ligne de produit spécifique
        /// </summary>
        private InvoiceItemImportModel? ParseInvoiceItem(IXLWorksheet worksheet, int row)
        {
            var item = new InvoiceItemImportModel();

            // Code produit (B)
            item.ProductCode = GetCellValueAsString(worksheet, $"B{row}");
            if (string.IsNullOrWhiteSpace(item.ProductCode))
                return null;

            // Désignation (C)
            item.Description = GetCellValueAsString(worksheet, $"C{row}");

            // Prix unitaire (D)
            if (decimal.TryParse(GetCellValueAsString(worksheet, $"D{row}"), out decimal unitPrice))
            {
                item.UnitPrice = unitPrice;
            }

            // Quantité (E)
            if (decimal.TryParse(GetCellValueAsString(worksheet, $"E{row}"), out decimal quantity))
            {
                item.Quantity = quantity;
            }

            // Unité/Emballage (F)
            item.Unit = GetCellValueAsString(worksheet, $"F{row}");
            if (string.IsNullOrWhiteSpace(item.Unit))
                item.Unit = "pcs";

            // Type TVA (G)
            item.VatType = GetCellValueAsString(worksheet, $"G{row}");
            if (string.IsNullOrWhiteSpace(item.VatType))
                item.VatType = "TVA";

            // Montant HT (H)
            if (decimal.TryParse(GetCellValueAsString(worksheet, $"H{row}"), out decimal amountHT))
            {
                item.AmountHT = amountHT;
            }

            // Trace simple pour debug
            // Console.WriteLine($"Item parsé ligne {row}: {item.ProductCode} - {item.Description}");

            return item;
        }

        #endregion

        #region Utilitaires

        /// <summary>
        /// Récupère la valeur d'une cellule en tant que string
        /// </summary>
        private string GetCellValueAsString(IXLWorksheet worksheet, string cellAddress)
        {
            try
            {
                var cell = worksheet.Cell(cellAddress);
                if (cell.IsEmpty())
                    return string.Empty;

                return cell.Value.ToString().Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Normalise le moyen de paiement selon API DGI
        /// </summary>
        private string NormalizePaymentMethod(string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
                return "cash";

            return paymentMethod.ToLower().Trim() switch
            {
                "especes" or "espèces" or "cash" or "liquide" => "cash",
                "carte" or "card" or "cb" or "visa" or "mastercard" => "card",
                "mobile" or "mobile-money" or "momo" or "orange" or "mtn" => "mobile-money",
                "virement" or "bank-transfer" or "banque" or "transfer" => "bank-transfer",
                "cheque" or "chèque" or "check" => "check",
                "credit" or "crédit" or "a-terme" or "terme" => "credit",
                _ => "cash" // Par défaut
            };
        }

        #endregion

        #region Validation Fichier

        /// <summary>
        /// Valide qu'un fichier est bien un Excel Sage 100 v15
        /// </summary>
        public Task<ValidationResult> ValidateExcelFileAsync(string filePath)
        {
            return Task.Run(() => ValidateExcelFile(filePath));
        }

        /// <summary>
        /// Version synchrone de la validation
        /// </summary>
        public ValidationResult ValidateExcelFile(string filePath)
        {
            var result = new ValidationResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.AddError("Fichier introuvable");
                    return result;
                }

                var extension = Path.GetExtension(filePath).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    result.AddError("Format de fichier non supporté. Utilisez .xlsx ou .xls");
                    return result;
                }

                using var workbook = new XLWorkbook(filePath);

                if (workbook.Worksheets.Count == 0)
                {
                    result.AddError("Aucune feuille trouvée dans le fichier");
                    return result;
                }

                // Valider quelques feuilles pour détecter la structure Sage 100
                var sheetsValidated = 0;
                foreach (var worksheet in workbook.Worksheets.Take(3)) // Valider max 3 feuilles
                {
                    if (ValidateWorksheetStructure(worksheet))
                    {
                        sheetsValidated++;
                    }
                }

                if (sheetsValidated == 0)
                {
                    result.AddError("Aucune feuille compatible Sage 100 v15 détectée");
                }
                else
                {
                    result.IsValid = true;
                    result.AddMessage($"{sheetsValidated} feuille(s) compatible(s) détectée(s)");
                }

                return result;
            }
            catch (Exception ex)
            {
                result.AddError($"Erreur validation : {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Valide la structure d'une feuille Sage 100
        /// </summary>
        private bool ValidateWorksheetStructure(IXLWorksheet worksheet)
        {
            try
            {
                // Vérifier la présence de cellules clés
                var hasInvoiceNumber = !string.IsNullOrWhiteSpace(GetCellValueAsString(worksheet, "A3"));
                var hasCustomerCode = !string.IsNullOrWhiteSpace(GetCellValueAsString(worksheet, "A5"));
                var hasDate = !string.IsNullOrWhiteSpace(GetCellValueAsString(worksheet, "A8"));

                return hasInvoiceNumber && hasCustomerCode && hasDate;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    #region Modèles de Résultat

    /// <summary>
    /// Résultat d'un import de factures
    /// </summary>
    public class InvoiceImportResult
    {
        public string SourceFile { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsSuccess { get; set; }
        public List<string> GlobalErrors { get; set; } = new();
        public List<InvoiceImportModelSage> ImportedInvoices { get; set; } = new();

        public TimeSpan Duration => EndTime - StartTime;
        public int TotalInvoices => ImportedInvoices.Count;
        public int ValidInvoices => ImportedInvoices.Count(i => i.IsValid);
        public int InvalidInvoices => ImportedInvoices.Count(i => !i.IsValid);

        public void AddGlobalError(string error)
        {
            GlobalErrors.Add(error);
        }

        public void AddInvoice(InvoiceImportModelSage invoice)
        {
            ImportedInvoices.Add(invoice);
        }

        public string GetSummary()
        {
            return $"Import {SourceFile}: {ValidInvoices}/{TotalInvoices} factures valides en {Duration.TotalSeconds:F1}s";
        }
    }

    /// <summary>
    /// Résultat de validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Messages { get; set; } = new();

        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        public void AddMessage(string message)
        {
            Messages.Add(message);
        }
    }

    #endregion
}
