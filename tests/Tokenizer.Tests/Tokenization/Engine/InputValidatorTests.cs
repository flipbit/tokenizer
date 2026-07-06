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
        InputValidator.ValidateTargetObject(null, _logger);
    }

    [Fact]
    public void GivenDictionaryTarget_WhenValidating_ThenDoesNotThrow()
    {
        var target = new Dictionary<string, object>();
        InputValidator.ValidateTargetObject(target, _logger);
    }

    [Fact]
    public void GivenWritableTarget_WhenValidating_ThenDoesNotThrow()
    {
        var target = new WritableTarget();
        InputValidator.ValidateTargetObject(target, _logger);
    }

    [Fact]
    public void GivenReadOnlyTarget_WhenValidating_ThenThrowsArgumentException()
    {
        var target = new ReadOnlyTarget("test");
        var ex = Assert.Throws<ArgumentException>(() =>
            InputValidator.ValidateTargetObject(target, _logger));
        Assert.Contains("no settable properties", ex.Message);
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
