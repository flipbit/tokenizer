namespace Tokens.Data;

/// <summary>
/// Target POCO for small-tier benchmarks (3-5 tokens).
/// Exercises: Trim, IsNotEmpty, ToUpper.
/// </summary>
public class SmallRecord
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
