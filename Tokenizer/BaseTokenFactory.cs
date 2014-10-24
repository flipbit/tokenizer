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
