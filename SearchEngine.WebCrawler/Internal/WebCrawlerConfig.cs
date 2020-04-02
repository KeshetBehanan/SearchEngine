using SearchEngine.Data;

namespace SearchEngine.WebCrawler
{
    /// <summary>
    /// Config for the <see cref="DataHelper"/> class.
    /// </summary>
    public sealed class WebCrawlerConfig
    {
        /// <summary>
        /// The user agent of the crawler.
        /// </summary>
        internal string UserAgent { get; private set; }

        /// <summary>
        /// The string which used for connecting the database.
        /// </summary>
        internal string ConnectionString { get; private set; }

        /// <summary>
        /// Is this run the initial run?
        /// </summary>
        internal bool IsFirstTime { get; set; }

        /// <summary>
        /// The max webpages the crawler will wait for finishing the process of them.
        /// </summary>
        internal int MaxWaitForWebpages { get; private set; }

        /// <summary>
        /// The time in seconds that a webpage has to be loaded.
        /// </summary>
        internal int TimeoutInSeconds { get; private set; }

        /// <summary>
        /// The time in minutes that a keywords parsing process has to be finished.
        /// </summary>
        internal int TimeoutForKeywordsParsingInMinutes { get; private set; }

        /// <summary>
        /// The Id of this crawler.
        /// </summary>
        internal int Id { get; private set; }

        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="userAgent">The user agent of the crawler.</param>
        /// <param name="connectionString">The string which used for connecting the database.</param>
        /// <param name="maxWaitForWebpages">The max webpages the crawler will wait for finishing the process of them.</param>
        /// <param name="timeoutInSeconds">The time in seconds that a webpage has to be loaded.</param>
        /// <param name="id">The Id of this crawler.</param>
        private WebCrawlerConfig(string userAgent, string connectionString, int maxWaitForWebpages,
            int timeoutInSeconds, int timeoutForKeywordsParsingInMinutes, int id)
        {
            ConnectionString = connectionString;
            UserAgent = userAgent;
            IsFirstTime = false;
            MaxWaitForWebpages = maxWaitForWebpages;
            TimeoutInSeconds = timeoutInSeconds;
            TimeoutForKeywordsParsingInMinutes = timeoutForKeywordsParsingInMinutes;
            Id = id;
        }

        /// <summary>
        /// Returns a config by the entered parameters.
        /// </summary>
        /// <param name="userAgent">The user agent of the crawler.</param>
        /// <param name="connectionString">The string which used for connecting the database.</param>
        /// <param name="maxWaitForWebpages">The max webpages the crawler will wait for finishing the process of them.</param>
        /// <param name="timeoutInSeconds">The time in seconds that a webpage has to be loaded.</param>
        /// <param name="id">The Id of this crawler.</param>
        /// <returns>A new <see cref="DataHelperConfig"/>.</returns>
        public static WebCrawlerConfig Create(string userAgent, string connectionString, int maxWaitForWebpages,
            int timeoutInSeconds, int timeoutForKeywordsParsingInMinutes, int id)
        {
            var dhc = new WebCrawlerConfig(userAgent, connectionString, maxWaitForWebpages, timeoutInSeconds, timeoutForKeywordsParsingInMinutes, id);
            return dhc;
        }
    }
}
