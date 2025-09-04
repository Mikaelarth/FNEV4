using System.ComponentModel.DataAnnotations;

namespace FNEV4.Core.Entities
{
    /// <summary>
    /// Classe de base pour toutes les entités
    /// Conforme aux spécifications ARCHITECTURE.md (GUID + audit trail)
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Identifiant unique (GUID pour éviter conflits)
        /// </summary>
        [Key]
        public virtual Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Date de création automatique
        /// </summary>
        public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Soft delete (marquage au lieu de suppression)
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
