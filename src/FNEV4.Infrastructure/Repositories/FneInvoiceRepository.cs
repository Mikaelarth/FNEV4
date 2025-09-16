using Microsoft.EntityFrameworkCore;
using FNEV4.Core.Entities;
using FNEV4.Core.Interfaces;
using FNEV4.Infrastructure.Data;

namespace FNEV4.Infrastructure.Repositories
{
    /// <summary>
    /// Repository pour la gestion des factures FNE
    /// Utilise le système centralisé avec les entités FneInvoice existantes
    /// </summary>
    public class FneInvoiceRepository : IInvoiceRepository
    {
        private readonly FNEV4DbContext _context;

        public FneInvoiceRepository(FNEV4DbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FneInvoice>> GetAllAsync()
        {
            return await _context.FneInvoices
                .Include(f => f.Items)
                .Include(f => f.Client)
                .OrderByDescending(f => f.InvoiceDate)
                .ToListAsync();
        }

        public async Task<FneInvoice?> GetByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return null;
                
            return await _context.FneInvoices
                .Include(f => f.Items)
                .Include(f => f.Client)
                .FirstOrDefaultAsync(f => f.Id == guidId);
        }

        public async Task<FneInvoice?> GetByIdWithDetailsAsync(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return null;
                
            var invoice = await _context.FneInvoices
                .Include(f => f.Items)
                .Include(f => f.Client)
                .AsSplitQuery() // Pour optimiser les requêtes multiples
                .FirstOrDefaultAsync(f => f.Id == guidId);
                
            return invoice;
        }

        public async Task<IEnumerable<FneInvoice>> GetFilteredAsync(
            string? searchText = null,
            string? status = null,
            DateTime? dateDebut = null,
            DateTime? dateFin = null)
        {
            var query = _context.FneInvoices
                .Include(f => f.Items)
                .Include(f => f.Client)
                .AsQueryable();

            // Filtrage par texte de recherche
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(f => 
                    f.InvoiceNumber.Contains(searchText) ||
                    f.ClientCode.Contains(searchText) ||
                    (f.Client != null && f.Client.CompanyName != null && f.Client.CompanyName.Contains(searchText)) ||
                    (f.Client != null && f.Client.ClientNcc != null && f.Client.ClientNcc.Contains(searchText)) ||
                    (f.Client != null && f.Client.Name != null && f.Client.Name.Contains(searchText)));
            }

            // Filtrage par statut
            if (!string.IsNullOrWhiteSpace(status) && status != "Tous")
            {
                query = query.Where(f => f.Status == status);
            }

            // Filtrage par date
            if (dateDebut.HasValue)
            {
                query = query.Where(f => f.InvoiceDate >= dateDebut.Value);
            }

            if (dateFin.HasValue)
            {
                query = query.Where(f => f.InvoiceDate <= dateFin.Value);
            }

            return await query
                .OrderByDescending(f => f.InvoiceDate)
                .ToListAsync();
        }

        public async Task<FneInvoice> AddAsync(FneInvoice invoice)
        {
            if (invoice.Id == Guid.Empty)
            {
                invoice.Id = Guid.NewGuid();
            }

            invoice.CreatedAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            _context.FneInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            return invoice;
        }

        public async Task<FneInvoice> UpdateAsync(FneInvoice invoice)
        {
            invoice.UpdatedAt = DateTime.UtcNow;

            _context.FneInvoices.Update(invoice);
            await _context.SaveChangesAsync();

            return invoice;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (!Guid.TryParse(id, out var guidId))
                return false;
                
            var invoice = await _context.FneInvoices.FindAsync(guidId);
            if (invoice == null)
                return false;

            _context.FneInvoices.Remove(invoice);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<InvoiceStatistics> GetStatisticsAsync()
        {
            var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var nextMonth = currentMonth.AddMonths(1);

            var totalInvoices = await _context.FneInvoices.CountAsync();
            var certifiedInvoices = await _context.FneInvoices
                .CountAsync(f => f.Status == "Certified" || f.Status == "Certifiée");
            var pendingInvoices = await _context.FneInvoices
                .CountAsync(f => f.Status == "Pending" || f.Status == "En attente");

            var monthlyRevenue = await _context.FneInvoices
                .Where(f => f.InvoiceDate >= currentMonth && f.InvoiceDate < nextMonth)
                .SumAsync(f => f.TotalAmountTTC);

            return new InvoiceStatistics
            {
                TotalInvoices = totalInvoices,
                CertifiedInvoices = certifiedInvoices,
                PendingInvoices = pendingInvoices,
                MonthlyRevenue = monthlyRevenue
            };
        }

        public async Task<IEnumerable<FneInvoice>> GetAvailableForCertificationAsync()
        {
            // Chargement des factures avec leurs clients via une jointure explicite
            var invoicesWithClients = await (from invoice in _context.FneInvoices
                                              join client in _context.Clients on invoice.ClientId equals client.Id into clientJoin
                                              from client in clientJoin.DefaultIfEmpty()
                                              where invoice.Status == "draft" && invoice.CertifiedAt == null
                                              select new { Invoice = invoice, Client = client })
                                              .OrderByDescending(x => x.Invoice.InvoiceDate)
                                              .ToListAsync();

            // Assigner manuellement les clients aux factures
            var result = invoicesWithClients.Select(x =>
            {
                if (x.Client != null)
                {
                    x.Invoice.Client = x.Client;
                }
                return x.Invoice;
            }).ToList();

            return result;
        }
    }
}