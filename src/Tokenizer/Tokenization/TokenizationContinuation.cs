using Tokens.Diagnostics;

namespace Tokens.Tokenization;

/// <summary>
/// A typed handle returned by <see cref="ITokenizationEngine.BeginTokenization"/> that
/// captures the state needed by <see cref="ITokenizationEngine.ContinueTokenization"/>
/// and <see cref="ITokenizationEngine.EndTokenization"/>. Enforces correct call ordering
/// at compile time — you cannot call Continue/End without first calling Begin.
/// </summary>
internal sealed class TokenizationContinuation
{
    public Template Template { get; }
    public object? TargetObject { get; }
    public TokenizeResultBase Result { get; }
    public IDiagnosticCollector Collector { get; }
    public IHintStrategy? HintStrategy { get; }
    public bool HasExplicitLimit { get; }
    public int IterationCount { get; set; }

    public TokenizationContinuation(
        Template template,
        object? targetObject,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy,
        bool hasExplicitLimit)
    {
        Template = template;
        TargetObject = targetObject;
        Result = result;
        Collector = collector;
        HintStrategy = hintStrategy;
        HasExplicitLimit = hasExplicitLimit;
    }
}
