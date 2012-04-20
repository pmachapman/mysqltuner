// -----------------------------------------------------------------------
// <copyright file="FormMain.Designer.cs" company="Peter Chapman">
// Copyright 2012 Peter Chapman. See http://mysqltuner.codeplex.com/license for licence details.
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
            this.results.Size = new System.Drawing.Size(348, 388);
            this.results.TabIndex = 0;
            // 
            // Status
            // 
            this.status.HeaderText = "Status";
            this.status.MinimumWidth = 50;
            this.status.Name = "status";
            this.status.ReadOnly = true;
            this.status.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.status.Width = 50;
            // 
            // Notice
            // 
            this.notice.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.notice.HeaderText = "Notice";
            this.notice.MinimumWidth = 100;
            this.notice.Name = "notice";
            this.notice.ReadOnly = true;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(372, 412);
            this.Controls.Add(this.results);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormMain";
            this.Text = "MySQL Tuner";
            ((System.ComponentModel.ISupportInitialize)(this.results)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
    }
}