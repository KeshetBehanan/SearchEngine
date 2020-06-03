using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace SearchEngine.Data
{
    /// <summary>
    /// A record which links between <see cref="Data.Keyword"/> and <see cref="Data.Webpage"/>.
    /// </summary>
    public class KeywordWebpageRecord
    {
        #region Public Properties

        /// <summary>
        /// The ID of the record.
        /// </summary>
        [Key, NotNull]
        public int Id { get; private set; }

        /// <summary>
        /// The webpage.
        /// </summary>
        [NotNull]
        public Webpage Webpage { get; private set; }

        /// <summary>
        /// The ID of the <see cref="Webpage"/>.
        /// </summary>
        public int WebpageId { get; private set; }

        /// <summary>
        /// The keyword.
        /// </summary>
        [NotNull]
        public Keyword Keyword { get; private set; }

        /// <summary>
        /// The score of the keyword in this webpage.
        /// </summary>
        [NotNull]
        public int Score { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// The private constructor of <see cref="KeywordWebpageRecord"/>.
        /// </summary>
        /// <param name="score">The score of the keyword in this webpage.</param>
        private KeywordWebpageRecord(int score)
        {
            Score = score;
        }

        /// <summary>
        /// The constructor of <see cref="KeywordWebpageRecord"/>.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        /// <param name="webpage">The webpage.</param>
        /// <param name="score">The score of the keyword in this webpage.</param>
        public KeywordWebpageRecord(Webpage webpage, Keyword keyword, int score) : this(score)
        {
            Webpage = webpage;
            Keyword = keyword;
        }

        #endregion
    }
}
