using Tokens.Compilation.Binders;
using Tokens.Compilation.Parsing;
using Xunit;

namespace Tokens.Tests.Compilation.Parsing.Binding
{
    /// <summary>
    /// Tests for modifier binding logic from AST to TokenDefinition
    /// </summary>
    public class TemplateBinderModifierTests
    {
        [Fact]
        public void GivenFrontMatterTerminateOnNewLine_WhenBinding_ThenAllTokensTerminateOnNewline()
        {
            // Arrange
            var input = "---\nTerminateOnNewLine: true\n---\nA: {a}\nB: {b}";
            var parser = new TemplateParser();
            var doc = parser.Parse(input);

            // Act
            var def = TemplateBinder.Bind(doc);

            // Assert
            Assert.Equal(2, def.Tokens.Count);
            Assert.All(def.Tokens, t => Assert.True(t.TerminateOnNewline));
        }
        private readonly ITemplateDefinitionParser _parser = new AstTemplateDefinitionParser();

        [Fact]
        public void GivenOptionalModifier_WhenBinding_ThenSetsOptionalFlag()
        {
            // Arrange & Act
            var template = _parser.Parse("{name?}");

            // Assert
            var token = Assert.Single(template.Tokens);
            Assert.True(token.Optional);
            Assert.False(token.Required);
        }

        [Fact]
        public void GivenRequiredModifier_WhenBinding_ThenSetsRequiredFlag()
        {
            // Arrange & Act
            var template = _parser.Parse("{name!}");

            // Assert
            var token = Assert.Single(template.Tokens);
            Assert.True(token.Required);
            Assert.False(token.Optional);
        }

        [Fact]
        public void GivenRepeatingModifier_WhenBinding_ThenSetsRepeatingAndOptional()
        {
            // Arrange & Act
            var template = _parser.Parse("{name*}");

            // Assert
            var token = Assert.Single(template.Tokens);
            Assert.True(token.Repeating);
            Assert.True(token.Optional);
        }

        [Fact]
        public void GivenTerminateModifier_WhenBinding_ThenSetsTerminateOnNewline()
        {
            // Arrange & Act
            var template = _parser.Parse("{name$}");

            // Assert
            var token = Assert.Single(template.Tokens);
            Assert.True(token.TerminateOnNewline);
        }

        [Fact]
        public void GivenAllModifiers_WhenBinding_ThenSetsAllFlags()
        {
            // Arrange & Act
            var template = _parser.Parse("{name?*$}");

            // Assert
            var token = Assert.Single(template.Tokens);
            Assert.True(token.Optional);
            Assert.True(token.Repeating);
            Assert.True(token.TerminateOnNewline);
        }

        [Fact]
        public void GivenNoModifiers_WhenBinding_ThenUsesDefaults()
        {
            // Arrange & Act
            var template = _parser.Parse("{name}");

            // Assert
            var token = Assert.Single(template.Tokens);
            Assert.False(token.Optional);
            Assert.False(token.Required);
            Assert.False(token.Repeating);
            Assert.False(token.TerminateOnNewline);
        }

        [Fact]
        public void GivenOptionalAndTerminate_WhenBinding_ThenSetsBothFlags()
        {
            // Arrange & Act
            var template = _parser.Parse("{name?$}");

            // Assert
            var token = Assert.Single(template.Tokens);
            Assert.True(token.Optional);
            Assert.True(token.TerminateOnNewline);
        }

        [Fact]
        public void GivenRepeatingImpliesOptional_WhenBinding_ThenOptionalIsSet()
        {
            // Arrange & Act
            var template = _parser.Parse("{name*}");

            // Assert
            var token = Assert.Single(template.Tokens);
            Assert.True(token.Repeating);
            Assert.True(token.Optional);
        }

        [Fact]
        public void GivenOnceDecorator_WhenBinding_ThenSetsConsiderOnce()
        {
            // Arrange & Act
            var template = _parser.Parse("{name : Once}");

            // Assert
            var token = Assert.Single(template.Tokens);
            Assert.True(token.ConsiderOnce);
            Assert.Empty(token.Decorators);
        }
    }
}
