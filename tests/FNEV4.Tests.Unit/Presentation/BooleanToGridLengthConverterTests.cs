using System.Globalization;
using System.Windows;
using Xunit;
using FluentAssertions;
using FNEV4.Presentation.Converters;

namespace FNEV4.Tests.Unit.Presentation
{
    /// <summary>
    /// Tests unitaires pour BooleanToGridLengthConverter
    /// Validation des conversions de largeur de menu
    /// </summary>
    public class BooleanToGridLengthConverterTests
    {
        private readonly BooleanToGridLengthConverter _converter;

        public BooleanToGridLengthConverterTests()
        {
            _converter = new BooleanToGridLengthConverter();
        }

        [Fact]
        public void Convert_WhenExpandedTrue_ShouldReturnExpandedWidth()
        {
            // Arrange
            bool isExpanded = true;
            string parameter = "280,64";

            // Act
            var result = _converter.Convert(isExpanded, typeof(GridLength), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeOfType<GridLength>();
            var gridLength = (GridLength)result;
            gridLength.Value.Should().Be(280);
        }

        [Fact]
        public void Convert_WhenExpandedFalse_ShouldReturnCollapsedWidth()
        {
            // Arrange
            bool isExpanded = false;
            string parameter = "280,64";

            // Act
            var result = _converter.Convert(isExpanded, typeof(GridLength), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeOfType<GridLength>();
            var gridLength = (GridLength)result;
            gridLength.Value.Should().Be(64);
        }

        [Fact]
        public void Convert_WithDifferentWidths_ShouldReturnCorrectValues()
        {
            // Arrange
            string parameter = "320,80";

            // Act - Expanded
            var expandedResult = _converter.Convert(true, typeof(GridLength), parameter, CultureInfo.InvariantCulture);
            var expandedGridLength = (GridLength)expandedResult;

            // Act - Collapsed
            var collapsedResult = _converter.Convert(false, typeof(GridLength), parameter, CultureInfo.InvariantCulture);
            var collapsedGridLength = (GridLength)collapsedResult;

            // Assert
            expandedGridLength.Value.Should().Be(320);
            collapsedGridLength.Value.Should().Be(80);
        }

        [Fact]
        public void Convert_WithInvalidParameter_ShouldReturnDefaultWidth()
        {
            // Arrange
            bool isExpanded = true;
            string invalidParameter = "invalid";

            // Act
            var result = _converter.Convert(isExpanded, typeof(GridLength), invalidParameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeOfType<GridLength>();
            var gridLength = (GridLength)result;
            gridLength.Value.Should().Be(280); // Valeur par défaut
        }

        [Fact]
        public void Convert_WithNullParameter_ShouldReturnDefaultWidth()
        {
            // Arrange
            bool isExpanded = true;

            // Act
            var result = _converter.Convert(isExpanded, typeof(GridLength), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeOfType<GridLength>();
            var gridLength = (GridLength)result;
            gridLength.Value.Should().Be(280); // Valeur par défaut
        }

        [Fact]
        public void ConvertBack_ShouldThrowNotImplementedException()
        {
            // Arrange
            var gridLength = new GridLength(280);

            // Act & Assert
            var exception = Assert.Throws<NotImplementedException>(
                () => _converter.ConvertBack(gridLength, typeof(bool), null, CultureInfo.InvariantCulture)
            );

            exception.Should().NotBeNull();
        }

        [Theory]
        [InlineData("100,50", true, 100)]
        [InlineData("100,50", false, 50)]
        [InlineData("200,30", true, 200)]
        [InlineData("200,30", false, 30)]
        public void Convert_WithVariousParameters_ShouldReturnCorrectWidth(string parameter, bool isExpanded, double expectedWidth)
        {
            // Act
            var result = _converter.Convert(isExpanded, typeof(GridLength), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeOfType<GridLength>();
            var gridLength = (GridLength)result;
            gridLength.Value.Should().Be(expectedWidth);
        }
    }
}
