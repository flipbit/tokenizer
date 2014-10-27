using System.Collections.Generic;

namespace Tokens
{
    /// <summary>
    /// Represents a <see cref="Token"/> function.
    /// </summary>
    public class Function
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Function"/> class.
        /// </summary>
        public Function()
        {
            Parameters = new List<string>();
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public IList<string> Parameters { get; private set; }
    }
}
