namespace Tokens;

/// <summary>
/// Contains the result of running a match against multiple <see cref="Template"/>
/// objects against an input string with the <see cref="TokenMatcher"/>. 
/// </summary>
public sealed class TokenMatcherResult
{
    private readonly List<TokenizeResult> _results;

    /// <summary>
    /// Initializes a new, empty <see cref="TokenMatcherResult"/>.
    /// </summary>
    public TokenMatcherResult()
    {
        _results = new List<TokenizeResult>();
    }

    /// <summary>
    /// Contains the result of processing each <see cref="Template"/> against the input text.
    /// </summary>
    public IReadOnlyList<TokenizeResult> Results => _results;

    /// <summary>
    /// Returns the best matching result
    /// </summary>
    public TokenizeResult? BestMatch { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether any template produced a successful match.
    /// </summary>
    public bool Success => BestMatch != null;

    internal void AddResult(TokenizeResult result)
    {
        _results.Add(result);
    }

    internal TokenizeResult? GetBestMatch() => _results
        .Where(r => r.Success)
        .OrderByDescending(r => r.Hints.Matches.Count)
        .ThenByDescending(r => r.Tokens.Matches.Count)
        .ThenBy(r => r.Template.Tokens.Count)
        .ThenBy(r => r.Template.Id)
        .FirstOrDefault();
}

/// <summary>
/// Contains the result of running a match against multiple <see cref="Template"/>
/// objects against an input string with the <see cref="TokenMatcher"/>, where each
/// successful result is bound to a new instance of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type that matched tokens are reflected onto.</typeparam>
public sealed class TokenMatcherResult<T> where T : class, new()
{
    private readonly List<TokenizeResult<T>> _results;

    /// <summary>
    /// Initializes a new, empty <see cref="TokenMatcherResult{T}"/>.
    /// </summary>
    public TokenMatcherResult()
    {
        _results = new List<TokenizeResult<T>>();
    }

    /// <summary>
    /// Contains the result of processing each <see cref="Template"/> against the input text.
    /// </summary>
    public IReadOnlyList<TokenizeResult<T>> Results => _results;

    /// <summary>
    /// Returns the best matching result
    /// </summary>
    public TokenizeResult<T>? BestMatch { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether any template produced a successful match.
    /// </summary>
    public bool Success => BestMatch != null;

    internal void AddResult(TokenizeResult<T> result)
    {
        _results.Add(result);
    }

    internal TokenizeResult<T>? GetBestMatch() => _results
        .Where(r => r.Success)
        .OrderByDescending(r => r.Hints.Matches.Count)
        .ThenByDescending(r => r.Tokens.Matches.Count)
        .ThenBy(r => r.Template.Tokens.Count)
        .ThenBy(r => r.Template.Id)
        .FirstOrDefault();
}
