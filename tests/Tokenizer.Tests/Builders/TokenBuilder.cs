using Tokens.Enumerators;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens.Builders;

/// <summary>
/// Builder for creating Token instances for testing
/// </summary>
public class TokenBuilder
{
    private Token _token = new("default");

    public TokenBuilder WithContent(string content)
    {
        _token = new Token(content);
        return this;
    }

    public TokenBuilder WithName(string name)
    {
        _token.Name = name;
        return this;
    }

    public TokenBuilder WithPreamble(string preamble)
    {
        _token.Preamble = preamble;
        return this;
    }

    public TokenBuilder WithLocation(FileLocation location)
    {
        _token.Location = location;
        return this;
    }

    public TokenBuilder WithLocation(int line, int column)
    {
        _token.Location = new FileLocation().Clone();
        // Note: FileLocation properties are read-only, so we can't set them directly
        // This is a limitation of the current API design
        return this;
    }

    public TokenBuilder WithRequired(bool required = true)
    {
        _token.Required = required;
        return this;
    }

    public TokenBuilder WithOptional(bool optional = true)
    {
        _token.Required = !optional;
        return this;
    }

    public TokenBuilder WithRepeating(bool repeating = true)
    {
        _token.Repeating = repeating;
        return this;
    }

    public TokenBuilder WithConsiderOnce(bool considerOnce = true)
    {
        _token.ConsiderOnce = considerOnce;
        return this;
    }

    public TokenBuilder WithTerminateOnNewLine(bool terminateOnNewLine = true)
    {
        _token.TerminateOnNewLine = terminateOnNewLine;
        return this;
    }

    public TokenBuilder WithIsFrontMatterToken(bool isFrontMatterToken = true)
    {
        _token.IsFrontMatterToken = isFrontMatterToken;
        return this;
    }

    public TokenBuilder WithConcatenationString(string concatenationString)
    {
        _token.ConcatenationString = concatenationString;
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

    public TokenBuilder WithId(int id)
    {
        _token.Id = id;
        return this;
    }

    public Token Build()
    {
        return _token;
    }
}