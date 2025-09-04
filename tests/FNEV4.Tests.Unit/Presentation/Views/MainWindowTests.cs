using FluentAssertions;
using FNEV4.Presentation.Views;
using System.Windows;
using Xunit;

namespace FNEV4.Tests.Unit.Presentation.Views
{
    /// <summary>
    /// Tests d'intégration pour MainWindow
    /// Validation de l'initialisation et du comportement de l'interface
    /// </summary>
    public class MainWindowTests
    {
        [Fact(Skip = "WPF UI tests require STA thread and complex setup")]
        public void Constructor_ShouldInitializeSuccessfully()
        {
            // Arrange & Act
            var mainWindow = new MainWindow();

            // Assert
            mainWindow.Should().NotBeNull();
            mainWindow.DataContext.Should().NotBeNull();
            mainWindow.Title.Should().NotBeNullOrEmpty();
        }

        [Fact(Skip = "WPF UI tests require STA thread and complex setup")]
        public void DataContext_ShouldBeMainViewModel()
        {
            // Arrange & Act
            var mainWindow = new MainWindow();

            // Assert
            mainWindow.DataContext.Should().BeOfType<FNEV4.Presentation.ViewModels.MainViewModel>();
        }

        [Fact(Skip = "WPF UI tests require STA thread and complex setup")]
        public void WindowProperties_ShouldBeConfiguredCorrectly()
        {
            // Arrange & Act
            var mainWindow = new MainWindow();

            // Assert
            mainWindow.WindowStartupLocation.Should().Be(WindowStartupLocation.CenterScreen);
            mainWindow.MinHeight.Should().Be(600);
            mainWindow.MinWidth.Should().Be(900);
            mainWindow.Height.Should().Be(800);
            mainWindow.Width.Should().Be(1200);
        }
    }
}
