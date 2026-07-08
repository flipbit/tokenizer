using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Tokens.Exceptions;

namespace Tokens.Reflection;

/// <summary>
/// Sets property values on objects via dot-separated property paths, with automatic
/// intermediate object creation and type-prefix stripping.
/// </summary>
[SuppressMessage("Meziantou.Analyzer", "MA0182", Justification = "Wired in Task 5 — class is used via instance in Assign<T>()")]
internal sealed class PropertyPathSetter
{
    private const int MaxDepth = 10;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
    private static readonly ConcurrentDictionary<string, string[]> PathSegmentCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Sets a scalar value at the given dot-separated property path.
    /// </summary>
    [SuppressMessage("Performance", "CA1822", Justification = "Instance method — will access instance state when options are added in Task 5")]
    public void SetScalar(object root, string propertyPath, object value, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(propertyPath))
        {
            throw new ArgumentNullException(nameof(propertyPath));
        }

        var segments = ParseSegments(propertyPath);
        var startDepth = StripTypePrefix(root, segments, comparison) ? 1 : 0;

        if (segments.Length - startDepth > MaxDepth)
        {
            throw new InvalidOperationException(
                $"Property path '{propertyPath}' exceeds the maximum allowed depth of {MaxDepth}.");
        }

        var (owner, prop) = ResolveLeaf(root, segments, comparison);
        AssignScalar(owner, prop, value);
    }

    /// <summary>
    /// Sets a collection of values at the given dot-separated property path.
    /// </summary>
    [SuppressMessage("Performance", "CA1822", Justification = "Instance method — will access instance state when options are added in Task 5")]
    public void SetCollection(object root, string propertyPath, IReadOnlyList<object> values, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(propertyPath))
        {
            throw new ArgumentNullException(nameof(propertyPath));
        }

        var segments = ParseSegments(propertyPath);
        var startDepth = StripTypePrefix(root, segments, comparison) ? 1 : 0;

        if (segments.Length - startDepth > MaxDepth)
        {
            throw new InvalidOperationException(
                $"Property path '{propertyPath}' exceeds the maximum allowed depth of {MaxDepth}.");
        }

        var (owner, prop) = ResolveLeaf(root, segments, comparison);
        AssignCollection(owner, prop, values);
    }

    /// <summary>
    /// Returns true if the property at the given path on <paramref name="rootType"/> is a supported collection type.
    /// </summary>
    [SuppressMessage("Performance", "CA1822", Justification = "Instance method — will access instance state when options are added in Task 5")]
    public bool IsCollectionProperty(Type rootType, string propertyPath, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(propertyPath))
        {
            return false;
        }

        var segments = ParseSegments(propertyPath);
        var startIndex = segments.Length > 1 && string.Equals(rootType.Name, segments[0], comparison) ? 1 : 0;

        var currentType = rootType;
        for (var i = startIndex; i < segments.Length - 1; i++)
        {
            var intermediate = FindProperty(currentType, segments[i], comparison);
            if (intermediate == null)
            {
                return false;
            }

            currentType = intermediate.PropertyType;
        }

        var prop = FindProperty(currentType, segments[segments.Length - 1], comparison);
        return prop != null && IsCollectionType(prop.PropertyType);
    }

    // ── Private: resolution ──────────────────────────────────────────────────

    private static (object owner, PropertyInfo property) ResolveLeaf(object root, string[] segments, StringComparison comparison)
    {
        var owner = TraverseToLeaf(root, segments, comparison);
        var leafName = segments[segments.Length - 1];
        var prop = FindProperty(owner.GetType(), leafName, comparison)
            ?? throw new MissingMemberException(
                $"Could not find property '{leafName}' on {owner.GetType().Name}");

        return (owner, prop);
    }

    private static object TraverseToLeaf(object current, string[] segments, StringComparison comparison)
    {
        // segments[0] may be the type name prefix — skip it if so
        var startIndex = StripTypePrefix(current, segments, comparison) ? 1 : 0;
        var lastSegment = segments.Length - 1;

        for (var i = startIndex; i < lastSegment; i++)
        {
            var prop = FindProperty(current.GetType(), segments[i], comparison)
                ?? throw new MissingMemberException(
                    $"Could not find property '{segments[i]}' on {current.GetType().Name}");

            ValidateIntermediateType(prop.PropertyType, segments[i]);

            var next = prop.GetValue(current, index: null);

            if (next == null)
            {
                next = Activator.CreateInstance(prop.PropertyType)
                    ?? throw new InvalidOperationException(
                        $"Failed to create instance of '{prop.PropertyType.Name}'");
                prop.SetValue(current, next, index: null);
            }

            current = next;
        }

        return current;
    }

    private static bool StripTypePrefix(object root, string[] segments, StringComparison comparison)
    {
        return segments.Length > 1
            && string.Equals(root.GetType().Name, segments[0], comparison);
    }

    private static void ValidateIntermediateType(Type type, string segmentName)
    {
        if (type.IsValueType)
        {
            throw new InvalidOperationException(
                $"Value types cannot be used as intermediate path segments ('{segmentName}' is a value type '{type.Name}'). " +
                "Intermediate properties must be reference types.");
        }

        if (type.IsInterface)
        {
            throw new InvalidOperationException(
                $"Cannot auto-instantiate interface type '{type.Name}' for intermediate path segment '{segmentName}'.");
        }

        if (type.IsAbstract)
        {
            throw new InvalidOperationException(
                $"Cannot auto-instantiate abstract type '{type.Name}' for intermediate path segment '{segmentName}'.");
        }
    }

    private static void AssignScalar(object owner, PropertyInfo prop, object value)
    {
        if (!prop.CanWrite || prop.GetSetMethod() == null)
        {
            throw new InvalidOperationException(
                $"Cannot set property '{prop.Name}' on type '{owner.GetType().Name}': property is read-only.");
        }

        var converted = ConvertValue(value, prop.PropertyType);
        prop.SetValue(owner, converted, index: null);
    }

    private static void AssignCollection(object owner, PropertyInfo prop, IReadOnlyList<object> values)
    {
        throw new NotImplementedException(
            $"AssignCollection is not yet implemented (property '{prop.Name}' on '{owner.GetType().Name}', {values.Count} values).");
    }

    // ── Private: type helpers ────────────────────────────────────────────────

    private static object ConvertValue(object value, Type targetType)
    {
        // 1. Pass-through
        if (targetType.IsInstanceOfType(value)) return value;

        // 2. Enum
        if (targetType.IsEnum) return ConvertToEnum(value, targetType);

        // 3. Nullable<T> — unwrap and recurse
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = targetType.GetGenericArguments()[0];
            return ConvertValue(value, underlyingType);
        }

        // 4. Non-IConvertible structs
        var nonConvertible = TryConvertNonIConvertible(value, targetType);
        if (nonConvertible != null) return nonConvertible;

        // 5. IConvertible primitives
        try
        {
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new TypeConversionException(
                $"Unable to convert '{value}' to type {targetType.Name}", value, targetType, ex);
        }
    }

    private static object ConvertToEnum(object value, Type enumType)
    {
        if (value.GetType() == enumType) return value;

        var valueString = value.ToString()
            ?? throw new TypeConversionException(
                $"Cannot convert null string to enum type {enumType.Name}", value, enumType);

        try { return Enum.Parse(enumType, valueString, ignoreCase: true); }
        catch (ArgumentException ex)
        {
            throw new TypeConversionException(
                $"Unable to convert '{valueString}' to enum type {enumType.Name}", value, enumType, ex);
        }
    }

    private static object? TryConvertNonIConvertible(object value, Type targetType)
    {
        var valueString = value.ToString();
        if (valueString == null) return null;

        try
        {
            if (targetType == typeof(Guid)) return Guid.Parse(valueString);
            if (targetType == typeof(TimeSpan)) return TimeSpan.Parse(valueString, CultureInfo.InvariantCulture);
            if (targetType == typeof(DateTimeOffset)) return DateTimeOffset.Parse(valueString, CultureInfo.InvariantCulture);
#if NET6_0_OR_GREATER
            if (targetType == typeof(DateOnly)) return DateOnly.Parse(valueString, CultureInfo.InvariantCulture);
            if (targetType == typeof(TimeOnly)) return TimeOnly.Parse(valueString, CultureInfo.InvariantCulture);
#endif
            return null;
        }
        catch (FormatException ex)
        {
            throw new TypeConversionException(
                $"Unable to convert '{value}' to type {targetType.Name}", value, targetType, ex);
        }
    }

    private static bool IsCollectionType(Type type)
    {
        if (type.IsArray) return true;

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(List<>)) return true;
            if (def == typeof(IList<>)) return true;
            if (def == typeof(ICollection<>)) return true;
            if (def == typeof(HashSet<>)) return true;

            var fullName = def.FullName;
            if (fullName == "System.Collections.Immutable.ImmutableList`1" ||
                fullName == "System.Collections.Immutable.ImmutableArray`1")
            {
                return true;
            }
        }

        return false;
    }

    // ── Private: caching helpers ─────────────────────────────────────────────

    private static string[] ParseSegments(string path)
    {
        return PathSegmentCache.GetOrAdd(path, static p => p.Split('.'));
    }

    private static PropertyInfo? FindProperty(Type type, string name, StringComparison comparison)
    {
        var properties = PropertyCache.GetOrAdd(type, static t => t.GetProperties());

        foreach (var prop in properties)
        {
            if (string.Equals(prop.Name, name, comparison))
            {
                return prop;
            }
        }

        return null;
    }
}
