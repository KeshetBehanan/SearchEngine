using HtmlAgilityPack;
using SearchEngine.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SearchEngine.WebCrawler
{
    /// <summary>
    /// A single bot that web crawling.
    /// </summary>
    public class WebCrawler : IDisposable
    {
        #region Private Members

        /// <summary>
        /// The config of the <see cref="WebCrawler"/>.
        /// </summary>
        private readonly WebCrawlerConfig config;

        /// <summary>
        /// Is the <see cref="WebCrawler"/> crawling?
        /// </summary>
        private CancellationTokenSource isCrawling;

        /// <summary>
        /// The Stemmer of this crawler.
        /// </summary>
        private readonly PorterStemmer stemmer;

        /// <summary>
        /// The classes which needed to be crawled.
        /// </summary>
        /// <remarks>
        /// The key is the class keyword, the value is the weight of this class.
        /// </remarks>
        private static readonly Dictionary<string, int> docClasses = new Dictionary<string, int>()
        {
            { "text()[normalize-space(.) != '']", 1 },
            { "p", 3 }, { "span", 2 }, { "blockquote", 3 }, { "cite", 2 },
            { "strong", 4 }, { "mark", 4 }, { "u", 3 }, { "b", 3 },
            { "h1", 14 }, { "h2", 12 }, { "h3", 10 }, { "h4", 6 }, { "h5", 4 }, { "h6", 4 }
        };

        /// <summary>
        /// The metas which needed to be crawled.
        /// </summary>
        /// <remarks>
        /// The key is the meta keyword, the value is the weight of this meta.
        /// </remarks>
        private static readonly Dictionary<string, int> docMetas = new Dictionary<string, int>()
        {
            { "domain", 48 }, { "url", 20 },
            { "title", 24 }, { "description", 8 }
        };

        #endregion Private Members

        #region Private Constants

        /// <summary>
        /// Use this URL to crawl the first URLs.
        /// </summary>
        private const string URL_TO_CRAWL_IF_THERE_IS_NO_ROWS = "https://moz.com/top500/";

        #endregion Private Constants

        #region Constructor

        /// <summary>
        /// The constructor of the <see cref="WebCrawler"/>.
        /// </summary>
        /// <param name="config"></param>
        public WebCrawler(WebCrawlerConfig config)
        {
            this.config = config;
            stemmer = new PorterStemmer();
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// Starts to crawl webpages.
        /// </summary>
        public Task StartAsync()
        {
            LogMessage("Starting the crawler...");

            isCrawling = new CancellationTokenSource();

            CheckIfQueueIsEmpty();

            Task.Run(CrawlAsync, isCrawling.Token);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops to crawl webpages.
        /// </summary>
        public async Task StopAsync()
        {
            await LogMessage("Stopping the crawler...");
            if(isCrawling != null && !isCrawling.IsCancellationRequested)
                isCrawling.Cancel();
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Checks if there are webpages waiting to be crawled in the queue.
        /// If there are no webpages, it creates the default one.
        /// </summary>
        private Task CheckIfQueueIsEmpty()
        {
            using var dataHelper = new DataHelper(DataHelperConfig.Create(config.ConnectionString));

            if(isCrawling.IsCancellationRequested)
            {
                LogMessage("Canceled queue checking because the task is canceled.", DebugLevel.Error);
                return Task.CompletedTask;
            }

            if(!dataHelper.Queue.Any())
            {
                LogMessage("No webpages found to crawl. Adding the default one.");
                var dn = dataHelper.DomainNames.Add(new DomainName(new Uri(URL_TO_CRAWL_IF_THERE_IS_NO_ROWS).DnsSafeHost) { Priority = 1 });
                var ur = dataHelper.Queue.Add(new UrlRecord(URL_TO_CRAWL_IF_THERE_IS_NO_ROWS, dn.Entity));
                dn.Entity.AddUrlRecord(ur.Entity);

                dataHelper.SaveChanges();

                config.IsFirstTime = true;
                LogMessage("`IsFirstTime` has changed to `true`.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Crawling the web.
        /// </summary>
        private Task CrawlAsync()
        {
            #region Main Crawl Method

            var random = new Random();

            if(isCrawling.IsCancellationRequested)
            {
                LogMessage("Canceled crawling because the task is canceled.", DebugLevel.Error);
                return Task.CompletedTask;
            }

            using var http = new HttpClient(new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                MaxAutomaticRedirections = 10,
                MaxRequestContentBufferSize = 100000,
            })
            {
                Timeout = TimeSpan.FromSeconds(config.TimeoutInSeconds)
            };

            #region Headers

            http.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 (compatible; {config.UserAgent})");
            http.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            http.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            http.DefaultRequestHeaders.Add("Accept-Language", "en-US, en-UK");
            http.DefaultRequestHeaders.Add("Accept-Charset", "utf-16, utf-8");
            http.DefaultRequestHeaders.Add("Connection", "keep-alive");
            http.DefaultRequestHeaders.Add("Cache-Control", "no-cache");

            #endregion Headers

            var tasksOfParsingKeywords = new List<Task>(config.MaxWaitForWebpages);

            while(true)
            {
                using var dataHelper = new DataHelper(DataHelperConfig.Create(config.ConnectionString));

                if(isCrawling.IsCancellationRequested)
                {
                    if(tasksOfParsingKeywords.Count > 0)
                    {
                        Task.WaitAll(tasksOfParsingKeywords.ToArray());
                        dataHelper.SaveChanges();
                    }
                    LogMessage("Finished crawling.");
                    return Task.CompletedTask;
                }

                if(tasksOfParsingKeywords.Count >= config.MaxWaitForWebpages)
                {
                    Task.WaitAny(tasksOfParsingKeywords.ToArray());
                }

                var urlRecord = PopUrlRecord();

                HttpResponseMessage res;
                try
                {
                    res = http.GetAsync(urlRecord.Url, HttpCompletionOption.ResponseHeadersRead).Result;
                }
                catch(Exception e)
                {
                    LogMessage($"Getting `{urlRecord.Url.AbsoluteUri}` didn't success. Error: {e.Message}", DebugLevel.Warning);
                    continue;
                }

                if(res.IsSuccessStatusCode)
                {
                    var url = res.RequestMessage.RequestUri;

                    if(dataHelper.Index.Any(x => x.Url == url))
                    {
                        continue;
                    }

                    try
                    {
                        var raw = res.Content.ReadAsStringAsync().Result;
                        var doc = new HtmlDocument();
                        doc.LoadHtml(raw);

                        var metadata = ParseMetadata(doc.DocumentNode);
                        var webpage = new Webpage(url, metadata, urlRecord.Domain);

                        ParseUrls(doc.DocumentNode, url);

                        dataHelper.SaveChanges();

                        var pkTask = Task.Run(() => ParseKeywords(doc.DocumentNode, webpage).Wait());
                        pkTask.ContinueWith((x => tasksOfParsingKeywords.Remove(pkTask)));
                        tasksOfParsingKeywords.Add(pkTask);
                    }
                    catch(Exception e)
                    {
                        LogMessage($"Crawling `{url.AbsoluteUri}` didn't finish successfully. Error: {e.Message}", DebugLevel.Warning);
                    }
                }
                else
                {
                    LogMessage($"Getting `{res.RequestMessage.RequestUri}` didn't success. Status Code: {res.StatusCode}", DebugLevel.Warning);
                }

                if(config.IsFirstTime)
                {
                    isCrawling.Cancel();
                    LogMessage("Please restart the program AFTER the crawler will finished.");
                }
            }

            #endregion

            #region Local Methods

            // Gets a URL from the queue and removes it.
            UrlRecord PopUrlRecord()
            {
                using var dataHelper = new DataHelper(DataHelperConfig.Create(config.ConnectionString));
                UrlRecord url = null;

                do
                {
                    // Picking a domain name, with more chance for prioritized domain names (50% - 50%).
                    var dns = random.Next(2) == 0 ?
                                 dataHelper.DomainNames.Where(x => x.UrlRecords.Any() && x.Priority == 0).OrderBy(x => Guid.NewGuid()).FirstOrDefault() :
                                 dataHelper.DomainNames.Where(x => x.UrlRecords.Any() && x.Priority != 0).OrderBy(x => Guid.NewGuid()).FirstOrDefault();

                    if(dns != null)
                    {
                        // Pick random URL from the domain name URLs list.
                        url = dataHelper.Queue.Where(x => x.Domain.Id == dns.Id).FirstOrDefault();
                    }
                } while(url == null);

                // Remove the record from the queue.
                dataHelper.Queue.Remove(url);
                try
                {
                    dataHelper.SaveChanges();
                }
                catch { }

                return url;
            }

            // Returns the metadata of a webpage, after parsing its content.
            static Metadata ParseMetadata(HtmlNode doc)
            {
                var title = doc.Descendants("title")?.FirstOrDefault()?.InnerText;
                if(string.IsNullOrWhiteSpace(title))
                    title = doc.Descendants("meta")?.Where(x => x.GetAttributeValue("name", "") == "title")?.FirstOrDefault()?.GetAttributeValue("content", "");
                if(string.IsNullOrWhiteSpace(title))
                    title = doc.Descendants("meta")?.Where(x => x.GetAttributeValue("property", "") == "og:site_name")?.FirstOrDefault()?.GetAttributeValue("content", "");

                var description = doc.Descendants("meta")?.Where(x => x.GetAttributeValue("name", "") == "description")?.FirstOrDefault()?.GetAttributeValue("content", "");
                if(string.IsNullOrWhiteSpace(description))
                    description = doc.Descendants("meta")?.Where(x => x.GetAttributeValue("property", "") == "og:description").FirstOrDefault()?.GetAttributeValue("content", "");
                if(string.IsNullOrWhiteSpace(description))
                    description = doc.Descendants("p")?.Where(x => x.InnerText.Length > 16)?.FirstOrDefault()?.InnerText;

                return new Metadata(title, description);
            }

            // Parses the URLs of this webpage.
            Task ParseUrls(HtmlNode doc, Uri url)
            {
                using var dataHelper = new DataHelper(DataHelperConfig.Create(config.ConnectionString));

                try
                {
                    var urls = doc.Descendants("a")
                        .Select(x => x.GetAttributeValue("href", "").ToLower()) // Select all links of `a` tag
                        .Where(x => !string.IsNullOrWhiteSpace(x)) // remove empty links
                        .Select(x => new Uri(url, x)) // Convert it to a Uri type
                        .Select(x => new Uri(x.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped))) // format the links
                        .Distinct() // Make it unique
                        .Where(x => !(x.LocalPath.EndsWith(".js") || x.LocalPath.EndsWith(".css"))) // Don't include JS and CSS files
                        .Where(x => !(dataHelper.Queue.Any(y => y.Url == x) || dataHelper.Index.Any(y => y.Url == x))) // Check if already exists
                        .Select(x =>
                        {
                            DomainName dn;
                            // making sure there is no duplicates
                            if(!dataHelper.DomainNames.Any(y => y.Domain == x.DnsSafeHost))
                            {
                                dn = dataHelper.DomainNames.Add(new DomainName(x.DnsSafeHost)).Entity;
                                dataHelper.SaveChanges();
                            }
                            else
                            {
                                dn = dataHelper.DomainNames.First(y => y.Domain == x.DnsSafeHost);
                            }
                            var ur = new UrlRecord(x, dn);
                            dn.AddUrlRecord(ur);
                            return ur;
                        }); // Convert it to a UrlRecord type

                    dataHelper.Queue.AddRange(urls);
                    dataHelper.SaveChanges();
            }
                catch(Exception e)
            {
                LogMessage($"Parsing URLs in `{url.AbsoluteUri}` didn't success. Error: {e.Message}", DebugLevel.Warning);
            }
            return Task.CompletedTask;
            }

            Task ParseKeywords(HtmlNode doc, Webpage webpage)
            {
                using var dataHelper = new DataHelper(DataHelperConfig.Create(config.ConnectionString));

                #region Meta

                var titleKeywords = Regex.Matches(webpage?.Metadata?.Title, @"([\p{L}']+|\d+)")
                    .Select(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x).Select(x => new { Keyword = x.Key, Count = x.Count() })
                    .ToDictionary(x => x.Keyword, x => x.Count);
                var descKeywords = Regex.Matches(webpage?.Metadata?.Description, @"([\p{L}']+|\d+)")
                    .Select(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x).Select(x => new { Keyword = x.Key, Count = x.Count() })
                    .ToDictionary(x => x.Keyword, x => x.Count);

                var domainKeywords = Regex.Matches(Regex.Replace(webpage.Url.Authority, @"\..+$", ""), @"(\p{L}+|\d+)")
                    .Select(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x).Select(x => new { Keyword = x.Key, Count = x.Count() })
                    .ToDictionary(x => x.Keyword, x => x.Count);
                var urlKeywords = Regex.Matches(Regex.Replace(webpage.Url.LocalPath, @"\..+$", ""), @"(\p{L}+|\d+)")
                    .Select(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x).Select(x => new { Keyword = x.Key, Count = x.Count() })
                    .ToDictionary(x => x.Keyword, x => x.Count);

                var linksTasks = new Task[]
                {
                    LinkWordsToWebpage(titleKeywords, webpage, docMetas["title"], dataHelper),
                    LinkWordsToWebpage(descKeywords, webpage, docMetas["description"], dataHelper),
                    LinkWordsToWebpage(domainKeywords, webpage, docMetas["domain"], dataHelper),
                    LinkWordsToWebpage(urlKeywords, webpage, docMetas["url"], dataHelper)
                };

                Task.WaitAll(linksTasks);

                #endregion

                #region Body Classes

                var body = doc.SelectSingleNode("//body");

                // Remove style and script nodes.
                body.SelectNodes("//script")?.ToList().ForEach(x => x.Remove());
                body.SelectNodes("//style")?.ToList().ForEach(x => x.Remove());

                Parallel.ForEach(docClasses.Keys, keyword =>
                {
                    var keywords = body.SelectNodes($"//{keyword}")?
                        .Select(x => x.InnerText)
                        //.Where(x => !string.IsNullOrWhiteSpace(x))
                        .SelectMany(x => Regex.Matches(x, @"([\p{L}']+|\d+)")).Select(x => x.Value)
                        .Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x)
                        .Select(x => new { Keyword = x.Key, Count = x.Count() }).ToDictionary(x => x.Keyword, x => x.Count);
                    if(keywords != null)
                        LinkWordsToWebpage(keywords, webpage, docClasses[keyword], dataHelper).Wait();
                });

                Task.WaitAll(linksTasks);

                #endregion

                return Task.CompletedTask;
            }

            #endregion Local Methods
        }

        #endregion Private Methods

        #region Log Event

        /// <summary>
        /// The delegate of <see cref="Log"/>.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <param name="debugLevel">The debug level.</param>
        public delegate void LogMessageDelegate(string message, DebugLevel debugLevel);

        /// <summary>
        /// Logs a message.
        /// </summary>
        public event LogMessageDelegate Log;

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">The logged message.</param>
        /// <param name="debugLevel">The debug level.</param>
        private Task LogMessage(string message, DebugLevel debugLevel = DebugLevel.Info)
        {
            Log($"[{config.Id:000}] [{DateTime.Now.ToLongTimeString()}] {("[" + debugLevel + "]").PadRight(9)} {message}", debugLevel);
            return Task.CompletedTask;
        }

        #endregion Log Event

        #region Interface Methods

        public void Dispose()
        {
            StopAsync().Wait();
        }

        #endregion Interface Methods

        #region Helpers

        /// <summary>
        /// Links the keywords to the webpage.
        /// </summary>
        /// <param name="keywords">The keywords and their count of appears in the webpage.</param>
        /// <param name="webpage">The webpage.</param>
        /// <param name="score">The score of one appear of these keywords.</param>
        /// <returns></returns>
        private Task LinkWordsToWebpage(Dictionary<string, int> keywords, Webpage webpage, int score, DataHelper dataHelper)
        {
            while(keywords.Count > 0)
            {
                var w = keywords.First();
                var w1 = w.Key;
                var w2 = w1.ToLower();

                //if(w2 == "of" || w2 == "in" || w2 == "on" || w2 == "at" || w2 == "from" || w2 == "a" || w2 == "an" || w2 == "the")
                //{
                //    words.RemoveAt(0);
                //    continue;
                //}

                if(w2.EndsWith("n't"))
                {
                    w1 = Regex.Replace(w1, @"n't", "", RegexOptions.IgnoreCase);
                    w2 = w2.Replace("n't", "");
                    if(keywords.ContainsKey("not"))
                        keywords["not"]++;
                    else
                        keywords.Add("not", 1);
                }
                else if(w2.EndsWith("'ll"))
                {
                    w1 = Regex.Replace(w1, @"'ll", "", RegexOptions.IgnoreCase);
                    w2 = w2.Replace("'ll", "");
                    if(keywords.ContainsKey("be"))
                        keywords["be"]++;
                    else
                        keywords.Add("be", 1);
                }
                else if(w2.EndsWith("'re"))
                {
                    w1 = Regex.Replace(w1, @"'re", "", RegexOptions.IgnoreCase);
                    w2 = w2.Replace("'re", "");
                    if(keywords.ContainsKey("be"))
                        keywords["be"]++;
                    else
                        keywords.Add("be", 1);
                }
                else if(w2 == "he's" || w2 == "she's" || w2 == "it's" ||
                    w2 == "what's" || w2 == "when's" || w2 == "where's" || w2 == "how's" || w2 == "what's" || w2 == "why's")
                {
                    w1 = Regex.Replace(w1, @"'s", "", RegexOptions.IgnoreCase);
                    w2 = w2.Replace("'s", "");
                    if(keywords.ContainsKey("be"))
                        keywords["be"]++;
                    else
                        keywords.Add("be", 1);
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

                if(!string.IsNullOrWhiteSpace(w3))
                {
                    try
                    {
                        Keyword key;
                        lock(dataHelper)
                            key = dataHelper.Keywords.FirstOrDefault(x => x.RootKeywordForm == w3);

                        if(key == null)
                        {
                            key = dataHelper.Keywords.Add(new Keyword(w3)).Entity;
                            lock(dataHelper)
                                dataHelper.SaveChanges();
                        }

                        var kwr = key.KeywordWebpageRecords.FirstOrDefault(x => x.Webpage == webpage);
                        if(kwr == null)
                        {
                            key.KeywordWebpageRecords.Add(new KeywordWebpageRecord(webpage, key, score * w.Value));
                        }
                        else
                        {
                            kwr.Score += score;
                        }

                        lock(dataHelper)
                            dataHelper.SaveChanges();
                    }
                    catch { }
                }

                keywords.Remove(w.Key);
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}