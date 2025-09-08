using System.ComponentModel;
using System.Windows.Input;

namespace FNEV4.Presentation.ViewModels.Maintenance
{
    /// <summary>
    /// ViewModel ultra simple pour tester sans injection de dépendances
    /// </summary>
    public class BaseDonneesViewModelSimple : INotifyPropertyChanged
    {
        private string _sqlResults = "ViewModel simple connecté ✓\nPas d'injection de dépendances";

        public string SqlResults
        {
            get => _sqlResults;
            set
            {
                _sqlResults = value;
                OnPropertyChanged(nameof(SqlResults));
            }
        }

        public string DatabasePath => "C:\\wamp64\\www\\FNEV4\\Data\\FNEV4.db";
        public string DatabaseSize => "Test Mode";
        public string ConnectionStatus => "Test Simple";

        public ICommand TestCommand { get; }
        public ICommand InitializeDatabaseCommand { get; }

        public BaseDonneesViewModelSimple()
        {
            TestCommand = new SimpleCommand(() =>
            {
                SqlResults = $"🎉 BOUTON TEST FONCTIONNE ! 🎉\n" +
                           $"Timestamp: {System.DateTime.Now:HH:mm:ss}\n" +
                           $"DataContext connecté sans injection ✓";
                
                System.Windows.MessageBox.Show("Test simple réussi !", "Success");
            });

            InitializeDatabaseCommand = new SimpleCommand(() =>
            {
                SqlResults = "🔧 INITIALISATION SIMPLE 🔧\n" +
                           "Création manuelle de la base...\n" +
                           "Sans injection de dépendances";
                
                try
                {
                    CreateSimpleDatabase();
                    SqlResults += "\n✅ Base créée avec succès !";
                }
                catch (System.Exception ex)
                {
                    SqlResults += $"\n❌ Erreur: {ex.Message}";
                }
            });
        }

        private void CreateSimpleDatabase()
        {
            var dbPath = "Data\\FNEV4.db";
            
            // Créer le dossier si nécessaire
            var directory = System.IO.Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Créer une base SQLite simple
            var connectionString = $"Data Source={dbPath}";
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS TestTable (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT,
                    CreatedAt TEXT
                );
                INSERT OR REPLACE INTO TestTable (Id, Name, CreatedAt) 
                VALUES (1, 'Test Simple', datetime('now'));
            ";
            command.ExecuteNonQuery();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Commande simple sans dépendances
    /// </summary>
    public class SimpleCommand : ICommand
    {
        private readonly System.Action _execute;

        public SimpleCommand(System.Action execute)
        {
            _execute = execute ?? throw new System.ArgumentNullException(nameof(execute));
        }

        public event System.EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}
