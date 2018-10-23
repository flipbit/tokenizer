using System.Collections.Generic;
using Tokens.Extensions;

namespace Tokens
{
    /// <summary>
    /// Represents a single token in a string
    /// </summary>
    public class Token
    {
        public Token()
        {
            Operators = new List<OperatorContext>();
            Validators = new List<ValidatorContext>();
        }

        /// <summary>
        /// Gets or sets the prefixed string that must appear before the token.
        /// </summary>
        /// <value>
        /// The prefix.
        /// </value>
        public string Preamble { get; set; }

        /// <summary>
        /// Gets or sets the value of the token.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets the operators to perform on this Token.
        /// </summary>
        public IList<OperatorContext> Operators { get; }

        /// <summary>
        /// Gets the validators to perform on this Token
        /// </summary>
        public IList<ValidatorContext> Validators { get; }

        public bool CanAssign(object value)
        {
            foreach (var validator in Validators)
            {
                if (validator.Validate(value) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Assign(object target, object value)
        {
            if (CanAssign(value) == false) return false;

            target.SetValue(Name, value);

            return true;
        }
    }
}