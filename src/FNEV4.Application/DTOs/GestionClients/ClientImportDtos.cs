namespace FNEV4.Application.DTOs.GestionClients
{
    /// <summary>
    /// Résultat de l'import Excel des clients
    /// </summary>
    public class ClientImportResultDto
    {
        public bool IsSuccess { get; set; }
        public int ProcessedCount { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int SkippedCount { get; set; }
        public int DuplicateCount { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.Now;
        public string ImportedBy { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public List<ClientImportErrorDto> RowErrors { get; set; } = new();
        public List<string> GeneralErrors { get; set; } = new();
        public string Summary { get; set; } = string.Empty;

        public string GetSummary()
        {
            return $"{SuccessCount} créés, {ErrorCount} erreurs, {SkippedCount} ignorés";
        }
    }

    /// <summary>
    /// Erreur d'import pour une ligne spécifique
    /// </summary>
    public class ClientImportErrorDto
    {
        public int RowNumber { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string RowData { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Options d'import des clients
    /// </summary>
    public class ClientImportOptionsDto
    {
        public bool IgnoreDuplicates { get; set; } = true;
        public bool UpdateExisting { get; set; } = false;
        public bool ValidateOnly { get; set; } = false;
        public int MaxErrors { get; set; } = 100;
        public bool SkipEmptyRows { get; set; } = true;
        public bool CreateMissingCategories { get; set; } = true;
        public string ImportedBy { get; set; } = Environment.UserName;
        public DateTime ImportDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Aperçu avant import
    /// </summary>
    public class ClientImportPreviewDto
    {
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int ErrorRows { get; set; }
        public int DuplicateRows { get; set; }
        public int EmptyRows { get; set; }
        public List<ClientImportErrorDto> SampleErrors { get; set; } = new();
        public List<string> DetectedColumns { get; set; } = new();
        public string FileName { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; } = DateTime.Now;
    }
}
