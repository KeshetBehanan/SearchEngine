using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SearchEngine.Data;

namespace SearchEngine.SearchWebsite.Pages
{
    public class SearchModel : PageModel
    {

        private const float NORMALIZED_RATIO = 2f;

        #region Input Properties

        /// <summary>
        /// The query.
        /// </summary>
        [FromQuery(Name = "q")]
        public string Query { get; set; }

        /// <summary>
        /// The result page number.
        /// </summary>
        [FromQuery(Name = "n")]
        public int ResultPageNumber { get; set; } = 1;

        /// <summary>
        /// Is it debug mode?
        /// </summary>
        [FromQuery(Name = "debug")]
        public bool IsDebug { get; set; }

        #endregion

        #region Display Properties

        /// <summary>
        /// The number of results of the search.
        /// </summary>
        public int TotalResults { get; set; }

        /// <summary>
        /// The time the search was taken.
        /// </summary>
        public TimeSpan TimeToLoad { get; set; }

        /// <summary>
        /// The list of the results of this page.
        /// </summary>
        public List<ResultRecord> Results { get; set; }

        #endregion

        #region Public Consts

        /// <summary>
        /// The number of results which is shown per page.
        /// </summary>
        public const int NUMBER_OF_RESULTS_PER_PAGE = 15;

        #endregion

        #region Private Members

        /// <summary>
        /// The data helper with the database.
        /// </summary>
        private readonly DataHelper dataHelper;

        #endregion

        #region Constructor


        /// <summary>
        /// The constructor of <see cref="SearchModel"/>.
        /// </summary>
        /// <param name="dataHelper">The data helper with the database.</param>
        public SearchModel(DataHelper dataHelper) => this.dataHelper = dataHelper;


        #endregion

        /// <summary>
        /// The method that is called when the client search for `/Search`.
        /// </summary>
        public IActionResult OnGet()
        {
            if(string.IsNullOrWhiteSpace(Query))
            {
                return Redirect("/");
            }
            if(ResultPageNumber < 1)
            {
                return Redirect($"/Search?q={HttpUtility.UrlEncode(Query)}");
            }

            var sw = new Stopwatch();
            sw.Start();

            var stemmer = new PorterStemmer();
            var input = Query.Split().Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var terms = new List<string>(input.Count);

            while(input.Count > 0)
            {
                var w1 = input.First();
                var w2 = w1.ToLower();

                if(w2.EndsWith("n't"))
                {
                    w1 = Regex.Replace(w1, @"n't", "", RegexOptions.IgnoreCase);
                    w2 = w2.Replace("n't", "");
                    if(!terms.Contains("not"))
                        terms.Add("not");
                }
                else if(w2.EndsWith("'ll"))
                {
                    w1 = Regex.Replace(w1, @"'ll", "", RegexOptions.IgnoreCase);
                    w2 = w2.Replace("'ll", "");
                    if(!terms.Contains("be"))
                        terms.Add("be");
                }
                else if(w2.EndsWith("'re"))
                {
                    w1 = Regex.Replace(w1, @"'re", "", RegexOptions.IgnoreCase);
                    w2 = w2.Replace("'re", "");
                    if(!terms.Contains("be"))
                        terms.Add("be");
                }
                else if(w2 == "i'm" || w2 == "he's" || w2 == "she's" || w2 == "it's" ||
                    w2 == "what's" || w2 == "when's" || w2 == "where's" || w2 == "how's" || w2 == "what's" || w2 == "why's")
                {
                    w1 = Regex.Replace(w1, @"'s|'m", "", RegexOptions.IgnoreCase);
                    w2 = w2.Replace("'s", "").Replace("'m", "");
                    if(!terms.Contains("be"))
                        terms.Add("be");
                }
                else if(w2.EndsWith("'s") || w2.EndsWith("s'"))
                {
                    w1 = Regex.Replace(w1, @"('s)|(s')$", "", RegexOptions.IgnoreCase);
                }
                if(w2 == "am" || w2 == "is" || w2 == "are" || w2 == "will" || w2 == "was" || w2 == "were" || w2 == "been" || w2 == "wo")
                    w1 = "be";

                string w3 = null;

                try
                {
                    w3 = stemmer.StemWord(w1);
                }
                catch { }

                if(!string.IsNullOrWhiteSpace(w3) && !terms.Contains(w3))
                    terms.Add(w3);

                input.RemoveAt(0);
            }
            if(terms.Count == 0)
            {
                return Redirect("/");
            }

            var normalizedTerms = terms.Where(x => x != x.ToLower()).Select(x => x.ToLower()).ToList();

            var results1 = dataHelper.Keywords.Where(x => terms.Contains(x.RootKeywordForm)).SelectMany(x => x.KeywordWebpageRecords).ToList().GroupBy(x => x.WebpageId)
                .Select(x => new
                {
                    WebpageId = x.Key,
                    Score = x.Sum(y => y.Score) * NORMALIZED_RATIO
                });

            var results2 = dataHelper.Keywords.Where(x => normalizedTerms.Contains(x.RootKeywordForm)).SelectMany(x => x.KeywordWebpageRecords).ToList().GroupBy(x => x.WebpageId)
                .Select(x => new
                {
                    WebpageId = x.Key,
                    Score = (float)x.Sum(y => y.Score)
                });

            var results = results1.Union(results2).GroupBy(x => x.WebpageId);

            TotalResults = results.Count();
            Results = results.Select(x => new 
            { 
                WebpageData = GetWebpageData(x.Key),
                Score = x.Sum(y => y.Score)
            }).OrderByDescending(x => x.Score).Skip((ResultPageNumber - 1) * NUMBER_OF_RESULTS_PER_PAGE).Take(NUMBER_OF_RESULTS_PER_PAGE)
            .Select(x => new ResultRecord()
            {
                Url = x.WebpageData.Url,
                Title = x.WebpageData.Title,
                Description = x.WebpageData.Description,
                Score = x.Score
            }).ToList();

            sw.Stop();
            TimeToLoad = sw.Elapsed;
            return Page();
        }

        /// <summary>
        /// Gets the data of the webpage.
        /// </summary>
        /// <param name="id">The ID of the webpage.</param>
        /// <returns>The URL, title and description of the webpage.</returns>
        private (string Url, string Title, string Description) GetWebpageData(int id)
        {
            var webpage = dataHelper.Index.Include(x => x.Metadata).First(x => id == x.Id);
            //var metadata = dataHelper.Index.First(x => id == x.Id).Metadata;
            return (webpage.Url.AbsoluteUri, webpage.Metadata?.Title, webpage.Metadata?.Description);
        }
    }
}