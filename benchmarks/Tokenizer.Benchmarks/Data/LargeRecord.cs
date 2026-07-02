namespace Tokens.Data;

/// <summary>
/// Target POCO for large-tier benchmarks (30-50 tokens).
/// Exercises all transformers and validators.
/// </summary>
public class LargeRecord
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string LooseUrl { get; set; } = string.Empty;
    public string AbsoluteUrl { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Count { get; set; } = string.Empty;
    public string Total { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Rating { get; set; } = string.Empty;
    public string Score { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public object Keywords { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
    public string Found { get; set; } = string.Empty;
}
