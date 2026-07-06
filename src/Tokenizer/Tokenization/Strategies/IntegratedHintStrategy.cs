using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization.Strategies;

/// <summary>
/// Single-pass hint strategy that tracks hints as token preambles are matched during
/// tokenization, rather than scanning the input in a separate phase. Stream-native —
/// does not require enumerator reset.
/// </summary>
internal sealed class IntegratedHintStrategy : IHintStrategy
{
    private Template? _currentTemplate;
    private readonly HashSet<string> _matchedPreambles = new();

    /// <inheritdoc />
    public bool PreProcess(Template template, TokenEnumerator enumerator,
                           string? rawInput, TokenizeResultBase result, IDiagnosticCollector collector)
    {
        _currentTemplate = template;
        _matchedPreambles.Clear();

        return false;
    }

    /// <inheritdoc />
    public void OnTokenMatched(Token token)
    {
        if (string.IsNullOrEmpty(token.Preamble) == false)
        {
            _matchedPreambles.Add(token.Preamble);
        }
    }

    /// <inheritdoc />
    public bool PostProcess(TokenizeResultBase result)
    {
        if (_currentTemplate == null || _currentTemplate.Hints.Count == 0)
        {
            return false;
        }

        foreach (var hint in _currentTemplate.Hints)
        {
            if (string.IsNullOrEmpty(hint.Text))
            {
                continue;
            }

            var satisfied = _matchedPreambles.Any(p => p.Contains(hint.Text));

            if (satisfied)
            {
                result.Hints.TryAddMatch(hint, new TokenEnumerator(string.Empty));
            }
        }

        foreach (var hint in _currentTemplate.Hints)
        {
            result.Hints.TryAddMiss(hint);
        }

        return result.Hints.Misses.Any(h => h.Optional == false);
    }
}
