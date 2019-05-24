using System;
using System.Collections.Generic;
using Tokens.Exceptions;
using Tokens.Extensions;
using Tokens.Logging;
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

        /// <summary>
        /// If <c>true</c> then this <see cref="Token"/> is optional and can be skipped
        /// during processing.
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// If <c>true</c> then this <see cref="Token"/> can map multiple instances onto
        /// an <see cref="IList{T}"/>.
        /// </summary>
        public bool Repeating { get; set; }

        /// <summary>
        /// If <c>true</c> then this <see cref="Token"/> will map a value up to the next
        /// newline.
        /// </summary>
        public bool TerminateOnNewLine { get; set; }

        /// <summary>
        /// If <c>true</c> then this <see cref="Token"/> must be present in the input for
        /// the processing to be successful.
        /// </summary>
        public bool Required { get; set; }

        public int Id { get; set; }

        public bool CanAssign(object value)
        {
            return CanAssign(value, null);
        }

        internal bool CanAssign(object value, ILog log)
        {
            // Don't assign tokens with no name
            // (can happen if the token is the last token in the template)
            if (string.IsNullOrWhiteSpace(Name)) return false;

            foreach (var validator in Validators)
            {
                if (validator.Validate(value) == false)
                {
                    log?.Debug($"    -> {validator.ValidatorType.Name} Validation Failure: {value}");

                    return false;
                }
            }

            return true;
        }

        internal bool Assign(object target, string value, TokenizerOptions options)
        {
            return Assign(target, value, options, null);
        }

        internal bool Assign(object target, string value, TokenizerOptions options, ILog log)
        {
            log?.Debug($"  -> Assigning '{Name}' to '{value}'");

            if (CanAssign(value, log) == false) return false;

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

            object input = value;

            foreach (var transformer in Transformers)
            {
                var output = transformer.Transform(input);
        
                log?.Debug($"    -> {transformer.GetType().Name}: Transformed '{input}' to '{output}'");

                input = output;
            }

            if (target is List<Substitution> list)
            {
                list.Add(new Substitution
                {
                    Name = Name,
                    Value = input
                });
                return true;
            }

            try
            {
                target.SetValue(Name, input);
            }
            catch (MissingMemberException)
            {
                log?.Warn($"       Missing property on target: {Name}");

                throw;
            }
            catch (TypeConversionException ex)
            {
                log?.Warn($"       {ex.Message}");

                return false;
            }
            catch (Exception e)
            {
                log?.Error(e, $"Unexpected error when assigning '{Name}' to '{input}':");

                var ex = new TokenAssignmentException(this, e);

                throw ex;
            }

            return true;
        }
    }
}