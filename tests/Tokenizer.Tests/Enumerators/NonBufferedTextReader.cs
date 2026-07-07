namespace Tokens.Enumerators;

/// <summary>
/// A TextReader that returns -1 from Peek() even when more data is available,
/// and delivers data in small chunks — simulating a non-buffered reader
/// (e.g. NetworkStream-backed StreamReader where Peek() returns -1 between reads).
/// </summary>
internal class NonBufferedTextReader : TextReader
{
    private readonly string _data;
    private int _position;
    private readonly int _chunkSize;

    public NonBufferedTextReader(string data, int chunkSize = 5)
    {
        _data = data;
        _chunkSize = chunkSize;
    }

    public override int Read(char[] buffer, int index, int count)
    {
        if (_position >= _data.Length) return 0;
        var available = Math.Min(Math.Min(count, _chunkSize), _data.Length - _position);
        _data.CopyTo(_position, buffer, index, available);
        _position += available;
        return available;
    }

    public override int Peek()
    {
        // Always return -1, simulating a non-buffered reader
        return -1;
    }
}
