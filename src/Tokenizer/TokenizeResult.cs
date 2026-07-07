using Tokens.Exceptions;
using Tokens.Extensions;

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
        foreach (var m in Matches)
        {
            if (string.Equals(m.Token.Name, key, StringComparison.Ordinal))
                return m.Value;
        }

        throw new TokenizerException($"Token '{key}' was not found in the input text.");
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
        foreach (var m in Matches)
        {
            if (string.Equals(m.Token.Name, key, StringComparison.Ordinal))
                return (T)m.Value;
        }

        throw new TokenizerException($"Token '{key}' was not found in the input text.");
    }

    /// <summary>
    /// Returns the value of the first matched token with the given <paramref name="key"/>,
    /// or <see langword="null"/> if no match was found.
    /// </summary>
    /// <param name="key">The token name to look up.</param>
    /// <returns>The matched value, or <see langword="null"/>.</returns>
    public object? FirstOrDefault(string key)
    {
        foreach (var m in Matches)
        {
            if (string.Equals(m.Token?.Name, key, StringComparison.Ordinal))
                return m.Value;
        }

        return null;
    }

    /// <summary>
    /// Returns the value of the first matched token with the given <paramref name="key"/> cast to <typeparamref name="T"/>,
    /// or the default value of <typeparamref name="T"/> if no match was found.
    /// </summary>
    /// <typeparam name="T">The type to cast the matched value to.</typeparam>
    /// <param name="key">The token name to look up.</param>
    /// <returns>The matched value cast to <typeparamref name="T"/>, or <see langword="default"/>.</returns>
    public T? FirstOrDefault<T>(string key)
    {
        foreach (var m in Matches)
        {
            if (string.Equals(m.Token?.Name, key, StringComparison.Ordinal))
                return (T)m.Value;
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
            .Where(m => string.Equals(m.Token.Name, key, StringComparison.Ordinal))
            .Select(m => m.Value)
            .ToList();
    }

    /// <summary>
    /// Determines whether a token with the given <paramref name="key"/> was matched in the input.
    /// </summary>
    /// <param name="key">The token name to look up.</param>
    /// <returns><see langword="true"/> if at least one match exists for the key; otherwise <see langword="false"/>.</returns>
    public bool Contains(string key)
    {
        return Matches.Any(m => string.Equals(m.Token.Name, key, StringComparison.Ordinal));
    }

    /// <summary>
    /// Projects this result onto a new instance of <typeparamref name="T"/>,
    /// assigning matched values to the object's properties via reflection.
    /// The original result is not modified.
    /// </summary>
    /// <typeparam name="T">The type to populate with matched values.</typeparam>
    /// <returns>A new <see cref="TokenizeResult{T}"/> with the populated object.</returns>
    public TokenizeResult<T> Assign<T>() where T : class, new()
    {
        var typed = new TokenizeResult<T>(Template, Tokens, Hints, Diagnostics);
        var target = typed.Value;
        var options = Template.Options;

        if (target is IDictionary<string, object> dictionary)
        {
            AssignToDictionary(dictionary, typed);
        }
        else
        {
            AssignToObject(target, options, typed);
        }

        return typed;
    }

    private static void AssignToDictionary(IDictionary<string, object> dictionary, TokenizeResultBase typed)
    {
        foreach (var match in typed.Tokens.Matches)
        {
            if (match.Token.IsRepeating)
            {
                List<object> list;
                if (dictionary.ContainsKey(match.Token.Name))
                {
                    list = dictionary[match.Token.Name] as List<object> ?? new List<object> { dictionary[match.Token.Name] };
                }
                else
                {
                    list = new List<object>();
                }
                list.Add(match.Value);
                dictionary[match.Token.Name] = list;
            }
            else if (dictionary.ContainsKey(match.Token.Name))
            {
                dictionary[match.Token.Name] = match.Value;
            }
            else
            {
                dictionary.Add(match.Token.Name, match.Value);
            }
        }
    }

    private static void AssignToObject(object target, TokenizerOptions options, TokenizeResultBase typed)
    {
        foreach (var match in typed.Tokens.Matches)
        {
            try
            {
                target.SetValue(match.Token.Name, match.Value, StringComparison.Ordinal);
            }
            catch (MissingMemberException)
            {
                if (!options.IgnoreMissingProperties)
                {
                    typed.AddException(new MissingMemberException(
                        $"Property '{match.Token.Name}' not found on type '{target.GetType().Name}'."));
                }
            }
            catch (TypeConversionException ex)
            {
                typed.AddException(ex);
            }
            catch (TokenAssignmentException ex)
            {
                typed.AddException(ex);
            }
            catch (ArgumentException ex)
            {
                typed.AddException(ex);
            }
        }
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
    /// Creates a projected result carrying forward matching state from a completed tokenization.
    /// Stage 1 exceptions are not copied — only assignment exceptions belong on typed results.
    /// </summary>
    internal TokenizeResult(Template template, TokenResult tokens, HintResult hints, Diagnostics.DiagnosticResult? diagnostics)
        : base(template, tokens, hints, diagnostics)
    {
        Value = new T();
    }

    /// <summary>
    /// An instance of <typeparamref name="T"/> populated with data from the input string.
    /// </summary>
    public T Value { get; init; }

    /// <summary>
    /// True when matching succeeded and no assignment errors occurred.
    /// </summary>
    public override bool Success => base.Success && Exceptions.Count == 0;
}
