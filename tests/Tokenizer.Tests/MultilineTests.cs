using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class MultilineTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    private class Student
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public List<string> Classes { get; set; } = null!;
    }

    public MultilineTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenMultilinePatternWithRepeatingToken_WhenTokenizingMultilineInput_ThenExtractsAllValues()
    {
        // Arrange
        const string pattern = "First Name:\n  {FirstName $ }\n\nClasses:\n  {Classes $ * }\n\nLast Name:\n  {LastName $ }\n\n";
        const string input = "First Name:\n  Alice\n\nClasses:\n  French\n  History\n  Maths\n\nLast Name:\n  Smith\n\n";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Student>(template, input);

        // Assert
        Assert.Equal("Alice", result.Value.FirstName);
        Assert.Equal(3, result.Value.Classes.Count);
        Assert.Equal("French", result.Value.Classes[0]);
        Assert.Equal("History", result.Value.Classes[1]);
        Assert.Equal("Maths", result.Value.Classes[2]);
        Assert.Equal("Smith", result.Value.LastName);
    }

    [Fact]
    public void GivenIndentedMultilinePatternWithRepeatingToken_WhenTokenizingIndentedInput_ThenExtractsAllValues()
    {
        // Arrange
        const string pattern = "    Relevant dates:\n        Registered on: {FirstName}\n        Expiry date:  {LastName}\n\n    Registration status:\n        Registered until expiry date.\n\n    Name servers:\n        { Classes $ *}\n\n";
        const string input = "    Relevant dates:\n        Registered on: Alice\n        Expiry date:  Smith\n\n    Registration status:\n        Registered until expiry date.\n\n    Name servers:\n        ns1.rbsov.bbc.co.uk       212.58.241.67\n        ns1.tcams.bbc.co.uk       212.72.49.3\n        ns1.thdow.bbc.co.uk       212.58.240.163\n";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Student>(template, input);

        // Assert
        Assert.Equal("Alice", result.Value.FirstName);
        Assert.Equal(3, result.Value.Classes.Count);
        Assert.Equal("ns1.rbsov.bbc.co.uk       212.58.241.67", result.Value.Classes[0]);
        Assert.Equal("ns1.tcams.bbc.co.uk       212.72.49.3", result.Value.Classes[1]);
        Assert.Equal("ns1.thdow.bbc.co.uk       212.58.240.163", result.Value.Classes[2]);
        Assert.Equal("Smith", result.Value.LastName);
    }
}
