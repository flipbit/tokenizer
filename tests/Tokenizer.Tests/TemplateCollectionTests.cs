using Tokens.Builders;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TemplateCollectionTests : TokenizerTestBase
{
    public TemplateCollectionTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly TemplateCollection collection = new();

    [Fact]
    public void TestCollectionContainsTagWhenTrue()
    {
        var template = new Template(string.Empty);
        template.AddTag("One");

        collection.Add(template);

        Assert.True(collection.ContainsTag("One"));
    }

    [Fact]
    public void TestCollectionContainsTagWhenTrueAndDifferentCase()
    {
        var template = new Template(string.Empty);
        template.AddTag("One");

        collection.Add(template);

        Assert.True(collection.ContainsTag("one"));
    }

    [Fact]
    public void TestCollectionContainsTagWhenFalse()
    {
        var template = new Template(string.Empty);
        template.AddTag("One");

        collection.Add(template);

        Assert.False(collection.ContainsTag("two"));
    }

    [Fact]
    public void TestCollectionContainsAllTagsWhenTrue()
    {
        var template = new Template(string.Empty);
        template.AddTag("One");
        template.AddTag("Two");

        collection.Add(template);

        Assert.True(collection.ContainsAllTags("One", "Two"));
    }

    [Fact]
    public void TestCollectionContainsAllTagsWhenFalse()
    {
        var template = new Template(string.Empty);
        template.AddTag("One");
        template.AddTag("Two");

        collection.Add(template);

        Assert.False(collection.ContainsAllTags("One", "Two", "Three"));
    }

    [Fact]
    public void TestCollectionCount()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        collection.Add(tokenizer.Compile("One: {One}"));
        collection.Add(tokenizer.Compile("Two: {Two}"));
        collection.Add(tokenizer.Compile("Three: {Three}"));

        // Assert
        Assert.Equal(3, collection.Count);

        collection.Clear();

        Assert.Empty(collection);
    }

    [Fact]
    public void GivenCollectionWithTemplates_WhenEnumerated_ThenReturnsAllTemplates()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var coll = new TemplateCollection();
        var template1 = tokenizer.Compile("First: {First}");
        template1.Name = "first";
        var template2 = tokenizer.Compile("Second: {Second}");
        template2.Name = "second";
        coll.Add(template1);
        coll.Add(template2);

        // Act
        var templates = coll.ToList();

        // Assert
        Assert.Equal(2, templates.Count);
        Assert.Contains(templates, t => t.Name == "first");
        Assert.Contains(templates, t => t.Name == "second");
    }

    [Fact]
    public void GivenEmptyCollection_WhenEnumerated_ThenReturnsEmpty()
    {
        // Arrange
        var coll = new TemplateCollection();

        // Act
        var templates = coll.ToList();

        // Assert
        Assert.Empty(templates);
    }

    [Fact]
    public void GivenCollection_WhenUsedWithLinq_ThenSupportsLinqOperations()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var coll = new TemplateCollection();
        var alpha = tokenizer.Compile("Alpha: {Alpha}");
        alpha.Name = "alpha";
        var beta = tokenizer.Compile("Beta: {Beta}");
        beta.Name = "beta";
        coll.Add(alpha);
        coll.Add(beta);

        // Act
        var names = coll.Select(t => t.Name).OrderBy(n => n).ToList();

        // Assert
        Assert.Equal(new[] { "alpha", "beta" }, names);
    }

    [Fact]
    public void GivenCollection_WhenCastToInterface_ThenIsIReadOnlyCollection()
    {
        // Arrange & Act
        IReadOnlyCollection<Template> coll = new TemplateCollection();

        // Assert
        Assert.NotNull(coll);
        Assert.Empty(coll);
    }

    [Fact]
    public void GivenTemplateWithId_WhenAdded_ThenCanRetrieveById()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var template = tokenizer.Compile("Name: {Name}");
        var coll = new TemplateCollection();

        // Act
        coll.Add(template);

        // Assert
        Assert.True(coll.TryGet(template.Id, out var retrieved));
        Assert.Same(template, retrieved);
    }

    [Fact]
    public void GivenTemplateWithName_WhenAdded_ThenCanRetrieveByName()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var template = tokenizer.Compile("Name: {Name}");
        template.Name = "my-template";
        var coll = new TemplateCollection();

        // Act
        coll.Add(template);

        // Assert
        Assert.NotNull(coll.Get("my-template"));
    }

    [Fact]
    public void GivenSamePatternAddedTwice_WhenSecondHasDifferentName_ThenLastWriteWins()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var t1 = tokenizer.Compile("Name: {Name}");
        t1.Name = "first";
        var t2 = tokenizer.Compile("Name: {Name}");
        t2.Name = "second";
        var coll = new TemplateCollection();

        // Act
        coll.Add(t1);
        coll.Add(t2);

        // Assert — same Id, so last write wins
        Assert.Single(coll);
        Assert.Equal("second", coll.First().Name);
    }
}
