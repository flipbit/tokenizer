# PRD: Token Match Debugging & Diagnostics

## Context

During investigation of a failing test (`SampleTests.TestWhoisVe`), the debugging process revealed significant gaps in observability and diagnostics. The token `Expiration` was not matching the input data, but determining why required:

1. Manual tracing through multiple pipeline stages (Lexer → Parser → Binder → Compiler → Runtime Matching)
2. Writing custom diagnostic tests to isolate the issue
3. Inspecting intermediate data structures at each stage
4. Manual comparison of expected vs. actual input at character level

The issue turned out to be in the runtime matching stage (preamble matching), but it took significant effort to narrow it down.

## Problem Statement

When tokenization fails or produces unexpected results, developers have limited visibility into:

- **Which stage failed** (parsing, compilation, or runtime matching)
- **Why a token didn't match** (preamble mismatch, decorator validation failure, etc.)
- **Where in the input** the failure occurred
- **What was expected vs. what was found** at the point of failure
- **The state of the matching engine** at the point of failure

Error messages like "Token 'Expiration' was not found in the input text" provide no actionable debugging information.

## Goals

1. **Reduce time to diagnose** tokenization failures from hours to minutes
2. **Provide actionable error messages** that point directly to the root cause
3. **Enable self-service debugging** without requiring deep knowledge of the codebase
4. **Prevent regressions** through better visibility into tokenization behavior
5. **Improve developer experience** for both library maintainers and consumers

## Proposed Solutions

### 1. Structured Diagnostic Output / Trace Mode

**Priority: High | Effort: Medium**

Add diagnostic capabilities to `TokenizerOptions`:

```csharp
public class TokenizerOptions
{
    public DiagnosticLevel DiagnosticLevel { get; set; } = DiagnosticLevel.None;
}

public enum DiagnosticLevel
{
    None,        // No diagnostics (current behavior)
    Summary,     // High-level: what matched, what didn't
    Detailed,    // Token-by-token matching attempts
    Verbose      // Character-by-character matching with positions
}
```

Capture diagnostic information during tokenization:

```csharp
public class TokenizeResult
{
    // Existing properties...
    public TokenizeDiagnostics Diagnostics { get; set; }
}

public class TokenizeDiagnostics
{
    public int TotalTokensInTemplate { get; set; }
    public int TokensMatched { get; set; }
    public List<TokenMatchAttempt> MatchAttempts { get; set; }
    public string Summary { get; set; }

    public string ToMarkdownReport() { ... }
    public string ToJsonReport() { ... }
}

public class TokenMatchAttempt
{
    public string TokenName { get; set; }
    public int TokenId { get; set; }
    public bool Matched { get; set; }
    public string FailureReason { get; set; }
    public int InputPosition { get; set; }
    public string ExpectedPreamble { get; set; }
    public string ActualInput { get; set; }  // Next N characters from input
    public TimeSpan Duration { get; set; }

    public string ToDetailedReport() { ... }
}
```

**Benefits:**
- Zero cost when disabled (default)
- Opt-in for debugging scenarios
- Structured data for programmatic analysis
- Export to multiple formats (text, JSON, markdown)

**Implementation Notes:**
- Add conditional compilation or runtime checks to minimize performance impact
- Capture diagnostics in `TokenizationEngine` or similar runtime matching code
- Include factory method to create human-readable reports

---

### 2. Enhanced Error Messages with Context

**Priority: High | Effort: Low**

Transform error messages from:
```
Token 'Expiration' was not found in the input text.
```

To:
```
Token 'Expiration' (ID: 34) failed to match at position 1234.

Expected preamble:
  "\n   Fecha de Vencimiento: " (25 chars)
  Escaped: "\\n   Fecha de Vencimiento: "

Input at position 1234:
  "\n\nFecha de Vencimiento: 2010-11-21..." (showing next 50 chars)
  Escaped: "\\n\\nFecha de Vencimiento: 2010-11-21..."

Last successfully matched token:
  BillingContact.FaxNumber (ID: 33) at position 1210

Possible issues:
  - Preamble mismatch (check whitespace/newlines)
  - Previous token consumed too much input
  - Decorator validation failed
```

**Benefits:**
- Immediate value with minimal code changes
- Works with existing error handling
- Actionable information for developers

**Implementation Notes:**
- Add context to exceptions (position, last matched token, expected vs. actual)
- Create helper methods for formatting escaped strings
- Include heuristics for common failure patterns

---

### 3. Preamble Mismatch Visual Diff

**Priority: Medium | Effort: Low**

When a preamble doesn't match, show character-by-character comparison:

```
Preamble Mismatch for token 'Expiration':

Expected: "\n   Fecha de Vencimiento: "
          ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓
Actual:   "\n\nFecha de Vencimiento: "
           ✓ ✗ ✗ ✗ ✗

Position 1235: Expected ' ' (space, U+0020), got '\n' (newline, U+000A)
```

**Benefits:**
- Instantly identifies whitespace/invisible character issues
- Visual format is easy to understand
- Pinpoints exact mismatch location

**Implementation Notes:**
- Create `PreambleDiffer` utility class
- Use Unicode escape sequences for non-printable characters
- Consider using ANSI color codes for terminal output

---

### 4. Tokenization Summary Report

**Priority: Medium | Effort: Medium**

Generate a comprehensive report after tokenization (markdown or JSON):

```markdown
# Tokenization Report: whois.ve

**Template:** whois.ve (53 tokens)
**Input:** aloespa.com.ve (1367 chars)
**Result:** ✗ Partial Match (34/53 tokens matched)
**Duration:** 12ms

## Summary

- ✓ 34 tokens matched successfully
- ✗ 19 tokens failed to match
- ⚠️ 3 tokens skipped due to previous failures

## Matched Tokens (34)

| ID | Token Name | Position | Value | Duration |
|----|------------|----------|-------|----------|
| 1  | Registrant.Name | 142 | "Rafael Perez" | 0.2ms |
| 2  | Registrant.RegistryId | 156 | "aloespa.com.ve-dom" | 0.1ms |
| ... | ... | ... | ... | ... |

## Failed Tokens (19)

| ID | Token Name | Failure Reason | Position | Details |
|----|------------|----------------|----------|---------|
| 34 | Expiration | Preamble mismatch | 1234 | Expected "\n   ", found "\n\n" |
| 35 | Updated | Skipped (previous token failed) | - | - |
| 36 | Registered | Skipped (previous token failed) | - | - |

## Timeline

```
0────142────156────...────1210────1234────[FAIL]
│     │      │             │        │
│     │      │             │        └─ Token #34 Failed
│     │      │             └─ Token #33 Matched
│     │      └─ Token #2 Matched
│     └─ Token #1 Matched
└─ Start
```

## Performance

- Fastest match: Token #5 (0.05ms)
- Slowest match: Token #28 (2.3ms)
- Average match time: 0.35ms
```

**Benefits:**
- High-level overview of tokenization results
- Easy to spot patterns in failures
- Shareable format for bug reports
- Timeline visualization helps understand matching flow

**Implementation Notes:**
- Create `ReportGenerator` class with multiple output formats
- Include performance metrics (requires diagnostic mode)
- Consider adding filtering/sorting options

---

### 5. Enhanced Logging Integration

**Priority: Low | Effort: Low**

Improve existing `ILog` integration with structured data:

```csharp
log.Debug("Attempting to match token {TokenId} '{TokenName}' at position {Position}",
    token.Id, token.Name, currentPosition);

log.Debug("  Preamble: {Preamble} ({Length} chars)",
    EscapeForLog(token.Preamble), token.Preamble.Length);

log.Debug("  Input: {Input}",
    EscapeForLog(GetInputContext(currentPosition, 50)));

if (!preambleMatched)
{
    log.Warning("Preamble mismatch for token {TokenId} '{TokenName}' at position {Position}",
        token.Id, token.Name, currentPosition);
    log.Warning("  Expected: {Expected}", EscapeForLog(token.Preamble));
    log.Warning("  Found: {Found}", EscapeForLog(actualPreamble));
    log.Warning("  Diff: {Diff}", CreatePreambleDiff(token.Preamble, actualPreamble));
}
```

**Benefits:**
- Integrates with existing logging infrastructure
- Structured logging for better analysis (e.g., with Seq, Splunk)
- Can be enabled/disabled via log level configuration

**Implementation Notes:**
- Add helper methods for escaping and context extraction
- Use semantic logging with named parameters
- Consider performance impact of string formatting

---

### 6. Unit Test Helpers

**Priority: Medium | Effort: Low**

Add fluent assertion helpers for tests:

```csharp
[Fact]
public void TestWhoisVe_WithDiagnostics()
{
    var tokenizer = new Tokenizer(new TokenizerOptions
    {
        DiagnosticLevel = DiagnosticLevel.Detailed
    });

    var result = tokenizer.Tokenize(template, input);

    // Fluent assertions for diagnostics
    result.Diagnostics
        .ShouldHaveMatched("Expiration")
        .AtPosition(1234)
        .WithValue(new DateTime(2010, 11, 21, 15, 21, 32, DateTimeKind.Utc));

    // Or investigate why it failed
    var failure = result.Diagnostics.GetFailure("Expiration");
    _output.WriteLine(failure.DetailedReport);

    // Assert on specific aspects
    Assert.Equal("Preamble mismatch", failure.Reason);
    Assert.Contains("Expected \\n  ", failure.Details);
}
```

**Benefits:**
- Makes test failures more informative
- Reduces boilerplate in tests
- Encourages writing diagnostic-aware tests

**Implementation Notes:**
- Create extension methods for `TokenizeDiagnostics`
- Add assertion helpers that integrate with xUnit
- Include output helpers for `ITestOutputHelper`

---

### 7. Compilation Validation Report

**Priority: Low | Effort: Medium**

After compilation, before runtime matching, detect potential issues:

```
Template Compilation Summary: whois.ve

Total tokens: 53
- Required: 8
- Optional: 45
- With decorators: 42
- With multiline preambles: 12

Warnings:
⚠️  Token #34 'Expiration' has multiline preamble with whitespace-only lines
    This may cause matching issues if input has different whitespace.
    Preamble: "\n   Fecha de Vencimiento: "

⚠️  Token #12 'AdminContact.Email' has complex decorator chain (3 decorators)
    Validation/transformation failures may not be obvious in error messages.

⚠️  Tokens #22-#25 have identical preambles
    May match incorrectly depending on input order.

Suggestions:
💡 Consider using TrimPreambleBeforeNewLine option to handle whitespace variations
💡 Use EOL decorator consistently to avoid ambiguous matches
```

**Benefits:**
- Catches common template issues early
- Educates developers about best practices
- Prevents runtime surprises

**Implementation Notes:**
- Create validation pass after compilation
- Build library of common anti-patterns
- Make warnings configurable (enable/disable specific checks)

---

### 8. Interactive Debugger / REPL (Future)

**Priority: Low | Effort: High**

Create a CLI tool for interactive debugging:

```bash
$ tokenizer-debug --template whois.ve.txt --input aloespa.com.ve.txt

Loading template... 53 tokens loaded
Loading input... 1367 chars loaded

> step
Attempting token #1 'Registrant.Name'... ✓ Matched at position 142
Value: "Rafael Perez"

> step
Attempting token #2 'Registrant.RegistryId'... ✓ Matched at position 156
Value: "aloespa.com.ve-dom"

> jump 34
Jumping to token #34 'Expiration'...

> step
Attempting token #34 'Expiration'... ✗ Failed at position 1234
Reason: Preamble mismatch

> show-context
Position 1234:
...2418246437 (FAX) 2418246437
[blank line]
   Fecha de Vencimiento: 2010-11-21 15:21:32
...

> show-preamble
Expected: "\n   Fecha de Vencimiento: "
Found:    "\n\nFecha de Vencimiento: "
          ✓ ✗ ✗

> skip
Skipping token #34...

> continue
...
```

**Benefits:**
- Ultimate debugging experience
- Educational for understanding tokenization
- Useful for complex template development

**Implementation Notes:**
- Separate project/tool (not core library)
- Build on top of diagnostic infrastructure
- Consider using Spectre.Console for rich terminal UI

---

### 9. Snapshot Testing Support (Future)

**Priority: Low | Effort: Medium**

Capture full tokenization state for regression testing:

```csharp
[Fact]
public void TestWhoisVe_Snapshot()
{
    var result = tokenizer.Tokenize(template, input);

    // Generates/compares snapshot file
    Snapshot.Match(result, new SnapshotOptions
    {
        IncludePositions = true,
        IncludeDiagnostics = true,
        IncludeUnmatchedTokens = true,
        IncludeTimings = false  // Non-deterministic
    });
}
```

Generated snapshot file (`TestWhoisVe_Snapshot.snap`):
```json
{
  "success": true,
  "matchCount": 34,
  "matches": [
    {
      "tokenId": 1,
      "tokenName": "Registrant.Name",
      "position": 142,
      "value": "Rafael Perez"
    },
    ...
  ],
  "unmatchedTokens": [
    {
      "tokenId": 34,
      "tokenName": "Expiration",
      "reason": "Preamble mismatch",
      "position": 1234
    }
  ]
}
```

**Benefits:**
- Automatic detection of behavioral changes
- Clear diff when tokenization changes
- Living documentation of expected behavior

**Implementation Notes:**
- Use existing snapshot libraries (e.g., Verify)
- Serialize `TokenizeResult` + diagnostics
- Filter out non-deterministic data

---

## Implementation Priorities

### Phase 1: Quick Wins (1-2 weeks)
1. Enhanced error messages with context (#2)
2. Preamble mismatch visual diff (#3)
3. Enhanced logging integration (#5)

**Impact:** Immediately improves debugging experience with minimal effort

### Phase 2: Diagnostic Infrastructure (3-4 weeks)
1. Structured diagnostic output (#1)
2. Unit test helpers (#6)
3. Tokenization summary report (#4)

**Impact:** Provides comprehensive diagnostic capabilities

### Phase 3: Advanced Features (Future)
1. Compilation validation report (#7)
2. Snapshot testing support (#9)
3. Interactive debugger (#8)

**Impact:** Power-user features and advanced debugging

## Success Metrics

- **Time to diagnose** tokenization failures reduced by 80%
- **Error message actionability** - 90% of failures debuggable from error message alone
- **Developer satisfaction** - Positive feedback from library consumers
- **Support burden** - Reduced number of "why isn't this matching?" questions

## Open Questions

1. **Performance impact** - What is acceptable overhead for diagnostic mode?
2. **Output formats** - Which formats are most useful (JSON, markdown, plain text)?
3. **Verbosity levels** - Are 4 levels sufficient, or do we need more granularity?
4. **Storage** - Should diagnostic data be stored/cached for post-mortem analysis?
5. **Configuration** - Should diagnostics be configurable per-token or only globally?

## References

- Initial investigation: `SampleTests.TestWhoisVe` failure
- Diagnostic tests: `tests/Tokenizer.Tests/Compilation/Parsing/Template/TemplateParserQuotedStringTests.cs`
- Related systems: Roslyn diagnostics, Rust compiler error messages

---

**Document Status:** Draft
**Author:** Claude (via debugging session)
**Date:** 2025-10-16
**Version:** 1.0
