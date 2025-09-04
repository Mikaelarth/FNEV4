using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FNEV4.Core.Entities;

namespace FNEV4.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour FneApiLog
    /// </summary>
    public class FneApiLogConfiguration : IEntityTypeConfiguration<FneApiLog>
    {
        public void Configure(EntityTypeBuilder<FneApiLog> builder)
        {
            builder.ToTable("FneApiLogs");

            // Clé primaire
            builder.HasKey(x => x.Id);

            // Propriétés requises
            builder.Property(x => x.OperationType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Endpoint)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.HttpMethod)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(x => x.Environment)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.LogLevel)
                .IsRequired()
                .HasMaxLength(20);

            // Index pour performance
            builder.HasIndex(x => x.FneInvoiceId);
            builder.HasIndex(x => x.Timestamp);
            builder.HasIndex(x => x.OperationType);
            builder.HasIndex(x => x.IsSuccess);
            builder.HasIndex(x => x.LogLevel);

            // Relations
            builder.HasOne(x => x.FneInvoice)
                .WithMany(i => i.ApiLogs)
                .HasForeignKey(x => x.FneInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
