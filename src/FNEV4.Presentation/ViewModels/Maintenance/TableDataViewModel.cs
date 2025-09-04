using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Infrastructure.Services;

namespace FNEV4.Presentation.ViewModels.Maintenance
{
    public class TableDataViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private string _tableName = string.Empty;
        private int _rowCount;
        private string _searchText = string.Empty;
        private int _pageSize = 50;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private DataTable? _tableData;
        private Timer? _searchTimer;
        private bool _isLoading = false;
        private DataRowView? _selectedRecord;

        public TableDataViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            RefreshDataCommand = new RelayCommand(RefreshData);
            ExportCsvCommand = new RelayCommand(ExportToCsv);
            PreviousPageCommand = new RelayCommand(PreviousPage, CanPreviousPage);
            NextPageCommand = new RelayCommand(NextPage, CanNextPage);
            AddRecordCommand = new RelayCommand(AddRecord);
            EditRecordCommand = new RelayCommand(EditRecord, CanEditRecord);
            DeleteRecordCommand = new RelayCommand(DeleteRecord, CanDeleteRecord);
        }

        public string TableName
        {
            get => _tableName;
            set => SetProperty(ref _tableName, value);
        }

        public int RowCount
        {
            get => _rowCount;
            set => SetProperty(ref _rowCount, value);
        }

        public string SearchText
        {
            get => _searchText;
            set 
            { 
                if (SetProperty(ref _searchText, value))
                {
                    // Débounce la recherche pour éviter trop de requêtes
                    _searchTimer?.Dispose();
                    _searchTimer = new Timer(OnSearchTimerElapsed, null, 500, Timeout.Infinite);
                }
            }
        }

        private void OnSearchTimerElapsed(object? state)
        {
            // Exécuter sur le thread UI
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                CurrentPage = 1; // Réinitialiser à la première page
                RefreshData();
            });
        }

        public int PageSize
        {
            get => _pageSize;
            set 
            { 
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1; // Réinitialiser à la première page
                    RefreshData(); // Actualiser les données
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public DataTable? TableData
        {
            get => _tableData;
            set => SetProperty(ref _tableData, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public DataRowView? SelectedRecord
        {
            get => _selectedRecord;
            set => SetProperty(ref _selectedRecord, value);
        }

        public string PaginationInfo => $"Lignes {((CurrentPage - 1) * PageSize) + 1} à {Math.Min(CurrentPage * PageSize, RowCount)} sur {RowCount}";

        public string CurrentPageInfo => $"Page {CurrentPage} / {TotalPages}";

        public ICommand RefreshDataCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand AddRecordCommand { get; }
        public ICommand EditRecordCommand { get; }
        public ICommand DeleteRecordCommand { get; }

        public void LoadTableData(string tableName)
        {
            TableName = tableName;
            CurrentPage = 1; // Réinitialiser à la première page
            RefreshData();
        }

        private async void RefreshData()
        {
            try
            {
                if (string.IsNullOrEmpty(TableName)) return;

                IsLoading = true;

                // Charger les vraies données depuis la base
                var realData = await _databaseService.GetTableDataAsync(TableName, PageSize, CurrentPage, SearchText);
                
                TableData = realData;
                
                // Pour la pagination, nous utilisons le nombre de lignes retournées
                // Dans une vraie implémentation, il faudrait une méthode séparée pour compter le total
                if (realData.Rows.Count == 0)
                {
                    RowCount = 0;
                    TotalPages = 1;
                }
                else
                {
                    // Si on a le nombre max de lignes, il pourrait y avoir d'autres pages
                    RowCount = realData.Rows.Count;
                    TotalPages = realData.Rows.Count < PageSize ? CurrentPage : CurrentPage + 1;
                }
                
                OnPropertyChanged(nameof(PaginationInfo));
                OnPropertyChanged(nameof(CurrentPageInfo));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des données: {ex.Message}");
                // En cas d'erreur, créer un DataTable vide
                TableData = new DataTable();
                RowCount = 0;
                TotalPages = 1;
                OnPropertyChanged(nameof(PaginationInfo));
                OnPropertyChanged(nameof(CurrentPageInfo));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExportToCsv()
        {
            if (TableData == null || TableData.Rows.Count == 0)
                return;
                
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Fichiers CSV (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"{TableName}_export.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    ExportDataTableToCsv(TableData, dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'export CSV: {ex.Message}");
            }
        }

        private void ExportDataTableToCsv(DataTable dataTable, string filePath)
        {
            using var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8);
            
            // En-têtes
            var headers = dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
            writer.WriteLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            // Données
            foreach (DataRow row in dataTable.Rows)
            {
                var fields = row.ItemArray.Select(field => $"\"{field?.ToString()?.Replace("\"", "\"\"")}\"");
                writer.WriteLine(string.Join(",", fields));
            }
        }

        private void PreviousPage()
        {
            if (CanPreviousPage())
            {
                CurrentPage--;
                RefreshData();
                OnPropertyChanged(nameof(PaginationInfo));
                OnPropertyChanged(nameof(CurrentPageInfo));
            }
        }

        private bool CanPreviousPage() => CurrentPage > 1;

        private void NextPage()
        {
            if (CanNextPage())
            {
                CurrentPage++;
                RefreshData();
                OnPropertyChanged(nameof(PaginationInfo));
                OnPropertyChanged(nameof(CurrentPageInfo));
            }
        }

        private bool CanNextPage() => CurrentPage < TotalPages;

        private void AddRecord()
        {
            try
            {
                if (TableData == null) return;

                // Pour l'instant, utiliser un MessageBox simple - à remplacer par un vrai dialog
                var result = MessageBox.Show(
                    "Fonctionnalité d'ajout d'enregistrement en cours de développement.\n\nVoulez-vous actualiser les données ?",
                    "Ajouter un enregistrement",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    RefreshData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ajout : {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditRecord()
        {
            try
            {
                if (SelectedRecord?.Row == null || TableData == null) return;

                // Pour l'instant, afficher les détails de l'enregistrement
                var record = SelectedRecord.Row;
                var details = string.Join("\n", TableData.Columns.Cast<DataColumn>()
                    .Where(col => !ShouldExcludeColumn(col.ColumnName))
                    .Select(col => $"{col.ColumnName}: {record[col.ColumnName]}"));

                var result = MessageBox.Show(
                    $"Détails de l'enregistrement :\n\n{details}\n\nFonctionnalité de modification en cours de développement.\n\nVoulez-vous actualiser les données ?",
                    "Modifier l'enregistrement",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    RefreshData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la modification : {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ShouldExcludeColumn(string columnName)
        {
            var excludedColumns = new[] { "Id", "CreatedAt", "UpdatedAt", "IsDeleted", "CreatedBy", "UpdatedBy" };
            return excludedColumns.Any(col => columnName.Equals(col, StringComparison.OrdinalIgnoreCase));
        }

        private bool CanEditRecord() => SelectedRecord != null;

        private async void DeleteRecord()
        {
            try
            {
                if (SelectedRecord?.Row == null) return;

                var result = MessageBox.Show(
                    "Êtes-vous sûr de vouloir supprimer cet enregistrement ?",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    IsLoading = true;
                    
                    // Obtenir l'ID de l'enregistrement (première colonne généralement)
                    var record = SelectedRecord.Row;
                    var idValue = record[0]?.ToString();
                    
                    if (!string.IsNullOrEmpty(idValue))
                    {
                        await _databaseService.DeleteRecordAsync(TableName, idValue);
                        RefreshData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression : {ex.Message}", "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanDeleteRecord() => SelectedRecord != null;

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

        public void Dispose()
        {
            _searchTimer?.Dispose();
        }
    }
}
