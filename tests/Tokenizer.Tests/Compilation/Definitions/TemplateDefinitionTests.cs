using System.Collections.Generic;
using System.Linq;
using Tokens.Compilation.Definitions;
using Xunit;

namespace Tokens.Tests.Compilation.Definitions;

public class TemplateDefinitionTests
{
    [Fact]
    public void GivenNewTemplateDefinition_WhenCreated_ThenHasDefaultValues()
    {
        // Arrange & Act
        var template = new TemplateDefinition();

        // Assert
        Assert.Null(template.Options);
        Assert.Empty(template.Tokens);
        Assert.Empty(template.Hints);
        Assert.Empty(template.Tags);
        Assert.Null(template.Name);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenSettingOptions_ThenOptionsIsSet()
    {
        // Arrange
        var template = new TemplateDefinition();
        var options = TokenizerOptions.Defaults;

        // Act
        template.Options = options;

        // Assert
        Assert.Equal(options, template.Options);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenSettingName_ThenNameIsSet()
    {
        // Arrange
        var template = new TemplateDefinition();

        // Act
        template.Name = "Test Template";

        // Assert
        Assert.Equal("Test Template", template.Name);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenAddingTokens_ThenTokensAreAdded()
    {
        // Arrange
        var template = new TemplateDefinition();
        var token1 = new TokenDefinition();
        token1.AppendName("Token1");
        var token2 = new TokenDefinition();
        token2.AppendName("Token2");

        // Act
        template.Tokens.Add(token1);
        template.Tokens.Add(token2);

        // Assert
        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Token1", template.Tokens[0].Name);
        Assert.Equal("Token2", template.Tokens[1].Name);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenAddingHints_ThenHintsAreAdded()
    {
        // Arrange
        var template = new TemplateDefinition();
        var hint1 = new Hint { Text = "Hint 1", Optional = false };
        var hint2 = new Hint { Text = "Hint 2", Optional = true };

        // Act
        template.Hints.Add(hint1);
        template.Hints.Add(hint2);

        // Assert
        Assert.Equal(2, template.Hints.Count);
        Assert.Equal("Hint 1", template.Hints[0].Text);
        Assert.False(template.Hints[0].Optional);
        Assert.Equal("Hint 2", template.Hints[1].Text);
        Assert.True(template.Hints[1].Optional);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenAddingTags_ThenTagsAreAdded()
    {
        // Arrange
        var template = new TemplateDefinition();

        // Act
        template.Tags.Add("Tag1");
        template.Tags.Add("Tag2");
        template.Tags.Add("Tag3");

        // Assert
        Assert.Equal(3, template.Tags.Count);
        Assert.Equal("Tag1", template.Tags[0]);
        Assert.Equal("Tag2", template.Tags[1]);
        Assert.Equal("Tag3", template.Tags[2]);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenClearingTokens_ThenTokensAreCleared()
    {
        // Arrange
        var template = new TemplateDefinition();
        var token = new TokenDefinition();
        token.AppendName("TestToken");
        template.Tokens.Add(token);

        // Act
        template.Tokens.Clear();

        // Assert
        Assert.Empty(template.Tokens);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenClearingHints_ThenHintsAreCleared()
    {
        // Arrange
        var template = new TemplateDefinition();
        var hint = new Hint { Text = "Test Hint", Optional = false };
        template.Hints.Add(hint);

        // Act
        template.Hints.Clear();

        // Assert
        Assert.Empty(template.Hints);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenClearingTags_ThenTagsAreCleared()
    {
        // Arrange
        var template = new TemplateDefinition();
        template.Tags.Add("TestTag");

        // Act
        template.Tags.Clear();

        // Assert
        Assert.Empty(template.Tags);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenRemovingToken_ThenTokenIsRemoved()
    {
        // Arrange
        var template = new TemplateDefinition();
        var token1 = new TokenDefinition();
        token1.AppendName("Token1");
        var token2 = new TokenDefinition();
        token2.AppendName("Token2");
        template.Tokens.Add(token1);
        template.Tokens.Add(token2);

        // Act
        template.Tokens.Remove(token1);

        // Assert
        Assert.Single(template.Tokens);
        Assert.Equal("Token2", template.Tokens[0].Name);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenRemovingHint_ThenHintIsRemoved()
    {
        // Arrange
        var template = new TemplateDefinition();
        var hint1 = new Hint { Text = "Hint 1", Optional = false };
        var hint2 = new Hint { Text = "Hint 2", Optional = true };
        template.Hints.Add(hint1);
        template.Hints.Add(hint2);

        // Act
        template.Hints.Remove(hint1);

        // Assert
        Assert.Single(template.Hints);
        Assert.Equal("Hint 2", template.Hints[0].Text);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenRemovingTag_ThenTagIsRemoved()
    {
        // Arrange
        var template = new TemplateDefinition();
        template.Tags.Add("Tag1");
        template.Tags.Add("Tag2");

        // Act
        template.Tags.Remove("Tag1");

        // Assert
        Assert.Single(template.Tags);
        Assert.Equal("Tag2", template.Tags[0]);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenCheckingContainsToken_ThenReturnsCorrectResult()
    {
        // Arrange
        var template = new TemplateDefinition();
        var token1 = new TokenDefinition();
        token1.AppendName("Token1");
        var token2 = new TokenDefinition();
        token2.AppendName("Token2");
        template.Tokens.Add(token1);

        // Act & Assert
        Assert.True(template.Tokens.Contains(token1));
        Assert.False(template.Tokens.Contains(token2));
    }

    [Fact]
    public void GivenTemplateDefinition_WhenCheckingContainsHint_ThenReturnsCorrectResult()
    {
        // Arrange
        var template = new TemplateDefinition();
        var hint1 = new Hint { Text = "Hint 1", Optional = false };
        var hint2 = new Hint { Text = "Hint 2", Optional = true };
        template.Hints.Add(hint1);

        // Act & Assert
        Assert.True(template.Hints.Contains(hint1));
        Assert.False(template.Hints.Contains(hint2));
    }

    [Fact]
    public void GivenTemplateDefinition_WhenCheckingContainsTag_ThenReturnsCorrectResult()
    {
        // Arrange
        var template = new TemplateDefinition();
        template.Tags.Add("Tag1");

        // Act & Assert
        Assert.True(template.Tags.Contains("Tag1"));
        Assert.False(template.Tags.Contains("Tag2"));
    }

    [Fact]
    public void GivenTemplateDefinition_WhenGettingTokenIndex_ThenReturnsCorrectIndex()
    {
        // Arrange
        var template = new TemplateDefinition();
        var token1 = new TokenDefinition();
        token1.AppendName("Token1");
        var token2 = new TokenDefinition();
        token2.AppendName("Token2");
        template.Tokens.Add(token1);
        template.Tokens.Add(token2);

        // Act & Assert
        Assert.Equal(0, template.Tokens.IndexOf(token1));
        Assert.Equal(1, template.Tokens.IndexOf(token2));
    }

    [Fact]
    public void GivenTemplateDefinition_WhenGettingHintIndex_ThenReturnsCorrectIndex()
    {
        // Arrange
        var template = new TemplateDefinition();
        var hint1 = new Hint { Text = "Hint 1", Optional = false };
        var hint2 = new Hint { Text = "Hint 2", Optional = true };
        template.Hints.Add(hint1);
        template.Hints.Add(hint2);

        // Act & Assert
        Assert.Equal(0, template.Hints.IndexOf(hint1));
        Assert.Equal(1, template.Hints.IndexOf(hint2));
    }

    [Fact]
    public void GivenTemplateDefinition_WhenGettingTagIndex_ThenReturnsCorrectIndex()
    {
        // Arrange
        var template = new TemplateDefinition();
        template.Tags.Add("Tag1");
        template.Tags.Add("Tag2");

        // Act & Assert
        Assert.Equal(0, template.Tags.IndexOf("Tag1"));
        Assert.Equal(1, template.Tags.IndexOf("Tag2"));
    }

    [Fact]
    public void GivenTemplateDefinition_WhenInsertingToken_ThenTokenIsInserted()
    {
        // Arrange
        var template = new TemplateDefinition();
        var token1 = new TokenDefinition();
        token1.AppendName("Token1");
        var token2 = new TokenDefinition();
        token2.AppendName("Token2");
        var token3 = new TokenDefinition();
        token3.AppendName("Token3");
        template.Tokens.Add(token1);
        template.Tokens.Add(token3);

        // Act
        template.Tokens.Insert(1, token2);

        // Assert
        Assert.Equal(3, template.Tokens.Count);
        Assert.Equal("Token1", template.Tokens[0].Name);
        Assert.Equal("Token2", template.Tokens[1].Name);
        Assert.Equal("Token3", template.Tokens[2].Name);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenInsertingHint_ThenHintIsInserted()
    {
        // Arrange
        var template = new TemplateDefinition();
        var hint1 = new Hint { Text = "Hint 1", Optional = false };
        var hint2 = new Hint { Text = "Hint 2", Optional = true };
        var hint3 = new Hint { Text = "Hint 3", Optional = false };
        template.Hints.Add(hint1);
        template.Hints.Add(hint3);

        // Act
        template.Hints.Insert(1, hint2);

        // Assert
        Assert.Equal(3, template.Hints.Count);
        Assert.Equal("Hint 1", template.Hints[0].Text);
        Assert.Equal("Hint 2", template.Hints[1].Text);
        Assert.Equal("Hint 3", template.Hints[2].Text);
    }

    [Fact]
    public void GivenTemplateDefinition_WhenInsertingTag_ThenTagIsInserted()
    {
        // Arrange
        var template = new TemplateDefinition();
        template.Tags.Add("Tag1");
        template.Tags.Add("Tag3");

        // Act
        template.Tags.Insert(1, "Tag2");

        // Assert
        Assert.Equal(3, template.Tags.Count);
        Assert.Equal("Tag1", template.Tags[0]);
        Assert.Equal("Tag2", template.Tags[1]);
        Assert.Equal("Tag3", template.Tags[2]);
    }
}
