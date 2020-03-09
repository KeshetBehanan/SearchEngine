using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SearchEngine.SearchWebsite.Pages
{
    public class SearchModel : PageModel
    {
        [FromQuery(Name = "q")]
        public string Query { get; set; }

        [FromQuery(Name = "n")]
        public int ResultPageNumber { get; set; } = 1;

        [FromQuery(Name = "debug")]
        public bool IsDebug { get; set; }

        public int TotalResults { get; set; }

        public TimeSpan TimeToLoad { get; set; }

        public List<ResultRecord> Results { get; set; }

        public const int NUMBER_OF_RESULTS_PER_PAGE = 15;

        public IActionResult OnGet()
        {
            if(string.IsNullOrWhiteSpace(Query))
            {
                return Redirect("/");
            }
            if(ResultPageNumber < 1)
            {
                throw new Exception("error");
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

            TotalResults = 13442;

            sw.Stop();
            TimeToLoad = sw.Elapsed;
            return Page();
        }
    }
}