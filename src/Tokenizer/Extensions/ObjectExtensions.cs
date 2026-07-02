using System.Collections.Concurrent;
using System.Reflection;
using Tokens.Exceptions;

namespace Tokens.Extensions;

/// <summary>
/// Extension methods for setting and getting property values on objects via dot-separated property paths.
/// </summary>
public static class ObjectExtensions
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    private static PropertyInfo[] GetCachedProperties(Type type)
    {
        return PropertyCache.GetOrAdd(type, t => t.GetProperties());
    }

    /// <summary>
    /// Sets the given value on the given property with the given path.
    /// </summary>
    public static T SetValue<T>(this T @object, string propertyPath, object value) where T : class
    {
        return SetValue(@object, propertyPath, value, StringComparison.InvariantCulture);
    }

    /// <summary>
    /// Sets the value at the given dot-separated <paramref name="propertyPath"/> using the specified string comparison.
    /// </summary>
    /// <typeparam name="T">The type of the object being modified.</typeparam>
    /// <param name="object">The object on which to set the value.</param>
    /// <param name="propertyPath">A dot-separated path to the property (e.g. <c>"Address.City"</c>).</param>
    /// <param name="value">The value to assign.</param>
    /// <param name="stringComparison">The comparison to use when matching property names.</param>
    /// <returns>The modified object.</returns>
    public static T SetValue<T>(this T @object, string propertyPath, object value, StringComparison stringComparison) where T : class
    {
        if (string.IsNullOrEmpty(propertyPath))
        {
            throw new ArgumentNullException(nameof(propertyPath));
        }

        var segments = propertyPath.Split('.');
        var objectType = @object.GetType().Name;
        var depth = string.Equals(objectType, segments[0], stringComparison) ? 1 : 0;
        @object = (T)SetInnerValue(@object, segments, depth, value, stringComparison);

        return @object;
    }

    private static object SetInnerValue(object @object, string[] segments, int depth, object value, StringComparison stringComparison)
    {
        var set = false;
        var propertyInfos = GetCachedProperties(@object.GetType());

        foreach (var propertyInfo in propertyInfos)
        {
            if (!string.Equals(propertyInfo.Name, segments[depth], stringComparison)) continue;

            set = true;

            // Layer 4: Debug Instrumentation
            // Log property details before attempting assignment for forensics
            System.Diagnostics.Debug.WriteLine(
                $"[SetValue] Attempting to set property '{propertyInfo.Name}' on type '{@object.GetType().Name}'. " +
                $"CanWrite: {propertyInfo.CanWrite}, HasSetter: {propertyInfo.GetSetMethod() != null}, " +
                $"PropertyType: {propertyInfo.PropertyType.Name}, ValueType: {value?.GetType().Name ?? "null"}");

            if (depth == segments.Length - 1)
            {
                if (propertyInfo.PropertyType.IsGenericType &&
                   (propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(IList<>) ||
                    propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    var list = propertyInfo.GetValue(@object, null);

                    if (list == null)
                    {
                        if (!propertyInfo.CanWrite || propertyInfo.GetSetMethod() == null)
                        {
                            throw new InvalidOperationException(
                                $"Cannot set property '{propertyInfo.Name}' on type '{@object.GetType().Name}': " +
                                "property is read-only and the collection is null.");
                        }

                        var genericType = propertyInfo.PropertyType.GetGenericArguments()[0];
                        var enumerableType = typeof(List<>);
                        var constructedEnumerableType = enumerableType.MakeGenericType(genericType);
                        list = Activator.CreateInstance(constructedEnumerableType)
                            ?? throw new InvalidOperationException($"Failed to create instance of {constructedEnumerableType.Name}");

                        propertyInfo.SetValue(@object, list, null);
                    }

                    var addMethod = list.GetType().GetMethod("Add")
                        ?? throw new InvalidOperationException($"Type {list.GetType().Name} does not have an Add method");

                    if (value is IEnumerable<string> valueList)
                    {
                        foreach (var valueItem in valueList)
                        {
                            addMethod.Invoke(list, new[] { valueItem });
                        }
                    }
                    else
                    {
                        addMethod.Invoke(list, new[] { value });
                    }

                }
                else if (!propertyInfo.CanWrite || propertyInfo.GetSetMethod() == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot set property '{propertyInfo.Name}' on type '{@object.GetType().Name}': " +
                        "property is read-only. Anonymous types and read-only properties cannot be modified.");
                }
                else if (propertyInfo.PropertyType.IsGenericType &&
                         propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var genericType = propertyInfo.PropertyType.GetGenericArguments()[0];

                    try
                    {
                        if (value == null)
                        {
                            propertyInfo.SetValue(@object, null, null);
                        }
                        else if (value.GetType() == genericType)
                        {
                            propertyInfo.SetValue(@object, value, null);
                        }
                        else
                        {
                            var convertedValue = Convert.ChangeType(value, genericType);

                            propertyInfo.SetValue(@object, convertedValue, null);
                        }
                    }
                    catch (FormatException e)
                    {
                        throw new TypeConversionException($"Unable to convert '{value}' to type {genericType}", value ?? "null", genericType, e);
                    }
                }
                else if (value != null)
                {
                    var convertedValue = ChangeType(value, propertyInfo.PropertyType);

                    propertyInfo.SetValue(@object, convertedValue, null);
                }

                break;
            }

            var currentValue = propertyInfo.GetValue(@object, null);

            if (currentValue == null)
            {
                try
                {
                    currentValue = Activator.CreateInstance(propertyInfo.PropertyType)
                        ?? throw new InvalidOperationException($"Failed to create instance of {propertyInfo.PropertyType.Name}");
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Could not create type: " + propertyInfo.PropertyType, ex);
                }

                propertyInfo.SetValue(@object, currentValue, null);
            }

            if (value != null)
            {
                SetInnerValue(currentValue, segments, depth + 1, value, stringComparison);
            }

            break;
        }

        if (!set)
        {
            throw new MissingMemberException($@"Could find property '{segments[depth]}' on {@object.GetType().Name}");
        }

        return @object;
    }

    private static object ChangeType(object value, Type targetType)
    {
        if (targetType.IsEnum)
        {
            return ChangeEnumType(value, targetType);
        }

        return Convert.ChangeType(value, targetType);
    }

    private static object ChangeEnumType(object value, Type targetType)
    {
        if (value.GetType() == targetType)
        {
            return value;
        }

        var valueString = value.ToString()
            ?? throw new InvalidOperationException($"Cannot convert null string to enum type {targetType.Name}");

        return Enum.Parse(targetType, valueString, true);
    }

    /// <summary>
    /// Gets the value at the given dot-separated <paramref name="propertyPath"/> using ordinal culture comparison.
    /// </summary>
    /// <param name="target">The object to read from.</param>
    /// <param name="propertyPath">A dot-separated path to the property (e.g. <c>"Address.City"</c>).</param>
    /// <returns>The property value, or <c>null</c> if the path resolves to a null intermediate.</returns>
    public static object? GetValue(this object target, string propertyPath)
    {
        return GetValue<object>(target, propertyPath, StringComparison.InvariantCulture);
    }

    /// <summary>
    /// Gets the value at the given dot-separated <paramref name="propertyPath"/>, cast to <typeparamref name="T"/>,
    /// using invariant culture comparison.
    /// </summary>
    /// <typeparam name="T">The type to cast the result to.</typeparam>
    /// <param name="target">The object to read from.</param>
    /// <param name="propertyPath">A dot-separated path to the property (e.g. <c>"Address.City"</c>).</param>
    /// <returns>The property value cast to <typeparamref name="T"/>, or <c>default</c>.</returns>
    public static T? GetValue<T>(this object target, string propertyPath)
    {
        return GetValue<T>(target, propertyPath, StringComparison.InvariantCulture);
    }

    /// <summary>
    /// Gets the value at the given dot-separated <paramref name="propertyPath"/>, cast to <typeparamref name="T"/>,
    /// using the specified string comparison.
    /// </summary>
    /// <typeparam name="T">The type to cast the result to.</typeparam>
    /// <param name="target">The object to read from.</param>
    /// <param name="propertyPath">A dot-separated path to the property (e.g. <c>"Address.City"</c>).</param>
    /// <param name="stringComparison">The comparison to use when matching property names.</param>
    /// <returns>The property value cast to <typeparamref name="T"/>, or <c>default</c>.</returns>
    public static T? GetValue<T>(this object target, string propertyPath, StringComparison stringComparison)
    {
        if (string.IsNullOrEmpty(propertyPath))
        {
            throw new ArgumentNullException(nameof(propertyPath));
        }

        var segments = propertyPath.Split('.');
        var objectType = target.GetType().Name;
        var depth = string.Equals(objectType, segments[0], stringComparison) ? 1 : 0;

        return GetInnerValue<T>(target, segments, depth, stringComparison);

    }

    private static T? GetInnerValue<T>(object @object, string[] segments, int depth, StringComparison stringComparison)
    {
        var propertyInfos = GetCachedProperties(@object.GetType());

        foreach (var propertyInfo in propertyInfos)
        {
            if (!string.Equals(propertyInfo.Name, segments[depth], stringComparison)) continue;

            if (depth == segments.Length - 1)
            {
                var value = propertyInfo.GetValue(@object);

                return value == null ? default : (T)value;
            }

            var currentValue = propertyInfo.GetValue(@object);

            if (currentValue == null)
            {
                return default;
            }

            return GetInnerValue<T>(currentValue, segments, depth + 1, stringComparison);
        }

        throw new MissingMemberException($@"Could find property '{segments[depth]}' on {@object.GetType().Name}");
    }
}
