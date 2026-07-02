namespace Tokens.Enumerators;

/// <summary>
/// A forward-only, character-level enumerator over a string that tracks the current
/// <see cref="FileLocation"/> (line and column) as it advances.
/// </summary>
public class TokenEnumerator
{
    private readonly string pattern;
    private readonly int patternLength;

    private int currentLocation;

    private bool resetNextLine;

    /// <summary>
    /// Initializes a new instance of <see cref="TokenEnumerator"/> over the specified string.
    /// Windows-style line endings (<c>\r\n</c>) are normalised to <c>\n</c>.
    /// </summary>
    /// <param name="pattern">The string to enumerate.</param>
    public TokenEnumerator(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) == false)
        {
            if (pattern.Contains("\r\n"))
            {
                pattern = pattern.Replace("\r\n", "\n");
            }
        }

        if (string.IsNullOrEmpty(pattern))
        {
            patternLength = 0;
        }
        else
        {
            patternLength = pattern.Length;
        }

        this.pattern = pattern;

        currentLocation = 0;
        Location = new FileLocation();
    }

    /// <summary>
    /// Gets a value indicating whether all characters in the string have been consumed.
    /// </summary>
    public bool IsEmpty => currentLocation >= patternLength;

    /// <summary>
    /// Gets the current position in the source string as a line/column <see cref="FileLocation"/>.
    /// </summary>
    public FileLocation Location { get; }

    /// <summary>
    /// Advances the enumerator by one character and returns it, updating <see cref="Location"/>.
    /// Returns <c>'\0'</c> if the enumerator is already at the end of the string.
    /// </summary>
    /// <returns>The next character, or <c>'\0'</c> if <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    public char Next()
    {
        if (IsEmpty) return '\0';

        var next = pattern[currentLocation];
        currentLocation++;

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
    /// Returns <c>'\0'</c> if the enumerator is already at the end of the string.
    /// </summary>
    /// <returns>The next character, or <c>'\0'</c> if <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    public char Peek()
    {
        if (IsEmpty) return '\0';

        return pattern[currentLocation];
    }

    /// <summary>
    /// Returns the character at the specified offset ahead of the current position, without advancing.
    /// Returns <c>'\0'</c> if the offset is beyond the end of the string or the enumerator is empty.
    /// </summary>
    /// <param name="offset">The zero-based number of characters ahead of the current position to look at.</param>
    /// <returns>The character at the given offset, or <c>'\0'</c> if the position is out of range.</returns>
    public char Peek(int offset)
    {
        if (IsEmpty) return '\0';

        var location = currentLocation + offset;

        if (location >= patternLength) return '\0';

        return pattern[currentLocation + offset];
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
        if (currentLocation + value.Length > patternLength) return false;

#if NET8_0_OR_GREATER
        return pattern.AsSpan(currentLocation, value.Length).SequenceEqual(value.AsSpan());
#else
        return string.CompareOrdinal(pattern, currentLocation, value, 0, value.Length) == 0;
#endif
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
    /// Resets the enumerator to the beginning of the string and clears the tracked <see cref="Location"/>.
    /// </summary>
    public void Reset()
    {
        currentLocation = 0;
        Location.Reset();
    }
}
