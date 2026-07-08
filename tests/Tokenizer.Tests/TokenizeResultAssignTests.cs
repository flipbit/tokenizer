using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenizeResultAssignTests : TokenizerTestBase
{
    public TokenizeResultAssignTests(ITestOutputHelper output) : base(output)
    {
    }

    public class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public int? Score { get; set; }
    }

    public class PersonSummary
    {
        public string Name { get; set; } = null!;
    }

    [Fact]
    public void GivenMatchesWithStringValue_WhenAssign_ThenPopulatesProperty()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice", new FileLocation()))
            .Build();

        // Act
        var person = result.Assign<Person>();

        // Assert
        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void GivenMatchesWithMultipleProperties_WhenAssign_ThenPopulatesAll()
    {
        // Arrange
        var nameToken = new TokenBuilder().WithName("Name").Build();
        var ageToken = new TokenBuilder().WithName("Age").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(nameToken, ageToken).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(nameToken, "Bob", new FileLocation()),
                new TokenMatch(ageToken, 30, new FileLocation()))
            .Build();

        // Act
        var person = result.Assign<Person>();

        // Assert
        Assert.Equal("Bob", person.Name);
        Assert.Equal(30, person.Age);
    }

    [Fact]
    public void GivenTypeConversionFailure_WhenAssign_ThenThrowsAssignmentFailedException()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Score").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "not-a-number", new FileLocation()))
            .Build();

        // Act & Assert
        var ex = Assert.Throws<AssignmentFailedException>(() => result.Assign<Person>());
        Assert.Single(ex.Errors);
        Assert.IsType<TypeConversionException>(ex.Errors[0]);
    }

    [Fact]
    public void GivenMissingPropertyWithIgnoreEnabled_WhenAssign_ThenReturnsSuccessfully()
    {
        // Arrange
        var token = new TokenBuilder().WithName("NonExistent").Build();
        var options = new TokenizerOptions { IgnoreMissingProperties = true };
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithOptions(options).Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "value", new FileLocation()))
            .Build();

        // Act
        var person = result.Assign<Person>();

        // Assert — no exception thrown, person has default values
        Assert.NotNull(person);
    }

    [Fact]
    public void GivenMissingPropertyWithIgnoreDisabled_WhenAssign_ThenThrowsAssignmentFailedException()
    {
        // Arrange
        var token = new TokenBuilder().WithName("NonExistent").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "value", new FileLocation()))
            .Build();

        // Act & Assert
        var ex = Assert.Throws<AssignmentFailedException>(() => result.Assign<Person>());
        Assert.Single(ex.Errors);
        Assert.IsType<MissingMemberException>(ex.Errors[0]);
    }

    [Fact]
    public void GivenConcatenatableToken_WhenAssign_ThenSetsValue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        token.CanConcatenate = true;
        token.ConcatenationString = ", ";
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice, Bob", new FileLocation()))
            .Build();

        // Act
        var person = result.Assign<Person>();

        // Assert
        Assert.Equal("Alice, Bob", person.Name);
    }

    [Fact]
    public void GivenResult_WhenAssignCalledTwice_ThenBothSucceed()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice", new FileLocation()))
            .Build();

        // Act
        var first = result.Assign<Person>();
        var second = result.Assign<PersonSummary>();

        // Assert
        Assert.Equal("Alice", first.Name);
        Assert.Equal("Alice", second.Name);
        Assert.NotSame((object)first, second);
    }

    [Fact]
    public void GivenPartialAssignmentFailure_WhenAssign_ThenExceptionContainsPartialResult()
    {
        // Arrange
        var nameToken = new TokenBuilder().WithName("Name").Build();
        var scoreToken = new TokenBuilder().WithName("Score").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(nameToken, scoreToken).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(nameToken, "Alice", new FileLocation()),
                new TokenMatch(scoreToken, "not-a-number", new FileLocation()))
            .Build();

        // Act & Assert
        var ex = Assert.Throws<AssignmentFailedException>(() => result.Assign<Person>());
        Assert.NotNull(ex.PartialResult);
        var partial = Assert.IsType<Person>(ex.PartialResult);
        Assert.Equal("Alice", partial.Name);
    }
}
