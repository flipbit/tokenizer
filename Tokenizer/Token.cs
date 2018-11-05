using System;
using System.Collections.Generic;
using Tokens.Extensions;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens
{
    /// <summary>
    /// Represents a single token in a string
    /// </summary>
    public class Token
    {
        public Token()
        {
            Transformers = new List<TransformerContext>();
            Validators = new List<ValidatorContext>();
        }

        /// <summary>
        /// Gets or sets the preamble string that must appear before the token.
        /// </summary>
        public string Preamble { get; set; }

        /// <summary>
        /// Gets or sets the value of the token.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the operators to perform on this Token.
        /// </summary>
        public IList<TransformerContext> Transformers { get; }

        /// <summary>
        /// Gets the validators to perform on this Token
        /// </summary>
        public IList<ValidatorContext> Validators { get; }

        public bool Optional { get; set; }

        public bool Repeating { get; set; }

        public bool TerminateOnNewLine { get; set; }

        public int Id { get; set; }

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

        public bool Assign(object target, string value, TokenizerOptions options)
        {
            if (CanAssign(value) == false) return false;

            if (TerminateOnNewLine)
            {
                var index = value.IndexOf("\n");
                if (index > 0)
                {
                    value = value.Substring(0, index);
                }
            }

            if (options.TrimTrailingWhiteSpace)
            {
                value = value.TrimEnd();
            }

            object transformed = value;

            foreach (var transformer in Transformers)
            {
                transformed = transformer.Transform(transformed);
            }

            if (target is List<Substitution> list)
            {
                list.Add(new Substitution
                {
                    Name = Name,
                    Value = transformed
                });
                return true;
            }

            try
            {
                target.SetValue(Name, transformed);
            }
            catch (MissingMemberException)
            {
                if (options.ThrowExceptionOnMissingProperty)
                {
                    throw;
                }
            }

            return true;
        }
    }
}