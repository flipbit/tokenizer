using System.Text;
using Tokens.Exceptions;

namespace Tokens.Extensions;

/// <summary>
/// Extension methods for <see cref="TextReader"/>.
/// </summary>
internal static class TextReaderExtensions
{
    /// <summary>
    /// Asynchronously reads all content from the <paramref name="reader"/>, enforcing
    /// a maximum character length if <paramref name="maxLength"/> is greater than zero.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="maxLength">Maximum allowed length. Zero or negative disables the limit.</param>
    /// <param name="ct">A cancellation token to observe.</param>
    /// <returns>The full content of the reader as a string.</returns>
    /// <exception cref="TokenizerException">Thrown when the content exceeds <paramref name="maxLength"/>.</exception>
    public static async Task<string> ReadToEndBoundedAsync(this TextReader reader, int maxLength, CancellationToken ct)
    {
        var sb = new StringBuilder();
        var buffer = new char[4096];
        int read;
#if NET8_0_OR_GREATER
        while ((read = await reader.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false)) > 0)
#else
        while ((read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
#endif
        {
            ct.ThrowIfCancellationRequested();
            sb.Append(buffer, 0, read);
            if (maxLength > 0 && sb.Length > maxLength)
            {
                throw new TokenizerException(
                    $"Template length {sb.Length.ToInvariant("N0")} exceeds maximum allowed length of {maxLength.ToInvariant("N0")}. " +
                    "Increase TokenizerOptions.MaxTemplateLength to allow larger templates.");
            }
        }
        return sb.ToString();
    }
}
