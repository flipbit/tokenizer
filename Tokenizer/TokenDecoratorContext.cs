﻿using System;
using System.Collections.Generic;
using System.Linq;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens
{
    /// <summary>
    /// Contains an instance of a <see cref="ITokenDecorator"/> that can perform
    /// an operation on a <see cref="Token"/>.
    /// </summary>
    public class TokenDecoratorContext
    {
        public TokenDecoratorContext(Type tokenDecorator)
        {
            DecoratorType = tokenDecorator;
            Parameters = new List<string>();
        }

        /// <summary>
        /// Specifies the decorator type
        /// </summary>
        public Type DecoratorType { get; }

        /// <summary>
        /// Creates an instance of the decorator
        /// </summary>
        /// <returns></returns>
        public ITokenDecorator CreateDecorator()
        {
            return (ITokenDecorator) Activator.CreateInstance(DecoratorType);
        }

        /// <summary>
        /// Contains the parameters to pass the decorator
        /// </summary>
        public IList<string> Parameters { get; }

        /// <summary>
        /// Returns <c>true</c> if the decorator is a <see cref="ITokenTransformer"/> used to transform
        /// the token value.
        /// </summary>
        public bool IsTransformer => typeof(ITokenTransformer).IsAssignableFrom(DecoratorType);

        /// <summary>
        /// Returns <c>true</c> if the decorator is a <see cref="ITokenValidator"/> used to validate
        /// the token value.
        /// </summary>
        public bool IsValidator => typeof(ITokenValidator).IsAssignableFrom(DecoratorType);

        /// <summary>
        /// Determines if this validator should reverse it's output
        /// </summary>
        public bool IsNotValidator { get; set; }

        /// <summary>
        /// Transforms the token value.
        /// </summary>
        public bool CanTransform(object value, out object transformed)
        {
            var instance = (ITokenTransformer) CreateDecorator();

            return instance.CanTransform(value, Parameters.ToArray(), out transformed);
        }

        /// <summary>
        /// Validates the token value.
        /// </summary>
        public bool Validate(object value)
        {
            var instance = (ITokenValidator) CreateDecorator();

            if (IsNotValidator)
            {
                return !instance.IsValid(value, Parameters.ToArray());
            }

            return instance.IsValid(value, Parameters.ToArray());
        }
    }
}
