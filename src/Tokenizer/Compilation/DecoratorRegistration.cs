namespace Tokens.Compilation;

/// <summary>
/// Pairs a decorator type with a factory that creates instances without reflection.
/// </summary>
internal readonly record struct DecoratorRegistration(Type Type, Func<ITokenDecorator> Factory);
