using System;
using System.Collections.Generic;
using System.Linq;
using Tokens.Validators;

namespace Tokens
{
    public class ValidatorContext
    {
        public Type ValidatorType { get; }

        public ValidatorContext(Type validatorType)
        {
            ValidatorType = validatorType;
            Parameters = new List<string>();
        }

        public ITokenValidator CreateValidator()
        {
            return (ITokenValidator) Activator.CreateInstance(ValidatorType);
        }


        public IList<string> Parameters { get; }

        public bool Validate(object value)
        {
            var instance = CreateValidator();

            return instance.IsValid(value, Parameters.ToArray());
        }
    }
}