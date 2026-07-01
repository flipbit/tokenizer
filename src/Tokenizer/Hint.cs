namespace Tokens;

/// <summary>
/// Defines a string of text that can occur in a template's input.
/// A hint can optionally be required to be present.
/// Hints are used when determining whether the input is valid, and to determine
/// the best matched template for a given input.
/// </summary>
/// <param name="Text">The text to appear in the input</param>
/// <param name="Optional">If <c>true</c> then this hint must appear in the input in order for the
/// <see cref="Template"/> to be considered successfully matched.</param>
public sealed record Hint(string Text = "", bool Optional = false);
