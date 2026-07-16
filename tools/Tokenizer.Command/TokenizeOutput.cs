using System.Text.Json.Serialization;

namespace Tokenizer.Command;

/// <summary>
/// JSON output model for a tokenization result.
/// </summary>
internal sealed class TokenizeOutput
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("matches")]
    public MatchOutput[] Matches { get; init; } = Array.Empty<MatchOutput>();

    [JsonPropertyName("diagnostics")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Diagnostics { get; init; }
}
