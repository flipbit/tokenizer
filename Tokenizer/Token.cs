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

        /// <summary>
        /// Sets the token logger
        /// </summary>
        static Token()
        {
            Log = LogProvider.GetLogger(nameof(Token));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Token"/> class.
        /// </summary>
        public Token()
        {
            Decorators = new List<TokenDecoratorContext>();
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
        /// Gets the decorators on this Token
        /// </summary>
        public IList<TokenDecoratorContext> Decorators { get; }

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

        /// <summary>
        /// The unique id of this token in the <see cref="Template"/>.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Defines a token that must have been matched in the input before this token
        /// can be considered.  Used with repeating tokens that would otherwise be
        /// to aggressive in their matching.
        /// </summary>
        public int DependsOnId { get; set; }

        internal bool Assign(object target, string value, TokenizerOptions options, int line, int column)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (string.IsNullOrWhiteSpace(Name)) return false;


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

            if (options.TrimTrailingWhiteSpace)
            {
                value = value.TrimEnd();
            }

            object input = value;

            foreach (var decorator in Decorators)
            {
                if (decorator.IsTransformer)
                {
                    var output = decorator.Transform(input);
            
                    Log.Debug($"     -> {decorator.DecoratorType.Name}: Transformed '{input}' to '{output}'");

                    input = output;
                }

                if (decorator.IsValidator)
                {
                    if (decorator.Validate(input))
                    {
                        Log.Debug($"    -> {decorator.DecoratorType.Name} OK!");
                    }
                    else
                    {
                        Log.Debug($"    -> {decorator.DecoratorType.Name} Validation Failure: {value}");

                        return false;
                    }
                }
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

        public bool CanAssign(string value)
        {
            object input = value;

            foreach (var decorator in Decorators)
            {
                if (decorator.IsTransformer)
                {
                    var output = decorator.Transform(input);
            
                    input = output;
                }

                if (decorator.IsValidator)
                {
                    if (decorator.Validate(input) == false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}