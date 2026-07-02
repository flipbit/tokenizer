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
        collection.Add(new Template("One", string.Empty));
        collection.Add(new Template("Two", string.Empty));
        collection.Add(new Template("Three", string.Empty));

        Assert.Equal(3, collection.Count);

        collection.Clear();

        Assert.Empty(collection);
    }

    [Fact]
    public void GivenCollectionWithTemplates_WhenEnumerated_ThenReturnsAllTemplates()
    {
        // Arrange
        var coll = new TemplateCollection();
        var template1 = new TemplateBuilder().WithName("first").WithContent("a").Build();
        var template2 = new TemplateBuilder().WithName("second").WithContent("b").Build();
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
        var coll = new TemplateCollection();
        coll.Add(new TemplateBuilder().WithName("alpha").WithContent("a").Build());
        coll.Add(new TemplateBuilder().WithName("beta").WithContent("b").Build());

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
}
