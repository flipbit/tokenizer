using Tokens.Transformers;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenMatcherTests : TokenizerTestBase
{
    private readonly ITokenMatcher _matcher;

    private class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }

    public TokenMatcherTests(ITestOutputHelper output) : base(output)
    {
        _matcher = new TokenMatcher();
    }

    [Fact]
    public void TestParseOnePattern()
    {
        _matcher.RegisterTemplate("Name: {Person.Name}", "Person");

        var result = _matcher.Match<Person>("Name: Alice");

        var person = result.BestMatch!.Value;

        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public void TestParseTwoPatterns()
    {
        _matcher.RegisterTemplate("Name: {Person.Name}", "no-age");
        _matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

        var result = _matcher.Match<Person>("Name: Alice, Age: 30");

        var match = result.BestMatch!;

        Assert.Equal("Alice", match.Value.Name);
        Assert.Equal(30, match.Value.Age);
        Assert.Equal("with-age", match.Template.Name);
    }

    [Fact]
    public void TestMatchWithHint()
    {
        var tokenizer = CreateTokenizer();

        var template1 = tokenizer.Compile("Name: {Person.Name: SubstringBefore(',') }").Template;
        template1.Name = "no-age";
        var template2 = tokenizer.Compile("Name: {Person.Name}, Age: {Person.Age}").Template;
        template2.Name = "with-age";
        template1.AddHint(new Hint(Text: "Name"));

        _matcher.RegisterTemplate(template1);
        _matcher.RegisterTemplate(template2);

        var result = _matcher.Match<Person>("Name: Alice, Age: 30");

        var match = result.BestMatch!;

        Assert.Equal("Alice", match.Value.Name);
        Assert.Equal(0, match.Value.Age);
        Assert.Equal("no-age", match.Template.Name);
    }

    [Fact]
    public void TestMatchWithMultipleHints()
    {
        var tokenizer = CreateTokenizer();

        var template1 = tokenizer.Compile("Name: {Person.Name: SubstringBefore(',') }").Template;
        template1.Name = "no-age";
        var template2 = tokenizer.Compile("Name: {Person.Name}, Age: {Person.Age}").Template;
        template2.Name = "with-age";
        template1.AddHint(new Hint(Text: "Name"));
        template2.AddHint(new Hint(Text: "Name"));
        template2.AddHint(new Hint(Text: "Age"));

        _matcher.RegisterTemplate(template1);
        _matcher.RegisterTemplate(template2);

        var result = _matcher.Match<Person>("Name: Alice, Age: 30");

        var match = result.BestMatch!;

        Assert.Equal("Alice", match.Value.Name);
        Assert.Equal(30, match.Value.Age);
        Assert.Equal("with-age", match.Template.Name);
    }

    [Fact]
    public void TestParseTwoPatternsContinuesOnError()
    {
        var options = new TokenizerOptions().WithTransformer<BlowsUpTransformer>();
        var matcherWithTransformer = new TokenMatcher(options);

        matcherWithTransformer.RegisterTemplate("Name: {Person.Name:BlowsUp}", "no-age");
        matcherWithTransformer.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

        var result = matcherWithTransformer.Match<Person>("Name: Alice, Age: 30");

        var match = result.BestMatch!;

        Assert.Equal("Alice", match.Value.Name);
        Assert.Equal(30, match.Value.Age);
        Assert.Equal("with-age", match.Template.Name);
    }

    [Fact]
    public void TestParseTwoPatternsNeedsAllRequiredTokens()
    {
        _matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
        _matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}, Location: {Location!}", "with-age");

        var result = _matcher.Match<Person>("Name: Alice, Age: 30");

        Assert.True(result.Success);

        var match = result.BestMatch!;

        Assert.Equal("Alice", match.Value.Name);
        Assert.Equal(0, match.Value.Age);
        Assert.Equal("no-age", match.Template.Name);
    }

    [Fact]
    public void TestParseTwoPatternsWithTags()
    {
        _matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
        _matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

        _matcher.Templates.Get("no-age")!.AddTag("no-age");

        var result = _matcher.Match<Person>("Name: Alice, Age: 30", ["no-age"]);

        Assert.True(result.Success);

        var match = result.BestMatch!;

        Assert.Equal("Alice", match.Value.Name);
        Assert.Equal(0, match.Value.Age);
        Assert.Equal("no-age", match.Template.Name);
    }

    [Fact]
    public void TestParseTwoPatternsWithNoMatchingTags()
    {
        _matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
        _matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

        _matcher.Templates.Get("no-age")!.AddTag("no-age");
        _matcher.Templates.Get("with-age")!.AddTag("with-age");

        var result = _matcher.Match<Person>("Name: Alice, Age: 30", ["Foo"]);

        Assert.False(result.Success);
        Assert.Null(result.BestMatch);
    }
    [Fact]
    public void TestParseTwoPatternsWithNoTagInput()
    {
        _matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
        _matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

        _matcher.Templates.Get("no-age")!.AddTag("no-age");
        _matcher.Templates.Get("with-age")!.AddTag("with-age");

        var result = _matcher.Match<Person>("Name: Alice, Age: 30");

        var match = result.BestMatch!;

        Assert.True(result.Success);
        Assert.Equal("Alice", match.Value.Name);
        Assert.Equal(30, match.Value.Age);
        Assert.Equal("with-age", match.Template.Name);
    }

    [Fact]
    public void TestParseTwoPatternsWithTagsSelectsBestMatch()
    {
        _matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
        _matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");

        _matcher.Templates.Get("no-age")!.AddTag("no-age");
        _matcher.Templates.Get("no-age")!.AddTag("person");
        _matcher.Templates.Get("with-age")!.AddTag("with-age");
        _matcher.Templates.Get("with-age")!.AddTag("person");

        var result = _matcher.Match<Person>("Name: Alice, Age: 30", ["person"]);

        Assert.True(result.Success);

        var match = result.BestMatch!;

        Assert.Equal("Alice", match.Value.Name);
        Assert.Equal(30, match.Value.Age);
        Assert.Equal("with-age", match.Template.Name);
    }

    [Fact]
    public void TestParseTwoPatternsWithTagsSelectsBestMatchWithNoTags()
    {
        _matcher.RegisterTemplate("Name: { Name $ }", "with-name");
        _matcher.RegisterTemplate("Name: { Name $ }Age: { Age $ }", "with-age");
        _matcher.RegisterTemplate("Name: { Name $ }Age: { Age $ }Location { Location $ }", "with-location");

        var result = _matcher.Match("Name: Alice\nAge: 30");

        Assert.True(result.Success);

        var match = result.BestMatch!;

        Assert.Equal("Alice", match.First("Name"));
        Assert.Equal("30", match.First("Age"));
        Assert.Equal("with-age", match.Template.Name);
    }

    [Fact]
    public void TestDocumentationTags1()
    {
        var template1 = "---\nname: template1\ntag: standard\noutOfOrder: true\nterminateOnNewLine: true\n---\nName: {Name}\nAge: {Age}\n";

        var template2 = "---\nname: template2\ntag: extended\noutOfOrder: true\nterminateOnNewLine: true\n---\nName: {Name}\nAge: {Age}\nAddress: {Address}\n";

        _matcher.RegisterTemplate(template1);
        _matcher.RegisterTemplate(template2);

        var input = "Name: Alice\nAge: 30\nAddress: London\n";


        var result = _matcher.Match(input, ["standard"]);

        var match = result.BestMatch!;

        Assert.Equal("template1", match.Template.Name);
        Assert.Equal("Alice", match.First("Name"));
        Assert.Equal("30", match.First("Age"));
    }

    [Fact]
    public void GivenTemplateWithFrontMatterSet_WhenInputMatchesNoTokens_ThenResultIsNotSuccessful()
    {
        // Arrange
        var template = "---\nname: found-template\nset: Status = Found\n---\nName: {Name}\nAge: {Age}\n";

        _matcher.RegisterTemplate(template);

        // Act
        var result = _matcher.Match("This input matches nothing in the template");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.BestMatch);
    }

    [Fact]
    public void GivenFrontMatterOnlyTemplate_WhenHintMatches_ThenResultIsSuccessful()
    {
        // Arrange - template with set: and hint but no extractable tokens
        var template = "---\nname: not-found-template\nset: Status = NotFound\nhint: not found\n---\nnot found\n";

        _matcher.RegisterTemplate(template);

        // Act
        var result = _matcher.Match("not found...");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.BestMatch);
    }
}
