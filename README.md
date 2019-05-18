Tokenizer - Data Extraction Library
===================================

[![GitHub Stars](https://img.shields.io/github/stars/flipbit/tokenizer.svg)](https://github.com/flipbit/tokenizer/stargazers) [![GitHub Issues](https://img.shields.io/github/issues/flipbit/tokenizer.svg)](https://github.com/flipbit/tokenizer/issues) [![NuGet Version](https://img.shields.io/nuget/v/tokenizer.svg)](https://www.nuget.org/packages/Tokenizer/) [![NuGet Downloads](https://img.shields.io/nuget/dt/tokenizer.svg)](https://www.nuget.org/packages/Tokenizer/) 

Tokenizer is a .NET Standard and .NET Framework library that allows you to extract information from text using predefined patterns.  Tokens embedded within patterns are extracted, validated and transformed before being returned as a strongly typed object:

```csharp
var pattern = @"First Name: {FirstName}, Last Name: {LastName}, Enrolled: {Enrolled:ToDateTime('dd MMM yyyy')}";
var input = @"First Name: Alice, Last Name: Smith, Enrolled: 16 Jan 2018";

var tokenized = new Tokenizer().Tokenize<Student>(pattern, input);
var student = tokenized.Value;

Assert.AreEqual("Alice", student.FirstName);
Assert.AreEqual("Smith", student.LastName);
Assert.AreEqual(new DateTime(2018, 1, 16), student.Enrolled);
```

Tokens work by matching the preceding text (preamble) in your input.  When a match is found, the text after the preamble is taken and used to populate the token.  Text is taken up to a terminator, or until the next token begins.

## In Order Processing

Tokens can be processed either in the order they appear in the input pattern, or in any order.  If processing in order, a token can be marked as optional with the `?` suffix to allow matching to continue if it is not present in the input.

```csharp
var pattern = 
@"---
# Tokens must appear in defined order
OutOfOrder: false
---
First Name: {FirstName}
Middle Name: {MiddleName?}
Last Name: {LastName}";

var input = 
@"First Name: Alice
Last Name: Smith";

var tokenized = new Tokenizer().Tokenize<Student>(pattern, input);
var student = tokenized.Value;

Assert.AreEqual("Alice", student.FirstName);
Assert.IsNull(student.MiddleName);
Assert.AreEqual("Smith", student.LastName);
```

## Line Handling

Multiple tokens can appear on the same line of text, or tokens can span multiple lines of text if desired.  Windows and Unix line endings are automatically handled in patterns and input.

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

var tokenized = new Tokenizer().Tokenize<Review>(pattern, input);
var review = tokenized.Value;

Assert.AreEqual("10/10\nWould parse text again.", review.Comment);
Assert.AreEqual("Bob", review.Name);
```

## New Line Termination

When data is embedded in a single line, appending the `$` symbol to the end of the Token name will match to the end of the current line:

```csharp
var pattern = @"Name: {Name$}
Age: {Age:IsNumeric()}";

var input = @"Name: Bob
Surname: Jones
Age: 31";

var tokenized = new Tokenizer().Tokenize<Person>(pattern, input);
var person = tokenized.Value;

Assert.AreEqual(person.Name, "Bob");  // Not: "Bob\nSurname: Jones"
Assert.AreEqual(person.Age, 31);
```

## Repeating

Lists and repeating data elements can be extracted multiple by appending the `*` suffix to the token.  Tokenizer will populate an underlying `List<>` or `IList<>` on the target object. 

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

var tokenized = new Tokenizer().Tokenize<Manager>(pattern, input);
var manager = tokenized.Value;

Assert.AreEqual("Sue", manager.Name);
Assert.AreEqual(3, manager.Manages.Count);
Assert.AreEqual("Alice", manager.Manages[0]);
Assert.AreEqual("Bob", manager.Manages[1]);
Assert.AreEqual("Charles", manager.Manages[2]);
Assert.AreEqual(1234, manager.Number);
```

Repeating tokens are also treated as optional tokens.

## Required Values

Fields in the pattern that are non-optional can be marked with the `!` character.  If this is set and the field is not found in the input, then the `TokenizeResult.Success` property will be `false`.

```csharp
var pattern = @"First Name: {FirstName!}, Last Name: {LastName!}"
var input = "First Name: Alice";

var tokenized = new Tokenizer().Tokenize<Student>(pattern, input);
var student = tokenized.Value;

// LastName was not in input
Assert.IsFalse(tokenized.Success);

// Value will still be populated with available fields
Assert.AreEqual("Alice", student.FirstName);
```

Using required fields can be useful when matching multiple patterns to select the best matching one.

## Configuration

Tokenizer configuration can be set either globally, per instance or per pattern.

```csharp
// Global configuration
TokenizerOptions.Defaults.TrimTrailingWhiteSpace = false;

// Instance configuration
var tokenizer = new Tokenizer();
tokenizer.Options.TrimTrailingWhiteSpace = true;

// Front matter configuration
var pattern = @"---
# Trim Whitespace
TrimTrailingWhitespace: true
---
First Name: {FirstName}
Last Name: {LastName}
...";

```

### Configuration Front Matter

Tokenizer templates are configurable via an embedded Front Matter section.  The options set in the Front Matter will effect the parsing of that template only, and override both Global and instance settings.

The Front Matter section is optional.  It is processed between matching `---` sequences at the start of the template pattern.  Within the Front Matter, lines starting with the hash sign (`#`) are treated as comments.

```yaml
---
# Treat missing properties on the target object as exceptions
ThrowExceptionOnMissingProperty: true

# Do a case insensitive compare when matching tokens to property names on the target
CaseSensitive: false
---
First Name: {FirstName}
Middle Names: {MiddleNames*}
Last Name: {LastName}

```

Configuration directives and their effects are listed in the Wiki.

## Data Transformations

Extracted data can be transformed before being set on the target object.

```csharp
var pattern = "Name: {Name:Trim(),ToLower()}";
var input = "Name:      Alice      ";

var tokenized = new Tokenizer().Tokenize<Person>(pattern, input);
var person = tokenized.Value;

Assert.AreEqual(person.Name, "alice");
```
Multiple transformations (and validators) can be chained together using the `,` symbol and are executed in the order they are specified.  It is easy to implement and register your own token transformers by implementing the `ITokenTransformer` interface.  See the Wiki for details how, and a list of built in transformers and their usage.

## Data Validation

Token validation functions are run against extracted content before it's mapped to the target object.  If a validation returns false, then the token is not mapped, and the input content is searched for another match.

```csharp
var pattern = "Age: {Age:IsNumeric}";
var input = "Age: Ten, Age: 11";

var tokenized = new Tokenizer().Tokenize<Person>(pattern, input);
var person = tokenized.Value;

Assert.AreEqual(person.Age, 11);
```

It is easy to implement and register your own token validators by implementing the `ITokenValidator` interface.  See the Wiki for details how, and a list of built in validators and their usage.
