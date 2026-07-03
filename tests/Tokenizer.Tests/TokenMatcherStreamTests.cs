using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenMatcherStreamTests : TokenizerTestBase
{
    private readonly ITokenMatcher matcher;

    private class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }

    public TokenMatcherStreamTests(ITestOutputHelper output) : base(output)
    {
        matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
    }

    [Fact]
    public void GivenTextReaderInput_WhenMatching_ThenFindsBestMatch()
    {
        // Arrange
        using var reader = new StringReader("Name: Alice, Age: 30");

        // Act
        var result = matcher.Match(reader);

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.Equal("with-age", result.BestMatch.Template.Name);
    }

    [Fact]
    public void GivenTextReaderInput_WhenMatchingGeneric_ThenPopulatesObject()
    {
        // Arrange
        using var reader = new StringReader("Name: Bob, Age: 25");

        // Act
        var result = matcher.Match<Person>(reader);

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.Equal("Bob", result.BestMatch.Value.Name);
        Assert.Equal(25, result.BestMatch.Value.Age);
    }

    [Fact]
    public void GivenTextReaderInputWithTags_WhenMatching_ThenFiltersCorrectly()
    {
        // Arrange
        var taggedMatcher = new TokenMatcher();
        taggedMatcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        taggedMatcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        taggedMatcher.Templates.Get("name-only")!.AddTag("personal");

        using var reader = new StringReader("Name: Carol");

        // Act
        var result = taggedMatcher.Match(reader, new[] { "personal" });

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.True(result.BestMatch.Success);
        Assert.Equal("name-only", result.BestMatch.Template.Name);
    }

    [Fact]
    public void GivenStreamInput_WhenMatching_ThenFindsBestMatch()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Dave, Age: 40"));

        // Act
        var result = matcher.Match(stream, Encoding.UTF8);

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.Equal("with-age", result.BestMatch.Template.Name);
    }

    [Fact]
    public void GivenStreamInput_WhenMatchingCompletes_ThenStreamIsNotDisposed()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Eve"));

        // Act
        matcher.Match(stream, Encoding.UTF8);

        // Assert - stream is still usable (not disposed)
        stream.Position = 0;
        Assert.True(stream.CanRead);

        // Cleanup
        stream.Dispose();
    }
}
