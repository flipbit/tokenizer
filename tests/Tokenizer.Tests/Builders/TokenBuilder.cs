using System;
using System.Collections.Generic;
using Tokens.Enumerators;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens.Builders;

/// <summary>
/// Builder for creating Token instances for testing
/// </summary>
public class TokenBuilder
{
    private string _content = "default";
    private string _name = string.Empty;
    private string _preamble = string.Empty;
    private FileLocation _location = new();
    private readonly List<Action<Token>> _configurations = new();

    public TokenBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    public TokenBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TokenBuilder WithPreamble(string preamble)
    {
        _preamble = preamble;
        return this;
    }

    public TokenBuilder WithLocation(FileLocation location)
    {
        _location = location;
        return this;
    }

    public TokenBuilder WithLocation(int line, int column)
    {
        _location = new FileLocation().Clone();
        return this;
    }

    public TokenBuilder WithRequired(bool required = true)
    {
        _configurations.Add(t => t.IsRequired = required);
        return this;
    }

    public TokenBuilder WithOptional(bool optional = true)
    {
        _configurations.Add(t =>
        {
            t.IsOptional = optional;
            t.IsRequired = !optional;
        });
        return this;
    }

    public TokenBuilder WithRepeating(bool repeating = true)
    {
        _configurations.Add(t => t.IsRepeating = repeating);
        return this;
    }

    public TokenBuilder WithConsiderOnce(bool considerOnce = true)
    {
        _configurations.Add(t => t.IsSingleUse = considerOnce);
        return this;
    }

    public TokenBuilder WithTerminateOnNewLine(bool terminateOnNewLine = true)
    {
        _configurations.Add(t => t.TerminateOnNewLine = terminateOnNewLine);
        return this;
    }

    public TokenBuilder WithIsFrontMatterToken(bool isFrontMatterToken = true)
    {
        _configurations.Add(t => t.IsFrontMatterToken = isFrontMatterToken);
        return this;
    }

    public TokenBuilder WithConcatenationString(string concatenationString)
    {
        _configurations.Add(t => t.ConcatenationString = concatenationString);
        return this;
    }

    public TokenBuilder WithTransformers(params ITokenTransformer[] transformers)
    {
        // Note: Transformers property doesn't exist in Token
        // This method is kept for API compatibility
        return this;
    }

    public TokenBuilder WithValidators(params ITokenValidator[] validators)
    {
        // Note: Validators property doesn't exist in Token
        // This method is kept for API compatibility
        return this;
    }

    public Token Build()
    {
        var token = new Token(_content, _name, _preamble, _location);
        foreach (var config in _configurations) config(token);
        return token;
    }
}
