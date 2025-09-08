using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FNEV4.Core.Entities;

namespace FNEV4.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour FolderConfiguration
    /// </summary>
    public class FolderConfigurationConfiguration : IEntityTypeConfiguration<FolderConfiguration>
    {
        public void Configure(EntityTypeBuilder<FolderConfiguration> builder)
        {
            builder.ToTable("FolderConfigurations");

            // Clé primaire
            builder.HasKey(f => f.Id);

            // Propriétés requises avec longueurs
            builder.Property(f => f.ImportFolderPath)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(f => f.ExportFolderPath)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(f => f.ArchiveFolderPath)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(f => f.LogsFolderPath)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(f => f.BackupFolderPath)
                .IsRequired()
                .HasMaxLength(500);

            // Propriétés optionnelles
            builder.Property(f => f.Name)
                .HasMaxLength(100);

            builder.Property(f => f.Description)
                .HasMaxLength(500);

            // Index pour performance
            builder.HasIndex(f => f.Name)
                .IsUnique()
                .HasDatabaseName("IX_FolderConfigurations_Name");

            builder.HasIndex(f => f.IsActive)
                .HasDatabaseName("IX_FolderConfigurations_IsActive");

            // Configuration par défaut pour une seule configuration active
            builder.HasData(new FolderConfiguration
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
                Name = "Configuration Par Défaut",
                Description = "Configuration standard des chemins FNEV4",
                ImportFolderPath = @"C:\wamp64\www\FNEV4\data\Import",
                ExportFolderPath = @"C:\wamp64\www\FNEV4\data\Export",
                ArchiveFolderPath = @"C:\wamp64\www\FNEV4\data\Archive",
                LogsFolderPath = @"C:\wamp64\www\FNEV4\data\Logs",
                BackupFolderPath = @"C:\wamp64\www\FNEV4\data\Backup",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }
}
