namespace Tokens.Enumerators;

/// <summary>
/// Represents a location in a text file
/// </summary>
public class FileLocation : IEquatable<FileLocation>
{
    private int _newLineCounter = 0;

    /// <summary>
    /// The column number
    /// </summary>
    public int Column { get; private set; }

    /// <summary>
    /// The line number
    /// </summary>
    public int Line { get; private set; }

    /// <summary>
    /// The paragraph number
    /// </summary>
    public int Paragraph { get; private set; }

    /// <summary>
    /// Creates a new instance of this class
    /// </summary>
    public FileLocation()
    {
        Column = 1;
        Line = 1;
        Paragraph = 1;
    }

    /// <summary>
    /// Increments the column count
    /// </summary>
    internal void Increment(char value)
    {
        if (value == '\r') return;
        if (value == '\n') return;

        if (!char.IsWhiteSpace(value))
        {
            _newLineCounter = 0;
        }

        Column++;
    }

    /// <summary>
    /// Increments the line and resets the column counts
    /// </summary>
    internal void NewLine()
    {
        if (Column == 1)
        {
            if (_newLineCounter == 1)
            {
                Paragraph++;
            }
        }

        Column = 1;
        Line++;
        _newLineCounter++;
    }

    /// <summary>
    /// Resets the counts
    /// </summary>
    internal void Reset()
    {
        Column = 1;
        Line = 1;
        Paragraph = 1;
    }

    /// <summary>
    /// Clones this instance
    /// </summary>
    /// <returns></returns>
    public FileLocation Clone()
    {
        return new FileLocation
        {
            Column = Column,
            Line = Line,
            Paragraph = Paragraph,
        };
    }

    /// <summary>
    /// Determines whether the specified <see cref="FileLocation"/> is equal to this instance.
    /// </summary>
    public bool Equals(FileLocation? other)
    {
        return other is not null && Column == other.Column && Line == other.Line && Paragraph == other.Paragraph;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as FileLocation);

    /// <inheritdoc />
    public override int GetHashCode()
    {
#if NETSTANDARD2_0
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Column;
            hash = hash * 31 + Line;
            hash = hash * 31 + Paragraph;
            return hash;
        }
#else
        return HashCode.Combine(Column, Line, Paragraph);
#endif
    }

    /// <summary>
    /// Determines whether two <see cref="FileLocation"/> instances are equal.
    /// </summary>
    public static bool operator ==(FileLocation? left, FileLocation? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two <see cref="FileLocation"/> instances are not equal.
    /// </summary>
    public static bool operator !=(FileLocation? left, FileLocation? right) => !Equals(left, right);

    /// <summary>
    /// Returns a string representation of this instance
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"Ln: {Line} Col: {Column} Para: {Paragraph}";
    }
}
