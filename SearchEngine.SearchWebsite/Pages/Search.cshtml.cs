using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SearchEngine.Data;

namespace SearchEngine.SearchWebsite.Pages
{
    public class SearchModel : PageModel
    {
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
            var terms = Query.Split(' ').Select(x => stemmer.StemWord(x)).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if(terms.Length == 0)
            {
                return Redirect("/");
            }

            var results = dataHelper.Keywords.Where(x => x.RootKeywordForm == terms[0]).SelectMany(x => x.KeywordWebpageRecords)
                .Select(x => new ResultRecord()
                {
                    Url = x.Webpage.Url.AbsoluteUri,
                    Title = x.Webpage.Metadata.Title,
                    Description = x.Webpage.Metadata.Description,
                    Score = x.Score
                });

            TotalResults = results.Count();
            Results = results.OrderByDescending(x => x.Score).Skip((ResultPageNumber - 1) * NUMBER_OF_RESULTS_PER_PAGE).Take(NUMBER_OF_RESULTS_PER_PAGE).ToList();

            sw.Stop();
            TimeToLoad = sw.Elapsed;
            return Page();
        }

        private static bool IsBelongs(Keyword x, string[] terms, bool normlize)
        {
            foreach(var term in terms)
            {
                if((normlize ? x.RootKeywordForm.ToLower() : x.RootKeywordForm) == term)
                    return true;
            }
            return false;
        }
    }
}