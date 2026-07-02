using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens.Builders;

/// <summary>
/// Builder for creating Template instances for testing
/// </summary>
public class TemplateBuilder
{
    private readonly List<Token> _tokens = new();
    private readonly List<Hint> _hints = new();
    private readonly List<string> _tags = new();
    private string _name = string.Empty;
    private TokenizerOptions? _options;

    public TemplateBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TemplateBuilder WithTokens(params Token[] tokens)
    {
        _tokens.AddRange(tokens);
        return this;
    }

    public TemplateBuilder WithHints(params Hint[] hints)
    {
        _hints.AddRange(hints);
        return this;
    }

    public TemplateBuilder WithTags(params string[] tags)
    {
        _tags.AddRange(tags);
        return this;
    }

    public TemplateBuilder WithOptions(TokenizerOptions options)
    {
        _options = options;
        return this;
    }

    public TemplateBuilder WithDefaultOptions()
    {
        _options = new TokenizerOptions();
        return this;
    }

    public TemplateBuilder WithGlobalTransformers(params ITokenTransformer[] transformers)
    {
        // Note: GlobalTransformers property doesn't exist in Template
        // This method is kept for API compatibility
        return this;
    }

    public TemplateBuilder WithGlobalValidators(params ITokenValidator[] validators)
    {
        // Note: GlobalValidators property doesn't exist in Template
        // This method is kept for API compatibility
        return this;
    }

    public Template Build()
    {
        var template = new Template(_name);
        foreach (var token in _tokens) template.AddToken(token);
        foreach (var hint in _hints) template.AddHint(hint);
        foreach (var tag in _tags) template.AddTag(tag);
        if (_options != null) template.Options = _options;
        return template;
    }
}
