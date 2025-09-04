using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FNEV4.Core.Entities;

namespace FNEV4.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour FneInvoiceItem
    /// </summary>
    public class FneInvoiceItemConfiguration : IEntityTypeConfiguration<FneInvoiceItem>
    {
        public void Configure(EntityTypeBuilder<FneInvoiceItem> builder)
        {
            builder.ToTable("FneInvoiceItems");

            // Clé primaire
            builder.HasKey(x => x.Id);

            // Propriétés requises
            builder.Property(x => x.ProductCode)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.VatCode)
                .IsRequired()
                .HasMaxLength(10);

            // Colonnes décimales
            builder.Property(x => x.UnitPrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.Quantity)
                .HasColumnType("decimal(10,3)");

            builder.Property(x => x.VatRate)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.LineAmountHT)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.LineVatAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.LineAmountTTC)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.ItemDiscount)
                .HasColumnType("decimal(5,2)");

            // Index pour performance
            builder.HasIndex(x => x.FneInvoiceId);
            builder.HasIndex(x => x.VatTypeId);
            builder.HasIndex(x => x.ProductCode);

            // Relations
            builder.HasOne(x => x.FneInvoice)
                .WithMany(i => i.Items)
                .HasForeignKey(x => x.FneInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.VatType)
                .WithMany(v => v.InvoiceItems)
                .HasForeignKey(x => x.VatTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
