using System;

namespace Tokens.Transformers
{
    public class BlowsUpTransformer : ITokenTransformer
    {
        public object Transform(object value, params string[] args)
        {
            throw new NotImplementedException();
        }
    }
}
