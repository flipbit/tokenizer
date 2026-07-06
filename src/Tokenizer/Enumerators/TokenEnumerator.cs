namespace Tokens.Enumerators;

/// <summary>
/// A forward-only, character-level enumerator over a <see cref="TextReader"/> that tracks the current
/// <see cref="FileLocation"/> (line and column) as it advances. All line endings are normalised to <c>\n</c>.
/// </summary>
public class TokenEnumerator
{
    private const int DefaultBufferSize = 1024;
    private const int RefillWatermark = 256;

    private TextReader _reader;
    private readonly string? _originalString;

    private char[] _buffer;
    private char[] _stagingBuffer;
    private int _readPos;
    private int _writePos;
    private int _bufferedCount;

    private bool _readerExhausted;
    private bool _resetNextLine;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenEnumerator"/> over the specified <see cref="TextReader"/>.
    /// All line endings (<c>\r\n</c>, lone <c>\r</c>) are normalised to <c>\n</c>.
    /// </summary>
    /// <param name="reader">The text reader to enumerate.</param>
    public TokenEnumerator(TextReader reader) : this(reader, originalString: null)
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
        _reader = reader;
        _originalString = originalString;
        _buffer = new char[DefaultBufferSize];
        _stagingBuffer = new char[DefaultBufferSize];
        _readPos = 0;
        _writePos = 0;
        _bufferedCount = 0;
        _readerExhausted = false;
        Location = new FileLocation();
        FillBuffer();
    }

    /// <summary>
    /// Gets a value indicating whether all characters have been consumed.
    /// When the _buffer is empty and the reader has not been marked exhausted,
    /// this property attempts a fill to discover exhaustion.
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            if (_bufferedCount == 0 && !_readerExhausted)
            {
                FillBuffer();
            }

            return _bufferedCount == 0 && _readerExhausted;
        }
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="Reset"/> is supported.
    /// Only string-backed enumerators support reset.
    /// </summary>
    public bool CanReset => _originalString != null;

    /// <summary>
    /// Gets the current position in the source as a line/column <see cref="FileLocation"/>.
    /// </summary>
    public FileLocation Location { get; }

    /// <summary>
    /// Gets the total number of characters consumed via <see cref="Next"/>.
    /// </summary>
    public long CharactersConsumed { get; private set; }

    /// <summary>
    /// Gets the total number of characters seen so far (consumed + still buffered).
    /// </summary>
    public long TotalCharactersSeen => CharactersConsumed + _bufferedCount;

    /// <summary>
    /// Gets a value indicating whether the _buffer is below the refill watermark
    /// and the reader has more data available.
    /// </summary>
    public bool NeedsRefill => _bufferedCount < RefillWatermark && !_readerExhausted;

    /// <summary>
    /// Reads a bulk chunk from the underlying reader into the ring _buffer (synchronous path).
    /// </summary>
    public void FillBuffer()
    {
        if (_readerExhausted) return;

        // Read into the reusable staging _buffer, then copy with CRLF normalization
        var available = _buffer.Length - _bufferedCount;
        if (available <= 0) return;

        var staging = _stagingBuffer;
        var read = _reader.Read(staging, 0, available);
        if (read == 0)
        {
            _readerExhausted = true;
            return;
        }

        CopyToRingBuffer(staging, read);
    }

    /// <summary>
    /// Reads a bulk chunk from the underlying reader into the ring _buffer (asynchronous path).
    /// </summary>
    /// <param name="ct">A cancellation token to observe.</param>
    public async ValueTask FillBufferAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (_readerExhausted) return;

        var available = _buffer.Length - _bufferedCount;
        if (available <= 0) return;

        var staging = _stagingBuffer;
#if NET8_0_OR_GREATER
        var read = await _reader.ReadAsync(staging.AsMemory(0, available), ct).ConfigureAwait(false);
#else
        var read = await _reader.ReadAsync(staging, 0, available).ConfigureAwait(false);
#endif
        if (read == 0)
        {
            _readerExhausted = true;
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

        if (_resetNextLine)
        {
            Location.NewLine();
            _resetNextLine = false;
        }
        else
        {
            Location.Increment(next);
        }

        if (next == '\n')
        {
            _resetNextLine = true;
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
        if (_bufferedCount == 0)
        {
            if (_readerExhausted) return '\0';
            FillBuffer();
            if (_bufferedCount == 0) return '\0';
        }

        return _buffer[_readPos];
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

        if (_bufferedCount < value.Length) return false;

        var pos = _readPos;
        for (var i = 0; i < value.Length; i++)
        {
            if (_buffer[pos] != value[i]) return false;
            pos++;
            if (pos >= _buffer.Length) pos = 0;
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

            if (!token.IsOptional) break;
        }

        return matches.Count > 0;
    }

    /// <summary>
    /// Resets the enumerator to the beginning and clears the tracked <see cref="Location"/>.
    /// Only supported for string-backed enumerators.
    /// </summary>
    public void Reset()
    {
        if (_originalString == null)
        {
            throw new System.NotSupportedException(
                "Reset is not supported on TextReader-based enumerators. " +
                "Use a hint strategy that does not require enumerator reset.");
        }

        _reader = new StringReader(_originalString);
        _readPos = 0;
        _writePos = 0;
        _bufferedCount = 0;
        _readerExhausted = false;
        _resetNextLine = false;
        Location.Reset();
        CharactersConsumed = 0;
        FillBuffer();
    }

    /// <summary>
    /// Reads one character from the ring _buffer, transparently refilling from the reader if needed.
    /// Returns <c>'\0'</c> if no more characters are available.
    /// </summary>
    private char DequeueChar()
    {
        if (_bufferedCount == 0)
        {
            if (_readerExhausted) return '\0';
            FillBuffer();
            if (_bufferedCount == 0) return '\0';
        }

        var c = _buffer[_readPos];
        _readPos++;
        if (_readPos >= _buffer.Length) _readPos = 0;
        _bufferedCount--;

        return c;
    }

    /// <summary>
    /// Ensures at least <paramref name="count"/> characters are buffered, growing the _buffer if necessary.
    /// </summary>
    private void EnsureBuffered(int count)
    {
        while (_bufferedCount < count && !_readerExhausted)
        {
            if (_bufferedCount >= _buffer.Length)
            {
                GrowBuffer();
            }
            FillBuffer();
        }
    }

    /// <summary>
    /// Doubles the ring _buffer capacity, linearizing existing data into the new _buffer.
    /// </summary>
    private void GrowBuffer()
    {
        // Buffer growth is bounded by TokenizerOptions.MaxInputLength, which is validated
        // by both sync and async tokenization paths before processing continues.
        var newSize = _buffer.Length * 2;
        var newBuffer = new char[newSize];

        // Linearize existing data into the new _buffer
        if (_bufferedCount > 0)
        {
            if (_readPos + _bufferedCount <= _buffer.Length)
            {
                Array.Copy(_buffer, _readPos, newBuffer, 0, _bufferedCount);
            }
            else
            {
                var firstChunk = _buffer.Length - _readPos;
                Array.Copy(_buffer, _readPos, newBuffer, 0, firstChunk);
                Array.Copy(_buffer, 0, newBuffer, firstChunk, _bufferedCount - firstChunk);
            }
        }

        _buffer = newBuffer;
        _readPos = 0;
        _writePos = _bufferedCount;

        if (newSize > _stagingBuffer.Length)
        {
            _stagingBuffer = new char[newSize];
        }
    }

    /// <summary>
    /// Copies characters from a staging _buffer into the ring _buffer,
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
                    var peek = _reader.Peek();
                    if (peek == '\n')
                    {
                        // \r\n split across reads — skip the \r, write \n, consume the \n from reader
                        _reader.Read();
                    }
                }

                // Write \n instead of lone \r
                c = '\n';
            }

            if (_bufferedCount >= _buffer.Length)
            {
                GrowBuffer();
            }

            _buffer[_writePos] = c;
            _writePos++;
            if (_writePos >= _buffer.Length) _writePos = 0;
            _bufferedCount++;
        }
    }
}
