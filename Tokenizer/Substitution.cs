namespace Tokens
{
    /// <summary>
    /// Represent a <see cref="Token"/> substitution
    /// </summary>
    public class Substitution
    {
        /// <summary>
        /// Gets or sets the name of the <see cref="Token"/> substituted
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the substition
        /// </summary>
        public object Value { get; set; }
    }
}
