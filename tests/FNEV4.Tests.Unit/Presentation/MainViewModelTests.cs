using Xunit;
using FluentAssertions;
using FNEV4.Presentation.ViewModels;

namespace FNEV4.Tests.Unit.Presentation
{
    /// <summary>
    /// Tests unitaires pour MainViewModel
    /// Validation de la navigation et des propriétés
    /// </summary>
    public class MainViewModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var viewModel = new MainViewModel();

            // Assert
            viewModel.ApplicationTitle.Should().Be("FNEV4 - Application FNE Desktop");
            viewModel.ApplicationVersion.Should().Be("v1.0.0");
            viewModel.CompanyName.Should().Be("Non configuré");
            viewModel.CurrentModuleName.Should().Be("Dashboard");
            viewModel.IsMenuExpanded.Should().BeTrue();
        }

        [Fact]
        public void NavigateToDashboard_ShouldUpdateCurrentModuleName()
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act
            viewModel.NavigateToDashboardCommand.Execute(null);

            // Assert
            viewModel.CurrentModuleName.Should().Be("Dashboard - Vue d'ensemble");
        }

        [Fact]
        public void NavigateToDashboardActions_ShouldUpdateCurrentModuleName()
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act
            viewModel.NavigateToDashboardActionsCommand.Execute(null);

            // Assert
            viewModel.CurrentModuleName.Should().Be("Dashboard - Actions rapides");
        }

        [Fact]
        public void NavigateToImportFichiers_ShouldUpdateCurrentModuleName()
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act
            viewModel.NavigateToImportFichiersCommand.Execute(null);

            // Assert
            viewModel.CurrentModuleName.Should().Be("Import - Import de fichiers");
        }



        [Fact]
        public void NavigateToCertificationManuelle_ShouldUpdateCurrentModuleName()
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act
            viewModel.NavigateToCertificationManuelleCommand.Execute(null);

            // Assert
            viewModel.CurrentModuleName.Should().Be("Certification - Certification manuelle");
        }

        [Fact]
        public void ToggleMenu_ShouldChangeIsMenuExpanded()
        {
            // Arrange
            var viewModel = new MainViewModel();
            var initialState = viewModel.IsMenuExpanded;

            // Act
            viewModel.ToggleMenuCommand.Execute(null);

            // Assert
            viewModel.IsMenuExpanded.Should().Be(!initialState);
        }

        [Fact]
        public void ToggleMenu_MultipleTimes_ShouldToggleCorrectly()
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act & Assert
            viewModel.IsMenuExpanded.Should().BeTrue(); // État initial

            viewModel.ToggleMenuCommand.Execute(null);
            viewModel.IsMenuExpanded.Should().BeFalse();

            viewModel.ToggleMenuCommand.Execute(null);
            viewModel.IsMenuExpanded.Should().BeTrue();
        }

        [Fact]
        public async Task RefreshConnectionStatus_ShouldUpdateConnectionStatus()
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act
            await viewModel.RefreshConnectionStatusCommand.ExecuteAsync(null);

            // Assert
            viewModel.ConnectionStatus.Should().Be("Connecté");
            viewModel.StickerBalance.Should().Be("1,245");
        }

        [Theory]
        [InlineData("NavigateToListeClientsCommand", "Clients - Liste des clients")]
        [InlineData("NavigateToRapportsStandardsCommand", "Rapports - Rapports standards")]
        [InlineData("NavigateToEntrepriseConfigCommand", "Configuration - Entreprise")]
        [InlineData("NavigateToLogsDiagnosticsCommand", "Maintenance - Logs & Diagnostics")]
        [InlineData("NavigateToDocumentationCommand", "Aide - Documentation")]
        public void NavigationCommands_ShouldUpdateCurrentModuleNameCorrectly(string commandName, string expectedModuleName)
        {
            // Arrange
            var viewModel = new MainViewModel();
            var commandProperty = viewModel.GetType().GetProperty(commandName);
            
            // Act
            if (commandProperty?.GetValue(viewModel) is System.Windows.Input.ICommand command)
            {
                command.Execute(null);
            }

            // Assert
            viewModel.CurrentModuleName.Should().Be(expectedModuleName);
        }
    }
}
