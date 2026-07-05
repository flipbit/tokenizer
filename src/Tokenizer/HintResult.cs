using Tokens.Enumerators;

namespace Tokens;

/// <summary>
/// Contains the results of processing a <see cref="Template"/> for
/// <see cref="Hint"/> strings.
/// </summary>
public sealed class HintResult
{
    private readonly List<HintMatch> _matches;
    private readonly List<Hint> _misses;

    /// <summary>
    /// Creates a new empty <see cref="HintResult"/>.
    /// </summary>
    public HintResult()
    {
        _matches = new List<HintMatch>();
        _misses = new List<Hint>();
    }

    /// <summary>
    /// Gets the hint matches
    /// </summary>
    public IReadOnlyList<HintMatch> Matches => _matches;

    /// <summary>
    /// Gets the hint misses
    /// </summary>
    public IReadOnlyList<Hint> Misses => _misses;

    internal bool TryAddMatch(Hint hint, TokenEnumerator enumerator)
    {
        if (_matches.Any(m => m.Text == hint.Text)) return false;

        _matches.Add(new HintMatch(hint.Text, hint.Optional, enumerator.Location.Clone()));

        return true;
    }

    internal bool TryAddMiss(Hint hint)
    {
        if (_misses.Any(m => m.Text == hint.Text) ||
            _matches.Any(m => m.Text == hint.Text)) return false;

        _misses.Add(hint with { });

        return true;
    }

    /// <summary>
    /// <c>true</c> when at least one required hint was not found in the input.
    /// </summary>
    public bool HasMissingRequiredHints => Misses.Any(m => m.Optional == false);

    /// <inheritdoc />
    public override string ToString() => $"HintResult({Matches.Count} matched, {Misses.Count} missed)";
}
