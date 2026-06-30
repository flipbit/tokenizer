using Tokens.Enumerators;

namespace Tokens.Diagnostics
{
    /// <summary>
    /// Active diagnostic collector that records all events during a tokenization call.
    /// Create one instance per tokenization call and pass it through the pipeline.
    /// </summary>
    internal sealed class DiagnosticCollector : IDiagnosticCollector
    {
        private readonly TokenizationDiagnostics diagnostics;

        /// <summary>
        /// Initialises a new collector for a single tokenization call.
        /// </summary>
        /// <param name="templateContent">The template pattern being used.</param>
        /// <param name="inputContent">The input text being tokenized.</param>
        public DiagnosticCollector(string templateContent, string inputContent)
        {
            diagnostics = new TokenizationDiagnostics(templateContent, inputContent);
        }

        /// <inheritdoc />
        public void Record(DiagnosticEventType type, string? tokenName = null, int? tokenId = null,
                           FileLocation? location = null, string? value = null, string? detail = null,
                           string? decoratorName = null, string[]? decoratorArgs = null)
        {
            var evt = new DiagnosticEvent
            {
                Type = type,
                TokenName = tokenName,
                TokenId = tokenId,
                Location = location?.Clone(),
                Value = value,
                Detail = detail,
                DecoratorName = decoratorName,
                DecoratorArgs = decoratorArgs,
            };

            diagnostics.Events.Add(evt);
        }

        /// <inheritdoc />
        public TokenizationDiagnostics? GetResult()
        {
            return diagnostics;
        }
    }
}
