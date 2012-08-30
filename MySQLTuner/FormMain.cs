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
    using MySql.Data.MySqlClient;

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
            this.Calculations = new Dictionary<string, int>();
            this.Recommendations = new List<string>();
            this.VariablesToAdjust = new List<string>();
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
        /// Gets or sets the calculations.
        /// </summary>
        /// <value>
        /// The calculations.
        /// </value>
        private Dictionary<string, int> Calculations { get; set; }

        /// <summary>
        /// Gets or sets the recommendations.
        /// </summary>
        /// <value>
        /// The recommendations.
        /// </value>
        private List<string> Recommendations { get; set; }

        /// <summary>
        /// Gets or sets the variables to adjust.
        /// </summary>
        /// <value>
        /// The variables to adjust.
        /// </value>
        private List<string> VariablesToAdjust { get; set; }

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
                using (DataGridViewRow row = new DataGridViewRow())
                {
                    using (DataGridViewCell statusCell = new DataGridViewImageCell())
                    {
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
                    }

                    using (DataGridViewCell noticeCell = new DataGridViewTextBoxCell())
                    {
                        noticeCell.Value = notice;
                        row.Cells.Add(noticeCell);
                    }

                    this.results.Rows.Add(row);
                }
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

            // Check current MySQL version
            this.ValidateMySqlVersion();

            // Suggest 64-bit upgrade
            this.CheckArchitecture();

            // Show enabled storage engines
            this.CheckStorageEngines();

            // Display some security recommendations
            this.SecurityRecommendations();

            // Calculate everything we need
            this.PerformCalculations();

            // TODO: Print the server stats

            // Make recommendations based on stats
            foreach (string recommendation in this.Recommendations)
            {
                this.PrintMessage(Status.Recommendation, recommendation);
            }

            if (this.VariablesToAdjust.Count > 0)
            {
                if (this.Calculations.ContainsKey("pct_physical_memory") && this.Calculations["pct_physical_memory"] > 90)
                {
                    this.PrintMessage(Status.Info, "MySQL's maximum memory usage is dangerously high");
                    this.PrintMessage(Status.Info, "Add RAM before increasing MySQL buffer variables");
                }

                foreach (string variableToAdjust in this.VariablesToAdjust)
                {
                    this.PrintMessage(Status.Recommendation, variableToAdjust);
                }
            }

            if (this.Recommendations.Count == 0 && this.VariablesToAdjust.Count == 0)
            {
                this.PrintMessage(Status.Info, "No additional performance recommendations are available.");
            }
        }

        /// <summary>
        /// Check for supported or EOL'ed MySQL versions.
        /// </summary>
        private void ValidateMySqlVersion()
        {
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
        }

        /// <summary>
        /// Check if 32-bit or 64-bit architecture, if local machine.
        /// </summary>
        private void CheckArchitecture()
        {
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
        }

        /// <summary>
        /// Show enabled storage engines.
        /// </summary>
        private void CheckStorageEngines()
        {
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
        }

        /// <summary>
        /// Display some security recommendations.
        /// </summary>
        private void SecurityRecommendations()
        {
            string sql = "SELECT CONCAT(user, '@', host) AS `username` FROM mysql.user WHERE password = '' OR password IS NULL ORDER BY `username`";
            using (MySqlCommand command = new MySqlCommand(sql, this.Server.Connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            this.PrintMessage(Status.Fail, "User '" + reader[0].ToString() + "' has no password set.");
                        }
                    }
                    else
                    {
                        this.PrintMessage(Status.Pass, "All database users have passwords assigned");
                    }
                }
            }
        }

        /// <summary>
        /// Performs the calculations.
        /// </summary>
        private void PerformCalculations()
        {
            // See if ther server is responding to our queries
            if (!this.Server.Status.ContainsKey("Questions") || this.Server.Status["Questions"] == "0")
            {
                this.PrintMessage(Status.Fail, "Your server has not answered any queries - cannot continue...");
                return;
            }

            // Per-thread memory
            if (this.Server.Version.Major > 3)
            {
                this.Calculations.Add("per_thread_buffers", Convert.ToInt32(this.Server.Variables["read_buffer_size"], Settings.Culture) + Convert.ToInt32(this.Server.Variables["read_rnd_buffer_size"], Settings.Culture) + Convert.ToInt32(this.Server.Variables["sort_buffer_size"], Settings.Culture) + Convert.ToInt32(this.Server.Variables["thread_stack"], Settings.Culture) + Convert.ToInt32(this.Server.Variables["join_buffer_size"], Settings.Culture));
            }
            else
            {
                this.Calculations.Add("per_thread_buffers", Convert.ToInt32(this.Server.Variables["record_buffer"], Settings.Culture) + Convert.ToInt32(this.Server.Variables["record_rnd_buffer"], Settings.Culture) + Convert.ToInt32(this.Server.Variables["sort_buffer"], Settings.Culture) + Convert.ToInt32(this.Server.Variables["thread_stack"], Settings.Culture) + Convert.ToInt32(this.Server.Variables["join_buffer_size"], Settings.Culture));
            }

            this.Calculations.Add("total_per_thread_buffers", Convert.ToInt32(this.Calculations["per_thread_buffers"], Settings.Culture) * Convert.ToInt32(this.Server.Variables["max_connections"], Settings.Culture));
            this.Calculations.Add("max_total_per_thread_buffers", Convert.ToInt32(this.Calculations["per_thread_buffers"], Settings.Culture) * Convert.ToInt32(this.Server.Status["Max_used_connections"], Settings.Culture));

            // Server-wide memory
            this.Calculations.Add("max_tmp_table_size", (Convert.ToInt32(this.Server.Variables["tmp_table_size"], Settings.Culture) > Convert.ToInt32(this.Server.Variables["max_heap_table_size"], Settings.Culture)) ? Convert.ToInt32(this.Server.Variables["max_heap_table_size"], Settings.Culture) : Convert.ToInt32(this.Server.Variables["tmp_table_size"], Settings.Culture));
            this.Calculations.Add("server_buffers", Convert.ToInt32(this.Server.Variables["key_buffer_size"], Settings.Culture) + Convert.ToInt32(this.Calculations["max_tmp_table_size"], Settings.Culture));
            this.Calculations["server_buffers"] += this.Server.Variables.ContainsKey("innodb_buffer_pool_size") ? Convert.ToInt32(this.Server.Variables["innodb_buffer_pool_size"], Settings.Culture) : 0;
            this.Calculations["server_buffers"] += this.Server.Variables.ContainsKey("innodb_additional_mem_pool_size") ? Convert.ToInt32(this.Server.Variables["innodb_additional_mem_pool_size"], Settings.Culture) : 0;
            this.Calculations["server_buffers"] += this.Server.Variables.ContainsKey("innodb_log_buffer_size") ? Convert.ToInt32(this.Server.Variables["innodb_log_buffer_size"], Settings.Culture) : 0;
            this.Calculations["server_buffers"] += this.Server.Variables.ContainsKey("query_cache_size") ? Convert.ToInt32(this.Server.Variables["query_cache_size"], Settings.Culture) : 0;

            // Global memory
            this.Calculations.Add("max_used_memory", this.Calculations["server_buffers"] + this.Calculations["max_total_per_thread_buffers"]);
            this.Calculations.Add("total_possible_used_memory", this.Calculations["server_buffers"] + this.Calculations["total_per_thread_buffers"]);
            this.Calculations.Add("pct_physical_memory", Convert.ToInt32((Convert.ToUInt64(this.Calculations["total_possible_used_memory"], Settings.Culture) * 100) / this.Server.PhysicalMemory, Settings.Culture));

            // Slow queries
            this.Calculations.Add("pct_slow_queries", Convert.ToInt32((Convert.ToInt32(this.Server.Status["Slow_queries"], Settings.Culture) / Convert.ToInt32(this.Server.Status["Questions"], Settings.Culture)) * 100, Settings.Culture));

            // Connections
            this.Calculations.Add("pct_connections_used", (Convert.ToInt32(this.Server.Status["Max_used_connections"], Settings.Culture) / Convert.ToInt32(this.Server.Variables["max_connections"], Settings.Culture)) * 100);
            this.Calculations["pct_connections_used"] = (this.Calculations["pct_connections_used"] > 100) ? 100 : this.Calculations["pct_connections_used"];

            // Key buffers
            if (this.Server.Version.Major > 3 && !(this.Server.Version.Major == 4 && this.Server.Version.Minor == 0))
            {
                this.Calculations.Add("pct_key_buffer_used", (1 - ((Convert.ToInt32(this.Server.Status["Key_blocks_unused"], Settings.Culture) * Convert.ToInt32(this.Server.Variables["key_cache_block_size"], Settings.Culture)) / Convert.ToInt32(this.Server.Variables["key_buffer_size"], Settings.Culture))) * 100);
            }

            if (Convert.ToInt32(this.Server.Status["Key_read_requests"], Settings.Culture) > 0)
            {
                this.Calculations.Add("pct_keys_from_mem", 100 - ((Convert.ToInt32(this.Server.Status["Key_reads"], Settings.Culture) / Convert.ToInt32(this.Server.Status["Key_read_requests"], Settings.Culture)) * 100));
            }
            else
            {
                this.Calculations.Add("pct_keys_from_mem", 0);
            }

            // Calculate the number of MyISAM indexes.
            this.Calculations.Add("total_myisam_indexes", this.Server.TotalMyIsamIndexes);

            // Query cache
            if (this.Server.Version.Major > 3)
            {
                this.Calculations.Add("query_cache_efficiency", (Convert.ToInt32(this.Server.Status["Qcache_hits"], Settings.Culture) / (Convert.ToInt32(this.Server.Status["Com_select"], Settings.Culture) + Convert.ToInt32(this.Server.Status["Qcache_hits"], Settings.Culture))) * 100);
                if (this.Server.Variables["query_cache_size"] != "0")
                {
                    this.Calculations.Add("pct_query_cache_used", 100 - ((Convert.ToInt32(this.Server.Status["Qcache_free_memory"], Settings.Culture) / Convert.ToInt32(this.Server.Variables["query_cache_size"], Settings.Culture)) * 100));
                }

                if (this.Server.Status["Qcache_lowmem_prunes"] == "0")
                {
                    this.Calculations.Add("query_cache_prunes_per_day", 0);
                }
                else
                {
                    this.Calculations.Add("query_cache_prunes_per_day", Convert.ToInt32(this.Server.Status["Qcache_lowmem_prunes"], Settings.Culture) / (Convert.ToInt32(this.Server.Status["Uptime"], Settings.Culture) / 86400));
                }
            }

            // Sorting
            this.Calculations.Add("total_sorts", Convert.ToInt32(this.Server.Status["Sort_scan"], Settings.Culture) + Convert.ToInt32(this.Server.Status["Sort_range"], Settings.Culture));
            if (this.Calculations["total_sorts"] > 0)
            {
                this.Calculations.Add("pct_temp_sort_table", (Convert.ToInt32(this.Server.Status["Sort_merge_passes"], Settings.Culture) / Convert.ToInt32(this.Calculations["total_sorts"], Settings.Culture)) * 100);
            }

            // Joins
            this.Calculations.Add("joins_without_indexes", Convert.ToInt32(this.Server.Status["Select_range_check"], Settings.Culture) + Convert.ToInt32(this.Server.Status["Select_full_join"], Settings.Culture));
            if (this.Calculations["joins_without_indexes"] > 0)
            {
                this.Calculations.Add("joins_without_indexes_per_day", this.Calculations["joins_without_indexes"] / (Convert.ToInt32(this.Server.Status["Uptime"], Settings.Culture) / 86400));
            }
            else
            {
                this.Calculations.Add("joins_without_indexes_per_day", 0);
            }

            // Temporary tables
            if (Convert.ToInt32(this.Server.Status["Created_tmp_tables"], Settings.Culture) > 0)
            {
                if (Convert.ToInt32(this.Server.Status["Created_tmp_disk_tables"], Settings.Culture) > 0)
                {
                    this.Calculations.Add("pct_temp_disk", (Convert.ToInt32(this.Server.Status["Created_tmp_disk_tables"], Settings.Culture) / (Convert.ToInt32(this.Server.Status["Created_tmp_tables"], Settings.Culture) + Convert.ToInt32(this.Server.Status["Created_tmp_disk_tables"], Settings.Culture))) * 100);
                }
                else
                {
                    this.Calculations.Add("pct_temp_disk", 0);
                }
            }

            // Table cache
            if (Convert.ToInt32(this.Server.Status["Opened_tables"], Settings.Culture) > 0)
            {
                this.Calculations.Add("table_cache_hit_rate", Convert.ToInt32(this.Server.Status["Open_tables"], Settings.Culture) * 100 / Convert.ToInt32(this.Server.Status["Opened_tables"], Settings.Culture));
            }
            else
            {
                this.Calculations.Add("table_cache_hit_rate", 100);
            }

            // Open files
            if (Convert.ToInt32(this.Server.Variables["open_files_limit"], Settings.Culture) > 0)
            {
                this.Calculations.Add("pct_files_open", Convert.ToInt32(this.Server.Status["Open_files"], Settings.Culture) * 100 / Convert.ToInt32(this.Server.Variables["open_files_limit"], Settings.Culture));
            }

            // Table locks
            if (Convert.ToInt32(this.Server.Status["Table_locks_immediate"], Settings.Culture) > 0)
            {
                if (this.Server.Status["Table_locks_waited"] == "0")
                {
                    this.Calculations.Add("pct_table_locks_immediate", 100);
                }
                else
                {
                    this.Calculations.Add("pct_table_locks_immediate", Convert.ToInt32(this.Server.Status["Table_locks_immediate"], Settings.Culture) * 100 / (Convert.ToInt32(this.Server.Status["Table_locks_waited"], Settings.Culture) + Convert.ToInt32(this.Server.Status["Table_locks_immediate"], Settings.Culture)));
                }
            }

            // Thread cache
            this.Calculations.Add("thread_cache_hit_rate", 100 - ((Convert.ToInt32(this.Server.Status["Threads_created"], Settings.Culture) / Convert.ToInt32(this.Server.Status["Connections"], Settings.Culture)) * 100));

            // Other
            if (Convert.ToInt32(this.Server.Status["Connections"], Settings.Culture) > 0)
            {
                this.Calculations.Add("pct_aborted_connections", (Convert.ToInt32(this.Server.Status["Aborted_connects"], Settings.Culture) / Convert.ToInt32(this.Server.Status["Connections"], Settings.Culture)) * 100);
            }

            if (Convert.ToInt32(this.Server.Status["Questions"], Settings.Culture) > 0)
            {
                this.Calculations.Add("total_reads", Convert.ToInt32(this.Server.Status["Com_select"], Settings.Culture));
                this.Calculations.Add("total_writes", Convert.ToInt32(this.Server.Status["Com_delete"], Settings.Culture) + Convert.ToInt32(this.Server.Status["Com_insert"], Settings.Culture) + Convert.ToInt32(this.Server.Status["Com_update"], Settings.Culture) + Convert.ToInt32(this.Server.Status["Com_replace"], Settings.Culture));
                if (this.Calculations["total_reads"] == 0)
                {
                    this.Calculations.Add("pct_reads", 0);
                    this.Calculations.Add("pct_writes", 100);
                }
                else
                {
                    this.Calculations.Add("pct_reads", (this.Calculations["total_reads"] / (this.Calculations["total_reads"] + this.Calculations["total_writes"])) * 100);
                    this.Calculations.Add("pct_writes", 100 - this.Calculations["pct_reads"]);
                }
            }

            // InnoDB
            if (this.Server.Variables["have_innodb"] == "YES")
            {
                this.Calculations.Add("innodb_log_size_pct", Convert.ToInt32(this.Server.Variables["innodb_log_file_size"], Settings.Culture) * 100 / Convert.ToInt32(this.Server.Variables["innodb_buffer_pool_size"], Settings.Culture));
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
