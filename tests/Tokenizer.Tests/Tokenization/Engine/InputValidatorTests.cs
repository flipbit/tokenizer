using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tokens.Tokenization.Engine;

public class InputValidatorTests
{
    private readonly ILogger<TokenizationEngine> _logger = NullLogger<TokenizationEngine>.Instance;

    [Fact]
    public void GivenNullTarget_WhenValidating_ThenDoesNotThrow()
    {
        // Act & Assert
        InputValidator.ValidateTargetObject(targetObject: null, _logger);
    }

    [Fact]
    public void GivenDictionaryTarget_WhenValidating_ThenDoesNotThrow()
    {
        // Arrange
        var target = new Dictionary<string, object>();

        // Act & Assert
        InputValidator.ValidateTargetObject(target, _logger);
    }

    [Fact]
    public void GivenWritableTarget_WhenValidating_ThenDoesNotThrow()
    {
        // Arrange
        var target = new WritableTarget();

        // Act & Assert
        InputValidator.ValidateTargetObject(target, _logger);
    }

    [Fact]
    public void GivenReadOnlyTarget_WhenValidating_ThenThrowsArgumentException()
    {
        // Arrange
        var target = new ReadOnlyTarget("test");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            InputValidator.ValidateTargetObject(target, _logger));
        Assert.Contains("no settable properties", ex.Message, StringComparison.Ordinal);
    }

    private class WritableTarget
    {
        public string Name { get; set; } = null!;
    }

    private sealed class ReadOnlyTarget
    {
        public ReadOnlyTarget(string name) { Name = name; }
        public string Name { get; }
    }
}
