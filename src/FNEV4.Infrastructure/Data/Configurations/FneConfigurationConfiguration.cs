using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FNEV4.Core.Entities;

namespace FNEV4.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour FneConfiguration
    /// </summary>
    public class FneConfigurationConfiguration : IEntityTypeConfiguration<FneConfiguration>
    {
        public void Configure(EntityTypeBuilder<FneConfiguration> builder)
        {
            builder.ToTable("FneConfigurations");

            // Clé primaire
            builder.HasKey(x => x.Id);

            // Propriétés requises
            builder.Property(x => x.ConfigurationName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Environment)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.BaseUrl)
                .IsRequired()
                .HasMaxLength(200);

            // Index unique
            builder.HasIndex(x => new { x.ConfigurationName, x.Environment })
                .IsUnique();

            // Index pour performance
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.IsValidatedByDgi);
            builder.HasIndex(x => x.Environment);

            // Données par défaut (seed data)
            builder.HasData(
                new FneConfiguration
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    ConfigurationName = "Test DGI",
                    Environment = "Test",
                    BaseUrl = "http://54.247.95.108/ws",
                    WebUrl = "http://54.247.95.108",
                    SupportEmail = "support.fne@dgi.gouv.ci",
                    RequestTimeoutSeconds = 30,
                    MaxRetryAttempts = 3,
                    RetryDelaySeconds = 5,
                    ApiVersion = "1.0",
                    IsActive = true,
                    IsValidatedByDgi = false,
                    CreatedDate = new DateTime(2025, 9, 4, 0, 0, 0, DateTimeKind.Utc),
                    CreatedAt = new DateTime(2025, 9, 4, 0, 0, 0, DateTimeKind.Utc),
                    Notes = "Configuration par défaut pour l'environnement de test DGI"
                }
            );
        }
    }
}
