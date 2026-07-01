using System;
using System.Collections.Generic;
using System.Linq;

namespace Tokens;

/// <summary>
/// Base class that holds the result of attempting to parse an input string against a
/// <see cref="Template"/>.
/// </summary>
public class TokenizeResultBase
{
    /// <summary>
    ///  Creates a new instance of the <see cref="TokenizeResultBase"/> class.
    /// </summary>
    private readonly List<Exception> _exceptions;

    public TokenizeResultBase(Template template)
    {
        _exceptions = new List<Exception>();

        Hints = new HintResult();
        Tokens = new TokenResult();

        Template = template;
    }

    /// <summary>
    /// The <see cref="Template"/> containing the mapping between tokens in the
    /// <see cref="Template"/> and properties on the object <see cref="T"/>.
    /// </summary>
    public Template Template { get; init; }

    /// <summary>
    /// A list of any exceptions that occurred during the matching process
    /// </summary>
    public IReadOnlyList<Exception> Exceptions => _exceptions;

    /// <summary>
    /// The matches that where made during the tokenization process
    /// </summary>
    public TokenResult Tokens { get; init; }

    /// <summary>
    /// Gets the hints found in the input
    /// </summary>
    public HintResult Hints { get; init; }

    internal void AddException(Exception exception)
    {
        _exceptions.Add(exception);
    }

    /// <summary>
    /// Structured diagnostic output from the tokenization process.
    /// Null when <see cref="TokenizerOptions.EnableDiagnostics"/> is false.
    /// </summary>
    public Diagnostics.TokenizationDiagnostics? Diagnostics { get; internal set; }

    /// <summary>
    /// Determines whether the matching process was successful
    /// </summary>
    public bool Success => Tokens.HasMatches &&
                           Tokens.HasMissingRequiredTokens == false &&
                           Hints.HasMissingRequiredHints == false &&
                           (Template.HasOnlyFrontMatterTokens || Tokens.Matches.Any(m => !m.Token.IsFrontMatterToken));
}
