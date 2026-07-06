namespace Tokens.Data;

/// <summary>
/// Generates synthetic template patterns and matching input text at three
/// workload tiers for benchmarking. All templates compile successfully and
/// all inputs produce successful tokenization with all validators passing.
/// </summary>
public static class WorkloadGenerator
{
    // ── Small tier: 3 tokens ──────────────────────────────────────────

    /// <summary>
    /// Small template: 3 tokens exercising Trim, IsNotEmpty, ToUpper.
    /// </summary>
    public static string SmallTemplate() =>
        """
        Name: { SmallRecord.Name : Trim, IsNotEmpty }
        Status: { SmallRecord.Status : ToUpper }
        Code: { SmallRecord.Code : Trim }
        """;

    /// <summary>
    /// Input text that matches <see cref="SmallTemplate"/>.
    /// </summary>
    public static string SmallInput() =>
        """
        Name: Alice Johnson
        Status: active
        Code: ABC-123
        """;

    // ── Medium tier: 12 tokens ────────────────────────────────────────

    /// <summary>
    /// Medium template: 12 tokens exercising Trim, ToUpper, ToLower,
    /// ToDateTime, SubstringBefore, SubstringAfter, Replace,
    /// IsNotEmpty, IsNumeric, IsEmail, IsDomainName, IsDateTime,
    /// Contains, StartsWith.
    /// </summary>
    public static string MediumTemplate() =>
        """
        Name: { MediumRecord.Name : Trim, IsNotEmpty }
        Email: { MediumRecord.Email : ToLower, IsEmail }
        Domain: { MediumRecord.Domain : ToLower, IsDomainName }
        Code: { MediumRecord.Code : ToUpper, StartsWith('R') }
        Count: { MediumRecord.Count : Trim, IsNumeric }
        Created: { MediumRecord.Created : Trim, ToDateTime('yyyy-MM-dd'), IsDateTime }
        Status: { MediumRecord.Status : ToUpper, Contains('ACT') }
        Description: { MediumRecord.Description : SubstringBefore('.') }
        Category: { MediumRecord.Category : SubstringAfter('-') }
        Reference: { MediumRecord.Reference : Replace('REF', 'R') }
        Tag: { MediumRecord.Tag : Trim }
        Origin: { MediumRecord.Origin : Trim }
        """;

    /// <summary>
    /// Input text that matches <see cref="MediumTemplate"/>.
    /// </summary>
    public static string MediumInput() =>
        """
        Name: Bob Smith
        Email: BOB@EXAMPLE.COM
        Domain: EXAMPLE.COM
        Code: ref-42
        Count: 12345
        Created: 2024-06-15
        Status: active
        Description: This is a test record. Extra text here.
        Category: type-electronics
        Reference: REF-001
        Tag: benchmark
        Origin: synthetic
        """;

    // ── Large tier: 39 tokens including repeating, front matter ──────

    /// <summary>
    /// Large template: 39 tokens exercising every transformer and validator.
    /// Includes front matter with name, tags, hints, and set directive.
    /// Includes repeating tokens.
    /// </summary>
    public static string LargeTemplate() =>
        """
        ---
        name: large-benchmark-template
        tag: benchmark
        tag: performance
        hint: Record Entry
        set: Found = Yes
        terminateOnNewLine: true
        ---

        Record Entry

        Name: { LargeRecord.Name : Trim, IsNotEmpty, MinLength(2) }
        Email: { LargeRecord.Email : ToLower, IsEmail }
        Phone: { LargeRecord.Phone : Trim, IsPhoneNumber }
        Domain: { LargeRecord.Domain : ToLower, IsDomainName }
        URL: { LargeRecord.Url : Trim, IsUrl }
        Loose URL: { LargeRecord.LooseUrl : Trim, IsLooseUrl }
        Absolute URL: { LargeRecord.AbsoluteUrl : Trim, IsLooseAbsoluteUrl }
        Code: { LargeRecord.Code : ToUpper, StartsWith('R'), MaxLength(20) }
        Count: { LargeRecord.Count : Trim, IsNumeric }
        Total: { LargeRecord.Total : Trim, IsNumeric, IsNot('0') }
        Created: { LargeRecord.Created : Trim, ToDateTime('yyyy-MM-dd'), IsDateTime }
        Updated: { LargeRecord.Updated : Trim, ToDateTimeUtc('yyyy-MM-dd') }
        Status: { LargeRecord.Status : ToUpper, Contains('ACT') }
        Type: { LargeRecord.Type : Trim, EndsWith('ry') }
        Description: { LargeRecord.Description : SubstringBefore('.'), MinLength(5) }
        Summary: { LargeRecord.Summary : SubstringAfter(':') }
        Notes: { LargeRecord.Notes : SubstringBeforeLast('.') }
        Category: { LargeRecord.Category : SubstringAfterLast('-') }
        SubCategory: { LargeRecord.SubCategory : Remove('#') }
        Reference: { LargeRecord.Reference : RemoveStart('REF-') }
        Identifier: { LargeRecord.Identifier : RemoveEnd('-ID') }
        Tag: { LargeRecord.Tag : Replace('_', '-') }
        Label: { LargeRecord.Label : Trim, IsNotEmpty }
        Origin: { LargeRecord.Origin : Trim, Contains('syn') }
        Source: { LargeRecord.Source : Trim }
        Target: { LargeRecord.Target : Trim }
        Priority: { LargeRecord.Priority : ToUpper }
        Level: { LargeRecord.Level : ToLower }
        Rating: { LargeRecord.Rating : Trim, IsNumeric }
        Score: { LargeRecord.Score : Trim, IsNumeric }
        Version: { LargeRecord.Version : Trim }
        Region: { LargeRecord.Region : Trim }
        Country: { LargeRecord.Country : Trim }
        City: { LargeRecord.City : Trim }
        Address: { LargeRecord.Address : Trim }
        PostalCode: { LargeRecord.PostalCode : Trim }
        Comment: { LargeRecord.Comment : Trim, MaxLength(200) }
        Keywords: { LargeRecord.Keywords : Split(',') }
        Items: { LargeRecord.Items : Trim, IsNotEmpty, Repeating }
        """;

    /// <summary>
    /// Input text that matches <see cref="LargeTemplate"/>.
    /// Values are chosen to pass all validators and exercise all transformers.
    /// </summary>
    public static string LargeInput() =>
        """
        Record Entry

        Name: Alice Johnson
        Email: ALICE@EXAMPLE.COM
        Phone: +1-555-0123
        Domain: EXAMPLE.COM
        URL: https://example.com/page
        Loose URL: example.com/page
        Absolute URL: https://example.com/absolute
        Code: ref-benchmark-01
        Count: 99999
        Total: 42
        Created: 2024-06-15
        Updated: 2024-07-20
        Status: active
        Type: category
        Description: Performance benchmark test record. Extra text here.
        Summary: Result: all tests passed
        Notes: First note. Second note. Final note.
        Category: type-sub-electronics
        SubCategory: test#value
        Reference: REF-001
        Identifier: BENCH-ID
        Tag: bench_mark
        Label: primary
        Origin: synthetic-data
        Source: generator
        Target: output
        Priority: high
        Level: INFO
        Rating: 95
        Score: 88
        Version: 2.0.1
        Region: us-east-1
        Country: United States
        City: New York
        Address: 123 Main Street
        PostalCode: 10001
        Comment: Synthetic benchmark record for performance testing
        Keywords: alpha,beta,gamma
        Items: item-alpha
        Items: item-beta
        Items: item-gamma
        """;

    // ── Non-matching templates for MatcherBenchmarks ─────────────────

    /// <summary>
    /// Generates a non-matching template with a unique hint that won't
    /// appear in the medium input. Used to fill TokenMatcher with
    /// templates that fail hint filtering quickly.
    /// </summary>
    public static string NonMatchingTemplate(int index) =>
        $$"""
        ---
        name: non-matching-{{index}}
        hint: XYZZY-NOMATCH-{{index}}
        ---

        XYZZY-NOMATCH-{{index}}

        Field: { NonMatch.Field }
        """;
}
