using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FNEV4.Core.Entities;

namespace FNEV4.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour FneInvoice
    /// </summary>
    public class FneInvoiceConfiguration : IEntityTypeConfiguration<FneInvoice>
    {
        public void Configure(EntityTypeBuilder<FneInvoice> builder)
        {
            builder.ToTable("FneInvoices");

            // Clé primaire
            builder.HasKey(x => x.Id);

            // Propriétés requises
            builder.Property(x => x.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.InvoiceType)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.PointOfSale)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(x => x.PaymentMethod)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Template)
                .IsRequired()
                .HasMaxLength(5);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20);

            // Colonnes décimales
            builder.Property(x => x.TotalAmountHT)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.TotalVatAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.TotalAmountTTC)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.GlobalDiscount)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.ForeignCurrencyRate)
                .HasColumnType("decimal(10,4)");

            // Index pour performance
            builder.HasIndex(x => x.InvoiceNumber);
            builder.HasIndex(x => x.FneReference);
            builder.HasIndex(x => x.InvoiceDate);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.ClientId);
            builder.HasIndex(x => x.ImportSessionId);

            // Relations
            builder.HasOne(x => x.Client)
                .WithMany(c => c.Invoices)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ParentInvoice)
                .WithMany(p => p.ChildInvoices)
                .HasForeignKey(x => x.ParentInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ImportSession)
                .WithMany(s => s.Invoices)
                .HasForeignKey(x => x.ImportSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Items)
                .WithOne(i => i.FneInvoice)
                .HasForeignKey(i => i.FneInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.ApiLogs)
                .WithOne(l => l.FneInvoice)
                .HasForeignKey(l => l.FneInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
