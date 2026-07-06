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
        "Name: { SmallRecord.Name : Trim, IsNotEmpty }\nStatus: { SmallRecord.Status : ToUpper }\nCode: { SmallRecord.Code : Trim }\n";

    /// <summary>
    /// Input text that matches <see cref="SmallTemplate"/>.
    /// </summary>
    public static string SmallInput() =>
        "Name: Alice Johnson\nStatus: active\nCode: ABC-123\n";

    // ── Medium tier: 12 tokens ────────────────────────────────────────

    /// <summary>
    /// Medium template: 12 tokens exercising Trim, ToUpper, ToLower,
    /// ToDateTime, SubstringBefore, SubstringAfter, Replace,
    /// IsNotEmpty, IsNumeric, IsEmail, IsDomainName, IsDateTime,
    /// Contains, StartsWith.
    /// </summary>
    public static string MediumTemplate() =>
        "Name: { MediumRecord.Name : Trim, IsNotEmpty }\nEmail: { MediumRecord.Email : ToLower, IsEmail }\nDomain: { MediumRecord.Domain : ToLower, IsDomainName }\nCode: { MediumRecord.Code : ToUpper, StartsWith('R') }\nCount: { MediumRecord.Count : Trim, IsNumeric }\nCreated: { MediumRecord.Created : Trim, ToDateTime('yyyy-MM-dd'), IsDateTime }\nStatus: { MediumRecord.Status : ToUpper, Contains('ACT') }\nDescription: { MediumRecord.Description : SubstringBefore('.') }\nCategory: { MediumRecord.Category : SubstringAfter('-') }\nReference: { MediumRecord.Reference : Replace('REF', 'R') }\nTag: { MediumRecord.Tag : Trim }\nOrigin: { MediumRecord.Origin : Trim }\n";

    /// <summary>
    /// Input text that matches <see cref="MediumTemplate"/>.
    /// </summary>
    public static string MediumInput() =>
        "Name: Bob Smith\nEmail: BOB@EXAMPLE.COM\nDomain: EXAMPLE.COM\nCode: ref-42\nCount: 12345\nCreated: 2024-06-15\nStatus: active\nDescription: This is a test record. Extra text here.\nCategory: type-electronics\nReference: REF-001\nTag: benchmark\nOrigin: synthetic\n";

    // ── Large tier: 39 tokens including repeating, front matter ──────

    /// <summary>
    /// Large template: 39 tokens exercising every transformer and validator.
    /// Includes front matter with name, tags, hints, and set directive.
    /// Includes repeating tokens.
    /// </summary>
    public static string LargeTemplate() =>
        "---\nname: large-benchmark-template\ntag: benchmark\ntag: performance\nhint: Record Entry\nset: Found = Yes\nterminateOnNewLine: true\n---\n\nRecord Entry\n\nName: { LargeRecord.Name : Trim, IsNotEmpty, MinLength(2) }\nEmail: { LargeRecord.Email : ToLower, IsEmail }\nPhone: { LargeRecord.Phone : Trim, IsPhoneNumber }\nDomain: { LargeRecord.Domain : ToLower, IsDomainName }\nURL: { LargeRecord.Url : Trim, IsUrl }\nLoose URL: { LargeRecord.LooseUrl : Trim, IsLooseUrl }\nAbsolute URL: { LargeRecord.AbsoluteUrl : Trim, IsLooseAbsoluteUrl }\nCode: { LargeRecord.Code : ToUpper, StartsWith('R'), MaxLength(20) }\nCount: { LargeRecord.Count : Trim, IsNumeric }\nTotal: { LargeRecord.Total : Trim, IsNumeric, IsNot('0') }\nCreated: { LargeRecord.Created : Trim, ToDateTime('yyyy-MM-dd'), IsDateTime }\nUpdated: { LargeRecord.Updated : Trim, ToDateTimeUtc('yyyy-MM-dd') }\nStatus: { LargeRecord.Status : ToUpper, Contains('ACT') }\nType: { LargeRecord.Type : Trim, EndsWith('ry') }\nDescription: { LargeRecord.Description : SubstringBefore('.'), MinLength(5) }\nSummary: { LargeRecord.Summary : SubstringAfter(':') }\nNotes: { LargeRecord.Notes : SubstringBeforeLast('.') }\nCategory: { LargeRecord.Category : SubstringAfterLast('-') }\nSubCategory: { LargeRecord.SubCategory : Remove('#') }\nReference: { LargeRecord.Reference : RemoveStart('REF-') }\nIdentifier: { LargeRecord.Identifier : RemoveEnd('-ID') }\nTag: { LargeRecord.Tag : Replace('_', '-') }\nLabel: { LargeRecord.Label : Trim, IsNotEmpty }\nOrigin: { LargeRecord.Origin : Trim, Contains('syn') }\nSource: { LargeRecord.Source : Trim }\nTarget: { LargeRecord.Target : Trim }\nPriority: { LargeRecord.Priority : ToUpper }\nLevel: { LargeRecord.Level : ToLower }\nRating: { LargeRecord.Rating : Trim, IsNumeric }\nScore: { LargeRecord.Score : Trim, IsNumeric }\nVersion: { LargeRecord.Version : Trim }\nRegion: { LargeRecord.Region : Trim }\nCountry: { LargeRecord.Country : Trim }\nCity: { LargeRecord.City : Trim }\nAddress: { LargeRecord.Address : Trim }\nPostalCode: { LargeRecord.PostalCode : Trim }\nComment: { LargeRecord.Comment : Trim, MaxLength(200) }\nKeywords: { LargeRecord.Keywords : Split(',') }\nItems: { LargeRecord.Items : Trim, IsNotEmpty, Repeating }\n";

    /// <summary>
    /// Input text that matches <see cref="LargeTemplate"/>.
    /// Values are chosen to pass all validators and exercise all transformers.
    /// </summary>
    public static string LargeInput() =>
        "Record Entry\n\nName: Alice Johnson\nEmail: ALICE@EXAMPLE.COM\nPhone: +1-555-0123\nDomain: EXAMPLE.COM\nURL: https://example.com/page\nLoose URL: example.com/page\nAbsolute URL: https://example.com/absolute\nCode: ref-benchmark-01\nCount: 99999\nTotal: 42\nCreated: 2024-06-15\nUpdated: 2024-07-20\nStatus: active\nType: category\nDescription: Performance benchmark test record. Extra text here.\nSummary: Result: all tests passed\nNotes: First note. Second note. Final note.\nCategory: type-sub-electronics\nSubCategory: test#value\nReference: REF-001\nIdentifier: BENCH-ID\nTag: bench_mark\nLabel: primary\nOrigin: synthetic-data\nSource: generator\nTarget: output\nPriority: high\nLevel: INFO\nRating: 95\nScore: 88\nVersion: 2.0.1\nRegion: us-east-1\nCountry: United States\nCity: New York\nAddress: 123 Main Street\nPostalCode: 10001\nComment: Synthetic benchmark record for performance testing\nKeywords: alpha,beta,gamma\nItems: item-alpha\nItems: item-beta\nItems: item-gamma\n";

    // ── Non-matching templates for MatcherBenchmarks ─────────────────

    /// <summary>
    /// Generates a non-matching template with a unique hint that won't
    /// appear in the medium input. Used to fill TokenMatcher with
    /// templates that fail hint filtering quickly.
    /// </summary>
    public static string NonMatchingTemplate(int index) =>
        $$"---\nname: non-matching-{{index}}\nhint: XYZZY-NOMATCH-{{index}}\n---\n\nXYZZY-NOMATCH-{{index}}\n\nField: { NonMatch.Field }\n";
}
