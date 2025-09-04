using FluentAssertions;
using FNEV4.Presentation.Converters;
using System.Globalization;
using System.Windows;
using Xunit;

namespace FNEV4.Tests.Unit.Presentation.Converters
{
    /// <summary>
    /// Tests unitaires pour BooleanToGridLengthConverter
    /// Validation de la conversion boolean vers GridLength pour le menu
    /// </summary>
    public class BooleanToGridLengthConverterTests
    {
        private readonly BooleanToGridLengthConverter _converter;

        public BooleanToGridLengthConverterTests()
        {
            _converter = new BooleanToGridLengthConverter();
        }

        [Fact]
        public void Convert_WithTrueAndValidParameter_ShouldReturnExpandedWidth()
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
        public void Convert_WithFalseAndValidParameter_ShouldReturnCollapsedWidth()
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
        public void Convert_WithInvalidParameter_ShouldReturnDefaultWidth()
        {
            // Arrange
            bool isExpanded = true;
            string parameter = "invalid";

            // Act
            var result = _converter.Convert(isExpanded, typeof(GridLength), parameter, CultureInfo.InvariantCulture);

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
            string parameter = null!;

            // Act
            var result = _converter.Convert(isExpanded, typeof(GridLength), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeOfType<GridLength>();
            var gridLength = (GridLength)result;
            gridLength.Value.Should().Be(280); // Valeur par défaut
        }

        [Fact]
        public void Convert_WithInvalidValueType_ShouldReturnDefaultWidth()
        {
            // Arrange
            string invalidValue = "not a boolean";
            string parameter = "280,64";

            // Act
            var result = _converter.Convert(invalidValue, typeof(GridLength), parameter, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeOfType<GridLength>();
            var gridLength = (GridLength)result;
            gridLength.Value.Should().Be(280); // Valeur par défaut
        }

        [Fact]
        public void Convert_WithSingleParameterValue_ShouldReturnDefaultWidth()
        {
            // Arrange
            bool isExpanded = true;
            string parameter = "280"; // Un seul paramètre au lieu de deux

            // Act
            var result = _converter.Convert(isExpanded, typeof(GridLength), parameter, CultureInfo.InvariantCulture);

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
            Assert.Throws<NotImplementedException>(() => 
                _converter.ConvertBack(gridLength, typeof(bool), null, CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(true, "300,80", 300)]
        [InlineData(false, "300,80", 80)]
        [InlineData(true, "250,50", 250)]
        [InlineData(false, "250,50", 50)]
        public void Convert_WithVariousValidParameters_ShouldReturnCorrectWidth(bool isExpanded, string parameter, double expectedWidth)
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
