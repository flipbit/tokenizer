using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Tokens.Compilation;

/// <summary>
/// Thread-safe compilation cache with LRU eviction.
/// Keys are SHA256 hashes of template pattern strings.
/// </summary>
internal sealed class TemplateCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> cache = new();
    private readonly int maxSize;
    private long accessCounter;
    private readonly object evictionLock = new();

    public TemplateCache(int maxSize)
    {
        this.maxSize = maxSize;
    }

    public int Count => cache.Count;

    public Template GetOrAdd(string pattern, Func<string, Template> compile)
    {
        if (maxSize <= 0)
        {
            return compile(pattern);
        }

        var key = ComputeHash(pattern);

        if (cache.TryGetValue(key, out var existing))
        {
            Interlocked.Exchange(ref existing.LastAccessed, Interlocked.Increment(ref accessCounter));
            return existing.Template;
        }

        var template = compile(pattern);
        var entry = new CacheEntry { Template = template, LastAccessed = Interlocked.Increment(ref accessCounter) };

        if (cache.TryAdd(key, entry))
        {
            EvictIfOverCapacity();
        }

        return template;
    }

    public void Clear()
    {
        cache.Clear();
    }

    private void EvictIfOverCapacity()
    {
        lock (evictionLock)
        {
            while (cache.Count > maxSize)
            {
                var oldest = default(KeyValuePair<string, CacheEntry>);
                var oldestTime = long.MaxValue;

                foreach (var kvp in cache)
                {
                    var accessed = Interlocked.Read(ref kvp.Value.LastAccessed);
                    if (accessed < oldestTime)
                    {
                        oldestTime = accessed;
                        oldest = kvp;
                    }
                }

                if (oldest.Key != null)
                {
                    cache.TryRemove(oldest.Key, out _);
                }
            }
        }
    }

    private static string ComputeHash(string input)
    {
        // SHA256.HashData and Convert.ToHexString require .NET 6+; other files use NET8_0_OR_GREATER for different APIs (e.g. SearchValues)
#if NET6_0_OR_GREATER
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
#else
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
#endif
    }

    private sealed class CacheEntry
    {
        public Template Template { get; init; } = null!;
        public long LastAccessed;
    }
}
