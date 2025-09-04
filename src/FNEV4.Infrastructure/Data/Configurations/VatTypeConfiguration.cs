using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FNEV4.Core.Entities;

namespace FNEV4.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour VatType
    /// </summary>
    public class VatTypeConfiguration : IEntityTypeConfiguration<VatType>
    {
        public void Configure(EntityTypeBuilder<VatType> builder)
        {
            builder.ToTable("VatTypes");

            // Clé primaire
            builder.HasKey(x => x.Id);

            // Propriétés requises
            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(100);

            // Colonnes décimales
            builder.Property(x => x.Rate)
                .HasColumnType("decimal(5,2)");

            // Index unique
            builder.HasIndex(x => x.Code)
                .IsUnique();

            // Index pour performance
            builder.HasIndex(x => x.IsActive);

            // Relations
            builder.HasMany(x => x.InvoiceItems)
                .WithOne(i => i.VatType)
                .HasForeignKey(i => i.VatTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Données par défaut (seed data)
            builder.HasData(
                new VatType
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Code = "TVA",
                    Description = "TVA normal de 18%",
                    Rate = 18.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new VatType
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Code = "TVAB",
                    Description = "TVA réduit de 9%",
                    Rate = 9.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new VatType
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Code = "TVAC",
                    Description = "TVA exec conv de 0%",
                    Rate = 0.00m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new VatType
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Code = "TVAD",
                    Description = "TVA exec leg de 0% pour TEE et RME",
                    Rate = 0.00m,
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 9, 4, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
