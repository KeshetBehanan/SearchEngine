using System;
using System.Collections.Generic;
using System.Text;

namespace SearchEngine.Data
{
    /// <summary>
    /// Config for the <see cref="DataHelper"/> class.
    /// </summary>
    public sealed class DataHelperConfig
    {
        #region Properties

        /// <summary>
        /// The string which used for connecting the database.
        /// </summary>
        internal string ConnectionString { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Private constructor.
        /// </summary>
        /// <param name="connectionString"></param>
        private DataHelperConfig(string connectionString)
        {
            ConnectionString = connectionString;
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// Returns a config by the entered parameters.
        /// </summary>
        /// <param name="connectionString">The string which the <see cref="DataHelper"/> needs to connect to the SQL Server.</param>
        /// <returns>A new <see cref="DataHelperConfig"/></returns>
        public static DataHelperConfig Create(string connectionString)
        {
            var dhc = new DataHelperConfig(connectionString);

            return dhc;
        }

        #endregion
    }
}
