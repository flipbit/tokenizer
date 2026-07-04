using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tokens.Enumerators;

/// <summary>
/// A forward-only, character-level enumerator over a <see cref="TextReader"/> that tracks the current
/// <see cref="FileLocation"/> (line and column) as it advances. All line endings are normalised to <c>\n</c>.
/// </summary>
public class TokenEnumerator
{
    private const int DefaultBufferSize = 1024;
    private const int RefillWatermark = 256;

    private TextReader reader;
    private readonly string? originalString;

    private char[] buffer;
    private char[] stagingBuffer;
    private int readPos;
    private int writePos;
    private int bufferedCount;

    private bool readerExhausted;
    private bool resetNextLine;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenEnumerator"/> over the specified <see cref="TextReader"/>.
    /// All line endings (<c>\r\n</c>, lone <c>\r</c>) are normalised to <c>\n</c>.
    /// </summary>
    /// <param name="reader">The text reader to enumerate.</param>
    public TokenEnumerator(TextReader reader) : this(reader, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TokenEnumerator"/> over the specified string.
    /// All line endings (<c>\r\n</c>, lone <c>\r</c>) are normalised to <c>\n</c>.
    /// </summary>
    /// <param name="pattern">The string to enumerate.</param>
    public TokenEnumerator(string pattern) : this(new StringReader(pattern ?? string.Empty), pattern ?? string.Empty)
    {
    }

    private TokenEnumerator(TextReader reader, string? originalString)
    {
        this.reader = reader;
        this.originalString = originalString;
        buffer = new char[DefaultBufferSize];
        stagingBuffer = new char[DefaultBufferSize];
        readPos = 0;
        writePos = 0;
        bufferedCount = 0;
        readerExhausted = false;
        Location = new FileLocation();
        FillBuffer();
    }

    /// <summary>
    /// Gets a value indicating whether all characters have been consumed.
    /// When the buffer is empty and the reader has not been marked exhausted,
    /// this property attempts a fill to discover exhaustion.
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            if (bufferedCount == 0 && !readerExhausted)
            {
                FillBuffer();
            }

            return bufferedCount == 0 && readerExhausted;
        }
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="Reset"/> is supported.
    /// Only string-backed enumerators support reset.
    /// </summary>
    public bool CanReset => originalString != null;

    /// <summary>
    /// Gets the current position in the source as a line/column <see cref="FileLocation"/>.
    /// </summary>
    public FileLocation Location { get; }

    /// <summary>
    /// Gets the total number of characters consumed via <see cref="Next"/>.
    /// </summary>
    public long CharactersConsumed { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the buffer is below the refill watermark
    /// and the reader has more data available.
    /// </summary>
    public bool NeedsRefill => bufferedCount < RefillWatermark && !readerExhausted;

    /// <summary>
    /// Reads a bulk chunk from the underlying reader into the ring buffer (synchronous path).
    /// </summary>
    public void FillBuffer()
    {
        if (readerExhausted) return;

        // Read into the reusable staging buffer, then copy with CRLF normalization
        var available = buffer.Length - bufferedCount;
        if (available <= 0) return;

        var staging = stagingBuffer;
        var read = reader.Read(staging, 0, available);
        if (read == 0)
        {
            readerExhausted = true;
            return;
        }

        CopyToRingBuffer(staging, read);
    }

    /// <summary>
    /// Reads a bulk chunk from the underlying reader into the ring buffer (asynchronous path).
    /// </summary>
    /// <param name="ct">A cancellation token to observe.</param>
    public async ValueTask FillBufferAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (readerExhausted) return;

        var available = buffer.Length - bufferedCount;
        if (available <= 0) return;

        var staging = stagingBuffer;
#if NET8_0_OR_GREATER
        var read = await reader.ReadAsync(staging.AsMemory(0, available), ct).ConfigureAwait(false);
#else
        var read = await reader.ReadAsync(staging, 0, available).ConfigureAwait(false);
#endif
        if (read == 0)
        {
            readerExhausted = true;
            return;
        }

        CopyToRingBuffer(staging, read);
    }

    /// <summary>
    /// Advances the enumerator by one character and returns it, updating <see cref="Location"/>.
    /// Returns <c>'\0'</c> if the enumerator is already at the end.
    /// </summary>
    /// <returns>The next character, or <c>'\0'</c> if <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    public char Next()
    {
        var next = DequeueChar();
        if (next == '\0') return '\0';

        CharactersConsumed++;

        if (resetNextLine)
        {
            Location.NewLine();
            resetNextLine = false;
        }
        else
        {
            Location.Increment(next);
        }

        if (next == '\n')
        {
            resetNextLine = true;
        }

        return next;
    }

    /// <summary>
    /// Returns the next character without advancing the enumerator.
    /// Returns <c>'\0'</c> if the enumerator is already at the end.
    /// </summary>
    /// <returns>The next character, or <c>'\0'</c> if <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    public char Peek()
    {
        if (bufferedCount == 0)
        {
            if (readerExhausted) return '\0';
            FillBuffer();
            if (bufferedCount == 0) return '\0';
        }

        return buffer[readPos];
    }

    /// <summary>
    /// Returns <see langword="true"/> if the characters starting at the current position match <paramref name="value"/>
    /// exactly, without advancing the enumerator.
    /// </summary>
    /// <param name="value">The string to compare against the current position.</param>
    /// <returns><see langword="true"/> if the upcoming characters match <paramref name="value"/>; otherwise <see langword="false"/>.</returns>
    public bool TryMatch(string value)
    {
        if (string.IsNullOrEmpty(value)) return true;

        // Fast path: check first character before buffering the full length
        if (value[0] != '\0' && Peek() != value[0]) return false;

        EnsureBuffered(value.Length);

        if (bufferedCount < value.Length) return false;

        var pos = readPos;
        for (var i = 0; i < value.Length; i++)
        {
            if (buffer[pos] != value[i]) return false;
            pos++;
            if (pos >= buffer.Length) pos = 0;
        }

        return true;
    }

    /// <summary>
    /// Advances the enumerator by the specified number of characters, consuming each one.
    /// </summary>
    /// <param name="count">The number of characters to advance.</param>
    public void Advance(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Next();
        }
    }

    /// <summary>
    /// Checks which of the given tokens have a preamble that matches the text at the current position,
    /// populating <paramref name="matches"/> with every token whose preamble is found.
    /// When matching an out-of-order template, tokens without a name are skipped.
    /// </summary>
    /// <param name="tokens">The tokens whose preambles should be tested.</param>
    /// <param name="outOfOrderTokens">
    /// When <see langword="true"/>, tokens without a name are excluded from consideration
    /// because they cannot carry a value in an out-of-order match.
    /// </param>
    /// <param name="matches">A list that is cleared and then populated with every token whose preamble matches.</param>
    /// <returns><see langword="true"/> if at least one token's preamble matched; otherwise <see langword="false"/>.</returns>
    public bool TryMatch(IEnumerable<Token> tokens, bool outOfOrderTokens, IList<Token> matches)
    {
        matches.Clear();

        foreach (var token in tokens)
        {
            // Special case: if matching out of order template,
            // don't match any tokens without a value
            if (outOfOrderTokens && string.IsNullOrWhiteSpace(token.Name))
            {
                continue;
            }

            if (TryMatch(token.Preamble))
            {
                matches.Add(token);
            }

            if (token.IsOptional == false) break;
        }

        return matches.Count > 0;
    }

    /// <summary>
    /// Resets the enumerator to the beginning and clears the tracked <see cref="Location"/>.
    /// Only supported for string-backed enumerators.
    /// </summary>
    public void Reset()
    {
        if (originalString == null)
        {
            throw new System.NotSupportedException(
                "Reset is not supported on TextReader-based enumerators. " +
                "Use a hint strategy that does not require enumerator reset.");
        }

        reader = new StringReader(originalString);
        readPos = 0;
        writePos = 0;
        bufferedCount = 0;
        readerExhausted = false;
        resetNextLine = false;
        Location.Reset();
        CharactersConsumed = 0;
        FillBuffer();
    }

    /// <summary>
    /// Reads one character from the ring buffer, transparently refilling from the reader if needed.
    /// Returns <c>'\0'</c> if no more characters are available.
    /// </summary>
    private char DequeueChar()
    {
        if (bufferedCount == 0)
        {
            if (readerExhausted) return '\0';
            FillBuffer();
            if (bufferedCount == 0) return '\0';
        }

        var c = buffer[readPos];
        readPos++;
        if (readPos >= buffer.Length) readPos = 0;
        bufferedCount--;

        return c;
    }

    /// <summary>
    /// Ensures at least <paramref name="count"/> characters are buffered, growing the buffer if necessary.
    /// </summary>
    private void EnsureBuffered(int count)
    {
        while (bufferedCount < count && !readerExhausted)
        {
            if (bufferedCount >= buffer.Length)
            {
                GrowBuffer();
            }
            FillBuffer();
        }
    }

    /// <summary>
    /// Doubles the ring buffer capacity, linearizing existing data into the new buffer.
    /// </summary>
    private void GrowBuffer()
    {
        var newSize = buffer.Length * 2;
        var newBuffer = new char[newSize];

        // Linearize existing data into the new buffer
        if (bufferedCount > 0)
        {
            if (readPos + bufferedCount <= buffer.Length)
            {
                Array.Copy(buffer, readPos, newBuffer, 0, bufferedCount);
            }
            else
            {
                var firstChunk = buffer.Length - readPos;
                Array.Copy(buffer, readPos, newBuffer, 0, firstChunk);
                Array.Copy(buffer, 0, newBuffer, firstChunk, bufferedCount - firstChunk);
            }
        }

        buffer = newBuffer;
        readPos = 0;
        writePos = bufferedCount;

        if (newSize > stagingBuffer.Length)
        {
            stagingBuffer = new char[newSize];
        }
    }

    /// <summary>
    /// Copies characters from a staging buffer into the ring buffer,
    /// normalizing all line endings (<c>\r\n</c>, lone <c>\r</c>) to <c>\n</c>.
    /// </summary>
    private void CopyToRingBuffer(char[] staging, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var c = staging[i];

            if (c == '\r')
            {
                // Check if next char is \n — if so, skip the \r (the \n will be written next iteration)
                if (i + 1 < count && staging[i + 1] == '\n')
                {
                    continue; // skip \r, the \n follows
                }

                // Lone \r or \r at end of staging — need to check reader for \n
                if (i + 1 >= count)
                {
                    // \r is at the end of our staging read — peek at reader to check for \n
                    var peek = reader.Peek();
                    if (peek == '\n')
                    {
                        // \r\n split across reads — skip the \r, write \n, consume the \n from reader
                        reader.Read();
                    }
                }

                // Write \n instead of lone \r
                c = '\n';
            }

            if (bufferedCount >= buffer.Length)
            {
                GrowBuffer();
            }

            buffer[writePos] = c;
            writePos++;
            if (writePos >= buffer.Length) writePos = 0;
            bufferedCount++;
        }
    }
}
