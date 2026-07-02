using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class SampleTests : TokenizerTestBase
{
    private readonly Tokenizer tokenizer;

    public SampleTests(ITestOutputHelper output) : base(output)
    {
        tokenizer = CreateTokenizer();
    }

    [Fact]
    // Fails at template parsing stage: ParsingException "Unescaped '}' in text" at line 32.
    // The whois.uk template has a syntax error, so diagnostics cannot be exercised here.
    // Root cause is a template authoring issue, not a tokenization engine issue.
    public void TestWhoisUk()
    {
        var template = ReadTemplate("whois.uk");
        var input = ReadData("bbc.co.uk");

        var diagTokenizer = CreateDiagnosticTokenizer();
        var result = diagTokenizer.Tokenize(template, input);

        try
        {
            Assert.NotNull(result);

            Assert.Equal("bbc.co.uk", result.First("DomainName"));
            Assert.Equal("British Broadcasting Corporation", result.First("Registrant.Name"));

            Assert.Equal(6, result.All("Registrant.Address").Count);
            Assert.Equal("British Broadcasting Corporation", result.All("Registrant.Address")[0]);
            Assert.Equal("Broadcasting House", result.All("Registrant.Address")[1]);
            Assert.Equal("Portland Place", result.All("Registrant.Address")[2]);
            Assert.Equal("London", result.All("Registrant.Address")[3]);
            Assert.Equal("W1A 1AA", result.All("Registrant.Address")[4]);
            Assert.Equal("United Kingdom", result.All("Registrant.Address")[5]);

            Assert.Equal("British Broadcasting Corporation [Tag = BBC]", result.First("Registrar.Name"));
            Assert.Equal("http://www.bbc.co.uk", result.First("Registrar.Url"));
            Assert.Equal(new DateTime(1996, 08, 01, 00, 00, 00, 000, DateTimeKind.Utc), result.First("Registered"));
            Assert.Equal(new DateTime(2014, 12, 13, 00, 00, 00, 000, DateTimeKind.Utc), result.First("Expiration"));
            Assert.Equal(new DateTime(2014, 06, 12, 00, 00, 00, 000, DateTimeKind.Utc), result.First("Updated"));
            Assert.Equal("Registered until expiry date.", result.First("DomainStatus"));

            Assert.Equal(3, result.All("NameServers").Count);
            Assert.Equal("ns1.rbsov.bbc.co.uk", result.All("NameServers")[0]);
            Assert.Equal("ns1.tcams.bbc.co.uk", result.All("NameServers")[1]);
            Assert.Equal("ns1.thdow.bbc.co.uk", result.All("NameServers")[2]);

            Assert.Equal("Found", result.First("Status"));
        }
        catch
        {
            if (result.Diagnostics != null)
            {
                Output.WriteLine(result.Diagnostics.RenderAlignment());
                Output.WriteLine("---");
                Output.WriteLine(result.Diagnostics.Summary.Verdict);
                foreach (var issue in result.Diagnostics.Summary.Issues)
                {
                    Output.WriteLine($"  {issue.Type}: {issue.TokenName} — {issue.Description}");
                    if (issue.Hint != null)
                        Output.WriteLine($"    Hint: {issue.Hint}");
                }
            }
            throw;
        }
    }

    [Fact]
    public void TestParseIanaServerDataData()
    {
        var pattern = ReadTemplate("whois.iana");
        var input = ReadData("com");

        var result = tokenizer.Tokenize(pattern, input);

        Assert.Equal("com", result.First("Tld"));
        Assert.Equal("VeriSign Global Registry Services", result.First("Organization.Name"));

        Assert.Equal(3, result.All("Organization.Address").Count);
        Assert.Equal("12061 Bluemont Way", result.All("Organization.Address")[0]);
        Assert.Equal("Reston Virginia 20190", result.All("Organization.Address")[1]);
        Assert.Equal("United States", result.All("Organization.Address")[2]);

        Assert.Equal("Registry Customer Service", result.First("AdminContact.Name"));
        Assert.Equal("VeriSign Global Registry Services", result.First("AdminContact.Organization"));

        Assert.Equal(3, result.All("AdminContact.Address").Count);
        Assert.Equal("12061 Bluemont Way", result.All("AdminContact.Address")[0]);
        Assert.Equal("Reston Virginia 20190", result.All("AdminContact.Address")[1]);
        Assert.Equal("United States", result.All("AdminContact.Address")[2]);

        Assert.Equal("+1 703 925-6999", result.First("AdminContact.TelephoneNumber"));
        Assert.Equal("+1 703 948 3978", result.First("AdminContact.FaxNumber"));
        Assert.Equal("info@verisign-grs.com", result.First("AdminContact.Email"));
        Assert.Equal("Registry Customer Service", result.First("TechContact.Name"));
        Assert.Equal("VeriSign Global Registry Services", result.First("TechContact.Organization"));

        Assert.Equal(3, result.All("TechContact.Address").Count);
        Assert.Equal("12061 Bluemont Way", result.All("TechContact.Address")[0]);
        Assert.Equal("Reston Virginia 20190", result.All("TechContact.Address")[1]);
        Assert.Equal("United States", result.All("TechContact.Address")[2]);

        Assert.Equal("+1 703 925-6999", result.First("TechContact.TelephoneNumber"));
        Assert.Equal("+1 703 948 3978", result.First("TechContact.FaxNumber"));
        Assert.Equal("info@verisign-grs.com", result.First("TechContact.Email"));

        Assert.Equal(13, result.All("NameServers").Count);
        Assert.Equal("A.GTLD-SERVERS.NET 192.5.6.30 2001:503:a83e:0:0:0:2:30", result.All("NameServers")[0]);
        Assert.Equal("B.GTLD-SERVERS.NET 192.33.14.30 2001:503:231d:0:0:0:2:30", result.All("NameServers")[1]);
        Assert.Equal("C.GTLD-SERVERS.NET 192.26.92.30", result.All("NameServers")[2]);
        Assert.Equal("D.GTLD-SERVERS.NET 192.31.80.30", result.All("NameServers")[3]);
        Assert.Equal("E.GTLD-SERVERS.NET 192.12.94.30", result.All("NameServers")[4]);
        Assert.Equal("F.GTLD-SERVERS.NET 192.35.51.30", result.All("NameServers")[5]);
        Assert.Equal("G.GTLD-SERVERS.NET 192.42.93.30", result.All("NameServers")[6]);
        Assert.Equal("H.GTLD-SERVERS.NET 192.54.112.30", result.All("NameServers")[7]);
        Assert.Equal("I.GTLD-SERVERS.NET 192.43.172.30", result.All("NameServers")[8]);
        Assert.Equal("J.GTLD-SERVERS.NET 192.48.79.30", result.All("NameServers")[9]);
        Assert.Equal("K.GTLD-SERVERS.NET 192.52.178.30", result.All("NameServers")[10]);
        Assert.Equal("L.GTLD-SERVERS.NET 192.41.162.30", result.All("NameServers")[11]);
        Assert.Equal("M.GTLD-SERVERS.NET 192.55.83.30", result.All("NameServers")[12]);

        Assert.Equal("whois.verisign-grs.com", result.First("Url"));
        Assert.Equal("Registration information: http://www.verisign-grs.com", result.First("Remarks"));
        Assert.Equal("1985-01-01", result.First("Created"));
        Assert.Equal("2012-02-15", result.First("Changed"));
        Assert.Equal("Found", result.First("Status"));
    }

    [Fact]
    public void TestParseAbogadoData()
    {
        var pattern = ReadTemplate("whois.iana");
        var input = ReadData("abogado");

        var result = tokenizer.Tokenize(pattern, input);

        Assert.Equal("abogado", result.First("Tld"));
        Assert.Equal("Minds + Machines Group Limited", result.First("Organization.Name"));

        Assert.Equal(2, result.All("Organization.Address").Count);
        Assert.Equal("Craigmuir Chambers, Road Town Tortola VG 1110", result.All("Organization.Address")[0]);
        Assert.Equal("Virgin Islands, British", result.All("Organization.Address")[1]);

        Assert.Equal("Admin Contact", result.First("AdminContact.Name"));
        Assert.Equal("Minds + Machines Ltd", result.First("AdminContact.Organization"));

        Assert.Equal(2, result.All("AdminContact.Address").Count);
        Assert.Equal("32 Nassau St, Dublin 2", result.All("AdminContact.Address")[0]);
        Assert.Equal("Ireland", result.All("AdminContact.Address")[1]);

        Assert.Equal("+1-877-734-4783", result.First("AdminContact.TelephoneNumber"));
        Assert.Equal("ops@mmx.co", result.First("AdminContact.Email"));
        Assert.Equal("TLD Registry Services Technical", result.First("TechContact.Name"));
        Assert.Equal("Nominet", result.First("TechContact.Organization"));

        Assert.Equal(6, result.All("TechContact.Address").Count);
        Assert.Equal("Minerva House,", result.All("TechContact.Address")[0]);
        Assert.Equal("Edmund Halley Road,", result.All("TechContact.Address")[1]);
        Assert.Equal("Oxford Science Park,", result.All("TechContact.Address")[2]);
        Assert.Equal("Oxford,", result.All("TechContact.Address")[3]);
        Assert.Equal("OX4 4DQ", result.All("TechContact.Address")[4]);
        Assert.Equal("United Kingdom", result.All("TechContact.Address")[5]);

        Assert.Equal("+44.1865332211", result.First("TechContact.TelephoneNumber"));
        Assert.Equal("registrytechnical@nominet.uk", result.First("TechContact.Email"));

        Assert.Equal(8, result.All("NameServers").Count);
        Assert.Equal("DNS1.NIC.ABOGADO 213.248.217.13 2a01:618:401:0:0:0:0:13", result.All("NameServers")[0]);
        Assert.Equal("DNS2.NIC.ABOGADO 103.49.81.13 2401:fd80:401:0:0:0:0:13", result.All("NameServers")[1]);
        Assert.Equal("DNS3.NIC.ABOGADO 213.248.221.13 2a01:618:405:0:0:0:0:13", result.All("NameServers")[2]);
        Assert.Equal("DNS4.NIC.ABOGADO 2401:fd80:405:0:0:0:0:13 43.230.49.13", result.All("NameServers")[3]);
        Assert.Equal("DNSA.NIC.ABOGADO 156.154.100.3 2001:502:ad09:0:0:0:0:3", result.All("NameServers")[4]);
        Assert.Equal("DNSB.NIC.ABOGADO 156.154.101.3", result.All("NameServers")[5]);
        Assert.Equal("DNSC.NIC.ABOGADO 156.154.102.3", result.All("NameServers")[6]);
        Assert.Equal("DNSD.NIC.ABOGADO 156.154.103.3", result.All("NameServers")[7]);

        Assert.Equal("whois.nic.abogado", result.First("Url"));
        Assert.Equal("Registration information: http://mm-registry.com", result.First("Remarks"));
        Assert.Equal("2014-07-10", result.First("Created"));
        Assert.Equal("2018-06-29", result.First("Changed"));
        Assert.Equal("Found", result.First("Status"));
    }

    [Fact]
    public void TestVerisignRedirect()
    {
        var pattern = ReadTemplate("whois.verisign-grs.com");
        var input = ReadData("facebook.com");

        var result = tokenizer.Tokenize(pattern, input);

        Assert.Equal("facebook.com", result.First("WhoisRedirect.Domain"));
        Assert.Equal("whois.registrarsafe.com", result.First("WhoisRedirect.Url"));
        Assert.Equal("http://www.registrarsafe.com", result.First("WhoisRedirect.ReferralUrl"));
        Assert.Equal(new DateTime(2018, 07, 23, 18, 17, 13, 000, DateTimeKind.Utc), result.First("WhoisRedirect.ModifiedDate"));
        Assert.Equal(new DateTime(1997, 03, 29, 05, 00, 00, 000, DateTimeKind.Utc), result.First("WhoisRedirect.CreatedDate"));
        Assert.Equal(new DateTime(2028, 03, 30, 04, 00, 00, 000, DateTimeKind.Utc), result.First("WhoisRedirect.ExpirationDate"));
        Assert.Equal("RegistrarSafe, LLC", result.First("WhoisRedirect.Registrar"));

        Assert.Equal(2, result.All("WhoisRedirect.NameServers").Count);
        Assert.Equal("A.NS.FACEBOOK.COM", result.All("WhoisRedirect.NameServers")[0]);
        Assert.Equal("B.NS.FACEBOOK.COM", result.All("WhoisRedirect.NameServers")[1]);

    }

    [Fact]
    public void TestWrongTemplate()
    {
        var pattern = ReadTemplate("whois.nic.br");
        var input = ReadData("08.pl");

        var result = tokenizer.Tokenize(pattern, input);

        Assert.False(result.Success);
    }

    [Fact]
    public void TestSilOrgRedirect()
    {
        var pattern = ReadTemplate("whois.verisign-grs.com");
        var input = ReadData("sil.org");

        var result = tokenizer.Tokenize(pattern, input);

        Assert.Equal("sil.org", result.First("WhoisRedirect.Domain"));
        Assert.Equal("whois.enom.com", result.First("WhoisRedirect.Url"));
        Assert.Equal("http://www.enom.com", result.First("WhoisRedirect.ReferralUrl"));
        Assert.Equal(new DateTime(2018, 03, 06, 00, 17, 46, 000, DateTimeKind.Utc), result.First("WhoisRedirect.ModifiedDate"));
        Assert.Equal(new DateTime(1991, 04, 15, 04, 00, 00, 000, DateTimeKind.Utc), result.First("WhoisRedirect.CreatedDate"));
        Assert.Equal(new DateTime(2020, 04, 16, 04, 00, 00, 000, DateTimeKind.Utc), result.First("WhoisRedirect.ExpirationDate"));
        Assert.Equal("eNom, Inc.", result.First("WhoisRedirect.Registrar"));

        Assert.Equal(3, result.All("WhoisRedirect.NameServers").Count);
        Assert.Equal("NSJ1.WSFO.ORG", result.All("WhoisRedirect.NameServers")[0]);
        Assert.Equal("NSC1.WSFO.ORG", result.All("WhoisRedirect.NameServers")[1]);
        Assert.Equal("NSD1.WSFO.ORG", result.All("WhoisRedirect.NameServers")[2]);
    }

    [Fact]
    // Diagnostics: 0 of 8 tokens matched. All report "preamble never found" despite preambles
    // being present in input. Root cause is likely the $/* shorthand syntax in the JPRS template
    // producing different preamble text than expected during compilation.
    public void TestAmazonCoJp()
    {
        var template = ReadTemplate("whois.jprs.jp");
        var input = ReadData("amazon.co.jp");

        var diagTokenizer = CreateDiagnosticTokenizer();
        var result = diagTokenizer.Tokenize(template, input);

        try
        {
            Assert.True(result.Success);
            Assert.Equal(11, result.Matches.Count);

            Assert.Equal("amazon.co.jp", result.First("DomainName"));
            Assert.Equal("Amazon, Inc.", result.First("Registrar.Name"));
            Assert.Equal("JC076JP", result.First("AdminContact.Name"));
            Assert.Equal("IK4644JP", result.First("TechnicalContact.Name"));
            Assert.Equal(new DateTime(2002, 11, 21), result.First("Registered"));
            Assert.Equal(new DateTime(2018, 12, 1), result.First("Updated"));

            var nameServers = (List<object>)result.All("NameServers");

            Assert.Equal("ns1.p31.dynect.net", nameServers[0]);
            Assert.Equal("ns2.p31.dynect.net", nameServers[1]);
            Assert.Equal("pdns1.ultradns.net", nameServers[2]);
            Assert.Equal("pdns6.ultradns.co.uk", nameServers[3]);
        }
        catch
        {
            if (result.Diagnostics != null)
            {
                Output.WriteLine(result.Diagnostics.RenderAlignment());
                Output.WriteLine("---");
                Output.WriteLine(result.Diagnostics.Summary.Verdict);
                foreach (var issue in result.Diagnostics.Summary.Issues)
                {
                    Output.WriteLine($"  {issue.Type}: {issue.TokenName} — {issue.Description}");
                    if (issue.Hint != null)
                        Output.WriteLine($"    Hint: {issue.Hint}");
                }
            }
            throw;
        }
    }

    [Fact]
    public void TestGoogleVg()
    {
        var template = ReadTemplate("whois.vg");
        var input = ReadData("google.vg");

        var result = tokenizer.Tokenize(template, input);

        Assert.True(result.Success);
        Assert.Equal(53, result.Matches.Count);
    }

    [Fact]
    public void TestVgNotFound()
    {
        var template = ReadTemplate("whois.vg.not.found");
        var input = ReadData("not.found.vg");

        var result = tokenizer.Tokenize(template, input);

        Assert.True(result.Success);
        Assert.Single(result.Matches);
    }


    [Fact]
    public void TestGoogleCc()
    {
        var template = ReadTemplate("whois.cc");
        var input = ReadData("google.cc");

        var result = tokenizer.Tokenize(template, input);

        Assert.True(result.Success);
        Assert.Equal(22, result.Matches.Count);

        var nameServers = result.All("NameServers");

        Assert.Equal(4, nameServers.Count);
        Assert.Equal("ns1.google.com", nameServers[0]);
        Assert.Equal("ns2.google.com", nameServers[1]);
        Assert.Equal("ns3.google.com", nameServers[2]);
        Assert.Equal("ns4.google.com", nameServers[3]);
    }

    [Fact]
    public void TestGoogleCoZa()
    {
        var template = ReadTemplate("whois.generic");
        var input = ReadData("google.co.za");

        var result = tokenizer.Tokenize(template, input);

        Assert.True(result.Success);
        Assert.Equal(58, result.Matches.Count);

        Assert.Equal("google.co.za", result.First("DomainName"));

        var nameServers = result.All("NameServers");

        Assert.Equal(4, nameServers.Count);
        Assert.Equal("ns1.google.com", nameServers[0]);
        Assert.Equal("ns2.google.com", nameServers[1]);
        Assert.Equal("ns3.google.com", nameServers[2]);
        Assert.Equal("ns4.google.com", nameServers[3]);
    }

    [Fact]
    public void TestGoogleBiz()
    {
        var template = ReadTemplate("whois.generic");
        var input = ReadData("google.biz");

        var result = tokenizer.Tokenize(template, input);

        Assert.True(result.Success);
        Assert.Equal(52, result.Matches.Count);

        var nameServers = result.All("NameServers");

        Assert.Equal(4, nameServers.Count);
        Assert.Equal("ns1.google.com", nameServers[0]);
        Assert.Equal("ns2.google.com", nameServers[1]);
        Assert.Equal("ns4.google.com", nameServers[2]);
        Assert.Equal("ns3.google.com", nameServers[3]);
    }

    [Fact]
    public void TestTokenMatcherCom()
    {
        var template = ReadTemplate("whois.iana");
        var input = ReadData("com");

        var result = tokenizer.Tokenize(template, input);

        Assert.Equal(39, result.Matches.Count);
    }

    [Fact]
    public void TestTokenMatcherCoCa()
    {
        var template = ReadTemplate("whois.co.ca");
        var input = ReadData("available.co.ca");

        var matcher = new TokenMatcher();

        matcher.RegisterTemplate(template);

        var match = matcher.Match(input);

        Assert.Equal(match.BestMatch!.First("DomainName"), "u34jedzcq.co.ca");
        Assert.Equal(match.BestMatch.First("Status"), "NotFound");
    }

    [Fact]
    public void TestWhoisEuOrg()
    {
        var template = ReadTemplate("whois.eu.org");
        var input = ReadData("google.eu.org");

        var result = tokenizer.Tokenize(template, input);

        Assert.Equal(result.First("DomainName"), "google.eu.org");
    }

    [Fact]
    public void TestWhoisGoogleTr()
    {
        var template = ReadTemplate("whois.tr");
        var input = ReadData("google.tr");

        var result = tokenizer.Tokenize(template, input);

        Assert.Equal(new DateTime(2001, 08, 23), result.First("Registered"));
    }

    [Fact]
    // Diagnostics: 33 of 40 matched. Missed: Registrant.TelephoneNumber, Registrant.FaxNumber,
    // Expiration, Updated, Registered, DomainStatus (preamble never found), plus NameServers
    // validator failures. The date/status tokens use Spanish-language preambles not in template.
    public void TestWhoisVe()
    {
        var template = ReadTemplate("whois.ve");
        var input = ReadData("aloespa.com.ve");

        var diagTokenizer = CreateDiagnosticTokenizer();
        var result = diagTokenizer.Tokenize(template, input);

        try
        {
            Assert.Equal("Rafael Perez", result.First("Registrant.Name"));
            Assert.Equal("aloespa.com.ve-dom", result.First("Registrant.RegistryId"));
            Assert.Equal("registro@tepuynet.com", result.First("Registrant.Email"));

            Assert.Equal(3, result.All("Registrant.Address").Count);
            Assert.Equal("Rafael Perez", result.All("Registrant.Address")[0]);
            Assert.Equal("Caracas", result.All("Registrant.Address")[1]);
            Assert.Equal("Caracas, D. Federal  VE", result.All("Registrant.Address")[2]);

            Assert.Equal("aloespa.com.ve", result.First("DomainName"));
            Assert.Equal("Tepuynet", result.First("AdminContact.Name"));
            Assert.Equal("aloespa.com.ve-adm", result.First("AdminContact.RegistryId"));
            Assert.Equal("registro@tepuynet.com", result.First("AdminContact.Email"));

            Assert.Equal(3, result.All("AdminContact.Address").Count);
            Assert.Equal("Tepuynet C.A.", result.All("AdminContact.Address")[0]);
            Assert.Equal("Av. Bolivar Norte Torre Banaven, Piso 9 Ofic. 9-9", result.All("AdminContact.Address")[1]);
            Assert.Equal("Valencia, Carabobo  VE", result.All("AdminContact.Address")[2]);

            Assert.Equal("2418246437", result.First("AdminContact.TelephoneNumber"));
            Assert.Equal("2418246437", result.First("AdminContact.FaxNumber"));
            Assert.Equal("Tepuynet", result.First("TechnicalContact.Name"));
            Assert.Equal("aloespa.com.ve-tec", result.First("TechnicalContact.RegistryId"));
            Assert.Equal("registro@tepuynet.com", result.First("TechnicalContact.Email"));

            Assert.Equal(3, result.All("TechnicalContact.Address").Count);
            Assert.Equal("Tepuynet C.A.", result.All("TechnicalContact.Address")[0]);
            Assert.Equal("Av. Bolivar Norte Torre Banaven, Piso 9 Ofic. 9-9", result.All("TechnicalContact.Address")[1]);
            Assert.Equal("Valencia, Carabobo  VE", result.All("TechnicalContact.Address")[2]);

            Assert.Equal("2418246437", result.First("TechnicalContact.TelephoneNumber"));
            Assert.Equal("2418246437", result.First("TechnicalContact.FaxNumber"));
            Assert.Equal("Tepuynet", result.First("BillingContact.Name"));
            Assert.Equal("aloespa.com.ve-bil", result.First("BillingContact.RegistryId"));
            Assert.Equal("registro@tepuynet.com", result.First("BillingContact.Email"));

            Assert.Equal(3, result.All("BillingContact.Address").Count);
            Assert.Equal("Tepuynet C.A.", result.All("BillingContact.Address")[0]);
            Assert.Equal("Av. Bolivar Norte Torre Banaven, Piso 9 Ofic. 9-9", result.All("BillingContact.Address")[1]);
            Assert.Equal("Valencia, Carabobo  VE", result.All("BillingContact.Address")[2]);

            Assert.Equal("2418246437", result.First("BillingContact.TelephoneNumber"));
            Assert.Equal("2418246437", result.First("BillingContact.FaxNumber"));
            Assert.Equal(new DateTime(2010, 11, 21, 15, 21, 32, 000, DateTimeKind.Utc), result.First("Expiration"));
            Assert.Equal(new DateTime(2006, 06, 08, 21, 54, 41, 000, DateTimeKind.Utc), result.First("Updated"));
            Assert.Equal(new DateTime(2005, 11, 21, 15, 21, 32, 000, DateTimeKind.Utc), result.First("Registered"));
            Assert.Equal("SUSPENDIDO", result.First("DomainStatus"));

            Assert.Equal(2, result.All("NameServers").Count);
            Assert.Equal("ns10.tepuyserver.net", result.All("NameServers")[0]);
            Assert.Equal("ns9.tepuyserver.net", result.All("NameServers")[1]);

            Assert.Equal("Found", result.First("Status"));
        }
        catch
        {
            if (result.Diagnostics != null)
            {
                Output.WriteLine(result.Diagnostics.RenderAlignment());
                Output.WriteLine("---");
                Output.WriteLine(result.Diagnostics.Summary.Verdict);
                foreach (var issue in result.Diagnostics.Summary.Issues)
                {
                    Output.WriteLine($"  {issue.Type}: {issue.TokenName} — {issue.Description}");
                    if (issue.Hint != null)
                        Output.WriteLine($"    Hint: {issue.Hint}");
                }
            }
            throw;
        }
    }

    [Fact()]
    //[Fact(Skip = "Ignore until debug process is finished")]
    public void TestWhoisVeDates()
    {
        var template = """
                          Fecha de Vencimiento: { Expiration ? : ToDateTimeUtc("yyyy-MM-dd HH:mm:ss"), EOL }
                          Ultima Actualizacion: { Updated ? : ToDateTimeUtc("yyyy-MM-dd HH:mm:ss"), EOL }
                          Fecha de Creacion: { Registered ? : ToDateTimeUtc("yyyy-MM-dd HH:mm:ss"), EOL }
                       """;
        var input = """
                       Fecha de Vencimiento: 2010-11-21 15:21:32
                       Ultima Actualizacion: 2006-06-08 21:54:41
                       Fecha de Creacion: 2005-11-21 15:21:32
                    """;

        ReadData("aloespa.com.ve");

        var result = tokenizer.Tokenize(template, input);

        Assert.Equal(new DateTime(2010, 11, 21, 15, 21, 32, 000, DateTimeKind.Utc), result.First("Expiration"));
        Assert.Equal(new DateTime(2006, 06, 08, 21, 54, 41, 000, DateTimeKind.Utc), result.First("Updated"));
        Assert.Equal(new DateTime(2005, 11, 21, 15, 21, 32, 000, DateTimeKind.Utc), result.First("Registered"));
    }

    [Fact]
    public void TestWhoisCoop()
    {
        var template = ReadTemplate("whois.coop");
        var input = ReadData("moscowfood.coop");

        var result = tokenizer.Tokenize(template, input);

        Assert.Equal("5662D-COOP", result.First("RegistryDomainId"));
        Assert.Equal("moscowfood.coop", result.First("DomainName"));
        Assert.Equal(new DateTime(2013, 01, 30, 00, 00, 00, 000, DateTimeKind.Utc), result.First("Expiration"));

        Assert.Equal(3, result.All("DomainStatus").Count);
        Assert.Equal("clientDeleteProhibited", result.All("DomainStatus")[0]);
        Assert.Equal("clientTransferProhibited", result.All("DomainStatus")[1]);
        Assert.Equal("clientUpdateProhibited", result.All("DomainStatus")[2]);

        Assert.Equal("Domain Bank Inc.", result.First("Registrar.Name"));
        Assert.Equal("31", result.First("Registrar.IanaId"));
        Assert.Equal(new DateTime(2001, 10, 09, 04, 36, 36, 000, DateTimeKind.Utc), result.First("Registered"));
        Assert.Equal("registrant", result.First("Type"));
        Assert.Equal("71764C-COOP", result.First("Contact.Id"));
        Assert.Equal("Kenna Eaton", result.First("Contact.Name"));
        Assert.Equal("Moscow Food Co-op", result.First("Contact.Organization"));

        Assert.Equal(5, result.All("Address").Count);
        Assert.Equal("P. O. Box 9485", result.All("Address")[0]);
        Assert.Equal("Moscow", result.All("Address")[1]);
        Assert.Equal("ID", result.All("Address")[2]);
        Assert.Equal("83843", result.All("Address")[3]);
        Assert.Equal("United States", result.All("Address")[4]);

        Assert.Equal("+1.2088828537", result.First("Phone"));
        Assert.Equal("+1.2088828082", result.First("Fax"));

        Assert.Equal(4, result.All("Email").Count);
        Assert.Equal("kenna@moscowfood.coop", result.All("Email")[0]);
        Assert.Equal("outreach@moscowfood.coop", result.All("Email")[1]);
        Assert.Equal("payable@moscowfood.coop", result.All("Email")[2]);
        Assert.Equal("joseph@moscowfood.coop", result.All("Email")[3]);


        Assert.Equal(2, result.All("NameServers").Count);
        Assert.Equal("ns2.west-datacenter.net", result.All("NameServers")[0]);
        Assert.Equal("ns1.west-datacenter.net", result.All("NameServers")[1]);

        Assert.Equal("Found", result.First("Status"));
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
