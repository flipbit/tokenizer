#if NET8_0_OR_GREATER
using System.IO.Hashing;
#endif

namespace Tokens.Extensions;

/// <summary>
/// Provides a non-cryptographic hash function for strings.
/// </summary>
internal static class StringHashExtensions
{
    /// <summary>
    /// Computes a non-cryptographic 64-bit hash of the string.
    /// Uses XxHash64 on .NET 8+ and FNV-1a on .NET Standard 2.0.
    /// </summary>
    public static ulong ComputeHash(this string input)
    {
#if NET8_0_OR_GREATER
        return XxHash64.HashToUInt64(System.Runtime.InteropServices.MemoryMarshal.AsBytes(input.AsSpan()));
#else
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
}
