using Microsoft.EntityFrameworkCore;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using FNEV4.Infrastructure.Data;

namespace FNEV4.Infrastructure.Repositories
{
    /// <summary>
    /// Implémentation du repository pour la gestion des clients
    /// </summary>
    public class ClientRepository : IClientRepository
    {
        private readonly FNEV4DbContext _context;

        public ClientRepository(FNEV4DbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Client> Clients, int TotalCount)> GetAllAsync(
            int pageNumber = 1, 
            int pageSize = 50, 
            string? searchTerm = null, 
            string? clientType = null, 
            bool? isActive = null)
        {
            var query = _context.Clients.AsQueryable();

            // Filtrage par terme de recherche
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(c => 
                    c.Name.ToLower().Contains(lowerSearchTerm) ||
                    c.ClientCode.ToLower().Contains(lowerSearchTerm) ||
                    (c.ClientNcc != null && c.ClientNcc.ToLower().Contains(lowerSearchTerm)) ||
                    (c.CompanyName != null && c.CompanyName.ToLower().Contains(lowerSearchTerm)) ||
                    (c.Email != null && c.Email.ToLower().Contains(lowerSearchTerm)));
            }

            // Filtrage par type de client
            if (!string.IsNullOrWhiteSpace(clientType))
            {
                query = query.Where(c => c.ClientType == clientType);
            }

            // Filtrage par statut actif/inactif
            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            // Compter le total avant pagination
            var totalCount = await query.CountAsync();

            // Pagination et tri
            var clients = await query
                .OrderBy(c => c.Name)
                .ThenBy(c => c.ClientCode)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (clients, totalCount);
        }

        public async Task<Client?> GetByIdAsync(Guid id)
        {
            return await _context.Clients
                .Include(c => c.Invoices)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Client?> GetByClientCodeAsync(string clientCode)
        {
            return await _context.Clients
                .FirstOrDefaultAsync(c => c.ClientCode == clientCode);
        }

        public async Task<Client?> GetByNccAsync(string ncc)
        {
            return await _context.Clients
                .FirstOrDefaultAsync(c => c.ClientNcc == ncc);
        }

        public async Task<IEnumerable<Client>> SearchAsync(ClientSearchCriteria searchCriteria)
        {
            var query = _context.Clients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchCriteria.Name))
            {
                query = query.Where(c => c.Name.Contains(searchCriteria.Name));
            }

            if (!string.IsNullOrWhiteSpace(searchCriteria.ClientCode))
            {
                query = query.Where(c => c.ClientCode.Contains(searchCriteria.ClientCode));
            }

            if (!string.IsNullOrWhiteSpace(searchCriteria.Ncc))
            {
                query = query.Where(c => c.ClientNcc != null && c.ClientNcc.Contains(searchCriteria.Ncc));
            }

            if (!string.IsNullOrWhiteSpace(searchCriteria.Email))
            {
                query = query.Where(c => c.Email != null && c.Email.Contains(searchCriteria.Email));
            }

            if (!string.IsNullOrWhiteSpace(searchCriteria.Phone))
            {
                query = query.Where(c => c.Phone != null && c.Phone.Contains(searchCriteria.Phone));
            }

            if (!string.IsNullOrWhiteSpace(searchCriteria.ClientType))
            {
                query = query.Where(c => c.ClientType == searchCriteria.ClientType);
            }

            if (searchCriteria.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == searchCriteria.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchCriteria.Country))
            {
                query = query.Where(c => c.Country == searchCriteria.Country);
            }

            if (searchCriteria.CreatedAfter.HasValue)
            {
                query = query.Where(c => c.CreatedDate >= searchCriteria.CreatedAfter.Value);
            }

            if (searchCriteria.CreatedBefore.HasValue)
            {
                query = query.Where(c => c.CreatedDate <= searchCriteria.CreatedBefore.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchCriteria.SellerName))
            {
                query = query.Where(c => c.SellerName != null && c.SellerName.Contains(searchCriteria.SellerName));
            }

            return await query
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Client> CreateAsync(Client client)
        {
            client.Id = Guid.NewGuid();
            client.CreatedDate = DateTime.UtcNow;
            
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            
            return client;
        }

        public async Task<Client> UpdateAsync(Client client)
        {
            client.LastModifiedDate = DateTime.UtcNow;
            
            _context.Clients.Update(client);
            await _context.SaveChangesAsync();
            
            return client;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return false;

            // Soft delete : marquer comme inactif
            client.IsActive = false;
            client.LastModifiedDate = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsClientCodeAsync(string clientCode, Guid? excludeId = null)
        {
            var query = _context.Clients.Where(c => c.ClientCode == clientCode);
            
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> ExistsNccAsync(string ncc, Guid? excludeId = null)
        {
            var query = _context.Clients.Where(c => c.ClientNcc == ncc);
            
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<ClientStatistics> GetStatisticsAsync()
        {
            var totalClients = await _context.Clients.CountAsync();
            var activeClients = await _context.Clients.CountAsync(c => c.IsActive);
            var inactiveClients = totalClients - activeClients;
            
            var individualClients = await _context.Clients.CountAsync(c => c.ClientType == "Individual");
            var companyClients = await _context.Clients.CountAsync(c => c.ClientType == "Company");
            var governmentClients = await _context.Clients.CountAsync(c => c.ClientType == "Government");
            var internationalClients = await _context.Clients.CountAsync(c => c.ClientType == "International");
            
            var clientsWithNcc = await _context.Clients.CountAsync(c => c.ClientNcc != null && c.ClientNcc != "");
            var clientsWithoutNcc = totalClients - clientsWithNcc;
            
            var lastClientCreated = await _context.Clients
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => c.CreatedDate)
                .FirstOrDefaultAsync();

            return new ClientStatistics
            {
                TotalClients = totalClients,
                ActiveClients = activeClients,
                InactiveClients = inactiveClients,
                IndividualClients = individualClients,
                CompanyClients = companyClients,
                GovernmentClients = governmentClients,
                InternationalClients = internationalClients,
                ClientsWithNcc = clientsWithNcc,
                ClientsWithoutNcc = clientsWithoutNcc,
                LastClientCreated = lastClientCreated
            };
        }

        public async Task<IEnumerable<FneInvoice>> GetClientInvoicesAsync(Guid clientId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.FneInvoices
                .Where(i => i.ClientId == clientId);

            if (fromDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate <= toDate.Value);
            }

            return await query
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<ClientImportResult> ImportBatchAsync(IEnumerable<Client> clients)
        {
            var result = new ClientImportResult
            {
                TotalProcessed = clients.Count()
            };

            foreach (var client in clients)
            {
                try
                {
                    // Vérifier les doublons
                    if (await ExistsClientCodeAsync(client.ClientCode))
                    {
                        result.Duplicates++;
                        result.Errors.Add($"Client avec code {client.ClientCode} existe déjà");
                        continue;
                    }

                    if (!string.IsNullOrEmpty(client.ClientNcc) && await ExistsNccAsync(client.ClientNcc))
                    {
                        result.Duplicates++;
                        result.Errors.Add($"Client avec NCC {client.ClientNcc} existe déjà");
                        continue;
                    }

                    // Créer le client
                    var createdClient = await CreateAsync(client);
                    result.ImportedClients.Add(createdClient);
                    result.SuccessfulImports++;
                }
                catch (Exception ex)
                {
                    result.FailedImports++;
                    result.Errors.Add($"Erreur lors de l'import du client {client.ClientCode}: {ex.Message}");
                }
            }

            return result;
        }
    }
}
