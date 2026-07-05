using System.Collections.Concurrent;
#if NET8_0_OR_GREATER
using System.IO.Hashing;
#endif

namespace Tokens.Compilation;

/// <summary>
/// Thread-safe compilation cache with LRU eviction.
/// Keys are non-cryptographic hashes of template pattern strings.
/// </summary>
internal sealed class TemplateCache
{
    private readonly ConcurrentDictionary<ulong, CacheEntry> cache = new();
    private readonly int maxSize;
    private long accessCounter;
    private readonly object evictionLock = new();

    public TemplateCache(int maxSize)
    {
        this.maxSize = maxSize;
    }

    public int Count => cache.Count;

    // Intentional race-to-add: two threads may compile the same pattern concurrently.
    // Duplicate work is harmless (both produce correct results). Lazy<T> wrapper would
    // add overhead to every cache hit to prevent a rare, benign edge case.
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
                var oldestKey = 0UL;
                var oldestTime = long.MaxValue;
                var found = false;

                foreach (var kvp in cache)
                {
                    var accessed = Interlocked.Read(ref kvp.Value.LastAccessed);
                    if (accessed < oldestTime)
                    {
                        oldestTime = accessed;
                        oldestKey = kvp.Key;
                        found = true;
                    }
                }

                if (found)
                {
                    cache.TryRemove(oldestKey, out _);
                }
            }
        }
    }

    private static ulong ComputeHash(string input)
    {
#if NET8_0_OR_GREATER
        return XxHash64.HashToUInt64(System.Runtime.InteropServices.MemoryMarshal.AsBytes(input.AsSpan()));
#else
        // FNV-1a 64-bit over chars
        const ulong fnvOffset = 14695981039346656037;
        const ulong fnvPrime = 1099511628211;

        var hash = fnvOffset;

        foreach (var c in input)
        {
            hash ^= c;
            hash *= fnvPrime;
        }

        return hash;
#endif
    }

    private sealed class CacheEntry
    {
        public Template Template { get; init; } = null!;
        public long LastAccessed;
    }
}
