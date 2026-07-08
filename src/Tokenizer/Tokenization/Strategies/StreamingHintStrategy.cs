using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization.Strategies;

internal sealed class StreamingHintStrategy : IHintStrategy
{
    private Template? _currentTemplate;
    private int _maxHintLength;
    private char[]? _overlapBuffer;
    private int _overlapCount;
    private readonly HashSet<string> _foundHints = new(StringComparer.Ordinal);

    public bool PreProcess(Template template, TokenEnumerator enumerator,
                           string? rawInput, TokenizeResult result, IDiagnosticCollector collector)
    {
        _currentTemplate = template;
        _foundHints.Clear();
        _overlapCount = 0;

        if (template.Hints.Count == 0)
        {
            _maxHintLength = 0;
            return false;
        }

        _maxHintLength = 0;
        foreach (var hint in template.Hints)
        {
            if (!string.IsNullOrEmpty(hint.Text) && hint.Text.Length > _maxHintLength)
            {
                _maxHintLength = hint.Text.Length;
            }
        }

        if (_maxHintLength > 1)
        {
            var overlapSize = _maxHintLength - 1;
            if (_overlapBuffer == null || _overlapBuffer.Length < overlapSize)
            {
                _overlapBuffer = new char[overlapSize];
            }
        }

        return false;
    }

    public void OnBufferFilled(char[] buffer, int count)
    {
        if (_currentTemplate == null || _currentTemplate.Hints.Count == 0 || count == 0)
        {
            return;
        }

        if (_foundHints.Count >= _currentTemplate.Hints.Count)
        {
            return;
        }

        ScanForHints(buffer, count);

        if (_maxHintLength > 1)
        {
            var overlapSize = _maxHintLength - 1;
            var copyCount = Math.Min(overlapSize, count);
            var sourceOffset = count - copyCount;
            Array.Copy(buffer, sourceOffset, _overlapBuffer!, 0, copyCount);
            _overlapCount = copyCount;
        }
    }

    public bool PostProcess(TokenizeResult result)
    {
        if (_currentTemplate == null || _currentTemplate.Hints.Count == 0)
        {
            return false;
        }

        foreach (var hint in _currentTemplate.Hints)
        {
            if (string.IsNullOrEmpty(hint.Text))
            {
                continue;
            }

            if (_foundHints.Contains(hint.Text))
            {
                result.Hints.TryAddMatch(hint, new TokenEnumerator(string.Empty));
            }
        }

        foreach (var hint in _currentTemplate.Hints)
        {
            result.Hints.TryAddMiss(hint);
        }

        return result.Hints.Misses.Any(h => !h.Optional);
    }

    private void ScanForHints(char[] buffer, int count)
    {
        foreach (var hint in _currentTemplate!.Hints)
        {
            if (string.IsNullOrEmpty(hint.Text) || _foundHints.Contains(hint.Text))
            {
                continue;
            }

            if (ScanChunk(buffer, count, hint.Text))
            {
                _foundHints.Add(hint.Text);
            }
        }
    }

    private bool ScanChunk(char[] buffer, int count, string hintText)
    {
        if (_overlapCount > 0 && _maxHintLength > 1)
        {
            if (ScanOverlap(buffer, count, hintText))
            {
                return true;
            }
        }

#if NET8_0_OR_GREATER
        var span = buffer.AsSpan(0, count);
        return span.IndexOf(hintText.AsSpan(), StringComparison.Ordinal) >= 0;
#else
        return IndexOfInCharArray(buffer, count, hintText) >= 0;
#endif
    }

    private bool ScanOverlap(char[] buffer, int count, string hintText)
    {
        var windowFromBuffer = Math.Min(hintText.Length - 1, count);
        var windowLength = _overlapCount + windowFromBuffer;

        if (windowLength < hintText.Length)
        {
            return false;
        }

        var maxStart = windowLength - hintText.Length;
        for (var start = 0; start <= maxStart; start++)
        {
            // Only check positions that straddle the boundary
            if (start + hintText.Length <= _overlapCount || start >= _overlapCount)
            {
                continue;
            }

            var matched = true;
            for (var j = 0; j < hintText.Length; j++)
            {
                var pos = start + j;
                var c = pos < _overlapCount ? _overlapBuffer![pos] : buffer[pos - _overlapCount];
                if (c != hintText[j])
                {
                    matched = false;
                    break;
                }
            }

            if (matched) return true;
        }

        return false;
    }

#if !NET8_0_OR_GREATER
    private static int IndexOfInCharArray(char[] buffer, int count, string value)
    {
        var valueLength = value.Length;
        if (valueLength == 0) return 0;
        if (count < valueLength) return -1;

        var firstChar = value[0];
        var maxStart = count - valueLength;

        for (var i = 0; i <= maxStart; i++)
        {
            if (buffer[i] != firstChar) continue;

            var found = true;
            for (var j = 1; j < valueLength; j++)
            {
                if (buffer[i + j] != value[j])
                {
                    found = false;
                    break;
                }
            }

            if (found) return i;
        }

        return -1;
    }
#endif
}
