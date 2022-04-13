// -----------------------------------------------------------------------
// <copyright file="FormLogOn.Designer.cs" company="Peter Chapman">
// Copyright 2012-2022 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    /// <summary>
    /// The Log On Form Designer.
    /// </summary>
    public partial class FormLogOn
    {
        /// <summary>
        /// The cancel button.
        /// </summary>
        private System.Windows.Forms.Button cancel;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// The password textbox's label.
        /// </summary>
        private System.Windows.Forms.Label labelPassword;

        /// <summary>
        /// The port textbox's label.
        /// </summary>
        private System.Windows.Forms.Label labelPort;

        /// <summary>
        /// The server textbox's label.
        /// </summary>
        private System.Windows.Forms.Label labelServer;

        /// <summary>
        /// The username textbox's label.
        /// </summary>
        private System.Windows.Forms.Label labelUserName;

        /// <summary>
        /// The ok button.
        /// </summary>
        private System.Windows.Forms.Button ok;

        /// <summary>
        /// The password textbox.
        /// </summary>
        private System.Windows.Forms.TextBox password;

        /// <summary>
        /// The port masked textbox.
        /// </summary>
        private System.Windows.Forms.MaskedTextBox port;

        /// <summary>
        /// The server textbox.
        /// </summary>
        private System.Windows.Forms.TextBox host;

        /// <summary>
        /// The username textbox.
        /// </summary>
        private System.Windows.Forms.TextBox userName;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormLogOn));
            this.host = new System.Windows.Forms.TextBox();
            this.labelServer = new System.Windows.Forms.Label();
            this.labelUserName = new System.Windows.Forms.Label();
            this.userName = new System.Windows.Forms.TextBox();
            this.labelPassword = new System.Windows.Forms.Label();
            this.password = new System.Windows.Forms.TextBox();
            this.labelPort = new System.Windows.Forms.Label();
            this.cancel = new System.Windows.Forms.Button();
            this.ok = new System.Windows.Forms.Button();
            this.port = new System.Windows.Forms.MaskedTextBox();
            this.useSsl = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // host
            // 
            this.host.Location = new System.Drawing.Point(73, 12);
            this.host.Name = "host";
            this.host.Size = new System.Drawing.Size(110, 20);
            this.host.TabIndex = 5;
            this.host.Text = "localhost";
            // 
            // labelServer
            // 
            this.labelServer.AutoSize = true;
            this.labelServer.Location = new System.Drawing.Point(12, 15);
            this.labelServer.Name = "labelServer";
            this.labelServer.Size = new System.Drawing.Size(38, 13);
            this.labelServer.TabIndex = 4;
            this.labelServer.Text = "Server";
            // 
            // labelUserName
            // 
            this.labelUserName.AutoSize = true;
            this.labelUserName.Location = new System.Drawing.Point(12, 41);
            this.labelUserName.Name = "labelUserName";
            this.labelUserName.Size = new System.Drawing.Size(55, 13);
            this.labelUserName.TabIndex = 8;
            this.labelUserName.Text = "Username";
            // 
            // userName
            // 
            this.userName.Location = new System.Drawing.Point(73, 38);
            this.userName.Name = "userName";
            this.userName.Size = new System.Drawing.Size(192, 20);
            this.userName.TabIndex = 9;
            this.userName.Text = "root";
            // 
            // labelPassword
            // 
            this.labelPassword.AutoSize = true;
            this.labelPassword.Location = new System.Drawing.Point(12, 67);
            this.labelPassword.Name = "labelPassword";
            this.labelPassword.Size = new System.Drawing.Size(53, 13);
            this.labelPassword.TabIndex = 10;
            this.labelPassword.Text = "Password";
            // 
            // password
            // 
            this.password.Location = new System.Drawing.Point(73, 64);
            this.password.Name = "password";
            this.password.PasswordChar = '*';
            this.password.Size = new System.Drawing.Size(192, 20);
            this.password.TabIndex = 0;
            // 
            // labelPort
            // 
            this.labelPort.AutoSize = true;
            this.labelPort.Location = new System.Drawing.Point(189, 15);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(26, 13);
            this.labelPort.TabIndex = 6;
            this.labelPort.Text = "Port";
            // 
            // cancel
            // 
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Location = new System.Drawing.Point(190, 113);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(75, 23);
            this.cancel.TabIndex = 3;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            this.cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // ok
            // 
            this.ok.Location = new System.Drawing.Point(108, 113);
            this.ok.Name = "ok";
            this.ok.Size = new System.Drawing.Size(75, 23);
            this.ok.TabIndex = 2;
            this.ok.Text = "Ok";
            this.ok.UseVisualStyleBackColor = true;
            this.ok.Click += new System.EventHandler(this.Ok_Click);
            // 
            // port
            // 
            this.port.Location = new System.Drawing.Point(222, 13);
            this.port.Mask = "00000";
            this.port.Name = "port";
            this.port.PromptChar = ' ';
            this.port.Size = new System.Drawing.Size(43, 20);
            this.port.TabIndex = 7;
            this.port.Text = "3306";
            // 
            // useSsl
            // 
            this.useSsl.AutoSize = true;
            this.useSsl.Location = new System.Drawing.Point(73, 90);
            this.useSsl.Name = "useSsl";
            this.useSsl.Size = new System.Drawing.Size(125, 17);
            this.useSsl.TabIndex = 1;
            this.useSsl.Text = "Use SSL Connection";
            this.useSsl.UseVisualStyleBackColor = true;
            // 
            // FormLogOn
            // 
            this.AcceptButton = this.ok;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancel;
            this.ClientSize = new System.Drawing.Size(277, 148);
            this.Controls.Add(this.useSsl);
            this.Controls.Add(this.port);
            this.Controls.Add(this.ok);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.labelPort);
            this.Controls.Add(this.labelPassword);
            this.Controls.Add(this.password);
            this.Controls.Add(this.labelUserName);
            this.Controls.Add(this.userName);
            this.Controls.Add(this.labelServer);
            this.Controls.Add(this.host);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "FormLogOn";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MySQL Tuner";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox useSsl;
    }
}