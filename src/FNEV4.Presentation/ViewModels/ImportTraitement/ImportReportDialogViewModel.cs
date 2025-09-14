using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FNEV4.Core.Models.ImportTraitement;
using System;
using System.Linq;

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
        
        partial void OnDetailedReportChanged(ImportDetailedReport value)
        {
            // Notifier les changements des propriétés calculées
            OnPropertyChanged(nameof(FacturesImportees));
            OnPropertyChanged(nameof(FacturesEchouees));
            OnPropertyChanged(nameof(TotalFactures));
            OnPropertyChanged(nameof(DureeTraitement));
            OnPropertyChanged(nameof(TauxReussite));
            OnPropertyChanged(nameof(TauxEchec));
            OnPropertyChanged(nameof(NombreFacturesEchoueesAffichees));
            OnPropertyChanged(nameof(HasFailures));
            OnPropertyChanged(nameof(HasGlobalErrors));
            OnPropertyChanged(nameof(HasWarnings));
            OnPropertyChanged(nameof(IsSuccessfulImport));
        }
        
        // Propriétés pour l'interface
        public bool HasFailures => DetailedReport.FailedInvoices.Any();
        public bool HasGlobalErrors => DetailedReport.GlobalErrors.Any();
        public bool HasWarnings => DetailedReport.GlobalWarnings.Any();
        
        public string FailedInvoicesTabHeader => $"❌ Échecs ({DetailedReport.FailedInvoices.Count})";
        public string GlobalErrorsTabHeader => $"⚠️ Erreurs système ({DetailedReport.GlobalErrors.Count})";
        
        public bool IsSuccessfulImport => DetailedReport.IsSuccess && !HasFailures && !HasGlobalErrors;
        
        // Propriétés pour le résumé dans l'en-tête (observables)
        public int FacturesImportees => DetailedReport?.FacturesImportees ?? 0;
        public int FacturesEchouees => DetailedReport?.FacturesEchouees ?? 0;
        public int TotalFactures => DetailedReport?.TotalFactures ?? 0;
        public TimeSpan DureeTraitement => DetailedReport?.DureeTraitement ?? TimeSpan.Zero;
        public decimal TauxReussite => DetailedReport?.TauxReussite ?? 0;
        public decimal TauxEchec => DetailedReport?.TauxEchec ?? 0;
        
        // Propriétés calculées pour l'affichage
        public int NombreFacturesEchoueesAffichees => DetailedReport?.FailedInvoices?.Count ?? 0;

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