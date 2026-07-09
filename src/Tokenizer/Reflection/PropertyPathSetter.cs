using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Tokens.Exceptions;
using Tokens.Temporal;

namespace Tokens.Reflection;

/// <summary>
/// Sets property values on objects via dot-separated property paths, with automatic
/// intermediate object creation and type-prefix stripping. Type conversion of temporal
/// types uses the provided <see cref="TokenizerOptions"/> for culture-aware parsing.
/// </summary>
internal sealed class PropertyPathSetter
{
    private const int MaxDepth = 10;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
    private static readonly ConcurrentDictionary<string, string[]> PathSegmentCache = new(StringComparer.Ordinal);

    private readonly TokenizerOptions _options;

    /// <summary>
    /// Initializes a new instance with the given options, used for culture-aware type conversion.
    /// </summary>
    public PropertyPathSetter(TokenizerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Sets a scalar value at the given dot-separated property path.
    /// </summary>
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
    public static bool IsCollectionProperty(Type rootType, string propertyPath, StringComparison comparison)
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

    private void AssignScalar(object owner, PropertyInfo prop, object value)
    {
        if (!prop.CanWrite || prop.GetSetMethod() == null)
        {
            throw new InvalidOperationException(
                $"Cannot set property '{prop.Name}' on type '{owner.GetType().Name}': property is read-only.");
        }

        var converted = ConvertValue(value, prop.PropertyType, _options);
        prop.SetValue(owner, converted, index: null);
    }

    private void AssignCollection(object owner, PropertyInfo prop, IReadOnlyList<object> values)
    {
        var propertyType = prop.PropertyType;
        var elementType = GetCollectionElementType(propertyType);

        if (elementType == null)
        {
            throw new InvalidOperationException(
                $"Collection type '{propertyType.Name}' on property '{prop.Name}' is not supported. " +
                "Supported types: List<T>, IList<T>, ICollection<T>, T[], HashSet<T>, ImmutableList<T>, ImmutableArray<T>.");
        }

        var converted = ConvertElements(values, elementType, _options);

        if (propertyType.IsArray)
        {
            AssignArray(owner, prop, converted, elementType);
        }
        else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(HashSet<>))
        {
            AssignHashSet(owner, prop, converted, elementType);
        }
        else if (IsImmutableCollectionType(propertyType))
        {
            AssignImmutableCollection(owner, prop, converted, elementType);
        }
        else
        {
            AssignList(owner, prop, converted, elementType);
        }
    }

    private static Type? GetCollectionElementType(Type propertyType)
    {
        if (propertyType.IsArray)
        {
            return propertyType.GetElementType();
        }

        if (!propertyType.IsGenericType)
        {
            return null;
        }

        var def = propertyType.GetGenericTypeDefinition();

        if (def == typeof(List<>) ||
            def == typeof(IList<>) ||
            def == typeof(ICollection<>) ||
            def == typeof(HashSet<>))
        {
            return propertyType.GetGenericArguments()[0];
        }

        if (IsImmutableCollectionType(propertyType))
        {
            return propertyType.GetGenericArguments()[0];
        }

        return null;
    }

    private static bool IsImmutableCollectionType(Type type)
    {
        if (!type.IsGenericType) return false;

        var fullName = type.GetGenericTypeDefinition().FullName;
        return fullName == "System.Collections.Immutable.ImmutableList`1" ||
               fullName == "System.Collections.Immutable.ImmutableArray`1";
    }

    private static List<object> ConvertElements(IReadOnlyList<object> values, Type elementType, TokenizerOptions options)
    {
        var result = new List<object>(values.Count);

        foreach (var value in values)
        {
            result.Add(ConvertValue(value, elementType, options));
        }

        return result;
    }

    private static void AssignArray(object owner, PropertyInfo prop, List<object> elements, Type elementType)
    {
        ThrowIfReadOnly(owner, prop);

        var array = Array.CreateInstance(elementType, elements.Count);

        for (var i = 0; i < elements.Count; i++)
        {
            array.SetValue(elements[i], i);
        }

        prop.SetValue(owner, array, index: null);
    }

    private static void AssignHashSet(object owner, PropertyInfo prop, List<object> elements, Type elementType)
    {
        ThrowIfReadOnly(owner, prop);

        var hashSetType = typeof(HashSet<>).MakeGenericType(elementType);
        var hashSet = Activator.CreateInstance(hashSetType)!;
        var addMethod = hashSetType.GetMethod("Add")!;

        foreach (var element in elements)
        {
            var added = (bool)addMethod.Invoke(hashSet, new[] { element })!;
            if (!added)
            {
                throw new InvalidOperationException(
                    $"Duplicate value '{element}' encountered while assigning to HashSet property '{prop.Name}'.");
            }
        }

        prop.SetValue(owner, hashSet, index: null);
    }

    private static void AssignImmutableCollection(object owner, PropertyInfo prop, List<object> elements, Type elementType)
    {
        ThrowIfReadOnly(owner, prop);

        var immutableTypeName = prop.PropertyType.GetGenericTypeDefinition().FullName!;
#pragma warning disable MA0001 // IndexOf(char) is inherently ordinal; no StringComparison overload exists
        var backtickIndex = immutableTypeName.IndexOf('`');
#pragma warning restore MA0001
        var nonGenericTypeName = backtickIndex >= 0 ? immutableTypeName.Substring(0, backtickIndex) : immutableTypeName;
        var immutableStaticType = prop.PropertyType.Assembly.GetType(nonGenericTypeName)
            ?? throw new InvalidOperationException(
                $"Could not find static type '{nonGenericTypeName}' for immutable collection assignment.");

        var createRange = immutableStaticType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "CreateRange" && m.GetParameters().Length == 1)
            ?? throw new InvalidOperationException(
                $"Could not find CreateRange method on '{nonGenericTypeName}'.");

        var genericCreateRange = createRange.MakeGenericMethod(elementType);

        var arrayInstance = Array.CreateInstance(elementType, elements.Count);

        for (var i = 0; i < elements.Count; i++)
        {
            arrayInstance.SetValue(elements[i], i);
        }

        var result = genericCreateRange.Invoke(null, new object[] { arrayInstance })!;
        prop.SetValue(owner, result, index: null);
    }

    private static void AssignList(object owner, PropertyInfo prop, List<object> elements, Type elementType)
    {
        // Getter-only: add to existing IList instance
        if (!prop.CanWrite || prop.GetSetMethod() == null)
        {
            var existing = prop.GetValue(owner, index: null) as System.Collections.IList
                ?? throw new InvalidOperationException(
                    $"Cannot set read-only property '{prop.Name}' on type '{owner.GetType().Name}': " +
                    "property is read-only and does not implement IList.");

            foreach (var element in elements)
            {
                existing.Add(element);
            }

            return;
        }

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

        foreach (var element in elements)
        {
            list.Add(element);
        }

        prop.SetValue(owner, list, index: null);
    }

    private static void ThrowIfReadOnly(object owner, PropertyInfo prop)
    {
        if (!prop.CanWrite || prop.GetSetMethod() == null)
        {
            throw new InvalidOperationException(
                $"Cannot set read-only property '{prop.Name}' on type '{owner.GetType().Name}'.");
        }
    }

    // ── Private: type helpers ────────────────────────────────────────────────

    private static object ConvertValue(object value, Type targetType, TokenizerOptions options)
    {
        // 1. Pass-through
        if (targetType.IsInstanceOfType(value)) return value;

        // 2. Enum
        if (targetType.IsEnum) return ConvertToEnum(value, targetType);

        // 3. Nullable<T> — unwrap and recurse
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = targetType.GetGenericArguments()[0];
            return ConvertValue(value, underlyingType, options);
        }

        // 4. Non-IConvertible structs
        var nonConvertible = TryConvertNonIConvertible(value, targetType, options);
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

    private static object? TryConvertNonIConvertible(object value, Type targetType, TokenizerOptions options)
    {
        var valueString = value.ToString();
        if (valueString == null) return null;

        try
        {
            if (targetType == typeof(Guid)) return Guid.Parse(valueString);
            if (targetType == typeof(TimeSpan)) return TimeSpan.Parse(valueString, CultureInfo.InvariantCulture);

            // DateTimeOffset projection — if value is already DateTimeOffset, project to target
            if (value is DateTimeOffset dto && DateTimeProjection.IsTemporalType(targetType))
            {
                return DateTimeProjection.Project(dto, targetType);
            }

            // Auto-conversion from string to temporal types
            // CodeQL cs/nested-if-statements: nested structure is required — fallback code
            // (TimeOnly parse) must only execute when IsTemporalType is true but TryParse fails
            if (DateTimeProjection.IsTemporalType(targetType))
            {
                if (TemporalParser.TryParse(valueString, formats: null, options, out var parsed))
                {
                    return DateTimeProjection.Project(parsed, targetType);
                }

#if NET6_0_OR_GREATER
                // TemporalParser does not handle bare time strings — fall back to TimeOnly.Parse
                if (targetType == typeof(TimeOnly))
                {
                    return TimeOnly.Parse(valueString, CultureInfo.InvariantCulture);
                }
#endif
            }

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
