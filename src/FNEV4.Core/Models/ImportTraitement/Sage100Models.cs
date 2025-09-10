using System;
using System.Collections.Generic;

namespace FNEV4.Core.Models.ImportTraitement
{
    /// <summary>
    /// Résultat d'un import Sage 100 v15
    /// </summary>
    public class Sage100ImportResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int FacturesImportees { get; set; }
        public int FacturesEchouees { get; set; }
        public TimeSpan DureeTraitement { get; set; }
        public List<Sage100FactureImportee> FacturesDetaillees { get; set; } = new();
    }

    /// <summary>
    /// Résultat de validation d'un fichier Sage 100 v15
    /// </summary>
    public class Sage100ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int NombreFeuilles { get; set; }
        public List<string> NomsFeuillesValides { get; set; } = new();
        public List<string> NomsFeuillesInvalides { get; set; } = new();
    }

    /// <summary>
    /// Aperçu d'un fichier Sage 100 v15
    /// </summary>
    public class Sage100PreviewResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int NombreFeuilles { get; set; }
        public int FacturesDetectees { get; set; }
        public List<Sage100FacturePreview> Apercu { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Données d'une facture Sage 100 v15
    /// </summary>
    public class Sage100FactureData
    {
        // En-tête facture (selon structure Excel officielle)
        public string NumeroFacture { get; set; } = string.Empty;           // A3
        public string CodeClient { get; set; } = string.Empty;              // A5  
        public string NccClientNormal { get; set; } = string.Empty;         // A6 (si code ≠ 1999)
        public DateTime DateFacture { get; set; }                           // A8
        public string PointDeVente { get; set; } = string.Empty;            // A10
        public string IntituleClient { get; set; } = string.Empty;          // A11
        
        // Spécifique clients divers (code 1999)
        public string NomReelClientDivers { get; set; } = string.Empty;     // A13 (si code = 1999)
        public string NccClientDivers { get; set; } = string.Empty;         // A15 (si code = 1999)
        
        // Autres infos
        public string NumeroFactureAvoir { get; set; } = string.Empty;      // A17
        public string MoyenPaiement { get; set; } = string.Empty;           // A18
        
        // Produits (à partir ligne 20)
        public List<Sage100ProduitData> Produits { get; set; } = new();
        
        // Métadonnées
        public string NomFeuille { get; set; } = string.Empty;
        public string NomFichierSource { get; set; } = string.Empty;
        
        // Propriétés calculées
        public bool EstClientDivers => CodeClient == "1999";
        public bool EstAvoir => !string.IsNullOrWhiteSpace(NumeroFactureAvoir);
        
        // Validation selon la structure Excel officielle
        public string GetNccClient() => EstClientDivers ? NccClientDivers : NccClientNormal;
        public string GetNomClient() => EstClientDivers ? NomReelClientDivers : IntituleClient;
    }

    /// <summary>
    /// Données d'un produit Sage 100 v15 (selon structure Excel officielle)
    /// </summary>
    public class Sage100ProduitData
    {
        public string CodeProduit { get; set; } = string.Empty;              // Colonne B (ex: "ORD001")
        public string Designation { get; set; } = string.Empty;             // Colonne C (ex: "Ordinateur Dell")
        public decimal PrixUnitaire { get; set; }                           // Colonne D (ex: 800000)
        public decimal Quantite { get; set; }                               // Colonne E (ex: 1)
        public string Emballage { get; set; } = string.Empty;               // Colonne F (ex: "pcs")
        public string CodeTva { get; set; } = string.Empty;                 // Colonne G (ex: "TVA")
        public decimal MontantHt { get; set; }                              // Colonne H (ex: 800000)
        public int NumeroLigne { get; set; }                                // Numéro de ligne dans Excel (≥ 20)
        
        // Propriétés calculées
        public decimal MontantTva => CodeTva == "TVA" ? MontantHt * 0.18m : 0;
        public decimal MontantTtc => MontantHt + MontantTva;
        
        // Validation
        public bool EstValide => !string.IsNullOrWhiteSpace(Designation) && 
                                PrixUnitaire > 0 && 
                                Quantite > 0 && 
                                MontantHt > 0;
    }

    /// <summary>
    /// Facture importée avec détails
    /// </summary>
    public class Sage100FactureImportee
    {
        public string NumeroFacture { get; set; } = string.Empty;
        public string NomFeuille { get; set; } = string.Empty;
        public bool EstImportee { get; set; }
        public string MessageErreur { get; set; } = string.Empty;
        public DateTime DateImport { get; set; }
        public int NombreProduits { get; set; }
        public decimal MontantTotal { get; set; }
    }

    /// <summary>
    /// Aperçu d'une facture
    /// </summary>
    public class Sage100FacturePreview
    {
        // Métadonnées fichier
        public string NomFeuille { get; set; } = string.Empty;
        public string NomFichierSource { get; set; } = string.Empty;
        
        // Données de base facture (selon structure Excel)
        public string NumeroFacture { get; set; } = string.Empty;           // A3
        public string CodeClient { get; set; } = string.Empty;              // A5
        public DateTime DateFacture { get; set; }                           // A8
        public string PointDeVente { get; set; } = string.Empty;            // A10 (ex: "Gestoci")
        public string MoyenPaiement { get; set; } = string.Empty;           // A18 (ex: "cash", "card")
        
        // Client (dépend du type)
        public string NccClient { get; set; } = string.Empty;               // A6 ou A15
        public string NomClient { get; set; } = string.Empty;               // A11 ou A13
        
        // Données calculées
        public int NombreProduits { get; set; }
        public decimal MontantHT { get; set; }                              // Calculé depuis produits
        public decimal MontantTTC { get; set; }                             // Calculé depuis produits  
        public decimal MontantTVA => MontantTTC - MontantHT;
        
        // Validation et statut
        public bool EstValide { get; set; }
        public bool ClientTrouve { get; set; }
        public List<string> Erreurs { get; set; } = new();
        public string Statut => EstValide ? "Valide" : "Erreur";
        
        // Liste des produits pour affichage détaillé
        public List<Sage100ProduitData> Produits { get; set; } = new();
        
        // Propriétés FNE
        public string TypeFacture { get; set; } = "B2B";                    // B2B, B2C, B2G, B2F
        public string Template { get; set; } = "N/A";                       // Template du client depuis la DB
        public bool EstClientDivers => CodeClient == "1999";
        public bool EstAvoir { get; set; }
        
        // Alias pour compatibilité interface
        public string FichierSource => NomFichierSource;
        public string IntituleClient => NomClient;
    }

    /// <summary>
    /// Moyens de paiement A18 supportés pour certification FNE
    /// </summary>
    public static class MoyensPaiementA18
    {
        public const string Cash = "cash";
        public const string Card = "card";
        public const string MobileMoney = "mobile-money";
        public const string BankTransfer = "bank-transfer";
        public const string Check = "check";
        public const string Credit = "credit";
        
        public static readonly List<string> Valides = new()
        {
            Cash, Card, MobileMoney, BankTransfer, Check, Credit
        };
        
        public static readonly Dictionary<string, string> Descriptions = new()
        {
            { Cash, "Espèces" },
            { Card, "Carte bancaire" },
            { MobileMoney, "Mobile Money" },
            { BankTransfer, "Virement bancaire" },
            { Check, "Chèque" },
            { Credit, "Crédit" }
        };
    }

    /// <summary>
    /// Codes TVA supportés
    /// </summary>
    public static class CodesTvaSupportes
    {
        public const string Tva = "TVA";
        public const string TvaB = "TVAB";
        public const string TvaC = "TVAC";
        public const string TvaD = "TVAD";
        
        public static readonly List<string> Valides = new()
        {
            Tva, TvaB, TvaC, TvaD
        };
    }

    /// <summary>
    /// Informations de template d'un client depuis la base de données
    /// </summary>
    public class ClientTemplateInfo
    {
        public string ClientCode { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string NomCommercial { get; set; } = string.Empty;
        public string Ncc { get; set; } = string.Empty;
        public bool Active { get; set; }
        public bool Exists { get; set; } = false;
    }
}
