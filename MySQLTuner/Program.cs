// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Peter Chapman">
// Copyright 2012-2024 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// The main program.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            // Program set up
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Display the log on form
            FormLogOn formLogOn = new FormLogOn();
            Application.Run(formLogOn);

            // If successful, start the main program
            if (formLogOn.DialogResult == DialogResult.OK)
            {
                Application.Run(new FormMain(formLogOn.Server));
            }
        }
    }
}
