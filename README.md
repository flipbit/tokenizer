Tokenizer - Data Extraction Library
===================================

[![GitHub Stars](https://img.shields.io/github/stars/flipbit/tokenizer.svg)](https://github.com/flipbit/tokenizer/stargazers) [![GitHub Issues](https://img.shields.io/github/issues/flipbit/tokenizer.svg)](https://github.com/flipbit/tokenizer/issues) [![NuGet Version](https://img.shields.io/nuget/v/tokenizer.svg)](https://www.nuget.org/packages/Tokenizer/) [![NuGet Downloads](https://img.shields.io/nuget/dt/tokenizer.svg)](https://www.nuget.org/packages/Tokenizer/) 

Tokenizer is a .NET Standard and .NET Framework library that allows you to extract information from free form text using predefined patterns.  Tokens embedded within patterns are extracted, validated and transformed before being returned as a strongly typed object:

```csharp
var pattern = @"First Name: {FirstName}, Last Name: {LastName}, Enrolled: {Enrolled:ToDateTime('dd MMM yyyy')}";
var input = @"First Name: Alice, Last Name: Smith, Enrolled: 16 Jan 2018";

var student = new Tokenizer().Parse<Student>(pattern, input);

Assert.AreEqual("Alice", student.FirstName);
Assert.AreEqual("Smith", student.LastName);
Assert.AreEqual(new DateTime(2018, 1, 16), student.Enrolled);
```

Tokens work by matching the preceding text (preamble) in your input.  When a match is found, the text after the preamble is taken and used to populate the token.  Text is taken up to a terminator, or until the next token begins.

## In Order Processing

Tokens can be processed in strict order according to the input pattern, or in any order.  If processing in order, a token can be marked as optional with the `?` suffix to allow matching to continue if it is not present in the input.

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

var student = new Tokenizer().Parse<Student>(pattern, input);

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

var review = new Tokenizer().Parse<Review>(pattern, input);

Assert.AreEqual("10/10\nWould parse text again.", review.Comment);
Assert.AreEqual("Bob", review.Name);
```

## Repeating

Lists and repeating data elements can be extracted from data 1 or more times by appending the `*` suffix to the token.  Tokenizer will populate an underlying `List<>` or `IList<>` on the target object. 

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

var result = new Tokenizer().Parse<Manager>(pattern, input);

Assert.AreEqual("Sue", result.Name);
Assert.AreEqual(3, result.Manages.Count);
Assert.AreEqual("Alice", result.Manages[0]);
Assert.AreEqual("Bob", result.Manages[1]);
Assert.AreEqual("Charles", result.Manages[2]);
Assert.AreEqual(1234, result.Number);
```

Repeating tokens are also treated as optional tokens.
