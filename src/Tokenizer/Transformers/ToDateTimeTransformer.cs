using Tokens.Temporal;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="DateTimeOffset"/>.
/// </summary>
public sealed class ToDateTimeTransformer : IOptionsAwareTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        // Fallback for non-options-aware callers — use default options
        return TryTransform(value, args, new TokenizerOptions(), out transformed);
    }

    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed)
    {
        if (TemporalParser.TryParse(value?.ToString(), args, options, out var result))
        {
            transformed = result;
            return true;
        }

        transformed = value!;
        return false;
    }
}
