using System.Collections.Concurrent;
using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Validators;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tokenization;

public class DecoratorPipelineTests : TokenizerTestBase
{
    private readonly DecoratorPipeline _pipeline;

    public DecoratorPipelineTests(ITestOutputHelper output) : base(output)
    {
        _pipeline = new DecoratorPipeline(new TokenizerOptions(), NullDiagnosticCollector.Instance);
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
        token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), new ConcurrentDictionary<Type, ITokenDecorator>()));

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
        token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), new ConcurrentDictionary<Type, ITokenDecorator>()));

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
        var pipeline = new DecoratorPipeline(options, NullDiagnosticCollector.Instance);
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
}
