# Tokenizer

<p align="center">
  <img src="docs/icon.svg" alt="Tokenizer" width="128" height="128" />
</p>

[![Build Status](https://github.com/flipbit/tokenizer/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/flipbit/tokenizer/actions)
[![NuGet Version](https://img.shields.io/nuget/v/tokenizer.svg)](https://www.nuget.org/packages/Tokenizer/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/tokenizer.svg)](https://www.nuget.org/packages/Tokenizer/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/flipbit/tokenizer/blob/main/LICENSE.txt)
[![Docs](https://img.shields.io/badge/docs-pullpatchpush.com-blue.svg)](https://pullpatchpush.com/tokenizer)

A .NET library for extracting structured data from text. Define patterns with placeholders, and Tokenizer matches them against input to populate your .NET objects.

## Documentation

For full documentation and an [interactive playground](https://pullpatchpush.com/tokenizer/playground), visit [pullpatchpush.com/tokenizer](https://pullpatchpush.com/tokenizer).

## Installation

```bash
dotnet add package Tokenizer --version 3.0.0
```

Or add a PackageReference:

```xml
<PackageReference Include="Tokenizer" Version="3.0.0" />
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

Each `TokenDiagnostic` tells a token's complete story: its outcome, every match attempt, assigned values (with input locations), and any issues with contextual hints. Issue codes (TK001–TK008) are stable across versions for programmatic filtering. See [ARCHITECTURE.md](https://github.com/flipbit/tokenizer/blob/main/ARCHITECTURE.md#diagnostics-subsystem) for the full diagnostic model, hint generators, and renderers.

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

## Built-in Transformers and Validators

Tokenizer ships with transformers for type conversion (`ToInt`, `ToDateTime`, `ToBoolean`), string manipulation (`Trim`, `Replace`, `Split`), and validators for format checking (`IsNumeric`, `IsEmail`, `IsUrl`). See the full [Transformers](https://pullpatchpush.com/tokenizer/transformers) and [Validators](https://pullpatchpush.com/tokenizer/validators) reference.

## Custom Transformers and Validators

Implement `ITokenTransformer` or `ITokenValidator` and register via options:

```csharp
var options = new TokenizerOptions()
    .WithTransformer<ReverseTransformer>()
    .WithValidator<IsUpperCaseValidator>();
var tokenizer = new Tokenizer(options);
```

See the [Extensibility](https://pullpatchpush.com/tokenizer/extensibility) guide for full examples.

## Configuration Reference

Options can be set per-instance via `TokenizerOptions` or per-template via YAML front matter. See the [Configuration](https://pullpatchpush.com/tokenizer/configuration) reference for the full list of options and directives.

## Architecture

See [ARCHITECTURE.md](https://github.com/flipbit/tokenizer/blob/main/ARCHITECTURE.md) for a detailed overview of the compilation pipeline and tokenization engine.

## Contributing

See [CONTRIBUTING.md](https://github.com/flipbit/tokenizer/blob/main/CONTRIBUTING.md) for guidelines on building, testing, and submitting changes.

## Security

For guidance on processing untrusted input (e.g. in a playground or SaaS feature), see [SECURITY.md](https://github.com/flipbit/tokenizer/blob/main/SECURITY.md).

To report a security vulnerability, please use [GitHub Security Advisories](https://github.com/flipbit/tokenizer/security/advisories/new).

## License

MIT. See [LICENSE.txt](https://github.com/flipbit/tokenizer/blob/main/LICENSE.txt).
