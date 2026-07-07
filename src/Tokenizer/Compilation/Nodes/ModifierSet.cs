namespace Tokens.Compilation.Nodes;

/// <summary>
/// Represents the set of modifier flags that can be applied to a token.
/// </summary>
/// <param name="IsOptional">Whether the token is optional (may not match).</param>
/// <param name="IsRepeating">Whether the token can repeat across multiple input lines.</param>
/// <param name="IsRequired">Whether the token must produce a value for the template to match.</param>
/// <param name="IsTerminate">Whether matching this token terminates further processing.</param>
public sealed record ModifierSet(bool IsOptional, bool IsRepeating, bool IsRequired, bool IsTerminate);
