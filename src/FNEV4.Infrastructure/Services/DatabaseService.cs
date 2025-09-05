using Microsoft.EntityFrameworkCore;
using FNEV4.Infrastructure.Data;
using FNEV4.Core.Entities;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;

namespace FNEV4.Infrastructure.Services
{
    /// <summary>
    /// Service pour la gestion et maintenance de la base de données
    /// </summary>
    public interface IDatabaseService
    {
        Task<DatabaseInfo> GetDatabaseInfoAsync();
        Task<List<TableInfo>> GetTablesInfoAsync();
        Task<DataTable> GetTableDataAsync(string tableName, int pageSize = 50, int pageNumber = 1, string searchFilter = "");
        Task<bool> BackupDatabaseAsync(string backupPath);
        Task<bool> RestoreDatabaseAsync(string backupPath);
        Task<bool> OptimizeDatabaseAsync();
        Task<bool> CheckIntegrityAsync();
        Task<bool> ReindexDatabaseAsync();
        Task<bool> UpdateConnectionStringAsync(string newDatabasePath);
        Task<bool> CreateAutomaticBackupAsync(string backupDirectory, bool compressBackup = false);
        Task<string> ExecuteQueryAsync(string query);
        Task<bool> ApplyMigrationsAsync();
        Task<bool> InitializeDatabaseAsync();
        Task<bool> InsertRecordAsync(string tableName, Dictionary<string, object> values);
        Task<bool> UpdateRecordAsync(string tableName, string id, Dictionary<string, object> values);
        Task<bool> DeleteRecordAsync(string tableName, string id);
        Task<List<string>> GetTableColumnsAsync(string tableName);
        string GetConnectionString();
        
        // Méthodes de surveillance et alertes
        Task<DatabaseMonitoringInfo> GetDatabaseMonitoringInfoAsync();
        Task<bool> CheckDatabaseAlertsAsync(double maxSizeMB, int maxTableCount, bool emailAlertsEnabled, string alertEmailAddress);
        Task<bool> SendEmailAlertAsync(string emailAddress, string subject, string message);
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly FNEV4DbContext _context;
        private string _connectionString;

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

        public async Task<DataTable> GetTableDataAsync(string tableName, int pageSize = 50, int pageNumber = 1, string searchFilter = "")
        {
            var dataTable = new DataTable();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // D'abord, récupérer la structure de la table
                using var schemaCommand = connection.CreateCommand();
                schemaCommand.CommandText = $"PRAGMA table_info([{tableName}])";
                
                using var schemaReader = await schemaCommand.ExecuteReaderAsync();
                var columns = new List<(string Name, string Type)>();
                
                while (await schemaReader.ReadAsync())
                {
                    var columnName = schemaReader.GetString("name");
                    var columnType = schemaReader.GetString("type");
                    
                    // Exclure les colonnes ID techniques (GUIDs) de l'affichage utilisateur
                    if (ShouldExcludeColumn(columnName, columnType))
                    {
                        continue;
                    }
                    
                    columns.Add((columnName, columnType));
                    
                    // Ajouter la colonne au DataTable
                    var netType = GetNetTypeFromSqliteType(columnType);
                    dataTable.Columns.Add(columnName, netType);
                }

                // Construire la liste des colonnes à récupérer (exclure les techniques)
                var selectColumns = columns.Select(c => $"[{c.Name}]").ToArray();
                var baseQuery = selectColumns.Any() 
                    ? $"SELECT {string.Join(", ", selectColumns)} FROM [{tableName}]"
                    : $"SELECT * FROM [{tableName}]"; // Fallback si aucune colonne
                var whereClause = "";
                var orderClause = " ORDER BY 1"; // Ordonner par la première colonne
                
                // Ajouter le filtre de recherche si spécifié
                if (!string.IsNullOrWhiteSpace(searchFilter) && columns.Any())
                {
                    // Rechercher seulement dans les colonnes visibles (non-exclues)
                    var searchConditions = columns
                        .Where(c => IsTextColumn(c.Type))
                        .Select(c => $"[{c.Name}] LIKE @search")
                        .ToArray();
                    
                    if (searchConditions.Any())
                    {
                        whereClause = " WHERE " + string.Join(" OR ", searchConditions);
                    }
                }

                // Pagination
                var offset = (pageNumber - 1) * pageSize;
                var limitClause = $" LIMIT {pageSize} OFFSET {offset}";

                var finalQuery = baseQuery + whereClause + orderClause + limitClause;

                // Exécuter la requête
                using var dataCommand = connection.CreateCommand();
                dataCommand.CommandText = finalQuery;
                
                if (!string.IsNullOrWhiteSpace(searchFilter) && whereClause.Contains("@search"))
                {
                    var parameter = dataCommand.CreateParameter();
                    parameter.ParameterName = "@search";
                    parameter.Value = $"%{searchFilter}%";
                    dataCommand.Parameters.Add(parameter);
                }

                using var dataReader = await dataCommand.ExecuteReaderAsync();
                while (await dataReader.ReadAsync())
                {
                    var row = dataTable.NewRow();
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        row[i] = dataReader.IsDBNull(i) ? DBNull.Value : dataReader.GetValue(i);
                    }
                    dataTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération des données de la table {tableName}: {ex.Message}");
                // Retourner un DataTable vide plutôt que de lever l'exception
            }

            return dataTable;
        }

        private static bool ShouldExcludeColumn(string columnName, string columnType)
        {
            // Exclure les colonnes ID techniques (clés primaires GUID)
            if (columnName.Equals("Id", StringComparison.OrdinalIgnoreCase) && 
                columnType.Equals("TEXT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // Exclure les colonnes de métadonnées techniques
            var technicalColumns = new[]
            {
                "CreatedAt", "UpdatedAt", "IsDeleted", "CreatedBy", "ModifiedBy",
                "CreatedDate", "LastModifiedDate", "LastConnectivityTest",
                "LastConnectivityResult", "LastTestErrorMessages"
            };
            
            return technicalColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase);
        }

        private static Type GetNetTypeFromSqliteType(string sqliteType)
        {
            return sqliteType.ToUpper() switch
            {
                "INTEGER" => typeof(long),
                "REAL" => typeof(double),
                "TEXT" => typeof(string),
                "BLOB" => typeof(byte[]),
                _ when sqliteType.Contains("INT") => typeof(long),
                _ when sqliteType.Contains("CHAR") || sqliteType.Contains("TEXT") => typeof(string),
                _ when sqliteType.Contains("REAL") || sqliteType.Contains("FLOAT") || sqliteType.Contains("DOUBLE") => typeof(double),
                _ when sqliteType.Contains("DECIMAL") || sqliteType.Contains("NUMERIC") => typeof(decimal),
                _ when sqliteType.Contains("DATE") || sqliteType.Contains("TIME") => typeof(DateTime),
                _ when sqliteType.Contains("BOOL") => typeof(bool),
                _ => typeof(string) // Par défaut
            };
        }

        private static bool IsTextColumn(string sqliteType)
        {
            var upperType = sqliteType.ToUpper();
            return upperType.Contains("TEXT") || 
                   upperType.Contains("CHAR") || 
                   upperType.Contains("VARCHAR") ||
                   upperType == "TEXT";
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
                {
                    System.Diagnostics.Debug.WriteLine($"Fichier de base de données non trouvé : {dbPath}");
                    return false;
                }

                // Créer le dossier de sauvegarde s'il n'existe pas
                var backupDir = Path.GetDirectoryName(backupPath);
                if (!string.IsNullOrEmpty(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // Méthode 1: Utilisation de SQLite VACUUM INTO (plus sûre)
                try
                {
                    using var connection = new SqliteConnection(_connectionString);
                    await connection.OpenAsync();
                    
                    // Utiliser VACUUM INTO pour créer une copie propre
                    var command = new SqliteCommand($"VACUUM INTO '{backupPath.Replace("'", "''")}'", connection);
                    await command.ExecuteNonQueryAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"Sauvegarde créée avec VACUUM INTO : {backupPath}");
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"VACUUM INTO échoué : {ex.Message}");
                    
                    // Méthode 2: Copie de fichier classique en cas d'échec de VACUUM INTO
                    // Forcer la fermeture des connexions
                    _context.Database.CloseConnection();
                    
                    // Attendre un court instant
                    await Task.Delay(100);
                    
                    // Copier le fichier
                    using var sourceStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var destinationStream = new FileStream(backupPath, FileMode.Create, FileAccess.Write);
                    await sourceStream.CopyToAsync(destinationStream);
                    
                    System.Diagnostics.Debug.WriteLine($"Sauvegarde créée par copie de fichier : {backupPath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la sauvegarde : {ex.Message}");
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

        public async Task<bool> ReindexDatabaseAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                
                // Obtenir toutes les tables
                var tables = new List<string>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }
                
                // Réindexer chaque table
                foreach (var table in tables)
                {
                    using var reindexCommand = connection.CreateCommand();
                    reindexCommand.CommandText = $"REINDEX {table}";
                    await reindexCommand.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"Table réindexée : {table}");
                }
                
                System.Diagnostics.Debug.WriteLine($"Réindexation terminée pour {tables.Count} tables");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la réindexation : {ex.Message}");
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

        public async Task<bool> InsertRecordAsync(string tableName, Dictionary<string, object> values)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var columns = string.Join(", ", values.Keys);
                var parameters = string.Join(", ", values.Keys.Select(k => $"@{k}"));
                var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

                using var command = new SqliteCommand(sql, connection);
                foreach (var kvp in values)
                {
                    command.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                }

                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'insertion : {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateRecordAsync(string tableName, string id, Dictionary<string, object> values)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var setClause = string.Join(", ", values.Keys.Select(k => $"{k} = @{k}"));
                var sql = $"UPDATE {tableName} SET {setClause} WHERE Id = @Id";

                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);
                foreach (var kvp in values)
                {
                    command.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                }

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour : {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteRecordAsync(string tableName, string id)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = $"DELETE FROM {tableName} WHERE Id = @Id";
                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la suppression : {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetTableColumnsAsync(string tableName)
        {
            var columns = new List<string>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var sql = $"PRAGMA table_info({tableName})";
                using var command = new SqliteCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var columnName = reader.GetString("name");
                    var columnType = reader.GetString("type");
                    if (!ShouldExcludeColumn(columnName, columnType))
                    {
                        columns.Add(columnName);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération des colonnes : {ex.Message}");
            }
            return columns;
        }

        public async Task<bool> UpdateConnectionStringAsync(string newDatabasePath)
        {
            try
            {
                // Valider le chemin
                if (string.IsNullOrWhiteSpace(newDatabasePath))
                {
                    throw new ArgumentException("Le chemin de la base de données ne peut pas être vide.", nameof(newDatabasePath));
                }

                // Créer le répertoire s'il n'existe pas
                var directory = Path.GetDirectoryName(newDatabasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Construire la nouvelle chaîne de connexion
                var newConnectionString = $"Data Source={newDatabasePath}";
                
                // Mettre à jour la chaîne de connexion interne
                _connectionString = newConnectionString;
                
                // Mettre à jour la chaîne de connexion dans le contexte Entity Framework
                _context.Database.SetConnectionString(newConnectionString);
                
                // S'assurer que la base de données est créée et migrée
                await _context.Database.EnsureCreatedAsync();
                
                System.Diagnostics.Debug.WriteLine($"Connexion mise à jour vers : {newDatabasePath}");
                System.Diagnostics.Debug.WriteLine($"Nouvelle chaîne de connexion : {newConnectionString}");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la mise à jour de la connexion : {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateAutomaticBackupAsync(string backupDirectory, bool compressBackup = false)
        {
            try
            {
                // Générer un nom de fichier avec timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"FNEV4_AutoBackup_{timestamp}.db";
                var backupPath = Path.Combine(backupDirectory, backupFileName);

                // Créer le répertoire s'il n'existe pas
                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                // Créer la sauvegarde
                var backupSuccess = await BackupDatabaseAsync(backupPath);
                
                if (backupSuccess && compressBackup)
                {
                    // Compresser la sauvegarde (implémentation future si nécessaire)
                    // Pour l'instant, juste marquer comme non compressé
                    System.Diagnostics.Debug.WriteLine($"Compression demandée mais non implémentée pour : {backupPath}");
                }

                if (backupSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"Sauvegarde automatique créée : {backupPath}");
                    
                    // Nettoyer les anciennes sauvegardes (garder seulement les 10 plus récentes)
                    await CleanupOldBackups(backupDirectory, 10);
                }

                return backupSuccess;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la sauvegarde automatique : {ex.Message}");
                return false;
            }
        }

        private async Task CleanupOldBackups(string backupDirectory, int maxBackups)
        {
            try
            {
                if (!Directory.Exists(backupDirectory))
                    return;

                var backupFiles = Directory.GetFiles(backupDirectory, "FNEV4_AutoBackup_*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                // Supprimer les anciens fichiers si on dépasse la limite
                if (backupFiles.Count > maxBackups)
                {
                    var filesToDelete = backupFiles.Skip(maxBackups);
                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            file.Delete();
                            System.Diagnostics.Debug.WriteLine($"Ancienne sauvegarde supprimée : {file.Name}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Erreur lors de la suppression de {file.Name} : {ex.Message}");
                        }
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du nettoyage des sauvegardes : {ex.Message}");
            }
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }

        public async Task<DatabaseMonitoringInfo> GetDatabaseMonitoringInfoAsync()
        {
            try
            {
                var dbPath = GetDatabasePath();
                var fileInfo = new FileInfo(dbPath);
                var sizeMB = fileInfo.Exists ? Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2) : 0;

                var tableCount = await GetTableCountAsync();

                return new DatabaseMonitoringInfo
                {
                    SizeMB = sizeMB,
                    TableCount = tableCount,
                    LastCheck = DateTime.Now,
                    SizeAlertTriggered = false,
                    TableCountAlertTriggered = false,
                    AlertMessages = new List<string>()
                };
            }
            catch (Exception ex)
            {
                return new DatabaseMonitoringInfo
                {
                    SizeMB = 0,
                    TableCount = 0,
                    LastCheck = DateTime.Now,
                    SizeAlertTriggered = false,
                    TableCountAlertTriggered = false,
                    AlertMessages = new List<string> { $"Erreur lors de la surveillance: {ex.Message}" }
                };
            }
        }

        public async Task<bool> CheckDatabaseAlertsAsync(double maxSizeMB, int maxTableCount, bool emailAlertsEnabled, string alertEmailAddress)
        {
            try
            {
                var monitoringInfo = await GetDatabaseMonitoringInfoAsync();
                var alertMessages = new List<string>();

                // Vérification de la taille
                if (monitoringInfo.SizeMB > maxSizeMB)
                {
                    monitoringInfo.SizeAlertTriggered = true;
                    alertMessages.Add($"🚨 ALERTE: La base de données ({monitoringInfo.SizeMB:F2} MB) dépasse la taille maximale autorisée ({maxSizeMB} MB)");
                }

                // Vérification du nombre de tables
                if (monitoringInfo.TableCount > maxTableCount)
                {
                    monitoringInfo.TableCountAlertTriggered = true;
                    alertMessages.Add($"🚨 ALERTE: Le nombre de tables ({monitoringInfo.TableCount}) dépasse le maximum autorisé ({maxTableCount})");
                }

                monitoringInfo.AlertMessages = alertMessages;

                // Envoi d'email si activé et alertes détectées
                if (emailAlertsEnabled && !string.IsNullOrWhiteSpace(alertEmailAddress) && alertMessages.Any())
                {
                    var subject = $"FNEV4 - Alertes Base de Données - {DateTime.Now:dd/MM/yyyy HH:mm}";
                    var message = $"Alertes détectées sur la base de données FNEV4:\n\n" +
                                 string.Join("\n", alertMessages) + "\n\n" +
                                 $"Vérification effectuée le: {monitoringInfo.LastCheck:dd/MM/yyyy HH:mm:ss}\n" +
                                 $"Chemin de la base: {GetDatabasePath()}";

                    await SendEmailAlertAsync(alertEmailAddress, subject, message);
                }

                return alertMessages.Any();
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas faire échouer l'application
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification des alertes: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailAlertAsync(string emailAddress, string subject, string message)
        {
            try
            {
                // Pour une implémentation réelle, vous devriez utiliser un service d'email comme SendGrid, SMTP, etc.
                // Ici, nous simulons l'envoi en sauvegardant dans un fichier log
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "email-alerts.log");
                var logDir = Path.GetDirectoryName(logPath);
                
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir!);
                }

                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] EMAIL ALERT\n" +
                              $"To: {emailAddress}\n" +
                              $"Subject: {subject}\n" +
                              $"Message:\n{message}\n" +
                              new string('-', 80) + "\n\n";

                await File.AppendAllTextAsync(logPath, logEntry);

                // Dans un vrai système, vous implementeriez ici l'envoi réel:
                /*
                using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential("votre-email@gmail.com", "votre-mot-de-passe");
                    
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress("votre-email@gmail.com"),
                        Subject = subject,
                        Body = message,
                        IsBodyHtml = false
                    };
                    mailMessage.To.Add(emailAddress);
                    
                    await smtpClient.SendMailAsync(mailMessage);
                }
                */

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'envoi d'email: {ex.Message}");
                return false;
            }
        }

        private async Task<int> GetTableCountAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch
            {
                return 0;
            }
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

    public class DatabaseMonitoringInfo
    {
        public double SizeMB { get; set; }
        public int TableCount { get; set; }
        public DateTime LastCheck { get; set; }
        public bool SizeAlertTriggered { get; set; }
        public bool TableCountAlertTriggered { get; set; }
        public List<string> AlertMessages { get; set; } = new List<string>();
    }

    public class TableInfo
    {
        public string Name { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public string Size { get; set; } = "0 KB";
        public string LastModified { get; set; } = string.Empty;
    }
}
