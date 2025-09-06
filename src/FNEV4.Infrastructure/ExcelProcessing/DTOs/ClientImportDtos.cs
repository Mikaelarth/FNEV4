using System.ComponentModel.DataAnnotations;

namespace FNEV4.Infrastructure.ExcelProcessing.DTOs
{
    /// <summary>
    /// Résultat de l'import Excel des clients
    /// </summary>
    public class ClientImportResultDto
    {
        public bool IsSuccess { get; set; }
        public int TotalRows { get; set; }
        public int SuccessfulImports { get; set; }
        public int ErrorCount { get; set; }
        public int DuplicateCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> GlobalErrors { get; set; } = new();
        public List<ClientImportErrorDto> RowErrors { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
        public DateTime ImportDate { get; set; } = DateTime.Now;
        public string ImportedBy { get; set; } = string.Empty;

        public string GetSummary()
        {
            return $"Import terminé: {SuccessfulImports}/{TotalRows} clients importés avec succès. " +
                   $"Erreurs: {ErrorCount}, Doublons: {DuplicateCount}, Ignorés: {SkippedCount}";
        }
    }

    /// <summary>
    /// Détails d'erreur pour une ligne spécifique
    /// </summary>
    public class ClientImportErrorDto
    {
        public int RowNumber { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public string ErrorType { get; set; } = string.Empty; // Validation, Duplicate, Database, etc.

        public string GetErrorSummary()
        {
            return $"Ligne {RowNumber} - {ClientCode}: {string.Join(", ", Errors)}";
        }
    }

    /// <summary>
    /// Options de configuration pour l'import
    /// </summary>
    public class ClientImportOptionsDto
    {
        /// <summary>
        /// Ignorer les doublons (ne pas importer si le code client existe)
        /// </summary>
        public bool IgnoreDuplicates { get; set; } = true;

        /// <summary>
        /// Mettre à jour les clients existants
        /// </summary>
        public bool UpdateExisting { get; set; } = false;

        /// <summary>
        /// Valider uniquement sans importer
        /// </summary>
        public bool ValidateOnly { get; set; } = false;

        /// <summary>
        /// Numéro de la première ligne de données (par défaut ligne 2, car ligne 1 = headers)
        /// </summary>
        public int StartRow { get; set; } = 2;

        /// <summary>
        /// Nombre maximum de lignes à traiter (0 = illimité)
        /// </summary>
        public int MaxRows { get; set; } = 0;

        /// <summary>
        /// Arrêter l'import après X erreurs
        /// </summary>
        public int MaxErrors { get; set; } = 100;

        /// <summary>
        /// Utilisateur effectuant l'import
        /// </summary>
        public string ImportedBy { get; set; } = "System";
    }

    /// <summary>
    /// Statistiques d'aperçu avant import
    /// </summary>
    public class ClientImportPreviewDto
    {
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
        public int DuplicateRows { get; set; }
        public List<string> DetectedColumns { get; set; } = new();
        public List<ClientImportErrorDto> SampleErrors { get; set; } = new();
        public Dictionary<string, int> ClientTypeDistribution { get; set; } = new();
        public Dictionary<string, int> CountryDistribution { get; set; } = new();
        public bool HasHeaders { get; set; } = true;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
    }
}
