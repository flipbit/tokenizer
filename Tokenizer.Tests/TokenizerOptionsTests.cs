using NUnit.Framework;
using System.Linq;
using Tokens.Parsers;

namespace Tokens
{
    [TestFixture]
    public class TokenizerOptionsTests
    {
        [Test]
        public void TestTrimBeforePreambleWhenTrue()
        {
            const string content = "Should be trimmed\r\nPreamble: { First } Second: { Second }";

            var parser = new TokenParser();

            parser.Options.TrimPreambleBeforeNewLine = true;

            var template = parser.Parse(content);

            Assert.AreEqual(2, template.Tokens.Count);
            Assert.AreEqual("Preamble: ", template.Tokens.ElementAt(0).Preamble);
            Assert.AreEqual("Second: ", template.Tokens.ElementAt(1).Preamble);
        } 

        [Test]
        public void TestTrimBeforePreambleWhenFalse()
        {
            const string content = "Should not be trimmed\r\nPreamble: { First } Second: { Second }";

            var parser = new TokenParser();

            parser.Options.TrimPreambleBeforeNewLine = false;

            var template = parser.Parse(content);

            Assert.AreEqual(2, template.Tokens.Count);
            Assert.AreEqual("Should not be trimmed\nPreamble: ", template.Tokens.ElementAt(0).Preamble);
            Assert.AreEqual("Second: ", template.Tokens.ElementAt(1).Preamble);
        } 

        [Test]
        public void TestTrimBeforePreambleWhenSetFromFrontMatter()
        {
            const string content = "---\nTrimPreambleBeforeNewLine: true\n---\nShould be trimmed\r\nPreamble: { First } Second: { Second }";

            var parser = new TokenParser();

            parser.Options.TrimPreambleBeforeNewLine = false;

            var template = parser.Parse(content);

            Assert.AreEqual(2, template.Tokens.Count);
            Assert.AreEqual("Preamble: ", template.Tokens.ElementAt(0).Preamble);
            Assert.AreEqual("Second: ", template.Tokens.ElementAt(1).Preamble);
            Assert.IsTrue(template.Options.TrimPreambleBeforeNewLine);
        } 
    }
}
