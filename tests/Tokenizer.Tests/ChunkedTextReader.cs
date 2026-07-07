namespace Tokens;

/// <summary>
/// A TextReader that delivers data in configurable chunk sizes,
/// simulating a network stream or similar source where data arrives incrementally.
/// Forces multiple buffer fills and cooperative yield cycles in the tokenization engine.
/// </summary>
internal class ChunkedTextReader : TextReader
{
    private readonly string _data;
    private int _position;
    private readonly int _chunkSize;

    public ChunkedTextReader(string data, int chunkSize)
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

    public override int Peek()
    {
        return _position < _data.Length ? _data[_position] : -1;
    }
}
