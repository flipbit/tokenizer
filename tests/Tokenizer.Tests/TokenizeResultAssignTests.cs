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
        var typed = result.Assign<Person>();

        // Assert
        Assert.Equal("Alice", typed.Value.Name);
        Assert.True(typed.Success);
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
        var typed = result.Assign<Person>();

        // Assert
        Assert.Equal("Bob", typed.Value.Name);
        Assert.Equal(30, typed.Value.Age);
    }

    [Fact]
    public void GivenTypeConversionFailure_WhenAssign_ThenSuccessIsFalseAndExceptionRecorded()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Score").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "not-a-number", new FileLocation()))
            .Build();

        // Act
        var typed = result.Assign<Person>();

        // Assert
        Assert.False(typed.Success);
        Assert.Single(typed.Exceptions);
        Assert.IsType<TypeConversionException>(typed.Exceptions[0]);
    }

    [Fact]
    public void GivenMissingPropertyWithIgnoreEnabled_WhenAssign_ThenSuccessIsTrue()
    {
        // Arrange
        var token = new TokenBuilder().WithName("NonExistent").Build();
        var options = new TokenizerOptions { IgnoreMissingProperties = true };
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithOptions(options).Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "value", new FileLocation()))
            .Build();

        // Act
        var typed = result.Assign<Person>();

        // Assert
        Assert.True(typed.Success);
        Assert.Empty(typed.Exceptions);
    }

    [Fact]
    public void GivenMissingPropertyWithIgnoreDisabled_WhenAssign_ThenSuccessIsFalseAndExceptionRecorded()
    {
        // Arrange
        var token = new TokenBuilder().WithName("NonExistent").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "value", new FileLocation()))
            .Build();

        // Act
        var typed = result.Assign<Person>();

        // Assert
        Assert.False(typed.Success);
        Assert.Single(typed.Exceptions);
        Assert.IsType<MissingMemberException>(typed.Exceptions[0]);
    }

    [Fact]
    public void GivenConcatenatableToken_WhenAssign_ThenSetsValue()
    {
        // Arrange — concatenation is handled in Stage 1 (TokenResult.AddMatch),
        // so the match list has a single pre-concatenated entry.
        var token = new TokenBuilder().WithName("Name").Build();
        token.CanConcatenate = true;
        token.ConcatenationString = ", ";
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice, Bob", new FileLocation()))
            .Build();

        // Act
        var typed = result.Assign<Person>();

        // Assert
        Assert.Equal("Alice, Bob", typed.Value.Name);
    }

    [Fact]
    public void GivenResult_WhenAssignCalledTwice_ThenOriginalIsUnmodified()
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
        Assert.Equal("Alice", first.Value.Name);
        Assert.Equal("Alice", second.Value.Name);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void GivenResultWithStageOneExceptions_WhenAssign_ThenStageOneExceptionsNotCopied()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice", new FileLocation()))
            .WithExceptions(new InvalidOperationException("stage 1 error"))
            .Build();

        // Act
        var typed = result.Assign<Person>();

        // Assert
        Assert.Single(result.Exceptions);  // Stage 1 exception stays on original
        Assert.Empty(typed.Exceptions);    // Not copied to typed result
        Assert.True(typed.Success);
    }

    [Fact]
    public void GivenSuccessfulResult_WhenAssignedWithTypeMismatch_ThenMatchingSuccessUnaffected()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Score").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "not-a-number", new FileLocation()))
            .Build();

        // Act & Assert
        Assert.True(result.Success);  // Matching succeeded
        var typed = result.Assign<Person>();
        Assert.False(typed.Success);  // Assignment failed
        Assert.True(result.Success);  // Original unchanged
    }
}
