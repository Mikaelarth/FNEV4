using System;
using System.Collections.Generic;

namespace FNEV4.Core.Models.ImportTraitement
{
    /// <summary>
    /// Rapport détaillé d'un import avec informations sur les échecs
    /// </summary>
    public class ImportDetailedReport
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime DateImport { get; set; } = DateTime.Now;
        public TimeSpan DureeTraitement { get; set; }
        
        // Statistiques globales
        public int FacturesImportees { get; set; }
        public int FacturesEchouees { get; set; }
        public int TotalFactures => FacturesImportees + FacturesEchouees;
        
        // Détails des échecs
        public List<Sage100FactureImportee> FailedInvoices { get; set; } = new();
        public List<string> GlobalErrors { get; set; } = new();
        public List<string> GlobalWarnings { get; set; } = new();
        
        // Informations sur les fichiers traités
        public List<string> FichiersTraites { get; set; } = new();
        public string FichierSource { get; set; } = string.Empty;
        
        // Statistiques détaillées
        public decimal TauxReussite => TotalFactures > 0 ? (decimal)FacturesImportees / TotalFactures * 100 : 0;
        public decimal TauxEchec => TotalFactures > 0 ? (decimal)FacturesEchouees / TotalFactures * 100 : 0;
        
        /// <summary>
        /// Ajoute une facture échouée au rapport
        /// </summary>
        public void AddFailedInvoice(Sage100FactureImportee factureEchouee)
        {
            FailedInvoices.Add(factureEchouee);
            FacturesEchouees = FailedInvoices.Count;
        }
        
        /// <summary>
        /// Ajoute une erreur globale
        /// </summary>
        public void AddGlobalError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                GlobalErrors.Add(error);
            }
        }
        
        /// <summary>
        /// Ajoute un avertissement global
        /// </summary>
        public void AddGlobalWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                GlobalWarnings.Add(warning);
            }
        }
        
        /// <summary>
        /// Indique si le rapport contient des informations détaillées à afficher
        /// </summary>
        public bool HasDetailedInfo => FailedInvoices.Count > 0 || GlobalErrors.Count > 0 || GlobalWarnings.Count > 0;
        
        /// <summary>
        /// Crée un rapport à partir d'un résultat d'import Sage100
        /// </summary>
        public static ImportDetailedReport FromSage100Result(Sage100ImportResult result, string fichierSource = "")
        {
            var report = new ImportDetailedReport
            {
                IsSuccess = result.IsSuccess,
                Message = result.Message,
                DateImport = DateTime.Now,
                DureeTraitement = result.DureeTraitement,
                FacturesImportees = result.FacturesImportees,
                FacturesEchouees = result.FacturesEchouees,
                FichierSource = fichierSource
            };
            
            // Ajouter les erreurs globales
            foreach (var error in result.Errors)
            {
                report.AddGlobalError(error);
            }
            
            // Ajouter les avertissements
            foreach (var warning in result.Warnings)
            {
                report.AddGlobalWarning(warning);
            }
            
            // Ajouter les factures détaillées (uniquement celles qui ont échoué)
            foreach (var facture in result.FacturesDetaillees)
            {
                if (!facture.EstImportee)
                {
                    report.AddFailedInvoice(facture);
                }
            }
            
            return report;
        }
        
        /// <summary>
        /// Obtient un résumé textuel du rapport
        /// </summary>
        public string GetSummary()
        {
            var summary = $"Import {(IsSuccess ? "réussi" : "échoué")} - ";
            summary += $"{FacturesImportees} importées, {FacturesEchouees} échouées";
            
            if (DureeTraitement.TotalSeconds > 0)
            {
                summary += $" (durée: {DureeTraitement.TotalSeconds:F1}s)";
            }
            
            return summary;
        }
    }
}