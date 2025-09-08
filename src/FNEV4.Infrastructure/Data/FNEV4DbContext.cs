using Microsoft.EntityFrameworkCore;
using FNEV4.Core.Entities;
using System.Reflection;
using System.Linq.Expressions;

namespace FNEV4.Infrastructure.Data
{
    /// <summary>
    /// Contexte de base de données principal pour FNEV4
    /// Structure conforme aux spécifications ARCHITECTURE.md et CAHIER-DES-CHARGES-FNEV4.md
    /// </summary>
    public class FNEV4DbContext : DbContext
    {
        public FNEV4DbContext(DbContextOptions<FNEV4DbContext> options) : base(options)
        {
        }

        #region DbSets - Entités FNE selon spécifications .md

        /// <summary>
        /// Configuration entreprise utilisatrice
        /// </summary>
        public DbSet<Company> Companies { get; set; }

        /// <summary>
        /// Configurations API FNE (Test/Production)
        /// </summary>
        public DbSet<FneConfiguration> FneConfigurations { get; set; }

        /// <summary>
        /// Référentiel clients avec NCC et classification
        /// </summary>
        public DbSet<Client> Clients { get; set; }

        /// <summary>
        /// Factures FNE principales avec statuts
        /// </summary>
        public DbSet<FneInvoice> FneInvoices { get; set; }

        /// <summary>
        /// Lignes de facture avec calculs
        /// </summary>
        public DbSet<FneInvoiceItem> FneInvoiceItems { get; set; }

        /// <summary>
        /// Types de TVA avec taux (TVA, TVAB, TVAC, TVAD)
        /// </summary>
        public DbSet<VatType> VatTypes { get; set; }

        /// <summary>
        /// Historique des imports Excel
        /// </summary>
        public DbSet<ImportSession> ImportSessions { get; set; }

        /// <summary>
        /// Journalisation complète des échanges API FNE
        /// </summary>
        public DbSet<FneApiLog> FneApiLogs { get; set; }

        /// <summary>
        /// Logs système de l'application
        /// </summary>
        public DbSet<LogEntry> LogEntries { get; set; }

        /// <summary>
        /// Configuration des chemins et dossiers de l'application
        /// </summary>
        public DbSet<FolderConfiguration> FolderConfigurations { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Application des configurations via Assembly Scanning
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Configuration soft delete global
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var isDeletedProperty = entityType.FindProperty("IsDeleted");
                if (isDeletedProperty != null)
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, "IsDeleted");
                    var condition = Expression.Equal(property, Expression.Constant(false));
                    var lambda = Expression.Lambda(condition, parameter);
                    
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "FNEV4.db");
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
                
                #if DEBUG
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
                #endif
            }
        }

        /// <summary>
        /// Override SaveChanges pour audit trail automatique
        /// </summary>
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync pour audit trail automatique
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Met à jour automatiquement les champs d'audit (CreatedAt, UpdatedAt)
        /// </summary>
        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
