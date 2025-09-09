using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Threading.Tasks;
using System;
using FNEV4.Core.Models.ImportTraitement;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FNEV4.Presentation.ViewModels.ImportTraitement
{
    /// <summary>
    /// ViewModel pour la fenêtre d'aperçu des factures Sage 100
    /// </summary>
    public partial class Sage100PreviewViewModel : ObservableObject
    {
        private readonly Sage100ImportViewModel _parentViewModel;

        #region Properties

        public ObservableCollection<Sage100FacturePreview> InvoicesPreviews { get; set; }

        [ObservableProperty]
        private int _totalFiles;

        [ObservableProperty]
        private int _validFiles;

        [ObservableProperty]
        private int _invalidFiles;

        [ObservableProperty]
        private int _totalInvoices;

        private List<string> _errors = new();
        public List<string> Errors
        {
            get => _errors;
            set
            {
                _errors = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ErrorsCount));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        public int ErrorsCount => Errors?.Count ?? 0;
        public bool HasErrors => ErrorsCount > 0;

        #endregion

        #region Constructor

        public Sage100PreviewViewModel(Sage100ImportViewModel parentViewModel = null)
        {
            _parentViewModel = parentViewModel;
            InvoicesPreviews = new ObservableCollection<Sage100FacturePreview>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Charge les données d'aperçu
        /// </summary>
        public void LoadPreviewData(
            List<Sage100FacturePreview> invoices, 
            int totalFiles, 
            int validFiles, 
            int invalidFiles, 
            List<string> errors = null)
        {
            // Mise à jour des statistiques
            TotalFiles = totalFiles;
            ValidFiles = validFiles;
            InvalidFiles = invalidFiles;
            TotalInvoices = invoices?.Count ?? 0;
            Errors = errors ?? new List<string>();

            // Chargement des factures
            InvoicesPreviews.Clear();
            if (invoices != null)
            {
                foreach (var invoice in invoices)
                {
                    InvoicesPreviews.Add(invoice);
                }
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void ExportPreview()
        {
            // TODO: Implémenter l'export Excel de l'aperçu
            // Pour l'instant, on peut afficher un message
            System.Windows.MessageBox.Show(
                "Fonctionnalité d'export Excel à implémenter", 
                "Export", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task ImportInvoices()
        {
            try
            {
                if (_parentViewModel == null)
                {
                    System.Windows.MessageBox.Show(
                        "Erreur: Impossible d'accéder au processus d'import.", 
                        "Erreur", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                    return;
                }

                var validInvoices = InvoicesPreviews.Where(f => f.EstValide).Count();
                var invalidInvoices = InvoicesPreviews.Where(f => !f.EstValide).Count();

                var confirmMessage = $"📋 CONFIRMATION D'IMPORT\n\n" +
                                   $"✅ Factures valides à importer: {validInvoices}\n" +
                                   $"❌ Factures avec erreurs (ignorées): {invalidInvoices}\n\n" +
                                   $"Voulez-vous procéder à l'import des {validInvoices} factures valides ?";

                var result = System.Windows.MessageBox.Show(
                    confirmMessage,
                    "Confirmation d'import",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // Fermer la fenêtre d'aperçu
                    var currentWindow = System.Windows.Application.Current.Windows
                        .OfType<Views.ImportTraitement.Sage100PreviewWindow>()
                        .FirstOrDefault();
                    
                    // Lancer l'import via le ViewModel parent
                    await _parentViewModel.ProcessImportFromPreview();
                    
                    currentWindow?.Close();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de l'import :\n{ex.Message}", 
                    "Erreur", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExportPreview => InvoicesPreviews?.Count > 0;
        private bool CanImportInvoices => InvoicesPreviews?.Any(f => f.EstValide) == true;

        #endregion
    }
}
