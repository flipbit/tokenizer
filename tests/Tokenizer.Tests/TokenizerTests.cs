using Tokens.Compilation;
using Tokens.Exceptions;
using Tokens.Transformers;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenizerTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    private sealed class Person
    {
        public string Name { get; set; } = null!;

        public int Age { get; set; }

        public DateTime Birthday { get; set; }
    }

    private sealed class TestClass
    {
        public string Message { get; set; } = null!;

        public string Name { get; set; } = null!;

        public int Counter { get; set; }

        public IList<string> List { get; set; } = null!;

        public TestClass Nested { get; set; } = null!;
    }

    private class Student
    {
        public string FirstName { get; set; } = null!;

        public string MiddleName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public DateTime Enrolled { get; set; }

        public int Number { get; set; }
    }

    private sealed class Teacher : Student
    {
        public List<string> Class { get; set; } = null!;
    }

    public TokenizerTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenPatternWithSingleToken_WhenTokenizingInput_ThenExtractsCorrectValue()
    {
        // Arrange
        const string pattern = @"First Name: {FirstName}";
        const string input = @"First Name: Alice";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var student = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Alice", student.FirstName);
    }

    [Fact]
    public void GivenPatternWithTwoTokens_WhenTokenizingInput_ThenExtractsBothValues()
    {
        // Arrange
        const string pattern = @"First Name: {Student.FirstName}, Last Name: {Student.LastName}";
        const string input = @"First Name: Alice, Last Name: Smith";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var employee = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Alice", employee.FirstName);
        Assert.Equal("Smith", employee.LastName);
    }

    [Fact]
    public void GivenPatternWithThreeTokens_WhenTokenizingInput_ThenExtractsAllThreeValues()
    {
        // Arrange
        const string pattern = @"First Name: {Student.FirstName}, Middle Name: {Student.MiddleName}, Last Name: {Student.LastName}";
        const string input = @"First Name: Alice, Middle Name: Roberta, Last Name: Smith";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var employee = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Alice", employee.FirstName);
        Assert.Equal("Roberta", employee.MiddleName);
        Assert.Equal("Smith", employee.LastName);
    }

    [Fact]
    public void GivenPatternWithLineBreaksAndOrderedTokens_WhenTokenizingMultilineInput_ThenExtractsAllValues()
    {
        // Arrange
        const string pattern =
            """
            ---
            # Tokens must appear in defined order
            OutOfOrder: false
            ---
            First Name: {FirstName}
            Middle Name: {MiddleName}
            Last Name: {LastName}
            """;
        const string input =
            """
            First Name: Alice
            Middle Name: Roberta
            Last Name: Smith
            """;

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var employee = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Alice", employee.FirstName);
        Assert.Equal("Roberta", employee.MiddleName);
        Assert.Equal("Smith", employee.LastName);
    }

    [Fact]
    public void GivenPatternWithMultilineTokens_WhenTokenizingInputWithLineBreaks_ThenPreservesLineBreaksInValues()
    {
        // Arrange
        const string pattern =
            """
            Comments:
            {FirstName}

            Name:
            {LastName}
            """;
        const string input =
            """
            Comments:
            Everything went well,
            we had a nice time.

            Name:
            Bob
            """;

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var employee = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Everything went well,\nwe had a nice time.", employee.FirstName);
        Assert.Equal("Bob", employee.LastName);
    }

    [Fact]
    public void GivenPatternWithTrailingTemplate_WhenTokenizingInput_ThenExtractsTokenValue()
    {
        // Arrange
        const string pattern = @"First Name: {Student.FirstName}, Role: Programmer";
        const string input = @"First Name: Alice, Role: Programmer";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var employee = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Alice", employee.FirstName);
    }

    [Fact]
    public void GivenPatternWithDifferentPropertyTypes_WhenTokenizingInput_ThenConvertsTypesCorrectly()
    {
        // Arrange
        const string pattern = @"First Name: {Student.FirstName}, Number: {Student.Number}";
        const string input = @"First Name: Bob, Number: 12345";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var employee = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Bob", employee.FirstName);
        Assert.Equal(12345, employee.Number);
    }

    [Fact]
    public void GivenPatternWithDateTimeTransformer_WhenTokenizingInput_ThenConvertsToDateTimeCorrectly()
    {
        // Arrange
        const string pattern = @"First Name: {FirstName}, Last Name: {LastName}, Enrolled: {Enrolled:ToDateTime('dd MMM yyyy')}";
        const string input = @"First Name: Alice, Last Name: Smith, Enrolled: 16 Jan 2018";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var employee = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Alice", employee.FirstName);
        Assert.Equal("Smith", employee.LastName);
        Assert.Equal(new DateTime(2018, 1, 16), employee.Enrolled);
    }

    [Fact]
    public void GivenPatternWithNumericValidator_WhenInputIsValidNumber_ThenExtractsValueSuccessfully()
    {
        // Arrange
        const string pattern = @"First Name: {Student.FirstName}, Number: {Student.Number:IsNumeric}";
        const string input = @"First Name: Bob, Number: 12345";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var employee = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Bob", employee.FirstName);
        Assert.Equal(12345, employee.Number);
    }

    [Fact]
    public void GivenPatternWithNumericValidator_WhenInputIsInvalidNumber_ThenUsesDefaultValue()
    {
        // Arrange
        const string pattern = @"First Name: {Student.FirstName}, Number: {Student.Number:IsNumeric}";
        const string input = @"First Name: Bob, Number: Not a number";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var employee = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Bob", employee.FirstName);
        Assert.Equal(0, employee.Number);
    }

    [Fact]
    public void GivenPatternWithNumericValidator_WhenFirstMatchIsInvalid_ThenPicksNextValidMatch()
    {
        // Arrange
        const string pattern = @"First Name: {Student.FirstName}, Number: {Student.Number:IsNumeric}";
        const string input = @"First Name: Bob, Number: (not a number), Number: 67890";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var employee = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Bob", employee.FirstName);
        Assert.Equal(67890, employee.Number);
    }

    [Fact]
    public void GivenPatternWithOptionalToken_WhenTokenIsNotPresent_ThenSucceedsWithMissRecorded()
    {
        // Arrange
        const string pattern = @"First Name: {Student.FirstName}, Middle Name: {Student.MiddleName?}, Last Name: {Student.LastName}";
        const string input = @"First Name: Bob, Last Name: Smith";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Student>(template, input);
        var student = result.Value;

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Bob", student.FirstName);
        Assert.Equal("Smith", student.LastName);
        Assert.Single(result.Tokens.Misses);
        Assert.Equal("Student.MiddleName", result.Tokens.Misses[0].Name);
    }

    [Fact]
    public void GivenPatternWithOptionalTokenAndValidator_WhenTokenIsInvalid_ThenSucceedsWithMissRecorded()
    {
        // Arrange
        const string pattern = @"First Name: {Student.FirstName}, Enrolled: {Student.Enrolled?:IsDateTime}, Last Name: {Student.LastName}";
        const string input = @"First Name: Bob, Enrolled: N/A, Last Name: Smith";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Student>(template, input);
        var student = result.Value;

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Bob", student.FirstName);
        Assert.Equal("Smith", student.LastName);
        Assert.Single(result.Tokens.Misses);
        Assert.Equal("Student.Enrolled", result.Tokens.Misses[0].Name);
    }

    [Fact]
    public void GivenPatternWithOptionalTokenAndFailingTransformer_WhenTransformerThrows_ThenSucceedsWithMissRecorded()
    {
        // Arrange
        const string pattern = @"First Name: {Student.FirstName}, Enrolled: {Student.Enrolled?:BlowsUp}, Last Name: {Student.LastName}";
        const string input = @"First Name: Bob, Enrolled: 1019-01-01, Last Name: Smith";

        // Always throws an exception
        var options = new TokenizerOptions().WithTransformer<BlowsUpTransformer>();
        var blowsUpTokenizer = CreateTokenizer(options);

        // Act
        var template = blowsUpTokenizer.Compile(pattern).Template;
        var result = blowsUpTokenizer.Tokenize<Student>(template, input);
        var student = result.Value;

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Bob", student.FirstName);
        Assert.Equal("Smith", student.LastName);
        Assert.Single(result.Tokens.Misses);
        Assert.Equal("Student.Enrolled", result.Tokens.Misses[0].Name);
    }

    [Fact]
    public void GivenPatternWithOptionalToken_WhenTokenIsPresent_ThenExtractsAllValues()
    {
        // Arrange
        const string pattern = @"First Name: {Student.FirstName}, Middle Name: {Student.MiddleName?}, Last Name: {Student.LastName}";
        const string input = @"First Name: Bob, Middle Name: Charles, Last Name: Smith";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var employee = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Bob", employee.FirstName);
        Assert.Equal("Charles", employee.MiddleName);
        Assert.Equal("Smith", employee.LastName);
    }

    [Fact]
    public void GivenPatternWithRequiredToken_WhenTokenIsNotPresent_ThenFailsWithMissRecorded()
    {
        // Arrange
        const string pattern = """
                               ---
                               OutOfOrder: true
                               ---
                               First Name: {Student.FirstName}, Middle Name: {Student.MiddleName!}, Last Name: {Student.LastName}
                               """;
        const string input = @"First Name: Bob, Last Name: Smith";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Student>(template, input);
        var student = result.Value;

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Bob", student.FirstName);
        Assert.Equal("Smith", student.LastName);
        Assert.Single(result.Tokens.Misses);
        Assert.Equal("Student.MiddleName", result.Tokens.Misses[0].Name);
    }

    [Fact]
    public void GivenPatternWithUnknownFunction_WhenTokenizing_ThenThrowsTokenizerException()
    {
        // Arrange
        const string pattern = "Hello {Student.FirstName:UnknownFunction} World";
        const string input = "Hello ... World";

        // Act & Assert
        Assert.Throws<TokenizerException>(() => _tokenizer.Tokenize<Student>(_tokenizer.Compile(pattern).Template, input));
    }

    [Fact]
    public void GivenPatternWithToken_WhenTokenIsNotPresentInInput_ThenReturnsNullValue()
    {
        // Arrange
        const string pattern = "First Name: {Student.FirstName}";
        const string input = "David";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Null(result.FirstName);
    }

    [Fact]
    public void GivenPatternWithListToken_WhenTokenizingInputWithMultipleValues_ThenExtractsAllValuesToList()
    {
        // Arrange
        const string pattern = "Student: {Teacher.Class*$}";
        const string input = "Student: Alice\r\nStudent: Bob";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Teacher>(template, input).Value;

        // Assert
        Assert.Equal(2, result.Class.Count);
        Assert.Equal("Alice", result.Class[0]);
        Assert.Equal("Bob", result.Class[1]);
    }

    [Fact]
    public void GivenPatternWithListTokenOnNewLines_WhenTokenizingMultilineInput_ThenExtractsAllValuesCorrectly()
    {
        // Arrange
        var pattern =
            """
            Name: {FirstName}
                        Student: {Class*}
                        Number: {Number}
            """;
        var input =
            """
            Name: Sue
                        Student: Alice
                        Student: Bob
                        Student: Charles
                        Number: 1234
            """;

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Teacher>(template, input).Value;

        // Assert
        Assert.Equal("Sue", result.FirstName);
        Assert.Equal(3, result.Class.Count);
        Assert.Equal("Alice", result.Class[0]);
        Assert.Equal("Bob", result.Class[1]);
        Assert.Equal("Charles", result.Class[2]);
        Assert.Equal(1234, result.Number);
    }

    [Fact]
    public void GivenPatternWithEmbeddedListToken_WhenTokenizingInput_ThenExtractsListValuesCorrectly()
    {
        // Arrange
        const string pattern = "Name: {Teacher.FirstName}, Student: {Teacher.Class*}, Number: {Teacher.Number}";
        const string input = "Name: Alice, Student: Bob, Student: Sue, Number: 1234";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Teacher>(template, input).Value;

        // Assert
        Assert.Equal("Alice", result.FirstName);
        Assert.Equal(2, result.Class.Count);
        Assert.Equal("Bob", result.Class[0]);
        Assert.Equal("Sue", result.Class[1]);
        Assert.Equal(1234, result.Number);
    }

    [Fact]
    public void GivenPatternWithMissingProperty_WhenTokenizing_ThenDoesNotThrowError()
    {
        // Arrange
        const string pattern = "Hello {TestClass.MissingPropertyName}";
        const string input = "Hello World";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<TestClass>(template, input);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenPatternWithMultipleListTokens_WhenInputHasEmptyLine_ThenStopsExtractingAtEmptyLine()
    {
        // Arrange
        const string pattern = """

                               Name servers:
                                       {TestClass.List*}
                                       {TestClass.List*}

                                   WHOIS lookup made at 10:35:59 22-Oct-2014
                               """;
        const string input = """

                             Name servers:
                                     ns1.rbsov.bbc.co.uk       212.58.241.67
                                     ns1.tcams.bbc.co.uk       212.72.49.3
                                     ns1.thdow.bbc.co.uk       212.58.240.163

                                 WHOIS lookup made at 10:35:59 22-Oct-2014
                             """;

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<TestClass>(template, input).Value;

        // Assert
        Assert.Equal(3, result.List.Count);
    }

    [Fact]
    public void GivenPatternWithMismatchedNewLines_WhenTokenizing_ThenHandlesLineEndingDifferences()
    {
        // Arrange
        const string pattern = "First Name:\n{Student.FirstName}";
        const string input = "First Name:\r\nAlice";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var employee = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Alice", employee.FirstName);
    }

    [Fact]
    public void GivenPatternWithOutOfOrderOption_WhenInputHasDifferentOrder_ThenExtractsValuesCorrectly()
    {
        // Arrange
        const string pattern = """
                               ---
                               OutOfOrder: true
                               ---
                               First Name: {Student.FirstName}
                               Middle Name: {Student.MiddleName}
                               Last Name: {Student.LastName}
                               """;
        const string input = """
                             Last Name: Smith
                             First Name: Bob
                             Middle Name: Charles
                             """;

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var student = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Bob", student.FirstName);
        Assert.Equal("Charles", student.MiddleName);
        Assert.Equal("Smith", student.LastName);
    }

    [Fact]
    public void GivenParsedTemplate_WhenTokenizingSameInputTwice_ThenReturnsSameResults()
    {
        // Arrange
        const string pattern = @"First Name: {Student.FirstName}, Last Name: {Student.LastName}";
        const string input = @"First Name: Alice, Last Name: Smith";
        var template = new TemplateCompiler(new TokenizerOptions()).Compile(pattern).Template;

        // Act
        var one = _tokenizer.Tokenize<Student>(template, input).Value;
        var two = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("Alice", one.FirstName);
        Assert.Equal("Smith", one.LastName);
        Assert.Equal("Alice", two.FirstName);
        Assert.Equal("Smith", two.LastName);
    }

    [Fact]
    public void GivenPatternWithTrimTrailingWhitespaceOption_WhenTokenizingInputWithTrailingSpaces_ThenTrimsWhitespace()
    {
        // Arrange
        const string pattern = """
                               ---
                               # Trim Whitespace
                               TrimTrailingWhitespace: true
                               ---
                               First Name: {FirstName}
                               Last Name: {LastName}
                               ...
                               """;
        const string input = "First Name: John    ";

        // Create _tokenizer with TrimTrailingWhiteSpace = false
        // Should get overridden by embedded pattern declaration
        var options = new TokenizerOptions { TrimTrailingWhiteSpace = false };
        var testTokenizer = CreateTokenizer(options);

        // Act
        var template = testTokenizer.Compile(pattern).Template;
        var student = testTokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("John", student.FirstName);
    }

    [Fact]
    public void GivenPatternWithNumericValidator_WhenInputHasInvalidThenValidNumber_ThenUsesValidNumber()
    {
        // Arrange
        const string pattern = "Age: {Age:IsNumeric}";
        const string input = "Age: Ten, Age: 11";

        // Act
        var _tok = new Tokenizer();
        var template = _tok.Compile(pattern).Template;
        var person = _tok.Tokenize<Person>(template, input).Value;

        // Assert
        Assert.Equal(11, person.Age);
    }

    [Fact]
    public void GivenPatternWithSetToken_WhenTokenizing_ThenSetsDefaultValueForProperty()
    {
        // Arrange
        const string pattern = """
                               ---
                               # Trim Whitespace
                               set: LastName = Smith
                               ---
                               First Name: {FirstName}
                               ...
                               """;
        const string input = "First Name: John    ";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var student = _tokenizer.Tokenize<Student>(template, input).Value;

        // Assert
        Assert.Equal("John", student.FirstName);
        Assert.Equal("Smith", student.LastName);
    }

    [Fact]
    public void GivenPatternWithIgnoreMissingPropertiesOption_WhenPropertyDoesNotExist_ThenIgnoresMissingProperty()
    {
        // Arrange
        const string pattern = """
                               ---
                               IgnoreMissingProperties: true
                               ---
                               First Name: {FirstName}
                               Last Name: {Foo}
                               ...
                               """;
        const string input = "First Name: John\nLast Name: Smith";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Student>(template, input);
        var student = result.Value;

        // Assert
        Assert.Equal("John", student.FirstName);
        Assert.Equal("Smith", result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Foo", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public void GivenPatternWithMultipleOptionalDateTokens_WhenOneMatches_ThenReturnsSingleMatch()
    {
        // Arrange
        const string pattern = @"Date: { Date? : ToDateTime('dd MMM yyyy') }Date: { Date? : ToDateTime('yyyy-MM-dd') }";
        const string input = "Date: 2001-01-01";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);
        var date = result.First<DateTime>("Date");

        // Assert
        Assert.Equal(new DateTime(2001, 1, 1), date);
        Assert.Single(result.Matches);
    }
}
