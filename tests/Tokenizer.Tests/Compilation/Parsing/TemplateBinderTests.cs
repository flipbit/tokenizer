using Xunit;

namespace Tokens.Compilation.Parsing
{
    /// <summary>
    /// Tests for template binding from AST to TemplateDefinition
    /// </summary>
    public class TemplateBinderTests
    {
        [Fact]
        public void GivenSimpleToken_WhenBinding_ThenMapsNameModifiersAndValue()
        {
            // Arrange
            var parser = new TemplateParser();
            var doc = parser.Parse("{name?=\"x\"}");

            // Act
            var def = TemplateBinder.Bind(doc);

            // Assert
            var tok = Assert.Single(def.Tokens);
            Assert.Equal("name", tok.Name);
            Assert.True(tok.Optional);
            Assert.Equal("x", tok.Value);
        }

        [Fact]
        public void GivenDecorators_WhenBinding_ThenMapsDecoratorsAndArgs()
        {
            var parser = new TemplateParser();
            var doc = parser.Parse("{id:trim:regex(\"[A-Z]+\", 3)}");

            var def = TemplateBinder.Bind(doc);
            var tok = Assert.Single(def.Tokens);
            Assert.Equal(2, tok.Decorators.Count);
            Assert.Equal("trim", tok.Decorators[0].Name);
            Assert.Equal("regex", tok.Decorators[1].Name);
            Assert.Equal(2, tok.Decorators[1].Args.Count);
            Assert.Equal("[A-Z]+", tok.Decorators[1].Args[0]);
            Assert.Equal("3", tok.Decorators[1].Args[1]);
        }

        [Fact]
        public void GivenPreambleBeforeToken_WhenBinding_ThenAttachesToToken()
        {
            var parser = new TemplateParser();
            var doc = parser.Parse("Hello {name}");

            var def = TemplateBinder.Bind(doc);
            var tok = Assert.Single(def.Tokens);
            Assert.Contains("Hello", tok.Preamble);
        }

        [Fact]
        public void GivenTextBetweenTokens_WhenBinding_ThenAttachesToSecondToken()
        {
            // Arrange
            var parser = new TemplateParser();
            var doc = parser.Parse("{first} middle {second}");

            // Act
            var def = TemplateBinder.Bind(doc);

            // Assert
            Assert.Equal(2, def.Tokens.Count);
            Assert.Equal("first", def.Tokens[0].Name);
            Assert.Equal("second", def.Tokens[1].Name);
            Assert.Contains("middle", def.Tokens[1].Preamble);
        }

        [Fact]
        public void GivenTrailingText_WhenBinding_ThenCreatesTerminalToken()
        {
            // Arrange
            var parser = new TemplateParser();
            var doc = parser.Parse("{name} trailing");

            // Act
            var def = TemplateBinder.Bind(doc);

            // Assert
            Assert.Equal(2, def.Tokens.Count);
            Assert.Equal("name", def.Tokens[0].Name);
            Assert.Equal(string.Empty, def.Tokens[1].Name);
            Assert.Contains("trailing", def.Tokens[1].Preamble);
        }

        [Fact]
        public void GivenMultipleTokens_WhenBinding_ThenPreservesOrder()
        {
            // Arrange
            var parser = new TemplateParser();
            var doc = parser.Parse("{first}{second}{third}");

            // Act
            var def = TemplateBinder.Bind(doc);

            // Assert
            Assert.Equal(3, def.Tokens.Count);
            Assert.Equal("first", def.Tokens[0].Name);
            Assert.Equal("second", def.Tokens[1].Name);
            Assert.Equal("third", def.Tokens[2].Name);
        }

        [Fact]
        public void GivenComplexTemplate_WhenBinding_ThenProducesValidDefinition()
        {
            // Arrange
            var parser = new TemplateParser();
            var doc = parser.Parse("Start {name:trim} middle {id?=123:format(00)} end");

            // Act
            var def = TemplateBinder.Bind(doc);

            // Assert
            Assert.Equal(3, def.Tokens.Count);
            Assert.Equal("name", def.Tokens[0].Name);
            Assert.Single(def.Tokens[0].Decorators);
            Assert.Equal("id", def.Tokens[1].Name);
            Assert.True(def.Tokens[1].Optional);
            Assert.Equal("123", def.Tokens[1].Value);
        }

        [Fact]
        public void GivenEmptyTemplate_WhenBinding_ThenReturnsEmptyDefinition()
        {
            // Arrange
            var parser = new TemplateParser();
            var doc = parser.Parse("");

            // Act
            var def = TemplateBinder.Bind(doc);

            // Assert
            Assert.Empty(def.Tokens);
        }

        [Fact]
        public void GivenTextOnly_WhenBinding_ThenCreatesTrailingToken()
        {
            // Arrange
            var parser = new TemplateParser();
            var doc = parser.Parse("Just text");

            // Act
            var def = TemplateBinder.Bind(doc);

            // Assert
            Assert.Single(def.Tokens);
            Assert.Equal(string.Empty, def.Tokens[0].Name);
            Assert.Equal("Just text", def.Tokens[0].Preamble);
        }
    }
}



