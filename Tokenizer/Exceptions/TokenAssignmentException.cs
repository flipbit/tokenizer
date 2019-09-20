using System;

namespace Tokens.Exceptions
{
    public class TokenAssignmentException : TokenizerException
    {
        public Token Token { get; }

        public TokenAssignmentException(Token token, string message) : base(message)
        {
            Token = token;
        }

        public TokenAssignmentException(Token token, Exception innerException) : base($"Unable to assign: {token.Name}", innerException)
        {
            Token = token;
        }
    }
}
