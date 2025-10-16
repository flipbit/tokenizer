using Tokens.Enumerators;

namespace Tokens.Compilation.Nodes
{
    /// <summary>
    /// Represents a chunk of preamble or free-form text outside of token blocks.
    /// </summary>
    public sealed class TextNode : ContentNode
    {
        public TextNode(FileLocation location, int start, int length, string text)
            : base(location, start, length)
        {
            Text = text ?? string.Empty;
        }

        /// <summary>
        /// Gets the text content for this node with escapes (e.g., "{{" → "{").
        /// </summary>
        public string Text { get; }
    }
}



