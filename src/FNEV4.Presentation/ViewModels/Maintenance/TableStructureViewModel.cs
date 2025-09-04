using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Infrastructure.Services;

namespace FNEV4.Presentation.ViewModels.Maintenance
{
    public class TableStructureViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private string _tableName = string.Empty;
        private int _columnCount;
        private string _createTableScript = string.Empty;

        public TableStructureViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            GenerateCreateScriptCommand = new RelayCommand(GenerateCreateScript);
            CopySqlCommand = new RelayCommand(CopySql);
        }

        public string TableName
        {
            get => _tableName;
            set => SetProperty(ref _tableName, value);
        }

        public int ColumnCount
        {
            get => _columnCount;
            set => SetProperty(ref _columnCount, value);
        }

        public string CreateTableScript
        {
            get => _createTableScript;
            set => SetProperty(ref _createTableScript, value);
        }

        public ObservableCollection<ColumnInfo> Columns { get; } = new();
        public ObservableCollection<IndexInfo> Indexes { get; } = new();
        public ObservableCollection<ConstraintInfo> Constraints { get; } = new();

        public ICommand GenerateCreateScriptCommand { get; }
        public ICommand CopySqlCommand { get; }

        public async Task LoadTableStructureAsync(string tableName)
        {
            TableName = tableName;
            await LoadColumnsAsync();
            await LoadIndexesAsync();
            await LoadConstraintsAsync();
            GenerateCreateScript();
        }

        private async Task LoadColumnsAsync()
        {
            try
            {
                Columns.Clear();
                
                // Charger les informations des colonnes depuis la base de données
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(_databaseService.GetConnectionString());
                await connection.OpenAsync();

                var sql = $"PRAGMA table_info({TableName})";
                using var command = new Microsoft.Data.Sqlite.SqliteCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var column = new ColumnInfo
                    {
                        Name = reader.GetString(1), // name
                        DataType = reader.GetString(2), // type
                        IsNullable = reader.GetInt32(3) == 0, // notnull
                        IsPrimaryKey = reader.GetInt32(5) == 1, // pk
                        DefaultValue = reader.IsDBNull(4) ? "" : reader.GetString(4) // dflt_value
                    };
                    
                    Columns.Add(column);
                }

                ColumnCount = Columns.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des colonnes : {ex.Message}");
            }
        }

        private async Task LoadIndexesAsync()
        {
            try
            {
                Indexes.Clear();
                
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(_databaseService.GetConnectionString());
                await connection.OpenAsync();

                // Charger la liste des index
                var sql = $"PRAGMA index_list({TableName})";
                using var command = new Microsoft.Data.Sqlite.SqliteCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var indexName = reader.GetString(1); // name
                    var isUnique = reader.GetInt32(2) == 1; // unique

                    // Obtenir les colonnes de l'index
                    var indexInfoSql = $"PRAGMA index_info({indexName})";
                    using var indexCommand = new Microsoft.Data.Sqlite.SqliteCommand(indexInfoSql, connection);
                    using var indexReader = await indexCommand.ExecuteReaderAsync();

                    var columns = new List<string>();
                    while (await indexReader.ReadAsync())
                    {
                        columns.Add(indexReader.GetString(2)); // name
                    }

                    var index = new IndexInfo
                    {
                        Name = indexName,
                        IsUnique = isUnique,
                        Columns = string.Join(", ", columns)
                    };
                    
                    Indexes.Add(index);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des index : {ex.Message}");
            }
        }

        private async Task LoadConstraintsAsync()
        {
            try
            {
                Constraints.Clear();
                
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(_databaseService.GetConnectionString());
                await connection.OpenAsync();

                // Charger les contraintes de clé étrangère
                var sql = $"PRAGMA foreign_key_list({TableName})";
                using var command = new Microsoft.Data.Sqlite.SqliteCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var constraint = new ConstraintInfo
                    {
                        Name = $"FK_{TableName}_{reader.GetString(3)}", // from
                        Type = "FOREIGN KEY",
                        Definition = $"FOREIGN KEY ({reader.GetString(3)}) REFERENCES {reader.GetString(2)}({reader.GetString(4)})" // from, table, to
                    };
                    
                    Constraints.Add(constraint);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des contraintes : {ex.Message}");
            }
        }

        private void GenerateCreateScript()
        {
            var script = $"CREATE TABLE {TableName} (\n";
            
            for (int i = 0; i < Columns.Count; i++)
            {
                var column = Columns[i];
                script += $"  {column.Name} {column.DataType}";
                
                if (column.IsPrimaryKey)
                    script += " PRIMARY KEY";
                    
                if (!column.IsNullable)
                    script += " NOT NULL";
                    
                if (!string.IsNullOrEmpty(column.DefaultValue))
                    script += $" DEFAULT {column.DefaultValue}";
                
                if (i < Columns.Count - 1)
                    script += ",";
                    
                script += "\n";
            }
            
            script += ");";
            CreateTableScript = script;
        }

        private void CopySql()
        {
            if (!string.IsNullOrEmpty(CreateTableScript))
            {
                System.Windows.Clipboard.SetText(CreateTableScript);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class ColumnInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class IndexInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool IsUnique { get; set; }
        public string Columns { get; set; } = string.Empty;
    }

    public class ConstraintInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
    }
}
