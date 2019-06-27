using System;
using System.Globalization;

namespace Tokens.Transformers
{
    /// <summary>
    /// Converts the token value to a <see cref="DateTime"/> in UTC format
    /// </summary>
    public class ToDateTimeUtcTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            if (ToDateTimeTransformer.TryParseDateTime(value, args, DateTimeStyles.AssumeUniversal, out var result))
            {
                transformed = DateTime.SpecifyKind(result.ToUniversalTime(), DateTimeKind.Utc);
                return true;
            };

            transformed = value;

            return false;
        }
    }
}
