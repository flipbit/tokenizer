namespace Tokens.Data;

/// <summary>
/// Target POCO for medium-tier benchmarks (10-15 tokens).
/// Exercises: Trim, ToUpper, ToLower, ToDateTime, SubstringBefore,
/// SubstringAfter, Replace, IsNotEmpty, IsNumeric, IsEmail,
/// IsDomainName, IsDateTime, Contains, StartsWith.
/// </summary>
public class MediumRecord
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Count { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
}
