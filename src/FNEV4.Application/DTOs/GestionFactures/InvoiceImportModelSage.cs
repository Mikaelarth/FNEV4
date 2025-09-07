using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FNEV4.Application.DTOs.GestionFactures
{
    /// <summary>
    /// Modèle d'import Excel pour les factures Sage 100 v15
    /// Structure spécifique : 1 classeur = N factures, 1 feuille = 1 facture
    /// </summary>
    public class InvoiceImportModelSage
    {
        #region En-tête Facture (Colonne A)
        
        /// <summary>
        /// Numéro de facture (Ligne A3)
        /// </summary>
        [Required(ErrorMessage = "Le numéro de facture est obligatoire")]
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Code client (Ligne A5)
        /// 1999 = client divers, autre = client normal
        /// </summary>
        [Required(ErrorMessage = "Le code client est obligatoire")]
        public string CustomerCode { get; set; } = string.Empty;

        /// <summary>
        /// NCC client normal (Ligne A6)
        /// Obligatoire si CustomerCode ≠ 1999
        /// </summary>
        public string CustomerNcc { get; set; } = string.Empty;

        /// <summary>
        /// Date facture (Ligne A8)
        /// Format Excel : nombre de jours depuis 1900-01-01
        /// </summary>
        [Required(ErrorMessage = "La date de facture est obligatoire")]
        public DateTime InvoiceDate { get; set; }

        /// <summary>
        /// Point de vente (Ligne A10)
        /// </summary>
        [Required(ErrorMessage = "Le point de vente est obligatoire")]
        public string PointOfSale { get; set; } = string.Empty;

        /// <summary>
        /// Intitulé client (Ligne A11)
        /// Nom générique pour clients divers ou nom du client normal
        /// </summary>
        [Required(ErrorMessage = "L'intitulé client est obligatoire")]
        public string CustomerTitle { get; set; } = string.Empty;

        /// <summary>
        /// Nom réel client divers (Ligne A13)
        /// Obligatoire si CustomerCode = 1999
        /// </summary>
        public string CustomerRealName { get; set; } = string.Empty;

        /// <summary>
        /// NCC client divers (Ligne A15)
        /// Obligatoire si CustomerCode = 1999
        /// </summary>
        public string CustomerDiversNcc { get; set; } = string.Empty;

        /// <summary>
        /// Numéro facture avoir (Ligne A17)
        /// Référence facture originale si c'est un avoir
        /// </summary>
        public string CreditNoteReference { get; set; } = string.Empty;

        /// <summary>
        /// Moyen de paiement (Ligne A18) - NOUVEAU
        /// cash, card, mobile-money, bank-transfer, check, credit
        /// </summary>
        [Required(ErrorMessage = "Le moyen de paiement est obligatoire")]
        public string PaymentMethod { get; set; } = "cash";

        #endregion

        #region Lignes Produits (à partir ligne 20)

        /// <summary>
        /// Liste des lignes de produits de la facture
        /// </summary>
        public List<InvoiceItemImportModel> Items { get; set; } = new();

        #endregion

        #region Métadonnées Import

        /// <summary>
        /// Nom du fichier Excel source
        /// </summary>
        public string SourceFileName { get; set; } = string.Empty;

        /// <summary>
        /// Nom de la feuille Excel (= nom de la facture)
        /// </summary>
        public string SourceSheetName { get; set; } = string.Empty;

        /// <summary>
        /// Erreurs de validation
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new();

        /// <summary>
        /// Indique si le modèle est valide
        /// </summary>
        public bool IsValid => ValidationErrors.Count == 0;

        /// <summary>
        /// Type de client détecté
        /// </summary>
        public ClientType ClientType => CustomerCode == "1999" ? ClientType.Divers : ClientType.Normal;

        /// <summary>
        /// Indique si c'est une facture d'avoir
        /// </summary>
        public bool IsCreditNote => !string.IsNullOrWhiteSpace(CreditNoteReference);

        #endregion

        #region Validation Métier

        /// <summary>
        /// Valide les règles métier spécifiques aux factures Sage 100
        /// </summary>
        public void ValidateBusinessRules()
        {
            ValidationErrors.Clear();

            // Validation numéro facture
            if (string.IsNullOrWhiteSpace(InvoiceNumber))
            {
                ValidationErrors.Add("Le numéro de facture est obligatoire");
            }

            // Validation client selon type
            if (ClientType == ClientType.Divers)
            {
                if (string.IsNullOrWhiteSpace(CustomerRealName))
                {
                    ValidationErrors.Add("Le nom réel du client divers est obligatoire (ligne A13)");
                }

                if (string.IsNullOrWhiteSpace(CustomerDiversNcc))
                {
                    ValidationErrors.Add("Le NCC du client divers est obligatoire (ligne A15)");
                }
                else if (!IsValidNcc(CustomerDiversNcc))
                {
                    ValidationErrors.Add("Format NCC client divers invalide (8-11 caractères alphanumériques)");
                }
            }
            else // Client normal
            {
                if (string.IsNullOrWhiteSpace(CustomerNcc))
                {
                    ValidationErrors.Add("Le NCC du client normal est obligatoire (ligne A6)");
                }
                else if (!IsValidNcc(CustomerNcc))
                {
                    ValidationErrors.Add("Format NCC client normal invalide (8-11 caractères alphanumériques)");
                }
            }

            // Validation moyen de paiement
            var validPaymentMethods = new[] { "cash", "card", "mobile-money", "bank-transfer", "check", "credit" };
            if (!validPaymentMethods.Contains(PaymentMethod?.ToLower()))
            {
                ValidationErrors.Add($"Moyen de paiement invalide. Valeurs autorisées : {string.Join(", ", validPaymentMethods)}");
            }

            // Validation date
            if (InvoiceDate == default(DateTime))
            {
                ValidationErrors.Add("La date de facture est obligatoire");
            }
            else if (InvoiceDate > DateTime.Now.AddDays(1))
            {
                ValidationErrors.Add("La date de facture ne peut pas être future");
            }

            // Validation lignes produits
            if (Items == null || Items.Count == 0)
            {
                ValidationErrors.Add("Au moins une ligne de produit est obligatoire");
            }
            else
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    item.ValidateBusinessRules();
                    
                    // Ajouter les erreurs avec le numéro de ligne
                    foreach (var error in item.ValidationErrors)
                    {
                        ValidationErrors.Add($"Ligne {i + 1}: {error}");
                    }
                }
            }

            // Validation cohérence avoir
            if (IsCreditNote)
            {
                if (string.IsNullOrWhiteSpace(CreditNoteReference))
                {
                    ValidationErrors.Add("La référence de la facture originale est obligatoire pour un avoir");
                }

                // Vérifier que les montants sont négatifs pour un avoir
                var totalHT = Items?.Sum(i => i.AmountHT) ?? 0;
                if (totalHT >= 0)
                {
                    ValidationErrors.Add("Les montants d'un avoir doivent être négatifs");
                }
            }
        }

        /// <summary>
        /// Valide le format d'un NCC
        /// </summary>
        private bool IsValidNcc(string ncc)
        {
            if (string.IsNullOrWhiteSpace(ncc))
                return false;

            // NCC : 8-11 caractères alphanumériques
            return ncc.Length >= 8 && ncc.Length <= 11 && 
                   System.Text.RegularExpressions.Regex.IsMatch(ncc, @"^[A-Za-z0-9]+$");
        }

        #endregion

        #region Conversion Entity

        /// <summary>
        /// Convertit vers l'entité Invoice
        /// </summary>
        public FNEV4.Core.Entities.Invoice ToInvoiceEntity()
        {
            return new FNEV4.Core.Entities.Invoice
            {
                InvoiceNumber = InvoiceNumber,
                CustomerCode = CustomerCode,
                CustomerNcc = ClientType == ClientType.Divers ? CustomerDiversNcc : CustomerNcc,
                InvoiceDate = InvoiceDate,
                PointOfSale = PointOfSale,
                CustomerTitle = CustomerTitle,
                CustomerRealName = ClientType == ClientType.Divers ? CustomerRealName : CustomerTitle,
                CreditNoteReference = CreditNoteReference,
                PaymentMethod = PaymentMethod.ToLower(), // Normaliser en minuscules
                
                // Calculs automatiques depuis les lignes
                TotalAmountHT = Items?.Sum(i => i.AmountHT) ?? 0,
                TotalVatAmount = Items?.Sum(i => i.VatAmount) ?? 0,
                TotalAmountTTC = Items?.Sum(i => i.AmountTTC) ?? 0,
                
                // Métadonnées
                SourceFileName = SourceFileName,
                SourceSheetName = SourceSheetName,
                Status = "Imported",
                
                // Relations
                Items = Items?.Select((item, index) => item.ToInvoiceItemEntity(index + 1)).ToList() ?? new List<FNEV4.Core.Entities.InvoiceItem>(),
                
                // Audit
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        #endregion
    }

    /// <summary>
    /// Type de client selon le code
    /// </summary>
    public enum ClientType
    {
        Normal,   // Code ≠ 1999
        Divers    // Code = 1999
    }

    /// <summary>
    /// Modèle pour une ligne de produit (à partir ligne 20)
    /// </summary>
    public class InvoiceItemImportModel
    {
        /// <summary>
        /// Code produit (Colonne B)
        /// </summary>
        [Required(ErrorMessage = "Le code produit est obligatoire")]
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>
        /// Désignation produit (Colonne C)
        /// </summary>
        [Required(ErrorMessage = "La désignation produit est obligatoire")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Prix unitaire (Colonne D)
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Le prix unitaire doit être positif")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Quantité (Colonne E)
        /// </summary>
        [Range(0.001, double.MaxValue, ErrorMessage = "La quantité doit être positive")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Unité/Emballage (Colonne F)
        /// </summary>
        public string Unit { get; set; } = "pcs";

        /// <summary>
        /// Type de TVA (Colonne G)
        /// TVA = 18%, TVAB = 9%, TVAC = 0%, TVAD = 0%
        /// </summary>
        [Required(ErrorMessage = "Le type de TVA est obligatoire")]
        public string VatType { get; set; } = "TVA";

        /// <summary>
        /// Montant HT (Colonne H)
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Le montant HT doit être positif")]
        public decimal AmountHT { get; set; }

        /// <summary>
        /// Taux de TVA calculé selon le type
        /// </summary>
        public decimal VatRate => VatType?.ToUpper() switch
        {
            "TVA" => 0.18m,    // 18%
            "TVAB" => 0.09m,   // 9%
            "TVAC" => 0.00m,   // 0% (convention)
            "TVAD" => 0.00m,   // 0% (légale)
            _ => 0.18m         // Par défaut 18%
        };

        /// <summary>
        /// Montant TVA calculé
        /// </summary>
        public decimal VatAmount => AmountHT * VatRate;

        /// <summary>
        /// Montant TTC calculé
        /// </summary>
        public decimal AmountTTC => AmountHT + VatAmount;

        /// <summary>
        /// Erreurs de validation
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new();

        /// <summary>
        /// Valide les règles métier pour une ligne produit
        /// </summary>
        public void ValidateBusinessRules()
        {
            ValidationErrors.Clear();

            // Validation cohérence quantité × prix = montant
            var expectedAmount = UnitPrice * Quantity;
            var tolerance = 0.01m; // Tolérance de 1 centime
            
            if (Math.Abs(AmountHT - expectedAmount) > tolerance)
            {
                ValidationErrors.Add($"Incohérence calcul : {UnitPrice} × {Quantity} = {expectedAmount:F2}, mais AmountHT = {AmountHT:F2}");
            }

            // Validation type TVA
            var validVatTypes = new[] { "TVA", "TVAB", "TVAC", "TVAD" };
            if (!validVatTypes.Contains(VatType?.ToUpper()))
            {
                ValidationErrors.Add($"Type TVA invalide. Types autorisés : {string.Join(", ", validVatTypes)}");
            }

            // Validation montants positifs
            if (UnitPrice < 0)
            {
                ValidationErrors.Add("Le prix unitaire ne peut pas être négatif");
            }

            if (Quantity <= 0)
            {
                ValidationErrors.Add("La quantité doit être positive");
            }

            if (AmountHT < 0)
            {
                ValidationErrors.Add("Le montant HT ne peut pas être négatif");
            }
        }

        /// <summary>
        /// Convertit vers l'entité InvoiceItem
        /// </summary>
        public FNEV4.Core.Entities.InvoiceItem ToInvoiceItemEntity(int lineNumber)
        {
            return new FNEV4.Core.Entities.InvoiceItem
            {
                LineNumber = lineNumber,
                ProductCode = ProductCode,
                Description = Description,
                UnitPrice = UnitPrice,
                Quantity = Quantity,
                Unit = Unit,
                VatType = VatType.ToUpper(),
                AmountHT = AmountHT,
                VatRate = VatRate,
                VatAmount = VatAmount,
                AmountTTC = AmountTTC,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
    }
}
