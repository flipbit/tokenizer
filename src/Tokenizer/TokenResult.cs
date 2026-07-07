using Tokens.Enumerators;

namespace Tokens;

/// <summary>
/// Holds the matched and unmatched tokens produced by a single template tokenization attempt.
/// </summary>
public sealed class TokenResult
{
    private readonly List<TokenMatch> _matches;
    private readonly List<Token> _misses;

    /// <summary>
    /// Creates a new empty <see cref="TokenResult"/>.
    /// </summary>
    public TokenResult()
    {
        _matches = new List<TokenMatch>();
        _misses = new List<Token>();
    }

    /// <summary>
    /// The tokens that were successfully matched in the input.
    /// </summary>
    public IReadOnlyList<TokenMatch> Matches => _matches;

    /// <summary>
    /// The tokens that were not found in the input.
    /// </summary>
    public IReadOnlyList<Token> Misses => _misses;

    internal void AddMatch(Token token, object value, FileLocation location)
    {
        if (TryConcatMatch(token, value)) return;

        _matches.Add(new TokenMatch(token, value, location.Clone()));
    }

    private bool TryConcatMatch(Token token, object value)
    {
        if (!token.CanConcatenate) return false;

        var index = _matches.FindIndex(m => string.Equals(m.Token.Name, token.Name, StringComparison.Ordinal));
        if (index < 0) return false;

        var match = _matches[index];

        if (!token.CanConcatenateValues(match.Value, value)) return false;

        var concatenated = token.ConcatenateValues(match.Value, value, token.ConcatenationString);
        if (concatenated != null) _matches[index] = match with { Value = concatenated };

        return true;
    }

    internal void AddMiss(Token token)
    {
        _misses.Add(token);
    }

    /// <summary>
    /// <see langword="true"/> when at least one required token was not matched in the input.
    /// </summary>
    public bool HasMissingRequiredTokens => _misses.Exists(m => m.IsRequired);

    /// <summary>
    /// <see langword="true"/> when at least one token was matched in the input.
    /// </summary>
    public bool HasMatches => _matches.Count > 0;

    /// <inheritdoc />
    public override string ToString() => $"TokenResult({Matches.Count} matched, {Misses.Count} missed)";
}
