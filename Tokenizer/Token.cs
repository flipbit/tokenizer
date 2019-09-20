using System;
using System.Collections.Generic;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Tokens.Extensions;
using Tokens.Logging;
using Tokens.Transformers;

namespace Tokens
{
    /// <summary>
    /// Represents a single token in a string
    /// </summary>
    public class Token
    {
        private static readonly ILog Log;
        private string content;

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
        public Token(string content)
        {
            this.content = content;
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

        /// <summary>
        /// Determines if this <see cref="Token"/> was defined in the template front matter section.
        /// </summary>
        public bool IsFrontMatterToken { get; set; }

        /// <summary>
        /// Determines if this token is a null placeholder
        /// </summary>
        public bool IsNull { get; set; }

        /// <summary>
        /// The location of this token in the template.
        /// </summary>
        public FileLocation Location { get; set; }

        /// <summary>
        /// If true, multiple instances of this token will be concatenated together
        /// on the target.
        /// </summary>
        public bool Concatenate { get; set; }

        /// <summary>
        /// Defines a joining string to use when concatenating two token values.
        /// </summary>
        public string ConcatenationString { get; set; }

        /// <summary>
        /// Returns the string from which this token was created.
        /// </summary>
        public override string ToString()
        {
            return content;
        }

        internal bool Assign(object target, string value, TokenizerOptions options, FileLocation location, out object assignedValue)
        {
            assignedValue = null;

            if (string.IsNullOrEmpty(value) && IsFrontMatterToken == false) return false;
            if (IsNull) return false;
            if (string.IsNullOrWhiteSpace(Name)) return false;

            value = value.TrimTrailingNewLine();
            
            if (string.IsNullOrEmpty(value) == false && TerminateOnNewLine)
            {
                var index = value.IndexOf("\n");
                if (index > 0)
                {
                    value = value.Substring(0, index);
                }
            }

            Log.Verbose("-> Ln: {0} Col: {1} : Assigning {2}[{3}] as {4}", location.Line, location.Column, Name, Id, value.ToLogInfoString());

            if (options.TrimTrailingWhiteSpace)
            {
                value = value.TrimEnd();
            }

            assignedValue = value;

            using (new LogIndentation())
            {
                foreach (var decorator in Decorators)
                {
                    if (decorator.IsTransformer)
                    {
                        var transformed = decorator.CanTransform(assignedValue, out var output);

                        if (transformed == false)
                        {
                            Log.Verbose($"-> {decorator.DecoratorType.Name}: Unable to transform value '{assignedValue}'!");
                            
                            return false;
                        }

                        if (decorator.DecoratorType == typeof(SetTransformer))
                        {
                            Log.Verbose($"-> {decorator.DecoratorType.Name}: Set value to '{output}'");
                        }
                        else if (output is DateTime time)
                        {
                            Log.Verbose($"-> {decorator.DecoratorType.Name}: Transformed '{assignedValue}' to {time:yyyy-MM-dd HH:mm:ss} ({time.Kind})");
                        }
                        else
                        {
                            Log.Verbose($"-> {decorator.DecoratorType.Name}: Transformed '{assignedValue}' to '{output}' ({output.GetType().Name})");
                        }

                        assignedValue = output;
                    }

                    if (decorator.IsValidator)
                    {
                        if (decorator.Validate(assignedValue))
                        {
                            Log.Verbose($"-> {decorator.DecoratorType.Name} OK!");
                        }
                        else
                        {
                            Log.Verbose($"-> {decorator.DecoratorType.Name} Validation Failure: {value}");

                            return false;
                        }
                    }
                }
            }

            if (target is IDictionary<string, object> dictionary)
            {
                return SetDictionaryValue(dictionary, assignedValue);
            }

            // Target can be null if not reflecting onto an object
            if (target is null)
            {
                return true;
            }

            try
            {
                if (Concatenate)
                {
                    if (assignedValue == null) return true;

                    var current = target.GetValue(Name);

                    if (CanConcatenate(current, assignedValue))
                    {
                        target.SetValue(Name, ConcatenateValues(current, assignedValue, ConcatenationString));
                    }
                    else
                    {
                        throw new TokenAssignmentException(this, $"Unable to concatenate type {assignedValue.GetType().Name} to {Name}");
                    }
                }
                else
                {
                    target.SetValue(Name, assignedValue);
                }
            }
            catch (MissingMemberException)
            {
                Log.Verbose($"Missing property on target: {Name}");

                if (options.IgnoreMissingProperties == false)
                {
                    throw;
                }
            }
            catch (TypeConversionException ex)
            {
                Log.Verbose($"{ex.Message}");

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

        internal bool CanAssign(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            // Trim trailing new line
            value = value.TrimTrailingNewLine();

            // Only check up to new line if set
            if (string.IsNullOrEmpty(value) == false && TerminateOnNewLine)
            {
                var index = value.IndexOf("\n");
                if (index > 0)
                {
                    value = value.Substring(0, index);
                }
            }

            object input = value;

            foreach (var decorator in Decorators)
            {
                if (decorator.IsTransformer)
                {
                    if (decorator.CanTransform(input, out var output) == false)
                    {
                        return false;
                    }
            
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

        internal bool CanConcatenate(object existingValue, object newValue)
        {
            if (existingValue is string && newValue is string)
            {
                return true;
            }

            return false;
        }

        internal object ConcatenateValues(object existingValue, object newValue, string concatenationString)
        {
            if (existingValue is string && newValue is string)
            {
                var concatStringValue = (concatenationString ?? string.Empty).Replace("<CR>", Environment.NewLine);

                return $"{existingValue}{concatStringValue}{newValue}";
            }

            return existingValue;
        }
    }
}