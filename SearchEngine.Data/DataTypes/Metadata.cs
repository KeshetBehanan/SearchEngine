using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Web;

namespace SearchEngine.Data
{
    /// <summary>
    /// The metadata of the <see cref="Webpage"/>.
    /// </summary>
    public class Metadata
    {
        #region Private Consts

        /// <summary>
        /// The maximum length of a valid title.
        /// </summary>
        private const int TITLE_MAX_LENGTH = 96;

        /// <summary>
        /// The maximum length of a valid description.
        /// </summary>
        private const int DESCRIPTION_MAX_LENGTH = 160;

        #endregion

        #region Public Properties

        /// <summary>
        /// The ID of the <see cref="Metadata"/>.
        /// </summary>
        [Key]
        public int Id { get; private set; }

        /// <summary>
        /// The title of the webpage.
        /// </summary>
        [MaxLength(TITLE_MAX_LENGTH), AllowNull]
        public string Title { get; set; }

        /// <summary>
        /// The description of the webpage.
        /// </summary>
        [MaxLength(DESCRIPTION_MAX_LENGTH), AllowNull]
        public string Description { get; set; }

        #endregion

        #region Internal Properties

        /// <summary>
        /// The webpage of this metadata.
        /// </summary>
        internal Webpage Webpage { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Empty constructor.
        /// </summary>
        public Metadata()
        {
            Title = null;
            Description = null;
        }

        /// <summary>
        /// The constructor of <see cref="Metadata"/>.
        /// </summary>
        /// <param name="title">The title of the webpage.</param>
        /// <param name="description">The description of the webpage.</param>
        public Metadata(string title, string description)
        {
            // Check the lengthes of the strings, and assign to the properties.
            if(title != null)
            {
                title = HttpUtility.HtmlDecode(title);
                title = title.Trim();
                title = Regex.Replace(title, @"\s+", " ");
                Title = title.Length > TITLE_MAX_LENGTH ? title.Remove(TITLE_MAX_LENGTH) : title;
            }
            if(description != null)
            {
                description = HttpUtility.HtmlDecode(description);
                description = description.Trim();
                description = Regex.Replace(description, @"\s+", " ");
                Description = description.Length > DESCRIPTION_MAX_LENGTH ? description.Remove(DESCRIPTION_MAX_LENGTH) : description;
            }
        }

        #endregion
    }
}