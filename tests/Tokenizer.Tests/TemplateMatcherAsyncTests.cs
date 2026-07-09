using System.Text;
using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TemplateMatcherAsyncTests : TokenizerTestBase
{
    private sealed class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }

    public TemplateMatcherAsyncTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task GivenTextReader_WhenTokenizeAsync_ThenFindsBestMatch()
    {
        var matcher = new TemplateMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        using var reader = new StringReader("Name: Alice, Age: 30");

        var result = await matcher.TokenizeAsync(reader);

        Assert.NotNull(result.BestMatch);
        Assert.Equal("with-age", result.BestMatch.Template.Name);
    }

    [Fact]
    public async Task GivenTextReader_WhenTokenizeAsyncAndAssign_ThenPopulatesObject()
    {
        var matcher = new TemplateMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        using var reader = new StringReader("Name: Bob, Age: 25");

        var result = await matcher.TokenizeAsync(reader);

        Assert.NotNull(result.BestMatch);
        var person = result.BestMatch.Assign<Person>();
        Assert.Equal("Bob", person.Name);
        Assert.Equal(25, person.Age);
    }

    [Fact]
    public async Task GivenSeekableStream_WhenTokenizeAsync_ThenRewindsBetweenTemplates()
    {
        var matcher = new TemplateMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Charlie, Age: 35"));

        var result = await matcher.TokenizeAsync(stream, Encoding.UTF8);

        Assert.True(result.Results.Count >= 2);
        Assert.NotNull(result.BestMatch);
        Assert.Equal("with-age", result.BestMatch.Template.Name);
    }

    [Fact]
    public async Task GivenSeekableStream_WhenTokenizeAsyncCompletes_ThenStreamIsNotDisposed()
    {
        var matcher = new TemplateMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Dave"));

        await matcher.TokenizeAsync(stream, Encoding.UTF8);

        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task GivenNonSeekableStream_WhenAllowStreamBufferingFalse_ThenThrows()
    {
        var matcher = new TemplateMatcher(new TokenizerOptions { AllowStreamBuffering = false });
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        using var stream = new NonSeekableStream(Encoding.UTF8.GetBytes("Name: Eve"));

        await Assert.ThrowsAsync<TokenizerException>(
            () => matcher.TokenizeAsync(stream, Encoding.UTF8));
    }

    [Fact]
    public async Task GivenNonSeekableStream_WhenAllowStreamBufferingTrue_ThenBuffersAndMatches()
    {
        var matcher = new TemplateMatcher(new TokenizerOptions { AllowStreamBuffering = true });
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        using var stream = new NonSeekableStream(Encoding.UTF8.GetBytes("Name: Frank"));

        var result = await matcher.TokenizeAsync(stream, Encoding.UTF8);

        Assert.NotNull(result.BestMatch);
    }

    [Fact]
    public async Task GivenTextReader_WhenRegisterTemplateAsync_ThenTemplateIsRegistered()
    {
        var matcher = new TemplateMatcher();
        using var reader = new StringReader("Name: {Name}");

        await matcher.RegisterTemplateAsync(reader);

        Assert.Single(matcher.Templates);
    }

    [Fact]
    public async Task GivenTemplatesWithTags_WhenTokenizeAsyncWithTags_ThenFiltersCorrectly()
    {
        // Arrange
        var matcher = new TemplateMatcher();
        matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        matcher.Templates.Get("no-age")!.AddTag("no-age");
        matcher.Templates.Get("with-age")!.AddTag("with-age");

        using var reader = new StringReader("Name: Alice, Age: 30");

        // Act — filter to only "with-age" tag
        var result = await matcher.TokenizeAsync(reader, new[] { "with-age" });

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.Equal("with-age", result.BestMatch.Template.Name);
    }

    [Fact]
    public async Task GivenTemplatesWithTags_WhenTokenizeAsyncWithTagsAndAssign_ThenFiltersCorrectly()
    {
        // Arrange
        var matcher = new TemplateMatcher();
        matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        matcher.Templates.Get("no-age")!.AddTag("no-age");
        matcher.Templates.Get("with-age")!.AddTag("with-age");

        using var reader = new StringReader("Name: Alice, Age: 30");

        // Act
        var result = await matcher.TokenizeAsync(reader, new[] { "no-age" });

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.Equal("no-age", result.BestMatch.Template.Name);
        var person = result.BestMatch.Assign<Person>();
        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public async Task GivenStream_WhenRegisterTemplateAsync_ThenTemplateIsRegistered()
    {
        // Arrange
        var matcher = new TemplateMatcher();
        var bytes = Encoding.UTF8.GetBytes("Name: {Name}");
        using var stream = new MemoryStream(bytes);

        // Act
        await matcher.RegisterTemplateAsync(stream, Encoding.UTF8);

        // Assert
        Assert.Single(matcher.Templates);
    }

    [Fact]
    public async Task GivenStreamWithTags_WhenTokenizeAsync_ThenFiltersCorrectly()
    {
        // Arrange
        var matcher = new TemplateMatcher();
        matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        matcher.Templates.Get("no-age")!.AddTag("no-age");
        matcher.Templates.Get("with-age")!.AddTag("with-age");

        var bytes = Encoding.UTF8.GetBytes("Name: Alice, Age: 30");
        using var stream = new MemoryStream(bytes);

        // Act — filter to only "with-age" tag
        var result = await matcher.TokenizeAsync(stream, Encoding.UTF8, new[] { "with-age" });

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.Equal("with-age", result.BestMatch.Template.Name);
    }

    [Fact]
    public async Task GivenStreamWithTags_WhenTokenizeAsyncAndAssign_ThenFiltersCorrectly()
    {
        // Arrange
        var matcher = new TemplateMatcher();
        matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        matcher.Templates.Get("no-age")!.AddTag("no-age");
        matcher.Templates.Get("with-age")!.AddTag("with-age");

        var bytes = Encoding.UTF8.GetBytes("Name: Alice, Age: 30");
        using var stream = new MemoryStream(bytes);

        // Act — filter to only "no-age" tag
        var result = await matcher.TokenizeAsync(stream, Encoding.UTF8, new[] { "no-age" });

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.Equal("no-age", result.BestMatch.Template.Name);
        var person = result.BestMatch.Assign<Person>();
        Assert.Equal("Alice", person.Name);
    }

    [Fact]
    public async Task GivenUnicodeEncodedStream_WhenTokenizeAsync_ThenDecodesCorrectly()
    {
        // Arrange — UTF-16 encoded stream
        var matcher = new TemplateMatcher();
        matcher.RegisterTemplate("Name: {Name}", "name-only");
        var bytes = Encoding.Unicode.GetBytes("Name: Alice");
        using var stream = new MemoryStream(bytes);

        // Act
        var result = await matcher.TokenizeAsync(stream, Encoding.Unicode);

        // Assert
        Assert.NotNull(result.BestMatch);
        var nameMatch = result.BestMatch.Tokens.Matches.FirstOrDefault(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal));
        Assert.NotNull(nameMatch);
        Assert.Equal("Alice", nameMatch.Value);
    }

    /// <summary>
    /// A stream wrapper that does not support seeking — simulates a NetworkStream.
    /// </summary>
    private sealed class NonSeekableStream : Stream
    {
        private readonly MemoryStream _inner;

        public NonSeekableStream(byte[] data) { _inner = new MemoryStream(data); }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => _inner.ReadAsync(buffer, offset, count, ct);
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
    }
}
