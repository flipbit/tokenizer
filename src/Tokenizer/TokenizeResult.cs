using Tokens.Exceptions;

namespace Tokens;

/// <summary>
/// Holds the result of attempting to parse an input string against a
/// <see cref="Template"/>.
/// </summary>
public sealed class TokenizeResult : TokenizeResultBase
{
    /// <summary>
    ///  Creates a new instance of the <see cref="TokenizeResult"/> class.
    /// </summary>
    public TokenizeResult(Template template) : base(template)
    {
    }

    /// <summary>
    /// A dictionary of values extracted from the input string.
    /// </summary>
    public IReadOnlyList<TokenMatch> Matches => Tokens.Matches;

    /// <summary>
    /// Returns the value of the first matched token with the given <paramref name="key"/>.
    /// Throws if no match is found.
    /// </summary>
    /// <param name="key">The token name to look up.</param>
    /// <returns>The matched value.</returns>
    /// <exception cref="Exceptions.TokenizerException">Thrown when no token with the given key was matched.</exception>
    public object First(string key)
    {
        if (!Matches.Any(m => m.Token.Name == key))
        {
            throw new TokenizerException($"Token '{key}' was not found in the input text.");
        }

        return Matches.First(m => m.Token.Name == key).Value;
    }

    /// <summary>
    /// Returns the value of the first matched token with the given <paramref name="key"/>, cast to <typeparamref name="T"/>.
    /// Throws if no match is found.
    /// </summary>
    /// <typeparam name="T">The type to cast the matched value to.</typeparam>
    /// <param name="key">The token name to look up.</param>
    /// <returns>The matched value cast to <typeparamref name="T"/>.</returns>
    /// <exception cref="Exceptions.TokenizerException">Thrown when no token with the given key was matched.</exception>
    public T First<T>(string key)
    {
        if (!Matches.Any(m => m.Token.Name == key))
        {
            throw new TokenizerException($"Token '{key}' was not found in the input text.");
        }

        return (T)Matches.First(m => m.Token.Name == key).Value;
    }

    /// <summary>
    /// Returns the value of the first matched token with the given <paramref name="key"/>,
    /// or <c>null</c> if no match was found.
    /// </summary>
    /// <param name="key">The token name to look up.</param>
    /// <returns>The matched value, or <c>null</c>.</returns>
    public object? FirstOrDefault(string key)
    {
        if (Matches.Any(m => m.Token?.Name == key))
        {
            return Matches.First(m => m.Token.Name == key).Value;
        }

        return null;
    }

    /// <summary>
    /// Returns the value of the first matched token with the given <paramref name="key"/> cast to <typeparamref name="T"/>,
    /// or the default value of <typeparamref name="T"/> if no match was found.
    /// </summary>
    /// <typeparam name="T">The type to cast the matched value to.</typeparam>
    /// <param name="key">The token name to look up.</param>
    /// <returns>The matched value cast to <typeparamref name="T"/>, or <c>default</c>.</returns>
    public T? FirstOrDefault<T>(string key)
    {
        if (Matches.Any(m => m.Token?.Name == key))
        {
            return (T)Matches.First(m => m.Token.Name == key).Value;
        }

        return default;
    }

    /// <summary>
    /// Returns all matched values for the token with the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The token name to look up.</param>
    /// <returns>A read-only list of all matched values for the token.</returns>
    public IReadOnlyList<object> All(string key)
    {
        return Matches
            .Where(m => m.Token.Name == key)
            .Select(m => m.Value)
            .ToList();
    }

    /// <summary>
    /// Determines whether a token with the given <paramref name="key"/> was matched in the input.
    /// </summary>
    /// <param name="key">The token name to look up.</param>
    /// <returns><c>true</c> if at least one match exists for the key; otherwise <c>false</c>.</returns>
    public bool Contains(string key)
    {
        return Matches.Any(m => m.Token.Name == key);
    }
}

/// <summary>
/// Holds the result of attempting to parse an input string against a
/// <see cref="Template"/> to generate an object of type <typeparamref name="T"/>.
/// </summary>
public sealed class TokenizeResult<T> : TokenizeResultBase where T : class, new()
{
    /// <summary>
    ///  Creates a new instance of the <see cref="TokenizeResult{T}"/> class.
    /// </summary>
    public TokenizeResult(Template template) : base(template)
    {
        Value = new T();
    }

    /// <summary>
    /// An instance of <typeparamref name="T"/> populated with data from the input string.
    /// </summary>
    public T Value { get; init; }
}
