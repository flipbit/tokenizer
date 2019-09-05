﻿using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Transformers
{
    /// <summary>
    /// Trims the token value after the first occurence of the given string 
    /// </summary>
    public class SubstringBeforeLastTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString())) 
            { 
                transformed = string.Empty;
                return true;
            }

            if (args == null || args.Length == 0) throw new TokenizerException($"SubstringBeforeLast(): missing argument processing: {value}");

            transformed = value.ToString().SubstringBeforeLastString(args[0]);

            return true;
        }
    }
}
