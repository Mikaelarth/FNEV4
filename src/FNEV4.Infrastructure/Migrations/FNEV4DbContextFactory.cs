using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using FNEV4.Infrastructure.Data;

namespace FNEV4.Infrastructure.Migrations
{
    /// <summary>
    /// Factory pour le DbContext en mode design-time (migrations)
    /// </summary>
    public class FNEV4DbContextFactory : IDesignTimeDbContextFactory<FNEV4DbContext>
    {
        public FNEV4DbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FNEV4DbContext>();
            
            // Utilise la base de données par défaut pour les migrations
            optionsBuilder.UseSqlite("Data Source=data/FNEV4.db");
            
            return new FNEV4DbContext(optionsBuilder.Options);
        }
    }
}
