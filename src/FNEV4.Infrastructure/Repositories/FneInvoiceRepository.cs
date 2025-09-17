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
            // Chargement des factures avec leurs clients ET leurs articles via Include
            var invoices = await _context.FneInvoices
                .Include(i => i.Items)  // Charger les articles de chaque facture
                .Include(i => i.Client) // Charger les clients  
                .Where(i => i.Status == "draft" && i.CertifiedAt == null)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            return invoices;
        }

        /// <summary>
        /// NOUVELLE MÉTHODE UNIFIÉE - Récupère les factures avec filtrage complet
        /// Remplace les multiples méthodes de filtrage par une approche unifiée
        /// </summary>
        public async Task<IEnumerable<FneInvoice>> GetInvoicesAsync(FNEV4.Core.Models.Filters.InvoiceFilter? filter = null)
        {
            var query = _context.FneInvoices
                .Include(i => i.Items)
                .Include(i => i.Client)
                .AsQueryable();

            // Appliquer les filtres si fournis
            if (filter != null)
            {
                // Filtrer par statuts
                if (filter.Statuses?.Length > 0)
                {
                    query = query.Where(i => filter.Statuses.Contains(i.Status));
                }
                else
                {
                    // Filtres booléens par défaut
                    var statusFilters = new List<string>();
                    if (filter.IncludeDraft) statusFilters.Add("draft");
                    if (filter.IncludeCertified) statusFilters.Add("certified");
                    if (filter.IncludeErrors) statusFilters.Add("error");
                    
                    if (statusFilters.Any())
                    {
                        query = query.Where(i => statusFilters.Contains(i.Status));
                    }
                }

                // Filtrer par types de facture
                if (filter.InvoiceTypes?.Length > 0)
                {
                    query = query.Where(i => filter.InvoiceTypes.Contains(i.InvoiceType));
                }

                // Filtrer par dates
                if (filter.DateFrom.HasValue)
                {
                    query = query.Where(i => i.InvoiceDate >= filter.DateFrom.Value);
                }
                if (filter.DateTo.HasValue)
                {
                    query = query.Where(i => i.InvoiceDate <= filter.DateTo.Value);
                }

                // Filtrer par montant
                if (filter.MinAmount.HasValue)
                {
                    query = query.Where(i => i.TotalAmountTTC >= filter.MinAmount.Value);
                }
                if (filter.MaxAmount.HasValue)
                {
                    query = query.Where(i => i.TotalAmountTTC <= filter.MaxAmount.Value);
                }

                // Recherche textuelle
                if (!string.IsNullOrEmpty(filter.SearchText))
                {
                    var searchLower = filter.SearchText.ToLower();
                    query = query.Where(i => 
                        i.InvoiceNumber.ToLower().Contains(searchLower) ||
                        i.ClientCode.ToLower().Contains(searchLower) ||
                        (i.Client != null && (
                            (i.Client.CompanyName ?? "").ToLower().Contains(searchLower) ||
                            (i.Client.Name ?? "").ToLower().Contains(searchLower)
                        ))
                    );
                }

                // Filtrer par code client
                if (!string.IsNullOrEmpty(filter.ClientCode))
                {
                    query = query.Where(i => i.ClientCode == filter.ClientCode);
                }

                // Tri
                query = filter.SortBy?.ToLower() switch
                {
                    "invoicedate" => filter.SortDescending ? 
                        query.OrderByDescending(i => i.InvoiceDate) : 
                        query.OrderBy(i => i.InvoiceDate),
                    "createdat" => filter.SortDescending ? 
                        query.OrderByDescending(i => i.CreatedAt) : 
                        query.OrderBy(i => i.CreatedAt),
                    "amount" => filter.SortDescending ? 
                        query.OrderByDescending(i => i.TotalAmountTTC) : 
                        query.OrderBy(i => i.TotalAmountTTC),
                    "invoicenumber" => filter.SortDescending ? 
                        query.OrderByDescending(i => i.InvoiceNumber) : 
                        query.OrderBy(i => i.InvoiceNumber),
                    "status" => filter.SortDescending ? 
                        query.OrderByDescending(i => i.Status) : 
                        query.OrderBy(i => i.Status),
                    _ => filter.SortDescending ? 
                        query.OrderByDescending(i => i.InvoiceDate) : 
                        query.OrderBy(i => i.InvoiceDate)
                };

                // Limite de résultats
                if (filter.Limit.HasValue && filter.Limit.Value > 0)
                {
                    query = query.Take(filter.Limit.Value);
                }
            }
            else
            {
                // Par défaut : toutes les factures triées par date décroissante
                query = query.OrderByDescending(i => i.InvoiceDate);
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Compte le nombre de factures selon les filtres
        /// </summary>
        public async Task<int> CountInvoicesAsync(FNEV4.Core.Models.Filters.InvoiceFilter? filter = null)
        {
            var query = _context.FneInvoices.AsQueryable();

            // Réutiliser la même logique de filtrage
            if (filter != null)
            {
                // Appliquer les mêmes filtres que GetInvoicesAsync (sans Include et OrderBy)
                if (filter.Statuses?.Length > 0)
                {
                    query = query.Where(i => filter.Statuses.Contains(i.Status));
                }
                else
                {
                    var statusFilters = new List<string>();
                    if (filter.IncludeDraft) statusFilters.Add("draft");
                    if (filter.IncludeCertified) statusFilters.Add("certified");
                    if (filter.IncludeErrors) statusFilters.Add("error");
                    
                    if (statusFilters.Any())
                    {
                        query = query.Where(i => statusFilters.Contains(i.Status));
                    }
                }

                if (filter.InvoiceTypes?.Length > 0)
                {
                    query = query.Where(i => filter.InvoiceTypes.Contains(i.InvoiceType));
                }

                if (filter.DateFrom.HasValue)
                {
                    query = query.Where(i => i.InvoiceDate >= filter.DateFrom.Value);
                }
                if (filter.DateTo.HasValue)
                {
                    query = query.Where(i => i.InvoiceDate <= filter.DateTo.Value);
                }

                if (filter.MinAmount.HasValue)
                {
                    query = query.Where(i => i.TotalAmountTTC >= filter.MinAmount.Value);
                }
                if (filter.MaxAmount.HasValue)
                {
                    query = query.Where(i => i.TotalAmountTTC <= filter.MaxAmount.Value);
                }

                if (!string.IsNullOrEmpty(filter.SearchText))
                {
                    var searchLower = filter.SearchText.ToLower();
                    query = query.Where(i => 
                        i.InvoiceNumber.ToLower().Contains(searchLower) ||
                        i.ClientCode.ToLower().Contains(searchLower)
                    );
                }

                if (!string.IsNullOrEmpty(filter.ClientCode))
                {
                    query = query.Where(i => i.ClientCode == filter.ClientCode);
                }
            }

            return await query.CountAsync();
        }

        /// <summary>
        /// Récupère les statistiques étendues
        /// </summary>
        public async Task<ExtendedInvoiceStatistics> GetExtendedStatisticsAsync(FNEV4.Core.Models.Filters.InvoiceFilter? filter = null)
        {
            var query = _context.FneInvoices.AsQueryable();

            // Appliquer les mêmes filtres de base
            if (filter != null)
            {
                if (filter.DateFrom.HasValue)
                {
                    query = query.Where(i => i.InvoiceDate >= filter.DateFrom.Value);
                }
                if (filter.DateTo.HasValue)
                {
                    query = query.Where(i => i.InvoiceDate <= filter.DateTo.Value);
                }
                if (!string.IsNullOrEmpty(filter.ClientCode))
                {
                    query = query.Where(i => i.ClientCode == filter.ClientCode);
                }
            }

            // Récupérer les données pour éviter les problèmes d'agrégation SQLite avec decimal
            var invoiceData = await query.Select(i => new { i.Status, i.InvoiceType, i.TotalAmountTTC }).ToListAsync();
            
            var stats = new ExtendedInvoiceStatistics
            {
                TotalInvoices = invoiceData.Count,
                DraftInvoices = invoiceData.Count(i => i.Status == "draft"),
                CertifiedInvoices = invoiceData.Count(i => i.Status == "certified"),
                ValidatedInvoices = invoiceData.Count(i => i.Status == "validated"),
                ErrorInvoices = invoiceData.Count(i => i.Status == "error"),
                RefundInvoices = invoiceData.Count(i => i.InvoiceType == "refund"),

                // Utiliser LINQ to Objects pour les sommes de décimaux
                TotalAmountDraft = invoiceData.Where(i => i.Status == "draft").Sum(i => i.TotalAmountTTC),
                TotalAmountCertified = invoiceData.Where(i => i.Status == "certified").Sum(i => i.TotalAmountTTC),
                TotalAmountError = invoiceData.Where(i => i.Status == "error").Sum(i => i.TotalAmountTTC),
            };

            // Calculer les propriétés héritées
            stats.PendingInvoices = stats.DraftInvoices + stats.ValidatedInvoices;
            stats.MonthlyRevenue = stats.TotalAmountCertified;

            // Taux de réussite
            if (stats.TotalInvoices > 0)
            {
                stats.SuccessRate = (double)stats.CertifiedInvoices / stats.TotalInvoices * 100;
            }

            // Dictionnaires pour les graphiques
            stats.InvoicesByStatus = new Dictionary<string, int>
            {
                ["draft"] = stats.DraftInvoices,
                ["certified"] = stats.CertifiedInvoices,
                ["validated"] = stats.ValidatedInvoices,
                ["error"] = stats.ErrorInvoices
            };

            stats.AmountsByStatus = new Dictionary<string, decimal>
            {
                ["draft"] = stats.TotalAmountDraft,
                ["certified"] = stats.TotalAmountCertified,
                ["error"] = stats.TotalAmountError
            };

            return stats;
        }
    }
}