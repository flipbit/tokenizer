using System.Collections.Concurrent;
using Tokens.Compilation;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TemplateCacheTests : TokenizerTestBase
{
    public TemplateCacheTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenEmptyCache_WhenGettingTemplate_ThenCompileFuncIsCalled()
    {
        // Arrange
        var cache = new TemplateCache(10);
        var compiled = false;

        // Act
        var template = cache.GetOrAdd("pattern", _ =>
        {
            compiled = true;
            return new Template("test");
        });

        // Assert
        Assert.True(compiled);
        Assert.Equal("test", template.Name);
    }

    [Fact]
    public void GivenCachedTemplate_WhenGettingSamePattern_ThenCompileFuncIsNotCalled()
    {
        // Arrange
        var cache = new TemplateCache(10);
        cache.GetOrAdd("pattern", _ => new Template("first"));

        // Act
        var compiled = false;
        var template = cache.GetOrAdd("pattern", _ =>
        {
            compiled = true;
            return new Template("second");
        });

        // Assert
        Assert.False(compiled);
        Assert.Equal("first", template.Name);
    }

    [Fact]
    public void GivenDifferentPatterns_WhenGetting_ThenEachCompiledSeparately()
    {
        // Arrange
        var cache = new TemplateCache(10);

        // Act
        var t1 = cache.GetOrAdd("pattern1", _ => new Template("first"));
        var t2 = cache.GetOrAdd("pattern2", _ => new Template("second"));

        // Assert
        Assert.Equal("first", t1.Name);
        Assert.Equal("second", t2.Name);
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void GivenFullCache_WhenAddingNew_ThenLeastRecentlyUsedIsEvicted()
    {
        // Arrange
        var cache = new TemplateCache(2);
        cache.GetOrAdd("oldest", _ => new Template("oldest"));
        cache.GetOrAdd("newer", _ => new Template("newer"));

        // Touch "newer" to make "oldest" the LRU
        cache.GetOrAdd("newer", _ => new Template("should-not-compile"));

        // Act — this should evict "oldest"
        cache.GetOrAdd("newest", _ => new Template("newest"));

        // Assert
        Assert.Equal(2, cache.Count);

        // "oldest" was evicted, so recompiling should call the func
        var recompiled = false;
        cache.GetOrAdd("oldest", _ =>
        {
            recompiled = true;
            return new Template("recompiled");
        });
        Assert.True(recompiled);
    }

    [Fact]
    public void GivenCacheWithEntries_WhenClearing_ThenCacheIsEmpty()
    {
        // Arrange
        var cache = new TemplateCache(10);
        cache.GetOrAdd("a", _ => new Template("a"));
        cache.GetOrAdd("b", _ => new Template("b"));

        // Act
        cache.Clear();

        // Assert
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void GivenZeroMaxSize_WhenGetting_ThenNeverCaches()
    {
        // Arrange
        var cache = new TemplateCache(0);
        cache.GetOrAdd("pattern", _ => new Template("first"));

        // Act
        var compiled = false;
        cache.GetOrAdd("pattern", _ =>
        {
            compiled = true;
            return new Template("second");
        });

        // Assert
        Assert.True(compiled);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void GivenCache_WhenAccessedFromMultipleThreads_ThenNoExceptions()
    {
        // Arrange
        var cache = new TemplateCache(50);
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, 100, i =>
        {
            try
            {
                var pattern = $"pattern {i % 20}";
                cache.GetOrAdd(pattern, p => new Template($"template-{p}"));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.Empty(exceptions);
    }
}
