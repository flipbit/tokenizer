using System;
using System.Collections.Generic;
using System.Linq;

namespace Tokens.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Sets the given value on the given propetrty with the given path.
        /// </summary>
        public static T SetValue<T>(this T @object, string propertyPath, object value) where T : class
        {
            var segments = propertyPath.Split('.');
            var objectType = @object.GetType().Name;

            // Must have at least a single property (e.g. "Object.Property")
            if (segments.Length < 2) throw new ArgumentException("Property Path Too Short: " + propertyPath);

            // Check object type
            if (objectType != segments[0]) throw new ArgumentException($"Invalid Property Path for {objectType}: {propertyPath}");

            @object = SetInnerValue(@object, segments.Skip(1).ToArray(), value) as T;

            return @object;
        }

        private static object SetInnerValue(object @object, IReadOnlyList<string> path, object value)
        {
            var propertyInfos = @object.GetType().GetProperties();

            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.Name != path[0]) continue;

                if (path.Count == 1)
                {
                    if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
                    {
                        var list = propertyInfo.GetValue(@object, null);

                        if (list == null)
                        {
                            var genericType = propertyInfo.PropertyType.GetGenericArguments()[0];
                            var enumerableType = typeof(List<>);
                            var constructedEnumerableType = enumerableType.MakeGenericType(genericType);
                            list = Activator.CreateInstance(constructedEnumerableType);

                            propertyInfo.SetValue(@object, list, null);
                        }

                        list.GetType().GetMethod("Add").Invoke(list, new[] { value });
                    }
                    else
                    {
                        var convertedValue = Convert.ChangeType(value, propertyInfo.PropertyType);

                        propertyInfo.SetValue(@object, convertedValue, null);
                    }

                    break;
                }

                var currentValue = propertyInfo.GetValue(@object, null);

                if (currentValue == null)
                {
                    try
                    {
                        currentValue = Activator.CreateInstance(propertyInfo.PropertyType);
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException("Could not create type: " + propertyInfo.PropertyType, ex);
                    }

                    propertyInfo.SetValue(@object, currentValue, null);
                }

                SetInnerValue(currentValue, path.Skip(1).ToArray(), value);

                break;
            }

            return @object;
        }
    }
}
