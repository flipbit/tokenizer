using System;
using Tokens.Enumerators;

namespace Tokens.Builders;

/// <summary>
/// Builder for creating TokenizeResult instances for testing
/// </summary>
public class TokenizeResultBuilder
{
    private TokenizeResult _result = new(new Template(string.Empty));

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
            _result.Hints.AddMatch(new Hint(Text: hintMatch.Text, Optional: hintMatch.Optional), new TokenEnumerator(""));
        }
        return this;
    }

    public TokenizeResultBuilder WithHintMisses(params Hint[] hintMisses)
    {
        foreach (var hintMiss in hintMisses)
        {
            _result.Hints.AddMiss(hintMiss);
        }
        return this;
    }

    public TokenizeResult Build()
    {
        return _result;
    }
}

/// <summary>
/// Builder for creating TokenizeResult&lt;T&gt; instances for testing
/// </summary>
public class TokenizeResultBuilder<T> where T : class, new()
{
    private TokenizeResult<T> _result = new(new Template(string.Empty));

    public TokenizeResultBuilder<T> WithTemplate(Template template)
    {
        _result = new TokenizeResult<T>(template);
        return this;
    }

    public TokenizeResultBuilder<T> WithValue(T value)
    {
        _result.Value = value;
        return this;
    }

    public TokenizeResultBuilder<T> WithMatches(params TokenMatch[] matches)
    {
        foreach (var match in matches)
        {
            _result.Tokens.AddMatch(match.Token, match.Value, match.Location);
        }
        return this;
    }

    public TokenizeResultBuilder<T> WithMisses(params Token[] misses)
    {
        foreach (var miss in misses)
        {
            _result.Tokens.AddMiss(miss);
        }
        return this;
    }

    public TokenizeResultBuilder<T> WithExceptions(params Exception[] exceptions)
    {
        foreach (var exception in exceptions)
        {
            _result.AddException(exception);
        }
        return this;
    }

    public TokenizeResultBuilder<T> WithHintMatches(params HintMatch[] hintMatches)
    {
        foreach (var hintMatch in hintMatches)
        {
            _result.Hints.AddMatch(new Hint(Text: hintMatch.Text, Optional: hintMatch.Optional), new TokenEnumerator(""));
        }
        return this;
    }

    public TokenizeResultBuilder<T> WithHintMisses(params Hint[] hintMisses)
    {
        foreach (var hintMiss in hintMisses)
        {
            _result.Hints.AddMiss(hintMiss);
        }
        return this;
    }

    public TokenizeResult<T> Build()
    {
        return _result;
    }
}
