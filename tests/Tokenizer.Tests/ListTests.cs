using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class ListTests : Tests.TokenizerTestBase
{
    private readonly Tokenizer tokenizer;

    public ListTests(ITestOutputHelper output) : base(output)
    {
        tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenPatternWithRepeatingToken_WhenTokenizingMultipleValues_ThenExtractsAllValuesInList()
    {
        // Arrange
        const string pattern = """
                               Domains:
                               { DomainName : Repeating, IsDomainName }

                               { SecondaryDomain }
                               """;
        const string input = """
                             Domains:
                             one.com
                             two.com
                             three.com

                             secondary.com
                             """;

        // Act
        var results = tokenizer.Tokenize(pattern, input);
        var domains = results.Matches.Where(m => m.Token.Name == "DomainName").ToList();

        // Assert
        Assert.Equal(3, domains.Count);
        Assert.Equal("one.com", domains[0].Value);
        Assert.Equal("two.com", domains[1].Value);
        Assert.Equal("three.com", domains[2].Value);
        Assert.Equal("secondary.com", results.First("SecondaryDomain"));
    }
}