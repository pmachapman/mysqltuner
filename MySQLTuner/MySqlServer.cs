// -----------------------------------------------------------------------
// <copyright file="MySqlServer.cs" company="Peter Chapman">
// Copyright 2019 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Authentication;
    using System.Text.RegularExpressions;
    using MySql.Data.MySqlClient;

    /// <summary>
    /// A MySQL database server.
    /// </summary>
    public class MySqlServer : IDisposable
    {
        /// <summary>
        /// Track whether Dispose has been called.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Initialises a new instance of the <see cref="MySqlServer"/> class.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        [CLSCompliant(false)]
        public MySqlServer(string userName, string password, string host, uint port)
            : this(userName, password, host)
        {
            this.Port = port;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MySqlServer"/> class.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="host">The host.</param>
        public MySqlServer(string userName, string password, string host)
            : this(userName, password)
        {
            this.Host = host;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MySqlServer"/> class.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <param name="password">The password.</param>
        public MySqlServer(string userName, string password)
            : this(userName)
        {
            this.Password = password;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MySqlServer"/> class.
        /// </summary>
        /// <param name="userName">The username.</param>
        public MySqlServer(string userName)
            : this()
        {
            this.UserName = userName;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MySqlServer"/> class.
        /// </summary>
        public MySqlServer()
        {
            // Add default values
            if (string.IsNullOrEmpty(this.Host))
            {
                this.Host = "localhost";
            }

            if (this.Port == 0)
            {
                this.Port = 3306;
            }

            // Setup variables
            this.EngineCount = new Dictionary<string, long>();
            this.EngineStatistics = new Dictionary<string, long>();
            this.Status = new Dictionary<string, string>();
            this.Variables = new Dictionary<string, string>();
        }

        /// <summary>
        /// Finalises an instance of the <see cref="MySqlServer"/> class.</summary>
        /// <remarks>
        /// Releases unmanaged resources and performs other clean up operations before the
        /// <see cref="MySqlServer"/> is reclaimed by garbage collection.
        /// </remarks>
        ~MySqlServer()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            this.Dispose(false);
        }

        /// <summary>
        /// Gets the engine table count.
        /// </summary>
        /// <value>
        /// The engine table count.
        /// </value>
        public Dictionary<string, long> EngineCount { get; private set; }

        /// <summary>
        /// Gets the engine statistics.
        /// </summary>
        /// <value>
        /// The engine statistics.
        /// </value>
        public Dictionary<string, long> EngineStatistics { get; private set; }

        /// <summary>
        /// Gets the number of fragmented tables.
        /// </summary>
        /// <value>
        /// The fragmented tables count.
        /// </value>
        public long FragmentedTables { get; private set; }

        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        /// <value>
        /// The host.
        /// </value>
        public string Host { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is local.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is local; otherwise, <c>false</c>.
        /// </value>
        public bool IsLocal
        {
            get
            {
                // TODO: A more accurate test than the one below!
                return this.Host.ToUpper(Settings.Culture) == "LOCALHOST" || this.Host == "127.0.0.1" || this.Host == "::1";
            }
        }

        /// <summary>
        /// Gets or sets the last error.
        /// </summary>
        /// <value>
        /// The last error.
        /// </value>
        public string LastError { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the amount of physical memory on the server.
        /// </summary>
        /// <value>
        /// The physical memory.
        /// </value>
        [CLSCompliant(false)]
        public ulong PhysicalMemory { get; set; }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>
        /// The port.
        /// </value>
        [CLSCompliant(false)]
        public uint Port { get; set; }

        /// <summary>
        /// Gets the replication status.
        /// </summary>
        /// <value>
        /// The replication status.
        /// </value>
        public Dictionary<string, string> ReplicationStatus { get; private set; }

        /// <summary>
        /// Gets or sets the number of slaves.
        /// </summary>
        /// <value>
        /// The number of slaves.
        /// </value>
        public long Slaves { get; set; }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>
        /// The status values.
        /// </value>
        public Dictionary<string, string> Status { get; private set; }

        /// <summary>
        /// Gets or sets the size of the swap file.
        /// </summary>
        /// <value>
        /// The swap file memory.
        /// </value>
        [CLSCompliant(false)]
        public ulong SwapMemory { get; set; }

        /// <summary>
        /// Gets the total number of MyISAM indexes.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "MyIsam", Justification = "It is the correct name, not hungarian notation.")]
        public long TotalMyIsamIndexes
        {
            get
            {
                if (this.IsLocal && this.Version.Major < 5)
                {
                    // TODO: Calculate the indexes from the file system
                    return 0;
                }
                else if (this.Version.Major >= 5)
                {
                    long totalMyIsamIndexes;
                    string sql = "SELECT IFNULL(SUM(INDEX_LENGTH),0) FROM information_schema.TABLES WHERE TABLE_SCHEMA NOT IN ('information_schema') AND ENGINE = 'MyISAM'";
                    using (MySqlCommand command = new MySqlCommand(sql, this.Connection))
                    {
                        object scalar = command.ExecuteScalar();
                        if (scalar != null)
                        {
                            if (!long.TryParse(scalar.ToString(), out totalMyIsamIndexes))
                            {
                                totalMyIsamIndexes = 0;
                            }
                        }
                        else
                        {
                            totalMyIsamIndexes = 0;
                        }
                    }

                    return totalMyIsamIndexes;
                }
                else
                {
                    // Unsupported version
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance's connection should use SSL.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance's connection should use SSL; otherwise, <c>false</c>.
        /// </value>
        public bool UseSsl { get; set; }

        /// <summary>
        /// Gets the variables.
        /// </summary>
        /// <value>
        /// The variables.
        /// </value>
        public Dictionary<string, string> Variables { get; private set; }

        /// <summary>
        /// Gets the server version.
        /// </summary>
        /// <value>
        /// The server version.
        /// </value>
        public Version Version
        {
            get
            {
                string version = this.Variables["version"];
                if (version.Contains("-"))
                {
                    version = version.Split('-')[0];
                }

                // Strip all non-numbers
                version = Regex.Replace(version, "[^.0-9]", string.Empty);

                return new Version(version);
            }
        }

        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        internal MySqlConnection Connection { get; set; }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            if (this.Connection != null)
            {
                this.Connection.Close();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Opens this instance.
        /// </summary>
        public void Open()
        {
            // Create the connection string
            MySqlConnectionStringBuilder connectionStringBuilder = new MySqlConnectionStringBuilder()
            {
                Server = this.Host,
                Port = this.Port,
                UserID = this.UserName,
                Password = this.Password,
                ConnectionTimeout = 30, // Stop any time out issues
                DefaultCommandTimeout = 0,
#if !MYSQL40
                SslMode = this.UseSsl ? MySqlSslMode.Preferred : MySqlSslMode.None,
#endif
            };

            // Create the connection
            try
            {
                string connectionString = connectionStringBuilder.ToString();
                if (!string.IsNullOrEmpty(connectionString))
                {
                    this.Connection = new MySqlConnection(connectionStringBuilder.ToString());
                    this.Connection.Open();
                }
            }
            catch (Exception ex)
            {
                if (ex is MySqlException
                    || ex is NotSupportedException
                    || ex is AuthenticationException)
                {
                    // Dispose of the connection
                    if (this.Connection != null)
                    {
                        this.Connection.Dispose();
                    }

                    // Set connection to null so it cannot be used
                    this.Connection = null;

                    // Set the last error
                    if (ex is AuthenticationException)
                    {
                        this.LastError = $"There was an error making a secure connection. Please try again using an unencrypted connection. Details:\r\n{ex.Message}";
                    }
                    else
                    {
                        this.LastError = ex.Message;
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Loads this instance.
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "This is a complex routine")]
        [SuppressMessage("Microsoft.Performance", "CA1809:AvoidExcessiveLocals", Justification = "This is a complex routine")]
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "The value in the query comes from the MySQL System tables.")]
        public void Load()
        {
            // We need to initiate at least one query so that our data is useable
            string sql = "SELECT VERSION()";
            using (MySqlCommand command = new MySqlCommand(sql, this.Connection))
            {
                command.ExecuteNonQuery();
            }

            // Load the variables
            sql = "SHOW /*!50000 GLOBAL */ VARIABLES";
            using (MySqlCommand command = new MySqlCommand(sql, this.Connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string key = GetStringFromReader(reader, 0);
                        if (this.Variables.ContainsKey(key))
                        {
                            this.Variables[key] = GetStringFromReader(reader, 1);
                        }
                        else
                        {
                            this.Variables.Add(key, GetStringFromReader(reader, 1));
                        }
                    }
                }
            }

            // Load the status
            sql = "SHOW /*!50000 GLOBAL */ STATUS";
            using (MySqlCommand command = new MySqlCommand(sql, this.Connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string key = GetStringFromReader(reader, 0);
                        if (this.Status.ContainsKey(key))
                        {
                            this.Status[key] = GetStringFromReader(reader, 1);
                        }
                        else
                        {
                            this.Status.Add(key, GetStringFromReader(reader, 1));
                        }
                    }
                }
            }

            // This only present in MySQL 5.0.3 or higher
            if (this.Version.Major >= 5 && !this.Variables.ContainsKey("innodb_support_xa"))
            {
                this.Variables["innodb_support_xa"] = "ON";
            }

            if (this.Variables.ContainsKey("wsrep_provider_options")
                && !string.IsNullOrEmpty(this.Variables["wsrep_provider_options"])
                && this.Variables.ContainsKey("wsrep_on")
                && this.Variables["wsrep_on"] != "OFF")
            {
                if (this.Variables.ContainsKey("have_galera"))
                {
                    this.Variables["have_galera"] = "NO";
                }
                else
                {
                    this.Variables.Add("have_galera", "NO");
                }
            }
            else
            {
                if (this.Variables.ContainsKey("have_galera"))
                {
                    this.Variables["have_galera"] = "YES";
                }
                else
                {
                    this.Variables.Add("have_galera", "YES");
                }
            }

            // Workaround for MySQL bug #59393 wrt. ignore-builtin-innodb
            if (this.Variables.ContainsKey("ignore_builtin_innodb") && this.Variables["ignore_builtin_innodb"] == "ON")
            {
                if (this.Variables.ContainsKey("have_innodb"))
                {
                    this.Variables["have_innodb"] = "NO";
                }
                else
                {
                    this.Variables.Add("have_innodb", "NO");
                }
            }

            // Support GTID MODE FOR MARIADB
            // Issue MariaDB GTID mode #272
            if (this.Variables.ContainsKey("gtid_strict_mode"))
            {
                if (this.Variables.ContainsKey("gtid_mode"))
                {
                    this.Variables["gtid_mode"] = this.Variables["gtid_strict_mode"];
                }
                else
                {
                    this.Variables.Add("gtid_mode", this.Variables["gtid_strict_mode"]);
                }
            }

            if (this.Variables.ContainsKey("thread_pool_size")
                && Convert.ToInt64(this.Variables["thread_pool_size"], Settings.Culture) > 0L)
            {
                if (this.Variables.ContainsKey("have_threadpool"))
                {
                    this.Variables["have_threadpool"] = "YES";
                }
                else
                {
                    this.Variables.Add("have_threadpool", "YES");
                }
            }
            else
            {
                if (this.Variables.ContainsKey("have_threadpool"))
                {
                    this.Variables["have_threadpool"] = "NO";
                }
                else
                {
                    this.Variables.Add("have_threadpool", "NO");
                }
            }

            // This only works in MySQL 4.1.2 or higher
            if (this.Version.Major > 4 || (this.Version.Major == 4 && this.Version.Minor >= 1))
            {
                // have_* for engines is deprecated and will be removed in MySQL 5.6;
                // check SHOW ENGINES and set corresponding old style variables.
                // Also works around MySQL bug #59393 wrt. skip-innodb
                sql = "SHOW ENGINES";
                using (MySqlCommand command = new MySqlCommand(sql, this.Connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string engine = GetStringFromReader(reader, 0).ToLower(Settings.Culture);
                            if (engine == "federated" || engine == "blackhole")
                            {
                                engine += "_engine";
                            }
                            else if (engine == "berkeleydb")
                            {
                                engine = "bdb";
                            }

                            string value = GetStringFromReader(reader, 1);
                            if (value == "DEFAULT")
                            {
                                value = "YES";
                            }

                            string key = "have_" + engine;
                            if (this.Variables.ContainsKey(key))
                            {
                                this.Variables[key] = value;
                            }
                            else
                            {
                                this.Variables.Add(key, value);
                            }
                        }
                    }
                }
            }

            // Get the replication status
            sql = "SHOW SLAVE STATUS";
            using (MySqlCommand command = new MySqlCommand(sql, this.Connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            this.ReplicationStatus.Add(reader.GetName(i), GetStringFromReader(reader, i));
                        }
                    }
                }
            }

            // Get the number of slaves
            sql = "SHOW SLAVE HOSTS";
            using (MySqlCommand command = new MySqlCommand(sql, this.Connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        this.Slaves++;
                    }
                }
            }

            // Get engine statistics
            if (this.Version.Major >= 5)
            {
                // MySQL 5 servers can have table sizes calculated quickly from information schema
                sql = "SELECT ENGINE, SUM(DATA_LENGTH), COUNT(ENGINE) FROM information_schema.TABLES WHERE TABLE_SCHEMA NOT IN ('information_schema', 'mysql') AND ENGINE IS NOT NULL GROUP BY ENGINE ORDER BY ENGINE ASC";
                using (MySqlCommand command = new MySqlCommand(sql, this.Connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string key = GetStringFromReader(reader, 0);
                            if (!long.TryParse(GetStringFromReader(reader, 1), out long size))
                            {
                                size = 0;
                            }

                            if (!long.TryParse(GetStringFromReader(reader, 2), out long count))
                            {
                                count = 0;
                            }

                            if (size > 0)
                            {
                                // Add the size
                                if (this.EngineStatistics.ContainsKey(key))
                                {
                                    this.EngineStatistics[key] = size;
                                }
                                else
                                {
                                    this.EngineStatistics.Add(key, size);
                                }

                                // Add the table count
                                if (this.EngineCount.ContainsKey(key))
                                {
                                    this.EngineCount[key] = count;
                                }
                                else
                                {
                                    this.EngineCount.Add(key, count);
                                }
                            }
                        }
                    }
                }

                // Get the number of fragmented tables
                sql = "SELECT COUNT(TABLE_NAME) FROM information_schema.TABLES WHERE TABLE_SCHEMA NOT IN ('information_schema', 'mysql') AND Data_free > 0 AND NOT ENGINE = 'MEMORY'";
                using (MySqlCommand command = new MySqlCommand(sql, this.Connection))
                {
                    object scalar = command.ExecuteScalar();
                    if (scalar != null)
                    {
                        if (!long.TryParse(scalar.ToString(), out long fragmentedTables))
                        {
                            fragmentedTables = 0;
                        }

                        this.FragmentedTables = fragmentedTables;
                    }
                }
            }
            else
            {
                // MySQL < 5 servers take a lot of work to get table sizes
                // Now we build a database list, and loop through it to get storage engine stats for tables
                List<string> databases = new List<string>();
                sql = "SHOW DATABASES";
                using (MySqlCommand command = new MySqlCommand(sql, this.Connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            databases.Add(GetStringFromReader(reader, 0));
                        }
                    }
                }

                // Reset the engine variables
                this.EngineCount = new Dictionary<string, long>();
                this.EngineStatistics = new Dictionary<string, long>();
                this.FragmentedTables = 0;

                // Go through every database
                foreach (string database in databases)
                {
                    if (database != "information_schema" && database != "performance_schema")
                    {
                        sql = "SHOW TABLE STATUS FROM `" + database + "`";
                        using (MySqlCommand command = new MySqlCommand(sql, this.Connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string key = GetStringFromReader(reader, 1);
                                    long size;
                                    long dataFree;
                                    if (this.Version.Major == 3 || (this.Version.Major == 4 && this.Version.Minor == 0))
                                    {
                                        // MySQL 3.23/4.0 keeps Data_Length in the 6th column
                                        if (!long.TryParse(GetStringFromReader(reader, 5), out size))
                                        {
                                            size = 0;
                                        }

                                        if (!long.TryParse(GetStringFromReader(reader, 8), out dataFree))
                                        {
                                            dataFree = 0;
                                        }
                                    }
                                    else
                                    {
                                        // MySQL 4.1+ keeps Data_Length in the 7th column
                                        if (!long.TryParse(GetStringFromReader(reader, 6), out size))
                                        {
                                            size = 0;
                                        }

                                        if (!long.TryParse(GetStringFromReader(reader, 9), out dataFree))
                                        {
                                            dataFree = 0;
                                        }
                                    }

                                    // Add the size
                                    if (this.EngineStatistics.ContainsKey(key))
                                    {
                                        this.EngineStatistics[key] += size;
                                    }
                                    else
                                    {
                                        this.EngineStatistics.Add(key, size);
                                    }

                                    // Add the table count
                                    if (this.EngineCount.ContainsKey(key))
                                    {
                                        this.EngineCount[key]++;
                                    }
                                    else
                                    {
                                        this.EngineCount.Add(key, 1);
                                    }

                                    // See if this table is fragmented
                                    if (dataFree > 0)
                                    {
                                        this.FragmentedTables++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <remarks>
        ///   <para>Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        ///   </para>
        ///   <para>
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the destructor and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        ///   </para>
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing && this.Connection != null)
                {
                    // Dispose managed resources.
                    this.Connection.Dispose();
                }

                // Note disposing has been done.
                this.disposed = true;
            }
        }

        /// <summary>
        /// Gets the string from reader for the specified column number.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="i">The column number.</param>
        /// <returns>
        /// The string.
        /// </returns>
        /// <remarks>
        /// Returns an empty string on DBNull.
        /// </remarks>
        private static string GetStringFromReader(MySqlDataReader reader, int i)
        {
            // If I didn't need .Net 2.0 compatibility, I would write this as an extension method
            return reader.IsDBNull(i) ? string.Empty : reader.GetString(i);
        }
    }
}
