using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Models.ImportTraitement;
using System;

namespace FNEV4.Presentation.ViewModels.ImportTraitement
{
    public partial class ImportReportDialogViewModel : ObservableObject
    {
        private readonly ImportDetailedReport _report;
        
        public event EventHandler? RequestClose;

        [ObservableProperty]
        private ImportDetailedReport _detailedReport;

        public ImportReportDialogViewModel(ImportDetailedReport report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
            DetailedReport = report;
        }

        [RelayCommand]
        private void Close()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void ExportToExcel()
        {
            // TODO: Implémenter l'export Excel des détails si nécessaire
            System.Windows.MessageBox.Show("Export Excel à implémenter si nécessaire", "Info", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}