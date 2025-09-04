using Microsoft.EntityFrameworkCore;
using FNEV4.Infrastructure.Data;
using FNEV4.Core.Entities;
using System.Data;
using Microsoft.Data.Sqlite;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Service pour la gestion et maintenance de la base de données
    /// </summary>
    public interface IDatabaseService
    {
        Task<DatabaseInfo> GetDatabaseInfoAsync();
        Task<List<TableInfo>> GetTablesInfoAsync();
        Task<bool> BackupDatabaseAsync(string backupPath);
        Task<bool> RestoreDatabaseAsync(string backupPath);
        Task<bool> OptimizeDatabaseAsync();
        Task<bool> CheckIntegrityAsync();
        Task<string> ExecuteQueryAsync(string query);
        Task<bool> ApplyMigrationsAsync();
        Task<bool> InitializeDatabaseAsync();
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly FNEV4DbContext _context;
        private readonly string _connectionString;

        public DatabaseService(FNEV4DbContext context)
        {
            _context = context;
            _connectionString = _context.Database.GetConnectionString() ?? "";
        }

        public async Task<DatabaseInfo> GetDatabaseInfoAsync()
        {
            var dbPath = GetDatabasePath();
            var info = new DatabaseInfo
            {
                Path = dbPath,
                ConnectionStatus = await TestConnectionAsync() ? "Connectée" : "Déconnectée"
            };

            if (File.Exists(dbPath))
            {
                var fileInfo = new FileInfo(dbPath);
                info.Size = FormatFileSize(fileInfo.Length);
                info.LastModified = fileInfo.LastWriteTime;
            }

            try
            {
                // Version SQLite
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT sqlite_version()";
                var version = await command.ExecuteScalarAsync();
                info.Version = $"SQLite {version}";

                // Nombre de tables
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                var tableCount = await command.ExecuteScalarAsync();
                info.TableCount = Convert.ToInt32(tableCount);
            }
            catch
            {
                info.Version = "Inconnu";
                info.TableCount = 0;
            }

            return info;
        }

        public async Task<List<TableInfo>> GetTablesInfoAsync()
        {
            var tables = new List<TableInfo>();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Récupérer les tables
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT name 
                    FROM sqlite_master 
                    WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name != '__EFMigrationsHistory'
                    ORDER BY name";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var tableName = reader.GetString(0);
                    var tableInfo = await GetTableInfoAsync(connection, tableName);
                    tables.Add(tableInfo);
                }
            }
            catch (Exception ex)
            {
                // Log l'erreur
                Console.WriteLine($"Erreur lors de la récupération des tables: {ex.Message}");
            }

            return tables;
        }

        private async Task<TableInfo> GetTableInfoAsync(SqliteConnection connection, string tableName)
        {
            var tableInfo = new TableInfo { Name = tableName };

            try
            {
                // Nombre de lignes
                using var countCommand = connection.CreateCommand();
                countCommand.CommandText = $"SELECT COUNT(*) FROM [{tableName}]";
                var rowCount = await countCommand.ExecuteScalarAsync();
                tableInfo.RowCount = Convert.ToInt32(rowCount);

                // Estimation de la taille (approximative)
                tableInfo.Size = EstimateTableSize(tableInfo.RowCount);
                tableInfo.LastModified = DateTime.Now.ToString("dd/MM/yyyy");
            }
            catch
            {
                tableInfo.RowCount = 0;
                tableInfo.Size = "0 KB";
            }

            return tableInfo;
        }

        public async Task<bool> BackupDatabaseAsync(string backupPath)
        {
            try
            {
                var dbPath = GetDatabasePath();
                if (!File.Exists(dbPath))
                    return false;

                // Créer le dossier de sauvegarde s'il n'existe pas
                var backupDir = Path.GetDirectoryName(backupPath);
                if (!string.IsNullOrEmpty(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // Copier le fichier de base de données
                File.Copy(dbPath, backupPath, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RestoreDatabaseAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                    return false;

                var dbPath = GetDatabasePath();
                
                // Fermer les connexions existantes
                await _context.Database.CloseConnectionAsync();
                
                // Restaurer la sauvegarde
                File.Copy(backupPath, dbPath, true);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> OptimizeDatabaseAsync()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("VACUUM");
                await _context.Database.ExecuteSqlRawAsync("ANALYZE");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckIntegrityAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA integrity_check";
                var result = await command.ExecuteScalarAsync();
                return result?.ToString() == "ok";
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> ExecuteQueryAsync(string query)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                using var command = connection.CreateCommand();
                command.CommandText = query;

                if (query.Trim().ToUpper().StartsWith("SELECT"))
                {
                    // Requête de lecture
                    using var reader = await command.ExecuteReaderAsync();
                    var results = new List<string>();
                    
                    // En-têtes
                    var headers = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        headers.Add(reader.GetName(i));
                    }
                    results.Add(string.Join(" | ", headers));
                    results.Add(string.Join("-|-", headers.Select(h => new string('-', h.Length))));

                    // Données
                    while (await reader.ReadAsync())
                    {
                        var row = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader.GetValue(i)?.ToString() ?? "NULL");
                        }
                        results.Add(string.Join(" | ", row));
                    }

                    return string.Join("\n", results);
                }
                else
                {
                    // Requête de modification
                    var affectedRows = await command.ExecuteNonQueryAsync();
                    return $"Requête exécutée avec succès. {affectedRows} ligne(s) affectée(s).";
                }
            }
            catch (Exception ex)
            {
                return $"Erreur lors de l'exécution de la requête: {ex.Message}";
            }
        }

        public async Task<bool> ApplyMigrationsAsync()
        {
            try
            {
                await _context.Database.MigrateAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Début InitializeDatabaseAsync ===");
                System.Diagnostics.Debug.WriteLine($"Connection String: {_connectionString}");
                
                // Vérifier que le dossier existe
                var dbPath = "Data/FNEV4.db";
                var directory = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    System.Diagnostics.Debug.WriteLine($"Dossier créé: {directory}");
                }
                
                // Supprimer et recréer la base avec structure vide
                System.Diagnostics.Debug.WriteLine("Suppression de la base de données...");
                var deleted = await _context.Database.EnsureDeletedAsync();
                System.Diagnostics.Debug.WriteLine($"Base supprimée: {deleted}");
                
                System.Diagnostics.Debug.WriteLine("Création de la base de données...");
                var created = await _context.Database.EnsureCreatedAsync();
                System.Diagnostics.Debug.WriteLine($"Base créée: {created}");
                
                // Vérifier que le fichier existe
                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    System.Diagnostics.Debug.WriteLine($"Fichier DB créé: {dbPath} ({fileInfo.Length} bytes)");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ERREUR: Fichier DB non trouvé: {dbPath}");
                }
                
                System.Diagnostics.Debug.WriteLine("=== Fin InitializeDatabaseAsync ===");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR InitializeDatabaseAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        private async Task<bool> TestConnectionAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        private string GetDatabasePath()
        {
            var connectionString = _context.Database.GetConnectionString();
            if (connectionString?.Contains("Data Source=") == true)
            {
                var dataSource = connectionString.Split("Data Source=")[1].Split(';')[0];
                return Path.GetFullPath(dataSource);
            }
            return "";
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private static string EstimateTableSize(int rowCount)
        {
            // Estimation approximative : 1KB par 10 lignes
            var estimatedBytes = rowCount * 100;
            return FormatFileSize(estimatedBytes);
        }
    }

    public class DatabaseInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Size { get; set; } = "0 KB";
        public string Version { get; set; } = "Inconnu";
        public string ConnectionStatus { get; set; } = "Déconnectée";
        public DateTime? LastModified { get; set; }
        public int TableCount { get; set; }
    }

    public class TableInfo
    {
        public string Name { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public string Size { get; set; } = "0 KB";
        public string LastModified { get; set; } = string.Empty;
    }
}
