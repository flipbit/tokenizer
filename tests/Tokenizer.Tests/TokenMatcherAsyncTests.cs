using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenMatcherAsyncTests : TokenizerTestBase
{
    private class Person
    {
        public string Name { get; set; } = null!;
        public int Age { get; set; }
    }

    public TokenMatcherAsyncTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task GivenTextReader_WhenMatchAsync_ThenFindsBestMatch()
    {
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        using var reader = new StringReader("Name: Alice, Age: 30");

        var result = await matcher.MatchAsync(reader);

        Assert.NotNull(result.BestMatch);
        Assert.Equal("with-age", result.BestMatch.Template.Name);
    }

    [Fact]
    public async Task GivenTextReader_WhenMatchAsyncGeneric_ThenPopulatesObject()
    {
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        using var reader = new StringReader("Name: Bob, Age: 25");

        var result = await matcher.MatchAsync<Person>(reader);

        Assert.NotNull(result.BestMatch);
        Assert.Equal("Bob", result.BestMatch.Value.Name);
        Assert.Equal(25, result.BestMatch.Value.Age);
    }

    [Fact]
    public async Task GivenSeekableStream_WhenMatchAsync_ThenRewindsBetweenTemplates()
    {
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Charlie, Age: 35"));

        var result = await matcher.MatchAsync(stream, Encoding.UTF8);

        Assert.True(result.Results.Count >= 2);
        Assert.NotNull(result.BestMatch);
        Assert.Equal("with-age", result.BestMatch.Template.Name);
    }

    [Fact]
    public async Task GivenSeekableStream_WhenMatchAsyncCompletes_ThenStreamIsNotDisposed()
    {
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("Name: Dave"));

        await matcher.MatchAsync(stream, Encoding.UTF8);

        Assert.True(stream.CanRead);
        stream.Dispose();
    }

    [Fact]
    public async Task GivenNonSeekableStream_WhenAllowStreamBufferingFalse_ThenThrows()
    {
        var matcher = new TokenMatcher(new TokenizerOptions { AllowStreamBuffering = false });
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        using var stream = new NonSeekableStream(Encoding.UTF8.GetBytes("Name: Eve"));

        await Assert.ThrowsAsync<TokenizerException>(
            () => matcher.MatchAsync(stream, Encoding.UTF8));
    }

    [Fact]
    public async Task GivenNonSeekableStream_WhenAllowStreamBufferingTrue_ThenBuffersAndMatches()
    {
        var matcher = new TokenMatcher(new TokenizerOptions { AllowStreamBuffering = true });
        matcher.RegisterTemplate("Name: {Person.Name}", "name-only");
        using var stream = new NonSeekableStream(Encoding.UTF8.GetBytes("Name: Frank"));

        var result = await matcher.MatchAsync(stream, Encoding.UTF8);

        Assert.NotNull(result.BestMatch);
    }

    [Fact]
    public async Task GivenTextReader_WhenRegisterTemplateAsync_ThenTemplateIsRegistered()
    {
        var matcher = new TokenMatcher();
        using var reader = new StringReader("Name: {Name}");

        await matcher.RegisterTemplateAsync(reader, "my-template");

        Assert.True(matcher.Templates.TryGet("my-template", out _));
    }

    [Fact]
    public async Task GivenTemplatesWithTags_WhenMatchAsyncWithTags_ThenFiltersCorrectly()
    {
        // Arrange
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        matcher.Templates.Get("no-age")!.AddTag("no-age");
        matcher.Templates.Get("with-age")!.AddTag("with-age");

        using var reader = new StringReader("Name: Alice, Age: 30");

        // Act — filter to only "with-age" tag
        var result = await matcher.MatchAsync(reader, new[] { "with-age" });

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.Equal("with-age", result.BestMatch.Template.Name);
    }

    [Fact]
    public async Task GivenTemplatesWithTags_WhenMatchAsyncGenericWithTags_ThenFiltersCorrectly()
    {
        // Arrange
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Person.Name: SubstringBefore(',')}", "no-age");
        matcher.RegisterTemplate("Name: {Person.Name}, Age: {Person.Age}", "with-age");
        matcher.Templates.Get("no-age")!.AddTag("no-age");
        matcher.Templates.Get("with-age")!.AddTag("with-age");

        using var reader = new StringReader("Name: Alice, Age: 30");

        // Act
        var result = await matcher.MatchAsync<Person>(reader, new[] { "no-age" });

        // Assert
        Assert.NotNull(result.BestMatch);
        Assert.Equal("no-age", result.BestMatch.Template.Name);
        Assert.Equal("Alice", result.BestMatch.Value.Name);
    }

    [Fact]
    public async Task GivenStream_WhenRegisterTemplateAsync_ThenTemplateIsRegistered()
    {
        // Arrange
        var matcher = new TokenMatcher();
        var bytes = Encoding.UTF8.GetBytes("Name: {Name}");
        using var stream = new MemoryStream(bytes);

        // Act
        await matcher.RegisterTemplateAsync(stream, Encoding.UTF8);

        // Assert
        Assert.Single(matcher.Templates.Names);
    }

    [Fact]
    public async Task GivenStream_WhenRegisterTemplateAsyncWithName_ThenTemplateHasName()
    {
        // Arrange
        var matcher = new TokenMatcher();
        var bytes = Encoding.UTF8.GetBytes("Name: {Name}");
        using var stream = new MemoryStream(bytes);

        // Act
        await matcher.RegisterTemplateAsync(stream, Encoding.UTF8, "stream-template");

        // Assert
        Assert.True(matcher.Templates.TryGet("stream-template", out _));
    }

    [Fact]
    public async Task GivenUnicodeEncodedStream_WhenMatchAsync_ThenDecodesCorrectly()
    {
        // Arrange — UTF-16 encoded stream
        var matcher = new TokenMatcher();
        matcher.RegisterTemplate("Name: {Name}", "name-only");
        var bytes = Encoding.Unicode.GetBytes("Name: Alice");
        using var stream = new MemoryStream(bytes);

        // Act
        var result = await matcher.MatchAsync(stream, Encoding.Unicode);

        // Assert
        Assert.NotNull(result.BestMatch);
        var nameMatch = result.BestMatch.Tokens.Matches.FirstOrDefault(m => m.Token.Name == "Name");
        Assert.NotNull(nameMatch);
        Assert.Equal("Alice", nameMatch.Value);
    }

    /// <summary>
    /// A stream wrapper that does not support seeking — simulates a NetworkStream.
    /// </summary>
    private class NonSeekableStream : Stream
    {
        private readonly MemoryStream inner;

        public NonSeekableStream(byte[] data) { inner = new MemoryStream(data); }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => inner.ReadAsync(buffer, offset, count, ct);
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
    }
}
