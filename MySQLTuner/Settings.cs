// -----------------------------------------------------------------------
// <copyright file="Settings.cs" company="Peter Chapman">
// Copyright 2012 Peter Chapman. See http://mysqltuner.codeplex.com/license for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    using System.Globalization;

    /// <summary>
    /// Global settings.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Gets the program's culture.
        /// </summary>
        public static CultureInfo Culture
        {
            get
            {
                return CultureInfo.CreateSpecificCulture("en-NZ");
            }
        }
    }
}
