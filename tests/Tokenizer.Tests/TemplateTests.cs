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
        var template = new Template(string.Empty);
        template.AddTag("One");

        Assert.True(template.HasTag("One"));
    }

    [Fact]
    public void TestHasTagWhenTrueWhenDifferentCase()
    {
        var template = new Template(string.Empty);
        template.AddTag("One");

        Assert.True(template.HasTag("one"));
    }

    [Fact]
    public void TestHasTagWhenTrueWhenMultipleTags()
    {
        var template = new Template(string.Empty);
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        Assert.True(template.HasTag("two"));
    }

    [Fact]
    public void TestHasTagWhenMissing()
    {
        var template = new Template(string.Empty);
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        Assert.False(template.HasTag("Four"));
    }

    [Fact]
    public void TestHasTagWhenNullInput()
    {
        var template = new Template(string.Empty);
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        Assert.False(template.HasTag(null!));
    }

    [Fact]
    public void TestHasTagsWhenTrue()
    {
        var template = new Template(string.Empty);
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        Assert.True(template.HasTags(["One", "Two"]));
    }

    [Fact]
    public void TestHasTagsWhenTrueAndDifferentCase()
    {
        var template = new Template(string.Empty);
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        Assert.True(template.HasTags(["One", "three"]));
    }

    [Fact]
    public void TestHasTagsWhenHasMissingSomeTags()
    {
        var template = new Template(string.Empty);
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
        var template = new Template(string.Empty);

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
        var template = new Template(string.Empty);
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
        var template = new Template(string.Empty);
        template.AddTag("One");
        template.AddTag("Two");
        template.AddTag("Three");

        IList<string> missing;
        var hasTags = template.HasTags(new string[0], out missing);

        Assert.True(hasTags);

        Assert.Empty(missing);
    }

}
