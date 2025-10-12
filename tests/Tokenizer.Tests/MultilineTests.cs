using System.Collections.Generic;
using Xunit;

namespace Tokens;

public class MultilineTests
{
    private readonly Tokenizer tokenizer;

    private class Student
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public List<string> Classes { get; set; }
    }

    public MultilineTests()
    {
        SerilogConfig.Init();

        tokenizer = new Tokenizer(new TokenizerOptions{ EnableLogging = true });
    }

    [Fact]
    public void GivenMultilinePatternWithRepeatingToken_WhenTokenizingMultilineInput_ThenExtractsAllValues()
    {
        // Arrange
        const string pattern = """
                               First Name:
                                 {FirstName $ }

                               Classes:
                                 {Classes $ * }

                               Last Name:
                                 {LastName $ }

                               """;
        const string input = """
                             First Name:
                               Alice

                             Classes:
                               French
                               History
                               Maths

                             Last Name:
                               Smith

                             """;

        // Act
        var result = tokenizer.Tokenize<Student>(pattern, input);

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
        const string pattern = """
                                   Relevant dates:
                                       Registered on: {FirstName}
                                       Expiry date:  {LastName}

                                   Registration status:
                                       Registered until expiry date.

                                   Name servers:
                                       { Classes $ *}

                               """;
        const string input = """
                                 Relevant dates:
                                     Registered on: Alice
                                     Expiry date:  Smith

                                 Registration status:
                                     Registered until expiry date.

                                 Name servers:
                                     ns1.rbsov.bbc.co.uk       212.58.241.67
                                     ns1.tcams.bbc.co.uk       212.72.49.3
                                     ns1.thdow.bbc.co.uk       212.58.240.163
                             """;

        // Act
        var result = tokenizer.Tokenize<Student>(pattern, input);

        // Assert
        Assert.Equal("Alice", result.Value.FirstName);
        Assert.Equal(3, result.Value.Classes.Count);
        Assert.Equal("ns1.rbsov.bbc.co.uk       212.58.241.67", result.Value.Classes[0]);
        Assert.Equal("ns1.tcams.bbc.co.uk       212.72.49.3", result.Value.Classes[1]);
        Assert.Equal("ns1.thdow.bbc.co.uk       212.58.240.163", result.Value.Classes[2]);
        Assert.Equal("Smith", result.Value.LastName);
    }
}