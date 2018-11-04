using System.Linq;
using NUnit.Framework;
using Tokens.Transformers;

namespace Tokens.Parsers
{
    [TestFixture]
    public class TokenParserTests
    {
        private TokenParser parser;

        [SetUp]
        public void SetUp()
        {
            parser = new TokenParser();
        }

        [Test]
        public void TestParseToken()
        {
            var raw = new RawToken();
            raw.Name = "Token";
            raw.Preamble = "Preamble";

            var template = parser.Parse("Preamble{Token:ToDateTime(yyyy-MM-dd)}", "name");

            Assert.AreEqual("name", template.Name);
            Assert.AreEqual(1, template.Tokens.Count);

            var token = template.Tokens.First();

            Assert.AreEqual(1, token.Transformers.Count);

            var tokenOperator = token.Transformers.First();

            Assert.AreEqual(typeof(ToDateTimeTransformer), tokenOperator.OperatorType);
            Assert.AreEqual(1, tokenOperator.Parameters.Count);
            Assert.AreEqual("yyyy-MM-dd", tokenOperator.Parameters[0]);
        }
    }
}
