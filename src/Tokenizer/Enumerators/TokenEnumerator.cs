using System.IO;
using System.Collections.Generic;

namespace Tokens.Enumerators;

/// <summary>
/// A forward-only, character-level enumerator over a <see cref="TextReader"/> that tracks the current
/// <see cref="FileLocation"/> (line and column) as it advances. All line endings are normalised to <c>\n</c>.
/// </summary>
public class TokenEnumerator
{
    private TextReader reader;
    private readonly string? originalString;
    private readonly Queue<char> pushback = new Queue<char>();

    private bool isEmpty;
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
        isEmpty = reader.Peek() == -1;
        Location = new FileLocation();
    }

    /// <summary>
    /// Gets a value indicating whether all characters have been consumed.
    /// </summary>
    public bool IsEmpty => isEmpty && pushback.Count == 0;

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
    /// Advances the enumerator by one character and returns it, updating <see cref="Location"/>.
    /// Returns <c>'\0'</c> if the enumerator is already at the end.
    /// </summary>
    /// <returns>The next character, or <c>'\0'</c> if <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    public char Next()
    {
        var next = ReadChar();
        if (next == '\0') return '\0';

        CharactersConsumed++;

        // Eagerly detect end-of-input so IsEmpty is accurate immediately
        if (pushback.Count == 0 && !isEmpty && reader.Peek() == -1)
        {
            isEmpty = true;
        }

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
        if (pushback.Count > 0)
        {
            return pushback.Peek();
        }

        if (isEmpty) return '\0';

        var raw = reader.Peek();
        if (raw == -1)
        {
            isEmpty = true;
            return '\0';
        }

        if (raw == '\r')
        {
            // Need to resolve CRLF properly — read through ReadChar and push into pushback
            var resolved = ReadChar();
            if (resolved == '\0') return '\0';
            pushback.Enqueue(resolved);
            return resolved;
        }

        return (char)raw;
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

        EnsurePushback(value.Length);

        if (pushback.Count < value.Length) return false;

        var index = 0;
        foreach (var c in pushback)
        {
            if (index >= value.Length) break;
            if (c != value[index]) return false;
            index++;
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

        pushback.Clear();
        reader = new StringReader(originalString);
        isEmpty = reader.Peek() == -1;
        resetNextLine = false;
        Location.Reset();
        CharactersConsumed = 0;
    }

    /// <summary>
    /// Reads one character from the pushback queue or the underlying reader,
    /// normalizing all line endings to <c>\n</c>.
    /// </summary>
    private char ReadChar()
    {
        if (pushback.Count > 0)
        {
            return pushback.Dequeue();
        }

        if (isEmpty) return '\0';

        var raw = reader.Read();
        if (raw == -1)
        {
            isEmpty = true;
            return '\0';
        }

        var c = (char)raw;

        if (c == '\r')
        {
            // Check if followed by \n — if so, consume it
            if (reader.Peek() == '\n')
            {
                reader.Read();
            }
            return '\n';
        }

        return c;
    }

    /// <summary>
    /// Reads characters from the reader into the pushback queue until it contains
    /// at least <paramref name="count"/> characters, or the reader is exhausted.
    /// Only reads from the underlying reader, not from the pushback queue.
    /// </summary>
    private void EnsurePushback(int count)
    {
        while (pushback.Count < count)
        {
            if (isEmpty) break;

            var raw = reader.Read();
            if (raw == -1)
            {
                isEmpty = true;
                break;
            }

            if (raw == '\r')
            {
                if (reader.Peek() == '\n')
                {
                    reader.Read();
                }
                pushback.Enqueue('\n');
            }
            else
            {
                pushback.Enqueue((char)raw);
            }
        }
    }
}
