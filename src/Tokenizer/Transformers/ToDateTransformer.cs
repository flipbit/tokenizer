#if NET6_0_OR_GREATER
using Tokens.Temporal;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="DateOnly"/>.
/// Silently drops any time component present in the value.
/// </summary>
public sealed class ToDateTransformer : IOptionsAwareTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        return TryTransform(value, args, new TokenizerOptions(), out transformed);
    }

    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed)
    {
        if (TemporalParser.TryParse(value?.ToString(), args, options, out var dto))
        {
            transformed = DateOnly.FromDateTime(dto.Date);
            return true;
        }

        transformed = value!;
        return false;
    }
}
#endif
