
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using SearchEngine.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// The namespace for all the classes of the web crawler application.
/// </summary>
namespace SearchEngine.WebCrawler
{
    public class Program
    {
        /// <summary>
        /// The name of the config file, by default.
        /// </summary>
        private static string pathToProgramConfig = @"ProgramConfig.json";

        /// <summary>
        /// The config of the program.
        /// </summary>
        private static ProgramConfig config;

        private static List<WebCrawler> webCrawlers;

        [Obsolete]
        private static async Task Main()
        {
            #region Config

            pathToProgramConfig = AppDomain.CurrentDomain.BaseDirectory + pathToProgramConfig;

            if(!File.Exists(pathToProgramConfig))
            {
                var txt = JsonConvert.SerializeObject(new ProgramConfig(), Formatting.Indented);
                try
                {
                    File.WriteAllText(pathToProgramConfig, txt);
                }
                catch(Exception e)
                {
                    LogMaster($"Couldn't create or write to `{pathToProgramConfig}`. Error: {e.Message}", DebugLevel.Error);
                    return;
                }
                LogMaster($"Created config at `{pathToProgramConfig}`. Please fill it and restart the program.", DebugLevel.Warning);
                return;
            }
            else
            {
                try
                {
                    var txt = File.ReadAllText(pathToProgramConfig);
                    config = JsonConvert.DeserializeObject<ProgramConfig>(txt);
                }
                catch(Exception e)
                {
                    LogMaster($"Couldn't read or deserialize the config at `{pathToProgramConfig}`. Error: {e.Message}", DebugLevel.Error);
                    return;
                }
            }

            LogMaster("Config serialized successfully.", DebugLevel.Info);

            #endregion

            #region Check Connection

            var (isSucceeded, isFirstTime) = CheckConnection();

            if(!isSucceeded)
            {
                return;
            }
            if(isFirstTime)
            {
                config.NumberOfCrawlers = 1;
                config.Crawler_TimeoutForKeywordsParsingInMinutes = 1000000; // Don't timeout the process.
                // Setting case-sensitive to the keywords.
                using var dh = new DataHelper(DataHelperConfig.Create(config.Crawler_ConnectionString));
                dh.Database.ExecuteSqlCommand(
                $"ALTER TABLE [{nameof(DataHelper.Keywords)}] ALTER COLUMN [{nameof(Keyword.RootKeywordForm)}] " +
                "nvarchar(64) COLLATE SQL_Latin1_General_CP1_CS_AS;");
            }

            #endregion

            webCrawlers = new List<WebCrawler>();

            for(int i = 0; i < config.NumberOfCrawlers; i++)
            {
                // Create a web crawler and assign config
                var wc = new WebCrawler(WebCrawlerConfig.Create(
                    userAgent: config.Crawler_UserAgent,
                    connectionString: config.Crawler_ConnectionString,
                    maxWaitForWebpages: config.Crawler_MaxWaitForWebpages,
                    timeoutInSeconds: config.Crawler_TimeoutInSeconds,
                    timeoutForKeywordsParsingInMinutes: config.Crawler_TimeoutForKeywordsParsingInMinutes,
                    id: i
                    ));

                wc.Log += Log;
                await Task.Run(wc.StartAsync);

                webCrawlers.Add(wc);
            }

            // Don't close the application
            string cmd;
            do
            {
                cmd = Console.ReadLine().ToLower();
                if(cmd == "stop all")
                {
                    while(webCrawlers.Count > 0)
                    {
                        var wc = webCrawlers[0];
                        webCrawlers.Remove(wc);
                        wc.StopAsync().Wait();
                    }
                }
                else
                {
                    var match = Regex.Match(cmd, @"stop (\d+)");
                    if(match.Success)
                    {
                        var n = int.Parse(match.Groups[1].Value);
                        for(int i = 0; i < n && i <= webCrawlers.Count; i++)
                        {
                            webCrawlers[i].StopAsync().Wait();
                        }
                    }
                }
            } while(cmd != "exit");
        }

        /// <summary>
        /// Logs the events of the web crawler.
        /// </summary>
        /// <param name="msg">The message.</param>
        /// <param name="level">The debug level.</param>
        private static void Log(string msg, DebugLevel level)
        {
            switch(level)
            {
                case DebugLevel.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case DebugLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case DebugLevel.Error:
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
            }
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        /// <summary>
        /// Logs the events of the master program.
        /// </summary>
        /// <param name="msg">The message.</param>
        /// <param name="level">The debug level.</param>
        private static void LogMaster(string msg, DebugLevel level = DebugLevel.Info)
        {
            Log($"[---] [{DateTime.Now.ToLongTimeString()}] {("[" + level + "]").PadRight(9)} {msg}", level);
        }

        /// <summary>
        /// Ensure that the connections to the database and the Internet exist.
        /// </summary>
        /// <returns>Returns the status if it succeeded and if it is the first time.</returns>
        private static (bool isSucceeded, bool isFirstTime) CheckConnection()
        {
            using var dataHelper = new DataHelper(DataHelperConfig.Create(config.Crawler_ConnectionString));
            bool isFirstTime = false;

            LogMaster("Checking connections...");

            // Check if there is connection to the database
            LogMaster("Starting checking database connection...");

            try
            {
                var sw = Stopwatch.StartNew();

                // Check if the database was created.
                if(dataHelper.Database.EnsureCreated())
                {
                    sw.Stop();
                    LogMaster($"The database was created in {sw.Elapsed.TotalSeconds:F3}s.");
                    isFirstTime = true;
                }
                else
                    LogMaster("Found the database.");

                LogMaster("Database connection checking completed successfully.");
            }
            catch(Exception e)
            {
                LogMaster($"Failed to connect to the database. Error: {e.Message}", DebugLevel.Error);
                return (false, isFirstTime);
            }

            // Check if there is connection to the Internet
            LogMaster("Starting to check Internet connection...");
            var ping = new Ping();
            var errors = 0;
            try
            {
                LogMaster("Pinging 8.8.8.8...");
                var pr = ping.Send("8.8.8.8", 20000);
                if(pr.Status == IPStatus.Success)
                    LogMaster($"Connection to 8.8.8.8 completed successfully in {pr.RoundtripTime}ms.");
                else
                {
                    LogMaster($"Connection to 8.8.8.8 completed with an error ({pr.Status}).", DebugLevel.Warning);
                    errors++;
                }

                LogMaster("Pinging 8.8.4.4...");
                pr = ping.Send("8.8.4.4", 20000);
                if(pr.Status == IPStatus.Success)
                    LogMaster($"Connection to 8.8.4.4 completed successfully in {pr.RoundtripTime}ms.");
                else
                {
                    LogMaster($"Connection to 8.8.4.4 completed with an error ({pr.Status}).", DebugLevel.Warning);
                    errors++;
                }
            }
            catch(PingException e)
            {
                LogMaster($"Failed to connect to the Internet. Error: {e.Message}", DebugLevel.Error);
                return (false, isFirstTime);
            }
            finally
            {
                ping.Dispose();
            }

            if(errors > 1)
            {
                LogMaster($"Failed to connect to the Internet because too many connection errors.", DebugLevel.Error);
                return (false, isFirstTime);
            }

            LogMaster("Internet connection checking completed successfully.");

            LogMaster("Connections checking completed successfully.");
            return (true, isFirstTime);
        }
    }
}
