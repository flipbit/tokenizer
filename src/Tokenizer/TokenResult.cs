using Tokens.Enumerators;

namespace Tokens;

/// <summary>
/// Holds the matched and unmatched tokens produced by a single template tokenization attempt.
/// </summary>
public sealed class TokenResult
{
    private readonly List<TokenMatch> _matches;
    private readonly List<Token> _misses;

    public TokenResult()
    {
        _matches = new List<TokenMatch>();
        _misses = new List<Token>();
    }

    public IReadOnlyList<TokenMatch> Matches => _matches;

    public IReadOnlyList<Token> Misses => _misses;

    internal void AddMatch(Token token, object value, FileLocation location)
    {
        if (TryConcatMatch(token, value, location)) return;

        _matches.Add(new TokenMatch(token, value, location.Clone()));
    }

    private bool TryConcatMatch(Token token, object value, FileLocation location)
    {
        if (token.CanConcatenate == false) return false;

        var index = _matches.FindIndex(m => m.Token.Name == token.Name);
        if (index < 0) return false;

        var match = _matches[index];

        if (token.CanConcatenateValues(match.Value, value) == false) return false;

        var concatenated = token.ConcatenateValues(match.Value, value, token.ConcatenationString);
        if (concatenated != null) _matches[index] = match with { Value = concatenated };

        return true;
    }

    internal void AddMiss(Token token)
    {
        _misses.Add(token);
    }

    public bool HasMissingRequiredTokens => Misses.Any(m => m.IsRequired);

    public bool HasMatches => Matches.Any();
}
