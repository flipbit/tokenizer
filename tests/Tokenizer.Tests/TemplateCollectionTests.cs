using Tokens.Builders;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TemplateCollectionTests : TokenizerTestBase
{
    public TemplateCollectionTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly TemplateCollection _collection = new();

    [Fact]
    public void TestCollectionContainsTagWhenTrue()
    {
        var template = new TemplateBuilder().WithId(1).Build();
        template.AddTag("One");

        _collection.Add(template);

        Assert.True(_collection.ContainsTag("One"));
    }

    [Fact]
    public void TestCollectionContainsTagWhenTrueAndDifferentCase()
    {
        var template = new TemplateBuilder().WithId(2).Build();
        template.AddTag("One");

        _collection.Add(template);

        Assert.True(_collection.ContainsTag("one"));
    }

    [Fact]
    public void TestCollectionContainsTagWhenFalse()
    {
        var template = new TemplateBuilder().WithId(3).Build();
        template.AddTag("One");

        _collection.Add(template);

        Assert.False(_collection.ContainsTag("two"));
    }

    [Fact]
    public void TestCollectionContainsAllTagsWhenTrue()
    {
        var template = new TemplateBuilder().WithId(4).Build();
        template.AddTag("One");
        template.AddTag("Two");

        _collection.Add(template);

        Assert.True(_collection.ContainsAllTags("One", "Two"));
    }

    [Fact]
    public void TestCollectionContainsAllTagsWhenFalse()
    {
        var template = new TemplateBuilder().WithId(5).Build();
        template.AddTag("One");
        template.AddTag("Two");

        _collection.Add(template);

        Assert.False(_collection.ContainsAllTags("One", "Two", "Three"));
    }

    [Fact]
    public void TestCollectionCount()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        _collection.Add(tokenizer.Compile("One: {One}").Template);
        _collection.Add(tokenizer.Compile("Two: {Two}").Template);
        _collection.Add(tokenizer.Compile("Three: {Three}").Template);

        // Assert
        Assert.Equal(3, _collection.Count);

        _collection.Clear();

        Assert.Empty(_collection);
    }

    [Fact]
    public void GivenCollectionWithTemplates_WhenEnumerated_ThenReturnsAllTemplates()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var coll = new TemplateCollection();
        var template1 = tokenizer.Compile("First: {First}").Template;
        template1.Name = "first";
        var template2 = tokenizer.Compile("Second: {Second}").Template;
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
        var alpha = tokenizer.Compile("Alpha: {Alpha}").Template;
        alpha.Name = "alpha";
        var beta = tokenizer.Compile("Beta: {Beta}").Template;
        beta.Name = "beta";
        coll.Add(alpha);
        coll.Add(beta);

        // Act
        var names = coll.Select(t => t.Name).Order().ToList();

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
        var template = tokenizer.Compile("Name: {Name}").Template;
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
        var template = tokenizer.Compile("Name: {Name}").Template;
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
        var t1 = tokenizer.Compile("Name: {Name}").Template;
        t1.Name = "first";
        var t2 = tokenizer.Compile("Name: {Name}").Template;
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
