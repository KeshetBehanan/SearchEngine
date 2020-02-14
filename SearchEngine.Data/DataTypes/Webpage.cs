using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace SearchEngine.Data
{
    /// <summary>
    /// A record of an indexed webpage
    /// </summary>
    /// 
    public class Webpage
    {
        #region Public Properties

        /// <summary>
        /// The ID of the <see cref="Webpage"/>.
        /// </summary>
        [Key]
        public int Id { get; private set; }

        /// <summary>
        /// The GUID of the <see cref="Webpage"/>.
        /// </summary>
        public Guid Guid { get; private set; }

        /// <summary>
        /// The <see cref="DateTime"/> when the <see cref="Webpage"/> was added to the queue.
        /// </summary>
        public DateTime AddedAt { get; private set; }

        /// <summary>
        /// The URL of the <see cref="Webpage"/>.
        /// </summary>
        [MaxLength(2048)]
        public Uri Url { get; private set; }

        /// <summary>
        /// The domain name of this webpage.
        /// </summary>
        public DomainName Domain { get; private set; }

        /// <summary>
        /// The metadata of the <see cref="Webpage"/>.
        /// </summary>
        [AllowNull]
        public Metadata Metadata { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// The default private constructor of <see cref="Webpage"/>.
        /// </summary>
        /// <param name="url">The URL of the <see cref="Webpage"/>.</param>
        private Webpage(Uri url)
        {
            Guid = Guid.NewGuid();
            AddedAt = DateTime.Now;
            Url = url;
        }

        /// <summary>
        /// The default constructor of <see cref="Webpage"/>.
        /// </summary>
        /// <param name="url">The URL of the <see cref="Webpage"/>.</param>
        /// <param name="metadata">The metadata of the <see cref="Webpage"/>.</param>
        /// <param name="domain">The domain name of this webpage.</param>
        public Webpage(Uri url, Metadata metadata, DomainName domain) : this(url)
        {
            Metadata = metadata;
            Domain = domain;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the metadata for the webpage.
        /// </summary>
        /// <param name="metadata">The metadata of the <see cref="Webpage"/>.</param>
        public void SetMetadata(Metadata metadata)
        {
            Metadata = metadata;
        }

        #endregion
    }
}
