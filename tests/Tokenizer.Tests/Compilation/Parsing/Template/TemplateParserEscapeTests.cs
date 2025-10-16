using System.Linq;
using Tokens.Compilation.Parsing;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Tests.Compilation.Parsing.Template
{
    /// <summary>
    /// Tests for escape sequence handling ({{ and }})
    /// </summary>
    public class TemplateParserEscapeTests
    {
        private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

        [Fact]
        public void GivenTokenWithEscapedBrackets_WhenParsing_ThenUnescapesBrackets()
        {
            // Arrange & Act
            var template = _parser.Parse("This {{is}} the preamble{TokenName}");

            // Assert
            Assert.Single(template.Tokens);

            var token = template.Tokens.First();

            Assert.Equal("This {is} the preamble", token.Preamble);
            Assert.Equal("TokenName", token.Name);
            Assert.False(token.Optional);
            Assert.False(token.TerminateOnNewline);
            Assert.False(token.Repeating);
        }

        [Fact]
        public void GivenTokenWithUnescapedClosingBracket_WhenParsing_ThenThrowsParsingException()
        {
            // Arrange, Act & Assert
            try
            {
                _parser.Parse("This {{is} the preamble{TokenName}");

                Assert.Fail("Should of thrown.");
            }
            catch (ParsingException e)
            {
                Assert.Equal(1, e.Line);
                Assert.Equal(10, e.Column);
            }
        }
    }
}
