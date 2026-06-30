using System;
using Tokens.Builders;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tests.Tokenization.HintProcessorTests;

/// <summary>
/// Tests for HintProcessor error handling and validation
/// </summary>
public class HintProcessorErrorTests
{
    private readonly Tokens.Tokenization.HintProcessor _processor = new();

    [Fact]
    public void GivenNullTemplate_WhenFindAndValidateHints_ThenThrowsException()
    {
        // Arrange
        var enumerator = new TokenEnumerator("test");
        var result = new TokenizeResultBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _processor.FindAndValidateHints(null!, enumerator, result));
    }

    [Fact]
    public void GivenNullEnumerator_WhenFindAndValidateHints_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder().Build();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _processor.FindAndValidateHints(template, null!, result));
    }

    [Fact]
    public void GivenNullResult_WhenFindAndValidateHints_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder().Build();
        var enumerator = new TokenEnumerator("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _processor.FindAndValidateHints(template, enumerator, null!));
    }
}
