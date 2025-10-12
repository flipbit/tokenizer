using Xunit;

namespace Tokens;

public class TemplateCollectionTests
{
    private readonly TemplateCollection collection = new();

    [Fact]
    public void TestCollectionContainsTagWhenTrue()
    {
        var template = new Template();
        template.Tags.Add("One");

        collection.Add(template);

        Assert.True(collection.ContainsTag("One"));
    }

    [Fact]
    public void TestCollectionContainsTagWhenTrueAndDifferentCase()
    {
        var template = new Template();
        template.Tags.Add("One");

        collection.Add(template);

        Assert.True(collection.ContainsTag("one"));
    }

    [Fact]
    public void TestCollectionContainsTagWhenFalse()
    {
        var template = new Template();
        template.Tags.Add("One");

        collection.Add(template);

        Assert.False(collection.ContainsTag("two"));
    }

    [Fact]
    public void TestCollectionContainsAllTagsWhenTrue()
    {
        var template = new Template();
        template.Tags.Add("One");
        template.Tags.Add("Two");

        collection.Add(template);

        Assert.True(collection.ContainsAllTags("One", "Two"));
    }

    [Fact]
    public void TestCollectionContainsAllTagsWhenFalse()
    {
        var template = new Template();
        template.Tags.Add("One");
        template.Tags.Add("Two");

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

        Assert.Equal(0, collection.Count);
    }
}