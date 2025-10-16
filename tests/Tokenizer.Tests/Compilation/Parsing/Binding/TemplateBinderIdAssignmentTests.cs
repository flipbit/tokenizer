using System.Linq;
using Tokens.Compilation.Binders;
using Xunit;

namespace Tokens.Compilation.Parsing
{
    /// <summary>
    /// Tests for token Id assignment when binding AST to TemplateDefinition
    /// </summary>
    public class TemplateBinderIdAssignmentTests
    {
        [Fact]
        public void GivenMultipleTokens_WhenBinding_ThenAssignsSequentialIdsStartingAtOne()
        {
            // Arrange
            var parser = new TemplateParser();
            var doc = parser.Parse("{first}{second}{third}");

            // Act
            var def = TemplateBinder.Bind(doc);

            // Assert
            Assert.Equal(3, def.Tokens.Count);
            Assert.Equal(1, def.Tokens[0].Id);
            Assert.Equal(2, def.Tokens[1].Id);
            Assert.Equal(3, def.Tokens[2].Id);
            Assert.All(def.Tokens, t => Assert.Equal(0, t.DependsOnId));
        }

        [Fact]
        public void GivenRepeatingTokenWithMultilinePreambleTail_WhenBinding_ThenAssignsUniqueIdsToSplitTokens()
        {
            // Arrange: preamble has a non-whitespace segment before last newline and whitespace after it.
            var parser = new TemplateParser();
            var doc = parser.Parse("Start line\n    {item*}");

            // Act
            var def = TemplateBinder.Bind(doc);

            // Assert: token is expanded into two definitions with sequential non-zero Ids
            Assert.Equal(2, def.Tokens.Count);
            Assert.Equal("item", def.Tokens[0].Name);
            Assert.Equal("item", def.Tokens[1].Name);
            Assert.Equal(1, def.Tokens[0].Id);
            Assert.Equal(2, def.Tokens[1].Id);
            Assert.NotEqual(def.Tokens[0].Id, def.Tokens[1].Id);
            Assert.All(def.Tokens, t => Assert.Equal(0, t.DependsOnId));
        }
    }
}


