namespace Tokens.Builders;

/// <summary>
/// Builder for creating Hint instances for testing
/// </summary>
public class HintBuilder
{
    private Hint _hint = new();

    public HintBuilder WithText(string text)
    {
        _hint.Text = text;
        return this;
    }

    public HintBuilder WithOptional(bool optional = true)
    {
        _hint.Optional = optional;
        return this;
    }

    public HintBuilder WithRequired(bool required = true)
    {
        _hint.Optional = !required;
        return this;
    }

    public Hint Build()
    {
        return _hint;
    }
}