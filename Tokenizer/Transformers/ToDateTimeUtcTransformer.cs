using System;

namespace Tokens.Transformers
{
    /// <summary>
    /// Converts the token value to a <see cref="DateTime"/> in UTC format
    /// </summary>
    public class ToDateTimeUtcTransformer : ITokenTransformer
    {
        public object Transform(object value, params string[] args)
        {
            if (value == null) return string.Empty;

            if (ToDateTimeTransformer.TryParseDateTime(value, args, out var result))
            {
                value = DateTime.SpecifyKind(result, DateTimeKind.Utc);
            }

            return value;
        }
    }
}
