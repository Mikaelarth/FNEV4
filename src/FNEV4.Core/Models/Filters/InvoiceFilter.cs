namespace FNEV4.Core.Models.Filters
{
    /// <summary>
    /// Filtres flexibles pour la recherche unifiée des factures FNE
    /// Utilisé par le nouveau menu unifié "Factures FNE"
    /// </summary>
    public class InvoiceFilter
    {
        /// <summary>
        /// Statuts à inclure (null = tous les statuts)
        /// Exemples: ["draft"], ["certified"], ["draft", "certified"]
        /// </summary>
        public string[]? Statuses { get; set; }

        /// <summary>
        /// Date de début (inclusive)
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Date de fin (inclusive)
        /// </summary>
        public DateTime? DateTo { get; set; }

        /// <summary>
        /// Recherche textuelle (numéro de facture, nom client, etc.)
        /// </summary>
        public string? SearchText { get; set; }

        /// <summary>
        /// Filtrer par code client
        /// </summary>
        public string? ClientCode { get; set; }

        /// <summary>
        /// Types de factures à inclure (null = tous types)
        /// Exemples: ["sale"], ["refund"], ["sale", "refund"]
        /// </summary>
        public string[]? InvoiceTypes { get; set; }

        /// <summary>
        /// Inclure les factures certifiées (par défaut true)
        /// </summary>
        public bool IncludeCertified { get; set; } = true;

        /// <summary>
        /// Inclure les factures en brouillon (par défaut true)
        /// </summary>
        public bool IncludeDraft { get; set; } = true;

        /// <summary>
        /// Inclure les factures avec erreur (par défaut true)
        /// </summary>
        public bool IncludeErrors { get; set; } = true;

        /// <summary>
        /// Inclure toutes les factures quel que soit leur statut
        /// </summary>
        public bool IncludeAllStatuses { get; set; } = true;

        /// <summary>
        /// Date de début (alias pour DateFrom pour compatibilité ViewModels)
        /// </summary>
        public DateTime? StartDate 
        { 
            get => DateFrom; 
            set => DateFrom = value; 
        }

        /// <summary>
        /// Date de fin (alias pour DateTo pour compatibilité ViewModels)
        /// </summary>
        public DateTime? EndDate 
        { 
            get => DateTo; 
            set => DateTo = value; 
        }

        /// <summary>
        /// Montant minimum
        /// </summary>
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Montant maximum
        /// </summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>
        /// Trier par (InvoiceDate, CreatedAt, Amount, etc.)
        /// </summary>
        public string SortBy { get; set; } = "InvoiceDate";

        /// <summary>
        /// Ordre de tri (true = décroissant, false = croissant)
        /// </summary>
        public bool SortDescending { get; set; } = true;

        /// <summary>
        /// Nombre maximum de résultats (null = pas de limite)
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// Créer un filtre pour toutes les factures
        /// </summary>
        public static InvoiceFilter All() => new InvoiceFilter();

        /// <summary>
        /// Créer un filtre pour les factures à certifier seulement
        /// </summary>
        public static InvoiceFilter ForCertification() => new InvoiceFilter
        {
            Statuses = new[] { "draft" },
            IncludeCertified = false,
            IncludeErrors = false
        };

        /// <summary>
        /// Créer un filtre pour les factures certifiées seulement
        /// </summary>
        public static InvoiceFilter CertifiedOnly() => new InvoiceFilter
        {
            Statuses = new[] { "certified" },
            IncludeDraft = false,
            IncludeErrors = false
        };

        /// <summary>
        /// Créer un filtre pour les factures avec erreur seulement
        /// </summary>
        public static InvoiceFilter ErrorsOnly() => new InvoiceFilter
        {
            Statuses = new[] { "error" },
            IncludeDraft = false,
            IncludeCertified = false
        };
    }
}