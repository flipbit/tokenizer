using System.Collections.Generic;
using System.Text;

namespace Tokens.Enumerators;

public class TokenEnumerator
{
    private readonly string pattern;
    private readonly int patternLength;

    private int currentLocation;

    private bool resetNextLine;

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

    public bool IsEmpty => currentLocation >= patternLength;

    public FileLocation Location { get; }

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

    public char Peek()
    {
        if (IsEmpty) return '\0';

        return pattern[currentLocation];
    }

    public char Peek(int offset)
    {
        if (IsEmpty) return '\0';

        var location = currentLocation + offset;

        if (location >= patternLength) return '\0';

        return pattern[currentLocation + offset];
    }

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

    public void Advance(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Next();
        }
    }

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

    public void Reset()
    {
        currentLocation = 0;
        Location.Reset();
    }
}
