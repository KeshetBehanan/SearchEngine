using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SearchEngine.Data
{
    /// <summary>
    /// The type of a keyword.
    /// </summary>
    public class Keyword
    {
        #region Public Properties

        /// <summary>
        /// The ID of this keyword.
        /// </summary>
        [Key, NotNull]
        public int Id { get; private set; }

        /// <summary>
        /// The root form of a keyword.
        /// </summary>
        /// <remarks>
        /// The use of Porter Stemming Algorithm needed for this form of the keyword.
        /// </remarks>
        [MaxLength(64), NotNull, Required]
        public string RootKeywordForm { get; private set; }

        /// <summary>
        /// List of all the <see cref="KeywordWebpageRecord"/>s linked to this keyword.
        /// </summary>
        public List<KeywordWebpageRecord> KeywordWebpageRecords { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// The constructor of <see cref="Keyword"/>.
        /// </summary>
        /// <param name="rootKeywordForm">The root form of a keyword.</param>
        public Keyword(string rootKeywordForm)
        {
            RootKeywordForm = rootKeywordForm;
            KeywordWebpageRecords = new List<KeywordWebpageRecord>();
        }

        #endregion
    }
}
