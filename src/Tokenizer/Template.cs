using Tokens.Extensions;

namespace Tokens;

/// <summary>
/// Represents a template to use to extract data from
/// free text.
/// </summary>
public sealed class Template
{
    private static int templateCounter;

    private readonly List<Token> tokens;
    private readonly List<Hint> hints;
    private readonly List<string> tags;
    private string name;

    /// <summary>
    /// Creates a new unnamed template.
    /// </summary>
    public Template() : this(string.Empty)
    {
    }

    /// <summary>
    /// Creates a new template with the given name.
    /// </summary>
    /// <param name="name">A name that identifies this template.</param>
    public Template(string name) : this(name, new TokenizerOptions())
    {
    }

    /// <summary>
    /// Creates a new template with the given name and options.
    /// </summary>
    /// <param name="name">A name that identifies this template.</param>
    /// <param name="options">The options to use when parsing this template.</param>
    public Template(string name, TokenizerOptions options)
    {
        tokens = new List<Token>();
        hints = new List<Hint>();
        tags = new List<string>();
        Options = options;
        this.name = name;
    }

    /// <summary>
    /// Creates a new template with a content-based Id, name, and options.
    /// </summary>
    internal Template(string pattern, string name, TokenizerOptions options)
    {
        tokens = new List<Token>();
        hints = new List<Hint>();
        tags = new List<string>();
        Options = options;
        this.name = name;
        Id = pattern.ComputeHash();
    }

    /// <summary>
    /// Content-based identity derived from the raw pattern string hash.
    /// Two templates compiled from the same pattern string have the same Id.
    /// </summary>
    public ulong Id { get; }

    /// <summary>
    /// The name of the template. If no name is specified, a unique name is auto-generated.
    /// </summary>
    public string Name
    {
        get
        {
            if (string.IsNullOrEmpty(name))
            {
                name = $"Template_{Interlocked.Increment(ref templateCounter)}";
            }

            return name;
        }
        set => name = value;
    }

    /// <summary>
    /// Contains the hints associated with this <see cref="Template"/>.
    /// A <see cref="Hint"/> is used to select the best matching template by the <see cref="TokenMatcher"/> based
    /// on text found within the input string.
    /// </summary>
    public IReadOnlyList<Hint> Hints => hints;

    /// <summary>
    /// Contains the tags associated with this <see cref="Template"/>.
    /// A tag is used to select the best matching template by the <see cref="TokenMatcher"/> based on tags passed
    /// in with the input string.
    /// </summary>
    public IReadOnlyList<string> Tags => tags;

    /// <summary>
    /// The tokens contained within the template
    /// </summary>
    public IReadOnlyCollection<Token> Tokens => tokens.AsReadOnly();

    /// <summary>
    /// Contains the <see cref="TokenizerOptions"/> used when parsing this <see cref="Template"/>.
    /// </summary>
    public TokenizerOptions Options { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        return !string.IsNullOrEmpty(name) ? $"Template('{name}')" : $"Template({Tokens.Count} tokens)";
    }

    internal void AddHint(Hint hint)
    {
        hints.Add(hint);
    }

    internal void AddTag(string tag)
    {
        tags.Add(tag);
    }

    /// <summary>
    /// Determines if this instance contains the given tag.
    /// </summary>
    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;

        foreach (var candidate in tags)
        {
            if (string.Equals(candidate, tag, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if this instance contains all of the given tags.
    /// </summary>
    public bool HasTags(IList<string> tags)
    {
        if (tags == null) return false;

        foreach (var tag in tags)
        {
            if (!HasTag(tag)) return false;
        }

        return true;
    }

    /// <summary>
    /// Determines if this instance contains all of the given tags.
    /// </summary>
    public bool HasTags(IList<string> tags, out IList<string> missing)
    {
        missing = new List<string>();

        if (tags == null)
        {
            return false;
        }

        foreach (var tag in tags)
        {
            if (HasTag(tag) == false)
            {
                missing.Add(tag);
            }
        }

        return missing.Count == 0;
    }

    internal bool HasOnlyFrontMatterTokens => tokens.Where(t => !string.IsNullOrWhiteSpace(t.Name)).All(t => t.IsFrontMatterToken);

    internal void GetTokenIdsUpTo(Token token, HashSet<int> matchIds)
    {
        // Only remove match if out-of-order token
        if (Options.OutOfOrderTokens)
        {
            if (token.IsRepeating == false) matchIds.Add(token.Id);
            return;
        }

        foreach (var candidate in tokens)
        {
            if (candidate == token)
            {
                if (candidate.IsRepeating == false)
                {
                    matchIds.Add(candidate.Id);
                }
                break;
            }

            matchIds.Add(candidate.Id);
        }
    }

    internal void AddToken(Token token)
    {
        token.Id = tokens.Count + 1;
        tokens.Add(token);
    }

    internal IEnumerable<Token> TokensExcluding(HashSet<int> excludedIds, List<Token> buffer, HashSet<int> idBuffer)
    {
        buffer.Clear();
        idBuffer.Clear();

        foreach (var token in tokens)
        {
            if (token.IsFrontMatterToken) continue;
            if (excludedIds.Contains(token.Id)) continue;
            buffer.Add(token);
            idBuffer.Add(token.Id);
        }

        buffer.RemoveAll(t => idBuffer.Contains(t.DependsOnId));

        return buffer;
    }

    internal IEnumerable<Token> TokensExcluding(HashSet<int> matchIds, CandidateTokenList candidates, HashSet<int> excludedRepeatingTokens, HashSet<int> exclusionBuffer, List<Token> tokenBuffer, HashSet<int> idBuffer)
    {
        exclusionBuffer.Clear();
        foreach (var id in matchIds) exclusionBuffer.Add(id);
        foreach (var token in candidates.Tokens)
        {
            if (!token.IsRepeating) exclusionBuffer.Add(token.Id);
        }
        foreach (var id in excludedRepeatingTokens) exclusionBuffer.Add(id);

        return TokensExcluding(exclusionBuffer, tokenBuffer, idBuffer);
    }
}
