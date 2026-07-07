using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens.Compilation;

/// <summary>
/// Discovers built-in transformers and validators via assembly reflection
/// and merges custom registrations from <see cref="TokenizerOptions"/>.
/// </summary>
internal sealed class DecoratorRegistry
{
    public IReadOnlyList<Type> Transformers { get; }

    public IReadOnlyList<Type> Validators { get; }

    public DecoratorRegistry(TokenizerOptions options)
    {
        var assembly = typeof(ITokenTransformer).Assembly;

        var transformers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ITokenTransformer).IsAssignableFrom(t))
            .ToList();

        var validators = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ITokenValidator).IsAssignableFrom(t))
            .ToList();

        foreach (var t in options.Transformers)
        {
            if (!transformers.Contains(t))
            {
                transformers.Add(t);
            }
        }

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
