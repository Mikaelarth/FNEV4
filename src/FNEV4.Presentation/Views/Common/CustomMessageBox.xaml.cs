using System;
using System.Windows;
using System.Windows.Media;
using FNEV4.Core.Models.ImportTraitement;
using MaterialDesignThemes.Wpf;

namespace FNEV4.Presentation.Views.Common
{
    public partial class CustomMessageBox : Window
    {
        public enum MessageBoxType
        {
            Information,
            Warning,
            Error,
            Success
        }

        public enum MessageBoxResult
        {
            OK,
            Details
        }

        public MessageBoxResult Result { get; private set; } = MessageBoxResult.OK;
        public ImportDetailedReport? DetailedReport { get; private set; }

        public CustomMessageBox()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            Close();
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Details;
            ShowDetailedReport();
        }

        private void ShowDetailedReport()
        {
            if (DetailedReport == null) return;

            try
            {
                // Créer et afficher la boîte de dialogue du rapport
                var reportDialog = new Views.ImportTraitement.ImportReportDialog();
                var reportViewModel = new ViewModels.ImportTraitement.ImportReportDialogViewModel(DetailedReport);
                
                reportDialog.DataContext = reportViewModel;
                
                // Créer une fenêtre pour contenir le UserControl
                var dialogWindow = new Window
                {
                    Title = "Rapport d'Import Détaillé",
                    Content = reportDialog,
                    Width = 900,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                // Gérer l'événement de fermeture
                reportViewModel.RequestClose += (s, e) => dialogWindow.Close();

                dialogWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur lors de l'affichage du rapport :\n{ex.Message}", 
                              "Erreur", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        public static MessageBoxResult Show(string message, string title = "Information", 
            MessageBoxType type = MessageBoxType.Information, 
            ImportDetailedReport? detailedReport = null)
        {
            var messageBox = new CustomMessageBox();
            
            // DEBUG: Vérifier si le rapport détaillé est reçu
            System.Diagnostics.Debug.WriteLine($"🔍 DEBUG CustomMessageBox - DetailedReport reçu: {detailedReport != null}");
            if (detailedReport != null)
            {
                System.Diagnostics.Debug.WriteLine($"🔍 DEBUG - FacturesEchouees: {detailedReport.FacturesEchouees}");
                System.Diagnostics.Debug.WriteLine($"🔍 DEBUG - FailedInvoices.Count: {detailedReport.FailedInvoices.Count}");
            }
            
            // Configuration du titre et du message
            messageBox.Title = title;
            messageBox.TitleText.Text = title;
            messageBox.MessageText.Text = message;
            messageBox.DetailedReport = detailedReport;

            // Configuration de l'icône et des couleurs selon le type
            switch (type)
            {
                case MessageBoxType.Information:
                    messageBox.MessageIcon.Kind = PackIconKind.Information;
                    messageBox.MessageIcon.Foreground = new SolidColorBrush(Colors.DodgerBlue);
                    break;
                case MessageBoxType.Warning:
                    messageBox.MessageIcon.Kind = PackIconKind.Warning;
                    messageBox.MessageIcon.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
                case MessageBoxType.Error:
                    messageBox.MessageIcon.Kind = PackIconKind.Error;
                    messageBox.MessageIcon.Foreground = new SolidColorBrush(Colors.Red);
                    break;
                case MessageBoxType.Success:
                    messageBox.MessageIcon.Kind = PackIconKind.CheckCircle;
                    messageBox.MessageIcon.Foreground = new SolidColorBrush(Colors.Green);
                    break;
            }

            // Afficher le bouton "Consulter les détails" si un rapport est disponible
            if (detailedReport != null)
            {
                System.Diagnostics.Debug.WriteLine($"🔍 DEBUG - Affichage du bouton détails (rapport non null)");
                messageBox.DetailsButton.Visibility = Visibility.Visible;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"🔍 DEBUG - Pas de bouton détails (rapport null)");
            }

            // Définir le propriétaire si possible
            if (System.Windows.Application.Current?.MainWindow != null)
            {
                messageBox.Owner = System.Windows.Application.Current.MainWindow;
            }

            messageBox.ShowDialog();
            return messageBox.Result;
        }
    }
}
