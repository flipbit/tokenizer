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
    private Template _template = new(string.Empty);
    private T? _value;
    private readonly System.Collections.Generic.List<TokenMatch> _matches = new();
    private readonly System.Collections.Generic.List<Token> _misses = new();
    private readonly System.Collections.Generic.List<Exception> _exceptions = new();
    private readonly System.Collections.Generic.List<HintMatch> _hintMatches = new();
    private readonly System.Collections.Generic.List<Hint> _hintMisses = new();

    public TokenizeResultBuilder<T> WithTemplate(Template template)
    {
        _template = template;
        return this;
    }

    public TokenizeResultBuilder<T> WithValue(T value)
    {
        _value = value;
        return this;
    }

    public TokenizeResultBuilder<T> WithMatches(params TokenMatch[] matches)
    {
        _matches.AddRange(matches);
        return this;
    }

    public TokenizeResultBuilder<T> WithMisses(params Token[] misses)
    {
        _misses.AddRange(misses);
        return this;
    }

    public TokenizeResultBuilder<T> WithExceptions(params Exception[] exceptions)
    {
        _exceptions.AddRange(exceptions);
        return this;
    }

    public TokenizeResultBuilder<T> WithHintMatches(params HintMatch[] hintMatches)
    {
        _hintMatches.AddRange(hintMatches);
        return this;
    }

    public TokenizeResultBuilder<T> WithHintMisses(params Hint[] hintMisses)
    {
        _hintMisses.AddRange(hintMisses);
        return this;
    }

    public TokenizeResult<T> Build()
    {
        var result = _value != null
            ? new TokenizeResult<T>(_template) { Value = _value }
            : new TokenizeResult<T>(_template);

        foreach (var match in _matches)
            result.Tokens.AddMatch(match.Token, match.Value, match.Location);
        foreach (var miss in _misses)
            result.Tokens.AddMiss(miss);
        foreach (var exception in _exceptions)
            result.AddException(exception);
        foreach (var hintMatch in _hintMatches)
            result.Hints.AddMatch(new Hint(Text: hintMatch.Text, Optional: hintMatch.Optional), new TokenEnumerator(""));
        foreach (var hintMiss in _hintMisses)
            result.Hints.AddMiss(hintMiss);

        return result;
    }
}
