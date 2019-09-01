using System;
using System.Globalization;
using Tokens.Extensions;

namespace Tokens.Transformers
{
    /// <summary>
    /// Converts the token value to a <see cref="DateTime"/> in UTC format
    /// </summary>
    public class ToDateTimeUtcTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            var valueString = value as string;

            if (string.IsNullOrWhiteSpace(valueString) == false)
            {
                if (valueString.Contains("(UTC)"))
                {
                    valueString = valueString.SubstringBeforeString("(UTC)");
                }

                if (valueString.Contains("UTC"))
                {
                    valueString = valueString.SubstringBeforeString("UTC");
                }

                value = valueString.Trim();
            }

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
