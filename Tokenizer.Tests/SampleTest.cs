using System;
using System.IO;
using NUnit.Framework;
using Tokens.Samples.Classes;

namespace Tokens
{
    [TestFixture]
    public class SampleTest
    {
        private Tokenizer tokenizer;

        [SetUp]
        public void SetUp()
        {
            tokenizer = new Tokenizer();
        }

        [Test]
        public void TestParseUkWhoisData()
        {
            var pattern = File.ReadAllText("..\\..\\Samples\\Patterns\\nominet.txt");
            var input = File.ReadAllText("..\\..\\Samples\\Data\\bbc.co.uk.txt");

            var result = tokenizer.Parse<WhoisRecord>(pattern, input);

            Assert.IsNotNull(result.Value);
            Assert.AreEqual("bbc.co.uk", result.Value.Domain);
            Assert.AreEqual("British Broadcasting Corporation", result.Value.Registrant.Organization);
            Assert.AreEqual("British Broadcasting Corporation", result.Value.Registrant.Street);
            Assert.AreEqual("Broadcasting House", result.Value.Registrant.City);
            Assert.AreEqual("Portland Place", result.Value.Registrant.State);
            Assert.AreEqual("London", result.Value.Registrant.PostalCode);
            Assert.AreEqual("British Broadcasting Corporation", result.Value.RegistrarName);
            Assert.AreEqual("http://www.bbc.co.uk", result.Value.RegistrarUrl);
            Assert.AreEqual(1, result.Value.NameServers.Count);
            Assert.AreEqual("ns1.rbsov.bbc.co.uk       212.58.241.67", result.Value.NameServers[0]);
            Assert.AreEqual(new DateTime(2014, 12, 13), result.Value.ExpirationDate);
            Assert.AreEqual(new DateTime(2014, 6, 12), result.Value.ModificationDate);
        }
    }
}
