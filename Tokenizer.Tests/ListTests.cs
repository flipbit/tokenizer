using System.Linq;
using NUnit.Framework;

namespace Tokens
{
    [TestFixture]
    public class ListTests
    {
        private Tokenizer tokenizer;

        [SetUp]
        public void SetUp()
        {
            LogConfig.Init();

            tokenizer = new Tokenizer(new TokenizerOptions{ EnableLogging = true });
        }

        [Test]
        public void TestExtractSingleValue()
        {
            const string pattern = @"Domains:
{ DomainName : Repeating, IsDomainName }

{ SecondaryDomain }";
            const string input = @"Domains:
one.com
two.com
three.com

secondary.com";

            var results = tokenizer.Tokenize(pattern, input);

            var domains = results.Matches.Where(m => m.Token.Name == "DomainName").ToList();

            Assert.AreEqual(3, domains.Count);
            Assert.AreEqual("one.com", domains[0].Value);
            Assert.AreEqual("two.com", domains[1].Value);
            Assert.AreEqual("three.com", domains[2].Value);

            Assert.AreEqual("secondary.com", results.First("SecondaryDomain"));
        }
    }
}

