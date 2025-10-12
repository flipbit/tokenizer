using Xunit;

namespace Tokens;

public class HintTests
{
    private readonly Tokenizer tokenizer;

    private class Student
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }
    }

    public HintTests()
    {
        SerilogConfig.Init();

        tokenizer = new Tokenizer(new TokenizerOptions{ EnableLogging = true });
    }

    [Fact]
    public void TestOneHintFound()
    {
        const string pattern = """
                               ---
                               Hint: First Name
                               ---
                               First Name: {FirstName}
                               """;
        const string input = "First Name: Alice";

        var result = tokenizer.Tokenize<Student>(pattern, input);

        Assert.Equal("Alice", result.Value.FirstName);

        Assert.Single(result.Hints.Matches);
        Assert.Equal("First Name", result.Hints.Matches[0].Text);
        Assert.False(result.Hints.Matches[0].Optional);

        Assert.Empty(result.Hints.Misses);
    }

    [Fact]
    public void TestOneHintNotFound()
    {
        const string pattern = """
                               ---
                               Hint: Last Name
                               ---
                               First Name: {FirstName}
                               """;
        const string input = "First Name: Alice";

        var result = tokenizer.Tokenize<Student>(pattern, input);

        Assert.Null(result.Value.FirstName);

        Assert.Empty(result.Hints.Matches);

        Assert.Single(result.Hints.Misses);
        Assert.Equal("Last Name", result.Hints.Misses[0].Text);
        Assert.False(result.Hints.Misses[0].Optional);
    }

    [Fact]
    public void TestTwoHintsFound()
    {
        const string pattern = """
                               ---
                               Hint: First Name
                               Hint?: Last Name
                               ---
                               First Name: {FirstName:Trim} Last Name: {LastName}
                               """;
        const string input = "First Name: Alice  Last Name: Smith";

        var result = tokenizer.Tokenize<Student>(pattern, input);

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
    public void TestTwoHintsMixed()
    {
        const string pattern = """
                               ---
                               Hint: First Name
                               Hint?: Middle Name
                               ---
                               First Name: {FirstName:Trim} Last Name: {LastName}
                               """;
        const string input = "First Name: Alice  Last Name: Smith";

        var result = tokenizer.Tokenize<Student>(pattern, input);

        Assert.Equal("Alice", result.Value.FirstName);
        Assert.Equal("Smith", result.Value.LastName);

        Assert.Single(result.Hints.Matches);
        Assert.Equal("First Name", result.Hints.Matches[0].Text);
        Assert.False(result.Hints.Matches[0].Optional);

        Assert.Single(result.Hints.Misses);
        Assert.Equal("Middle Name", result.Hints.Misses[0].Text);
        Assert.True(result.Hints.Misses[0].Optional);
    }
}