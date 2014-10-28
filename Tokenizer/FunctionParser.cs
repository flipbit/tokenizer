using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tokens
{
    /// <summary>
    /// Parses <see cref="Function"/> objects from strings for use in <see cref="Token"/> 
    /// validation and operations.
    /// </summary>
    public class FunctionParser
    {
        /// <summary>
        /// Parses the specified input and returns a <see cref="Function"/>.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public IList<Function> Parse(string input)
        {
            var functions = new List<Function>();

            if (!string.IsNullOrEmpty(input))
            {
                var segements = input.Split(new[] {"&&"}, StringSplitOptions.RemoveEmptyEntries);

                foreach (var segement in segements)
                {
                    functions.Add(ParseSegment(segement.Trim()));
                }
            }

            return functions;
        }

        private Function ParseSegment(string input)
        {
            var function = new Function();

            function.Name = input.SubstringBeforeString("(");

            var functionRegex = Regex.Match(input, @"\b[^()]+\((.*)\)$");

            if (!functionRegex.Success)
            {
                throw new InvalidOperationException("Can't parse function: " + input);
            }

            var parameters = functionRegex.Groups[1].Value;
            var parametersRegex = Regex.Matches(parameters, @"([^,]+\(.+?\))|([^,]+)");

            foreach (var item in parametersRegex)
            {
                function.Parameters.Add(item.ToString());
            }

            return function;
        }
    }
}
