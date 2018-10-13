using System.Linq;
using NUnit.Framework;

namespace Tokens
{
    [TestFixture]
    public class FlatTokenParserTests
    {
        private FlatTokenParser parser;

        [SetUp]
        public void SetUp()
        {
            parser = new FlatTokenParser();
        }

        [Test]
        public void TestParseEmptyString()
        {
            var tokens = parser.Parse(string.Empty);

            Assert.AreEqual(0, tokens.Count);
        }

        [Test]
        public void TestParseNullString()
        {
            var tokens = parser.Parse(null);

            Assert.AreEqual(0, tokens.Count);
        }

        [Test]
        public void TestParseSingleToken()
        {
            var tokens = parser.Parse("This is the preamble{This is the token}");

            Assert.AreEqual(1, tokens.Count);

            var token = tokens.First();

            Assert.AreEqual("This is the preamble", token.Preamble);
            Assert.AreEqual("This is the token", token.Name);
            Assert.IsFalse(token.Optional);
            Assert.IsFalse(token.TerminateOnNewline);
            Assert.IsFalse(token.Repeating);
        }
    }
}
