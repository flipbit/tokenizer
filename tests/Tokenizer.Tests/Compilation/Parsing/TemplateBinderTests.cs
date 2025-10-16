using Xunit;

namespace Tokens.Compilation.Parsing
{
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
    }
}



