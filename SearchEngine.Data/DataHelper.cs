using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// The namespace for all the data classes.
/// </summary>
namespace SearchEngine.Data
{
    /// <summary>
    /// The module which used for receiving and writing to the database.
    /// </summary>
    public class DataHelper : DbContext, IDisposable
    {
        #region Properties

        /// <summary>
        /// The queue of webpages of the web crawler.
        /// </summary>
        public DbSet<UrlRecord> Queue { get; private set; }

        /// <summary>
        /// The index of the search engine, where all the crawled data is stored.
        /// </summary>
        public DbSet<Webpage> Index { get; private set; }

        /// <summary>
        /// The domain names, which used by the URL Records in the queue.
        /// </summary>
        public DbSet<DomainName> DomainNames { get; private set; }

        /// <summary>
        /// Stores all the keywords
        /// </summary>
        public DbSet<Keyword> Keywords { get; private set; }

        #endregion

        #region Private Members

        /// <summary>
        /// The config of the <see cref="DataHelper"/>.
        /// </summary>
        private readonly DataHelperConfig config;

        #endregion

        #region Constructor

        /// <summary>
        /// The constructor of the <see cref="DataHelper"/>.
        /// </summary>
        public DataHelper(DbContextOptions options) : base(options) { }

        /// <summary>
        /// The constructor of the <see cref="DataHelper"/>.
        /// </summary>
        /// <param name="config">The config of the <see cref="DataHelper"/></param>
        public DataHelper(DataHelperConfig config)
        {
            this.config = config;
        }

        #endregion

        #region Override Methods

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // Connect to the database.
            if(config != null)
                optionsBuilder.UseSqlServer(config.ConnectionString);

            optionsBuilder.EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Uri data type conversion
            modelBuilder.Entity<UrlRecord>()
                .Property(x => x.Url)
                .HasConversion(
                    x => x.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped),
                    x => new Uri(x)
                );
            modelBuilder.Entity<Webpage>()
                .Property(x => x.Url)
                .HasConversion(
                    x => x.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped),
                    x => new Uri(x)
                );

            // Configure relationships
            modelBuilder.Entity<Webpage>()
                .HasOne(x => x.Metadata)
                .WithOne(x => x.Webpage)
                .HasForeignKey<Metadata>(x => x.Id);

            modelBuilder.Entity<KeywordWebpageRecord>()
                .HasOne(x => x.Webpage);

            modelBuilder.Entity<KeywordWebpageRecord>()
                .HasOne(x => x.Keyword)
                .WithMany(x => x.KeywordWebpageRecords);
        }

        #endregion
    }
}

