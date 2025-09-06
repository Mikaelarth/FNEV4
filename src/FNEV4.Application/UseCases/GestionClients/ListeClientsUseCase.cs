using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;

namespace FNEV4.Application.UseCases.GestionClients
{
    /// <summary>
    /// Use case pour la gestion de la liste des clients
    /// Module: Gestion Clients > Liste des clients
    /// </summary>
    public class ListeClientsUseCase
    {
        private readonly IClientRepository _clientRepository;

        public ListeClientsUseCase(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        /// <summary>
        /// Obtenir la liste paginée des clients avec filtres
        /// </summary>
        /// <param name="request">Paramètres de la requête</param>
        /// <returns>Résultat avec liste paginée et métadonnées</returns>
        public async Task<ListeClientsResponse> ExecuteAsync(ListeClientsRequest request)
        {
            try
            {
                // Obtenir les clients avec pagination et filtres
                var (clients, totalCount) = await _clientRepository.GetAllAsync(
                    pageNumber: request.PageNumber,
                    pageSize: request.PageSize,
                    searchTerm: request.SearchTerm,
                    clientType: request.ClientType,
                    isActive: request.IsActive
                );

                // Calculer les métadonnées de pagination
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
                var hasNextPage = request.PageNumber < totalPages;
                var hasPreviousPage = request.PageNumber > 1;

                return new ListeClientsResponse
                {
                    Success = true,
                    Clients = clients.ToList(),
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = hasNextPage,
                    HasPreviousPage = hasPreviousPage
                };
            }
            catch (Exception ex)
            {
                return new ListeClientsResponse
                {
                    Success = false,
                    ErrorMessage = $"Erreur lors de la récupération des clients: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Obtenir les statistiques des clients pour le tableau de bord
        /// </summary>
        /// <returns>Statistiques détaillées</returns>
        public async Task<ClientStatisticsResponse> GetStatisticsAsync()
        {
            try
            {
                var statistics = await _clientRepository.GetStatisticsAsync();

                return new ClientStatisticsResponse
                {
                    Success = true,
                    Statistics = statistics
                };
            }
            catch (Exception ex)
            {
                return new ClientStatisticsResponse
                {
                    Success = false,
                    ErrorMessage = $"Erreur lors de la récupération des statistiques: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Recherche rapide de clients par terme
        /// </summary>
        /// <param name="searchTerm">Terme de recherche</param>
        /// <param name="maxResults">Nombre maximum de résultats</param>
        /// <returns>Liste des clients correspondants</returns>
        public async Task<QuickSearchResponse> QuickSearchAsync(string searchTerm, int maxResults = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return new QuickSearchResponse
                    {
                        Success = true,
                        Clients = new List<Client>()
                    };
                }

                var (clients, _) = await _clientRepository.GetAllAsync(
                    pageNumber: 1,
                    pageSize: maxResults,
                    searchTerm: searchTerm
                );

                return new QuickSearchResponse
                {
                    Success = true,
                    Clients = clients.ToList()
                };
            }
            catch (Exception ex)
            {
                return new QuickSearchResponse
                {
                    Success = false,
                    ErrorMessage = $"Erreur lors de la recherche: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Supprimer un client (soft delete)
        /// </summary>
        /// <param name="clientId">ID du client à supprimer</param>
        /// <returns>Résultat de la suppression</returns>
        public async Task<DeleteClientResponse> DeleteClientAsync(Guid clientId)
        {
            try
            {
                // Vérifier si le client existe
                var client = await _clientRepository.GetByIdAsync(clientId);
                if (client == null)
                {
                    return new DeleteClientResponse
                    {
                        Success = false,
                        ErrorMessage = "Client non trouvé"
                    };
                }

                // Vérifier s'il y a des factures associées
                var invoices = await _clientRepository.GetClientInvoicesAsync(clientId);
                if (invoices.Any())
                {
                    return new DeleteClientResponse
                    {
                        Success = false,
                        ErrorMessage = $"Impossible de supprimer le client {client.Name}. Il a {invoices.Count()} facture(s) associée(s)."
                    };
                }

                // Effectuer la suppression (soft delete)
                var deleted = await _clientRepository.DeleteAsync(clientId);
                
                return new DeleteClientResponse
                {
                    Success = deleted,
                    ErrorMessage = deleted ? null : "Erreur lors de la suppression"
                };
            }
            catch (Exception ex)
            {
                return new DeleteClientResponse
                {
                    Success = false,
                    ErrorMessage = $"Erreur lors de la suppression: {ex.Message}"
                };
            }
        }
    }

    #region DTOs et Responses

    /// <summary>
    /// Paramètres de requête pour la liste des clients
    /// </summary>
    public class ListeClientsRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SearchTerm { get; set; }
        public string? ClientType { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Réponse pour la liste des clients
    /// </summary>
    public class ListeClientsResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<Client> Clients { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// Réponse pour les statistiques des clients
    /// </summary>
    public class ClientStatisticsResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public ClientStatistics? Statistics { get; set; }
    }

    /// <summary>
    /// Réponse pour la recherche rapide
    /// </summary>
    public class QuickSearchResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<Client> Clients { get; set; } = new();
    }

    /// <summary>
    /// Réponse pour la suppression de client
    /// </summary>
    public class DeleteClientResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion
}
