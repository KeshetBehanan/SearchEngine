using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace SearchEngine.Data
{
    /// <summary>
    /// An URL Record in the queue of the web crawler.
    /// </summary>
    public class UrlRecord
    {
        #region Public Properties

        /// <summary>
        /// The ID of the record.
        /// </summary>
        [Key, NotNull]
        public int Id { get; private set; }

        /// <summary>
        /// The <see cref="DateTime"/> when the <see cref="UrlRecord"/> was added to the queue.
        /// </summary>
        [NotNull]
        public DateTime AddedAt { get; private set; }

        /// <summary>
        /// The URL of the record.
        /// </summary>
        [MaxLength(2048), NotNull]
        public Uri Url { get; private set; }

        /// <summary>
        /// The <see cref="DomainName"/> of this URL Record.
        /// </summary>
        [NotNull]
        public DomainName Domain { get; private set; }

        #endregion

        #region Internal Properties

        #endregion

        #region Constructors

        /// <summary>
        /// The constructor of <see cref="UrlRecord"/>.
        /// </summary>
        /// <param name="url">The URL of the record.</param>
        private UrlRecord(Uri url)
        {
            AddedAt = DateTime.Now;
            Url = url;
        }

        /// <summary>
        /// The constructor of <see cref="UrlRecord"/>.
        /// </summary>
        /// <param name="url">The URL of the record.</param>
        private UrlRecord(string url)
        {
            AddedAt = DateTime.Now;
            Url = new Uri(url);
        }

        /// <summary>
        /// The constructor of <see cref="UrlRecord"/>.
        /// </summary>
        /// <param name="url">The URL of the record.</param>
        public UrlRecord(Uri url, DomainName domain) : this(url)
        {
            Domain = domain;
        }

        /// <summary>
        /// The constructor of <see cref="UrlRecord"/>.
        /// </summary>
        /// <param name="url">The URL of the record.</param>
        public UrlRecord(string url, DomainName domain) : this(url)
        {
            Domain = domain;
        }

        #endregion
    }
}
