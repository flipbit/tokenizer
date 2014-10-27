using System;
using System.Collections.Generic;

namespace Tokens
{
    /// <summary>
    /// Represents a single token in a string
    /// </summary>
    public class Token
    {
        /// <summary>
        /// Gets or sets the prefixed string that must appear before the token.
        /// </summary>
        /// <value>
        /// The prefix.
        /// </value>
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the suffixed string that must appear after the token.
        /// </summary>
        /// <value>
        /// The suffix.
        /// </value>
        public string Suffix { get; set; }

        /// <summary>
        /// Gets or sets the prerequisite for this token.  This is an entire line
        /// that must appear before this token is found.
        /// </summary>
        /// <value>
        /// The prerequisite.
        /// </value>
        public string Prerequisite { get; set; }

        /// <summary>
        /// Gets or sets the value of the token.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the operation to perform on the replaced value.
        /// </summary>
        /// <value>
        /// The operation.
        /// </value>
        public string Operation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Token"/> has replaced some text in the source input.
        /// </summary>
        /// <value>
        ///   <c>true</c> if replaced; otherwise, <c>false</c>.
        /// </value>
        public bool Replaced { get; set; }

        /// <summary>
        /// Determines whether this instance is contained within the given input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public bool ContainedIn(string input)
        {
            return input.Contains(Prefix) && input.Contains(Suffix);
        }

        public string PerformOperation(string value)
        {
            string result;

            switch (Operation)
            {
                case "ToUpper()":
                    result = value.ToUpper();
                    break;

                case "ToLower()":
                    result = value.ToLower();
                    break;

                case "":
                case null:
                    result = value;
                    break;

                default:
                    throw new ArgumentException("Unknown Token Operation: " + Operation);
            }

            return result;
        }

        /// <summary>
        /// Determines if this token's prerequisites have been satisfied by the existing processing.
        /// </summary>
        /// <param name="processed">The processed.</param>
        /// <returns></returns>
        public bool PrerequisiteSatisfied(IList<string> processed)
        {
            if (string.IsNullOrEmpty(Prerequisite)) return true;

            return processed.Contains(Prerequisite);
        }
    }
}