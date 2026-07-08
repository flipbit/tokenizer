using System.Collections.ObjectModel;

namespace Tokens;

/// <summary>
/// Represents a template to use to extract data from
/// free text.
/// </summary>
public sealed class Template
{
    private readonly List<Token> _tokens;
    private readonly List<Hint> _hints;
    private readonly List<string> _tags;
    private ReadOnlyCollection<Token>? _readOnlyTokens;

    /// <summary>
    /// Creates a new template with a content-based Id and options.
    /// </summary>
    internal Template(ulong id, TokenizerOptions options)
    {
        _tokens = new List<Token>();
        _hints = new List<Hint>();
        _tags = new List<string>();
        Options = options;
        Id = id;
        Name = string.Empty;
    }

    /// <summary>
    /// Content-based identity derived from the raw pattern string hash.
    /// Two templates compiled from the same pattern string have the same Id.
    /// </summary>
    public ulong Id { get; }

    /// <summary>
    /// The name of the template.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Contains the hints associated with this <see cref="Template"/>.
    /// A <see cref="Hint"/> is used to select the best matching template by the <see cref="TemplateMatcher"/> based
    /// on text found within the input string.
    /// </summary>
    public IReadOnlyList<Hint> Hints => _hints;

    /// <summary>
    /// Contains the tags associated with this <see cref="Template"/>.
    /// A tag is used to select the best matching template by the <see cref="TemplateMatcher"/> based on tags passed
    /// in with the input string.
    /// </summary>
    public IReadOnlyList<string> Tags => _tags;

    /// <summary>
    /// The tokens contained within the template
    /// </summary>
    public IReadOnlyCollection<Token> Tokens => _readOnlyTokens ??= _tokens.AsReadOnly();

    /// <summary>
    /// Contains the <see cref="TokenizerOptions"/> used when parsing this <see cref="Template"/>.
    /// </summary>
    public TokenizerOptions Options { get; init; }

    /// <inheritdoc />
    public override string ToString()
    {
        return !string.IsNullOrEmpty(Name) ? $"Template('{Name}')" : $"Template({Tokens.Count} tokens)";
    }

    internal void AddHint(Hint hint)
    {
        _hints.Add(hint);
    }

    internal void AddTag(string tag)
    {
        _tags.Add(tag);
    }

    /// <summary>
    /// Determines if this instance contains the given tag.
    /// </summary>
    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;

        foreach (var candidate in _tags)
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
            if (!HasTag(tag))
            {
                missing.Add(tag);
            }
        }

        return missing.Count == 0;
    }

    internal bool HasOnlyFrontMatterTokens
    {
        get
        {
            foreach (var token in _tokens)
            {
                if (!string.IsNullOrWhiteSpace(token.Name) && !token.IsFrontMatterToken)
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal void GetTokenIdsUpTo(Token token, HashSet<int> matchIds)
    {
        // Only remove match if out-of-order token
        if (Options.OutOfOrderTokens)
        {
            if (!token.IsRepeating) matchIds.Add(token.Id);
            return;
        }

        foreach (var candidate in _tokens)
        {
            if (candidate == token)
            {
                if (!candidate.IsRepeating)
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
        token.Id = _tokens.Count + 1;
        _tokens.Add(token);
        _readOnlyTokens = null;
    }

    internal IEnumerable<Token> TokensExcluding(HashSet<int> excludedIds, List<Token> buffer, HashSet<int> idBuffer)
    {
        buffer.Clear();
        idBuffer.Clear();

        foreach (var token in _tokens)
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
