using System.Text;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Tokenization;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class CandidateTokenListTests : TokenizerTestBase
{
    private static readonly FileLocation NoLocation = new FileLocation();
    private static readonly TokenizerOptions DefaultOptions = new TokenizerOptions();
    private static readonly DecoratorPipeline DefaultPipeline = new DecoratorPipeline(DefaultOptions, NullTokenizationDiagnosticCollector.Instance);

    public CandidateTokenListTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenTokenWithPreamble_WhenAddingToList_ThenListContainsTokenAndSetsPreamble()
    {
        // Arrange
        var token = new Token(string.Empty, "bar", NoLocation);
        var list = new CandidateTokenList();

        // Act
        list.Add(token);

        // Assert
        Assert.Equal(1, list.Count);
        Assert.Equal("bar", list.Preamble);
    }

    // Add: first token sets Preamble, TerminateOnNewLine, IsNullToken

    [Fact]
    public void GivenTokenWithTerminateOnNewLine_WhenAddingAsFirstToken_ThenTerminateOnNewLineIsTrue()
    {
        // Arrange
        var token = new Token("Name", "pre", NoLocation);
        token.TerminateOnNewLine = true;
        var list = new CandidateTokenList();

        // Act
        list.Add(token);

        // Assert
        Assert.True(list.TerminateOnNewLine);
    }

    [Fact]
    public void GivenTokenWithBlankName_WhenAddingAsFirstToken_ThenIsNullTokenIsTrue()
    {
        // Arrange
        var token = new Token("   ", "pre", NoLocation);
        var list = new CandidateTokenList();

        // Act
        list.Add(token);

        // Assert
        Assert.True(list.IsNullToken);
    }

    [Fact]
    public void GivenTokenWithNonBlankName_WhenAddingAsFirstToken_ThenIsNullTokenIsFalse()
    {
        // Arrange
        var token = new Token("Name", "pre", NoLocation);
        var list = new CandidateTokenList();

        // Act
        list.Add(token);

        // Assert
        Assert.False(list.IsNullToken);
    }

    [Fact]
    public void GivenFirstTokenSetsProperties_WhenAddingSecondToken_ThenPropertiesAreNotOverridden()
    {
        // Arrange
        var first = new Token("First", "preamble-one", NoLocation);
        first.TerminateOnNewLine = true;
        var second = new Token("Second", "preamble-two", NoLocation);
        second.TerminateOnNewLine = false;
        var list = new CandidateTokenList();

        // Act
        list.Add(first);
        list.Add(second);

        // Assert
        Assert.Equal("preamble-one", list.Preamble);
        Assert.True(list.TerminateOnNewLine);
        Assert.Equal(2, list.Count);
    }

    // AddRange

    [Fact]
    public void GivenMultipleTokens_WhenAddingViaAddRange_ThenAllTokensAreAdded()
    {
        // Arrange
        var tokens = new[]
        {
            new Token("Name1", "pre1", NoLocation),
            new Token("Name2", "pre2", NoLocation),
            new Token("Name3", "pre3", NoLocation),
        };
        var list = new CandidateTokenList();

        // Act
        list.AddRange(tokens);

        // Assert
        Assert.Equal(3, list.Count);
        Assert.Equal("pre1", list.Preamble);
    }

    // Clear

    [Fact]
    public void GivenPopulatedList_WhenClearing_ThenPreambleIsReset()
    {
        // Arrange
        var token = new Token("Name", "preamble", NoLocation);
        var list = new CandidateTokenList();
        list.Add(token);

        // Act
        list.Clear();

        // Assert
        Assert.Equal(string.Empty, list.Preamble);
    }

    [Fact]
    public void GivenPopulatedList_WhenClearing_ThenTokensAreRemoved()
    {
        // Arrange
        var token = new Token("Name", "preamble", NoLocation);
        var list = new CandidateTokenList();
        list.Add(token);

        // Act
        list.Clear();

        // Assert
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void GivenListWithTerminateOnNewLine_WhenClearing_ThenTerminateOnNewLineIsReset()
    {
        // Arrange
        var token = new Token("Name", "preamble", NoLocation);
        token.TerminateOnNewLine = true;
        var list = new CandidateTokenList();
        list.Add(token);

        // Act
        list.Clear();

        // Assert
        Assert.False(list.TerminateOnNewLine);
    }

    [Fact]
    public void GivenListWithNullToken_WhenClearing_ThenIsNullTokenIsReset()
    {
        // Arrange
        var token = new Token("   ", "preamble", NoLocation);
        var list = new CandidateTokenList();
        list.Add(token);

        // Act
        list.Clear();

        // Assert
        Assert.False(list.IsNullToken);
    }

    // TryEvaluate: returns true when a candidate accepts the value

    [Fact]
    public void GivenTokenWithName_WhenTryEvaluateCalledWithValue_ThenReturnsTrueAndSetsEvaluated()
    {
        // Arrange
        var token = new Token("Name", "preamble", NoLocation);
        var list = new CandidateTokenList();
        list.Add(token);
        var value = new StringBuilder("hello");

        // Act
        var result = list.TryEvaluate(value, DefaultPipeline, NoLocation, out var evaluated, out var evaluatedValue);

        // Assert
        Assert.True(result);
        Assert.Same(token, evaluated);
        Assert.Equal("hello", evaluatedValue);
    }

    // TryEvaluate: returns false when no candidates match

    [Fact]
    public void GivenEmptyList_WhenTryEvaluateCalled_ThenReturnsFalse()
    {
        // Arrange
        var list = new CandidateTokenList();
        var value = new StringBuilder("hello");

        // Act
        var result = list.TryEvaluate(value, DefaultPipeline, NoLocation, out var evaluated, out var evaluatedValue);

        // Assert
        Assert.False(result);
        Assert.Null(evaluated);
        Assert.Null(evaluatedValue);
    }

    [Fact]
    public void GivenTokenWithBlankName_WhenTryEvaluateCalled_ThenReturnsFalse()
    {
        // Arrange
        var token = new Token("   ", "preamble", NoLocation);
        var list = new CandidateTokenList();
        list.Add(token);
        var value = new StringBuilder("hello");

        // Act
        var result = list.TryEvaluate(value, DefaultPipeline, NoLocation, out var evaluated, out _);

        // Assert
        Assert.False(result);
        Assert.Null(evaluated);
    }

    // CanAnyEvaluate: returns true when at least one token can accept

    [Fact]
    public void GivenTokenWithName_WhenCanAnyEvaluateCalledWithNonEmptyValue_ThenReturnsTrue()
    {
        // Arrange
        var token = new Token("Name", "preamble", NoLocation);
        var list = new CandidateTokenList();
        list.Add(token);

        // Act
        var result = list.CanAnyEvaluate("some value", DefaultPipeline);

        // Assert
        Assert.True(result);
    }

    // CanAnyEvaluate: returns false when no tokens match

    [Fact]
    public void GivenEmptyList_WhenCanAnyEvaluateCalled_ThenReturnsFalse()
    {
        // Arrange
        var list = new CandidateTokenList();

        // Act
        var result = list.CanAnyEvaluate("some value", DefaultPipeline);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenAnyToken_WhenCanAnyEvaluateCalledWithEmptyValue_ThenReturnsFalse()
    {
        // Arrange
        var token = new Token("Name", "preamble", NoLocation);
        var list = new CandidateTokenList();
        list.Add(token);

        // Act
        var result = list.CanAnyEvaluate(string.Empty, DefaultPipeline);

        // Assert
        Assert.False(result);
    }

    // HasCandidates

    [Fact]
    public void GivenListWithTokens_WhenCheckingHasCandidates_ThenReturnsTrue()
    {
        // Arrange
        var token = new Token("Name", "preamble", NoLocation);
        var list = new CandidateTokenList();
        list.Add(token);

        // Act & Assert
        Assert.True(list.HasCandidates);
    }

    [Fact]
    public void GivenEmptyList_WhenCheckingHasCandidates_ThenReturnsFalse()
    {
        // Arrange
        var list = new CandidateTokenList();

        // Act & Assert
        Assert.False(list.HasCandidates);
    }

    // Remove

    [Fact]
    public void GivenListWithToken_WhenTokenRemoved_ThenCountDecreases()
    {
        // Arrange
        var token = new Token("Name", "preamble", NoLocation);
        var list = new CandidateTokenList();
        list.Add(token);

        // Act
        list.Remove(token);

        // Assert
        Assert.Equal(0, list.Count);
        Assert.False(list.HasCandidates);
    }

    // Count

    [Fact]
    public void GivenEmptyList_WhenCheckingCount_ThenReturnsZero()
    {
        // Arrange
        var list = new CandidateTokenList();

        // Act & Assert
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void GivenListAfterAddAndClear_WhenCheckingCount_ThenReturnsZero()
    {
        // Arrange
        var token = new Token("Name", "preamble", NoLocation);
        var list = new CandidateTokenList();
        list.Add(token);
        list.Clear();

        // Act & Assert
        Assert.Equal(0, list.Count);
    }

    // TerminateOnNewLine: reflects first token's setting

    [Fact]
    public void GivenTokenWithTerminateOnNewLineFalse_WhenAddedAsFirstToken_ThenTerminateOnNewLineIsFalse()
    {
        // Arrange
        var token = new Token("Name", "preamble", NoLocation);
        token.TerminateOnNewLine = false;
        var list = new CandidateTokenList();

        // Act
        list.Add(token);

        // Assert
        Assert.False(list.TerminateOnNewLine);
    }

    // Edge cases: empty list TryAssign and CanAnyAssign already covered above.
    // Additional: clearing and re-adding should use new first token's properties.

    [Fact]
    public void GivenClearedList_WhenNewTokenAdded_ThenPropertiesReflectNewToken()
    {
        // Arrange
        var firstToken = new Token("FirstName", "first-preamble", NoLocation);
        firstToken.TerminateOnNewLine = true;
        var secondToken = new Token("SecondName", "second-preamble", NoLocation);
        secondToken.TerminateOnNewLine = false;
        var list = new CandidateTokenList();
        list.Add(firstToken);
        list.Clear();

        // Act
        list.Add(secondToken);

        // Assert
        Assert.Equal("second-preamble", list.Preamble);
        Assert.False(list.TerminateOnNewLine);
        Assert.False(list.IsNullToken);
        Assert.Equal(1, list.Count);
    }
}
