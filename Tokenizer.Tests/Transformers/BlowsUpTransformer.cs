using System;

namespace Tokens.Transformers
{
    public class BlowsUpTransformer : ITokenTransformer
    {
        public bool CanTransform(object value, string[] args, out object transformed)
        {
            throw new NotImplementedException();
        }
    }
}
