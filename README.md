# Tokenizer

[![Build Status](https://github.com/flipbit/tokenizer/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/flipbit/tokenizer/actions)
[![NuGet Version](https://img.shields.io/nuget/v/tokenizer.svg)](https://www.nuget.org/packages/Tokenizer/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/tokenizer.svg)](https://www.nuget.org/packages/Tokenizer/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)

A .NET library for extracting structured data from text. Define patterns with placeholders, and Tokenizer matches them against input to populate your .NET objects.

## Installation

```bash
dotnet add package Tokenizer --version 3.0.0-beta.2
```

Or add a PackageReference:

```xml
<PackageReference Include="Tokenizer" Version="3.0.0-beta.2" />
```

## Quick Start

Define a pattern with `{TokenName}` placeholders, and Tokenizer will match the surrounding text and pull values into your object's properties.

```csharp
var tokenizer = new Tokenizer();

var pattern = "Name: {Name}, Age: {Age:ToInt()}";
var input = "Name: Alice, Age: 30";

var template = tokenizer.Compile(pattern).Template;
var person = tokenizer.Tokenize<Person>(template, input);

Assert.Equal("Alice", person.Name);
Assert.Equal(30, person.Age);
```

Tokens work by matching the preceding text (the "preamble") in the input. When a match is found, everything after the preamble becomes the token's value, and extraction continues until the next preamble turns up or the input ends.

## Features

### In-Order vs Out-of-Order Processing

By default, tokens must appear in the order defined by the template. If you need them to match in any order, set `OutOfOrder: true` in front matter (or `OutOfOrderTokens = true` on `TokenizerOptions`). In-order mode also supports the `?` suffix to mark tokens as optional.

```csharp
var pattern =
@"---
OutOfOrder: false
---
First Name: {FirstName}
Middle Name: {MiddleName?}
Last Name: {LastName}";

var input =
@"First Name: Alice
Last Name: Smith";

var template = tokenizer.Compile(pattern).Template;
var student = tokenizer.Tokenize<Student>(template, input);

Assert.Equal("Alice", student.FirstName);
Assert.Null(student.MiddleName);
Assert.Equal("Smith", student.LastName);
```

### Multiline Tokens

Tokens can span multiple lines - a token's value extends until the next token's preamble is found.

```csharp
var pattern =
@"Comments:
{Comment:Trim()}

Name:
{Name}";

var input =
@"Comments:
10/10
Would parse text again.

Name:
Bob";

var template = tokenizer.Compile(pattern).Template;
var review = tokenizer.Tokenize<Review>(template, input);

Assert.Equal("10/10\nWould parse text again.", review.Comment);
Assert.Equal("Bob", review.Name);
```

### Newline Termination

Append `$` to a token name to terminate extraction at the end of the current line.

```csharp
var pattern = @"Name: {Name$}
Age: {Age:IsNumeric()}";

var input = @"Name: Bob
Surname: Jones
Age: 31";

var template = tokenizer.Compile(pattern).Template;
var person = tokenizer.Tokenize<Person>(template, input);

Assert.Equal("Bob", person.Name);  // Not "Bob\nSurname: Jones"
Assert.Equal(31, person.Age);
```

### Repeating Tokens

Append `*` to extract multiple values into a `List<>` or `IList<>` property. Repeating tokens are implicitly optional.

```csharp
var pattern =
@"Name: {Manager.Name}
Employee: {Manager.Manages*}
Number: {Manager.Number}";

var input =
@"Name: Sue
Employee: Alice
Employee: Bob
Employee: Charles
Number: 1234";

var template = tokenizer.Compile(pattern).Template;
var manager = tokenizer.Tokenize<Manager>(template, input);

Assert.Equal("Sue", manager.Name);
Assert.Equal(3, manager.Manages.Count);
Assert.Equal("Alice", manager.Manages[0]);
Assert.Equal(1234, manager.Number);
```

### Required and Optional Fields

Mark tokens as required with `!`. If a required token is missing, `Tokenize<T>` returns `null` (since `TokenizeResult.Success` will be `false`).

```csharp
var pattern = @"First Name: {FirstName!}, Last Name: {LastName!}";
var input = "First Name: Alice";

var template = tokenizer.Compile(pattern).Template;

// Tokenize<T> returns null when required tokens are missing
var student = tokenizer.Tokenize<Student>(template, input);
Assert.Null(student);

// Use the raw result to access partial matches
var result = tokenizer.Tokenize(template, input);
Assert.False(result.Success);
```

### Configuration

Options can be set per-instance or per-template via YAML front matter.

```csharp
// Per-instance
var tokenizer = new Tokenizer(new TokenizerOptions
{
    TrimTrailingWhiteSpace = false,
    OutOfOrderTokens = true
});

// Per-template front matter (overrides instance settings)
var pattern = @"---
TrimTrailingWhitespace: true
CaseSensitive: false
---
First Name: {FirstName}
Last Name: {LastName}";
```

Front matter is placed between `---` markers at the start of a template. Lines starting with `#` are comments.

### Data Transformers

You can transform extracted values before they're assigned. Chain multiple transformers with commas.

```csharp
var pattern = "Name: {Name:Trim(),ToLower()}";
var input = "Name:      Alice      ";

var template = tokenizer.Compile(pattern).Template;
var person = tokenizer.Tokenize<Person>(template, input);

Assert.Equal("alice", person.Name);
```

### Data Validators

Validators test extracted values. If validation fails, the engine skips the current match and keeps searching for the next one.

```csharp
var pattern = "Age: {Age:IsNumeric}";
var input = "Age: Ten, Age: 11";

var template = tokenizer.Compile(pattern).Template;
var person = tokenizer.Tokenize<Person>(template, input);

Assert.Equal(11, person.Age);
```

In this example, `"Ten"` fails the `IsNumeric` check, so the engine continues scanning and finds `"11"`.

### Template Compilation and Caching

You can compile templates once and reuse them across multiple tokenization calls.

```csharp
var compiled = tokenizer.Compile("Name: {Name}, Age: {Age:ToInt()}");

// Check for compilation errors
if (compiled.Errors.Any())
{
    // Handle errors
}

// Reuse the compiled template
var template = compiled.Template;
var person1 = tokenizer.Tokenize<Person>(template, input1);
var person2 = tokenizer.Tokenize<Person>(template, input2);
```

### Multi-Template Matching

Use `TemplateMatcher` to match input against multiple templates and select the best result.

```csharp
var matcher = new TemplateMatcher(tokenizer);
matcher.RegisterTemplate("Name: {Name}", "person");
matcher.RegisterTemplate("Order: {OrderId}", "order");

var person = matcher.Tokenize<Person>("Name: Alice");
Assert.Equal("Alice", person.Name);
```

### Diagnostics

You can enable structured diagnostics to trace how the engine processes each token - useful for debugging templates and understanding why tokens matched or missed.

```csharp
var tokenizer = new Tokenizer(new TokenizerOptions { EnableDiagnostics = true });
var result = tokenizer.Tokenize(template, input);

if (result.Diagnostics != null)
{
    foreach (var token in result.Diagnostics.Tokens)
    {
        // token.TokenName, token.Outcome (Matched/Rejected/NeverFound/Blocked)
        // token.AssignedValues, token.AssignedLocations
        // token.Attempts — every match consideration
        // token.Issues — problems with adaptive hints and stable TK codes
    }
}
```

Each `TokenDiagnostic` tells a token's complete story: its outcome, every match attempt, assigned values (with input locations), and any issues with contextual hints. Issue codes (TK001–TK008) are stable across versions for programmatic filtering. See [ARCHITECTURE.md](ARCHITECTURE.md#diagnostics-subsystem) for the full diagnostic model, hint generators, and renderers.

### Async and Streaming

You can compile and tokenize from streams or readers for large inputs.

```csharp
using var reader = new StreamReader(stream);
var template = (await tokenizer.CompileAsync(reader)).Template;
var person = await tokenizer.TokenizeAsync<Person>(template, reader);
```

### Dependency Injection

You can register Tokenizer services with the built-in DI container.

```csharp
// Default options
services.AddTokenizer();

// With explicit options
services.AddTokenizer(new TokenizerOptions { OutOfOrderTokens = true });

// From configuration section
services.AddTokenizer(configuration.GetSection("Tokenizer"));
```

This registers `ITokenizer`, `Tokenizer`, and `ITemplateMatcher` as singletons.

## Built-in Transformers

| Name | Description |
|------|-------------|
| `DefaultValue(fallback)` | Returns fallback when value is null or empty |
| `RegexReplace(pattern, replacement)` | Regex-based replacement |
| `Remove(text)` | Removes all occurrences of text |
| `RemoveEnd(text)` | Removes text from the end |
| `RemoveStart(text)` | Removes text from the start |
| `Replace(old, new)` | Replaces all occurrences |
| `Set(value)` | Replaces the extracted value entirely |
| `Split(delimiter)` | Splits into a list |
| `SubstringAfter(text)` | Text after first occurrence |
| `SubstringAfterLast(text)` | Text after last occurrence |
| `SubstringBefore(text)` | Text before first occurrence |
| `SubstringBeforeLast(text)` | Text before last occurrence |
| `TitleCase` | Converts to Title Case |
| `ToBoolean` | Converts to bool |
| `ToDate(format)` | Converts to DateOnly (NET 8+) |
| `ToDateTime(format)` | Converts to DateTimeOffset |
| `ToDecimal` | Converts to decimal |
| `ToGuid` | Converts to Guid |
| `ToInt` | Converts to int |
| `ToLower` | Converts to lowercase |
| `ToTime(format)` | Converts to TimeOnly (NET 8+) |
| `ToUpper` | Converts to uppercase |
| `Trim` | Trims whitespace |
| `Truncate(maxLength)` | Truncates to max length |

## Built-in Validators

| Name | Description |
|------|-------------|
| `Contains(text)` | Value contains text |
| `EndsWith(text)` | Value ends with text |
| `IsAlphanumeric` | Letters and digits only |
| `IsDate(format)` | Valid date (NET 8+) |
| `IsDateTime(format)` | Valid date/time |
| `IsDomainName` | Valid domain name |
| `IsEmail` | Valid email address |
| `IsGuid` | Valid GUID |
| `IsInRange(min, max)` | Numeric value in range |
| `IsInteger` | Valid integer |
| `IsIpAddress` | Valid IP address |
| `IsLooseAbsoluteUrl` | URL-like string (absolute) |
| `IsLooseUrl` | URL-like string |
| `IsNot(text)` | Value is not equal to text |
| `IsNotEmpty` | Non-empty value |
| `IsNumeric` | Valid number |
| `IsPhoneNumber` | Valid phone number |
| `IsTime(format)` | Valid time (NET 8+) |
| `IsUrl` | Valid URL |
| `MatchesRegex(pattern)` | Matches regex pattern |
| `MaxLength(n)` | At most n characters |
| `MinLength(n)` | At least n characters |
| `StartsWith(text)` | Value starts with text |

## Custom Transformers and Validators

To add your own, implement `ITokenTransformer` or `ITokenValidator` and register them via options.

```csharp
using Tokens.Transformers;

public sealed class ReverseTransformer : ITokenTransformer
{
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value is string s)
        {
            transformed = new string(s.Reverse().ToArray());
            return true;
        }

        transformed = value;
        return false;
    }
}

// Register it
var options = new TokenizerOptions()
    .WithTransformer<ReverseTransformer>();
var tokenizer = new Tokenizer(options);
```

Validators follow the same pattern with `ITokenValidator.IsValid(object value, params string[] args)`.

## Configuration Reference

You can set these on `TokenizerOptions` (constructor) or in template front matter (YAML between `---` markers).

| Option | Default | Description |
|--------|---------|-------------|
| `OutOfOrderTokens` | `false` | Allow tokens to match in any order |
| `TrimTrailingWhiteSpace` | `true` | Trim trailing whitespace from extracted values |
| `TrimLeadingWhitespaceInTokenPreamble` | `true` | Trim leading whitespace in the static text before a token |
| `TrimPreambleBeforeNewLine` | `false` | Discard preamble text that appears before a newline |
| `TerminateOnNewLine` | `false` | Extract token values up to the first newline only |
| `IgnoreMissingProperties` | `false` | Silently ignore tokens that do not map to a property on the target |
| `EnableDiagnostics` | `false` | Include structured diagnostic trace in results |
| `TokenStringComparison` | `InvariantCulture` | String comparison used for matching token names to properties |
| `MaxInputLength` | `1048576` | Maximum input length (0 to disable) |
| `MaxTemplateLength` | `65536` | Maximum template length (0 to disable) |
| `MaxTokenCount` | `500` | Maximum tokens per template (0 to disable) |
| `MaxIterations` | `0` (auto) | Maximum tokenization loop iterations |
| `AllowStreamBuffering` | `false` | Buffer non-seekable streams for multi-template matching |
| `Culture` | `null` | Culture for parsing date/time values |
| `DefaultOffset` | `null` | UTC offset for date/time values without offset info |
| `DefaultTimezone` | `null` | IANA/Windows timezone ID for date/time values without offset info |

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for a detailed overview of the compilation pipeline and tokenization engine.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on building, testing, and submitting changes.

## Security

For guidance on processing untrusted input (e.g. in a playground or SaaS feature), see [SECURITY.md](SECURITY.md).

To report a security vulnerability, please use [GitHub Security Advisories](https://github.com/flipbit/tokenizer/security/advisories/new).

## License

MIT. See [LICENSE.txt](LICENSE.txt).
