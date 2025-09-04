using System.ComponentModel.DataAnnotations;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Sessions d'import de fichiers Excel
    /// Historique complet des traitements conformément aux spécifications
    /// </summary>
    public class ImportSession : BaseEntity
    {
        /// <summary>
        /// Nom du fichier Excel importé
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Chemin complet du fichier
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Date et heure de début d'import
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Date et heure de fin d'import
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Statut de l'import (Pending, Processing, Completed, Failed)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Nombre total de factures trouvées dans le fichier
        /// </summary>
        public int TotalInvoicesFound { get; set; }

        /// <summary>
        /// Nombre de factures importées avec succès
        /// </summary>
        public int InvoicesImported { get; set; }

        /// <summary>
        /// Nombre d'erreurs rencontrées
        /// </summary>
        public int ErrorsCount { get; set; }

        /// <summary>
        /// Messages d'erreur détaillés (JSON)
        /// </summary>
        public string? ErrorMessages { get; set; }

        /// <summary>
        /// Taille du fichier en octets
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Nom de l'utilisateur ayant lancé l'import
        /// </summary>
        [MaxLength(100)]
        public string? UserName { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Navigation - Factures importées lors de cette session
        /// </summary>
        public virtual ICollection<FneInvoice> Invoices { get; set; } = new List<FneInvoice>();

        #endregion
    }
}
