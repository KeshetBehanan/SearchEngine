using System;
using System.Collections.Generic;
using System.Text;

namespace SearchEngine.WebCrawler
{
    /// <summary>
    /// The config of the program.
    /// </summary>
    internal class ProgramConfig
    {
        /// <summary>
        /// The number of crawlers crawling in the same time.
        /// </summary>
        public int NumberOfCrawlers { get; set; }

        /// <summary>
        /// The user agent of the crawler.
        /// </summary>
        public string Crawler_UserAgent { get; set; }

        /// <summary>
        /// The string which used for connecting the database.
        /// </summary>
        public string Crawler_ConnectionString { get; set; }

        /// <summary>
        /// The max webpages the crawler will wait for finishing the process of them.
        /// </summary>
        public int Crawler_MaxWaitForWebpages { get; set; }

        /// <summary>
        /// The time in seconds that a webpage has to be loaded.
        /// </summary>
        public int Crawler_TimeoutInSeconds { get; set; }
    }
}
