using System;
using NUnit.Framework;
using Tokens.Samples;
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
            var pattern = Resources.Pattern_nominet;
            var input = Resources.Data_bbc_co_uk;

            var result = tokenizer.Parse<WhoisRecord>(pattern, input);

            Assert.IsNotNull(result);
            Assert.AreEqual("bbc.co.uk", result.Domain);
            Assert.AreEqual("British Broadcasting Corporation", result.Registrant.Organization);
            Assert.AreEqual("British Broadcasting Corporation", result.Registrant.Street);
            Assert.AreEqual("Broadcasting House", result.Registrant.City);
            Assert.AreEqual("Portland Place", result.Registrant.State);
            Assert.AreEqual("London", result.Registrant.PostalCode);
            Assert.AreEqual("British Broadcasting Corporation [Tag = BBC]", result.RegistrarName);
            Assert.AreEqual("http://www.bbc.co.uk", result.RegistrarUrl);
            Assert.AreEqual(3, result.NameServers.Count);
            Assert.AreEqual("ns1.rbsov.bbc.co.uk       212.58.241.67", result.NameServers[0]);
            Assert.AreEqual("ns1.tcams.bbc.co.uk       212.72.49.3", result.NameServers[1]);
            Assert.AreEqual("ns1.thdow.bbc.co.uk       212.58.240.163", result.NameServers[2]);
            Assert.AreEqual(new DateTime(2014, 12, 13), result.ExpirationDate);
            Assert.AreEqual(new DateTime(2014, 6, 12), result.ModificationDate);
        }

        [Test]
        public void TestParseIanaServerDataData()
        {
            var pattern = Resources.Pattern_iana;
            var input = Resources.Data_com;

            tokenizer.Options.ThrowExceptionOnMissingProperty = true;

            var result = tokenizer.Parse<WhoisServer>(pattern, input);

            Assert.IsNotNull(result);
            Assert.AreEqual("com", result.TLD);
            Assert.AreEqual("VeriSign Global Registry Services", result.Organization.Name);
            Assert.AreEqual(3, result.Organization.Address.Count);
            Assert.AreEqual("12061 Bluemont Way", result.Organization.Address[0]);
            Assert.AreEqual("Reston Virginia 20190", result.Organization.Address[1]);
            Assert.AreEqual("United States", result.Organization.Address[2]);
            Assert.AreEqual("Registry Customer Service", result.AdminContact.Name);
            Assert.AreEqual("VeriSign Global Registry Services", result.AdminContact.Organization);
            Assert.AreEqual(3, result.AdminContact.Address.Count);
            Assert.AreEqual("12061 Bluemont Way", result.AdminContact.Address[0]);
            Assert.AreEqual("Reston Virginia 20190", result.AdminContact.Address[1]);
            Assert.AreEqual("United States", result.AdminContact.Address[2]);
            Assert.AreEqual("+1 703 925-6999", result.AdminContact.PhoneNumber);
            Assert.AreEqual("+1 703 948 3978", result.AdminContact.FaxNumber);
            Assert.AreEqual("info@verisign-grs.com", result.AdminContact.Email);
            Assert.AreEqual("Registry Customer Service", result.TechContact.Name);
            Assert.AreEqual("VeriSign Global Registry Services", result.TechContact.Organization);
            Assert.AreEqual(3, result.TechContact.Address.Count);
            Assert.AreEqual("12061 Bluemont Way", result.TechContact.Address[0]);
            Assert.AreEqual("Reston Virginia 20190", result.TechContact.Address[1]);
            Assert.AreEqual("United States", result.TechContact.Address[2]);
            Assert.AreEqual("+1 703 925-6999", result.TechContact.PhoneNumber);
            Assert.AreEqual("+1 703 948 3978", result.TechContact.FaxNumber);
            Assert.AreEqual("info@verisign-grs.com", result.TechContact.Email);
            Assert.AreEqual(13, result.NameServers.Count);
            Assert.AreEqual("A.GTLD-SERVERS.NET 192.5.6.30 2001:503:a83e:0:0:0:2:30", result.NameServers[0]);
            Assert.AreEqual("B.GTLD-SERVERS.NET 192.33.14.30 2001:503:231d:0:0:0:2:30", result.NameServers[1]);
            Assert.AreEqual("C.GTLD-SERVERS.NET 192.26.92.30", result.NameServers[2]);
            Assert.AreEqual("D.GTLD-SERVERS.NET 192.31.80.30", result.NameServers[3]);
            Assert.AreEqual("E.GTLD-SERVERS.NET 192.12.94.30", result.NameServers[4]);
            Assert.AreEqual("F.GTLD-SERVERS.NET 192.35.51.30", result.NameServers[5]);
            Assert.AreEqual("G.GTLD-SERVERS.NET 192.42.93.30", result.NameServers[6]);
            Assert.AreEqual("H.GTLD-SERVERS.NET 192.54.112.30", result.NameServers[7]);
            Assert.AreEqual("I.GTLD-SERVERS.NET 192.43.172.30", result.NameServers[8]);
            Assert.AreEqual("J.GTLD-SERVERS.NET 192.48.79.30", result.NameServers[9]);
            Assert.AreEqual("K.GTLD-SERVERS.NET 192.52.178.30", result.NameServers[10]);
            Assert.AreEqual("L.GTLD-SERVERS.NET 192.41.162.30", result.NameServers[11]);
            Assert.AreEqual("M.GTLD-SERVERS.NET 192.55.83.30", result.NameServers[12]);
            Assert.AreEqual("whois.verisign-grs.com", result.Url);
            Assert.AreEqual("Registration information: http://www.verisign-grs.com", result.Remarks);
            Assert.AreEqual(new DateTime(1985, 1, 1), result.Created);
            Assert.AreEqual(new DateTime(2012, 2, 15), result.Changed);
        }

        [Test]
        public void TestParseAbogadoData()
        {
            var pattern = Resources.Pattern_iana;
            var input = Resources.Data_abogado;

            tokenizer.Options.ThrowExceptionOnMissingProperty = true;

            var result = tokenizer.Parse<WhoisServer>(pattern, input);

            Assert.IsNotNull(result);
            Assert.AreEqual("abogado", result.TLD);
            Assert.AreEqual("Minds + Machines Group Limited", result.Organization.Name);
            Assert.AreEqual(2, result.Organization.Address.Count);
            Assert.AreEqual("Craigmuir Chambers, Road Town Tortola VG 1110", result.Organization.Address[0]);
            Assert.AreEqual("Virgin Islands, British", result.Organization.Address[1]);
            Assert.AreEqual("Admin Contact", result.AdminContact.Name);
            Assert.AreEqual("Minds + Machines Ltd", result.AdminContact.Organization);
            Assert.AreEqual(2, result.AdminContact.Address.Count);
            Assert.AreEqual("32 Nassau St, Dublin 2", result.AdminContact.Address[0]);
            Assert.AreEqual("Ireland", result.AdminContact.Address[1]);
            Assert.AreEqual("+1-877-734-4783", result.AdminContact.PhoneNumber);
            Assert.AreEqual("ops@mmx.co", result.AdminContact.Email);
            Assert.AreEqual("TLD Registry Services Technical", result.TechContact.Name);
            Assert.AreEqual("Nominet", result.TechContact.Organization);
            Assert.AreEqual(6, result.TechContact.Address.Count);
            Assert.AreEqual("Minerva House,", result.TechContact.Address[0]);
            Assert.AreEqual("Edmund Halley Road,", result.TechContact.Address[1]);
            Assert.AreEqual("Oxford Science Park,", result.TechContact.Address[2]);
            Assert.AreEqual("Oxford,", result.TechContact.Address[3]);
            Assert.AreEqual("OX4 4DQ", result.TechContact.Address[4]);
            Assert.AreEqual("United Kingdom", result.TechContact.Address[5]);
            Assert.AreEqual("+44.1865332211", result.TechContact.PhoneNumber);
            Assert.AreEqual("registrytechnical@nominet.uk", result.TechContact.Email);
            Assert.AreEqual(8, result.NameServers.Count);
            Assert.AreEqual("DNS1.NIC.ABOGADO 213.248.217.13 2a01:618:401:0:0:0:0:13", result.NameServers[0]);
            Assert.AreEqual("DNS2.NIC.ABOGADO 103.49.81.13 2401:fd80:401:0:0:0:0:13", result.NameServers[1]);
            Assert.AreEqual("DNS3.NIC.ABOGADO 213.248.221.13 2a01:618:405:0:0:0:0:13", result.NameServers[2]);
            Assert.AreEqual("DNS4.NIC.ABOGADO 2401:fd80:405:0:0:0:0:13 43.230.49.13", result.NameServers[3]);
            Assert.AreEqual("DNSA.NIC.ABOGADO 156.154.100.3 2001:502:ad09:0:0:0:0:3", result.NameServers[4]);
            Assert.AreEqual("DNSB.NIC.ABOGADO 156.154.101.3", result.NameServers[5]);
            Assert.AreEqual("DNSC.NIC.ABOGADO 156.154.102.3", result.NameServers[6]);
            Assert.AreEqual("DNSD.NIC.ABOGADO 156.154.103.3", result.NameServers[7]);
            //Assert.AreEqual("55367 8 2 A3065C94F7700D7CAB948BBD4A39845E3881F70E61FFB1D9E71DE1AF566919C6", result.NameServers[8]);
            Assert.AreEqual("whois.nic.abogado", result.Url);
            Assert.AreEqual("Registration information: http://mm-registry.com", result.Remarks);
            Assert.AreEqual(new DateTime(2014, 7, 10), result.Created);
            Assert.AreEqual(new DateTime(2018, 6, 29), result.Changed);
        }

        [Test]
        public void TestFacebookRedirect()
        {
            var pattern = Resources.Pattern_verisign_grs;
            var input = Resources.Data_facebook_com_redirect;

            tokenizer.Options.ThrowExceptionOnMissingProperty = true;

            var result = tokenizer.Parse<WhoisRedirect>(pattern, input);

            Assert.IsNotNull(result);
            Assert.AreEqual("facebook.com", result.Domain);
            Assert.AreEqual("whois.registrarsafe.com", result.Url);

        }
    }
}
