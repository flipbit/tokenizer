using Xunit;
using Xunit.Abstractions;

namespace Tokens.Types;

public class BoolTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    private class Student
    {
        public string Name { get; set; } = null!;

        public bool Enrolled { get; set; }
    }

    public BoolTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public void TestSetBoolValueWhenTrue()
    {
        const string pattern = @"Name: {Name}, Enrolled: {Enrolled}";
        const string input = @"Name: Alice, Enrolled: true";

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Student>(template, input);

        Assert.Equal("Alice", result.Value.Name);
        Assert.True(result.Value.Enrolled);
    }

    [Fact]
    public void TestSetBoolValueWhenTrueAndUpperCase()
    {
        const string pattern = @"Name: {Name}, Enrolled: {Enrolled}";
        const string input = @"Name: Alice, Enrolled: TRUE";

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Student>(template, input);

        Assert.Equal("Alice", result.Value.Name);
        Assert.True(result.Value.Enrolled);
    }

    [Fact]
    public void TestSetBoolValueWhenFalse()
    {
        const string pattern = @"Name: {Name}, Enrolled: {Enrolled}";
        const string input = @"Name: Alice, Enrolled: False";

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Student>(template, input);

        Assert.Equal("Alice", result.Value.Name);
        Assert.False(result.Value.Enrolled);
    }
}
