using System;

namespace Tokens.Transformers;

public class BlowsUpTransformer : ITokenTransformer
{
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        throw new NotImplementedException();
    }
}
