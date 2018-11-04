using System;
using System.Collections.Generic;
using System.Linq;

namespace Tokens.Transformers
{
    public class TransformerContext
    {
        public TransformerContext(Type tokenOperator)
        {
            OperatorType = tokenOperator;
            Parameters = new List<string>();
        }

        public Type OperatorType { get; }

        public ITokenTransformer CreateOperator()
        {
            return (ITokenTransformer) Activator.CreateInstance(OperatorType);
        }

        public IList<string> Parameters { get; }

        public object Transform(object value)
        {
            var instance = CreateOperator();

            return instance.Transform(value, Parameters.ToArray());
        }
    }
}
