using System.Collections.Concurrent;
using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Validators;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tokenization;

public class TokenAssignerTests : TokenizerTestBase
{
    private readonly TokenAssigner _assigner;

    public TokenAssignerTests(ITestOutputHelper output) : base(output)
    {
        _assigner = new TokenAssigner(new TokenizerOptions(), NullDiagnosticCollector.Instance);
    }

    public class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public int? Score { get; set; }
    }

    [Fact]
    public void GivenTokenWithValidValue_WhenAssigning_ThenSetsPropertyValue()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _assigner.Assign(token, person, "Sue", new FileLocation(), out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("Sue", person.Name);
        Assert.Equal("Sue", value);
    }

    [Fact]
    public void GivenTokenWithNumericValidator_WhenAssigningValidNumber_ThenSetsPropertyValue()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("Age").Build();
        token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), new ConcurrentDictionary<Type, ITokenDecorator>()));

        // Act
        var result = _assigner.Assign(token, person, "20", new FileLocation(), out var value);

        // Assert
        Assert.True(result);
        Assert.Equal(20, person.Age);
    }

    [Fact]
    public void GivenTokenWithNumericValidator_WhenAssigningInvalidNumber_ThenReturnsFalse()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("Age").Build();
        token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), new ConcurrentDictionary<Type, ITokenDecorator>()));

        // Act
        var result = _assigner.Assign(token, person, "Twenty", new FileLocation(), out _);

        // Assert
        Assert.False(result);
        Assert.Equal(0, person.Age);
    }

    [Fact]
    public void GivenTokenWithTerminateOnNewLine_WhenValueContainsNewLine_ThenTruncatesAtNewLine()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder()
            .WithName("Name")
            .WithTerminateOnNewLine(true)
            .Build();

        // Act
        _assigner.Assign(token, person, "Alice\nBob", new FileLocation(), out _);

        // Assert
        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void GivenNullTarget_WhenAssigning_ThenReturnsTrueWithoutSideEffects()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _assigner.Assign(token, target: null, "Sue", new FileLocation(), out var value);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenEmptyValue_WhenAssigning_ThenReturnsFalse()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _assigner.Assign(token, person, string.Empty, new FileLocation(), out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenDictionaryTarget_WhenAssigning_ThenSetsKeyValue()
    {
        // Arrange
        var dict = new Dictionary<string, object>(StringComparer.Ordinal);
        var token = new TokenBuilder().WithName("Key").Build();

        // Act
        var result = _assigner.Assign(token, dict, "Value", new FileLocation(), out _);

        // Assert
        Assert.True(result);
        Assert.Equal("Value", dict["Key"]);
    }

    [Fact]
    public void GivenMissingPropertyWithIgnoreEnabled_WhenAssigning_ThenReturnsTrueWithoutThrowing()
    {
        // Arrange
        var person = new Person();
        var options = new TokenizerOptions { IgnoreMissingProperties = true };
        var assigner = new TokenAssigner(options, NullDiagnosticCollector.Instance);
        var token = new TokenBuilder().WithName("NonExistent").Build();

        // Act
        var result = assigner.Assign(token, person, "value", new FileLocation(), out _);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenMissingPropertyWithIgnoreDisabled_WhenAssigning_ThenThrowsMissingMemberException()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("NonExistent").Build();

        // Act & Assert
        Assert.Throws<MissingMemberException>(() =>
            _assigner.Assign(token, person, "value", new FileLocation(), out _));
    }

    [Fact]
    public void GivenTokenWithTrimTrailingWhitespace_WhenAssigning_ThenTrimsValue()
    {
        // Arrange
        var person = new Person();
        var options = new TokenizerOptions { TrimTrailingWhiteSpace = true };
        var assigner = new TokenAssigner(options, NullDiagnosticCollector.Instance);
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        assigner.Assign(token, person, "Sue   ", new FileLocation(), out _);

        // Assert
        Assert.Equal("Sue", person.Name);
    }

    [Fact]
    public void GivenTokenWithValidValue_WhenCanAssign_ThenReturnsTrue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _assigner.CanAssign(token, "Sue");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenEmptyValue_WhenCanAssign_ThenReturnsFalse()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();

        // Act
        var result = _assigner.CanAssign(token, string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenConcatenatableToken_WhenAssigningTwice_ThenConcatenatesValues()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("Name").Build();
        token.CanConcatenate = true;
        token.ConcatenationString = ", ";

        // Act
        _assigner.Assign(token, person, "Alice", new FileLocation(), out _);
        _assigner.Assign(token, person, "Bob", new FileLocation(), out _);

        // Assert
        Assert.Equal("Alice, Bob", person.Name);
    }

    [Fact]
    public void GivenTypeConversionFailure_WhenAssigning_ThenReturnsFalse()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder().WithName("Score").Build();

        // Act
        var result = _assigner.Assign(token, person, "not-a-number", new FileLocation(), out _);

        // Assert
        Assert.False(result);
        Assert.Null(person.Score);
    }

    [Fact]
    public void GivenRepeatingTokenWithDictionaryTarget_WhenAssigningMultipleTimes_ThenBuildsListValue()
    {
        // Arrange
        var dict = new Dictionary<string, object>(StringComparer.Ordinal);
        var token = new TokenBuilder().WithName("Items").WithRepeating(true).Build();

        // Act
        _assigner.Assign(token, dict, "one", new FileLocation(), out _);
        _assigner.Assign(token, dict, "two", new FileLocation(), out _);

        // Assert
        var list = Assert.IsType<List<object>>(dict["Items"]);
        Assert.Equal(2, list.Count);
        Assert.Equal("one", list[0]);
        Assert.Equal("two", list[1]);
    }
}
