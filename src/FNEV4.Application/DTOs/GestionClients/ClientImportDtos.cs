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
        public List<ClientPreviewDto> PreviewClients { get; set; } = new();
        public string FileName { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Client pour l'aperçu avant import
    /// </summary>
    public class ClientPreviewDto
    {
        public int RowNumber { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Ncc { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Representative { get; set; }
        public string? Currency { get; set; }
        public string? PaymentMethod { get; set; } // Nouveau champ pour moyen de paiement
        public string? Active { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "Nouveau"; // Nouveau, Doublon, Erreur, À mettre à jour
        public List<string> ValidationIssues { get; set; } = new();
        public bool IsValid => ValidationIssues.Count == 0;
        public string StatusColor => Status switch
        {
            "Nouveau" => "#4CAF50",
            "À mettre à jour" => "#2196F3", 
            "Doublon" => "#FF9800",
            "Erreur" => "#F44336",
            _ => "#9E9E9E"
        };
        
        // Propriétés calculées pour l'affichage
        public string DisplayAddress => string.IsNullOrWhiteSpace(Address) ? "" : 
            $"{Address}{(string.IsNullOrWhiteSpace(City) ? "" : $", {City}")}{(string.IsNullOrWhiteSpace(PostalCode) ? "" : $" {PostalCode}")}";
            
        public string ProblemsText => ValidationIssues.Count == 0 ? "" : string.Join("; ", ValidationIssues);
    }
}
