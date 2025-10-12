using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens.Builders;

/// <summary>
/// Builder for creating Template instances for testing
/// </summary>
public class TemplateBuilder
{
    private Template _template = new();

    public TemplateBuilder WithName(string name)
    {
        _template.Name = name;
        return this;
    }

    public TemplateBuilder WithContent(string content)
    {
        _template.Content = content;
        return this;
    }

    public TemplateBuilder WithTokens(params Token[] tokens)
    {
        // Note: Tokens are read-only in Template, they are added through parsing
        // This method is kept for API compatibility but doesn't actually add tokens
        return this;
    }

    public TemplateBuilder WithHints(params Hint[] hints)
    {
        foreach (var hint in hints)
        {
            _template.Hints.Add(hint);
        }
        return this;
    }

    public TemplateBuilder WithTags(params string[] tags)
    {
        foreach (var tag in tags)
        {
            _template.Tags.Add(tag);
        }
        return this;
    }

    public TemplateBuilder WithOptions(TokenizerOptions options)
    {
        _template.Options = options;
        return this;
    }

    public TemplateBuilder WithDefaultOptions()
    {
        _template.Options = TokenizerOptions.Defaults;
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
        return _template;
    }
}