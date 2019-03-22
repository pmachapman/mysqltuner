// -----------------------------------------------------------------------
// <copyright file="FormMain.Designer.cs" company="Peter Chapman">
// Copyright 2019 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    /// <summary>
    /// The Main Form's Designer.
    /// </summary>
    public partial class FormMain
    {
        /// <summary>
        /// The background worker.
        /// </summary>
        private System.ComponentModel.BackgroundWorker backgroundWorker;

        /// <summary>
        /// The close button.
        /// </summary>
        private System.Windows.Forms.Button close;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// The results grid view.
        /// </summary>
        private System.Windows.Forms.DataGridView results;

        /// <summary>
        /// The status column of the results grid view.
        /// </summary>
        private System.Windows.Forms.DataGridViewImageColumn status;

        /// <summary>
        /// The notice column of the results grid view.
        /// </summary>
        private System.Windows.Forms.DataGridViewTextBoxColumn notice;

        /// <summary>
        /// The main progress bar.
        /// </summary>
        private System.Windows.Forms.ProgressBar progessBarMain;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing"><c>true</c> if managed resources should be disposed; otherwise, <c>false</c>.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.results = new System.Windows.Forms.DataGridView();
            this.status = new System.Windows.Forms.DataGridViewImageColumn();
            this.notice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.close = new System.Windows.Forms.Button();
            this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.progessBarMain = new System.Windows.Forms.ProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.results)).BeginInit();
            this.SuspendLayout();
            // 
            // results
            // 
            this.results.AllowUserToAddRows = false;
            this.results.AllowUserToDeleteRows = false;
            this.results.AllowUserToResizeRows = false;
            this.results.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.results.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.results.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.status,
            this.notice});
            this.results.Location = new System.Drawing.Point(12, 12);
            this.results.Name = "results";
            this.results.ReadOnly = true;
            this.results.RowHeadersVisible = false;
            this.results.Size = new System.Drawing.Size(532, 315);
            this.results.TabIndex = 0;
            // 
            // status
            // 
            this.status.HeaderText = "Status";
            this.status.MinimumWidth = 50;
            this.status.Name = "status";
            this.status.ReadOnly = true;
            this.status.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.status.Width = 50;
            // 
            // notice
            // 
            this.notice.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.notice.HeaderText = "Notice";
            this.notice.MinimumWidth = 100;
            this.notice.Name = "notice";
            this.notice.ReadOnly = true;
            // 
            // close
            // 
            this.close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.close.Location = new System.Drawing.Point(469, 337);
            this.close.Name = "close";
            this.close.Size = new System.Drawing.Size(75, 23);
            this.close.TabIndex = 1;
            this.close.Text = "Close";
            this.close.UseVisualStyleBackColor = true;
            this.close.Click += new System.EventHandler(this.Close_Click);
            // 
            // backgroundWorker
            // 
            this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorker_DoWork);
            // 
            // ProgessBarMain
            // 
            this.progessBarMain.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.progessBarMain.Location = new System.Drawing.Point(12, 340);
            this.progessBarMain.Name = "ProgessBarMain";
            this.progessBarMain.Size = new System.Drawing.Size(178, 16);
            this.progessBarMain.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progessBarMain.TabIndex = 2;
            // 
            // FormMain
            // 
            this.AcceptButton = this.close;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.close;
            this.ClientSize = new System.Drawing.Size(556, 368);
            this.Controls.Add(this.progessBarMain);
            this.Controls.Add(this.close);
            this.Controls.Add(this.results);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormMain";
            this.Text = "MySQL Tuner";
            this.Load += new System.EventHandler(this.FormMain_Load);
            ((System.ComponentModel.ISupportInitialize)(this.results)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
    }
}