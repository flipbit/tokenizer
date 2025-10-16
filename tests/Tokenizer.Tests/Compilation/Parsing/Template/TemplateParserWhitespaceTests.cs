using System.Linq;
using Tokens.Compilation.Parsing;
using Xunit;
using Tokens.Compilation.Binders;

namespace Tokens.Tests.Compilation.Parsing.Template
{
    /// <summary>
    /// Tests for whitespace and line ending handling
    /// </summary>
    public class TemplateParserWhitespaceTests
    {
        [Fact]
        public void GivenFrontMatterTrimPreamble_WhenParsing_ThenTrimsPreambleBeforeNewLine()
        {
            // Arrange
            var input = "---\nTrimPreambleBeforeNewLine: true\n---\nShould be trimmed\r\nPreamble: { Name }";
            var parser = new TemplateParser();

            // Act
            var doc = parser.Parse(input);
            var def = TemplateBinder.Bind(doc);

            // Assert
            Assert.Single(def.Tokens);
            Assert.Equal("Preamble: ", def.Tokens[0].Preamble);
        }
        private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

        [Fact]
        public void GivenTokenWithWindowsLineEndings_WhenParsing_ThenConvertsToUnixLineEndings()
        {
            // Arrange & Act
            var template = _parser.Parse("Preamble\r\n{TokenName}\r\nPostamble");

            // Assert
            Assert.Equal(2, template.Tokens.Count);

            var token = template.Tokens.First();
            Assert.Equal("Preamble\n", token.Preamble);
            Assert.Equal("TokenName", token.Name);

            var second = template.Tokens[1];
            Assert.Equal(string.Empty, second.Name);
            Assert.Equal("\nPostamble", second.Preamble);
        }

        [Fact]
        public void GivenTokenWithUnixLineEndings_WhenParsing_ThenPreservesUnixLineEndings()
        {
            // Arrange & Act
            var template = _parser.Parse("Preamble\n{TokenName}\nPostamble with linefeed: \r\n");

            // Assert
            Assert.Equal(2, template.Tokens.Count);

            var token = template.Tokens.First();
            Assert.Equal("Preamble\n", token.Preamble);
            Assert.Equal("TokenName", token.Name);

            var second = template.Tokens[1];
            Assert.Equal(string.Empty, second.Name);
            Assert.Equal("\nPostamble with linefeed: \n", second.Preamble);
        }
    }
}
