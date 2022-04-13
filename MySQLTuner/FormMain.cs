// -----------------------------------------------------------------------
// <copyright file="FormMain.cs" company="Peter Chapman">
// Copyright 2012-2022 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// The main form.
    /// </summary>
    public partial class FormMain : TuningCalculator
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="FormMain"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public FormMain(MySqlServer server)
            : this()
        {
            this.Server = server;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="FormMain"/> class.
        /// </summary>
        public FormMain()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// The delegate for adding a row to the results.
        /// </summary>
        /// <param name="status">The status of the message.</param>
        /// <param name="notice">The message's notice.</param>
        private delegate void PrintDelegate(Status status, string notice);

        /// <summary>
        /// The delegate for showing the progress bar as complete or incomplete.
        /// </summary>
        /// <param name="visible">If set to <c>true</c>, the progress bar is complete; otherwise <c>false</c>.</param>
        private delegate void ProgressBarCompleteDelegate(bool visible);

        /// <summary>
        /// Prints a message.
        /// </summary>
        /// <param name="messageStatus">The status of the message.</param>
        /// <param name="message">The message's notice.</param>
        public override void PrintMessage(Status messageStatus, string message)
        {
            // See if this is is called from another thread
            if (this.results.InvokeRequired)
            {
                PrintDelegate sd = new PrintDelegate(this.PrintMessage);
                this.Invoke(sd, new object[] { messageStatus, message });
            }
            else
            {
                // Setup the cells and add the row
                using (DataGridViewRow row = new DataGridViewRow())
                {
                    using (DataGridViewImageWithAltTextCell statusCell = new DataGridViewImageWithAltTextCell())
                    {
                        switch (messageStatus)
                        {
                            case Status.Pass:
                                statusCell.AltText = "Pass";
                                statusCell.Value = Properties.Resources.Pass;
                                break;
                            case Status.Fail:
                                statusCell.AltText = "Fail";
                                statusCell.Value = Properties.Resources.Fail;
                                break;
                            case Status.Info:
                            default:
                                statusCell.AltText = "Info";
                                statusCell.Value = Properties.Resources.Info;
                                break;
                            case Status.Recommendation:
                                statusCell.AltText = "Recommendation";
                                statusCell.Value = Properties.Resources.Recommendation;
                                break;
                        }

                        row.Cells.Add(statusCell);
                    }

                    using (DataGridViewCell noticeCell = new DataGridViewTextBoxCell())
                    {
                        noticeCell.Value = message;
                        row.Cells.Add(noticeCell);
                    }

                    this.results.Rows.Add(row);
                }
            }
        }

        /// <summary>
        /// Shows the progress bar as complete or incomplete.
        /// </summary>
        /// <param name="complete">if set to <c>true</c>, the progress bar is complete; otherwise <c>false</c>.</param>
        public override void ProgressComplete(bool complete)
        {
            // See if this is is called from another thread
            if (this.results.InvokeRequired)
            {
                ProgressBarCompleteDelegate sd = new ProgressBarCompleteDelegate(this.ProgressComplete);
                this.Invoke(sd, new object[] { complete });
            }
            else
            {
                this.progessBarMain.Style = ProgressBarStyle.Continuous;
                if (complete)
                {
                    this.progessBarMain.Value = 100;
                }
                else
                {
                    // An arbitrary number to show incompletion
                    this.progessBarMain.Value = 40;
                }
            }
        }

        /// <summary>
        /// Handles the DoWork event of the Background Worker.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.DoWorkEventArgs"/> instance containing the event data.</param>
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) => this.Calculate(this.Server);

        /// <summary>
        /// Handles the Click event of the Close button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Close_Click(object sender, System.EventArgs e) => this.Close();

        /// <summary>
        /// Handles the Load event of the Main form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// Starts the background worker.
        /// </remarks>
        private void FormMain_Load(object sender, System.EventArgs e) => this.backgroundWorker.RunWorkerAsync();
    }
}
