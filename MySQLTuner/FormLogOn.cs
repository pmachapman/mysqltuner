// -----------------------------------------------------------------------
// <copyright file="FormLogOn.cs" company="Peter Chapman">
// Copyright 2012 Peter Chapman. See http://mysqltuner.codeplex.com/license for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// The log on form.
    /// </summary>
    public partial class FormLogOn : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormLogOn"/> class.
        /// </summary>
        public FormLogOn()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        internal MySqlServer Server { get; set; }

        /// <summary>
        /// Opens the main form.
        /// </summary>
        /// <param name="server">The server.</param>
        public static void OpenMainForm(MySqlServer server)
        {
            Application.Run(new FormMain(server));
        }

        /// <summary>
        /// Handles the Click event of the cancel button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Handles the Click event of the ok button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Ok_Click(object sender, EventArgs e)
        {
            // Get the port
            int portNumber;
            if (!int.TryParse(this.port.Text, out portNumber) || portNumber < 0)
            {
                portNumber = 3306;
            }

            // Create a new server object
            this.Server = new MySqlServer();
            this.Server.Host = this.host.Text;
            this.Server.Port = portNumber;
            this.Server.UserName = this.userName.Text;
            this.Server.Password = this.password.Text;
            this.Server.Open();

            // Check for errors
            if (!string.IsNullOrEmpty(this.Server.LastError))
            {
                MessageBox.Show(this.Server.LastError, "Error connecting to server", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                this.Server.Dispose();
            }
            else
            {
                // Close this dialog
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}
