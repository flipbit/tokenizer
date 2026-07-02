# Tier 5: Missing Validators and Transformers — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add 14 new built-in validators and transformers to fill functional gaps in the decorator set.

**Architecture:** Each validator/transformer is a sealed class implementing `ITokenValidator` or `ITokenTransformer`, registered in `TokenParser`'s constructor. All follow the same null/empty guard → args check → logic → return pattern established by the existing 30+ decorators.

**Tech Stack:** C# / .NET, xUnit, no additional dependencies

---

## File Map

**New validator source files** (create in `src/Tokenizer/Validators/`):
- `IsAlphanumericValidator.cs`
- `IsIntegerValidator.cs`
- `IsGuidValidator.cs`
- `IsIpAddressValidator.cs`
- `IsInRangeValidator.cs`
- `MatchesRegexValidator.cs`

**New transformer source files** (create in `src/Tokenizer/Transformers/`):
- `ToIntTransformer.cs`
- `ToDecimalTransformer.cs`
- `ToBooleanTransformer.cs`
- `ToGuidTransformer.cs`
- `TruncateTransformer.cs`
- `DefaultValueTransformer.cs`
- `RegexReplaceTransformer.cs`
- `TitleCaseTransformer.cs`

**New test files** (create in `tests/Tokenizer.Tests/`):
- `Validators/IsAlphanumericValidatorTests.cs`
- `Validators/IsIntegerValidatorTests.cs`
- `Validators/IsGuidValidatorTests.cs`
- `Validators/IsIpAddressValidatorTests.cs`
- `Validators/IsInRangeValidatorTests.cs`
- `Validators/MatchesRegexValidatorTests.cs`
- `Transformers/ToIntTransformerTests.cs`
- `Transformers/ToDecimalTransformerTests.cs`
- `Transformers/ToBooleanTransformerTests.cs`
- `Transformers/ToGuidTransformerTests.cs`
- `Transformers/TruncateTransformerTests.cs`
- `Transformers/DefaultValueTransformerTests.cs`
- `Transformers/RegexReplaceTransformerTests.cs`
- `Transformers/TitleCaseTransformerTests.cs`

**Modified files:**
- `src/Tokenizer/Compilation/TokenParser.cs` — add registration calls

---

## Task 1: IsAlphanumericValidator

**Files:**
- Create: `src/Tokenizer/Validators/IsAlphanumericValidator.cs`
- Test: `tests/Tokenizer.Tests/Validators/IsAlphanumericValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Validators/IsAlphanumericValidatorTests.cs`:

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsAlphanumericValidatorTests : TokenizerTestBase
{
    public IsAlphanumericValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsAlphanumericValidator validator = new();

    [Fact]
    public void GivenAlphanumericString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "abc123";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenAlphaOnlyString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "abcdef";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNumericOnlyString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "123456";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenStringWithSpaces_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "abc 123";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenStringWithSpecialChars_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "abc-123!";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTemplateWithIsAlphanumericValidator_WhenInputIsAlphanumeric_ThenExtractsValue()
    {
        // Arrange
        var template = "Code: { Code : IsAlphanumeric }";
        var input = "Code: ABC123";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("ABC123", result.First("Code"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IsAlphanumericValidatorTests"`
Expected: Build failure — `IsAlphanumericValidator` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Validators/IsAlphanumericValidator.cs`:

```csharp
namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value contains only alphanumeric characters
/// </summary>
public sealed class IsAlphanumericValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        foreach (var c in valueString)
        {
            if (!char.IsLetterOrDigit(c)) return false;
        }

        return true;
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the existing `RegisterValidator<ContainsValidator>();` line (~line 75):

```csharp
RegisterValidator<IsAlphanumericValidator>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IsAlphanumericValidatorTests"`
Expected: All 8 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass, no regressions

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Validators/IsAlphanumericValidator.cs tests/Tokenizer.Tests/Validators/IsAlphanumericValidatorTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add IsAlphanumericValidator"
```

---

## Task 2: IsIntegerValidator

**Files:**
- Create: `src/Tokenizer/Validators/IsIntegerValidator.cs`
- Test: `tests/Tokenizer.Tests/Validators/IsIntegerValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Validators/IsIntegerValidatorTests.cs`:

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsIntegerValidatorTests : TokenizerTestBase
{
    public IsIntegerValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsIntegerValidator validator = new();

    [Fact]
    public void GivenIntegerString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "42";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNegativeIntegerString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "-100";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenLargeIntegerString_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "9223372036854775807"; // long.MaxValue

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenFloatString_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "10.5";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNonNumericString_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTemplateWithIsIntegerValidator_WhenInputIsInteger_ThenExtractsValue()
    {
        // Arrange
        var template = "Count: { Count : IsInteger }";
        var input = "Count: 42";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("42", result.First("Count"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IsIntegerValidatorTests"`
Expected: Build failure — `IsIntegerValidator` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Validators/IsIntegerValidator.cs`:

```csharp
using System.Globalization;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value is an integer
/// </summary>
public sealed class IsIntegerValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        return long.TryParse(valueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the `RegisterValidator<IsAlphanumericValidator>();` line:

```csharp
RegisterValidator<IsIntegerValidator>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IsIntegerValidatorTests"`
Expected: All 8 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Validators/IsIntegerValidator.cs tests/Tokenizer.Tests/Validators/IsIntegerValidatorTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add IsIntegerValidator"
```

---

## Task 3: IsGuidValidator

**Files:**
- Create: `src/Tokenizer/Validators/IsGuidValidator.cs`
- Test: `tests/Tokenizer.Tests/Validators/IsGuidValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Validators/IsGuidValidatorTests.cs`:

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsGuidValidatorTests : TokenizerTestBase
{
    public IsGuidValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsGuidValidator validator = new();

    [Fact]
    public void GivenValidGuidWithHyphens_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "d3b07384-d9a0-4e9b-8a0d-1e6b2a3c4d5e";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidGuidWithoutHyphens_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "d3b07384d9a04e9b8a0d1e6b2a3c4d5e";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidGuidWithBraces_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "{d3b07384-d9a0-4e9b-8a0d-1e6b2a3c4d5e}";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenInvalidGuid_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "not-a-guid";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTemplateWithIsGuidValidator_WhenInputIsGuid_ThenExtractsValue()
    {
        // Arrange
        var template = "ID: { Id : IsGuid }";
        var input = "ID: d3b07384-d9a0-4e9b-8a0d-1e6b2a3c4d5e";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("d3b07384-d9a0-4e9b-8a0d-1e6b2a3c4d5e", result.First("Id"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IsGuidValidatorTests"`
Expected: Build failure — `IsGuidValidator` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Validators/IsGuidValidator.cs`:

```csharp
namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value is a valid GUID
/// </summary>
public sealed class IsGuidValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        return Guid.TryParse(valueString, out _);
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the `RegisterValidator<IsIntegerValidator>();` line:

```csharp
RegisterValidator<IsGuidValidator>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IsGuidValidatorTests"`
Expected: All 7 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Validators/IsGuidValidator.cs tests/Tokenizer.Tests/Validators/IsGuidValidatorTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add IsGuidValidator"
```

---

## Task 4: IsIpAddressValidator

**Files:**
- Create: `src/Tokenizer/Validators/IsIpAddressValidator.cs`
- Test: `tests/Tokenizer.Tests/Validators/IsIpAddressValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Validators/IsIpAddressValidatorTests.cs`:

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsIpAddressValidatorTests : TokenizerTestBase
{
    public IsIpAddressValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsIpAddressValidator validator = new();

    [Fact]
    public void GivenValidIpv4Address_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "192.168.1.1";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidIpv6Address_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "::1";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValidFullIpv6Address_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenInvalidIpAddress_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "999.999.999.999";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNonIpString_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = validator.IsValid(input);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTemplateWithIsIpAddressValidator_WhenInputIsIpAddress_ThenExtractsValue()
    {
        // Arrange
        var template = "Server: { Ip : IsIpAddress }";
        var input = "Server: 10.0.0.1";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("10.0.0.1", result.First("Ip"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IsIpAddressValidatorTests"`
Expected: Build failure — `IsIpAddressValidator` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Validators/IsIpAddressValidator.cs`:

```csharp
using System.Net;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value is a valid IP address (IPv4 or IPv6)
/// </summary>
public sealed class IsIpAddressValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        return IPAddress.TryParse(valueString, out _);
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the `RegisterValidator<IsGuidValidator>();` line:

```csharp
RegisterValidator<IsIpAddressValidator>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IsIpAddressValidatorTests"`
Expected: All 8 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Validators/IsIpAddressValidator.cs tests/Tokenizer.Tests/Validators/IsIpAddressValidatorTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add IsIpAddressValidator"
```

---

## Task 5: IsInRangeValidator

**Files:**
- Create: `src/Tokenizer/Validators/IsInRangeValidator.cs`
- Test: `tests/Tokenizer.Tests/Validators/IsInRangeValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Validators/IsInRangeValidatorTests.cs`:

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class IsInRangeValidatorTests : TokenizerTestBase
{
    public IsInRangeValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly IsInRangeValidator validator = new();

    [Fact]
    public void GivenValueInRange_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "50";

        // Act
        var result = validator.IsValid(input, "1", "100");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValueAtMinBoundary_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "1";

        // Act
        var result = validator.IsValid(input, "1", "100");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValueAtMaxBoundary_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "100";

        // Act
        var result = validator.IsValid(input, "1", "100");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenValueBelowRange_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "0";

        // Act
        var result = validator.IsValid(input, "1", "100");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenValueAboveRange_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "101";

        // Act
        var result = validator.IsValid(input, "1", "100");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenDecimalValueInRange_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "50.5";

        // Act
        var result = validator.IsValid(input, "0.0", "100.0");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNegativeValueInRange_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "-5";

        // Act
        var result = validator.IsValid(input, "-10", "10");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNonNumericValue_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "abc";

        // Act
        var result = validator.IsValid(input, "1", "100");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(null!, "1", "100");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(string.Empty, "1", "100");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenMissingArgs_WhenValidating_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.IsValid("50"));
    }

    [Fact]
    public void GivenOnlyOneArg_WhenValidating_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.IsValid("50", "1"));
    }

    [Fact]
    public void GivenNonNumericMinArg_WhenValidating_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.IsValid("50", "abc", "100"));
    }

    [Fact]
    public void GivenNonNumericMaxArg_WhenValidating_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.IsValid("50", "1", "abc"));
    }

    [Fact]
    public void GivenTemplateWithIsInRangeValidator_WhenInputIsInRange_ThenExtractsValue()
    {
        // Arrange
        var template = "Age: { Age : IsInRange(1, 120) }";
        var input = "Age: 25";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("25", result.First("Age"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IsInRangeValidatorTests"`
Expected: Build failure — `IsInRangeValidator` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Validators/IsInRangeValidator.cs`:

```csharp
using System.Globalization;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value is within a numeric range (inclusive)
/// </summary>
public sealed class IsInRangeValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (args == null || args.Length < 2)
        {
            throw new ArgumentException("IsInRange(min, max): you must specify both min and max values");
        }

        if (!decimal.TryParse(args[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var min))
        {
            throw new ArgumentException($"IsInRange(min, max): min value '{args[0]}' is not a valid number");
        }

        if (!decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var max))
        {
            throw new ArgumentException($"IsInRange(min, max): max value '{args[1]}' is not a valid number");
        }

        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        if (!decimal.TryParse(valueString, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericValue))
        {
            return false;
        }

        return numericValue >= min && numericValue <= max;
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the `RegisterValidator<IsIpAddressValidator>();` line:

```csharp
RegisterValidator<IsInRangeValidator>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "IsInRangeValidatorTests"`
Expected: All 15 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Validators/IsInRangeValidator.cs tests/Tokenizer.Tests/Validators/IsInRangeValidatorTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add IsInRangeValidator"
```

---

## Task 6: MatchesRegexValidator

**Files:**
- Create: `src/Tokenizer/Validators/MatchesRegexValidator.cs`
- Test: `tests/Tokenizer.Tests/Validators/MatchesRegexValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Validators/MatchesRegexValidatorTests.cs`:

```csharp
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Validators;

public class MatchesRegexValidatorTests : TokenizerTestBase
{
    public MatchesRegexValidatorTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly MatchesRegexValidator validator = new();

    [Fact]
    public void GivenMatchingPattern_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "123-4567";

        // Act
        var result = validator.IsValid(input, @"^\d{3}-\d{4}$");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNonMatchingPattern_WhenValidating_ThenReturnsFalse()
    {
        // Arrange
        var input = "abc";

        // Act
        var result = validator.IsValid(input, @"^\d+$");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenPatternWithInlineCaseInsensitiveFlag_WhenValidating_ThenReturnsTrue()
    {
        // Arrange
        var input = "HELLO";

        // Act
        var result = validator.IsValid(input, @"(?i)^hello$");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNullValue_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(null!, @"\d+");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenEmptyString_WhenValidating_ThenReturnsFalse()
    {
        // Act
        var result = validator.IsValid(string.Empty, @"\d+");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenMissingArgs_WhenValidating_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => validator.IsValid("test"));
    }

    [Fact]
    public void GivenTemplateWithMatchesRegexValidator_WhenInputMatches_ThenExtractsValue()
    {
        // Arrange
        var template = @"Phone: { Phone : MatchesRegex(^\d{3}-\d{4}$) }";
        var input = "Phone: 555-1234";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("555-1234", result.First("Phone"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "MatchesRegexValidatorTests"`
Expected: Build failure — `MatchesRegexValidator` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Validators/MatchesRegexValidator.cs`:

```csharp
using System.Text.RegularExpressions;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value matches a regular expression pattern
/// </summary>
public sealed class MatchesRegexValidator : ITokenValidator
{
    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (args == null || args.Length == 0)
        {
            throw new ArgumentException("MatchesRegex(pattern): missing argument — you must specify a regex pattern");
        }

        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        return Regex.IsMatch(valueString, args[0]);
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the `RegisterValidator<IsInRangeValidator>();` line:

```csharp
RegisterValidator<MatchesRegexValidator>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "MatchesRegexValidatorTests"`
Expected: All 7 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Validators/MatchesRegexValidator.cs tests/Tokenizer.Tests/Validators/MatchesRegexValidatorTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add MatchesRegexValidator"
```

---

## Task 7: ToIntTransformer

**Files:**
- Create: `src/Tokenizer/Transformers/ToIntTransformer.cs`
- Test: `tests/Tokenizer.Tests/Transformers/ToIntTransformerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Transformers/ToIntTransformerTests.cs`:

```csharp
using Xunit;

namespace Tokens.Transformers;

public class ToIntTransformerTests
{
    private readonly ToIntTransformer transformer = new();

    [Fact]
    public void GivenValidIntegerString_WhenTransforming_ThenReturnsInt()
    {
        // Act
        var result = transformer.TryTransform("42", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<int>(transformed);
        Assert.Equal(42, transformed);
    }

    [Fact]
    public void GivenNegativeIntegerString_WhenTransforming_ThenReturnsNegativeInt()
    {
        // Act
        var result = transformer.TryTransform("-100", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(-100, transformed);
    }

    [Fact]
    public void GivenZeroString_WhenTransforming_ThenReturnsZero()
    {
        // Act
        var result = transformer.TryTransform("0", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(0, transformed);
    }

    [Fact]
    public void GivenFloatString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform("10.5", [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal("10.5", transformed);
    }

    [Fact]
    public void GivenNonNumericString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform("hello", [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal("hello", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform(null!, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Null(transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform(string.Empty, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenOverflowValue_WhenTransforming_ThenReturnsFalse()
    {
        // Arrange
        var input = "99999999999999999999";

        // Act
        var result = transformer.TryTransform(input, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal(input, transformed);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToIntTransformerTests"`
Expected: Build failure — `ToIntTransformer` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Transformers/ToIntTransformer.cs`:

```csharp
using System.Globalization;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to an <see cref="int"/>
/// </summary>
public sealed class ToIntTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = value;
            return false;
        }

        if (int.TryParse(valueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            transformed = result;
            return true;
        }

        transformed = value;
        return false;
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the existing `RegisterTransformer<SplitTransformer>();` line:

```csharp
RegisterTransformer<ToIntTransformer>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToIntTransformerTests"`
Expected: All 8 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Transformers/ToIntTransformer.cs tests/Tokenizer.Tests/Transformers/ToIntTransformerTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add ToIntTransformer"
```

---

## Task 8: ToDecimalTransformer

**Files:**
- Create: `src/Tokenizer/Transformers/ToDecimalTransformer.cs`
- Test: `tests/Tokenizer.Tests/Transformers/ToDecimalTransformerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Transformers/ToDecimalTransformerTests.cs`:

```csharp
using Xunit;

namespace Tokens.Transformers;

public class ToDecimalTransformerTests
{
    private readonly ToDecimalTransformer transformer = new();

    [Fact]
    public void GivenValidDecimalString_WhenTransforming_ThenReturnsDecimal()
    {
        // Act
        var result = transformer.TryTransform("123.45", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<decimal>(transformed);
        Assert.Equal(123.45m, transformed);
    }

    [Fact]
    public void GivenIntegerString_WhenTransforming_ThenReturnsDecimal()
    {
        // Act
        var result = transformer.TryTransform("42", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<decimal>(transformed);
        Assert.Equal(42m, transformed);
    }

    [Fact]
    public void GivenNegativeDecimalString_WhenTransforming_ThenReturnsNegativeDecimal()
    {
        // Act
        var result = transformer.TryTransform("-99.9", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(-99.9m, transformed);
    }

    [Fact]
    public void GivenNonNumericString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform("hello", [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal("hello", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform(null!, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Null(transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform(string.Empty, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal(string.Empty, transformed);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToDecimalTransformerTests"`
Expected: Build failure — `ToDecimalTransformer` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Transformers/ToDecimalTransformer.cs`:

```csharp
using System.Globalization;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="decimal"/>
/// </summary>
public sealed class ToDecimalTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = value;
            return false;
        }

        if (decimal.TryParse(valueString, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            transformed = result;
            return true;
        }

        transformed = value;
        return false;
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the `RegisterTransformer<ToIntTransformer>();` line:

```csharp
RegisterTransformer<ToDecimalTransformer>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToDecimalTransformerTests"`
Expected: All 6 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Transformers/ToDecimalTransformer.cs tests/Tokenizer.Tests/Transformers/ToDecimalTransformerTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add ToDecimalTransformer"
```

---

## Task 9: ToBooleanTransformer

**Files:**
- Create: `src/Tokenizer/Transformers/ToBooleanTransformer.cs`
- Test: `tests/Tokenizer.Tests/Transformers/ToBooleanTransformerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Transformers/ToBooleanTransformerTests.cs`:

```csharp
using Xunit;

namespace Tokens.Transformers;

public class ToBooleanTransformerTests
{
    private readonly ToBooleanTransformer transformer = new();

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("yes")]
    [InlineData("Yes")]
    [InlineData("YES")]
    [InlineData("1")]
    public void GivenTruthyString_WhenTransforming_ThenReturnsTrueBoolean(string input)
    {
        // Act
        var result = transformer.TryTransform(input, [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<bool>(transformed);
        Assert.True((bool)transformed);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("FALSE")]
    [InlineData("no")]
    [InlineData("No")]
    [InlineData("NO")]
    [InlineData("0")]
    public void GivenFalsyString_WhenTransforming_ThenReturnsFalseBoolean(string input)
    {
        // Act
        var result = transformer.TryTransform(input, [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<bool>(transformed);
        Assert.False((bool)transformed);
    }

    [Fact]
    public void GivenUnrecognizedString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform("maybe", [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal("maybe", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform(null!, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Null(transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform(string.Empty, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal(string.Empty, transformed);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToBooleanTransformerTests"`
Expected: Build failure — `ToBooleanTransformer` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Transformers/ToBooleanTransformer.cs`:

```csharp
namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="bool"/>
/// </summary>
public sealed class ToBooleanTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = value;
            return false;
        }

        if (valueString.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            valueString.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            valueString == "1")
        {
            transformed = true;
            return true;
        }

        if (valueString.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            valueString.Equals("no", StringComparison.OrdinalIgnoreCase) ||
            valueString == "0")
        {
            transformed = false;
            return true;
        }

        transformed = value;
        return false;
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the `RegisterTransformer<ToDecimalTransformer>();` line:

```csharp
RegisterTransformer<ToBooleanTransformer>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToBooleanTransformerTests"`
Expected: All 5 tests pass (Theory tests count as multiple)

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Transformers/ToBooleanTransformer.cs tests/Tokenizer.Tests/Transformers/ToBooleanTransformerTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add ToBooleanTransformer"
```

---

## Task 10: ToGuidTransformer

**Files:**
- Create: `src/Tokenizer/Transformers/ToGuidTransformer.cs`
- Test: `tests/Tokenizer.Tests/Transformers/ToGuidTransformerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Transformers/ToGuidTransformerTests.cs`:

```csharp
using Xunit;

namespace Tokens.Transformers;

public class ToGuidTransformerTests
{
    private readonly ToGuidTransformer transformer = new();

    [Fact]
    public void GivenValidGuidString_WhenTransforming_ThenReturnsGuid()
    {
        // Arrange
        var input = "d3b07384-d9a0-4e9b-8a0d-1e6b2a3c4d5e";

        // Act
        var result = transformer.TryTransform(input, [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<Guid>(transformed);
        Assert.Equal(Guid.Parse(input), transformed);
    }

    [Fact]
    public void GivenGuidWithoutHyphens_WhenTransforming_ThenReturnsGuid()
    {
        // Arrange
        var input = "d3b07384d9a04e9b8a0d1e6b2a3c4d5e";

        // Act
        var result = transformer.TryTransform(input, [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.IsType<Guid>(transformed);
    }

    [Fact]
    public void GivenInvalidGuidString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform("not-a-guid", [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal("not-a-guid", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform(null!, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Null(transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsFalse()
    {
        // Act
        var result = transformer.TryTransform(string.Empty, [], out var transformed);

        // Assert
        Assert.False(result);
        Assert.Equal(string.Empty, transformed);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToGuidTransformerTests"`
Expected: Build failure — `ToGuidTransformer` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Transformers/ToGuidTransformer.cs`:

```csharp
namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="Guid"/>
/// </summary>
public sealed class ToGuidTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = value;
            return false;
        }

        if (Guid.TryParse(valueString, out var result))
        {
            transformed = result;
            return true;
        }

        transformed = value;
        return false;
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the `RegisterTransformer<ToBooleanTransformer>();` line:

```csharp
RegisterTransformer<ToGuidTransformer>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "ToGuidTransformerTests"`
Expected: All 5 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Transformers/ToGuidTransformer.cs tests/Tokenizer.Tests/Transformers/ToGuidTransformerTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add ToGuidTransformer"
```

---

## Task 11: TruncateTransformer

**Files:**
- Create: `src/Tokenizer/Transformers/TruncateTransformer.cs`
- Test: `tests/Tokenizer.Tests/Transformers/TruncateTransformerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Transformers/TruncateTransformerTests.cs`:

```csharp
using Xunit;

namespace Tokens.Transformers;

public class TruncateTransformerTests
{
    private readonly TruncateTransformer transformer = new();

    [Fact]
    public void GivenStringLongerThanMaxLength_WhenTransforming_ThenTruncates()
    {
        // Act
        var result = transformer.TryTransform("hello world", ["5"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hello", transformed);
    }

    [Fact]
    public void GivenStringShorterThanMaxLength_WhenTransforming_ThenReturnsUnchanged()
    {
        // Act
        var result = transformer.TryTransform("hi", ["10"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hi", transformed);
    }

    [Fact]
    public void GivenStringEqualToMaxLength_WhenTransforming_ThenReturnsUnchanged()
    {
        // Act
        var result = transformer.TryTransform("hello", ["5"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hello", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsEmptyString()
    {
        // Act
        var result = transformer.TryTransform(null!, ["5"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Act
        var result = transformer.TryTransform(string.Empty, ["5"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenMissingArgs_WhenTransforming_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformer.TryTransform("hello", null!, out var t));
    }

    [Fact]
    public void GivenNonIntegerArg_WhenTransforming_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformer.TryTransform("hello", ["abc"], out var t));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TruncateTransformerTests"`
Expected: Build failure — `TruncateTransformer` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Transformers/TruncateTransformer.cs`:

```csharp
namespace Tokens.Transformers;

/// <summary>
/// Truncates the token value to a maximum length
/// </summary>
public sealed class TruncateTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length == 0)
        {
            throw new ArgumentException($"Truncate(maxLength): missing argument processing: {value}");
        }

        try
        {
            var maxLength = Convert.ToInt32(args[0]);

            transformed = valueString.Length <= maxLength
                ? valueString
                : valueString[..maxLength];

            return true;
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Truncate parameter must be an integer", ex);
        }
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the `RegisterTransformer<ToGuidTransformer>();` line:

```csharp
RegisterTransformer<TruncateTransformer>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TruncateTransformerTests"`
Expected: All 7 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Transformers/TruncateTransformer.cs tests/Tokenizer.Tests/Transformers/TruncateTransformerTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add TruncateTransformer"
```

---

## Task 12: DefaultValueTransformer

**Files:**
- Create: `src/Tokenizer/Transformers/DefaultValueTransformer.cs`
- Test: `tests/Tokenizer.Tests/Transformers/DefaultValueTransformerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Transformers/DefaultValueTransformerTests.cs`:

```csharp
using Xunit;

namespace Tokens.Transformers;

public class DefaultValueTransformerTests
{
    private readonly DefaultValueTransformer transformer = new();

    [Fact]
    public void GivenNonEmptyValue_WhenTransforming_ThenReturnsOriginalValue()
    {
        // Act
        var result = transformer.TryTransform("hello", ["fallback"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hello", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsFallback()
    {
        // Act
        var result = transformer.TryTransform(null!, ["N/A"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("N/A", transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsFallback()
    {
        // Act
        var result = transformer.TryTransform(string.Empty, ["default"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("default", transformed);
    }

    [Fact]
    public void GivenWhitespaceOnlyString_WhenTransforming_ThenReturnsWhitespace()
    {
        // Act
        var result = transformer.TryTransform("   ", ["fallback"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("   ", transformed);
    }

    [Fact]
    public void GivenMissingArgs_WhenTransforming_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformer.TryTransform(null!, null!, out var t));
    }

    [Fact]
    public void GivenEmptyArgs_WhenTransforming_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformer.TryTransform(null!, [], out var t));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DefaultValueTransformerTests"`
Expected: Build failure — `DefaultValueTransformer` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Transformers/DefaultValueTransformer.cs`:

```csharp
namespace Tokens.Transformers;

/// <summary>
/// Returns a fallback value when the token value is null or empty
/// </summary>
public sealed class DefaultValueTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (args == null || args.Length == 0)
        {
            throw new ArgumentException("DefaultValue(fallback): missing argument — you must specify a fallback value");
        }

        var valueString = value?.ToString();

        if (string.IsNullOrEmpty(valueString))
        {
            transformed = args[0];
            return true;
        }

        transformed = value;
        return true;
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the `RegisterTransformer<TruncateTransformer>();` line:

```csharp
RegisterTransformer<DefaultValueTransformer>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "DefaultValueTransformerTests"`
Expected: All 6 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Transformers/DefaultValueTransformer.cs tests/Tokenizer.Tests/Transformers/DefaultValueTransformerTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add DefaultValueTransformer"
```

---

## Task 13: RegexReplaceTransformer

**Files:**
- Create: `src/Tokenizer/Transformers/RegexReplaceTransformer.cs`
- Test: `tests/Tokenizer.Tests/Transformers/RegexReplaceTransformerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Transformers/RegexReplaceTransformerTests.cs`:

```csharp
using Xunit;

namespace Tokens.Transformers;

public class RegexReplaceTransformerTests
{
    private readonly RegexReplaceTransformer transformer = new();

    [Fact]
    public void GivenMatchingPattern_WhenTransforming_ThenReplacesMatches()
    {
        // Act
        var result = transformer.TryTransform("abc123def456", [@"\d+", "#"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("abc#def#", transformed);
    }

    [Fact]
    public void GivenNonMatchingPattern_WhenTransforming_ThenReturnsOriginal()
    {
        // Act
        var result = transformer.TryTransform("hello", [@"\d+", "#"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hello", transformed);
    }

    [Fact]
    public void GivenPatternWithCaptureGroup_WhenTransforming_ThenUsesGroupInReplacement()
    {
        // Act
        var result = transformer.TryTransform("2026-07-02", [@"(\d{4})-(\d{2})-(\d{2})", "$2/$3/$1"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("07/02/2026", transformed);
    }

    [Fact]
    public void GivenPatternWithInlineCaseFlag_WhenTransforming_ThenRespectsCaseFlag()
    {
        // Act
        var result = transformer.TryTransform("Hello HELLO hello", ["(?i)hello", "hi"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("hi hi hi", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsEmptyString()
    {
        // Act
        var result = transformer.TryTransform(null!, [@"\d+", "#"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Act
        var result = transformer.TryTransform(string.Empty, [@"\d+", "#"], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenMissingArgs_WhenTransforming_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformer.TryTransform("hello", null!, out var t));
    }

    [Fact]
    public void GivenOnlyOneArg_WhenTransforming_ThenThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformer.TryTransform("hello", [@"\d+"], out var t));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "RegexReplaceTransformerTests"`
Expected: Build failure — `RegexReplaceTransformer` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Transformers/RegexReplaceTransformer.cs`:

```csharp
using System.Text.RegularExpressions;

namespace Tokens.Transformers;

/// <summary>
/// Replaces occurrences matching a regular expression pattern
/// </summary>
public sealed class RegexReplaceTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        if (args == null || args.Length < 2)
        {
            throw new ArgumentException($"RegexReplace(pattern, replacement): missing arguments processing: {value}");
        }

        transformed = Regex.Replace(valueString, args[0], args[1]);

        return true;
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the `RegisterTransformer<DefaultValueTransformer>();` line:

```csharp
RegisterTransformer<RegexReplaceTransformer>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "RegexReplaceTransformerTests"`
Expected: All 8 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Transformers/RegexReplaceTransformer.cs tests/Tokenizer.Tests/Transformers/RegexReplaceTransformerTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add RegexReplaceTransformer"
```

---

## Task 14: TitleCaseTransformer

**Files:**
- Create: `src/Tokenizer/Transformers/TitleCaseTransformer.cs`
- Test: `tests/Tokenizer.Tests/Transformers/TitleCaseTransformerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Tokenizer.Tests/Transformers/TitleCaseTransformerTests.cs`:

```csharp
using Xunit;

namespace Tokens.Transformers;

public class TitleCaseTransformerTests
{
    private readonly TitleCaseTransformer transformer = new();

    [Fact]
    public void GivenLowercaseString_WhenTransforming_ThenReturnsTitleCase()
    {
        // Act
        var result = transformer.TryTransform("hello world", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("Hello World", transformed);
    }

    [Fact]
    public void GivenUppercaseString_WhenTransforming_ThenReturnsTitleCase()
    {
        // Act
        var result = transformer.TryTransform("HELLO WORLD", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("Hello World", transformed);
    }

    [Fact]
    public void GivenMixedCaseString_WhenTransforming_ThenReturnsTitleCase()
    {
        // Act
        var result = transformer.TryTransform("hELLO wORLD", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("Hello World", transformed);
    }

    [Fact]
    public void GivenSingleWord_WhenTransforming_ThenCapitalizesFirstLetter()
    {
        // Act
        var result = transformer.TryTransform("hello", [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("Hello", transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsEmptyString()
    {
        // Act
        var result = transformer.TryTransform(null!, [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Act
        var result = transformer.TryTransform(string.Empty, [], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TitleCaseTransformerTests"`
Expected: Build failure — `TitleCaseTransformer` does not exist

- [ ] **Step 3: Write the implementation**

Create `src/Tokenizer/Transformers/TitleCaseTransformer.cs`:

```csharp
using System.Globalization;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to title case
/// </summary>
public sealed class TitleCaseTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        transformed = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(valueString.ToLowerInvariant());

        return true;
    }
}
```

- [ ] **Step 4: Register in TokenParser**

In `src/Tokenizer/Compilation/TokenParser.cs`, add after the `RegisterTransformer<RegexReplaceTransformer>();` line:

```csharp
RegisterTransformer<TitleCaseTransformer>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj --filter "TitleCaseTransformerTests"`
Expected: All 6 tests pass

- [ ] **Step 6: Run full test suite**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Tokenizer/Transformers/TitleCaseTransformer.cs tests/Tokenizer.Tests/Transformers/TitleCaseTransformerTests.cs src/Tokenizer/Compilation/TokenParser.cs
git commit -m "Add TitleCaseTransformer"
```

---

## Task 15: Update ROADMAP.md

**Files:**
- Modify: `docs/ROADMAP.md`

- [ ] **Step 1: Mark all Tier 5 items as complete**

In `docs/ROADMAP.md`, change each `- [ ]` checkbox in the Tier 5 section to `- [x]`.

- [ ] **Step 2: Run full test suite one final time**

Run: `dotnet test ./tests/Tokenizer.Tests/Tokenizer.Tests.csproj`
Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add docs/ROADMAP.md
git commit -m "Update ROADMAP.md: mark Tier 5 items complete"
```
