using System.Collections.Generic;

namespace Tokens
{
    /// <summary>
    /// Represents a single token in a string
    /// </summary>
    public class Token
    {
        private IList<Function> functions;
        private string operation;

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
        public string Operation
        {
            get
            {
                return operation;
            }
            set 
            { 
                operation = value;
                functions = null;
            }
        }

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

        /// <summary>
        /// Gets the functions to perform on this Token.
        /// </summary>
        /// <returns></returns>
        public IList<Function> Functions
        {
            get
            {
                if (functions == null)
                {
                    functions = new FunctionParser().Parse(Operation);
                }

                return functions;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a list.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is list; otherwise, <c>false</c>.
        /// </value>
        public bool IsList { get; set; }
    }
}