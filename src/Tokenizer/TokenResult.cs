using System;
using System.Collections.Generic;
using System.Linq;
using Tokens.Enumerators;

namespace Tokens;

/// <summary>
/// Holds the matched and unmatched tokens produced by a single template tokenization attempt.
/// </summary>
public sealed class TokenResult
{
    private readonly List<Match> _matches;
    private readonly List<Token> _misses;

    public TokenResult()
    {
        _matches = new List<Match>();
        _misses = new List<Token>();
    }

    public IReadOnlyList<Match> Matches => _matches;

    public IReadOnlyList<Token> Misses => _misses;

    internal void AddMatch(Token token, object value, FileLocation location)
    {
        if (TryConcatMatch(token, value, location)) return;

        _matches.Add(new Match(token, value, location.Clone()));
    }

    private bool TryConcatMatch(Token token, object value, FileLocation location)
    {
        if (token.CanConcatenate == false) return false;

        if (_matches.Any(m => m.Token.Name == token.Name) == false) return false;

        var match = _matches.First(m => m.Token.Name == token.Name);

        if (token.CanConcatenateValues(match.Value, value) == false) return false;

        var concatenated = token.ConcatenateValues(match.Value, value, token.ConcatenationString);
        if (concatenated != null) match.Value = concatenated;

        return true;
    }

    internal void AddMiss(Token token)
    {
        _misses.Add(token);
    }

    public bool HasMissingRequiredTokens => Misses.Any(m => m.IsRequired);

    public bool HasMatches => Matches.Any();
}
