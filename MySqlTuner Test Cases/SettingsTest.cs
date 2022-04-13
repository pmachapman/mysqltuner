// -----------------------------------------------------------------------
// <copyright file="SettingsTest.cs" company="Peter Chapman">
// Copyright 2012-2022 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner.Test
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MySqlTuner;

    /// <summary>
    /// This is a test class for <c>SettingsTest</c> and is intended
    /// to contain all <c>SettingsTest</c> Unit Tests.
    /// </summary>
    [TestClass]
    public class SettingsTest
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
        /// A test for checking if this is a 64-Bit Operating System.
        /// </summary>
        [TestMethod]
        public void Is64BitOperatingSystemTest()
        {
            // Test that the Is64BitOperatingSystem property does not crash
            bool result = Settings.Is64BitOperatingSystem;
            Assert.IsTrue(result);
        }
    }
}