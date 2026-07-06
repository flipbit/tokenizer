using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class ListTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    public ListTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenPatternWithRepeatingToken_WhenTokenizingMultipleValues_ThenExtractsAllValuesInList()
    {
        // Arrange
        const string pattern = "Domains:\n{ DomainName : Repeating, IsDomainName }\n\n{ SecondaryDomain }\n";
        const string input = "Domains:\none.com\ntwo.com\nthree.com\n\nsecondary.com\n";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var results = _tokenizer.Tokenize(template, input);
        var domains = results.Matches.Where(m => m.Token.Name == "DomainName").ToList();

        // Assert
        Assert.Equal(3, domains.Count);
        Assert.Equal("one.com", domains[0].Value);
        Assert.Equal("two.com", domains[1].Value);
        Assert.Equal("three.com", domains[2].Value);
        Assert.Equal("secondary.com", results.First("SecondaryDomain"));
    }
}
