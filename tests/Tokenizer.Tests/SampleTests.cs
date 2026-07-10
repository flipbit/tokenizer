using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class SampleTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    public SampleTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
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
        var compiled = diagTokenizer.Compile(template).Template;
        var result = diagTokenizer.Tokenize(compiled, input);

        try
        {
            Assert.NotNull(result);

            Assert.Equal("bbc.co.uk", result.Matches.First(m => string.Equals(m.Token.Name, "DomainName", StringComparison.Ordinal)).Value);
            Assert.Equal("British Broadcasting Corporation", result.Matches.First(m => string.Equals(m.Token.Name, "Registrant.Name", StringComparison.Ordinal)).Value);

            var registrantAddress = result.Matches.Where(m => string.Equals(m.Token.Name, "Registrant.Address", StringComparison.Ordinal)).Select(m => m.Value).ToList();
            Assert.Equal(6, registrantAddress.Count);
            Assert.Equal("British Broadcasting Corporation", registrantAddress[0]);
            Assert.Equal("Broadcasting House", registrantAddress[1]);
            Assert.Equal("Portland Place", registrantAddress[2]);
            Assert.Equal("London", registrantAddress[3]);
            Assert.Equal("W1A 1AA", registrantAddress[4]);
            Assert.Equal("United Kingdom", registrantAddress[5]);

            Assert.Equal("British Broadcasting Corporation [Tag = BBC]", result.Matches.First(m => string.Equals(m.Token.Name, "Registrar.Name", StringComparison.Ordinal)).Value);
            Assert.Equal("http://www.bbc.co.uk", result.Matches.First(m => string.Equals(m.Token.Name, "Registrar.Url", StringComparison.Ordinal)).Value);
            Assert.Equal(new DateTimeOffset(1996, 08, 01, 00, 00, 00, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "Registered", StringComparison.Ordinal)).Value);
            Assert.Equal(new DateTimeOffset(2014, 12, 13, 00, 00, 00, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "Expiration", StringComparison.Ordinal)).Value);
            Assert.Equal(new DateTimeOffset(2014, 06, 12, 00, 00, 00, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "Updated", StringComparison.Ordinal)).Value);
            Assert.Equal("Registered until expiry date.", result.Matches.First(m => string.Equals(m.Token.Name, "DomainStatus", StringComparison.Ordinal)).Value);

            var nameServers = result.Matches.Where(m => string.Equals(m.Token.Name, "NameServers", StringComparison.Ordinal)).Select(m => m.Value).ToList();
            Assert.Equal(3, nameServers.Count);
            Assert.Equal("ns1.rbsov.bbc.co.uk", nameServers[0]);
            Assert.Equal("ns1.tcams.bbc.co.uk", nameServers[1]);
            Assert.Equal("ns1.thdow.bbc.co.uk", nameServers[2]);

            Assert.Equal("Found", result.Matches.First(m => string.Equals(m.Token.Name, "Status", StringComparison.Ordinal)).Value);
        }
        catch
        {
            if (result.Diagnostics != null)
            {
                Output.WriteLine(result.Diagnostics.RenderAlignment());
                Output.WriteLine("---");
                Output.WriteLine(result.Diagnostics.Verdict);
                foreach (var token in result.Diagnostics.Tokens)
                {
                    foreach (var issue in token.Issues)
                    {
                        Output.WriteLine($"  {issue.Type}: {issue.TokenName} — {issue.Description}");
                        if (issue.Hint != null)
                            Output.WriteLine($"    Hint: {issue.Hint}");
                    }
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

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);

        Assert.Equal("com", result.Matches.First(m => string.Equals(m.Token.Name, "Tld", StringComparison.Ordinal)).Value);
        Assert.Equal("VeriSign Global Registry Services", result.Matches.First(m => string.Equals(m.Token.Name, "Organization.Name", StringComparison.Ordinal)).Value);

        var orgAddress = result.Matches.Where(m => string.Equals(m.Token.Name, "Organization.Address", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(3, orgAddress.Count);
        Assert.Equal("12061 Bluemont Way", orgAddress[0]);
        Assert.Equal("Reston Virginia 20190", orgAddress[1]);
        Assert.Equal("United States", orgAddress[2]);

        Assert.Equal("Registry Customer Service", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.Name", StringComparison.Ordinal)).Value);
        Assert.Equal("VeriSign Global Registry Services", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.Organization", StringComparison.Ordinal)).Value);

        var adminAddress = result.Matches.Where(m => string.Equals(m.Token.Name, "AdminContact.Address", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(3, adminAddress.Count);
        Assert.Equal("12061 Bluemont Way", adminAddress[0]);
        Assert.Equal("Reston Virginia 20190", adminAddress[1]);
        Assert.Equal("United States", adminAddress[2]);

        Assert.Equal("+1 703 925-6999", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.TelephoneNumber", StringComparison.Ordinal)).Value);
        Assert.Equal("+1 703 948 3978", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.FaxNumber", StringComparison.Ordinal)).Value);
        Assert.Equal("info@verisign-grs.com", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.Email", StringComparison.Ordinal)).Value);
        Assert.Equal("Registry Customer Service", result.Matches.First(m => string.Equals(m.Token.Name, "TechContact.Name", StringComparison.Ordinal)).Value);
        Assert.Equal("VeriSign Global Registry Services", result.Matches.First(m => string.Equals(m.Token.Name, "TechContact.Organization", StringComparison.Ordinal)).Value);

        var techAddress = result.Matches.Where(m => string.Equals(m.Token.Name, "TechContact.Address", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(3, techAddress.Count);
        Assert.Equal("12061 Bluemont Way", techAddress[0]);
        Assert.Equal("Reston Virginia 20190", techAddress[1]);
        Assert.Equal("United States", techAddress[2]);

        Assert.Equal("+1 703 925-6999", result.Matches.First(m => string.Equals(m.Token.Name, "TechContact.TelephoneNumber", StringComparison.Ordinal)).Value);
        Assert.Equal("+1 703 948 3978", result.Matches.First(m => string.Equals(m.Token.Name, "TechContact.FaxNumber", StringComparison.Ordinal)).Value);
        Assert.Equal("info@verisign-grs.com", result.Matches.First(m => string.Equals(m.Token.Name, "TechContact.Email", StringComparison.Ordinal)).Value);

        var nameServers = result.Matches.Where(m => string.Equals(m.Token.Name, "NameServers", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(13, nameServers.Count);
        Assert.Equal("A.GTLD-SERVERS.NET 192.5.6.30 2001:503:a83e:0:0:0:2:30", nameServers[0]);
        Assert.Equal("B.GTLD-SERVERS.NET 192.33.14.30 2001:503:231d:0:0:0:2:30", nameServers[1]);
        Assert.Equal("C.GTLD-SERVERS.NET 192.26.92.30", nameServers[2]);
        Assert.Equal("D.GTLD-SERVERS.NET 192.31.80.30", nameServers[3]);
        Assert.Equal("E.GTLD-SERVERS.NET 192.12.94.30", nameServers[4]);
        Assert.Equal("F.GTLD-SERVERS.NET 192.35.51.30", nameServers[5]);
        Assert.Equal("G.GTLD-SERVERS.NET 192.42.93.30", nameServers[6]);
        Assert.Equal("H.GTLD-SERVERS.NET 192.54.112.30", nameServers[7]);
        Assert.Equal("I.GTLD-SERVERS.NET 192.43.172.30", nameServers[8]);
        Assert.Equal("J.GTLD-SERVERS.NET 192.48.79.30", nameServers[9]);
        Assert.Equal("K.GTLD-SERVERS.NET 192.52.178.30", nameServers[10]);
        Assert.Equal("L.GTLD-SERVERS.NET 192.41.162.30", nameServers[11]);
        Assert.Equal("M.GTLD-SERVERS.NET 192.55.83.30", nameServers[12]);

        Assert.Equal("whois.verisign-grs.com", result.Matches.First(m => string.Equals(m.Token.Name, "Url", StringComparison.Ordinal)).Value);
        Assert.Equal("Registration information: http://www.verisign-grs.com", result.Matches.First(m => string.Equals(m.Token.Name, "Remarks", StringComparison.Ordinal)).Value);
        Assert.Equal("1985-01-01", result.Matches.First(m => string.Equals(m.Token.Name, "Created", StringComparison.Ordinal)).Value);
        Assert.Equal("2012-02-15", result.Matches.First(m => string.Equals(m.Token.Name, "Changed", StringComparison.Ordinal)).Value);
        Assert.Equal("Found", result.Matches.First(m => string.Equals(m.Token.Name, "Status", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public void TestParseAbogadoData()
    {
        var pattern = ReadTemplate("whois.iana");
        var input = ReadData("abogado");

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);

        Assert.Equal("abogado", result.Matches.First(m => string.Equals(m.Token.Name, "Tld", StringComparison.Ordinal)).Value);
        Assert.Equal("Minds + Machines Group Limited", result.Matches.First(m => string.Equals(m.Token.Name, "Organization.Name", StringComparison.Ordinal)).Value);

        var orgAddress = result.Matches.Where(m => string.Equals(m.Token.Name, "Organization.Address", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(2, orgAddress.Count);
        Assert.Equal("Craigmuir Chambers, Road Town Tortola VG 1110", orgAddress[0]);
        Assert.Equal("Virgin Islands, British", orgAddress[1]);

        Assert.Equal("Admin Contact", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.Name", StringComparison.Ordinal)).Value);
        Assert.Equal("Minds + Machines Ltd", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.Organization", StringComparison.Ordinal)).Value);

        var adminAddress = result.Matches.Where(m => string.Equals(m.Token.Name, "AdminContact.Address", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(2, adminAddress.Count);
        Assert.Equal("32 Nassau St, Dublin 2", adminAddress[0]);
        Assert.Equal("Ireland", adminAddress[1]);

        Assert.Equal("+1-877-734-4783", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.TelephoneNumber", StringComparison.Ordinal)).Value);
        Assert.Equal("ops@mmx.co", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.Email", StringComparison.Ordinal)).Value);
        Assert.Equal("TLD Registry Services Technical", result.Matches.First(m => string.Equals(m.Token.Name, "TechContact.Name", StringComparison.Ordinal)).Value);
        Assert.Equal("Nominet", result.Matches.First(m => string.Equals(m.Token.Name, "TechContact.Organization", StringComparison.Ordinal)).Value);

        var techAddress = result.Matches.Where(m => string.Equals(m.Token.Name, "TechContact.Address", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(6, techAddress.Count);
        Assert.Equal("Minerva House,", techAddress[0]);
        Assert.Equal("Edmund Halley Road,", techAddress[1]);
        Assert.Equal("Oxford Science Park,", techAddress[2]);
        Assert.Equal("Oxford,", techAddress[3]);
        Assert.Equal("OX4 4DQ", techAddress[4]);
        Assert.Equal("United Kingdom", techAddress[5]);

        Assert.Equal("+44.1865332211", result.Matches.First(m => string.Equals(m.Token.Name, "TechContact.TelephoneNumber", StringComparison.Ordinal)).Value);
        Assert.Equal("registrytechnical@nominet.uk", result.Matches.First(m => string.Equals(m.Token.Name, "TechContact.Email", StringComparison.Ordinal)).Value);

        var nameServers = result.Matches.Where(m => string.Equals(m.Token.Name, "NameServers", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(8, nameServers.Count);
        Assert.Equal("DNS1.NIC.ABOGADO 213.248.217.13 2a01:618:401:0:0:0:0:13", nameServers[0]);
        Assert.Equal("DNS2.NIC.ABOGADO 103.49.81.13 2401:fd80:401:0:0:0:0:13", nameServers[1]);
        Assert.Equal("DNS3.NIC.ABOGADO 213.248.221.13 2a01:618:405:0:0:0:0:13", nameServers[2]);
        Assert.Equal("DNS4.NIC.ABOGADO 2401:fd80:405:0:0:0:0:13 43.230.49.13", nameServers[3]);
        Assert.Equal("DNSA.NIC.ABOGADO 156.154.100.3 2001:502:ad09:0:0:0:0:3", nameServers[4]);
        Assert.Equal("DNSB.NIC.ABOGADO 156.154.101.3", nameServers[5]);
        Assert.Equal("DNSC.NIC.ABOGADO 156.154.102.3", nameServers[6]);
        Assert.Equal("DNSD.NIC.ABOGADO 156.154.103.3", nameServers[7]);

        Assert.Equal("whois.nic.abogado", result.Matches.First(m => string.Equals(m.Token.Name, "Url", StringComparison.Ordinal)).Value);
        Assert.Equal("Registration information: http://mm-registry.com", result.Matches.First(m => string.Equals(m.Token.Name, "Remarks", StringComparison.Ordinal)).Value);
        Assert.Equal("2014-07-10", result.Matches.First(m => string.Equals(m.Token.Name, "Created", StringComparison.Ordinal)).Value);
        Assert.Equal("2018-06-29", result.Matches.First(m => string.Equals(m.Token.Name, "Changed", StringComparison.Ordinal)).Value);
        Assert.Equal("Found", result.Matches.First(m => string.Equals(m.Token.Name, "Status", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public void TestVerisignRedirect()
    {
        var pattern = ReadTemplate("whois.verisign-grs.com");
        var input = ReadData("facebook.com");

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);

        Assert.Equal("facebook.com", result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.Domain", StringComparison.Ordinal)).Value);
        Assert.Equal("whois.registrarsafe.com", result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.Url", StringComparison.Ordinal)).Value);
        Assert.Equal("http://www.registrarsafe.com", result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.ReferralUrl", StringComparison.Ordinal)).Value);
        Assert.Equal(new DateTimeOffset(2018, 07, 23, 18, 17, 13, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.ModifiedDate", StringComparison.Ordinal)).Value);
        Assert.Equal(new DateTimeOffset(1997, 03, 29, 05, 00, 00, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.CreatedDate", StringComparison.Ordinal)).Value);
        Assert.Equal(new DateTimeOffset(2028, 03, 30, 04, 00, 00, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.ExpirationDate", StringComparison.Ordinal)).Value);
        Assert.Equal("RegistrarSafe, LLC", result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.Registrar", StringComparison.Ordinal)).Value);

        var nameServers = result.Matches.Where(m => string.Equals(m.Token.Name, "WhoisRedirect.NameServers", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(2, nameServers.Count);
        Assert.Equal("A.NS.FACEBOOK.COM", nameServers[0]);
        Assert.Equal("B.NS.FACEBOOK.COM", nameServers[1]);
    }

    [Fact]
    public void TestWrongTemplate()
    {
        var pattern = ReadTemplate("whois.nic.br");
        var input = ReadData("08.pl");

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);

        Assert.False(result.Success);
    }

    [Fact]
    public void TestSilOrgRedirect()
    {
        var pattern = ReadTemplate("whois.verisign-grs.com");
        var input = ReadData("sil.org");

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);

        Assert.Equal("sil.org", result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.Domain", StringComparison.Ordinal)).Value);
        Assert.Equal("whois.enom.com", result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.Url", StringComparison.Ordinal)).Value);
        Assert.Equal("http://www.enom.com", result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.ReferralUrl", StringComparison.Ordinal)).Value);
        Assert.Equal(new DateTimeOffset(2018, 03, 06, 00, 17, 46, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.ModifiedDate", StringComparison.Ordinal)).Value);
        Assert.Equal(new DateTimeOffset(1991, 04, 15, 04, 00, 00, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.CreatedDate", StringComparison.Ordinal)).Value);
        Assert.Equal(new DateTimeOffset(2020, 04, 16, 04, 00, 00, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.ExpirationDate", StringComparison.Ordinal)).Value);
        Assert.Equal("eNom, Inc.", result.Matches.First(m => string.Equals(m.Token.Name, "WhoisRedirect.Registrar", StringComparison.Ordinal)).Value);

        var nameServers = result.Matches.Where(m => string.Equals(m.Token.Name, "WhoisRedirect.NameServers", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(3, nameServers.Count);
        Assert.Equal("NSJ1.WSFO.ORG", nameServers[0]);
        Assert.Equal("NSC1.WSFO.ORG", nameServers[1]);
        Assert.Equal("NSD1.WSFO.ORG", nameServers[2]);
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
        var compiled = diagTokenizer.Compile(template).Template;
        var result = diagTokenizer.Tokenize(compiled, input);

        try
        {
            Assert.True(result.Success);
            Assert.Equal(11, result.Matches.Count);

            Assert.Equal("amazon.co.jp", result.Matches.First(m => string.Equals(m.Token.Name, "DomainName", StringComparison.Ordinal)).Value);
            Assert.Equal("Amazon, Inc.", result.Matches.First(m => string.Equals(m.Token.Name, "Registrar.Name", StringComparison.Ordinal)).Value);
            Assert.Equal("JC076JP", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.Name", StringComparison.Ordinal)).Value);
            Assert.Equal("IK4644JP", result.Matches.First(m => string.Equals(m.Token.Name, "TechnicalContact.Name", StringComparison.Ordinal)).Value);
            var registered = (DateTimeOffset)result.Matches.First(m => string.Equals(m.Token.Name, "Registered", StringComparison.Ordinal)).Value;
            Assert.Equal(2002, registered.Year); Assert.Equal(11, registered.Month); Assert.Equal(21, registered.Day);
            var updated = (DateTimeOffset)result.Matches.First(m => string.Equals(m.Token.Name, "Updated", StringComparison.Ordinal)).Value;
            Assert.Equal(2018, updated.Year); Assert.Equal(12, updated.Month); Assert.Equal(1, updated.Day);

            var nameServers = result.Matches.Where(m => string.Equals(m.Token.Name, "NameServers", StringComparison.Ordinal)).Select(m => m.Value).ToList();

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
                Output.WriteLine(result.Diagnostics.Verdict);
                foreach (var token in result.Diagnostics.Tokens)
                {
                    foreach (var issue in token.Issues)
                    {
                        Output.WriteLine($"  {issue.Type}: {issue.TokenName} — {issue.Description}");
                        if (issue.Hint != null)
                            Output.WriteLine($"    Hint: {issue.Hint}");
                    }
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

        var compiled = _tokenizer.Compile(template).Template;
        var result = _tokenizer.Tokenize(compiled, input);

        Assert.True(result.Success);
        Assert.Equal(53, result.Matches.Count);
    }

    [Fact]
    public void TestVgNotFound()
    {
        var template = ReadTemplate("whois.vg.not.found");
        var input = ReadData("not.found.vg");

        var compiled = _tokenizer.Compile(template).Template;
        var result = _tokenizer.Tokenize(compiled, input);

        Assert.True(result.Success);
        Assert.Single(result.Matches);
    }


    [Fact]
    public void TestGoogleCc()
    {
        var template = ReadTemplate("whois.cc");
        var input = ReadData("google.cc");

        var compiled = _tokenizer.Compile(template).Template;
        var result = _tokenizer.Tokenize(compiled, input);

        Assert.True(result.Success);
        Assert.Equal(22, result.Matches.Count);

        var nameServers = result.Matches.Where(m => string.Equals(m.Token.Name, "NameServers", StringComparison.Ordinal)).Select(m => m.Value).ToList();

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

        var compiled = _tokenizer.Compile(template).Template;
        var result = _tokenizer.Tokenize(compiled, input);

        Assert.True(result.Success);
        Assert.Equal(58, result.Matches.Count);

        Assert.Equal("google.co.za", result.Matches.First(m => string.Equals(m.Token.Name, "DomainName", StringComparison.Ordinal)).Value);

        var nameServers = result.Matches.Where(m => string.Equals(m.Token.Name, "NameServers", StringComparison.Ordinal)).Select(m => m.Value).ToList();

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

        var compiled = _tokenizer.Compile(template).Template;
        var result = _tokenizer.Tokenize(compiled, input);

        Assert.True(result.Success);
        Assert.Equal(52, result.Matches.Count);

        var nameServers = result.Matches.Where(m => string.Equals(m.Token.Name, "NameServers", StringComparison.Ordinal)).Select(m => m.Value).ToList();

        Assert.Equal(4, nameServers.Count);
        Assert.Equal("ns1.google.com", nameServers[0]);
        Assert.Equal("ns2.google.com", nameServers[1]);
        Assert.Equal("ns4.google.com", nameServers[2]);
        Assert.Equal("ns3.google.com", nameServers[3]);
    }

    [Fact]
    public void TestTemplateMatcherCom()
    {
        var template = ReadTemplate("whois.iana");
        var input = ReadData("com");

        var compiled = _tokenizer.Compile(template).Template;
        var result = _tokenizer.Tokenize(compiled, input);

        Assert.Equal(39, result.Matches.Count);
    }

    [Fact]
    public void TestTemplateMatcherCoCa()
    {
        var template = ReadTemplate("whois.co.ca");
        var input = ReadData("available.co.ca");

        var matcher = new TemplateMatcher();

        matcher.RegisterTemplate(template);

        var match = matcher.Tokenize(input);

        Assert.Equal("u34jedzcq.co.ca", match.BestMatch!.Matches.First(m => string.Equals(m.Token.Name, "DomainName", StringComparison.Ordinal)).Value);
        Assert.Equal("NotFound", match.BestMatch.Matches.First(m => string.Equals(m.Token.Name, "Status", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public void TestWhoisEuOrg()
    {
        var template = ReadTemplate("whois.eu.org");
        var input = ReadData("google.eu.org");

        var compiled = _tokenizer.Compile(template).Template;
        var result = _tokenizer.Tokenize(compiled, input);

        Assert.Equal("google.eu.org", result.Matches.First(m => string.Equals(m.Token.Name, "DomainName", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public void TestWhoisGoogleTr()
    {
        var template = ReadTemplate("whois.tr");
        var input = ReadData("google.tr");

        var compiled = _tokenizer.Compile(template).Template;
        var result = _tokenizer.Tokenize(compiled, input);

        Assert.Equal(new DateTimeOffset(2001, 08, 23, 0, 0, 0, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "Registered", StringComparison.Ordinal)).Value);
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
        var compiled = diagTokenizer.Compile(template).Template;
        var result = diagTokenizer.Tokenize(compiled, input);

        try
        {
            Assert.Equal("Rafael Perez", result.Matches.First(m => string.Equals(m.Token.Name, "Registrant.Name", StringComparison.Ordinal)).Value);
            Assert.Equal("aloespa.com.ve-dom", result.Matches.First(m => string.Equals(m.Token.Name, "Registrant.RegistryId", StringComparison.Ordinal)).Value);
            Assert.Equal("registro@tepuynet.com", result.Matches.First(m => string.Equals(m.Token.Name, "Registrant.Email", StringComparison.Ordinal)).Value);

            var registrantAddress = result.Matches.Where(m => string.Equals(m.Token.Name, "Registrant.Address", StringComparison.Ordinal)).Select(m => m.Value).ToList();
            Assert.Equal(3, registrantAddress.Count);
            Assert.Equal("Rafael Perez", registrantAddress[0]);
            Assert.Equal("Caracas", registrantAddress[1]);
            Assert.Equal("Caracas, D. Federal  VE", registrantAddress[2]);

            Assert.Equal("aloespa.com.ve", result.Matches.First(m => string.Equals(m.Token.Name, "DomainName", StringComparison.Ordinal)).Value);
            Assert.Equal("Tepuynet", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.Name", StringComparison.Ordinal)).Value);
            Assert.Equal("aloespa.com.ve-adm", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.RegistryId", StringComparison.Ordinal)).Value);
            Assert.Equal("registro@tepuynet.com", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.Email", StringComparison.Ordinal)).Value);

            var adminAddress = result.Matches.Where(m => string.Equals(m.Token.Name, "AdminContact.Address", StringComparison.Ordinal)).Select(m => m.Value).ToList();
            Assert.Equal(3, adminAddress.Count);
            Assert.Equal("Tepuynet C.A.", adminAddress[0]);
            Assert.Equal("Av. Bolivar Norte Torre Banaven, Piso 9 Ofic. 9-9", adminAddress[1]);
            Assert.Equal("Valencia, Carabobo  VE", adminAddress[2]);

            Assert.Equal("2418246437", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.TelephoneNumber", StringComparison.Ordinal)).Value);
            Assert.Equal("2418246437", result.Matches.First(m => string.Equals(m.Token.Name, "AdminContact.FaxNumber", StringComparison.Ordinal)).Value);
            Assert.Equal("Tepuynet", result.Matches.First(m => string.Equals(m.Token.Name, "TechnicalContact.Name", StringComparison.Ordinal)).Value);
            Assert.Equal("aloespa.com.ve-tec", result.Matches.First(m => string.Equals(m.Token.Name, "TechnicalContact.RegistryId", StringComparison.Ordinal)).Value);
            Assert.Equal("registro@tepuynet.com", result.Matches.First(m => string.Equals(m.Token.Name, "TechnicalContact.Email", StringComparison.Ordinal)).Value);

            var techAddress = result.Matches.Where(m => string.Equals(m.Token.Name, "TechnicalContact.Address", StringComparison.Ordinal)).Select(m => m.Value).ToList();
            Assert.Equal(3, techAddress.Count);
            Assert.Equal("Tepuynet C.A.", techAddress[0]);
            Assert.Equal("Av. Bolivar Norte Torre Banaven, Piso 9 Ofic. 9-9", techAddress[1]);
            Assert.Equal("Valencia, Carabobo  VE", techAddress[2]);

            Assert.Equal("2418246437", result.Matches.First(m => string.Equals(m.Token.Name, "TechnicalContact.TelephoneNumber", StringComparison.Ordinal)).Value);
            Assert.Equal("2418246437", result.Matches.First(m => string.Equals(m.Token.Name, "TechnicalContact.FaxNumber", StringComparison.Ordinal)).Value);
            Assert.Equal("Tepuynet", result.Matches.First(m => string.Equals(m.Token.Name, "BillingContact.Name", StringComparison.Ordinal)).Value);
            Assert.Equal("aloespa.com.ve-bil", result.Matches.First(m => string.Equals(m.Token.Name, "BillingContact.RegistryId", StringComparison.Ordinal)).Value);
            Assert.Equal("registro@tepuynet.com", result.Matches.First(m => string.Equals(m.Token.Name, "BillingContact.Email", StringComparison.Ordinal)).Value);

            var billingAddress = result.Matches.Where(m => string.Equals(m.Token.Name, "BillingContact.Address", StringComparison.Ordinal)).Select(m => m.Value).ToList();
            Assert.Equal(3, billingAddress.Count);
            Assert.Equal("Tepuynet C.A.", billingAddress[0]);
            Assert.Equal("Av. Bolivar Norte Torre Banaven, Piso 9 Ofic. 9-9", billingAddress[1]);
            Assert.Equal("Valencia, Carabobo  VE", billingAddress[2]);

            Assert.Equal("2418246437", result.Matches.First(m => string.Equals(m.Token.Name, "BillingContact.TelephoneNumber", StringComparison.Ordinal)).Value);
            Assert.Equal("2418246437", result.Matches.First(m => string.Equals(m.Token.Name, "BillingContact.FaxNumber", StringComparison.Ordinal)).Value);
            Assert.Equal(new DateTimeOffset(2010, 11, 21, 15, 21, 32, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "Expiration", StringComparison.Ordinal)).Value);
            Assert.Equal(new DateTimeOffset(2006, 06, 08, 21, 54, 41, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "Updated", StringComparison.Ordinal)).Value);
            Assert.Equal(new DateTimeOffset(2005, 11, 21, 15, 21, 32, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "Registered", StringComparison.Ordinal)).Value);
            Assert.Equal("SUSPENDIDO", result.Matches.First(m => string.Equals(m.Token.Name, "DomainStatus", StringComparison.Ordinal)).Value);

            var nameServers = result.Matches.Where(m => string.Equals(m.Token.Name, "NameServers", StringComparison.Ordinal)).Select(m => m.Value).ToList();
            Assert.Equal(2, nameServers.Count);
            Assert.Equal("ns10.tepuyserver.net", nameServers[0]);
            Assert.Equal("ns9.tepuyserver.net", nameServers[1]);

            Assert.Equal("Found", result.Matches.First(m => string.Equals(m.Token.Name, "Status", StringComparison.Ordinal)).Value);
        }
        catch
        {
            if (result.Diagnostics != null)
            {
                Output.WriteLine(result.Diagnostics.RenderAlignment());
                Output.WriteLine("---");
                Output.WriteLine(result.Diagnostics.Verdict);
                foreach (var token in result.Diagnostics.Tokens)
                {
                    foreach (var issue in token.Issues)
                    {
                        Output.WriteLine($"  {issue.Type}: {issue.TokenName} — {issue.Description}");
                        if (issue.Hint != null)
                            Output.WriteLine($"    Hint: {issue.Hint}");
                    }
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

        var compiled = _tokenizer.Compile(template).Template;
        var result = _tokenizer.Tokenize(compiled, input);

        Assert.Equal(new DateTimeOffset(2010, 11, 21, 15, 21, 32, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "Expiration", StringComparison.Ordinal)).Value);
        Assert.Equal(new DateTimeOffset(2006, 06, 08, 21, 54, 41, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "Updated", StringComparison.Ordinal)).Value);
        Assert.Equal(new DateTimeOffset(2005, 11, 21, 15, 21, 32, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "Registered", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public void TestWhoisCoop()
    {
        var template = ReadTemplate("whois.coop");
        var input = ReadData("moscowfood.coop");

        var compiled = _tokenizer.Compile(template).Template;
        var result = _tokenizer.Tokenize(compiled, input);

        Assert.Equal("5662D-COOP", result.Matches.First(m => string.Equals(m.Token.Name, "RegistryDomainId", StringComparison.Ordinal)).Value);
        Assert.Equal("moscowfood.coop", result.Matches.First(m => string.Equals(m.Token.Name, "DomainName", StringComparison.Ordinal)).Value);
        Assert.Equal(new DateTimeOffset(2013, 01, 30, 00, 00, 00, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "Expiration", StringComparison.Ordinal)).Value);

        var domainStatuses = result.Matches.Where(m => string.Equals(m.Token.Name, "DomainStatus", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(3, domainStatuses.Count);
        Assert.Equal("clientDeleteProhibited", domainStatuses[0]);
        Assert.Equal("clientTransferProhibited", domainStatuses[1]);
        Assert.Equal("clientUpdateProhibited", domainStatuses[2]);

        Assert.Equal("Domain Bank Inc.", result.Matches.First(m => string.Equals(m.Token.Name, "Registrar.Name", StringComparison.Ordinal)).Value);
        Assert.Equal("31", result.Matches.First(m => string.Equals(m.Token.Name, "Registrar.IanaId", StringComparison.Ordinal)).Value);
        Assert.Equal(new DateTimeOffset(2001, 10, 09, 04, 36, 36, TimeSpan.Zero), result.Matches.First(m => string.Equals(m.Token.Name, "Registered", StringComparison.Ordinal)).Value);
        Assert.Equal("registrant", result.Matches.First(m => string.Equals(m.Token.Name, "Type", StringComparison.Ordinal)).Value);
        Assert.Equal("71764C-COOP", result.Matches.First(m => string.Equals(m.Token.Name, "Contact.Id", StringComparison.Ordinal)).Value);
        Assert.Equal("Kenna Eaton", result.Matches.First(m => string.Equals(m.Token.Name, "Contact.Name", StringComparison.Ordinal)).Value);
        Assert.Equal("Moscow Food Co-op", result.Matches.First(m => string.Equals(m.Token.Name, "Contact.Organization", StringComparison.Ordinal)).Value);

        var address = result.Matches.Where(m => string.Equals(m.Token.Name, "Address", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(5, address.Count);
        Assert.Equal("P. O. Box 9485", address[0]);
        Assert.Equal("Moscow", address[1]);
        Assert.Equal("ID", address[2]);
        Assert.Equal("83843", address[3]);
        Assert.Equal("United States", address[4]);

        Assert.Equal("+1.2088828537", result.Matches.First(m => string.Equals(m.Token.Name, "Phone", StringComparison.Ordinal)).Value);
        Assert.Equal("+1.2088828082", result.Matches.First(m => string.Equals(m.Token.Name, "Fax", StringComparison.Ordinal)).Value);

        var emails = result.Matches.Where(m => string.Equals(m.Token.Name, "Email", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(4, emails.Count);
        Assert.Equal("kenna@moscowfood.coop", emails[0]);
        Assert.Equal("outreach@moscowfood.coop", emails[1]);
        Assert.Equal("payable@moscowfood.coop", emails[2]);
        Assert.Equal("joseph@moscowfood.coop", emails[3]);

        var nameServers = result.Matches.Where(m => string.Equals(m.Token.Name, "NameServers", StringComparison.Ordinal)).Select(m => m.Value).ToList();
        Assert.Equal(2, nameServers.Count);
        Assert.Equal("ns2.west-datacenter.net", nameServers[0]);
        Assert.Equal("ns1.west-datacenter.net", nameServers[1]);

        Assert.Equal("Found", result.Matches.First(m => string.Equals(m.Token.Name, "Status", StringComparison.Ordinal)).Value);
    }

    private static string ReadData(string name)
    {
        return Read("Data", name);
    }

    private static string ReadTemplate(string name)
    {
        return Read("Patterns", name);
    }

    private static string Read(string type, string name)
    {
        var fileName = $@"../../../Samples/{type}/{name}.txt";

        return File.ReadAllText(fileName);
    }
}
