using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class HintTests : TokenizerTestBase
{
    private readonly ITokenizer tokenizer;

    private class Student
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;
    }

    public HintTests(ITestOutputHelper output) : base(output)
    {
        tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenPatternWithHint_WhenHintFoundInInput_ThenExtractsValueAndRecordsHintMatch()
    {
        // Arrange
        const string pattern = """
                               ---
                               Hint: First Name
                               ---
                               First Name: {FirstName}
                               """;
        const string input = "First Name: Alice";

        // Act
        var template = tokenizer.Compile(pattern);
        var result = tokenizer.Tokenize<Student>(template, input);

        // Assert
        Assert.Equal("Alice", result.Value.FirstName);
        Assert.Single(result.Hints.Matches);
        Assert.Equal("First Name", result.Hints.Matches[0].Text);
        Assert.False(result.Hints.Matches[0].Optional);
        Assert.Empty(result.Hints.Misses);
    }

    [Fact]
    public void GivenPatternWithHint_WhenHintNotFoundInInput_ThenRecordsHintMiss()
    {
        // Arrange
        const string pattern = """
                               ---
                               Hint: Last Name
                               ---
                               First Name: {FirstName}
                               """;
        const string input = "First Name: Alice";

        // Act
        var template = tokenizer.Compile(pattern);
        var result = tokenizer.Tokenize<Student>(template, input);

        // Assert
        Assert.Null(result.Value.FirstName);
        Assert.Empty(result.Hints.Matches);
        Assert.Single(result.Hints.Misses);
        Assert.Equal("Last Name", result.Hints.Misses[0].Text);
        Assert.False(result.Hints.Misses[0].Optional);
    }

    [Fact]
    public void GivenPatternWithTwoHints_WhenBothHintsFoundInInput_ThenExtractsValuesAndRecordsBothMatches()
    {
        // Arrange
        const string pattern = """
                               ---
                               Hint: First Name
                               Hint?: Last Name
                               ---
                               First Name: {FirstName:Trim} Last Name: {LastName}
                               """;
        const string input = "First Name: Alice  Last Name: Smith";

        // Act
        var template = tokenizer.Compile(pattern);
        var result = tokenizer.Tokenize<Student>(template, input);

        // Assert
        Assert.Equal("Alice", result.Value.FirstName);
        Assert.Equal("Smith", result.Value.LastName);
        Assert.Equal(2, result.Hints.Matches.Count);
        Assert.Equal("First Name", result.Hints.Matches[0].Text);
        Assert.False(result.Hints.Matches[0].Optional);
        Assert.Equal("Last Name", result.Hints.Matches[1].Text);
        Assert.True(result.Hints.Matches[1].Optional);
        Assert.Empty(result.Hints.Misses);
    }

    [Fact]
    public void GivenPatternWithTwoHints_WhenOnlyOneHintFoundInInput_ThenRecordsOneMatchAndOneMiss()
    {
        // Arrange
        const string pattern = """
                               ---
                               Hint: First Name
                               Hint?: Middle Name
                               ---
                               First Name: {FirstName:Trim} Last Name: {LastName}
                               """;
        const string input = "First Name: Alice  Last Name: Smith";

        // Act
        var template = tokenizer.Compile(pattern);
        var result = tokenizer.Tokenize<Student>(template, input);

        // Assert
        Assert.Equal("Alice", result.Value.FirstName);
        Assert.Equal("Smith", result.Value.LastName);
        Assert.Single(result.Hints.Matches);
        Assert.Equal("First Name", result.Hints.Matches[0].Text);
        Assert.False(result.Hints.Matches[0].Optional);
        Assert.Single(result.Hints.Misses);
        Assert.Equal("Middle Name", result.Hints.Misses[0].Text);
        Assert.True(result.Hints.Misses[0].Optional);
    }

    [Fact]
    public void GivenHintWithMultipleSpaces_WhenInputHasSameWhitespace_ThenHintMatches()
    {
        // Arrange
        const string pattern = "---\nhint: Domain status:         available\n---\nDomain name: {FirstName}\n";
        const string input = "Domain name: example.com\nDomain status:         available\n";

        // Act
        var template = tokenizer.Compile(pattern);
        var result = tokenizer.Tokenize<Student>(template, input);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Hints.Matches);
        Assert.Equal("Domain status:         available", result.Hints.Matches[0].Text);
    }
}
