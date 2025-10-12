namespace Tokens.Enumerators
{
    /// <summary>
    /// Represents a location in a text file
    /// </summary>
    public class FileLocation
    {
        private int newLineCounter = 0;

        /// <summary>
        /// The column number
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// The line number
        /// </summary>
        public int Line { get; private set;  }

        /// <summary>
        /// The paragraph number
        /// </summary>
        public int Paragraph { get; private set; }

        /// <summary>
        /// Creates a new instance of this class
        /// </summary>
        public FileLocation()
        {
            Column = 0;
            Line = 1;
            Paragraph = 1;
        }

        /// <summary>
        /// Increments the column count
        /// </summary>
        internal void Increment(string value)
        {
            if (value == "\r") return;
            if (value == "\n") return;

            if (string.IsNullOrWhiteSpace(value) == false)
            {
                newLineCounter = 0;
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
                if (newLineCounter == 1)
                {
                    Paragraph++;
                }
            }

            Column = 1;
            Line++;
            newLineCounter++;
        }

        /// <summary>
        /// Resets the counts
        /// </summary>
        internal void Reset()
        {
            Column = 0;
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
                Paragraph = Paragraph
            };
        }

        /// <summary>
        /// Returns a string representation of this instance
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Ln: {Line} Col: {Column} Para: {Paragraph}";
        }
    }
}
