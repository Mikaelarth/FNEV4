using System.Windows.Controls;

namespace FNEV4.Presentation.Views.Dashboard
{
    /// <summary>
    /// Interaction logic for DashboardActionsRapidesView.xaml
    /// </summary>
    public partial class DashboardActionsRapidesView : UserControl
    {
        public DashboardActionsRapidesView()
        {
            InitializeComponent();
        }

        public DashboardActionsRapidesView(ViewModels.Dashboard.DashboardActionsRapidesViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
