using Tokens.Builders;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TemplateTests : TokenizerTestBase
{
    public TemplateTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestHasTagWhenTrue()
    {
        var template = new TemplateBuilder().Build();
        template.AddTag("One");

        Assert.True(template.HasTag("One"));
    }

    [Fact]
    public void TestHasTagWhenTrueWhenDifferentCase()
    {
        var template = new TemplateBuilder().Build();
        template.AddTag("One");

        Assert.True(template.HasTag("one"));
    }

    [Fact]
    public void TestHasTagWhenTrueWhenMultipleTags()
    {
        var template = new TemplateBuilder().Build();
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        Assert.True(template.HasTag("two"));
    }

    [Fact]
    public void TestHasTagWhenMissing()
    {
        var template = new TemplateBuilder().Build();
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        Assert.False(template.HasTag("Four"));
    }

    [Fact]
    public void TestHasTagWhenNullInput()
    {
        var template = new TemplateBuilder().Build();
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        Assert.False(template.HasTag(null!));
    }

    [Fact]
    public void TestHasTagsWhenTrue()
    {
        var template = new TemplateBuilder().Build();
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        Assert.True(template.HasTags(["One", "Two"]));
    }

    [Fact]
    public void TestHasTagsWhenTrueAndDifferentCase()
    {
        var template = new TemplateBuilder().Build();
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        Assert.True(template.HasTags(["One", "three"]));
    }

    [Fact]
    public void TestHasTagsWhenHasMissingSomeTags()
    {
        var template = new TemplateBuilder().Build();
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");
        template.AddTag("Four");

        IList<string> missing;
        var hasTags = template.HasTags(["One", "Five"], out missing);

        Assert.False(hasTags);

        Assert.Single(missing);
        Assert.Equal("Five", missing[0]);
    }

    [Fact]
    public void TestHasTagsWhenHasNoTags()
    {
        var template = new TemplateBuilder().Build();

        IList<string> missing;
        var hasTags = template.HasTags(["One", "three"], out missing);

        Assert.False(hasTags);

        Assert.Equal(2, missing.Count);
        Assert.Equal("One", missing[0]);
        Assert.Equal("three", missing[1]);
    }

    [Fact]
    public void TestHasTagsWhenHasNullInput()
    {
        var template = new TemplateBuilder().Build();
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        IList<string> missing;
        var hasTags = template.HasTags(null!, out missing);

        Assert.False(hasTags);

        Assert.Empty(missing);
    }

    [Fact]
    public void TestHasTagsWhenHasEmptyInput()
    {
        var template = new TemplateBuilder().Build();
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        IList<string> missing;
        var hasTags = template.HasTags(Array.Empty<string>(), out missing);

        Assert.True(hasTags);

        Assert.Empty(missing);
    }

    [Fact]
    public void GivenNamedTemplate_WhenToString_ThenReturnsName()
    {
        // Arrange
        var template = new TemplateBuilder().WithName("invoice").Build();

        // Act
        var result = template.ToString();

        // Assert
        Assert.Equal("Template('invoice')", result);
    }

    [Fact]
    public void GivenUnnamedTemplate_WhenToString_ThenReturnsTokenCount()
    {
        // Arrange
        var template = new TemplateBuilder().Build();

        // Act
        var result = template.ToString();

        // Assert
        Assert.Equal("Template(0 tokens)", result);
    }

    [Fact]
    public void GivenTemplate_WhenConstructedWithOptions_ThenOptionsAreAccessible()
    {
        // Arrange
        var options = new TokenizerOptions { TrimTrailingWhiteSpace = false };

        // Act
        var template = new TemplateBuilder().WithName("test").WithOptions(options).Build();

        // Assert
        Assert.False(template.Options.TrimTrailingWhiteSpace);
    }

    [Fact]
    public void GivenTemplate_WhenCompiled_ThenHasContentBasedId()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Assert
        Assert.NotEqual(0UL, template.Id);
    }

    [Fact]
    public void GivenSamePattern_WhenCompiledTwice_ThenIdIsIdentical()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        const string pattern = "Name: {Name}";

        // Act
        var t1 = tokenizer.Compile(pattern).Template;
        var t2 = tokenizer.Compile(pattern).Template;

        // Assert
        Assert.Equal(t1.Id, t2.Id);
    }

    [Fact]
    public void GivenDifferentPatterns_WhenCompiled_ThenIdsAreDifferent()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var t1 = tokenizer.Compile("Name: {Name}").Template;
        var t2 = tokenizer.Compile("Age: {Age}").Template;

        // Assert
        Assert.NotEqual(t1.Id, t2.Id);
    }

}
