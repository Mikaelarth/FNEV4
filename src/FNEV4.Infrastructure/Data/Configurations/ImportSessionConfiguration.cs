using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FNEV4.Core.Entities;

namespace FNEV4.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour ImportSession
    /// </summary>
    public class ImportSessionConfiguration : IEntityTypeConfiguration<ImportSession>
    {
        public void Configure(EntityTypeBuilder<ImportSession> builder)
        {
            builder.ToTable("ImportSessions");

            // Clé primaire
            builder.HasKey(x => x.Id);

            // Propriétés requises
            builder.Property(x => x.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.FilePath)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20);

            // Index pour performance
            builder.HasIndex(x => x.StartedAt);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.UserName);

            // Relations
            builder.HasMany(x => x.Invoices)
                .WithOne(i => i.ImportSession)
                .HasForeignKey(i => i.ImportSessionId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
