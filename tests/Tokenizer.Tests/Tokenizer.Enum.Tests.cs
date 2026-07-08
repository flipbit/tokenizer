using Xunit;
using Xunit.Abstractions;

#pragma warning disable MA0048 // Scenario test: Tokenizer.Enum.Tests.cs
namespace Tokens;

public class EnumTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    private sealed class Student
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
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public void TestSetEnumValue()
    {
        const string pattern = @"Name: {Name}, Grade: {Grade}";
        const string input = @"Name: Alice, Grade: GradeB";

        var template = _tokenizer.Compile(pattern).Template;
        var student = _tokenizer.Tokenize<Student>(template, input);

        Assert.Equal("Alice", student!.Name);
        Assert.Equal(Grade.GradeB, student.Grade);
    }

    [Fact]
    public void TestSetEnumValueWhenWrongCase()
    {
        const string pattern = @"Name: {Name}, Grade: {Grade}";
        const string input = @"Name: Alice, Grade: Gradec";

        var template = _tokenizer.Compile(pattern).Template;
        var student = _tokenizer.Tokenize<Student>(template, input);

        Assert.Equal("Alice", student!.Name);
        Assert.Equal(Grade.GradeC, student.Grade);
    }

    [Fact]
    public void TestSetEnumValueWhenIncorrectValue()
    {
        const string pattern = @"Name: {Name}, Grade: {Grade}";
        const string input = @"Name: Alice, Grade: GradeE";

        var template = _tokenizer.Compile(pattern).Template;
        var ex = Assert.Throws<Exceptions.AssignmentFailedException>(() => _tokenizer.Tokenize<Student>(template, input));
        Assert.Single(ex.Errors);
    }
}
