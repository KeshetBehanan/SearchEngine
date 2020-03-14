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

            Results = new List<ResultRecord>()
            {
                new ResultRecord()
                {
                    Title = "Free Web Hosting - Host a Website for Free with Cpanel, PHP",
                    Url = "https://www.000webhost.com/",
                    Description = "Absolutely free web hosting with cPanel, PHP & MySQL for a stunning blogging start. Get free website hosting together with a free domain name at no cost at all!",
                    Score = 1000
                },
                new ResultRecord()
                {
                    Title = "Twitch",
                    Url = "https://www.twitch.tv/",
                    Description = "Twitch is the world’s leading live streaming platform for gamers and the things we love. Watch and chat now with millions of other fans from around the world",
                    Score = 900
                },
                new ResultRecord()
                {
                    Title = "wikiHow: How-to instructions you can trust.",
                    Url = "https://www.wikihow.com/Main-Page",
                    Description = "Learn how to do anything with wikiHow, the world's most popular how-to website. Easy, well-researched, and trustworthy instructions for everything you want to k",
                    Score = 800
                },
                new ResultRecord()
                {
                    Title = "Moz - SEO Software, Tools & Resources for Smarter Marketing",
                    Url = "https://moz.com/",
                    Score = 700
                },
                new ResultRecord()
                {
                    Url = "https://www.wikipedia.org/",
                    Score = 600
                }
            };

            sw.Stop();
            TimeToLoad = sw.Elapsed;
            return Page();
        }
    }
}