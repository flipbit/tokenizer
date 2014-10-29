using System;
using System.Collections.Generic;

namespace Tokens.Samples.Classes
{
    /// <summary>
    /// Represents a WHOIS server for a domain
    /// </summary>
    public class WhoisServer
    {
        /// <summary>
        /// Defines a TLD contact
        /// </summary>
        public class Contact
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Contact"/> class.
            /// </summary>
            public Contact()
            {
                Address = new List<string>();
            }

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the organization.
            /// </summary>
            /// <value>
            /// The organization.
            /// </value>
            public string Organization { get; set; }

            /// <summary>
            /// Gets or sets the address.
            /// </summary>
            /// <value>
            /// The address.
            /// </value>
            public IList<string> Address { get; set; }

            /// <summary>
            /// Gets or sets the phone number.
            /// </summary>
            /// <value>
            /// The phone number.
            /// </value>
            public string PhoneNumber { get; set; }

            /// <summary>
            /// Gets or sets the fax number.
            /// </summary>
            /// <value>
            /// The fax number.
            /// </value>
            public string FaxNumber { get; set; }

            /// <summary>
            /// Gets or sets the email.
            /// </summary>
            /// <value>
            /// The email.
            /// </value>
            public string Email { get; set; }
        }

        /// <summary>
        /// Represents the TLD Organisation
        /// </summary>
        public class WhoisOrganization
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="WhoisOrganization"/> class.
            /// </summary>
            public WhoisOrganization()
            {
                Address = new List<string>();
            }

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the address.
            /// </summary>
            /// <value>
            /// The address.
            /// </value>
            public IList<string> Address { get; set; } 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WhoisServer"/> class.
        /// </summary>
        public WhoisServer()
        {
            NameServers = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WhoisServer"/> class.
        /// </summary>
        /// <param name="tld">The TLD.</param>
        /// <param name="url">The URL.</param>
        public WhoisServer(string tld, string url) : base()
        {
            TLD = tld;
            Url = url;
        }

        /// <summary>
        /// Gets or sets the TLD for this server.
        /// </summary>
        /// <value>
        /// The TLD.
        /// </value>
        public string TLD { get; set; }

        /// <summary>
        /// Gets the URL of the WHOIS server.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the organization.
        /// </summary>
        /// <value>
        /// The organization.
        /// </value>
        public WhoisOrganization Organization { get; set; }

        /// <summary>
        /// Gets or sets the admin contact.
        /// </summary>
        /// <value>
        /// The admin contact.
        /// </value>
        public Contact AdminContact { get; set; }

        /// <summary>
        /// Gets or sets the tech contact.
        /// </summary>
        /// <value>
        /// The tech contact.
        /// </value>
        public Contact TechContact { get; set; }

        /// <summary>
        /// Gets or sets the name servers.
        /// </summary>
        /// <value>
        /// The name servers.
        /// </value>
        public IList<string> NameServers { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the create.
        /// </summary>
        /// <value>
        /// The create.
        /// </value>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the changed.
        /// </summary>
        /// <value>
        /// The changed.
        /// </value>
        public DateTime Changed { get; set; }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public string Source { get; set; }

        public string Remarks { get; set; }
    }
}
