using Tokens.Builders;
using Xunit;

namespace Tokens;

public class StringComparisonTests
{
    [Theory]
    [InlineData("test", "TEST")]
    [InlineData("Test", "test")]
    public void GivenTemplate_WhenCheckingTagCaseInsensitive_ThenFindsTag(string tagToAdd, string tagToFind)
    {
        var template = new TemplateBuilder().WithName("content").Build();
        template.AddTag(tagToAdd);
        Assert.True(template.HasTag(tagToFind));
    }

    [Fact]
    public void GivenTemplate_WhenCheckingNonexistentTag_ThenReturnsFalse()
    {
        var template = new TemplateBuilder().WithName("content").Build();
        template.AddTag("existing");
        Assert.False(template.HasTag("nonexistent"));
    }
}
