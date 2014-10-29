using System;
using System.IO;
using System.Linq;
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
            Assert.AreEqual(3, result.Value.NameServers.Count);
            Assert.AreEqual("ns1.rbsov.bbc.co.uk       212.58.241.67", result.Value.NameServers[0]);
            Assert.AreEqual("ns1.tcams.bbc.co.uk       212.72.49.3", result.Value.NameServers[1]);
            Assert.AreEqual("ns1.thdow.bbc.co.uk       212.58.240.163", result.Value.NameServers[2]);
            Assert.AreEqual(new DateTime(2014, 12, 13), result.Value.ExpirationDate);
            Assert.AreEqual(new DateTime(2014, 6, 12), result.Value.ModificationDate);
        }

        [Test]
        public void TestParseIanaServerDataData()
        {
            var pattern = File.ReadAllText("..\\..\\Samples\\Patterns\\iana.txt");
            var input = File.ReadAllText("..\\..\\Samples\\Data\\com.txt");

            tokenizer.Options.ThrowExceptionOnMissingProperty = true;

            var result = tokenizer.Parse<WhoisServer>(pattern, input);

            Assert.IsNotNull(result.Value);
            Assert.AreEqual("com", result.Value.TLD);
            Assert.AreEqual("VeriSign Global Registry Services", result.Value.Organization.Name);
            Assert.AreEqual(3, result.Value.Organization.Address.Count);
            Assert.AreEqual("12061 Bluemont Way", result.Value.Organization.Address[0]);
            Assert.AreEqual("Reston Virginia 20190", result.Value.Organization.Address[1]);
            Assert.AreEqual("United States", result.Value.Organization.Address[2]);
            Assert.AreEqual("Registry Customer Service", result.Value.AdminContact.Name);
            Assert.AreEqual("VeriSign Global Registry Services", result.Value.AdminContact.Organization);
            Assert.AreEqual(3, result.Value.AdminContact.Address.Count);
            Assert.AreEqual("12061 Bluemont Way", result.Value.AdminContact.Address[0]);
            Assert.AreEqual("Reston Virginia 20190", result.Value.AdminContact.Address[1]);
            Assert.AreEqual("United States", result.Value.AdminContact.Address[2]);
            Assert.AreEqual("+1 703 925-6999", result.Value.AdminContact.PhoneNumber);
            Assert.AreEqual("+1 703 948 3978", result.Value.AdminContact.FaxNumber);
            Assert.AreEqual("info@verisign-grs.com", result.Value.AdminContact.Email);
            Assert.AreEqual("Registry Customer Service", result.Value.TechContact.Name);
            Assert.AreEqual("VeriSign Global Registry Services", result.Value.TechContact.Organization);
            Assert.AreEqual(3, result.Value.TechContact.Address.Count);
            Assert.AreEqual("12061 Bluemont Way", result.Value.TechContact.Address[0]);
            Assert.AreEqual("Reston Virginia 20190", result.Value.TechContact.Address[1]);
            Assert.AreEqual("United States", result.Value.TechContact.Address[2]);
            Assert.AreEqual("+1 703 925-6999", result.Value.TechContact.PhoneNumber);
            Assert.AreEqual("+1 703 948 3978", result.Value.TechContact.FaxNumber);
            Assert.AreEqual("info@verisign-grs.com", result.Value.TechContact.Email);
            Assert.AreEqual(13, result.Value.NameServers.Count);
            Assert.AreEqual("A.GTLD-SERVERS.NET 192.5.6.30 2001:503:a83e:0:0:0:2:30", result.Value.NameServers[0]);
            Assert.AreEqual("B.GTLD-SERVERS.NET 192.33.14.30 2001:503:231d:0:0:0:2:30", result.Value.NameServers[1]);
            Assert.AreEqual("C.GTLD-SERVERS.NET 192.26.92.30", result.Value.NameServers[2]);
            Assert.AreEqual("D.GTLD-SERVERS.NET 192.31.80.30", result.Value.NameServers[3]);
            Assert.AreEqual("E.GTLD-SERVERS.NET 192.12.94.30", result.Value.NameServers[4]);
            Assert.AreEqual("F.GTLD-SERVERS.NET 192.35.51.30", result.Value.NameServers[5]);
            Assert.AreEqual("G.GTLD-SERVERS.NET 192.42.93.30", result.Value.NameServers[6]);
            Assert.AreEqual("H.GTLD-SERVERS.NET 192.54.112.30", result.Value.NameServers[7]);
            Assert.AreEqual("I.GTLD-SERVERS.NET 192.43.172.30", result.Value.NameServers[8]);
            Assert.AreEqual("J.GTLD-SERVERS.NET 192.48.79.30", result.Value.NameServers[9]);
            Assert.AreEqual("K.GTLD-SERVERS.NET 192.52.178.30", result.Value.NameServers[10]);
            Assert.AreEqual("L.GTLD-SERVERS.NET 192.41.162.30", result.Value.NameServers[11]);
            Assert.AreEqual("M.GTLD-SERVERS.NET 192.55.83.30", result.Value.NameServers[12]);
            Assert.AreEqual("whois.verisign-grs.com", result.Value.Url);
            Assert.AreEqual("Registration information: http://www.verisign-grs.com", result.Value.Remarks);
            Assert.AreEqual(new DateTime(1985, 1, 1), result.Value.Created);
            Assert.AreEqual(new DateTime(2012, 2, 15), result.Value.Changed);
        }
    }
}
