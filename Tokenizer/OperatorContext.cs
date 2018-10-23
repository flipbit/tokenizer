using System;
using System.Collections.Generic;
using Tokens.Operators;

namespace Tokens
{
    public class OperatorContext
    {
        public OperatorContext(Type tokenOperator)
        {
            OperatorType = tokenOperator;
            Parameters = new List<string>();
        }

        public Type OperatorType { get; }

        public ITokenOperator CreateOperator()
        {
            return (ITokenOperator) Activator.CreateInstance(OperatorType);
        }

        public IList<string> Parameters { get; }
    }
}
