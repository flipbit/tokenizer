using Tokens.Enumerators;
using Xunit;

namespace Tokens.Compilation.Nodes;

public class SyntaxNodeTests
{
    private sealed class DummyNode : SyntaxNode
    {
        public DummyNode(FileLocation location, int start, int length) : base(location, start, length) { }
    }

    [Fact]
    public void GivenLocation_WhenConstructingNode_ThenLocationIsClonedAndOffsetsSet()
    {
        // Arrange
        var loc = new FileLocation();

        // Act
        var node = new DummyNode(loc, 10, 5);

        // Assert
        Assert.NotSame(loc, node.Location);
        Assert.Equal(loc.Line, node.Location.Line);
        Assert.Equal(loc.Column, node.Location.Column);
        Assert.Equal(loc.Paragraph, node.Location.Paragraph);
        Assert.Equal(10, node.Start);
        Assert.Equal(5, node.Length);
    }
}


