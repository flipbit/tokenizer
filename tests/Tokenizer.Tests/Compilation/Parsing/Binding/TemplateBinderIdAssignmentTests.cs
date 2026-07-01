using System.Linq;
using Tokens.Compilation;
using Xunit;

namespace Tokens.Compilation.Parsing;

/// <summary>
/// Tests for token Id assignment during template construction.
/// IDs are auto-assigned by Template.AddToken(), not by TemplateBinder.
/// </summary>
public class TemplateBinderIdAssignmentTests
{
    [Fact]
    public void GivenMultipleTokens_WhenParsing_ThenAssignsSequentialIdsStartingAtOne()
    {
        // Arrange & Act
        var parser = new TokenParser();
        var template = parser.Parse("{first}{second}{third}");

        // Assert
        var tokens = template.Tokens.ToList();
        Assert.Equal(3, tokens.Count);
        Assert.Equal(1, tokens[0].Id);
        Assert.Equal(2, tokens[1].Id);
        Assert.Equal(3, tokens[2].Id);
        Assert.All(tokens, t => Assert.Equal(-1, t.DependsOnId));
    }

    [Fact]
    public void GivenRepeatingTokenWithMultilinePreambleTail_WhenParsing_ThenAssignsUniqueIdsToSplitTokens()
    {
        // Arrange & Act
        var parser = new TokenParser();
        var template = parser.Parse("Start line\n    {item*}");

        // Assert: token is expanded into two definitions with sequential non-zero Ids
        var tokens = template.Tokens.ToList();
        Assert.Equal(2, tokens.Count);
        Assert.Equal("item", tokens[0].Name);
        Assert.Equal("item", tokens[1].Name);
        Assert.Equal(1, tokens[0].Id);
        Assert.Equal(2, tokens[1].Id);
        Assert.NotEqual(tokens[0].Id, tokens[1].Id);
        // Non-repeating token has no dependency; repeating split depends on its non-repeating counterpart
        Assert.Equal(-1, tokens[0].DependsOnId);
        Assert.Equal(tokens[0].Id, tokens[1].DependsOnId);
    }
}
