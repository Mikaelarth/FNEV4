using FNEV4.Core.Entities;

namespace FNEV4.Core.Interfaces
{
    /// <summary>
    /// Interface pour la gestion des clients
    /// Référentiel clients avec NCC et classification DGI
    /// </summary>
    public interface IClientRepository
    {
        /// <summary>
        /// Obtenir tous les clients avec pagination
        /// </summary>
        /// <param name="pageNumber">Numéro de page (1-based)</param>
        /// <param name="pageSize">Taille de la page</param>
        /// <param name="searchTerm">Terme de recherche optionnel</param>
        /// <param name="clientType">Filtrer par type de client</param>
        /// <param name="isActive">Filtrer par statut actif/inactif</param>
        /// <returns>Liste paginée des clients</returns>
        Task<(IEnumerable<Client> Clients, int TotalCount)> GetAllAsync(
            int pageNumber = 1, 
            int pageSize = 50,
            string? searchTerm = null,
            string? clientType = null,
            bool? isActive = null);

        /// <summary>
        /// Obtenir un client par son ID
        /// </summary>
        /// <param name="id">ID du client</param>
        /// <returns>Client ou null si non trouvé</returns>
        Task<Client?> GetByIdAsync(Guid id);

        /// <summary>
        /// Obtenir un client par son code Sage
        /// </summary>
        /// <param name="clientCode">Code client Sage</param>
        /// <returns>Client ou null si non trouvé</returns>
        Task<Client?> GetByClientCodeAsync(string clientCode);

        /// <summary>
        /// Obtenir un client par son NCC
        /// </summary>
        /// <param name="ncc">Numéro de Compte Contribuable</param>
        /// <returns>Client ou null si non trouvé</returns>
        Task<Client?> GetByNccAsync(string ncc);

        /// <summary>
        /// Recherche avancée de clients
        /// </summary>
        /// <param name="searchCriteria">Critères de recherche</param>
        /// <returns>Liste des clients correspondants</returns>
        Task<IEnumerable<Client>> SearchAsync(ClientSearchCriteria searchCriteria);

        /// <summary>
        /// Créer un nouveau client
        /// </summary>
        /// <param name="client">Données du client</param>
        /// <returns>Client créé</returns>
        Task<Client> CreateAsync(Client client);

        /// <summary>
        /// Mettre à jour un client existant
        /// </summary>
        /// <param name="client">Données du client</param>
        /// <returns>Client mis à jour</returns>
        Task<Client> UpdateAsync(Client client);

        /// <summary>
        /// Supprimer un client (soft delete)
        /// </summary>
        /// <param name="id">ID du client</param>
        /// <returns>True si supprimé avec succès</returns>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Vérifier si un code client existe déjà
        /// </summary>
        /// <param name="clientCode">Code client à vérifier</param>
        /// <param name="excludeId">ID à exclure de la vérification (pour mise à jour)</param>
        /// <returns>True si le code existe</returns>
        Task<bool> ExistsClientCodeAsync(string clientCode, Guid? excludeId = null);

        /// <summary>
        /// Vérifier si un NCC existe déjà
        /// </summary>
        /// <param name="ncc">NCC à vérifier</param>
        /// <param name="excludeId">ID à exclure de la vérification (pour mise à jour)</param>
        /// <returns>True si le NCC existe</returns>
        Task<bool> ExistsNccAsync(string ncc, Guid? excludeId = null);

        /// <summary>
        /// Obtenir les statistiques des clients
        /// </summary>
        /// <returns>Statistiques des clients</returns>
        Task<ClientStatistics> GetStatisticsAsync();

        /// <summary>
        /// Obtenir l'historique des factures d'un client
        /// </summary>
        /// <param name="clientId">ID du client</param>
        /// <param name="fromDate">Date de début (optionnelle)</param>
        /// <param name="toDate">Date de fin (optionnelle)</param>
        /// <returns>Liste des factures du client</returns>
        Task<IEnumerable<FneInvoice>> GetClientInvoicesAsync(Guid clientId, DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Import en lot de clients depuis un fichier
        /// </summary>
        /// <param name="clients">Liste des clients à importer</param>
        /// <returns>Résultat de l'import</returns>
        Task<ClientImportResult> ImportBatchAsync(IEnumerable<Client> clients);
    }

    /// <summary>
    /// Critères de recherche avancée pour les clients
    /// </summary>
    public class ClientSearchCriteria
    {
        public string? Name { get; set; }
        public string? ClientCode { get; set; }
        public string? Ncc { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? ClientType { get; set; }
        public bool? IsActive { get; set; }
        public string? Country { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public string? SellerName { get; set; }
    }

    /// <summary>
    /// Statistiques des clients
    /// </summary>
    public class ClientStatistics
    {
        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public int InactiveClients { get; set; }
        public int IndividualClients { get; set; }
        public int CompanyClients { get; set; }
        public int GovernmentClients { get; set; }
        public int InternationalClients { get; set; }
        public int ClientsWithNcc { get; set; }
        public int ClientsWithoutNcc { get; set; }
        public DateTime? LastClientCreated { get; set; }
    }

    /// <summary>
    /// Résultat d'import de clients
    /// </summary>
    public class ClientImportResult
    {
        public int TotalProcessed { get; set; }
        public int SuccessfulImports { get; set; }
        public int FailedImports { get; set; }
        public int Duplicates { get; set; }
        public IList<string> Errors { get; set; } = new List<string>();
        public IList<Client> ImportedClients { get; set; } = new List<Client>();
    }
}
