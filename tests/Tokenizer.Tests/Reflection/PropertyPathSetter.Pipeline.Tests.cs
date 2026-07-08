using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable MA0048 // Scenario test: PropertyPathSetter.Pipeline.Tests.cs
#pragma warning disable MA0016 // Model classes intentionally use concrete List<T> for test clarity

namespace Tokens.Reflection;

public class PropertyPathSetterPipelineTests : TokenizerTestBase
{
    public PropertyPathSetterPipelineTests(ITestOutputHelper output) : base(output)
    {
    }

    // ── Model classes ───────────────────────────────────────────────────────────

    public sealed class ScalarTarget
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public int? Score { get; set; }
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
        public Guid Id { get; set; }
        public AddressTarget? Address { get; set; }
    }

    public sealed class AddressTarget
    {
        public string? City { get; set; }
    }

    public sealed class CollectionTarget
    {
        public string? Name { get; set; }
        public List<string>? Tags { get; set; }
        public string[]? Items { get; set; }
    }

    // ── Scalar property tests ───────────────────────────────────────────────────

    [Fact]
    public void GivenStringToken_WhenAssign_ThenStringPropertyPopulated()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "Alice", new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<ScalarTarget>();

        // Assert
        Assert.Equal("Alice", target.Name);
    }

    [Fact]
    public void GivenAlreadyTypedIntToken_WhenAssign_ThenIntPropertyPopulated()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Age").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, 42, new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<ScalarTarget>();

        // Assert
        Assert.Equal(42, target.Age);
    }

    [Fact]
    public void GivenStringIntToken_WhenAssign_ThenIntPropertyConverted()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Age").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "25", new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<ScalarTarget>();

        // Assert
        Assert.Equal(25, target.Age);
    }

    [Fact]
    public void GivenStringNullableIntToken_WhenAssign_ThenNullablePropertyPopulated()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Score").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "99", new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<ScalarTarget>();

        // Assert
        Assert.Equal(99, target.Score);
    }

    [Fact]
    public void GivenStringBoolToken_WhenAssign_ThenBoolPropertyConverted()
    {
        // Arrange
        var token = new TokenBuilder().WithName("IsActive").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "true", new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<ScalarTarget>();

        // Assert
        Assert.True(target.IsActive);
    }

    [Fact]
    public void GivenStringDecimalToken_WhenAssign_ThenDecimalPropertyConverted()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Price").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "19.99", new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<ScalarTarget>();

        // Assert
        Assert.Equal(19.99m, target.Price);
    }

    [Fact]
    public void GivenStringGuidToken_WhenAssign_ThenGuidPropertyConverted()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var token = new TokenBuilder().WithName("Id").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, guid.ToString(), new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<ScalarTarget>();

        // Assert
        Assert.Equal(guid, target.Id);
    }

    // ── Collection tests ────────────────────────────────────────────────────────

    [Fact]
    public void GivenMultipleTokensWithSameName_WhenAssignToList_ThenAllValuesCollected()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Tags").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(token, "tag1", new FileLocation()),
                new TokenMatch(token, "tag2", new FileLocation()),
                new TokenMatch(token, "tag3", new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<CollectionTarget>();

        // Assert
        Assert.Equal(new[] { "tag1", "tag2", "tag3" }, target.Tags);
    }

    [Fact]
    public void GivenMultipleTokensWithSameName_WhenAssignToArray_ThenAllValuesCollected()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Items").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(token, "a", new FileLocation()),
                new TokenMatch(token, "b", new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<CollectionTarget>();

        // Assert
        Assert.Equal(new[] { "a", "b" }, target.Items);
    }

    [Fact]
    public void GivenMixedScalarAndCollectionTokens_WhenAssign_ThenAllPropertiesPopulated()
    {
        // Arrange
        var nameToken = new TokenBuilder().WithName("Name").Build();
        var tagsToken = new TokenBuilder().WithName("Tags").Build();
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(nameToken, tagsToken).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(nameToken, "Alice", new FileLocation()),
                new TokenMatch(tagsToken, "dev", new FileLocation()),
                new TokenMatch(tagsToken, "ops", new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<CollectionTarget>();

        // Assert
        Assert.Equal("Alice", target.Name);
        Assert.Equal(new[] { "dev", "ops" }, target.Tags);
    }

    // ── Missing property tests ──────────────────────────────────────────────────

    [Fact]
    public void GivenUnknownTokenWithIgnoreMissingProperties_WhenAssign_ThenNoException()
    {
        // Arrange
        var nameToken = new TokenBuilder().WithName("Name").Build();
        var unknownToken = new TokenBuilder().WithName("Unknown").Build();
        var options = new TokenizerOptions { IgnoreMissingProperties = true };
        var template = new TemplateBuilder().WithName("Test")
            .WithTokens(nameToken, unknownToken).WithOptions(options).Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(
                new TokenMatch(nameToken, "Alice", new FileLocation()),
                new TokenMatch(unknownToken, "ignored", new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<ScalarTarget>();

        // Assert
        Assert.Equal("Alice", target.Name);
    }

    [Fact]
    public void GivenUnknownTokenWithIgnoreMissingPropertiesFalse_WhenAssign_ThenThrowsAssignmentFailedWithMissingMember()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Unknown").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "value", new FileLocation()))
            .Build();

        // Act & Assert
        var ex = Assert.Throws<AssignmentFailedException>(() => result.Assign<ScalarTarget>());
        Assert.Single(ex.Errors);
        Assert.IsType<MissingMemberException>(ex.Errors[0]);
    }

    // ── Nested property tests ───────────────────────────────────────────────────

    [Fact]
    public void GivenNestedPropertyPathToken_WhenAssign_ThenNestedPropertyPopulated()
    {
        // Arrange
        var token = new TokenBuilder().WithName("Address.City").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).WithDefaultOptions().Build();
        var result = new TokenizeResultBuilder().WithTemplate(template)
            .WithMatches(new TokenMatch(token, "London", new FileLocation()))
            .Build();

        // Act
        var target = result.Assign<ScalarTarget>();

        // Assert
        Assert.NotNull(target.Address);
        Assert.Equal("London", target.Address.City);
    }
}
