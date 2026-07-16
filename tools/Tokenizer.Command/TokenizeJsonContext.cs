using System.Text.Json.Serialization;

namespace Tokenizer.Command;

/// <summary>
/// Source-generated JSON serializer context for AOT compatibility.
/// </summary>
[JsonSerializable(typeof(TokenizeOutput))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class TokenizeJsonContext : JsonSerializerContext;
