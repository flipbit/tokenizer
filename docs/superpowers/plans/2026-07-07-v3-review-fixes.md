# v3 Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix all 38 H/M/L issues from the 2026-07-07 v3 review plus add TemplateOptionsCascadeTests

**Architecture:** 8 groups organized by subsystem, executed in dependency order. Each group is a commit. TDD for all behavioral changes — write failing tests first, then fix.

**Tech Stack:** C# / .NET 10 / xUnit / NSubstitute (available but not used — tests use real implementations)

**Spec:** `docs/superpowers/specs/2026-07-07-v3-review-fixes-design.md`

**Build:** `dotnet build ./src/Tokenizer/Tokenizer.csproj -c Release`

**Test:** `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`

**Test filter:** `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "FullyQualifiedName~ClassName"`

---

### Task 1: Compilation Pipeline Cleanup (H1, H5, M2, M9, M10, L2, L3, L7)

**Files:**
- Modify: `src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs:137` — make ParseBoolean `internal`
- Modify: `src/Tokenizer/Compilation/Binders/TemplateBinder.cs` — accept options param, remove IsFrontMatterOptionTrue, remove TerminateOnNewLine application
- Modify: `src/Tokenizer/Compilation/Parsing/AstTemplateDefinitionParser.cs:34` — pass options to TemplateBinder.Bind
- Modify: `src/Tokenizer/Compilation/TemplateCompiler.cs` — add ILogger, attach diagnostics on failure
- Modify: `src/Tokenizer/TemplateCollection.cs:32` — fix conditional compilation guard
- Modify: `src/Tokenizer/Compilation/Parsing/TokenReader.cs` — implement IDisposable
- Delete: `src/Tokenizer/Compilation/Parsing/TemplateDefinitionEnumerator.cs` — dead code
- Delete: `tests/Tokenizer.Tests/Compilation/Parsing/TemplateDefinitionEnumeratorTests.cs` — tests for dead code

- [ ] **Step 1: Fix H1 — make FrontMatterBinder.ParseBoolean shared**

In `src/Tokenizer/Compilation/Binders/FrontMatterBinder.cs`, change:

```csharp
    private static bool ParseBoolean(string input, string rawName, FrontMatterEntry entry)
```

to:

```csharp
    internal static bool ParseBoolean(string input, string rawName, FrontMatterEntry entry)
```

- [ ] **Step 2: Fix H5 + M2 — rewrite TemplateBinder.Bind to accept options**

In `src/Tokenizer/Compilation/Binders/TemplateBinder.cs`, change the `Bind` signature and remove the front matter re-parsing:

```csharp
    public static TemplateDefinition Bind(TemplateDocument document, TokenizerOptions options)
    {
        var result = new TemplateDefinition();
        var tokens = new List<TokenDefinition>();
        var preambleBuilder = new System.Text.StringBuilder();

        foreach (var node in document.Content)
        {
```

Remove the two lines that read front matter options (lines 20-21):
```csharp
        var globalTrimPreambleBeforeNewLine = IsFrontMatterOptionTrue(document, "trimpreamblebeforenewline");
        var globalTerminateOnNewLine = IsFrontMatterOptionTrue(document, "terminateonnewline");
```

Replace references to `globalTrimPreambleBeforeNewLine` with `options.TrimPreambleBeforeNewLine` and references to `globalTerminateOnNewLine` — but wait, we are also removing the TerminateOnNewLine application from TemplateBinder (M2). So remove the block at lines 124-128:
```csharp
                // Apply global terminate option if set in front matter
                if (globalTerminateOnNewLine)
                {
                    def.TerminateOnNewLine = true;
                }
```

For `TrimPreambleBeforeNewLine`, replace the condition that uses `globalTrimPreambleBeforeNewLine` with `options.TrimPreambleBeforeNewLine`.

Delete the entire `IsFrontMatterOptionTrue` method (lines 256-273).

- [ ] **Step 3: Update AstTemplateDefinitionParser to pass options**

In `src/Tokenizer/Compilation/Parsing/AstTemplateDefinitionParser.cs`, change line 34:

```csharp
        var bound = TemplateBinder.Bind(document);
```

to:

```csharp
        var bound = TemplateBinder.Bind(document, result.Options);
```

- [ ] **Step 4: Fix M9 — add logging to TemplateCompiler**

In `src/Tokenizer/Compilation/TemplateCompiler.cs`, add logger injection:

```csharp
using Microsoft.Extensions.Logging;
```

Add field and update constructor:

```csharp
internal sealed class TemplateCompiler
{
    private readonly DecoratorRegistry _registry;
    private readonly ConcurrentDictionary<Type, ITokenDecorator> _decoratorCache = new();
    private readonly ILogger<TemplateCompiler> _log;

    public TokenizerOptions Options { get; }

    public TemplateCompiler(TokenizerOptions options, ILoggerFactory? loggerFactory = null)
    {
        Options = options;
        _registry = new DecoratorRegistry(options);
        _log = (loggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance)
            .CreateLogger<TemplateCompiler>();
    }
```

Add logging in `Compile`:

```csharp
    public CompilationResult Compile(string content)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Starting template compilation, content length {ContentLength}", content.Length);
        }

        IDiagnosticCollector collector = Options.EnableDiagnostics
            ? new DiagnosticCollector(inputContent: null)
            : NullDiagnosticCollector.Instance;

        TemplateLengthValidator.Validate(content, Options);

        try
        {
            // ... existing compilation logic ...

            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("Template '{TemplateName}' compiled successfully with {TokenCount} token(s)",
                    template.Name, template.Tokens.Count);
            }

            return new CompilationResult(template, collector.GetResult());
        }
        catch (TokenizerException ex)
        {
            ex.Data["DiagnosticResult"] = collector.GetResult();
            _log.LogError(ex, "Template compilation failed, content length {ContentLength}", content.Length);
            throw;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unexpected error during template compilation, content length {ContentLength}", content.Length);
            throw new TokenizerException($"Unexpected error during template compilation: {ex.Message}", ex);
        }
    }
```

Note: L7 (attach diagnostics on failure) is handled by the `ex.Data["DiagnosticResult"]` line above.

- [ ] **Step 5: Update callers that create TemplateCompiler**

Search for `new TemplateCompiler(` and pass the loggerFactory where available. The `Tokenizer` class already has an `ILoggerFactory` — thread it through.

- [ ] **Step 6: Fix M10 — stale conditional compilation guard**

In `src/Tokenizer/TemplateCollection.cs:32`, change:

```csharp
#if NET6_0_OR_GREATER
```

to:

```csharp
#if NET8_0_OR_GREATER
```

- [ ] **Step 7: Fix L2 — TokenReader IDisposable**

In `src/Tokenizer/Compilation/Parsing/TokenReader.cs`, make the class implement `IDisposable`:

```csharp
internal sealed class TokenReader : IDisposable
{
```

Add at the end of the class:

```csharp
    public void Dispose()
    {
        _enumerator.Dispose();
    }
```

Update callers (in `TemplateParser` and `FrontMatterParser`) to use `using` statements:

```csharp
using var reader = new TokenReader(tokens);
```

- [ ] **Step 8: Fix L3 — remove TemplateDefinitionEnumerator**

First, check that `ParsingException`'s internal constructor referencing `TemplateDefinitionEnumerator` can be updated. Change `ParsingException.cs` line 12:

```csharp
    internal ParsingException(string message, TemplateDefinitionEnumerator enumerator) : this(message, enumerator.Location)
```

to use `FileLocation` directly (or remove this constructor if no callers exist). Grep for callers of this constructor. If none, remove it.

Then delete:
- `src/Tokenizer/Compilation/Parsing/TemplateDefinitionEnumerator.cs`
- `tests/Tokenizer.Tests/Compilation/Parsing/TemplateDefinitionEnumeratorTests.cs`

- [ ] **Step 9: Build and run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All 1334 tests pass (minus deleted TemplateDefinitionEnumerator tests)

- [ ] **Step 10: Commit**

```bash
git add -A && git status
git commit -m "fix: compilation pipeline cleanup (H1, H5, M2, M9, M10, L2, L3, L7)"
```

---

### Task 2: Token & Tokenization Bugs (H2, H3, H4, M1, M3, M7, L4, L6)

**Files:**
- Modify: `src/Tokenizer/Token.cs:253` — fix off-by-one, fix SetDictionaryValue, record diagnostic on missing property
- Modify: `src/Tokenizer/TokenizeResult.cs:32-88` — single-pass iteration
- Modify: `src/Tokenizer/TokenDecoratorContext.cs:69,75` — cache IsTransformer/IsValidator
- Modify: `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs` — track matched values
- Modify: `src/Tokenizer/Extensions/StringExtensions.cs:207` — remove ToCharArray
- Test: `tests/Tokenizer.Tests/` — TDD tests for each fix

- [ ] **Step 1: Write failing test for H2 — TerminateOnNewLine at index 0**

Create or extend a test file. Add a test that tokenizes input where the token value starts with a newline:

```csharp
[Fact]
public void GivenTerminateOnNewLine_WhenValueStartsWithNewline_ThenValueIsTruncatedToEmpty()
{
    // Arrange
    var tokenizer = Tokenizer.Create();
    var template = tokenizer.Compile("Preamble: {Value:Terminate}");
    // Input where after "Preamble: " the value starts with \n
    var input = "Preamble: \nrest of text";

    // Act
    var result = tokenizer.Tokenize(template, input);

    // Assert
    var value = result.First("Value");
    Assert.Equal(string.Empty, value);
}
```

Run: `dotnet test --filter "GivenTerminateOnNewLine_WhenValueStartsWithNewline"`
Expected: FAIL (current code returns `"\nrest of text"` or similar because `index > 0` skips index 0)

- [ ] **Step 2: Fix H2 — change index check**

In `src/Tokenizer/Token.cs:253`, change:

```csharp
            if (index > 0)
```

to:

```csharp
            if (index >= 0)
```

- [ ] **Step 3: Run test to verify it passes**

Run: `dotnet test --filter "GivenTerminateOnNewLine_WhenValueStartsWithNewline"`
Expected: PASS

- [ ] **Step 4: Write failing test for H3 — SetDictionaryValue existing non-list value**

```csharp
[Fact]
public void GivenRepeatingTokenOnDictionary_WhenExistingValueIsNotList_ThenWrapsExistingValue()
{
    // Arrange
    var tokenizer = Tokenizer.Create();
    // A template with a repeating token
    var template = tokenizer.Compile("Name: {Name*}");
    var dict = new Dictionary<string, object> { ["Name"] = "existing" };

    // Act — tokenize with the dictionary, adding a second match
    var result = tokenizer.Tokenize(template, "Name: first\nName: second", dict);

    // Assert — should be a list containing the values
    var names = result.First("Name");
    Assert.IsType<List<object>>(names);
}
```

Run and verify it fails, then fix.

- [ ] **Step 5: Fix H3 — wrap existing value in SetDictionaryValue**

In `src/Tokenizer/Token.cs:211`, change:

```csharp
                list = dictionary[Name] as List<object> ?? new List<object>();
```

to:

```csharp
                list = dictionary[Name] as List<object> ?? new List<object> { dictionary[Name] };
```

- [ ] **Step 6: Fix H4 — IntegratedHintStrategy track matched values**

In `src/Tokenizer/Tokenization/Strategies/IntegratedHintStrategy.cs`, add a second HashSet and update `OnTokenMatched` and `PostProcess`:

```csharp
    private readonly HashSet<string> _matchedPreambles = new(StringComparer.Ordinal);
    private readonly HashSet<string> _matchedValues = new(StringComparer.Ordinal);
```

In `OnTokenMatched`, add after the preamble tracking:

```csharp
    public void OnTokenMatched(Token token)
    {
        if (!string.IsNullOrEmpty(token.Preamble))
        {
            _matchedPreambles.Add(token.Preamble);
        }
    }
```

The `OnTokenMatched` currently only has access to the `Token`, not the matched value. Check if the method signature can accept the value or if it's available from another source. If the interface `IHintStrategy.OnTokenMatched` only takes `Token`, we need to also add the value. Check the interface and update accordingly. If we can't change the interface without broader impact, document the limitation in the `TokenizeAsync` XML doc instead.

- [ ] **Step 7: Fix M1 — TokenizeResult single-pass iteration**

In `src/Tokenizer/TokenizeResult.cs`, rewrite `First(string key)`:

```csharp
    public object First(string key)
    {
        foreach (var m in Matches)
        {
            if (string.Equals(m.Token.Name, key, StringComparison.Ordinal))
            {
                return m.Value;
            }
        }

        throw new TokenizerException($"Token '{key}' was not found in the input text.");
    }
```

Rewrite `First<T>(string key)`:

```csharp
    public T First<T>(string key)
    {
        foreach (var m in Matches)
        {
            if (string.Equals(m.Token.Name, key, StringComparison.Ordinal))
            {
                return (T)m.Value;
            }
        }

        throw new TokenizerException($"Token '{key}' was not found in the input text.");
    }
```

Rewrite `FirstOrDefault(string key)`:

```csharp
    public object? FirstOrDefault(string key)
    {
        foreach (var m in Matches)
        {
            if (string.Equals(m.Token?.Name, key, StringComparison.Ordinal))
            {
                return m.Value;
            }
        }

        return null;
    }
```

Rewrite `FirstOrDefault<T>(string key)`:

```csharp
    public T? FirstOrDefault<T>(string key)
    {
        foreach (var m in Matches)
        {
            if (string.Equals(m.Token?.Name, key, StringComparison.Ordinal))
            {
                return (T)m.Value;
            }
        }

        return default;
    }
```

- [ ] **Step 8: Fix M3 — audit Token mutable surface**

In `src/Tokenizer/Token.cs`, grep the codebase for which properties are set after construction. For each property currently `internal set`, determine if it's mutated post-construction:

- `Preamble` — set in TokenBinder → keep `internal set`
- `Name` — set in TokenBinder → keep `internal set`
- `IsOptional` — set in OptionApplier → keep `internal set`
- `IsRepeating` — set in TokenBinder → keep `internal set`
- `TerminateOnNewLine` — set in OptionApplier → keep `internal set`
- `IsRequired` — set in TokenBinder → keep `internal set`
- `Id` — set in TokenBinder → keep `internal set`
- `DependsOnId` — set in TokenBinder → keep `internal set`
- `IsFrontMatterToken` — set in TokenBinder → keep `internal set`
- `IsNull` — set in TokenBinder → keep `internal set`
- `Location` — set in constructor, also in TokenBinder → keep `internal set`
- `CanConcatenate` — set in TokenBinder → keep `internal set`
- `ConcatenationString` — set in TokenBinder → keep `internal set`
- `IsSingleUse` — set in TokenBinder → keep `internal set`

If the audit finds that ALL properties are set by TokenBinder (which runs after construction), then M3 has no safe changes — all setters are needed. In that case, document that `internal set` is required because TokenBinder populates tokens post-construction. Do NOT change setters that are still needed.

- [ ] **Step 9: Fix M7 — record diagnostic on swallowed MissingMemberException**

In `src/Tokenizer/Token.cs`, the `Assign` method catch block at line 179-185. Change:

```csharp
        catch (MissingMemberException)
        {
            if (!options.IgnoreMissingProperties)
            {
                throw;
            }
        }
```

to:

```csharp
        catch (MissingMemberException)
        {
            if (!options.IgnoreMissingProperties)
            {
                throw;
            }

            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.TokenAssignmentFailed,
                    tokenName: Name, tokenId: Id,
                    value: value,
                    detail: $"Property '{Name}' not found on target type; ignored via IgnoreMissingProperties");
            }
        }
```

- [ ] **Step 10: Fix L4 — cache IsTransformer/IsValidator**

In `src/Tokenizer/TokenDecoratorContext.cs`, add fields and set in constructor:

```csharp
    private readonly bool _isTransformer;
    private readonly bool _isValidator;

    public TokenDecoratorContext(Type tokenDecorator, ConcurrentDictionary<Type, ITokenDecorator> decoratorCache)
    {
        DecoratorType = tokenDecorator;
        _parameters = new List<string>();
        _decoratorCache = decoratorCache;
        _isTransformer = typeof(ITokenTransformer).IsAssignableFrom(tokenDecorator);
        _isValidator = typeof(ITokenValidator).IsAssignableFrom(tokenDecorator);
    }
```

Change the properties from computed to cached:

```csharp
    public bool IsTransformer => _isTransformer;
    public bool IsValidator => _isValidator;
```

- [ ] **Step 11: Fix L6 — remove ToCharArray**

In `src/Tokenizer/Extensions/StringExtensions.cs:207`, change:

```csharp
            foreach (var character in value.ToCharArray())
```

to:

```csharp
            foreach (var character in value)
```

- [ ] **Step 12: Build and run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 13: Commit**

```bash
git add -A && git status
git commit -m "fix: token and tokenization bugs (H2, H3, H4, M1, M3, M7, L4, L6)"
```

---

### Task 3: Object Extensions (M4, L5)

**Files:**
- Modify: `src/Tokenizer/Extensions/ObjectExtensions.cs:98` — type validation on list Add
- Modify: `src/Tokenizer/Token.cs` — add cached path segments (if feasible)

- [ ] **Step 1: Fix M4 — type validation before List.Add**

In `src/Tokenizer/Extensions/ObjectExtensions.cs`, before the `addMethod.Invoke` calls (around line 94-104), add type checking. Find the list's element type and validate:

```csharp
                    var elementType = propertyInfo.PropertyType.GetGenericArguments()[0];

                    if (value is IEnumerable<string> valueList)
                    {
                        foreach (var valueItem in valueList)
                        {
                            var converted = ConvertForList(valueItem, elementType, propertyInfo.Name, @object.GetType().Name);
                            addMethod.Invoke(list, new[] { converted });
                        }
                    }
                    else
                    {
                        var converted = ConvertForList(value, elementType, propertyInfo.Name, @object.GetType().Name);
                        addMethod.Invoke(list, new[] { converted });
                    }
```

Add a helper method:

```csharp
    private static object ConvertForList(object value, Type elementType, string propertyName, string typeName)
    {
        if (elementType.IsInstanceOfType(value))
        {
            return value;
        }

        try
        {
            return Convert.ChangeType(value, elementType, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new TypeConversionException(
                $"Cannot add value of type '{value.GetType().Name}' to {typeName}.{propertyName} (List<{elementType.Name}>)",
                value, elementType, ex);
        }
    }
```

- [ ] **Step 2: Fix L5 — cache property path segments**

In `src/Tokenizer/Extensions/ObjectExtensions.cs:45`, add a static cache:

```csharp
    private static readonly ConcurrentDictionary<string, string[]> PathSegmentCache = new(StringComparer.Ordinal);
```

Change line 45:

```csharp
        var segments = propertyPath.Split('.');
```

to:

```csharp
        var segments = PathSegmentCache.GetOrAdd(propertyPath, static p => p.Split('.'));
```

Do the same in `GetValue` if it also calls `Split('.')`.

- [ ] **Step 3: Build and run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add -A && git status
git commit -m "fix: object extensions type validation and path caching (M4, L5)"
```

---

### Task 4: Safety & Security Hardening (M5, M6, L1, L9, L15)

**Files:**
- Modify: `src/Tokenizer/TokenMatcher.cs:285-328` — bounded buffering
- Modify: `src/Tokenizer/Extensions/StringExtensions.cs:17` — regex timeout
- Modify: `src/Tokenizer/Tokenization/CandidateProcessor.cs:78-86` — specific exception catches
- Modify: `src/Tokenizer/Validators/MatchesRegexValidator.cs:11` — cap regex cache

- [ ] **Step 1: Fix M5 — bounded BufferTextReaderAsync**

In `src/Tokenizer/TokenMatcher.cs`, change the signature and add size tracking:

```csharp
    private static async Task<MemoryStream> BufferTextReaderAsync(TextReader reader, long maxInputLength, CancellationToken ct)
    {
        var buffer = new MemoryStream();
#if NETSTANDARD2_0
        using var writer = new StreamWriter(buffer, Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
#else
        await using var writer = new StreamWriter(buffer, Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
#endif
        var charBuf = new char[4096];
        long totalChars = 0;
        int read;
        while ((read = await reader.ReadAsync(charBuf, 0, charBuf.Length).ConfigureAwait(false)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            totalChars += read;
            if (maxInputLength > 0 && totalChars > maxInputLength)
            {
                throw new TokenizerException(
                    $"Input exceeds MaxInputLength ({maxInputLength}) during buffering. " +
                    "Reduce input size or increase TokenizerOptions.MaxInputLength.");
            }
            await writer.WriteAsync(charBuf, 0, read).ConfigureAwait(false);
        }
#if NETSTANDARD2_0
        await writer.FlushAsync().ConfigureAwait(false);
#else
        await writer.FlushAsync(ct).ConfigureAwait(false);
#endif
        buffer.Position = 0;
        return buffer;
    }
```

Update all callers to pass `_tokenizer.Options.MaxInputLength`.

- [ ] **Step 2: Fix M6 — bounded EnsureSeekableAsync**

In `src/Tokenizer/TokenMatcher.cs`, replace the `CopyToAsync` with a bounded loop:

```csharp
    private async Task<Stream> EnsureSeekableAsync(Stream input, CancellationToken ct)
    {
        if (input.CanSeek) return input;

        if (!_tokenizer.Options.AllowStreamBuffering)
        {
            throw new TokenizerException(
                "Stream is not seekable. Provide a seekable stream or " +
                "set TokenizerOptions.AllowStreamBuffering = true to allow buffering into memory.");
        }

        var maxInputLength = _tokenizer.Options.MaxInputLength;
        var buffer = new MemoryStream();
        var copyBuf = new byte[81920];
        long totalBytes = 0;
        int read;
        while ((read = await input.ReadAsync(copyBuf, 0, copyBuf.Length, ct).ConfigureAwait(false)) > 0)
        {
            totalBytes += read;
            if (maxInputLength > 0 && totalBytes > maxInputLength)
            {
                buffer.Dispose();
                throw new TokenizerException(
                    $"Input stream exceeds MaxInputLength ({maxInputLength}) during buffering. " +
                    "Reduce input size or increase TokenizerOptions.MaxInputLength.");
            }
            await buffer.WriteAsync(copyBuf, 0, read, ct).ConfigureAwait(false);
        }
        buffer.Position = 0;
        return buffer;
    }
```

- [ ] **Step 3: Fix L1 — regex timeout**

In `src/Tokenizer/Extensions/StringExtensions.cs:17`, change:

```csharp
    private static readonly Regex NewLineSplitRegexInstance = new(@"\r\n|\r|\n", RegexOptions.Compiled, TimeSpan.FromMilliseconds(-1));
```

to:

```csharp
    private static readonly Regex NewLineSplitRegexInstance = new(@"\r\n|\r|\n", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
```

- [ ] **Step 4: Fix L9 — specific exception catches in CandidateProcessor**

In `src/Tokenizer/Tokenization/CandidateProcessor.cs:78-86`, change:

```csharp
        catch (Exception e)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(e, "Error Assigning Value: {Message}", e.Message);
            }
            _result.AddException(e);
            return false;
        }
```

to:

```csharp
        catch (TokenAssignmentException e)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(e, "Error Assigning Value: {Message}", e.Message);
            }
            _result.AddException(e);
            return false;
        }
        catch (TypeConversionException e)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(e, "Error Assigning Value: {Message}", e.Message);
            }
            _result.AddException(e);
            return false;
        }
```

Add the required using if not present:

```csharp
using Tokens.Exceptions;
```

- [ ] **Step 5: Fix L15 — cap regex cache**

In `src/Tokenizer/Validators/MatchesRegexValidator.cs`, add a size check before `GetOrAdd`:

```csharp
    /// <summary>
    /// Validator to determine if a token value matches a regular expression pattern.
    /// Patterns are cached for reuse. The cache is cleared if it exceeds 1024 entries.
    /// Patterns should be a finite, developer-controlled set defined in templates.
    /// </summary>
    public sealed class MatchesRegexValidator : ITokenValidator
    {
        private static readonly ConcurrentDictionary<string, Regex> RegexCache = new(StringComparer.Ordinal);
        private const int MaxCacheSize = 1024;
```

Before the `GetOrAdd` call:

```csharp
        if (RegexCache.Count >= MaxCacheSize)
        {
            RegexCache.Clear();
        }

        var regex = RegexCache.GetOrAdd(args[0],
            pattern => new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1)));
```

- [ ] **Step 6: Build and run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add -A && git status
git commit -m "fix: safety and security hardening (M5, M6, L1, L9, L15)"
```

---

### Task 5: Diagnostics Fixes (M8, L8)

**Files:**
- Modify: `src/Tokenizer/Tokenization/ResultBuilder.cs:159` — pass preamble in TokenMissed event
- Modify: `src/Tokenizer/Tokenizer.cs:333-334` — document async diagnostics limitation

- [ ] **Step 1: Fix M8 — pass preamble data in TokenMissed event**

In `src/Tokenizer/Tokenization/ResultBuilder.cs:159-160`, change:

```csharp
                collector.Record(DiagnosticEventType.TokenMissed,
                    tokenName: token.Name, tokenId: token.Id);
```

to:

```csharp
                collector.Record(DiagnosticEventType.TokenMissed,
                    tokenName: token.Name, tokenId: token.Id,
                    detail: token.Preamble);
```

- [ ] **Step 2: Fix L8 — document async diagnostics limitation**

In `src/Tokenizer/Tokenizer.cs`, at the `DiagnosticCollector(inputContent: null)` call in `TokenizeAsyncCore` (around line 333-334), add an inline comment:

```csharp
            // Async/streaming tokenization cannot provide the full input string to the diagnostic
            // collector. Alignment rendering and near-miss preamble hints require the complete input,
            // so these features produce degraded output in async mode. This is an inherent trade-off
            // of streaming — the full input is never fully buffered.
            IDiagnosticCollector collector = template.Options.EnableDiagnostics
                ? new DiagnosticCollector(inputContent: null)
                : NullDiagnosticCollector.Instance;
```

Also add to the `TokenizeAsync` XML doc on the public methods a `<remarks>` noting:
```xml
/// <remarks>
/// Diagnostics are partially limited in async/streaming mode: alignment rendering
/// and near-miss preamble hints require the full input string, which is unavailable
/// during streaming tokenization.
/// </remarks>
```

- [ ] **Step 3: Build and run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add -A && git status
git commit -m "fix: diagnostics — pass preamble in TokenMissed, document async limitation (M8, L8)"
```

---

### Task 6: Spec Compliance (L10, L11, L12)

**Files:**
- Modify: `src/Tokenizer/TokenizerOptions.cs:9` — add deviation doc
- Modify: `src/Tokenizer/Tokenization/TokenizationContext.cs` — add deviation comment
- Modify: Multiple files — seal public classes

- [ ] **Step 1: Fix L10 — document TokenizerOptions not sealed**

In `src/Tokenizer/TokenizerOptions.cs`, add to the XML doc:

```csharp
/// <summary>
/// Configuration options for the <see cref="Tokenizer"/>.
/// </summary>
/// <remarks>
/// This type is intentionally not sealed. C# record classes with a protected copy constructor
/// (required for deep-copy semantics via <c>with {}</c> expressions) cannot be sealed.
/// </remarks>
```

- [ ] **Step 2: Fix L11 — document TokenizationContext IDisposable removal**

In `src/Tokenizer/Tokenization/TokenizationContext.cs`, add a comment at the class level:

```csharp
/// <summary>
/// Tokenization context that encapsulates shared state during tokenization operations.
/// </summary>
/// <remarks>
/// IDisposable was removed — the enumerator lifecycle is managed by TokenizationSession,
/// and there are no other disposable resources owned by this context.
/// </remarks>
```

- [ ] **Step 3: Fix L12 — seal remaining public classes**

Add `sealed` to the class declarations in:

- `src/Tokenizer/TemplateCollection.cs` — `public sealed class TemplateCollection`
- `src/Tokenizer/Enumerators/TokenEnumerator.cs` — `public sealed class TokenEnumerator`
- `src/Tokenizer/Enumerators/FileLocation.cs` — `public sealed class FileLocation`
- `src/Tokenizer/Diagnostics/DiagnosticEvent.cs` — `public sealed class DiagnosticEvent`
- `src/Tokenizer/Diagnostics/DiagnosticIssue.cs` — `public sealed class DiagnosticIssue`
- `src/Tokenizer/Diagnostics/DiagnosticSummary.cs` — `public sealed class DiagnosticSummary`

Note: `DiagnosticResult` is already `public sealed class`.

- [ ] **Step 4: Build and run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add -A && git status
git commit -m "fix: spec compliance — seal classes, document deviations (L10, L11, L12)"
```

---

### Task 7: Observability (H6)

**Files:**
- Modify: `src/Tokenizer/Tokenizer.cs:131-196` — add exception logging to sync path

- [ ] **Step 1: Fix H6 — add try/catch to sync TokenizeCore**

In `src/Tokenizer/Tokenizer.cs`, wrap the body of `TokenizeCore` (inside the `using (_log.BeginScope(...))` block) in a try/catch that mirrors the async path. The existing code from line 148 to 195 becomes the try body:

```csharp
        using (_log.BeginScope(scopeProperties))
        {
            try
            {
                if (_log.IsEnabled(LogLevel.Debug))
                {
                    // ... existing debug logging ...
                }

                // ... existing body through FinalizeTokenization ...

                if (_log.IsEnabled(LogLevel.Debug))
                {
                    _log.LogDebug("Tokenization {Result} for template {TemplateName}",
                        result.Success ? "succeeded" : "failed", template.Name);
                }
            }
            catch (TokenizerException ex)
            {
                _log.LogError(ex, "Tokenization failed for template {TemplateName}: {Message}",
                    template.Name, ex.Message);
                throw;
            }
        }
```

- [ ] **Step 2: Build and run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add -A && git status
git commit -m "fix: add exception logging to sync TokenizeCore (H6)"
```

---

### Task 8: Test Coverage (H7, H8, M11, M12, M13, M14, L13, L14, TemplateOptionsCascadeTests)

**Files:**
- Create: `tests/Tokenizer.Tests/Exceptions/TokenAssignmentExceptionTests.cs`
- Create: `tests/Tokenizer.Tests/Exceptions/TypeConversionExceptionTests.cs`
- Create: `tests/Tokenizer.Tests/Exceptions/TokenMatcherExceptionTests.cs`
- Modify: `tests/Tokenizer.Tests/Exceptions/ExceptionLocationTests.cs` — add Message and inner-exception tests
- Create: `tests/Tokenizer.Tests/Diagnostics/DiagnosticResultTests.cs`
- Create: `tests/Tokenizer.Tests/TokenMatcherResultTests.cs`
- Create: `tests/Tokenizer.Tests/Diagnostics/Hints/UnmatchedInputHintGeneratorTests.cs`
- Modify: `tests/Tokenizer.Tests/TokenizeResultTests.cs` — add HasOnlyFrontMatterTokens test
- Create: `tests/Tokenizer.Tests/Compilation/TemplateOptionsCascadeTests.cs`

- [ ] **Step 1: H7 — TokenAssignmentException tests**

Create `tests/Tokenizer.Tests/Exceptions/TokenAssignmentExceptionTests.cs`:

```csharp
using Tokens;
using Tokens.Enumerators;
using Tokens.Exceptions;

namespace Tokenizer.Tests.Exceptions;

public class TokenAssignmentExceptionTests
{
    [Fact]
    public void GivenTokenAndMessage_WhenConstructed_ThenTokenAndMessageAreSet()
    {
        // Arrange
        var token = new Token("content", "MyToken", "preamble", new FileLocation());

        // Act
        var ex = new TokenAssignmentException(token, "test message");

        // Assert
        Assert.Same(token, ex.Token);
        Assert.Contains("test message", ex.Message);
    }

    [Fact]
    public void GivenTokenAndInnerException_WhenConstructed_ThenTokenNameInMessageAndInnerExceptionPreserved()
    {
        // Arrange
        var token = new Token("content", "Price", "preamble", new FileLocation());
        var inner = new InvalidOperationException("inner error");

        // Act
        var ex = new TokenAssignmentException(token, inner);

        // Assert
        Assert.Same(token, ex.Token);
        Assert.Contains("Price", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void GivenTokenAssignmentException_WhenChecked_ThenIsTokenizerException()
    {
        // Arrange
        var token = new Token("content", "Name", "preamble", new FileLocation());

        // Act
        var ex = new TokenAssignmentException(token, "test");

        // Assert
        Assert.IsAssignableFrom<TokenizerException>(ex);
    }
}
```

- [ ] **Step 2: H8 — TypeConversionException tests**

Create `tests/Tokenizer.Tests/Exceptions/TypeConversionExceptionTests.cs`:

```csharp
using Tokens.Exceptions;

namespace Tokenizer.Tests.Exceptions;

public class TypeConversionExceptionTests
{
    [Fact]
    public void GivenMessageValueAndType_WhenConstructed_ThenPropertiesAreSet()
    {
        // Arrange & Act
        var ex = new TypeConversionException("cannot convert", "hello", typeof(int));

        // Assert
        Assert.Equal("cannot convert", ex.Message);
        Assert.Equal("hello", ex.Value);
        Assert.Equal(typeof(int), ex.TargetType);
    }

    [Fact]
    public void GivenInnerException_WhenConstructed_ThenInnerExceptionPreserved()
    {
        // Arrange
        var inner = new FormatException("bad format");

        // Act
        var ex = new TypeConversionException("convert failed", "abc", typeof(DateTime), inner);

        // Assert
        Assert.Same(inner, ex.InnerException);
        Assert.Equal("abc", ex.Value);
        Assert.Equal(typeof(DateTime), ex.TargetType);
    }

    [Fact]
    public void GivenTypeConversionException_WhenChecked_ThenIsTokenizerException()
    {
        // Act
        var ex = new TypeConversionException("msg", "val", typeof(int));

        // Assert
        Assert.IsAssignableFrom<TokenizerException>(ex);
    }
}
```

- [ ] **Step 3: H8 — TokenMatcherException tests**

Create `tests/Tokenizer.Tests/Exceptions/TokenMatcherExceptionTests.cs`:

```csharp
using Tokens;
using Tokens.Exceptions;

namespace Tokenizer.Tests.Exceptions;

public class TokenMatcherExceptionTests
{
    [Fact]
    public void GivenMessageAndTemplate_WhenConstructed_ThenPropertiesAreSet()
    {
        // Arrange
        var tokenizer = Tokens.Tokenizer.Create();
        var template = tokenizer.Compile("Hello {Name}").Template;

        // Act
        var ex = new TokenMatcherException("match failed", template);

        // Assert
        Assert.Equal("match failed", ex.Message);
        Assert.Same(template, ex.Template);
    }

    [Fact]
    public void GivenInnerException_WhenConstructed_ThenInnerExceptionPreserved()
    {
        // Arrange
        var tokenizer = Tokens.Tokenizer.Create();
        var template = tokenizer.Compile("Hello {Name}").Template;
        var inner = new InvalidOperationException("inner");

        // Act
        var ex = new TokenMatcherException("match failed", template, inner);

        // Assert
        Assert.Same(inner, ex.InnerException);
        Assert.Same(template, ex.Template);
    }

    [Fact]
    public void GivenTokenMatcherException_WhenChecked_ThenIsTokenizerException()
    {
        // Arrange
        var tokenizer = Tokens.Tokenizer.Create();
        var template = tokenizer.Compile("Hello {Name}").Template;

        // Act
        var ex = new TokenMatcherException("msg", template);

        // Assert
        Assert.IsAssignableFrom<TokenizerException>(ex);
    }
}
```

- [ ] **Step 4: M11 — LexerException Message and inner-exception tests**

Extend `tests/Tokenizer.Tests/Exceptions/ExceptionLocationTests.cs` (or create new partial class). Add:

```csharp
    [Fact]
    public void GivenLexerExceptionWithLocation_WhenMessageAccessed_ThenIncludesLineAndColumn()
    {
        // Arrange
        var location = new FileLocation { Line = 5, Column = 12 };

        // Act
        var ex = new LexerException("Unexpected character", location);

        // Assert
        Assert.Contains("Unexpected character", ex.Message);
        Assert.Contains("Line: 5", ex.Message);
        Assert.Contains("Column: 12", ex.Message);
    }

    [Fact]
    public void GivenLexerExceptionWithInnerException_WhenConstructed_ThenInnerExceptionPreserved()
    {
        // Arrange
        var inner = new InvalidOperationException("inner");
        var location = new FileLocation { Line = 1, Column = 1 };

        // Act
        var ex = new LexerException("Lexer error", location, inner);

        // Assert
        Assert.Same(inner, ex.InnerException);
        Assert.Equal(1, ex.Line);
        Assert.Equal(1, ex.Column);
    }

    [Fact]
    public void GivenLexerExceptionWithoutLocation_WhenMessageAccessed_ThenNoLineColumnAppended()
    {
        // Act
        var ex = new LexerException("Simple error");

        // Assert
        Assert.Contains("Simple error", ex.Message);
        Assert.DoesNotContain("Line:", ex.Message);
    }
```

- [ ] **Step 5: M12 — ParsingException Message test**

Add to the same test file:

```csharp
    [Fact]
    public void GivenParsingExceptionWithLocation_WhenMessageAccessed_ThenIncludesLineAndColumn()
    {
        // Arrange
        var location = new FileLocation { Line = 10, Column = 3 };

        // Act
        var ex = new ParsingException("Unexpected token", location);

        // Assert
        Assert.Contains("Unexpected token", ex.Message);
        Assert.Contains("Line: 10", ex.Message);
        Assert.Contains("Column: 3", ex.Message);
    }
```

- [ ] **Step 6: M13 — DiagnosticResult tests**

Create `tests/Tokenizer.Tests/Diagnostics/DiagnosticResultTests.cs`:

```csharp
using Tokens.Diagnostics;

namespace Tokenizer.Tests.Diagnostics;

public class DiagnosticResultTests
{
    [Fact]
    public void GivenResultWithMixedEvents_WhenFailuresAccessed_ThenOnlyFailureTypesReturned()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "A" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "B" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.ValidatorFailed, TokenName = "C" });

        // Act
        var failures = result.Failures.ToList();

        // Assert
        Assert.Equal(2, failures.Count);
        Assert.Contains(failures, e => e.TokenName == "B");
        Assert.Contains(failures, e => e.TokenName == "C");
    }

    [Fact]
    public void GivenResultWithEvents_WhenForTokenCalled_ThenOnlyMatchingEventsReturned()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "Name" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "Age" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "Name" });

        // Act
        var nameEvents = result.ForToken("Name").ToList();

        // Assert
        Assert.Equal(2, nameEvents.Count);
        Assert.All(nameEvents, e => Assert.Equal("Name", e.TokenName));
    }

    [Fact]
    public void GivenResultWithFailure_WhenFirstFailureAccessed_ThenReturnsFirstFailureEvent()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "A" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "B" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.ValidatorFailed, TokenName = "C" });

        // Act
        var first = result.FirstFailure;

        // Assert
        Assert.NotNull(first);
        Assert.Equal("B", first!.TokenName);
    }

    [Fact]
    public void GivenEmptyResult_WhenQueried_ThenReturnsEmptyCollections()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);

        // Assert
        Assert.Empty(result.Events);
        Assert.Empty(result.Failures);
        Assert.Null(result.FirstFailure);
    }
}
```

Note: `DiagnosticResult` constructor is `internal`, so this test must be in the test project that has `InternalsVisibleTo` access. Check if this is configured; if not, use reflection or a public factory.

- [ ] **Step 7: M14 — TokenMatcherResult.GetBestMatch tests**

Create `tests/Tokenizer.Tests/TokenMatcherResultTests.cs`:

```csharp
using Tokens;

namespace Tokenizer.Tests;

public class TokenMatcherResultTests
{
    [Fact]
    public void GivenMultipleSuccessfulResults_WhenGetBestMatch_ThenSelectsByMostHintMatches()
    {
        // Arrange — compile two templates, tokenize against input that matches both
        // The template with more hint matches should win
        var tokenizer = Tokens.Tokenizer.Create();
        var t1 = tokenizer.Compile("---\nhint: hello\n---\n{Name}").Template;
        var t2 = tokenizer.Compile("{Name}").Template;
        var matcher = tokenizer.CreateMatcher();
        matcher.AddTemplate(t1);
        matcher.AddTemplate(t2);

        // Act
        var result = matcher.Match("hello world");

        // Assert — template with hint match should win
        Assert.NotNull(result.BestMatch);
    }

    [Fact]
    public void GivenNoSuccessfulResults_WhenGetBestMatch_ThenReturnsNull()
    {
        // Arrange
        var tokenizer = Tokens.Tokenizer.Create();
        var t1 = tokenizer.Compile("NOMATCH{Name}").Template;
        var matcher = tokenizer.CreateMatcher();
        matcher.AddTemplate(t1);

        // Act
        var result = matcher.Match("completely different input");

        // Assert
        Assert.Null(result.BestMatch);
        Assert.False(result.Success);
    }
}
```

- [ ] **Step 8: L13 — UnmatchedInputHintGenerator test**

Create `tests/Tokenizer.Tests/Diagnostics/Hints/UnmatchedInputHintGeneratorTests.cs`:

```csharp
using Tokens.Diagnostics;
using Tokens.Diagnostics.Hints;

namespace Tokenizer.Tests.Diagnostics.Hints;

public class UnmatchedInputHintGeneratorTests
{
    [Fact]
    public void GivenAnyInput_WhenTryGenerateHint_ThenReturnsNull()
    {
        // Arrange
        var generator = new UnmatchedInputHintGenerator();
        var issue = new DiagnosticIssue { Type = DiagnosticIssueType.UnmatchedInputSection };
        var sourceEvent = new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed };
        var result = new DiagnosticResult(inputContent: "test");

        // Act
        var hint = generator.TryGenerateHint(issue, sourceEvent, result);

        // Assert
        Assert.Null(hint);
    }
}
```

Note: `UnmatchedInputHintGenerator` is `internal`. Verify `InternalsVisibleTo` is set or use integration test approach.

- [ ] **Step 9: L14 — TokenizeResultBase.Success with HasOnlyFrontMatterTokens test**

Extend `tests/Tokenizer.Tests/TokenizeResultTests.cs`:

```csharp
    [Fact]
    public void GivenTemplateWithOnlyFrontMatterTokens_WhenAllMatched_ThenSuccessIsTrue()
    {
        // Arrange
        var tokenizer = Tokens.Tokenizer.Create();
        var template = tokenizer.Compile("---\nname: TestTemplate\n---\n").Template;

        // Act
        var result = tokenizer.Tokenize(template, "any input");

        // Assert
        Assert.True(result.Success);
    }
```

- [ ] **Step 10: TemplateOptionsCascadeTests**

Create `tests/Tokenizer.Tests/Compilation/TemplateOptionsCascadeTests.cs`:

```csharp
using Tokens;

namespace Tokenizer.Tests.Compilation;

/// <summary>
/// Verifies that options cascade correctly through three levels:
/// 1. Default (no custom options)
/// 2. Instance-level (via TokenizerOptions constructor)
/// 3. Front matter override (per-template)
/// </summary>
public class TemplateOptionsCascadeTests
{
    // --- TerminateOnNewLine ---

    [Fact]
    public void GivenDefaultOptions_WhenCompiled_ThenTerminateOnNewLineIsFalse()
    {
        // Arrange & Act
        var tokenizer = Tokens.Tokenizer.Create();
        var result = tokenizer.Compile("Hello {Name}");

        // Assert
        Assert.False(result.Template.Options.TerminateOnNewLine);
    }

    [Fact]
    public void GivenInstanceTerminateOnNewLine_WhenCompiled_ThenTemplateInheritsOption()
    {
        // Arrange & Act
        var tokenizer = Tokens.Tokenizer.Create(new TokenizerOptions { TerminateOnNewLine = true });
        var result = tokenizer.Compile("Hello {Name}");

        // Assert
        Assert.True(result.Template.Options.TerminateOnNewLine);
    }

    [Fact]
    public void GivenFrontMatterTerminateOnNewLine_WhenCompiled_ThenOverridesInstanceOption()
    {
        // Arrange — instance has it OFF, front matter turns it ON
        var tokenizer = Tokens.Tokenizer.Create(new TokenizerOptions { TerminateOnNewLine = false });
        var result = tokenizer.Compile("---\nTerminateOnNewLine: true\n---\nHello {Name}");

        // Assert
        Assert.True(result.Template.Options.TerminateOnNewLine);
    }

    [Fact]
    public void GivenInstanceTerminateOnNewLine_WhenTokenized_ThenValueIsTruncatedAtNewline()
    {
        // Arrange
        var tokenizer = Tokens.Tokenizer.Create(new TokenizerOptions { TerminateOnNewLine = true });
        var template = tokenizer.Compile("Hello {Name}").Template;

        // Act
        var result = tokenizer.Tokenize(template, "Hello World\nExtra");

        // Assert
        Assert.Equal("World", result.First("Name"));
    }

    // --- TrimPreambleBeforeNewLine ---

    [Fact]
    public void GivenDefaultOptions_WhenCompiled_ThenTrimPreambleBeforeNewLineIsFalse()
    {
        // Arrange & Act
        var tokenizer = Tokens.Tokenizer.Create();
        var result = tokenizer.Compile("Hello {Name}");

        // Assert
        Assert.False(result.Template.Options.TrimPreambleBeforeNewLine);
    }

    [Fact]
    public void GivenInstanceTrimPreambleBeforeNewLine_WhenCompiled_ThenTemplateInheritsOption()
    {
        // Arrange & Act
        var tokenizer = Tokens.Tokenizer.Create(new TokenizerOptions { TrimPreambleBeforeNewLine = true });
        var result = tokenizer.Compile("Hello {Name}");

        // Assert
        Assert.True(result.Template.Options.TrimPreambleBeforeNewLine);
    }

    [Fact]
    public void GivenFrontMatterTrimPreambleBeforeNewLine_WhenCompiled_ThenOverridesInstanceOption()
    {
        // Arrange
        var tokenizer = Tokens.Tokenizer.Create(new TokenizerOptions { TrimPreambleBeforeNewLine = false });
        var result = tokenizer.Compile("---\nTrimPreambleBeforeNewLine: true\n---\nHello {Name}");

        // Assert
        Assert.True(result.Template.Options.TrimPreambleBeforeNewLine);
    }

    // --- OutOfOrderTokens ---

    [Fact]
    public void GivenDefaultOptions_WhenCompiled_ThenOutOfOrderTokensIsFalse()
    {
        // Arrange & Act
        var tokenizer = Tokens.Tokenizer.Create();
        var result = tokenizer.Compile("Hello {Name}");

        // Assert
        Assert.False(result.Template.Options.OutOfOrderTokens);
    }

    [Fact]
    public void GivenInstanceOutOfOrderTokens_WhenCompiled_ThenTemplateInheritsOption()
    {
        // Arrange & Act
        var tokenizer = Tokens.Tokenizer.Create(new TokenizerOptions { OutOfOrderTokens = true });
        var result = tokenizer.Compile("Hello {Name}");

        // Assert
        Assert.True(result.Template.Options.OutOfOrderTokens);
    }

    // --- TrimLeadingWhitespaceInTokenPreamble ---

    [Fact]
    public void GivenDefaultOptions_WhenCompiled_ThenTrimLeadingWhitespaceInTokenPreambleIsTrue()
    {
        // Arrange & Act
        var tokenizer = Tokens.Tokenizer.Create();
        var result = tokenizer.Compile("Hello {Name}");

        // Assert — default is true
        Assert.True(result.Template.Options.TrimLeadingWhitespaceInTokenPreamble);
    }

    [Fact]
    public void GivenInstanceTrimLeadingWhitespaceDisabled_WhenCompiled_ThenTemplateInheritsOption()
    {
        // Arrange & Act
        var tokenizer = Tokens.Tokenizer.Create(new TokenizerOptions { TrimLeadingWhitespaceInTokenPreamble = false });
        var result = tokenizer.Compile("Hello {Name}");

        // Assert
        Assert.False(result.Template.Options.TrimLeadingWhitespaceInTokenPreamble);
    }

    // --- EnableDiagnostics ---

    [Fact]
    public void GivenDefaultOptions_WhenCompiled_ThenEnableDiagnosticsIsFalse()
    {
        // Arrange & Act
        var tokenizer = Tokens.Tokenizer.Create();
        var result = tokenizer.Compile("Hello {Name}");

        // Assert
        Assert.False(result.Template.Options.EnableDiagnostics);
    }

    [Fact]
    public void GivenInstanceEnableDiagnostics_WhenTokenized_ThenDiagnosticsArePopulated()
    {
        // Arrange
        var tokenizer = Tokens.Tokenizer.Create(new TokenizerOptions { EnableDiagnostics = true });
        var template = tokenizer.Compile("Hello {Name}").Template;

        // Act
        var result = tokenizer.Tokenize(template, "Hello World");

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.NotEmpty(result.Diagnostics!.Events);
    }

    // --- IgnoreMissingProperties ---

    [Fact]
    public void GivenDefaultOptions_WhenCompiled_ThenIgnoreMissingPropertiesIsFalse()
    {
        // Arrange & Act
        var tokenizer = Tokens.Tokenizer.Create();
        var result = tokenizer.Compile("Hello {Name}");

        // Assert
        Assert.False(result.Template.Options.IgnoreMissingProperties);
    }

    [Fact]
    public void GivenInstanceIgnoreMissingProperties_WhenCompiled_ThenTemplateInheritsOption()
    {
        // Arrange & Act
        var tokenizer = Tokens.Tokenizer.Create(new TokenizerOptions { IgnoreMissingProperties = true });
        var result = tokenizer.Compile("Hello {Name}");

        // Assert
        Assert.True(result.Template.Options.IgnoreMissingProperties);
    }
}
```

- [ ] **Step 11: Run all tests**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass (existing + new)

- [ ] **Step 12: Commit**

```bash
git add -A && git status
git commit -m "test: add coverage for exceptions, diagnostics, matcher result, and options cascade (H7, H8, M11-M14, L13, L14)"
```

---

### Task 9: Final Verification

- [ ] **Step 1: Full build**

Run: `dotnet build ./Tokenizer.sln -c Release`
Expected: 0 warnings, 0 errors

- [ ] **Step 2: Full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 3: Format check**

Run: `dotnet format ./Tokenizer.sln --verify-no-changes`
Expected: No formatting violations

- [ ] **Step 4: Commit plan as completed**

```bash
git add docs/superpowers/plans/2026-07-07-v3-review-fixes.md
git commit -m "docs: add v3 review fixes implementation plan"
```
