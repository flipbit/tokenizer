using System.Diagnostics.CodeAnalysis;

namespace Tokens.Temporal;

/// <summary>
/// Projects a <see cref="DateTimeOffset"/> value to a target temporal type.
/// </summary>
[SuppressMessage("Meziantou.Analyzer", "MA0182", Justification = "Used by PropertyPathSetter for temporal type auto-conversion")]
internal static class DateTimeProjection
{
    /// <summary>
    /// Projects a <see cref="DateTimeOffset"/> to the specified target type.
    /// </summary>
    public static object Project(DateTimeOffset source, Type targetType)
    {
        if (targetType == typeof(DateTimeOffset)) return source;

        if (targetType == typeof(DateTime))
        {
            return source.Offset == TimeSpan.Zero
                ? source.UtcDateTime
                : source.DateTime;
        }

#if NET6_0_OR_GREATER
        if (targetType == typeof(DateOnly))
        {
            return DateOnly.FromDateTime(source.Date);
        }

        if (targetType == typeof(TimeOnly))
        {
            return TimeOnly.FromTimeSpan(source.TimeOfDay);
        }
#endif

        throw new InvalidOperationException(
            $"Cannot project DateTimeOffset to {targetType.Name}.");
    }

    /// <summary>
    /// Returns true if the target type is a temporal type that can be projected from DateTimeOffset.
    /// </summary>
    public static bool IsTemporalType(Type type)
    {
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return true;
#if NET6_0_OR_GREATER
        if (type == typeof(DateOnly) || type == typeof(TimeOnly)) return true;
#endif
        return false;
    }
}
