namespace Tokens
{
    public class TokenMatch<T> where T : class, new()
    {
        public int Matches { get; set; }

        public T Result { get; set; }

        public Template Template { get; set; }
    }
}