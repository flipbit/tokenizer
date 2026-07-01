using System;

namespace Tokens.Transformers;

/// <summary>
/// Splits a token value on a specified delimiter
/// </summary>
public sealed class SplitTransformer : ITokenTransformer
{
    public bool CanTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length != 1) throw new ArgumentException($"Split(value): missing arguments processing: {value}");

        var valueArray = valueString.Split(new[] { args[0] }, StringSplitOptions.RemoveEmptyEntries);
        if (valueArray.Length > 1)
        {
            transformed = valueArray;
        }
        else
        {
            transformed = value;
        }

        return true;
    }
}
