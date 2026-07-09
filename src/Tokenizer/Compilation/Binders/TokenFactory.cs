using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Tokens.Extensions;

namespace Tokens.Compilation.Binders;

/// <summary>
/// Creates Token instances from TokenDefinitions. Owns preamble computation logic.
/// </summary>
internal static class TokenFactory
{
    public static Token Create(TokenDefinition definition, TokenizerOptions options, IDiagnosticCollector collector)
    {
        var preamble = ComputePreamble(definition, options);
        var location = definition.Location ?? new Enumerators.FileLocation();
        var token = new Token(definition.Name ?? string.Empty, preamble, location);

        token.IsOptional = definition.IsOptional;
        token.IsRepeating = definition.IsRepeating;
        token.TerminateOnNewLine = definition.TerminateOnNewLine;
        token.IsRequired = definition.IsRequired;
        token.DependsOnId = definition.DependsOnId;
        token.IsFrontMatterToken = definition.IsFrontMatterToken;
        token.IsNull = definition.IsNull;
        token.IsSingleUse = definition.IsSingleUse;

        if (collector.IsEnabled)
        {
            collector.Record(DiagnosticEventType.TokenCreated,
                tokenName: token.Name,
                tokenId: definition.Id,
                detail: $"Content={definition.Content}, Optional={token.IsOptional}, Repeating={token.IsRepeating}");
        }

        return token;
    }

    private static string ComputePreamble(TokenDefinition definition, TokenizerOptions options)
    {
        string preamble;

        if (options.TrimLeadingWhitespaceInTokenPreamble)
        {
            if (definition.Preamble.IsOnlySpaces())
            {
                preamble = definition.Preamble;
            }
            else if (string.IsNullOrWhiteSpace(definition.Preamble))
            {
                preamble = definition.Preamble.TrimLeadingSpaces();
            }
            else
            {
                preamble = definition.Preamble.TrimStart();
            }
        }
        else
        {
            preamble = definition.Preamble;
        }

#pragma warning disable MA0001 // IndexOf(char) is inherently ordinal; no StringComparison overload exists
        if (options.TrimPreambleBeforeNewLine &&
            !string.IsNullOrEmpty(preamble) && preamble.IndexOf('\n') > -1)
        {
            var idx = preamble.LastIndexOf('\n');
            preamble = preamble.Substring(idx + 1);
        }
#pragma warning restore MA0001

        return preamble;
    }
}
