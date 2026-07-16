using System.Text.Json.Serialization;

namespace Tokenizer.Command;

/// <summary>
/// JSON output model for a single token match.
/// </summary>
internal sealed class MatchOutput
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
