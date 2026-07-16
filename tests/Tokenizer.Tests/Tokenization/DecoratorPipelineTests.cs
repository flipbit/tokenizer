using System.Collections.Concurrent;
using System.Globalization;
using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Transformers;
using Tokens.Validators;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tokenization;

public class DecoratorPipelineTests : TokenizerTestBase
{
    private readonly DecoratorPipeline _pipeline;

    public DecoratorPipelineTests(ITestOutputHelper output) : base(output)
    {
        _pipeline = new DecoratorPipeline(new TokenizerOptions(), NullTokenizationDiagnosticCollector.Instance);
    }

    // Spy transformer: implements IOptionsAwareTransformer and records whether options were received
    private sealed class OptionsAwareSpyTransformer : IOptionsAwareTransformer
    {
        public static CultureInfo? ReceivedCulture { get; private set; }

        public bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed)
        {
            ReceivedCulture = options.Culture;
            transformed = value;
            return true;
        }

        // ITokenTransformer fallback — should NOT be called when options-aware path is taken
        public bool TryTransform(object value, string[] args, out object transformed)
        {
            ReceivedCulture = null;
            transformed = value;
            return true;
        }
    }

    // Spy validator: implements IOptionsAwareValidator and records whether options were received
    private sealed class OptionsAwareSpyValidator : IOptionsAwareValidator
    {
        public static CultureInfo? ReceivedCulture { get; private set; }

        public bool IsValid(object value, string[] args, TokenizerOptions options)
        {
            ReceivedCulture = options.Culture;
            return true;
        }

        // ITokenValidator fallback — should NOT be called when options-aware path is taken
        public bool IsValid(object value, params string[] args)
        {
            ReceivedCulture = null;
            return true;
        }
    }

    [Fact]
    public void GivenTokenWithValidValue_WhenEvaluating_ThenReturnsTrueWithValue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _pipeline.Evaluate(token, "Sue", new FileLocation(), out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("Sue", value);
    }

    [Fact]
    public void GivenTokenWithNumericValidator_WhenEvaluatingValidNumber_ThenReturnsTrueWithValue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Age").Build();
        token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), () => new IsNumericValidator(), new ConcurrentDictionary<Type, ITokenDecorator>()));

        // Act
        var result = _pipeline.Evaluate(token, "20", new FileLocation(), out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("20", value);
    }

    [Fact]
    public void GivenTokenWithNumericValidator_WhenEvaluatingInvalidNumber_ThenReturnsFalse()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Age").Build();
        token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), () => new IsNumericValidator(), new ConcurrentDictionary<Type, ITokenDecorator>()));

        // Act
        var result = _pipeline.Evaluate(token, "Twenty", new FileLocation(), out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTokenWithTerminateOnNewLine_WhenValueContainsNewLine_ThenTruncatesAtNewLine()
    {
        // Arrange
        var token = new TokenBuilder()
            .WithName("Name")
            .WithTerminateOnNewLine(true)
            .Build();

        // Act
        _pipeline.Evaluate(token, "Alice\nBob", new FileLocation(), out var value);

        // Assert
        Assert.Equal("Alice", value);
    }

    [Fact]
    public void GivenEmptyValue_WhenEvaluating_ThenReturnsFalse()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _pipeline.Evaluate(token, string.Empty, new FileLocation(), out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTokenWithTrimTrailingWhitespace_WhenEvaluating_ThenTrimsValue()
    {
        // Arrange
        var options = new TokenizerOptions { TrimTrailingWhiteSpace = true };
        var pipeline = new DecoratorPipeline(options, NullTokenizationDiagnosticCollector.Instance);
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        pipeline.Evaluate(token, "Sue   ", new FileLocation(), out var value);

        // Assert
        Assert.Equal("Sue", value);
    }

    [Fact]
    public void GivenTokenWithValidValue_WhenCanEvaluate_ThenReturnsTrue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _pipeline.CanEvaluate(token, "Sue");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenEmptyValue_WhenCanEvaluate_ThenReturnsFalse()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _pipeline.CanEvaluate(token, string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenOptionsAwareTransformer_WhenPipelineEvaluates_ThenOptionsArePassedToTransformer()
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var options = new TokenizerOptions { Culture = culture };
        var pipeline = new DecoratorPipeline(options, NullTokenizationDiagnosticCollector.Instance);
        var cache = new ConcurrentDictionary<Type, ITokenDecorator>();
        var decorator = new TokenDecoratorContext(typeof(OptionsAwareSpyTransformer), () => new OptionsAwareSpyTransformer(), cache);
        var token = new TokenBuilder().WithName("Value").Build();
        token.AddDecorator(decorator);

        // Act
        pipeline.Evaluate(token, "test", new FileLocation(), out _);

        // Assert — spy confirms options-aware overload was invoked with the correct culture
        Assert.Equal(culture, OptionsAwareSpyTransformer.ReceivedCulture);
    }

    [Fact]
    public void GivenOptionsAwareValidator_WhenPipelineEvaluates_ThenOptionsArePassedToValidator()
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo("de-DE");
        var options = new TokenizerOptions { Culture = culture };
        var pipeline = new DecoratorPipeline(options, NullTokenizationDiagnosticCollector.Instance);
        var cache = new ConcurrentDictionary<Type, ITokenDecorator>();
        var decorator = new TokenDecoratorContext(typeof(OptionsAwareSpyValidator), () => new OptionsAwareSpyValidator(), cache);
        var token = new TokenBuilder().WithName("Value").Build();
        token.AddDecorator(decorator);

        // Act
        pipeline.Evaluate(token, "test", new FileLocation(), out _);

        // Assert — spy confirms options-aware overload was invoked with the correct culture
        Assert.Equal(culture, OptionsAwareSpyValidator.ReceivedCulture);
    }
}
