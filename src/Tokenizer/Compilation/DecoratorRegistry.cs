using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens.Compilation;

/// <summary>
/// Discovers built-in transformers and validators via assembly reflection
/// and merges custom registrations from <see cref="TokenizerOptions"/>.
/// </summary>
internal sealed class DecoratorRegistry
{
    private static readonly Lazy<(Type[] transformerTypes, Type[] validatorTypes)> BuiltInTypes = new(() =>
    {
        var assembly = typeof(ITokenTransformer).Assembly;
        var types = assembly.GetTypes();

        var transformers = types
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ITokenTransformer).IsAssignableFrom(t))
            .ToArray();

        var validators = types
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ITokenValidator).IsAssignableFrom(t))
            .ToArray();

        return (transformers, validators);
    });

    public IReadOnlyList<Type> Transformers { get; }

    public IReadOnlyList<Type> Validators { get; }

    public DecoratorRegistry(TokenizerOptions options)
    {
        var (builtInTransformers, builtInValidators) = BuiltInTypes.Value;

        var transformers = new List<Type>(builtInTransformers);
        var validators = new List<Type>(builtInValidators);

        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var t in options.Transformers)
        {
            if (!transformers.Contains(t))
            {
                transformers.Add(t);
            }
        }

        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var v in options.Validators)
        {
            if (!validators.Contains(v))
            {
                validators.Add(v);
            }
        }

        Transformers = transformers.AsReadOnly();
        Validators = validators.AsReadOnly();
    }
}
