// -----------------------------------------------------------------------
// <copyright file="Status.cs" company="Peter Chapman">
// Copyright 2012-2022 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    /// <summary>
    /// The status message type.
    /// </summary>
    public enum Status : int
    {
        /// <summary>
        /// The notice is a information notice.
        /// </summary>
        Info = 0,

        /// <summary>
        /// The notice is a pass notice.
        /// </summary>
        Pass = 1,

        /// <summary>
        /// The notice is a failure notice.
        /// </summary>
        Fail = 2,

        /// <summary>
        /// The notice is a recommendation notice.
        /// </summary>
        Recommendation = 3,
    }
}
