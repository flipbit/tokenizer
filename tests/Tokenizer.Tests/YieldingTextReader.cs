namespace Tokens;

/// <summary>
/// A TextReader that yields on async reads, exercising real async suspension
/// and resumption in the tokenization engine's cooperative buffering loop.
/// </summary>
internal class YieldingTextReader : TextReader
{
    private readonly string _data;
    private int _position;
    private readonly int _chunkSize;

    public YieldingTextReader(string data, int chunkSize)
    {
        _data = data;
        _chunkSize = chunkSize;
    }

    public override int Read(char[] buffer, int index, int count)
    {
        if (_position >= _data.Length) return 0;
        var toRead = Math.Min(Math.Min(count, _chunkSize), _data.Length - _position);
        _data.CopyTo(_position, buffer, index, toRead);
        _position += toRead;
        return toRead;
    }

    public override async Task<int> ReadAsync(char[] buffer, int index, int count)
    {
        await Task.Yield();
        return Read(buffer, index, count);
    }

    public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken ct = default)
    {
        await Task.Yield();
        ct.ThrowIfCancellationRequested();
        if (_position >= _data.Length) return 0;
        var toRead = Math.Min(Math.Min(buffer.Length, _chunkSize), _data.Length - _position);
        _data.AsSpan(_position, toRead).CopyTo(buffer.Span);
        _position += toRead;
        return toRead;
    }

    public override int Peek()
    {
        return _position < _data.Length ? _data[_position] : -1;
    }
}
