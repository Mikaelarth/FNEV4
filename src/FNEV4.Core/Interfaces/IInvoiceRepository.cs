using FNEV4.Core.Entities;

namespace FNEV4.Core.Interfaces
{
    /// <summary>
    /// Interface pour la gestion des opérations CRUD sur les factures FNE
    /// Utilise le système centralisé avec les entités FneInvoice existantes
    /// </summary>
    public interface IInvoiceRepository
    {
        /// <summary>
        /// Récupère toutes les factures FNE
        /// </summary>
        /// <returns>Liste des factures FNE</returns>
        Task<IEnumerable<FneInvoice>> GetAllAsync();
        
        /// <summary>
        /// Récupère une facture FNE par son ID
        /// </summary>
        /// <param name="id">ID de la facture</param>
        /// <returns>Facture correspondante ou null</returns>
        Task<FneInvoice?> GetByIdAsync(string id);
        
        /// <summary>
        /// Récupère les factures FNE avec filtres
        /// </summary>
        /// <param name="searchText">Texte de recherche</param>
        /// <param name="status">Statut filtré</param>
        /// <param name="dateDebut">Date de début</param>
        /// <param name="dateFin">Date de fin</param>
        /// <returns>Liste des factures filtrées</returns>
        Task<IEnumerable<FneInvoice>> GetFilteredAsync(
            string? searchText = null,
            string? status = null,
            DateTime? dateDebut = null,
            DateTime? dateFin = null);
        
        /// <summary>
        /// Ajoute une nouvelle facture FNE
        /// </summary>
        /// <param name="invoice">Facture à ajouter</param>
        /// <returns>Facture ajoutée avec ID</returns>
        Task<FneInvoice> AddAsync(FneInvoice invoice);
        
        /// <summary>
        /// Met à jour une facture FNE existante
        /// </summary>
        /// <param name="invoice">Facture à mettre à jour</param>
        /// <returns>Facture mise à jour</returns>
        Task<FneInvoice> UpdateAsync(FneInvoice invoice);
        
        /// <summary>
        /// Supprime une facture FNE
        /// </summary>
        /// <param name="id">ID de la facture à supprimer</param>
        /// <returns>True si supprimée avec succès</returns>
        Task<bool> DeleteAsync(string id);
        
        /// <summary>
        /// Obtient les statistiques des factures FNE
        /// </summary>
        /// <returns>Statistiques des factures</returns>
        Task<InvoiceStatistics> GetStatisticsAsync();
    }
    
    /// <summary>
    /// Classe pour les statistiques des factures FNE
    /// </summary>
    public class InvoiceStatistics
    {
        public int TotalInvoices { get; set; }
        public int CertifiedInvoices { get; set; }
        public int PendingInvoices { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }
}