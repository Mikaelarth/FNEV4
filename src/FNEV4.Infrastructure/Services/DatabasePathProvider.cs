using System;
using System.IO;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Service singleton pour centraliser et fixer le chemin de la base de données
    /// Garantit qu'une seule base de données est utilisée dans toute l'application
    /// </summary>
    public interface IDatabasePathProvider
    {
        string DatabasePath { get; }
        string GetConnectionString();
        void EnsureDatabaseDirectoryExists();
    }

    public class DatabasePathProvider : IDatabasePathProvider
    {
        private readonly string _databasePath;

        public DatabasePathProvider()
        {
            // CHEMIN ABSOLU FIXE - Plus jamais de variation !
            // Utilise un chemin relatif au répertoire de la solution
            _databasePath = GetFixedDatabasePath();
            EnsureDatabaseDirectoryExists();
        }

        public string DatabasePath => _databasePath;

        public string GetConnectionString()
        {
            return $"Data Source={_databasePath};Cache=Shared";
        }

        public void EnsureDatabaseDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static string GetFixedDatabasePath()
        {
            // Méthode 1: Variable d'environnement pour production
            var envPath = Environment.GetEnvironmentVariable("FNEV4_DATABASE_PATH");
            if (!string.IsNullOrEmpty(envPath))
            {
                return envPath;
            }

            // Méthode 2: Chemin fixe pour développement
            var devPath = @"C:\wamp64\www\FNEV4\data\FNEV4.db";
            if (Directory.Exists(Path.GetDirectoryName(devPath)))
            {
                return devPath;
            }

            // Méthode 3: Chercher le répertoire racine du projet
            var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (currentDir != null)
            {
                if (File.Exists(Path.Combine(currentDir.FullName, "FNEV4.sln")))
                {
                    return Path.Combine(currentDir.FullName, "data", "FNEV4.db");
                }
                currentDir = currentDir.Parent;
            }

            // Méthode 4: Fallback dans le répertoire de l'application
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FNEV4",
                "FNEV4.db"
            );
            
            return appDataPath;
        }
    }
}
