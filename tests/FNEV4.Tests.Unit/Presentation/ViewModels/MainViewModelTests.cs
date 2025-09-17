using FluentAssertions;
using FNEV4.Presentation.ViewModels;
using Xunit;

namespace FNEV4.Tests.Unit.Presentation.ViewModels
{
    /// <summary>
    /// Tests unitaires pour MainViewModel
    /// Validation des fonctionnalités de navigation et d'état
    /// </summary>
    public class MainViewModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var viewModel = new MainViewModel();

            // Assert
            viewModel.CurrentModuleName.Should().Be("Dashboard");
            viewModel.ApplicationTitle.Should().Be("FNEV4 - Application FNE Desktop");
            viewModel.ApplicationVersion.Should().Be("v1.0.0");
            viewModel.CompanyName.Should().Be("Non configuré");
            viewModel.ConnectionStatus.Should().Be("Déconnecté");
            viewModel.StickerBalance.Should().Be("0");
            viewModel.IsMenuExpanded.Should().BeTrue();
        }

        [Fact]
        public void ToggleMenu_ShouldChangeMenuExpansionState()
        {
            // Arrange
            var viewModel = new MainViewModel();
            var initialState = viewModel.IsMenuExpanded;

            // Act
            viewModel.ToggleMenuCommand.Execute(null);

            // Assert
            viewModel.IsMenuExpanded.Should().Be(!initialState);
        }

        [Theory]
        [InlineData("Dashboard - Vue d'ensemble")]
        [InlineData("Dashboard - Actions rapides")]
        public void DashboardNavigationCommands_ShouldUpdateCurrentModuleName(string expectedModuleName)
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act & Assert
            switch (expectedModuleName)
            {
                case "Dashboard - Vue d'ensemble":
                    viewModel.NavigateToDashboardCommand.Execute(null);
                    break;
                case "Dashboard - Actions rapides":
                    viewModel.NavigateToDashboardActionsCommand.Execute(null);
                    break;
            }

            viewModel.CurrentModuleName.Should().Be(expectedModuleName);
        }

        [Theory]
        [InlineData("Import - Import de fichiers")]
        [InlineData("Import - Parsing & Validation")]
        [InlineData("Import - Historique des imports")]
        public void ImportNavigationCommands_ShouldUpdateCurrentModuleName(string expectedModuleName)
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act & Assert
            switch (expectedModuleName)
            {
                case "Import - Import de fichiers":
                    viewModel.NavigateToImportFichiersCommand.Execute(null);
                    break;
                case "Import - Parsing & Validation":
                    viewModel.NavigateToParsingValidationCommand.Execute(null);
                    break;
                case "Import - Historique des imports":
                    viewModel.NavigateToHistoriqueImportsCommand.Execute(null);
                    break;
            }

            viewModel.CurrentModuleName.Should().Be(expectedModuleName);
        }



        [Theory]
        [InlineData("Certification - Certification manuelle")]
        [InlineData("Certification - Certification automatique")]
        [InlineData("Certification - Suivi des certifications")]
        [InlineData("Certification - Retry & Reprises")]
        public void CertificationNavigationCommands_ShouldUpdateCurrentModuleName(string expectedModuleName)
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act & Assert
            switch (expectedModuleName)
            {
                case "Certification - Certification manuelle":
                    viewModel.NavigateToCertificationManuelleCommand.Execute(null);
                    break;
                case "Certification - Certification automatique":
                    viewModel.NavigateToCertificationAutomatiqueCommand.Execute(null);
                    break;
                case "Certification - Suivi des certifications":
                    viewModel.NavigateToSuiviCertificationsCommand.Execute(null);
                    break;
                case "Certification - Retry & Reprises":
                    viewModel.NavigateToRetryReprisesCommand.Execute(null);
                    break;
            }

            viewModel.CurrentModuleName.Should().Be(expectedModuleName);
        }

        [Theory]
        [InlineData("Clients - Liste des clients")]
        [InlineData("Clients - Ajout/Modification")]
        [InlineData("Clients - Recherche avancée")]
        public void ClientsNavigationCommands_ShouldUpdateCurrentModuleName(string expectedModuleName)
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act & Assert
            switch (expectedModuleName)
            {
                case "Clients - Liste des clients":
                    viewModel.NavigateToListeClientsCommand.Execute(null);
                    break;
                case "Clients - Ajout/Modification":
                    viewModel.NavigateToAjoutModificationClientCommand.Execute(null);
                    break;
                case "Clients - Recherche avancée":
                    viewModel.NavigateToRechercheAvanceeCommand.Execute(null);
                    break;
            }

            viewModel.CurrentModuleName.Should().Be(expectedModuleName);
        }

        [Theory]
        [InlineData("Rapports - Rapports standards")]
        [InlineData("Rapports - Rapports FNE")]
        [InlineData("Rapports - Analyses personnalisées")]
        public void RapportsNavigationCommands_ShouldUpdateCurrentModuleName(string expectedModuleName)
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act & Assert
            switch (expectedModuleName)
            {
                case "Rapports - Rapports standards":
                    viewModel.NavigateToRapportsStandardsCommand.Execute(null);
                    break;
                case "Rapports - Rapports FNE":
                    viewModel.NavigateToRapportsFneCommand.Execute(null);
                    break;
                case "Rapports - Analyses personnalisées":
                    viewModel.NavigateToAnalysesPersonnaliseesCommand.Execute(null);
                    break;
            }

            viewModel.CurrentModuleName.Should().Be(expectedModuleName);
        }

        [Theory]
        [InlineData("Configuration - Entreprise")]
        [InlineData("Configuration - API FNE")]
        [InlineData("Configuration - Chemins & Dossiers")]
        [InlineData("Configuration - Interface utilisateur")]
        [InlineData("Configuration - Performances")]
        public void ConfigurationNavigationCommands_ShouldUpdateCurrentModuleName(string expectedModuleName)
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act & Assert
            switch (expectedModuleName)
            {
                case "Configuration - Entreprise":
                    viewModel.NavigateToEntrepriseConfigCommand.Execute(null);
                    break;
                case "Configuration - API FNE":
                    viewModel.NavigateToApiFneConfigCommand.Execute(null);
                    break;
                case "Configuration - Chemins & Dossiers":
                    viewModel.NavigateToCheminsDossiersCommand.Execute(null);
                    break;
                case "Configuration - Interface utilisateur":
                    viewModel.NavigateToInterfaceUtilisateurCommand.Execute(null);
                    break;
                case "Configuration - Performances":
                    viewModel.NavigateToPerformancesCommand.Execute(null);
                    break;
            }

            viewModel.CurrentModuleName.Should().Be(expectedModuleName);
        }

        [Theory]
        [InlineData("Maintenance - Logs & Diagnostics")]
        [InlineData("Maintenance - Base de données")]
        [InlineData("Maintenance - Synchronisation")]
        [InlineData("Maintenance - Outils techniques")]
        public void MaintenanceNavigationCommands_ShouldUpdateCurrentModuleName(string expectedModuleName)
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act & Assert
            switch (expectedModuleName)
            {
                case "Maintenance - Logs & Diagnostics":
                    viewModel.NavigateToLogsDiagnosticsCommand.Execute(null);
                    break;
                case "Maintenance - Base de données":
                    viewModel.NavigateToBaseDonneesCommand.Execute(null);
                    break;
                case "Maintenance - Synchronisation":
                    viewModel.NavigateToSynchronisationCommand.Execute(null);
                    break;
                case "Maintenance - Outils techniques":
                    viewModel.NavigateToOutilsTechniquesCommand.Execute(null);
                    break;
            }

            viewModel.CurrentModuleName.Should().Be(expectedModuleName);
        }

        [Theory]
        [InlineData("Aide - Documentation")]
        [InlineData("Aide - Support")]
        [InlineData("Aide - À propos")]
        public void AideNavigationCommands_ShouldUpdateCurrentModuleName(string expectedModuleName)
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act & Assert
            switch (expectedModuleName)
            {
                case "Aide - Documentation":
                    viewModel.NavigateToDocumentationCommand.Execute(null);
                    break;
                case "Aide - Support":
                    viewModel.NavigateToSupportCommand.Execute(null);
                    break;
                case "Aide - À propos":
                    viewModel.NavigateToAProposCommand.Execute(null);
                    break;
            }

            viewModel.CurrentModuleName.Should().Be(expectedModuleName);
        }

        [Fact]
        public async Task RefreshConnectionStatus_ShouldUpdateConnectionInfo()
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Act
            await viewModel.RefreshConnectionStatusCommand.ExecuteAsync(null);

            // Assert
            viewModel.ConnectionStatus.Should().Be("Connecté");
            viewModel.StickerBalance.Should().Be("1,245");
        }

        [Fact]
        public void AllNavigationCommands_ShouldNotBeNull()
        {
            // Arrange
            var viewModel = new MainViewModel();

            // Assert - Vérifier que toutes les commandes sont initialisées
            viewModel.NavigateToDashboardCommand.Should().NotBeNull();
            viewModel.NavigateToDashboardActionsCommand.Should().NotBeNull();
            
            viewModel.NavigateToImportFichiersCommand.Should().NotBeNull();
            viewModel.NavigateToParsingValidationCommand.Should().NotBeNull();
            viewModel.NavigateToHistoriqueImportsCommand.Should().NotBeNull();
            
            viewModel.NavigateToCertificationManuelleCommand.Should().NotBeNull();
            viewModel.NavigateToCertificationAutomatiqueCommand.Should().NotBeNull();
            viewModel.NavigateToSuiviCertificationsCommand.Should().NotBeNull();
            viewModel.NavigateToRetryReprisesCommand.Should().NotBeNull();
            
            viewModel.NavigateToListeClientsCommand.Should().NotBeNull();
            viewModel.NavigateToAjoutModificationClientCommand.Should().NotBeNull();
            viewModel.NavigateToRechercheAvanceeCommand.Should().NotBeNull();
            
            viewModel.NavigateToRapportsStandardsCommand.Should().NotBeNull();
            viewModel.NavigateToRapportsFneCommand.Should().NotBeNull();
            viewModel.NavigateToAnalysesPersonnaliseesCommand.Should().NotBeNull();
            
            viewModel.NavigateToEntrepriseConfigCommand.Should().NotBeNull();
            viewModel.NavigateToApiFneConfigCommand.Should().NotBeNull();
            viewModel.NavigateToCheminsDossiersCommand.Should().NotBeNull();
            viewModel.NavigateToInterfaceUtilisateurCommand.Should().NotBeNull();
            viewModel.NavigateToPerformancesCommand.Should().NotBeNull();
            
            viewModel.NavigateToLogsDiagnosticsCommand.Should().NotBeNull();
            viewModel.NavigateToBaseDonneesCommand.Should().NotBeNull();
            viewModel.NavigateToSynchronisationCommand.Should().NotBeNull();
            viewModel.NavigateToOutilsTechniquesCommand.Should().NotBeNull();
            
            viewModel.NavigateToDocumentationCommand.Should().NotBeNull();
            viewModel.NavigateToSupportCommand.Should().NotBeNull();
            viewModel.NavigateToAProposCommand.Should().NotBeNull();
            
            viewModel.ToggleMenuCommand.Should().NotBeNull();
            viewModel.RefreshConnectionStatusCommand.Should().NotBeNull();
        }
    }
}
