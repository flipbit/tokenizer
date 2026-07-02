using Tokens.Enumerators;

namespace Tokens;

/// <summary>
/// Represents a <see cref="Token"/> match in a <see cref="Template"/>
/// </summary>
public sealed record TokenMatch(Token Token, object Value, FileLocation Location)
{
    /// <inheritdoc />
    public override string ToString() => $"TokenMatch('{Token.Name}' = '{Value}' @ {Location})";
}
