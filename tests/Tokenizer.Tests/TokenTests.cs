using Tokens.Enumerators;
using Tokens.Validators;
using Xunit;
using Xunit.Abstractions;
using Tokens.Diagnostics;

namespace Tokens;

public class TokenTests : Tests.TokenizerTestBase
{
    public TokenTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly Token token = new("Test", string.Empty, string.Empty, new FileLocation());

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
        token.Name = "Person.Name";

        // Act
        var assigned = token.Assign(person, "Sue", new TokenizerOptions(), new FileLocation(), out var value, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(assigned);
        Assert.Equal("Sue", person.Name);
    }

    [Fact]
    public void GivenTokenWithNumericValidator_WhenAssigningValidNumber_ThenSetsPropertyValue()
    {
        // Arrange
        var person = new Person();
        token.Name = "Person.Age";
        token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator)));

        // Act
        var assigned = token.Assign(person, "20", new TokenizerOptions(), new FileLocation(), out var value, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(assigned);
        Assert.Equal(20, person.Age);
    }

    [Fact]
    public void GivenTokenWithNumericValidator_WhenAssigningInvalidNumber_ThenFailsToAssign()
    {
        // Arrange
        var person = new Person();
        token.Name = "Person.Age";
        token.AddDecorator(new TokenDecoratorContext(typeof(IsNumericValidator)));

        // Act
        var assigned = token.Assign(person, "Twenty", new TokenizerOptions(), new FileLocation(), out var value, NullDiagnosticCollector.Instance);

        // Assert
        Assert.False(assigned);
        Assert.Equal(0, person.Age);
    }

    [Fact]
    public void GivenTokenWithStringValue_WhenAssigningToObject_ThenSetsPropertyValue()
    {
        // Arrange
        var person = new Person();
        token.Name = "Person.Name";

        // Act
        var assigned = token.Assign(person, "Sue", new TokenizerOptions(), new FileLocation(), out var value, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(assigned);
        Assert.Equal("Sue", person.Name);
    }
}
