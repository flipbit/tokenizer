using System.Text;
using Tokens.Enumerators;

namespace Tokens.Compilation.Parsing;

internal sealed class TemplateDefinitionEnumerator
{
    private readonly string _pattern;
    private readonly int _patternLength;

    private int _currentLocation;
    private bool _resetNextLine;

    public TemplateDefinitionEnumerator(string pattern)
    {
        _pattern = pattern;

        if (string.IsNullOrEmpty(pattern))
        {
            _patternLength = 0;
        }
        else
        {
            _patternLength = pattern.Length;
        }

        _currentLocation = 0;
        Location = new FileLocation();
    }

    public bool IsEmpty => _currentLocation >= _patternLength;

    public FileLocation Location { get; }

    public string Next()
    {
        if (IsEmpty) return string.Empty;

        var nextChar = _pattern[_currentLocation];
        _currentLocation++;

        if (_resetNextLine)
        {
            Location.NewLine();
            _resetNextLine = false;
        }
        else
        {
            Location.Increment(nextChar);
        }

        if (nextChar == '\n')
        {
            _resetNextLine = true;
        }

        return nextChar.ToString();
    }

    public string Next(int length)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < length; i++)
        {
            sb.Append(Next());
        }

        return sb.ToString();
    }

    public string Peek()
    {
        if (IsEmpty) return string.Empty;

        return _pattern.Substring(_currentLocation, 1);
    }

    public string Peek(int length)
    {
        if (IsEmpty) return string.Empty;

        var different = (_currentLocation + length) - _patternLength;
        if (different > 0) length -= different;

        if (length < 1) return string.Empty;

        return _pattern.Substring(_currentLocation, length);
    }
}
