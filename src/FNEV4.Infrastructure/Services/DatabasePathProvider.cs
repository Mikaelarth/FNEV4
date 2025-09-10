using System;
using System.IO;
using Microsoft.Extensions.Configuration;

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
        private readonly IConfiguration? _configuration;

        public DatabasePathProvider(IConfiguration? configuration = null)
        {
            _configuration = configuration;
            // CHEMIN ABSOLU FIXE - Plus jamais de variation !
            // Utilise la configuration pour déterminer le chemin
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

        private string GetFixedDatabasePath()
        {
            // Méthode 1: Variable d'environnement (priorité absolue)
            var envPath = Environment.GetEnvironmentVariable("FNEV4_DATABASE_PATH");
            if (!string.IsNullOrEmpty(envPath))
            {
                return envPath;
            }

            // Méthode 2: Configuration appsettings.json
            var configPath = _configuration?["PathSettings:DatabasePath"];
            if (!string.IsNullOrEmpty(configPath))
            {
                // Si chemin relatif, le rendre absolu par rapport au répertoire de l'application
                if (!Path.IsPathRooted(configPath))
                {
                    configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath);
                }
                return configPath;
            }

            // Méthode 3: Mode basé sur la configuration d'environnement
            var environmentType = _configuration?["Environment:Type"] ?? "Development";
            var databaseMode = _configuration?["Environment:DatabaseMode"] ?? "Auto";

            switch (databaseMode.ToLower())
            {
                case "appdata":
                    return GetAppDataPath();
                
                case "project":
                    return GetProjectPath() ?? GetAppDataPath();
                
                case "auto":
                default:
                    // Auto: Détection intelligente
                    return environmentType.ToLower() == "production" 
                        ? GetAppDataPath() 
                        : (GetProjectPath() ?? GetAppDataPath());
            }
        }

        private static string GetAppDataPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FNEV4",
                "FNEV4.db"
            );
        }

        private static string? GetProjectPath()
        {
            // Chercher le répertoire racine du projet (présence du fichier .sln)
            var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            
            while (currentDir != null)
            {
                if (File.Exists(Path.Combine(currentDir.FullName, "FNEV4.sln")))
                {
                    return Path.Combine(currentDir.FullName, "data", "FNEV4.db");
                }
                currentDir = currentDir.Parent;
            }
            
            return null;
        }
    }
}
