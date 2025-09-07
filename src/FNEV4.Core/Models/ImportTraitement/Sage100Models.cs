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
    }

    /// <summary>
    /// Données d'une facture Sage 100 v15
    /// </summary>
    public class Sage100FactureData
    {
        // En-tête facture
        public string NumeroFacture { get; set; } = string.Empty;
        public string CodeClient { get; set; } = string.Empty;
        public string NccClient { get; set; } = string.Empty;
        public DateTime DateFacture { get; set; }
        public string PointDeVente { get; set; } = string.Empty;
        public string IntituleClient { get; set; } = string.Empty;
        
        // Spécifique clients divers (code 1999)
        public string NomReelClientDivers { get; set; } = string.Empty;
        public string NccClientDivers { get; set; } = string.Empty;
        
        // Autres infos
        public string NumeroFactureAvoir { get; set; } = string.Empty;
        
        /// <summary>
        /// Moyen de paiement de la facture (A18 Excel)
        /// Si vide, utilise le moyen de paiement par défaut du client
        /// Pour clients divers (1999), utilise "cash" par défaut
        /// </summary>
        public string MoyenPaiement { get; set; } = string.Empty;
        
        // Produits
        public List<Sage100ProduitData> Produits { get; set; } = new();
        
        // Métadonnées
        public string NomFeuille { get; set; } = string.Empty;
        public bool EstClientDivers => CodeClient == "1999";
    }

    /// <summary>
    /// Données d'un produit Sage 100 v15
    /// </summary>
    public class Sage100ProduitData
    {
        public string CodeProduit { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public decimal PrixUnitaire { get; set; }
        public decimal Quantite { get; set; }
        public string Emballage { get; set; } = string.Empty;
        public string CodeTva { get; set; } = string.Empty;
        public decimal MontantHt { get; set; }
        public int NumeroLigne { get; set; }
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
        public string NomFeuille { get; set; } = string.Empty;
        public string NumeroFacture { get; set; } = string.Empty;
        public string CodeClient { get; set; } = string.Empty;
        public string NomClient { get; set; } = string.Empty;
        public DateTime DateFacture { get; set; }
        public string MoyenPaiement { get; set; } = string.Empty;
        public int NombreProduits { get; set; }
        public decimal MontantEstime { get; set; }
        public bool EstValide { get; set; }
        public bool ClientTrouve { get; set; }
        public List<string> Erreurs { get; set; } = new();
        
        // Nouvelles propriétés pour certification FNE
        public string TypeFacture { get; set; } = "B2B"; // B2B, B2C, B2G, B2F
        public string TypeTva { get; set; } = "TVA"; // TVA, TVAB, TVAC, TVAD
        public string ConflitDonnees { get; set; } = string.Empty;
        public string ConflitDetails { get; set; } = string.Empty;
        
        // Propriétés additionnelles pour comparaison source de vérité
        public string NomClientExcel { get; set; } = string.Empty; // Nom du client dans Excel
        public string NomClientBDD { get; set; } = string.Empty;   // Nom du client dans BDD
        public string TelephoneClientExcel { get; set; } = string.Empty;
        public string TelephoneClientBDD { get; set; } = string.Empty;
        public string EmailClientExcel { get; set; } = string.Empty;
        public string EmailClientBDD { get; set; } = string.Empty;
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
}
