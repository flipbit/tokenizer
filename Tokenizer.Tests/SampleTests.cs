using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Tokens
{
    [TestFixture]
    public class SampleTests
    {
        private Tokenizer tokenizer;

        [SetUp]
        public void SetUp()
        {
            SerilogConfig.Init();

            tokenizer = new Tokenizer();
        }

        [Test]
        public void TestWhoisUk()
        {
            var template = ReadTemplate("whois.uk");
            var input = ReadData("bbc.co.uk");

            var result = tokenizer.Tokenize(template, input);

            Assert.IsNotNull(result);

            Assert.AreEqual("bbc.co.uk", result.First("DomainName"));
            Assert.AreEqual("British Broadcasting Corporation", result.First("Registrant.Name"));

            Assert.AreEqual(6, result.All("Registrant.Address").Count);
            Assert.AreEqual("British Broadcasting Corporation", result.All("Registrant.Address")[0]);
            Assert.AreEqual("Broadcasting House", result.All("Registrant.Address")[1]);
            Assert.AreEqual("Portland Place", result.All("Registrant.Address")[2]);
            Assert.AreEqual("London", result.All("Registrant.Address")[3]);
            Assert.AreEqual("W1A 1AA", result.All("Registrant.Address")[4]);
            Assert.AreEqual("United Kingdom", result.All("Registrant.Address")[5]);

            Assert.AreEqual("British Broadcasting Corporation [Tag = BBC]", result.First("Registrar.Name"));
            Assert.AreEqual("http://www.bbc.co.uk", result.First("Registrar.Url"));
            Assert.AreEqual(new DateTime(1996, 08, 01, 00, 00, 00, 000, DateTimeKind.Utc), result.First("Registered"));
            Assert.AreEqual(new DateTime(2014, 12, 13, 00, 00, 00, 000, DateTimeKind.Utc), result.First("Expiration"));
            Assert.AreEqual(new DateTime(2014, 06, 12, 00, 00, 00, 000, DateTimeKind.Utc), result.First("Updated"));
            Assert.AreEqual("Registered until expiry date.", result.First("DomainStatus"));

            Assert.AreEqual(3, result.All("NameServers").Count);
            Assert.AreEqual("ns1.rbsov.bbc.co.uk", result.All("NameServers")[0]);
            Assert.AreEqual("ns1.tcams.bbc.co.uk", result.All("NameServers")[1]);
            Assert.AreEqual("ns1.thdow.bbc.co.uk", result.All("NameServers")[2]);

            Assert.AreEqual("Found", result.First("Status"));
        }

        [Test]
        public void TestParseIanaServerDataData()
        {
            var pattern = ReadTemplate("whois.iana");
            var input = ReadData("com");

            var result = tokenizer.Tokenize(pattern, input);

            Assert.AreEqual("com", result.First("Tld"));
            Assert.AreEqual("VeriSign Global Registry Services", result.First("Organization.Name"));

            Assert.AreEqual(3, result.All("Organization.Address").Count);
            Assert.AreEqual("12061 Bluemont Way", result.All("Organization.Address")[0]);
            Assert.AreEqual("Reston Virginia 20190", result.All("Organization.Address")[1]);
            Assert.AreEqual("United States", result.All("Organization.Address")[2]);

            Assert.AreEqual("Registry Customer Service", result.First("AdminContact.Name"));
            Assert.AreEqual("VeriSign Global Registry Services", result.First("AdminContact.Organization"));

            Assert.AreEqual(3, result.All("AdminContact.Address").Count);
            Assert.AreEqual("12061 Bluemont Way", result.All("AdminContact.Address")[0]);
            Assert.AreEqual("Reston Virginia 20190", result.All("AdminContact.Address")[1]);
            Assert.AreEqual("United States", result.All("AdminContact.Address")[2]);

            Assert.AreEqual("+1 703 925-6999", result.First("AdminContact.TelephoneNumber"));
            Assert.AreEqual("+1 703 948 3978", result.First("AdminContact.FaxNumber"));
            Assert.AreEqual("info@verisign-grs.com", result.First("AdminContact.Email"));
            Assert.AreEqual("Registry Customer Service", result.First("TechContact.Name"));
            Assert.AreEqual("VeriSign Global Registry Services", result.First("TechContact.Organization"));

            Assert.AreEqual(3, result.All("TechContact.Address").Count);
            Assert.AreEqual("12061 Bluemont Way", result.All("TechContact.Address")[0]);
            Assert.AreEqual("Reston Virginia 20190", result.All("TechContact.Address")[1]);
            Assert.AreEqual("United States", result.All("TechContact.Address")[2]);

            Assert.AreEqual("+1 703 925-6999", result.First("TechContact.TelephoneNumber"));
            Assert.AreEqual("+1 703 948 3978", result.First("TechContact.FaxNumber"));
            Assert.AreEqual("info@verisign-grs.com", result.First("TechContact.Email"));

            Assert.AreEqual(13, result.All("NameServers").Count);
            Assert.AreEqual("A.GTLD-SERVERS.NET 192.5.6.30 2001:503:a83e:0:0:0:2:30", result.All("NameServers")[0]);
            Assert.AreEqual("B.GTLD-SERVERS.NET 192.33.14.30 2001:503:231d:0:0:0:2:30", result.All("NameServers")[1]);
            Assert.AreEqual("C.GTLD-SERVERS.NET 192.26.92.30", result.All("NameServers")[2]);
            Assert.AreEqual("D.GTLD-SERVERS.NET 192.31.80.30", result.All("NameServers")[3]);
            Assert.AreEqual("E.GTLD-SERVERS.NET 192.12.94.30", result.All("NameServers")[4]);
            Assert.AreEqual("F.GTLD-SERVERS.NET 192.35.51.30", result.All("NameServers")[5]);
            Assert.AreEqual("G.GTLD-SERVERS.NET 192.42.93.30", result.All("NameServers")[6]);
            Assert.AreEqual("H.GTLD-SERVERS.NET 192.54.112.30", result.All("NameServers")[7]);
            Assert.AreEqual("I.GTLD-SERVERS.NET 192.43.172.30", result.All("NameServers")[8]);
            Assert.AreEqual("J.GTLD-SERVERS.NET 192.48.79.30", result.All("NameServers")[9]);
            Assert.AreEqual("K.GTLD-SERVERS.NET 192.52.178.30", result.All("NameServers")[10]);
            Assert.AreEqual("L.GTLD-SERVERS.NET 192.41.162.30", result.All("NameServers")[11]);
            Assert.AreEqual("M.GTLD-SERVERS.NET 192.55.83.30", result.All("NameServers")[12]);

            Assert.AreEqual("whois.verisign-grs.com", result.First("Url"));
            Assert.AreEqual("Registration information: http://www.verisign-grs.com", result.First("Remarks"));
            Assert.AreEqual("1985-01-01", result.First("Created"));
            Assert.AreEqual("2012-02-15", result.First("Changed"));
            Assert.AreEqual("Found", result.First("Status"));
        }

        [Test]
        public void TestParseAbogadoData()
        {
            var pattern = ReadTemplate("whois.iana");
            var input = ReadData("abogado");

            var result = tokenizer.Tokenize(pattern, input);

            AssertWriter.Write(result);

            Assert.AreEqual("abogado", result.First("Tld"));
            Assert.AreEqual("Minds + Machines Group Limited", result.First("Organization.Name"));

            Assert.AreEqual(2, result.All("Organization.Address").Count);
            Assert.AreEqual("Craigmuir Chambers, Road Town Tortola VG 1110", result.All("Organization.Address")[0]);
            Assert.AreEqual("Virgin Islands, British", result.All("Organization.Address")[1]);

            Assert.AreEqual("Admin Contact", result.First("AdminContact.Name"));
            Assert.AreEqual("Minds + Machines Ltd", result.First("AdminContact.Organization"));

            Assert.AreEqual(2, result.All("AdminContact.Address").Count);
            Assert.AreEqual("32 Nassau St, Dublin 2", result.All("AdminContact.Address")[0]);
            Assert.AreEqual("Ireland", result.All("AdminContact.Address")[1]);

            Assert.AreEqual("+1-877-734-4783", result.First("AdminContact.TelephoneNumber"));
            Assert.AreEqual("ops@mmx.co", result.First("AdminContact.Email"));
            Assert.AreEqual("TLD Registry Services Technical", result.First("TechContact.Name"));
            Assert.AreEqual("Nominet", result.First("TechContact.Organization"));

            Assert.AreEqual(6, result.All("TechContact.Address").Count);
            Assert.AreEqual("Minerva House,", result.All("TechContact.Address")[0]);
            Assert.AreEqual("Edmund Halley Road,", result.All("TechContact.Address")[1]);
            Assert.AreEqual("Oxford Science Park,", result.All("TechContact.Address")[2]);
            Assert.AreEqual("Oxford,", result.All("TechContact.Address")[3]);
            Assert.AreEqual("OX4 4DQ", result.All("TechContact.Address")[4]);
            Assert.AreEqual("United Kingdom", result.All("TechContact.Address")[5]);

            Assert.AreEqual("+44.1865332211", result.First("TechContact.TelephoneNumber"));
            Assert.AreEqual("registrytechnical@nominet.uk", result.First("TechContact.Email"));

            Assert.AreEqual(8, result.All("NameServers").Count);
            Assert.AreEqual("DNS1.NIC.ABOGADO 213.248.217.13 2a01:618:401:0:0:0:0:13", result.All("NameServers")[0]);
            Assert.AreEqual("DNS2.NIC.ABOGADO 103.49.81.13 2401:fd80:401:0:0:0:0:13", result.All("NameServers")[1]);
            Assert.AreEqual("DNS3.NIC.ABOGADO 213.248.221.13 2a01:618:405:0:0:0:0:13", result.All("NameServers")[2]);
            Assert.AreEqual("DNS4.NIC.ABOGADO 2401:fd80:405:0:0:0:0:13 43.230.49.13", result.All("NameServers")[3]);
            Assert.AreEqual("DNSA.NIC.ABOGADO 156.154.100.3 2001:502:ad09:0:0:0:0:3", result.All("NameServers")[4]);
            Assert.AreEqual("DNSB.NIC.ABOGADO 156.154.101.3", result.All("NameServers")[5]);
            Assert.AreEqual("DNSC.NIC.ABOGADO 156.154.102.3", result.All("NameServers")[6]);
            Assert.AreEqual("DNSD.NIC.ABOGADO 156.154.103.3", result.All("NameServers")[7]);

            Assert.AreEqual("whois.nic.abogado", result.First("Url"));
            Assert.AreEqual("Registration information: http://mm-registry.com", result.First("Remarks"));
            Assert.AreEqual("2014-07-10", result.First("Created"));
            Assert.AreEqual("2018-06-29", result.First("Changed"));
            Assert.AreEqual("Found", result.First("Status"));
        }

        [Test]
        public void TestVerisignRedirect()
        {
            var pattern = ReadTemplate("whois.verisign-grs.com");
            var input = ReadData("facebook.com");

            var result = tokenizer.Tokenize(pattern, input);

            Assert.AreEqual("facebook.com", result.First("WhoisRedirect.Domain"));
            Assert.AreEqual("whois.registrarsafe.com", result.First("WhoisRedirect.Url"));
            Assert.AreEqual("http://www.registrarsafe.com", result.First("WhoisRedirect.ReferralUrl"));
            Assert.AreEqual(new DateTime(2018, 07, 23, 18, 17, 13, 000, DateTimeKind.Utc), result.First("WhoisRedirect.ModifiedDate"));
            Assert.AreEqual(new DateTime(1997, 03, 29, 05, 00, 00, 000, DateTimeKind.Utc), result.First("WhoisRedirect.CreatedDate"));
            Assert.AreEqual(new DateTime(2028, 03, 30, 04, 00, 00, 000, DateTimeKind.Utc), result.First("WhoisRedirect.ExpirationDate"));
            Assert.AreEqual("RegistrarSafe, LLC", result.First("WhoisRedirect.Registrar"));

            Assert.AreEqual(2, result.All("WhoisRedirect.NameServers").Count);
            Assert.AreEqual("A.NS.FACEBOOK.COM", result.All("WhoisRedirect.NameServers")[0]);
            Assert.AreEqual("B.NS.FACEBOOK.COM", result.All("WhoisRedirect.NameServers")[1]);

        }

        [Test]
        public void TestWrongTemplate()
        {
            var pattern = ReadTemplate("whois.nic.br");
            var input = ReadData("08.pl");

            var result = tokenizer.Tokenize(pattern, input);

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void TestSilOrgRedirect()
        {
            var pattern = ReadTemplate("whois.verisign-grs.com");
            var input = ReadData("sil.org");

            var result = tokenizer.Tokenize(pattern, input);

            Assert.AreEqual("sil.org", result.First("WhoisRedirect.Domain"));
            Assert.AreEqual("whois.enom.com", result.First("WhoisRedirect.Url"));
            Assert.AreEqual("http://www.enom.com", result.First("WhoisRedirect.ReferralUrl"));
            Assert.AreEqual(new DateTime(2018, 03, 06, 00, 17, 46, 000, DateTimeKind.Utc), result.First("WhoisRedirect.ModifiedDate"));
            Assert.AreEqual(new DateTime(1991, 04, 15, 04, 00, 00, 000, DateTimeKind.Utc), result.First("WhoisRedirect.CreatedDate"));
            Assert.AreEqual(new DateTime(2020, 04, 16, 04, 00, 00, 000, DateTimeKind.Utc), result.First("WhoisRedirect.ExpirationDate"));
            Assert.AreEqual("eNom, Inc.", result.First("WhoisRedirect.Registrar"));

            Assert.AreEqual(3, result.All("WhoisRedirect.NameServers").Count);
            Assert.AreEqual("NSJ1.WSFO.ORG", result.All("WhoisRedirect.NameServers")[0]);
            Assert.AreEqual("NSC1.WSFO.ORG", result.All("WhoisRedirect.NameServers")[1]);
            Assert.AreEqual("NSD1.WSFO.ORG", result.All("WhoisRedirect.NameServers")[2]);
        }

        [Test]
        public void TestAmazonCoJp()
        {
            var template = ReadTemplate("whois.jprs.jp");
            var input = ReadData("amazon.co.jp");

            var result = tokenizer.Tokenize(template, input);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(11, result.Matches.Count);

            Assert.AreEqual("amazon.co.jp", result.First("DomainName"));
            Assert.AreEqual("Amazon, Inc.", result.First("Registrar.Name"));
            Assert.AreEqual("JC076JP", result.First("AdminContact.Name"));
            Assert.AreEqual("IK4644JP", result.First("TechnicalContact.Name"));
            Assert.AreEqual(new DateTime(2002, 11, 21), result.First("Registered"));
            Assert.AreEqual(new DateTime(2018, 12, 1), result.First("Updated"));

            var nameServers = (List<object>) result.All("NameServers");

            Assert.AreEqual("ns1.p31.dynect.net", nameServers[0]);
            Assert.AreEqual("ns2.p31.dynect.net", nameServers[1]);
            Assert.AreEqual("pdns1.ultradns.net", nameServers[2]);
            Assert.AreEqual("pdns6.ultradns.co.uk", nameServers[3]);

        }

        [Test]
        public void TestGoogleVg()
        {
            var template = ReadTemplate("whois.vg");
            var input = ReadData("google.vg");

            var result = tokenizer.Tokenize(template, input);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(53, result.Matches.Count);
        }

        [Test]
        public void TestVgNotFound()
        {
            var template = ReadTemplate("whois.vg.not.found");
            var input = ReadData("not.found.vg");

            var result = tokenizer.Tokenize(template, input);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, result.Matches.Count);
        }


        [Test]
        public void TestGoogleCc()
        {
            var template = ReadTemplate("whois.cc");
            var input = ReadData("google.cc");

            var result = tokenizer.Tokenize(template, input);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(22, result.Matches.Count);

            var nameServers = result.All("NameServers");

            Assert.AreEqual(4, nameServers.Count);
            Assert.AreEqual("ns1.google.com", nameServers[0]);
            Assert.AreEqual("ns2.google.com", nameServers[1]);
            Assert.AreEqual("ns3.google.com", nameServers[2]);
            Assert.AreEqual("ns4.google.com", nameServers[3]);
        }

        [Test]
        public void TestGoogleCoZa()
        {
            var template = ReadTemplate("whois.generic");
            var input = ReadData("google.co.za");

            var result = tokenizer.Tokenize(template, input);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(58, result.Matches.Count);

            Assert.AreEqual("google.co.za", result.First("DomainName"));

            var nameServers = result.All("NameServers");

            Assert.AreEqual(4, nameServers.Count);
            Assert.AreEqual("ns1.google.com", nameServers[0]);
            Assert.AreEqual("ns2.google.com", nameServers[1]);
            Assert.AreEqual("ns3.google.com", nameServers[2]);
            Assert.AreEqual("ns4.google.com", nameServers[3]);
        }

        [Test]
        public void TestGoogleBiz()
        {
            var template = ReadTemplate("whois.generic");
            var input = ReadData("google.biz");

            var result = tokenizer.Tokenize(template, input);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(51, result.Matches.Count);

            var nameServers = result.All("NameServers");

            Assert.AreEqual(4, nameServers.Count);
            Assert.AreEqual("ns1.google.com", nameServers[0]);
            Assert.AreEqual("ns2.google.com", nameServers[1]);
            Assert.AreEqual("ns4.google.com", nameServers[2]);
            Assert.AreEqual("ns3.google.com", nameServers[3]);
        }

        [Test]
        public void TestTokenMatcherCom()
        {
            var template = ReadTemplate("whois.iana");
            var input = ReadData("com");

            var result = tokenizer.Tokenize(template, input);

            Assert.AreEqual(39, result.Matches.Count);
            AssertWriter.Write(result);
        }
  
        [Test]
        public void TestTokenMatcherCoCa()
        {
            var template = ReadTemplate("whois.co.ca");
            var input = ReadData("available.co.ca");

            var matcher = new TokenMatcher();

            matcher.RegisterTemplate(template);

            var match = matcher.Match(input);

            Assert.AreEqual(match.BestMatch.First("DomainName"), "u34jedzcq.co.ca");
            Assert.AreEqual(match.BestMatch.First("Status"), "NotFound");
        }
  
        [Test]
        public void TestWhoisEuOrg()
        {
            var template = ReadTemplate("whois.eu.org");
            var input = ReadData("google.eu.org");

            var result = tokenizer.Tokenize(template, input);

            Assert.AreEqual(result.First("DomainName"), "google.eu.org");
        }
  
        [Test]
        public void TestWhoisGoogleTr()
        {
            var template = ReadTemplate("whois.tr");
            var input = ReadData("google.tr");

            var result = tokenizer.Tokenize(template, input);

            Assert.AreEqual(new DateTime(2001, 08, 23), result.First("Registered"));
        }

        [Test]
        public void TestWhoisVe()
        {
            var template = ReadTemplate("whois.ve");
            var input = ReadData("aloespa.com.ve");

            var result = tokenizer.Tokenize(template, input);

            AssertWriter.Write(result);
            Assert.AreEqual("Rafael Perez", result.First("Registrant.Name"));
            Assert.AreEqual("aloespa.com.ve-dom", result.First("Registrant.RegistryId"));
            Assert.AreEqual("registro@tepuynet.com", result.First("Registrant.Email"));

            Assert.AreEqual(3, result.All("Registrant.Address").Count);
            Assert.AreEqual("Rafael Perez", result.All("Registrant.Address")[0]);
            Assert.AreEqual("Caracas", result.All("Registrant.Address")[1]);
            Assert.AreEqual("Caracas, D. Federal  VE", result.All("Registrant.Address")[2]);

            Assert.AreEqual("aloespa.com.ve", result.First("DomainName"));
            Assert.AreEqual("Tepuynet", result.First("AdminContact.Name"));
            Assert.AreEqual("aloespa.com.ve-adm", result.First("AdminContact.RegistryId"));
            Assert.AreEqual("registro@tepuynet.com", result.First("AdminContact.Email"));

            Assert.AreEqual(3, result.All("AdminContact.Address").Count);
            Assert.AreEqual("Tepuynet C.A.", result.All("AdminContact.Address")[0]);
            Assert.AreEqual("Av. Bolivar Norte Torre Banaven, Piso 9 Ofic. 9-9", result.All("AdminContact.Address")[1]);
            Assert.AreEqual("Valencia, Carabobo  VE", result.All("AdminContact.Address")[2]);

            Assert.AreEqual("2418246437", result.First("AdminContact.TelephoneNumber"));
            Assert.AreEqual("2418246437", result.First("AdminContact.FaxNumber"));
            Assert.AreEqual("Tepuynet", result.First("TechnicalContact.Name"));
            Assert.AreEqual("aloespa.com.ve-tec", result.First("TechnicalContact.RegistryId"));
            Assert.AreEqual("registro@tepuynet.com", result.First("TechnicalContact.Email"));

            Assert.AreEqual(3, result.All("TechnicalContact.Address").Count);
            Assert.AreEqual("Tepuynet C.A.", result.All("TechnicalContact.Address")[0]);
            Assert.AreEqual("Av. Bolivar Norte Torre Banaven, Piso 9 Ofic. 9-9", result.All("TechnicalContact.Address")[1]);
            Assert.AreEqual("Valencia, Carabobo  VE", result.All("TechnicalContact.Address")[2]);

            Assert.AreEqual("2418246437", result.First("TechnicalContact.TelephoneNumber"));
            Assert.AreEqual("2418246437", result.First("TechnicalContact.FaxNumber"));
            Assert.AreEqual("Tepuynet", result.First("BillingContact.Name"));
            Assert.AreEqual("aloespa.com.ve-bil", result.First("BillingContact.RegistryId"));
            Assert.AreEqual("registro@tepuynet.com", result.First("BillingContact.Email"));

            Assert.AreEqual(3, result.All("BillingContact.Address").Count);
            Assert.AreEqual("Tepuynet C.A.", result.All("BillingContact.Address")[0]);
            Assert.AreEqual("Av. Bolivar Norte Torre Banaven, Piso 9 Ofic. 9-9", result.All("BillingContact.Address")[1]);
            Assert.AreEqual("Valencia, Carabobo  VE", result.All("BillingContact.Address")[2]);

            Assert.AreEqual("2418246437", result.First("BillingContact.TelephoneNumber"));
            Assert.AreEqual("2418246437", result.First("BillingContact.FaxNumber"));
            Assert.AreEqual(new DateTime(2010, 11, 21, 15, 21, 32, 000, DateTimeKind.Utc), result.First("Expiration"));
            Assert.AreEqual(new DateTime(2006, 06, 08, 21, 54, 41, 000, DateTimeKind.Utc), result.First("Updated"));
            Assert.AreEqual(new DateTime(2005, 11, 21, 15, 21, 32, 000, DateTimeKind.Utc), result.First("Registered"));
            Assert.AreEqual("SUSPENDIDO", result.First("DomainStatus"));

            Assert.AreEqual(2, result.All("NameServers").Count);
            Assert.AreEqual("ns10.tepuyserver.net", result.All("NameServers")[0]);
            Assert.AreEqual("ns9.tepuyserver.net", result.All("NameServers")[1]);

            Assert.AreEqual("Found", result.First("Status"));
        }

        [Test]
        public void TestWhoisCoop()
        {
            var template = ReadTemplate("whois.coop");
            var input = ReadData("moscowfood.coop");

            var result = tokenizer.Tokenize(template, input);

            AssertWriter.Write(result);
            Assert.AreEqual("5662D-COOP", result.First("RegistryDomainId"));
            Assert.AreEqual("moscowfood.coop", result.First("DomainName"));
            Assert.AreEqual(new DateTime(2013, 01, 30, 00, 00, 00, 000, DateTimeKind.Utc), result.First("Expiration"));

            Assert.AreEqual(3, result.All("DomainStatus").Count);
            Assert.AreEqual("clientDeleteProhibited", result.All("DomainStatus")[0]);
            Assert.AreEqual("clientTransferProhibited", result.All("DomainStatus")[1]);
            Assert.AreEqual("clientUpdateProhibited", result.All("DomainStatus")[2]);

            Assert.AreEqual("Domain Bank Inc.", result.First("Registrar.Name"));
            Assert.AreEqual("31", result.First("Registrar.IanaId"));
            Assert.AreEqual(new DateTime(2001, 10, 09, 04, 36, 36, 000, DateTimeKind.Utc), result.First("Registered"));
            Assert.AreEqual("registrant", result.First("Type"));
            Assert.AreEqual("71764C-COOP", result.First("Contact.Id"));
            Assert.AreEqual("Kenna Eaton", result.First("Contact.Name"));
            Assert.AreEqual("Moscow Food Co-op", result.First("Contact.Organization"));

            Assert.AreEqual(5, result.All("Address").Count);
            Assert.AreEqual("P. O. Box 9485", result.All("Address")[0]);
            Assert.AreEqual("Moscow", result.All("Address")[1]);
            Assert.AreEqual("ID", result.All("Address")[2]);
            Assert.AreEqual("83843", result.All("Address")[3]);
            Assert.AreEqual("United States", result.All("Address")[4]);

            Assert.AreEqual("+1.2088828537", result.First("Phone"));
            Assert.AreEqual("+1.2088828082", result.First("Fax"));

            Assert.AreEqual(4, result.All("Email").Count);
            Assert.AreEqual("kenna@moscowfood.coop", result.All("Email")[0]);
            Assert.AreEqual("outreach@moscowfood.coop", result.All("Email")[1]);
            Assert.AreEqual("payable@moscowfood.coop", result.All("Email")[2]);
            Assert.AreEqual("joseph@moscowfood.coop", result.All("Email")[3]);


            Assert.AreEqual(2, result.All("NameServers").Count);
            Assert.AreEqual("ns2.west-datacenter.net", result.All("NameServers")[0]);
            Assert.AreEqual("ns1.west-datacenter.net", result.All("NameServers")[1]);

            Assert.AreEqual("Found", result.First("Status"));
        }

        private string ReadData(string name)
        {
            return Read("Data", name);
        }

        private string ReadTemplate(string name)
        {
            return Read("Patterns", name);
        }

        private string Read(string type, string name)
        {
            var fileName = $@"../../../Samples/{type}/{name}.txt";

            return File.ReadAllText(fileName);
        }
    }
}
