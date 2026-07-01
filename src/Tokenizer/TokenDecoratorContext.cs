using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens;

/// <summary>
/// Contains an instance of a <see cref="ITokenDecorator"/> that can perform
/// an operation on a <see cref="Token"/>.
/// </summary>
public sealed class TokenDecoratorContext
{
    private static readonly ConcurrentDictionary<Type, ITokenDecorator> DecoratorCache = new();

    private readonly List<string> _parameters;

    public TokenDecoratorContext(Type tokenDecorator)
    {
        DecoratorType = tokenDecorator;
        _parameters = new List<string>();
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
        return DecoratorCache.GetOrAdd(DecoratorType, type =>
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

        return instance.TryTransform(value, _parameters.ToArray(), out transformed);
    }

    /// <summary>
    /// Validates the token value.
    /// </summary>
    public bool Validate(object value)
    {
        var instance = (ITokenValidator)CreateDecorator();

        if (IsNotValidator)
        {
            return !instance.IsValid(value, _parameters.ToArray());
        }

        return instance.IsValid(value, _parameters.ToArray());
    }
}
