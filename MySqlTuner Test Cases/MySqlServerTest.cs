// -----------------------------------------------------------------------
// <copyright file="MySqlServerTest.cs" company="Peter Chapman">
// Copyright 2018 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MySqlTuner;

    /// <summary>
    /// This is a test class for <c>MySqlServerTest</c> and is intended
    /// to contain all <c>MySqlServerTest</c> Unit Tests
    /// </summary>
    [TestClass]
    public class MySqlServerTest
    {
        /// <summary>
        /// The test context instance.
        /// </summary>
        private TestContext testContextInstance;

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        /// <value>
        /// The test context.
        /// </value>
        public TestContext TestContext
        {
            get
            {
                return this.testContextInstance;
            }

            set
            {
                this.testContextInstance = value;
            }
        }

        /// <summary>
        /// A test for Open with no properties set.
        /// </summary>
        [TestMethod]
        public void OpenNoPropertiesTest()
        {
            // Test the MySQL server connection
            using (MySqlServer target = new MySqlServer())
            {
                target.Open();
            }
        }
    }
}