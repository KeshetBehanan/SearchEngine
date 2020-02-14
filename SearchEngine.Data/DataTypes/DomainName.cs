using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace SearchEngine.Data
{
    /// <summary>
    /// The type of a domain name.
    /// </summary>
    public class DomainName
    {
        #region Public Properties

        /// <summary>
        /// The ID of the domain name.
        /// </summary>
        [Key, NotNull]
        public int Id { get; private set; }

        /// <summary>
        /// The domain name.
        /// </summary>
        [MaxLength(253), NotNull, Required]
        public string Domain { get; private set; }

        /// <summary>
        /// The priority of this domain.
        /// </summary>
        [Editable(true)]
        public byte Priority { get; set; }

        /// <summary>
        /// Represents the URL 
        /// </summary>
        public List<UrlRecord> UrlRecords { get; private set; }

        #endregion

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="domain">The domain.</param>
        public DomainName(string domain)
        {
            Domain = domain;
            Priority = 0;
            UrlRecords = new List<UrlRecord>();
        }

        /// <summary>
        /// Adds an URL Record to the <see cref="UrlRecords"/> list.
        /// </summary>
        /// <param name="url">The <see cref="UrlRecord"/> to add.</param>
        public void AddUrlRecord(UrlRecord url)
        {
            UrlRecords.Add(url);
        }
    }
}