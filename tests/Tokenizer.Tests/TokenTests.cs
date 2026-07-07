using System.Collections.Concurrent;
using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Validators;
using Xunit;
using Xunit.Abstractions;
using Tokens.Diagnostics;

namespace Tokens;

public class TokenTests : TokenizerTestBase
{
    public TokenTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly Token _token = new("Test", string.Empty, string.Empty, new FileLocation());

    public class Person
    {
        public string Name { get; set; } = null!;

        public int Age { get; set; }

        public DateTime Birthday { get; set; }
    }

    [Fact]
    public void GivenTokenWithValidValue_WhenAssigningToObject_ThenSetsPropertyValue()
    {
        // Arrange
        var person = new Person();
        _token.Name = "Person.Name";

        // Act
        var assigned = _token.Assign(person, "Sue", new TokenizerOptions(), new FileLocation(), out var value, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(assigned);
        Assert.Equal("Sue", person.Name);
    }

    [Fact]
    public void GivenTokenWithNumericValidator_WhenAssigningValidNumber_ThenSetsPropertyValue()
    {
        // Arrange
        var person = new Person();
        _token.Name = "Person.Age";
        _token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), new ConcurrentDictionary<Type, ITokenDecorator>()));

        // Act
        var assigned = _token.Assign(person, "20", new TokenizerOptions(), new FileLocation(), out var value, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(assigned);
        Assert.Equal(20, person.Age);
    }

    [Fact]
    public void GivenTokenWithNumericValidator_WhenAssigningInvalidNumber_ThenFailsToAssign()
    {
        // Arrange
        var person = new Person();
        _token.Name = "Person.Age";
        _token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator), new ConcurrentDictionary<Type, ITokenDecorator>()));

        // Act
        var assigned = _token.Assign(person, "Twenty", new TokenizerOptions(), new FileLocation(), out var value, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(assigned);
        Assert.Equal(0, person.Age);
    }

    [Fact]
    public void GivenTokenWithStringValue_WhenAssigningToObject_ThenSetsPropertyValue()
    {
        // Arrange
        var person = new Person();
        _token.Name = "Person.Name";

        // Act
        var assigned = _token.Assign(person, "Sue", new TokenizerOptions(), new FileLocation(), out var value, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(assigned);
        Assert.Equal("Sue", person.Name);
    }

    [Fact]
    public void GivenTokenWithExactPropertyName_WhenAssigning_ThenSetsValue()
    {
        // Arrange
        var person = new Person();
        var token = new TokenBuilder()
            .WithName("Name")
            .Build();

        // Act
        token.Assign(person, "Alice", new TokenizerOptions(), new FileLocation(), out _, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal("Alice", person.Name);
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
        token.Assign(person, "Alice\nBob", new TokenizerOptions(), new FileLocation(), out _, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Equal("Alice", person.Name);
    }
}
