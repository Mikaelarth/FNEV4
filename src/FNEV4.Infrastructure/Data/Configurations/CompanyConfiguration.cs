using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FNEV4.Core.Entities;

namespace FNEV4.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour Company
    /// </summary>
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Companies");

            // Clé primaire
            builder.HasKey(x => x.Id);

            // Propriétés requises
            builder.Property(x => x.Ncc)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.CompanyName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Address)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Environment)
                .HasMaxLength(20);

            // Colonnes décimales
            builder.Property(x => x.ShareCapital)
                .HasColumnType("decimal(18,2)");

            // Index unique
            builder.HasIndex(x => x.Ncc)
                .IsUnique();

            // Index pour performance
            builder.HasIndex(x => x.CompanyName);
            builder.HasIndex(x => x.IsActive);
        }
    }
}
