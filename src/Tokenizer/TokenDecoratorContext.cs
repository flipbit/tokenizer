using System.Collections.Concurrent;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens;

/// <summary>
/// Contains an instance of a <see cref="ITokenDecorator"/> that can perform
/// an operation on a <see cref="Token"/>.
/// </summary>
public sealed class TokenDecoratorContext
{
    // Decorators are cached by type within a TokenParser instance: ITokenTransformer/ITokenValidator are stateless (input via params, output via return). User-registered decorators must be stateless and thread-safe.
    private readonly ConcurrentDictionary<Type, ITokenDecorator> _decoratorCache;

    private readonly List<string> _parameters;
    private string[]? _parameterArray;

    /// <summary>
    /// Creates a new <see cref="TokenDecoratorContext"/> for the specified decorator type.
    /// </summary>
    /// <param name="tokenDecorator">The <see cref="ITokenDecorator"/> type to wrap.</param>
    /// <param name="decoratorCache">The instance-scoped cache shared across all contexts for this parser.</param>
    public TokenDecoratorContext(Type tokenDecorator, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache)
    {
        DecoratorType = tokenDecorator;
        _parameters = new List<string>();
        _decoratorCache = decoratorCache;
    }

    /// <summary>
    /// Specifies the decorator type
    /// </summary>
    public Type DecoratorType { get; }

    /// <summary>
    /// Creates an instance of the decorator
    /// </summary>
    /// <returns></returns>
    public ITokenDecorator CreateDecorator()
    {
        return _decoratorCache.GetOrAdd(DecoratorType, type =>
        {
            var instance = Activator.CreateInstance(type)
                ?? throw new InvalidOperationException($"Failed to create instance of {type.Name}");
            return (ITokenDecorator)instance;
        });
    }

    /// <summary>
    /// Contains the parameters to pass the decorator
    /// </summary>
    public IReadOnlyList<string> Parameters => _parameters;

    internal void AddParameter(string parameter)
    {
        _parameters.Add(parameter);
    }

    private string[] GetParameterArray()
    {
        return _parameterArray ??= _parameters.ToArray();
    }

    /// <summary>
    /// Returns <c>true</c> if the decorator is a <see cref="ITokenTransformer"/> used to transform
    /// the token value.
    /// </summary>
    public bool IsTransformer => typeof(ITokenTransformer).IsAssignableFrom(DecoratorType);

    /// <summary>
    /// Returns <c>true</c> if the decorator is a <see cref="ITokenValidator"/> used to validate
    /// the token value.
    /// </summary>
    public bool IsValidator => typeof(ITokenValidator).IsAssignableFrom(DecoratorType);

    /// <summary>
    /// Determines if this validator should reverse it's output
    /// </summary>
    public bool IsNotValidator { get; set; }

    /// <summary>
    /// Transforms the token value.
    /// </summary>
    public bool TryTransform(object value, out object transformed)
    {
        var instance = (ITokenTransformer)CreateDecorator();

        return instance.TryTransform(value, GetParameterArray(), out transformed);
    }

    /// <summary>
    /// Validates the token value.
    /// </summary>
    public bool Validate(object value)
    {
        var instance = (ITokenValidator)CreateDecorator();

        if (IsNotValidator)
        {
            return !instance.IsValid(value, GetParameterArray());
        }

        return instance.IsValid(value, GetParameterArray());
    }
}
