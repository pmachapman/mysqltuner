// -----------------------------------------------------------------------
// <copyright file="MySqlServer.cs" company="Peter Chapman">
// Copyright 2012 Peter Chapman. See http://mysqltuner.codeplex.com/license for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    using System;
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
        /// Initializes a new instance of the <see cref="MySqlServer"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="userName">The username.</param>
        /// <param name="password">The password.</param>
        public MySqlServer(string host, string userName, string password)
            : this(userName, password)
        {
            this.Host = host;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlServer"/> class.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <param name="password">The password.</param>
        public MySqlServer(string userName, string password)
            : this(userName)
        {
            this.Password = password;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlServer"/> class.
        /// </summary>
        /// <param name="userName">The username.</param>
        public MySqlServer(string userName)
        {
            this.UserName = userName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlServer"/> class.
        /// </summary>
        public MySqlServer()
        {
        }

        /// <summary>Finalizes an instance of the <see cref="MySqlServer"/> class.</summary>
        /// <remarks>
        /// Releases unmanaged resources and performs other cleanup operations before the
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
        /// Gets or sets the host.
        /// </summary>
        /// <value>
        /// The host.
        /// </value>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        [CLSCompliant(false)]
        protected MySqlConnection Connection { get; set; }

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
        /// Opens this instance.
        /// </summary>
        public void Open()
        {
            // Create the connection string
            MySqlConnectionStringBuilder connectionStringBuilder = new MySqlConnectionStringBuilder();
            connectionStringBuilder.Server = this.Host;
            connectionStringBuilder.UserID = this.UserName;
            connectionStringBuilder.Password = this.Password;

            // Create the connection
            string connectionString = connectionStringBuilder.ToString();
            if (!string.IsNullOrEmpty(connectionString))
            {
                this.Connection = new MySqlConnection(connectionStringBuilder.ToString());
                this.Connection.Open();
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
        /// Releases unmanaged and - optionally - managed resources
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
        /// runtime from inside the finalizer and you should not reference
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
    }
}
