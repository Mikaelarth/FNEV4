using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FNEV4.Core.Entities;

namespace FNEV4.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour Client
    /// </summary>
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            builder.ToTable("Clients");

            // Clé primaire
            builder.HasKey(x => x.Id);

            // Propriétés requises
            builder.Property(x => x.ClientCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.ClientType)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.DefaultTemplate)
                .IsRequired()
                .HasMaxLength(5);

            builder.Property(x => x.DefaultPaymentMethod)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("cash");

            // Index uniques
            builder.HasIndex(x => x.ClientCode)
                .IsUnique();

            builder.HasIndex(x => x.ClientNcc);

            // Index pour performance
            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.ClientType);
            builder.HasIndex(x => x.DefaultPaymentMethod);
            builder.HasIndex(x => x.IsActive);

            // Relations
            builder.HasMany(x => x.Invoices)
                .WithOne(i => i.Client)
                .HasForeignKey(i => i.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
