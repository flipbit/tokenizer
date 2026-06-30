using System.Linq;
using Xunit;

namespace Tokens.Extensions;

public class StringExtensionsTest
{
    [Fact]
    public void TestSubStringEmptyString()
    {
        Assert.Equal(string.Empty, string.Empty.SubstringAfterString("c"));
    }

    [Fact]
    public void TestSubStringWithNonMatchingString()
    {
        Assert.Equal("banana", "banana".SubstringAfterString("c"));
    }


    [Fact]
    public void TestSubStringWithMultipleMatchingString()
    {
        Assert.Equal("ana", "banana".SubstringAfterAnyString("ban", "b"));
    }

    [Fact]
    public void TestSubStringWithMatchingString()
    {
        Assert.Equal("ana", "banana".SubstringAfterString("n"));
    }

    [Fact]
    public void TestSubStringWithAfterLastString()
    {
        Assert.Equal("a", "banana".SubstringAfterLastString("n"));
    }

    [Fact]
    public void TestSubStringBeforeWithMatchingString()
    {
        Assert.Equal("ba", "banana".SubstringBeforeString("n"));
    }

    [Fact]
    public void TestSubStringBeforeWithMatchingLastString()
    {
        Assert.Equal("bana", "banana".SubstringBeforeLastString("n"));
    }

    [Fact]
    public void TestSplitLines()
    {
        var result = "one\r\ntwo".ToLines().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("one", result[0]);
        Assert.Equal("two", result[1]);
    }

    [Fact]
    public void TestSplitWithNewLinesOnly()
    {
        var result = "one\ntwo".ToLines().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("one", result[0]);
        Assert.Equal("two", result[1]);
    }

    [Fact]
    public void TestSplitWithCarriageReturnsOnly()
    {
        var result = "one\rtwo".ToLines().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("one", result[0]);
        Assert.Equal("two", result[1]);
    }

    [Fact]
    public void TestSplitWithOneLineOnly()
    {
        var result = "one".ToLines().ToList();

        Assert.Single(result);
        Assert.Equal("one", result[0]);
    }

    [Fact]
    public void TestSplitWithNoLinesOnly()
    {
        var result = string.Empty.ToLines().ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void TestSplitWithNullValues()
    {
        var result = ((string) null!).ToLines().ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void TestKeepCharacters()
    {
        var result = "123456".Keep("123");

        Assert.Equal("123", result);
    }

    [Fact]
    public void TestKeepCharactersWhenNoneExist()
    {
        var result = "123456".Keep("789");

        Assert.Equal("", result);
    }

    [Fact]
    public void TestKeepCharactersWhenInputEmpty()
    {
        var result = "".Keep("789");

        Assert.Equal("", result);
    }

    [Fact]
    public void TestKeepCharactersWhenInputNull()
    {
        var result = ((string) null!).Keep("789");

        Assert.Equal("", result);
    }

    [Fact]
    public void TestKeepCharactersWhenMatchNull()
    {
        var result = "123456".Keep(null!);

        Assert.Equal("", result);
    }

    [Fact]
    public void TestSubstringBeforeNewLineWithUnixNewLine()
    {
        var result = "Hello\nWorld".SubstringBeforeNewLine();

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void TestSubstringBeforeNewLineWithWindowsNewLine()
    {
        var result = "Hello\r\nWorld".SubstringBeforeNewLine();

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void TestSubstringBeforeNewLineWitNoNewLine()
    {
        var result = "Hello World".SubstringBeforeNewLine();

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void TestSubstringBeforeNewLineWhenEmpty()
    {
        var result = "".SubstringBeforeNewLine();

        Assert.Equal("", result);
    }

    [Fact]
    public void TestSubstringBeforeNewLineWhenNull()
    {
        var result = ((string) null!).SubstringBeforeNewLine();

        Assert.Null(result);
    }

    [Fact]
    public void TestEndsWithNewLineWithUnixNewLine()
    {
        Assert.True("Ends with unix\n".EndsWithNewLine());
    }

    [Fact]
    public void TestEndsWithNewLineWithWindowsNewLine()
    {
        Assert.True("Ends with Windows\r\n".EndsWithNewLine());
    }

    [Fact]
    public void TestEndsWithNewLineWhenFalse()
    {
        Assert.False("Ends with nothing".EndsWithNewLine());
    }

    [Fact]
    public void TestEndsWithNewLineWhenEmpty()
    {
        Assert.False("".EndsWithNewLine());
    }

    [Fact]
    public void TestEndsWithNewLineWhenNull()
    {
        Assert.False(((string) null!).EndsWithNewLine());
    }

    [Fact]
    public void TestEndsWithNewLineWhenShort()
    {
        Assert.False("x".EndsWithNewLine());
    }

    [Fact]
    public void TestTrimTrailingNewLineWithUnixNewLine()
    {
        Assert.Equal("Ends with unix", "Ends with unix\n".TrimTrailingNewLine());
    }

    [Fact]
    public void TestTrimTrailingNewLineWithWindowsNewLine()
    {
        Assert.Equal("Ends with Windows", "Ends with Windows\r\n".TrimTrailingNewLine());
    }

    [Fact]
    public void TestTrimTrailingNewLineWhenFalse()
    {
        Assert.Equal("Ends with nothing", "Ends with nothing".TrimTrailingNewLine());
    }

    [Fact]
    public void TestTrimTrailingNewLineWhenEmpty()
    {
        Assert.Equal("", "".TrimTrailingNewLine());
    }

    [Fact]
    public void TestTrimTrailingNewLineWhenNull()
    {
        Assert.Null(((string) null!).TrimTrailingNewLine());
    }

    [Fact]
    public void TestTrimTrailingNewLineWhenShort()
    {
        Assert.Equal("x", "x".TrimTrailingNewLine());
    }

    [Fact]
    public void TestToLogInfoString()
    {
        Assert.Equal("Hello", "Hello".ToLogInfoString());
    }

    [Fact]
    public void TestToLogInfoStringWithControlCharacters()
    {
        Assert.Equal("Hello\\r\\n\\t", "Hello\r\n\t".ToLogInfoString());
    }
}