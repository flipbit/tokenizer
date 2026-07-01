using System.Linq;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Compilation.Nodes;

public class FrontMatterAstTests
{
    [Fact]
    public void GivenKeyValue_WhenCreatingEntry_ThenStoresKeyAndValue()
    {
        // Arrange
        var loc = new FileLocation();

        // Act
        var entry = new FrontMatterEntry(loc, 0, 7, "name", "Template");

        // Assert
        Assert.Equal("name", entry.Key);
        Assert.Equal("Template", entry.Value);
    }

    [Fact]
    public void GivenEntries_WhenBuildingBlock_ThenPreservesOrder()
    {
        // Arrange
        var loc = new FileLocation();
        var e1 = new FrontMatterEntry(loc, 0, 5, "name", "A");
        var e2 = new FrontMatterComment(loc, 5, 2, "# comment");

        // Act
        var block = new FrontMatterBlock(loc, 0, 10, new SyntaxNode[] { e1, e2 });

        // Assert
        Assert.Equal(2, block.Entries.Count);
        Assert.Same(e1, block.Entries.First());
        Assert.Same(e2, block.Entries.Last());
    }

    [Fact]
    public void GivenFrontMatterAndContent_WhenCreatingDocument_ThenPropertiesSet()
    {
        // Arrange
        var loc = new FileLocation();
        var fm = new FrontMatterBlock(loc, 0, 10, new SyntaxNode[0]);
        var content = new[] { new ContentNode(loc, 10, 20) };

        // Act
        var doc = new TemplateDocument(loc, 0, 30, fm, content);

        // Assert
        Assert.Same(fm, doc.FrontMatter);
        Assert.Single(doc.Content);
    }
}


