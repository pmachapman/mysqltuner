// -----------------------------------------------------------------------
// <copyright file="MySqlServerTest.cs" company="Peter Chapman">
// Copyright 2019 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner.Test
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MySqlTuner;

    /// <summary>
    /// This is a test class for <c>MySqlServerTest</c> and is intended
    /// to contain all <c>MySqlServerTest</c> Unit Tests.
    /// </summary>
    [TestClass]
    public class MySqlServerTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        /// <value>
        /// The test context.
        /// </value>
        [CLSCompliant(false)]
        public TestContext TestContext { get; set; }

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