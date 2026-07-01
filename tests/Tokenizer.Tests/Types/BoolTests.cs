using Xunit;
using Xunit.Abstractions;

namespace Tokens.Types;

public class BoolTests : Tests.TokenizerTestBase
{
    private readonly Tokenizer tokenizer;

    private class Student
    {
        public string Name { get; set; } = null!;

        public bool Enrolled { get; set; }
    }

    public BoolTests(ITestOutputHelper output) : base(output)
    {
        tokenizer = CreateTokenizer();
    }

    [Fact]
    public void TestSetBoolValueWhenTrue()
    {
        const string pattern = @"Name: {Name}, Enrolled: {Enrolled}";
        const string input = @"Name: Alice, Enrolled: true";

        var result = tokenizer.Tokenize<Student>(pattern, input);

        Assert.Equal("Alice", result.Value.Name);
        Assert.True(result.Value.Enrolled);
    }

    [Fact]
    public void TestSetBoolValueWhenTrueAndUpperCase()
    {
        const string pattern = @"Name: {Name}, Enrolled: {Enrolled}";
        const string input = @"Name: Alice, Enrolled: TRUE";

        var result = tokenizer.Tokenize<Student>(pattern, input);

        Assert.Equal("Alice", result.Value.Name);
        Assert.True(result.Value.Enrolled);
    }

    [Fact]
    public void TestSetBoolValueWhenFalse()
    {
        const string pattern = @"Name: {Name}, Enrolled: {Enrolled}";
        const string input = @"Name: Alice, Enrolled: False";

        var result = tokenizer.Tokenize<Student>(pattern, input);

        Assert.Equal("Alice", result.Value.Name);
        Assert.False(result.Value.Enrolled);
    }
}
