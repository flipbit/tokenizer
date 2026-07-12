namespace Tokens.Diagnostics;

/// <summary>
/// Holds all mutable state needed during the diagnostic build phase.
/// Passed to hint generators and issue factory to eliminate temporal coupling
/// with the immutable <see cref="TokenizationDiagnostics"/> result.
/// </summary>
internal sealed class BuildContext
{
    public string? InputContent { get; }
    public string[] InputLines { get; }
    public bool OutOfOrderTokens { get; }
    public HashSet<string> OptionalTokenNames { get; }
    public Dictionary<string, List<TokenizationEvent>> RejectionsPerToken { get; }
    public Dictionary<string, List<TokenizationEvent>> DecoratorSuccessesPerToken { get; }

    public BuildContext(string? inputContent, bool outOfOrderTokens, HashSet<string> optionalTokenNames)
    {
        InputContent = inputContent;
        InputLines = inputContent?.Split('\n') ?? Array.Empty<string>();
        OutOfOrderTokens = outOfOrderTokens;
        OptionalTokenNames = optionalTokenNames;
        RejectionsPerToken = new Dictionary<string, List<TokenizationEvent>>(StringComparer.Ordinal);
        DecoratorSuccessesPerToken = new Dictionary<string, List<TokenizationEvent>>(StringComparer.Ordinal);
    }
}
