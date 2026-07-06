using Microsoft.Extensions.Logging;

namespace Tokens.Tokenization;

internal static class InputValidator
{
    public static void ValidateTargetObject(object? targetObject, ILogger logger)
    {
        if (targetObject == null || targetObject is System.Collections.Generic.IDictionary<string, object>)
        {
            return;
        }

        var properties = targetObject.GetType().GetProperties();
        var hasSettableProperty = properties.Any(p => p.CanWrite && p.GetSetMethod() != null);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Target object type: {TypeName}, Properties: {PropertyCount}, Settable: {SettableCount}",
                targetObject.GetType().Name,
                properties.Length,
                properties.Count(p => p.CanWrite && p.GetSetMethod() != null));
        }

        if (!hasSettableProperty)
        {
            throw new ArgumentException(
                $"Target object of type '{targetObject.GetType().Name}' has no settable properties. " +
                "Anonymous types and objects with read-only properties cannot be used as tokenization targets. " +
                "Consider using a class with writable properties or passing null as the target.",
                nameof(targetObject));
        }
    }
}
