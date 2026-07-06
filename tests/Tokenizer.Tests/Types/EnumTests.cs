using Xunit;
using Xunit.Abstractions;

namespace Tokens.Types;

public class EnumTests : TokenizerTestBase
{
    private readonly ITokenizer tokenizer;

    private class Student
    {
        public string Name { get; set; } = null!;

        public Grade Grade { get; set; }
    }

    private enum Grade
    {
        GradeA,
        GradeB,
        GradeC,
    }

    public EnumTests(ITestOutputHelper output) : base(output)
    {
        tokenizer = CreateTokenizer();
    }

    [Fact]
    public void TestSetEnumValue()
    {
        const string pattern = @"Name: {Name}, Grade: {Grade}";
        const string input = @"Name: Alice, Grade: GradeB";

        var template = tokenizer.Compile(pattern);
        var result = tokenizer.Tokenize<Student>(template, input);

        Assert.Equal("Alice", result.Value.Name);
        Assert.Equal(Grade.GradeB, result.Value.Grade);
    }

    [Fact]
    public void TestSetEnumValueWhenWrongCase()
    {
        const string pattern = @"Name: {Name}, Grade: {Grade}";
        const string input = @"Name: Alice, Grade: Gradec";

        var template = tokenizer.Compile(pattern);
        var result = tokenizer.Tokenize<Student>(template, input);

        Assert.Equal("Alice", result.Value.Name);
        Assert.Equal(Grade.GradeC, result.Value.Grade);
    }

    [Fact]
    public void TestSetEnumValueWhenIncorrectValue()
    {
        const string pattern = @"Name: {Name}, Grade: {Grade}";
        const string input = @"Name: Alice, Grade: GradeE";

        var template = tokenizer.Compile(pattern);
        var result = tokenizer.Tokenize<Student>(template, input);

        Assert.Equal("Alice", result.Value.Name);
        Assert.Equal(Grade.GradeA, result.Value.Grade);
        Assert.Single(result.Exceptions);
    }
}
