using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable MA0048 // Scenario test: TokenizeResultAssign.ExistingInstance.Tests.cs
namespace Tokens;

public class TokenizeResultAssignExistingInstanceTests : TokenizerTestBase
{
    public TokenizeResultAssignExistingInstanceTests(ITestOutputHelper output) : base(output)
    {
    }

    public class PersonTarget
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public class ImmutablePerson
    {
        public ImmutablePerson(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public string Name { get; set; } = "";
    }

    public struct Measurement
    {
        public string Unit { get; set; }
        public int Value { get; set; }
    }

    public record PersonRecord
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public record struct Coordinate
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    [Fact]
    public void GivenExistingClassInstance_WhenAssign_ThenPopulatesProperties()
    {
        // Arrange
        var nameToken = new TokenBuilder().WithName("Name").Build();
        var ageToken = new TokenBuilder().WithName("Age").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(nameToken, ageToken).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(nameToken, "Alice", new FileLocation()),
                new TokenMatch(ageToken, 30, new FileLocation()))
            .Build();
        var target = new PersonTarget();

        // Act
        var populated = result.Assign(target);

        // Assert
        Assert.Same(target, populated);
        Assert.Equal("Alice", populated.Name);
        Assert.Equal(30, populated.Age);
    }

    [Fact]
    public void GivenPrePopulatedInstance_WhenAssign_ThenOverwritesValues()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Bob", new FileLocation()))
            .Build();
        var target = new PersonTarget { Name = "Alice", Age = 42 };

        // Act
        var populated = result.Assign(target);

        // Assert
        Assert.Equal("Bob", populated.Name);
        Assert.Equal(42, populated.Age);
    }

    [Fact]
    public void GivenClassWithoutParameterlessConstructor_WhenAssign_ThenPopulatesProperties()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice", new FileLocation()))
            .Build();
        var target = new ImmutablePerson("person-1");

        // Act
        var populated = result.Assign(target);

        // Assert
        Assert.Same(target, populated);
        Assert.Equal("person-1", populated.Id);
        Assert.Equal("Alice", populated.Name);
    }

    [Fact]
    public void GivenStruct_WhenAssign_ThenReturnValueHasPopulatedProperties()
    {
        // Arrange
        var unitToken = new TokenBuilder().WithName("Unit").Build();
        var valueToken = new TokenBuilder().WithName("Value").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(unitToken, valueToken).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(unitToken, "kg", new FileLocation()),
                new TokenMatch(valueToken, 100, new FileLocation()))
            .Build();
        var target = new Measurement();

        // Act
        var populated = result.Assign(target);

        // Assert
        Assert.Equal("kg", populated.Unit);
        Assert.Equal(100, populated.Value);
    }

    [Fact]
    public void GivenRecord_WhenAssign_ThenPopulatesProperties()
    {
        // Arrange
        var nameToken = new TokenBuilder().WithName("Name").Build();
        var ageToken = new TokenBuilder().WithName("Age").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(nameToken, ageToken).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(nameToken, "Alice", new FileLocation()),
                new TokenMatch(ageToken, 30, new FileLocation()))
            .Build();
        var target = new PersonRecord();

        // Act
        var populated = result.Assign(target);

        // Assert
        Assert.Same(target, populated);
        Assert.Equal("Alice", populated.Name);
        Assert.Equal(30, populated.Age);
    }

    [Fact]
    public void GivenRecordStruct_WhenAssign_ThenReturnValueHasPopulatedProperties()
    {
        // Arrange
        var xToken = new TokenBuilder().WithName("X").Build();
        var yToken = new TokenBuilder().WithName("Y").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(xToken, yToken).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(xToken, "1.5", new FileLocation()),
                new TokenMatch(yToken, "2.5", new FileLocation()))
            .Build();
        var target = new Coordinate();

        // Act
        var populated = result.Assign(target);

        // Assert
        Assert.Equal(1.5, populated.X);
        Assert.Equal(2.5, populated.Y);
    }

    [Fact]
    public void GivenPartialFailure_WhenAssignExistingInstance_ThenExceptionContainsPartialResult()
    {
        // Arrange
        var nameToken = new TokenBuilder().WithName("Name").Build();
        var ageToken = new TokenBuilder().WithName("Age").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(nameToken, ageToken).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(nameToken, "Alice", new FileLocation()),
                new TokenMatch(ageToken, "not-a-number", new FileLocation()))
            .Build();
        var target = new PersonTarget();

        // Act & Assert
        var ex = Assert.Throws<AssignmentFailedException>(() => result.Assign(target));
        Assert.NotNull(ex.PartialResult);
        var partial = Assert.IsType<PersonTarget>(ex.PartialResult);
        Assert.Equal("Alice", partial.Name);
    }
}
