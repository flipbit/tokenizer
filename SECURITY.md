# Security Policy

## Reporting a Vulnerability

To report a security vulnerability, please use
[GitHub Security Advisories](https://github.com/flipbit/tokenizer/security/advisories/new).
Do not open a public issue for security vulnerabilities.

## Processing Untrusted Input

The Tokenizer library is designed for use with developer-defined templates and trusted input.
When processing untrusted templates or input (e.g. in a playground, SaaS feature, or
user-facing tool), apply the following mitigations.

### Recommended Configuration

Use reduced limits when processing untrusted input:

```csharp
var options = new TokenizerOptions
{
    MaxInputLength = 65_536,          // 64KB (default: 1MB)
    MaxTemplateLength = 8_192,        // 8KB (default: 64KB)
    MaxTokenCount = 50,               // default: 500
    MaxIterations = 200_000,          // explicit cap (default: auto-calculated)
    MaxRegexTimeout = TimeSpan.FromMilliseconds(250),  // default: 1 second
};
```

### Cancellation and Timeouts

Always use a `CancellationToken` with a bounded timeout to prevent long-running tokenizations:

```csharp
// Async path (preferred)
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
var result = await tokenizer.TokenizeAsync(template, reader, cts.Token);

// Sync path
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
var result = tokenizer.Tokenize(template, input, cts.Token);
```

### Instance Isolation

Do not share a `TemplateMatcher` across untrusted users — each user should get their own
instance to prevent cross-request state leakage via the template collection.
`TemplateMatcher` holds a mutable `Templates` collection that accumulates registered
templates; if an attacker can call `RegisterTemplate()`, they can inject templates visible
to other users of the same instance.

The `Tokenizer` class itself is safe to register as a singleton. All per-call state
(tokenization context, session, diagnostic collector) is created as local variables within
each `Tokenize` / `TokenizeAsync` call and is not stored on the instance. `TemplateCompiler`
maintains a decorator cache keyed by `Type`, but decorator instances are stateless value
processors — no input data is retained across calls.

### Diagnostics and PII

`EnableDiagnostics = true` is safe for use with untrusted input when combined with:

- **Instance-per-request isolation** — diagnostic state is not shared across requests
- **Log level at Info or higher** — diagnostic details are logged at Debug level and will not
  appear in production logs at Info+
- **No serialization of `TokenizeResult.Exceptions`** — exception messages may contain
  fragments of input text

Do not persist or cache `DiagnosticResult` objects across requests.

### Reflection Surface

When processing untrusted input, use the non-generic `Tokenize(template, input)` overload
and work with the returned `TokenizeResult.Tokens.Matches` directly. The generic
`Tokenize<T>()` overload uses reflection (`PropertyPathSetter`) to map values onto objects,
which is safe but exposes additional attack surface (property enumeration, type instantiation).

### Error Handling

Never return raw exception messages, stack traces, or `Exception.Data` to untrusted users.
Exception messages may contain:

- .NET type names and property names from the target model
- Fragments of input text from failed type conversions
- Internal configuration values (e.g. `MaxInputLength`)

Wrap all tokenization calls in a global exception handler and return only sanitized error
messages to the user.

### Log Level Configuration

Set the minimum log level for the `Tokens` namespace to `Information` or higher in production.
At `Debug` level, the library logs extracted token values and diagnostic alignment output.

### Template Validation

Template front matter can override certain behavioral options (e.g. `OutOfOrder: true`).
If you need to restrict these options for untrusted templates, validate the compiled
`template.Options` after compilation:

```csharp
var compiled = tokenizer.Compile(userTemplate);
if (compiled.Template.Options.OutOfOrderTokens)
{
    // Reject or override — OutOfOrderTokens increases processing cost significantly
}
```

### Rate Limiting

The library does not implement rate limiting. Apply per-user or per-IP request throttling at
the HTTP layer when exposing tokenization as a service.
