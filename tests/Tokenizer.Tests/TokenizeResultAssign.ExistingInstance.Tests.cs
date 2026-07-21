using Tokens.Builders;
using Tokens.Enumerators;
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
}
