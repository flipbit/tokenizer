using System;
using System.Collections.Generic;
using System.Linq;

namespace Tokens
{
    /// <summary>
    /// Base factory class to perform operations on <see cref="Token"/> objects.
    /// </summary>
    public abstract class BaseTokenFactory<T> where T : class
    {
        /// <summary>
        /// Gets or sets the function parser.
        /// </summary>
        /// <value>
        /// The function parser.
        /// </value>
        public FunctionParser FunctionParser { get; set; }

        /// <summary>
        /// Gets the operators.
        /// </summary>
        /// <value>
        /// The operators.
        /// </value>
        public IList<T> Items { get; private set; }

        /// <summary>
        /// Initializes the <see cref="BaseTokenFactory"/> class.
        /// </summary>
        public BaseTokenFactory()
        {
            FunctionParser = new FunctionParser();
            Items = new List<T>();

            var types = GetType().Assembly.GetTypes();

            foreach (var type in types)
            {
                if (type.GetInterfaces().Contains(typeof(T)) && !type.IsInterface)
                {
                    var @operator = Activator.CreateInstance(type) as T;

                    Items.Add(@operator);
                }
            }
        }
    }
}
