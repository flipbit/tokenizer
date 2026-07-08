using Tokens.Enumerators;

namespace Tokens.Builders;

/// <summary>
/// Builder for creating TokenizeResult instances for testing
/// </summary>
public class TokenizeResultBuilder
{
    private TokenizeResult _result = new(new TemplateBuilder().Build());

    public TokenizeResultBuilder WithTemplate(Template template)
    {
        _result = new TokenizeResult(template);
        return this;
    }

    public TokenizeResultBuilder WithMatches(params TokenMatch[] matches)
    {
        foreach (var match in matches)
        {
            _result.Tokens.AddMatch(match.Token, match.Value, match.Location);
        }
        return this;
    }

    public TokenizeResultBuilder WithMisses(params Token[] misses)
    {
        foreach (var miss in misses)
        {
            _result.Tokens.AddMiss(miss);
        }
        return this;
    }

    public TokenizeResultBuilder WithExceptions(params Exception[] exceptions)
    {
        foreach (var exception in exceptions)
        {
            _result.AddException(exception);
        }
        return this;
    }

    public TokenizeResultBuilder WithHintMatches(params HintMatch[] hintMatches)
    {
        foreach (var hintMatch in hintMatches)
        {
            _result.Hints.TryAddMatch(new Hint(Text: hintMatch.Text, Optional: hintMatch.Optional), new TokenEnumerator(""));
        }
        return this;
    }

    public TokenizeResultBuilder WithHintMisses(params Hint[] hintMisses)
    {
        foreach (var hintMiss in hintMisses)
        {
            _result.Hints.TryAddMiss(hintMiss);
        }
        return this;
    }

    public TokenizeResult Build()
    {
        return _result;
    }
}
