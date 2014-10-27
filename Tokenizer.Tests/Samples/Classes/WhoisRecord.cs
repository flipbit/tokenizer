using System;
using System.Collections.Generic;

namespace Tokens.Samples.Classes
{
    /// <summary>
    /// Represents WHOIS information for a domain.
    /// </summary>
    public class WhoisRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhoisRecord"/> class.
        /// </summary>
        public WhoisRecord()
        {
            NameServers = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WhoisRecord"/> class.
        /// </summary>
        /// <param name="text">The text.</param>
        public WhoisRecord(string text)
        {
            NameServers = new List<string>();

            Text = text;
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the domain.
        /// </summary>
        /// <value>The domain.</value>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>The server.</value>
        public WhoisServer Server { get; set; }

        /// <summary>
        /// Gets or sets the date the domain was created.
        /// </summary>
        /// <value>The created.</value>
        public DateTime? Created { get; set; }

        /// <summary>
        /// Gets or sets the registrant.
        /// </summary>
        /// <value>
        /// The registrant.
        /// </value>
        public Contact Registrant { get; set; }

        /// <summary>
        /// Gets or sets the technical contact.
        /// </summary>
        /// <value>
        /// The technical contact.
        /// </value>
        public Contact TechnicalContact { get; set; }

        /// <summary>
        /// Gets or sets the admin contact.
        /// </summary>
        /// <value>
        /// The admin contact.
        /// </value>
        public Contact AdminContact { get; set; }

        /// <summary>
        /// Gets the nameservers.
        /// </summary>
        /// <value>
        /// The nameservers.
        /// </value>
        public IList<string> NameServers { get; private set; }

        /// <summary>
        /// Gets or sets the expiry date.
        /// </summary>
        /// <value>
        /// The expiry date.
        /// </value>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the modified date.
        /// </summary>
        /// <value>
        /// The modified date.
        /// </value>
        public DateTime ModificationDate { get; set; }

        /// <summary>
        /// Gets or sets the name of the registrar.
        /// </summary>
        /// <value>
        /// The name of the registrar.
        /// </value>
        public string RegistrarName { get; set; }

        /// <summary>
        /// Gets or sets the registrar URL.
        /// </summary>
        /// <value>
        /// The registrar URL.
        /// </value>
        public string RegistrarUrl { get; set; }
    }
}