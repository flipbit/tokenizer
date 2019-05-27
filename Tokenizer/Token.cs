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
        private static readonly ILog Log;

        static Token()
        {
            Log = LogProvider.GetLogger(nameof(Token));
        }

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

        public int DependsOnId { get; set; }

        internal bool CanAssign(object value)
        {
            // Don't assign tokens with no name
            // (can happen if the token is the last token in the template)
            if (string.IsNullOrWhiteSpace(Name)) return false;

            foreach (var validator in Validators)
            {
                if (validator.Validate(value) == false)
                {
                    Log.Debug($"    -> {validator.ValidatorType.Name} Validation Failure: {value}");

                    return false;
                }
            }

            return true;
        }

        internal bool Assign(object target, string value, TokenizerOptions options, int line, int column)
        {
            if (string.IsNullOrEmpty(value)) return false;

            if (value.Substring(value.Length - 1) == "\n")
            {
                value = value.Substring(0, value.Length - 1);
            }

            if (TerminateOnNewLine)
            {
                var index = value.IndexOf("\n");
                if (index > 0)
                {
                    value = value.Substring(0, index);
                }
            }

            Log.Debug("  -> Ln: {0} Col: {1} : Assigning {2} ({3}) as {4}", line, column, Name, Id, value);

            if (CanAssign(value) == false) return false;

            if (options.TrimTrailingWhiteSpace)
            {
                value = value.TrimEnd();
            }

            object input = value;

            foreach (var transformer in Transformers)
            {
                var output = transformer.Transform(input);
        
                Log.Debug($"     -> {transformer.OperatorType.Name}: Transformed '{input}' to '{output}'");

                input = output;
            }

            if (target is IDictionary<string, object> dictionary)
            {
                return SetDictionaryValue(dictionary, input);
            }

            try
            {
                target.SetValue(Name, input);
            }
            catch (MissingMemberException)
            {
                Log.Warn($"       Missing property on target: {Name}");

                throw;
            }
            catch (TypeConversionException ex)
            {
                Log.Warn($"       {ex.Message}");

                return false;
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error when assigning '{Name}' to '{input}':");

                var ex = new TokenAssignmentException(this, e);

                throw ex;
            }

            return true;
        }

        private bool SetDictionaryValue(IDictionary<string, object> dictionary, object input)
        {
            if (Repeating)
            {
                List<object> list;
                if (dictionary.ContainsKey(Name))
                {
                    list = dictionary[Name] as List<object>;
                }
                else
                {
                    list = new List<object>();
                }
                list.Add(input);
                input = list;
            }

            if (dictionary.ContainsKey(Name))
            {
                dictionary[Name] = input;
            }
            else
            {
                dictionary.Add(Name, input);
            }

            return true;
        }
    }
}