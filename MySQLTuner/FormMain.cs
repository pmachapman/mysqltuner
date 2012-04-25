// -----------------------------------------------------------------------
// <copyright file="FormMain.cs" company="Peter Chapman">
// Copyright 2012 Peter Chapman. See http://mysqltuner.codeplex.com/license for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Reflection;
    using System.Windows.Forms;
    using Microsoft.VisualBasic;
    using Microsoft.VisualBasic.Devices;

    /// <summary>
    /// The main form.
    /// </summary>
    public partial class FormMain : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormMain"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public FormMain(MySqlServer server)
            : this()
        {
            this.Server = server;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormMain"/> class.
        /// </summary>
        public FormMain()
        {
            this.InitializeComponent();
            this.Recommendations = new List<string>();
        }

        /// <summary>
        /// The delegate for adding a row to the results.
        /// </summary>
        /// <param name="status">The status of the message.</param>
        /// <param name="notice">The message's notice.</param>
        private delegate void PrintDelegate(Status status, string notice);

        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public MySqlServer Server { get; set; }

        /// <summary>
        /// Gets or sets the recommendations.
        /// </summary>
        /// <value>
        /// The recommendations.
        /// </value>
        private List<string> Recommendations { get; set; }

        /// <summary>
        /// Prints a message.
        /// </summary>
        /// <param name="status">The status of the message.</param>
        /// <param name="notice">The message's notice.</param>
        public void PrintMessage(Status status, string notice)
        {
            // See if this is is called from another thread
            if (this.results.InvokeRequired)
            {
                PrintDelegate sd = new PrintDelegate(this.PrintMessage);
                this.Invoke(sd, new object[] { status, notice });
            }
            else
            {
                // Setup the cells and add the row
                DataGridViewRow row = new DataGridViewRow();
                DataGridViewCell statusCell = new DataGridViewImageCell();
                switch (status)
                {
                    case Status.Pass:
                        statusCell.Value = (Image)Properties.Resources.Pass;
                        break;
                    case Status.Fail:
                        statusCell.Value = (Image)Properties.Resources.Fail;
                        break;
                    case Status.Info:
                    default:
                        statusCell.Value = (Image)Properties.Resources.Info;
                        break;
                    case Status.Recommendation:
                        statusCell.Value = (Image)Properties.Resources.Recommendation;
                        break;
                }

                row.Cells.Add(statusCell);
                DataGridViewCell noticeCell = new DataGridViewTextBoxCell();
                noticeCell.Value = notice;
                row.Cells.Add(noticeCell);
                this.results.Rows.Add(row);
            }
        }

        /// <summary>
        /// Calculates the parameter passed in bytes, and then rounds it to the nearest integer.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>The bytes formatted and rounded.</returns>
        private static string DisplayBytesRounded(int bytes)
        {
            if (bytes >= Math.Pow(1024, 3))
            {
                // GB
                return (int)(bytes / Math.Pow(1024, 3)) + "G";
            }
            else if (bytes >= Math.Pow(1024, 3))
            {
                // MB
                return (int)(bytes / Math.Pow(1024, 2)) + "M";
            }
            else if (bytes >= 1024)
            {
                // KB
                return (int)(bytes / 1024) + "K";
            }
            else
            {
                return bytes + "B";
            }
        }

        /// <summary>
        /// Handles the DoWork event of the Background Worker.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.DoWorkEventArgs"/> instance containing the event data.</param>
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Post the first message!
            this.PrintMessage(Status.Info, "MySQL Tuner " + Assembly.GetExecutingAssembly().GetName().Version.ToString(2) + " - Peter Chapman <peter@conglomo.co.nz>");

            // See if we are runing locally
            if (!this.Server.IsLocal)
            {
                this.PrintMessage(Status.Info, "Performing tests on " + this.Server.Host + ":" + this.Server.Port);
            }

            // See if an empty pasword was used
            if (string.IsNullOrEmpty(this.Server.Password))
            {
                this.PrintMessage(Status.Fail, "Successfully authenticated with no password - SECURITY RISK!");
            }

            // Get the memory
            ComputerInfo computerInfo = new ComputerInfo();
            if (this.Server.IsLocal)
            {
                this.Server.PhysicalMemory = computerInfo.TotalPhysicalMemory;
                this.Server.SwapMemory = computerInfo.TotalVirtualMemory - this.Server.PhysicalMemory;
            }
            else
            {
                // Ask for the physical memory value
                ulong physicalMemory;
                string memory = Interaction.InputBox("How much physical memory is on the server (in megabytes)?");
                if (string.IsNullOrEmpty(memory) || !ulong.TryParse(memory, out physicalMemory))
                {
                    this.PrintMessage(Status.Info, "Assuming the same amount of physical memory as this computer");
                    physicalMemory = computerInfo.TotalPhysicalMemory;
                }
                else
                {
                    this.PrintMessage(Status.Info, "Assuming " + physicalMemory + " MB of physical memory");
                    physicalMemory *= 1048576;
                }

                this.Server.PhysicalMemory = physicalMemory;

                // Ask for the swap memory value
                ulong swapMemory;
                memory = Interaction.InputBox("How much swap space is on the server (in megabytes)?");
                if (string.IsNullOrEmpty(memory) || !ulong.TryParse(memory, out swapMemory))
                {
                    this.PrintMessage(Status.Info, "Assuming the same amount of swap space as this computer");
                    swapMemory = computerInfo.TotalVirtualMemory - this.Server.PhysicalMemory;
                }
                else
                {
                    this.PrintMessage(Status.Info, "Assuming " + swapMemory + " MB of swap space");
                    swapMemory *= 1048576;
                }

                this.Server.SwapMemory = swapMemory;
            }

            // Load the server values from the database
            this.Server.Load();

            // Check for supported or EOL'ed MySQL versions
            if (this.Server.Version.Major < 5)
            {
                this.PrintMessage(Status.Fail, "Your MySQL version " + this.Server.Variables["version"] + " is EOL software!  Upgrade soon!");
            }
            else if (this.Server.Version.Major == 5)
            {
                this.PrintMessage(Status.Pass, "Currently running supported MySQL version " + this.Server.Variables["version"]);
            }
            else
            {
                this.PrintMessage(Status.Fail, "Currently running unsupported MySQL version " + this.Server.Variables["version"]);
            }

            // Check if 32-bit or 64-bit architecture, if local machine
            if (this.Server.IsLocal)
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    this.PrintMessage(Status.Pass, "Operating on 64-bit architecture");
                }
                else
                {
                    if (this.Server.PhysicalMemory > 2147483648)
                    {
                        this.PrintMessage(Status.Fail, "Switch to 64-bit OS - MySQL cannot currently use all of your RAM");
                    }
                    else
                    {
                        this.PrintMessage(Status.Pass, "Operating on 32-bit architecture with less than 2GB RAM");
                    }
                }
            }

            // Show enabled storage engines
            if (this.Server.Variables.ContainsKey("have_archive") && this.Server.Variables["have_archive"] == "YES")
            {
                this.PrintMessage(Status.Pass, "Archive Engine Installed");
            }
            else
            {
                this.PrintMessage(Status.Fail, "Archive Engine Not Installed");
            }

            if (this.Server.Variables.ContainsKey("have_bdb") && this.Server.Variables["have_bdb"] == "YES")
            {
                this.PrintMessage(Status.Pass, "Berkeley DB Engine Installed");
            }
            else
            {
                this.PrintMessage(Status.Fail, "Berkeley DB Engine Not Installed");
            }

            if (this.Server.Variables.ContainsKey("have_federated_engine") && this.Server.Variables["have_federated_engine"] == "YES")
            {
                this.PrintMessage(Status.Pass, "Federated Engine Installed");
            }
            else
            {
                this.PrintMessage(Status.Fail, "Federated Engine Not Installed");
            }

            if (this.Server.Variables.ContainsKey("have_innodb") && this.Server.Variables["have_innodb"] == "YES")
            {
                this.PrintMessage(Status.Pass, "InnoDB Engine Installed");
            }
            else
            {
                this.PrintMessage(Status.Fail, "InnoDB Engine Not Installed");
            }

            if (this.Server.Variables.ContainsKey("have_isam") && this.Server.Variables["have_isam"] == "YES")
            {
                this.PrintMessage(Status.Pass, "ISAM Engine Installed");
            }
            else
            {
                this.PrintMessage(Status.Fail, "ISAM Engine Not Installed");
            }

            if (this.Server.Variables.ContainsKey("have_ndbcluster") && this.Server.Variables["have_ndbcluster"] == "YES")
            {
                this.PrintMessage(Status.Pass, "NDBCLUSTER Engine Installed");
            }
            else
            {
                this.PrintMessage(Status.Fail, "NDBCLUSTER Engine Not Installed");
            }

            // Show data in storage engines
            foreach (KeyValuePair<string, int> engineStatistic in this.Server.EngineStatistics)
            {
                this.PrintMessage(Status.Info, "Data in " + engineStatistic.Key + " tables: " + DisplayBytesRounded(engineStatistic.Value) + " (Tables: " + this.Server.EngineCount[engineStatistic.Key] + ")");
            }

            // If the storage engine isn't being used, recommend it to be disabled
            if (!this.Server.EngineStatistics.ContainsKey("InnoDB") && this.Server.Variables.ContainsKey("have_innodb") && this.Server.Variables["have_innodb"] == "YES")
            {
                this.PrintMessage(Status.Fail, "InnoDB is enabled but isn't being used");
                this.Recommendations.Add("Add skip-innodb to MySQL configuration to disable InnoDB");
            }

            if (!this.Server.EngineStatistics.ContainsKey("BerkeleyDB") && this.Server.Variables.ContainsKey("have_bdb") && this.Server.Variables["have_bdb"] == "YES")
            {
                this.PrintMessage(Status.Fail, "BDB is enabled but isn't being used");
                this.Recommendations.Add("Add skip-bdb to MySQL configuration to disable BDB");
            }

            if (!this.Server.EngineStatistics.ContainsKey("ISAM") && this.Server.Variables.ContainsKey("have_isam") && this.Server.Variables["have_isam"] == "YES")
            {
                this.PrintMessage(Status.Fail, "ISAM is enabled but isn't being used");
                this.Recommendations.Add("Add skip-isam to MySQL configuration to disable ISAM (MySQL > 4.1.0");
            }

            // Fragmented tables
            if (this.Server.FragmentedTables > 0)
            {
                this.PrintMessage(Status.Fail, "Total fragmented tables: " + this.Server.FragmentedTables);
                this.Recommendations.Add("Run OPTIMIZE TABLE to defragment tables for better performance");
            }
            else
            {
                this.PrintMessage(Status.Pass, "Total fragmented tables: 0");
            }

            // TODO: Display some security recommendations
            // TODO: Calculate everything we need
            // TODO: Print the server stats
            // TODO: Make recommendations based on stats

            // Display the recommendations
            foreach (string recommendation in this.Recommendations)
            {
                this.PrintMessage(Status.Recommendation, recommendation);
            }
        }

        /// <summary>
        /// Handles the Click event of the Close button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Close_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Handles the FormClosing event of the Main form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.FormClosingEventArgs"/> instance containing the event data.</param>
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Clean up
            this.Server.Close();
            this.Server.Dispose();
        }

        /// <summary>
        /// Handles the Load event of the Main form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void FormMain_Load(object sender, System.EventArgs e)
        {
            // Start the background worker
            this.backgroundWorker.RunWorkerAsync();
        }
    }
}
