using System;
using System.Collections.Generic;
using System.Linq;

namespace Tokens
{
    /// <summary>
    /// Class that creates objects and populates their properties with values
    /// from input strings
    /// </summary>
    public class Tokenizer
    {
        /// <summary>
        /// Gets or sets the operator factory.
        /// </summary>
        /// <value>
        /// The operator factory.
        /// </value>
        public TokenOperatorFactory OperatorFactory { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tokenizer"/> class.
        /// </summary>
        public Tokenizer()
        {
            OperatorFactory = new TokenOperatorFactory();
        }

        /// <summary>
        /// Parses the given input and creates an object with values matching the specified pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pattern">The pattern.</param>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public TokenResult<T> ParseBlock<T>(string pattern, string input) where T : class, new()
        {
            var result = new T();

            return ParseBlock(result, pattern, input);
        }

        /// <summary>
        /// Parses the given input and creates an object with values matching the specified pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">The target.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public TokenResult<T> ParseBlock<T>(T target, string pattern, string input) where T : class
        {
            var result = new TokenResult<T>(target);

            return ParseBlock(result, pattern, input);
        }

        private TokenResult<T> ParseBlock<T>(TokenResult<T> result, string pattern, string input) where T : class
        {
            // Extract all the tokens from the pattern
            var tokens = GetTokens(pattern);

            // Parse input
            return ParseBlock(result, tokens, input, new string[0]);
        }

        /// <summary>
        /// Parses the given input and creates an object with values matching the specified pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result">The result.</param>
        /// <param name="tokens">The tokens.</param>
        /// <param name="input">The input.</param>
        /// <param name="processed">The lines that have already been processed.</param>
        /// <returns></returns>
        private TokenResult<T> ParseBlock<T>(TokenResult<T> result, IEnumerable<Token> tokens, string input, IList<string> processed) where T : class
        {
            foreach (var token in tokens)
            {
                // Check for missing operatations
                if (OperatorFactory.HasMissingFunctions(token))
                {
                    throw new ArgumentException("Token has missing operators: " + token.Operation);
                }

                // Check token prerequisites
                if (!token.PrerequisiteSatisfied(processed)) continue;

                // Skip already replaced tokens
                if (token.Replaced) continue;               
                
                // Ignore tokens that aren't contained in the input
                if (!token.ContainedIn(input)) continue;

                // Extract token value from the input text
                object value = input
                    .SubstringAfterString(token.Prefix)
                    .SubstringBeforeString(token.Suffix)
                    .Trim();

                // Perform Validation
                if (!OperatorFactory.Validate(token, value.ToString()))
                {
                    continue;
                }

                // Perform token operation
                value = OperatorFactory.PerformOperation(token, value.ToString());

                // Use reflection to set the property on the object with the token value
                result.Value = SetValue(result.Value, token.Value, value);

                // Add the match to the result collection
                result.Replacements.Add(token);

                // Remove token so it's not replaced again
                token.Replaced = true;

                break;
            }

            return result;            
        }

        /// <summary>
        /// Parses the given input and creates an object with values matching the specified pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pattern">The pattern.</param>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public TokenResult<T> Parse<T>(string pattern, string input) where T : class, new()
        {
            var target = new T();

            return Parse(target, pattern, input);
        }

        /// <summary>
        /// Parses the given input and creates an object with values matching the specified pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">The target.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public TokenResult<T> Parse<T>(T target, string pattern, string input) where T : class
        {
            var result = new TokenResult<T>(target);

            var tokens = GetTokens(pattern);
            var processed = new List<string>();

            foreach (var line in input.ToLines())
            {
                if (string.IsNullOrEmpty(line)) continue;

                result = ParseBlock(result, tokens, line, processed);

                processed.Add(line);
            }

            return result;
        }


        /// <summary>
        /// Gets the next token that appears in the given pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="prerequisite">The prerequisite.</param>
        /// <returns></returns>
        public Token GetNextToken(string pattern, string prerequisite)
        {
            var token = new Token();

            token.Prefix = pattern.SubstringBeforeString("#{");
            token.Suffix = pattern.SubstringAfterString("}").SubstringBeforeString("#{");
            token.Value = pattern.SubstringBeforeString("}").SubstringAfterString("#{");
            token.Prerequisite = prerequisite;

            // Limit prefix/suffix to the rest of the line
            token.Prefix = token.Prefix.SubstringAfterLastAnyString("\r\n", "\r", "\n");
            token.Suffix = token.Suffix.SubstringBeforeAnyString("\r\n", "\r", "\n");

            // Make whitespace empty strings
            if (token.Suffix.IsNullOrWhiteSpace()) token.Suffix = string.Empty;
            if (token.Prefix.IsNullOrWhiteSpace()) token.Prefix = string.Empty;

            if (token.Value.Contains(":"))
            {
                token.Operation = token.Value.SubstringAfterString(":");
                token.Value = token.Value.SubstringBeforeString(":");
            }

            return token;
        }

        /// <summary>
        /// Gets the tokens present in the given pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IList<Token> GetTokens(string pattern)
        {
            var results = new List<Token>();

            var lines = pattern.ToLines();
            var previousLine = string.Empty;

            foreach (var line in lines)
            {
                var currentLine = line.Clone() as string;

                if (string.IsNullOrEmpty(currentLine)) continue;

                if (!currentLine.Contains("#{"))
                {
                    previousLine = line;

                    continue;
                }

                while (currentLine.Contains("#{"))
                {
                    var token = GetNextToken(currentLine, previousLine);

                    results.Add(token);

                    currentLine = currentLine.SubstringAfterString("}");
                }

                //previousLine = currentLine;
            }

            return results;
        }

        /// <summary>
        /// Sets the given value on the given propetrty with the given path.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object">The object.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Property Path Too Short:  + propertyPath</exception>
        public T SetValue<T>(T @object, string propertyPath, object value) where T : class
        {
            var segments = propertyPath.Split('.');
            var objectType = @object.GetType().Name;

            // Must have at least a single property (e.g. "Object.Property")
            if (segments.Length < 2) throw new ArgumentException("Property Path Too Short: " + propertyPath);

            // Check object type
            if (objectType != segments[0]) throw new ArgumentException(string.Format("Invalid Property Path for {0}: {1}", objectType, propertyPath));

            @object = SetInnerValue(@object, segments.Skip(1).ToArray(), value) as T;

            return @object;
        }

        private object SetInnerValue(object @object, string[] path, object value)
        {
            var propertyInfos = @object.GetType().GetProperties();

            foreach (var propertyInfo in propertyInfos)
            {
                if (propertyInfo.Name != path[0]) continue;

                if (path.Length == 1)
                {
                    if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
                    {
                        var list = propertyInfo.GetValue(@object, null); 

                        if (list == null)
                        {
                            var genericType = propertyInfo.PropertyType.GetGenericArguments()[0];
                            var enumerableType = typeof (List<>);
                            var constructedEnumerableType = enumerableType.MakeGenericType(genericType);
                            list = Activator.CreateInstance(constructedEnumerableType);

                            propertyInfo.SetValue(@object, list, null);
                        }

                        list.GetType().GetMethod("Add").Invoke(list, new[] { value });
                    }
                    else
                    {
                        var convertedValue = Convert.ChangeType(value, propertyInfo.PropertyType);

                        propertyInfo.SetValue(@object, convertedValue, null);
                    }

                    break;
                }

                var currentValue = propertyInfo.GetValue(@object, null);

                if (currentValue == null)
                {
                    currentValue = Activator.CreateInstance(propertyInfo.PropertyType);

                    propertyInfo.SetValue(@object, currentValue, null);
                }

                SetInnerValue(currentValue, path.Skip(1).ToArray(), value);

                break;
            }

            return @object;
        }
    }
}
