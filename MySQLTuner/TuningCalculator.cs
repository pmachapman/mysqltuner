// -----------------------------------------------------------------------
// <copyright file="TuningCalculator.cs" company="Peter Chapman">
// Copyright 2014 Peter Chapman. See http://mysqltuner.codeplex.com/license for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Reflection;
    using System.Windows.Forms;
    using Microsoft.VisualBasic;
    using Microsoft.VisualBasic.Devices;
    using MySql.Data.MySqlClient;

    /// <summary>
    /// The tuning calculator.
    /// </summary>
    public abstract class TuningCalculator : Form
    {
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
        private Dictionary<string, long> Calculations { get; set; }

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
        /// <param name="messageStatus">The status of the message.</param>
        /// <param name="message">The message's notice.</param>
        public abstract void PrintMessage(Status messageStatus, string message);

        /// <summary>
        /// Shows the progress bar as complete or incomplete.
        /// </summary>
        /// <param name="complete">if set to <c>true</c>, the progress bar is complete; otherwise <c>false</c>.</param>
        public abstract void ProgressComplete(bool complete);

        /// <summary>
        /// Performs the calculations
        /// </summary>
        /// <param name="server">The server to calculate.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This only supports english")]
        public void Calculate(MySqlServer server)
        {
            try
            {
                // Set up the calculator
                this.Server = server;
                this.Calculations = new Dictionary<string, long>();
                this.Recommendations = new List<string>();
                this.VariablesToAdjust = new List<string>();

                // Post the first message!
                this.PrintMessage(Status.Info, "MySQL Tuner " + Assembly.GetExecutingAssembly().GetName().Version.ToString(2) + " - Peter Chapman <peter@conglomo.co.nz>");

                // Show the server
                this.PrintMessage(Status.Info, "Performing tests on " + this.Server.Host + ":" + this.Server.Port);

                // See if an empty password was used
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

                // Show enabled storage engines
                this.CheckStorageEngines();

                // Display some security recommendations
                this.SecurityRecommendations();

                // Calculate everything we need
                this.PerformCalculations();

                // Print the server stats
                this.MySqlStats();

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

                // Complete!
                this.PrintMessage(Status.Info, "Scan Complete");

                // Complete the progress bar
                this.ProgressComplete(true);
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException)
                {
                    // This is thrown if the form is closed
                    return;
                }
                else
                {
                    // Display the error to the user
                    this.PrintMessage(Status.Fail, ex.ToString());

                    // Show the progress as incomplete
                    this.ProgressComplete(false);

                    // Throw the error, crashing the thread
                    throw;
                }
            }
        }

        /// <summary>
        /// Calculates the parameter passed in bytes, and then rounds it to one decimal place.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>The bytes formatted and rounded to one decimal place.</returns>
        private static string DisplayBytes(long bytes)
        {
            if (bytes >= Math.Pow(1024, 3))
            {
                // GB
                return (bytes / Math.Pow(1024, 3)).ToString("F1", CultureInfo.CurrentCulture) + "G";
            }
            else if (bytes >= Math.Pow(1024, 2))
            {
                // MB
                return (bytes / Math.Pow(1024, 2)).ToString("F1", CultureInfo.CurrentCulture) + "M";
            }
            else if (bytes >= 1024D)
            {
                // KB
                return (bytes / 1024D).ToString("F1", CultureInfo.CurrentCulture) + "K";
            }
            else
            {
                return bytes + "B";
            }
        }

        /// <summary>
        /// Calculates the parameter passed in bytes, and then rounds it to the nearest integer.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>The bytes formatted and rounded.</returns>
        private static string DisplayBytesRounded(long bytes)
        {
            if (bytes >= Math.Pow(1024, 3))
            {
                // GB
                return (long)(bytes / Math.Pow(1024, 3)) + "G";
            }
            else if (bytes >= Math.Pow(1024, 2))
            {
                // MB
                return (long)(bytes / Math.Pow(1024, 2)) + "M";
            }
            else if (bytes >= 1024)
            {
                // KB
                return (long)(bytes / 1024) + "K";
            }
            else
            {
                return bytes + "B";
            }
        }

        /// <summary>
        /// Calculates the parameter passed to the nearest power of 1000, then rounds it to the nearest integer.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns>The number formatted and rounded.</returns>
        private static string DisplayRounded(long number)
        {
            if (number >= Math.Pow(1000, 3))
            {
                // GB
                return (long)(number / Math.Pow(1000, 3)) + "G";
            }
            else if (number >= Math.Pow(1000, 2))
            {
                // MB
                return (long)(number / Math.Pow(1000, 2)) + "M";
            }
            else if (number >= 1000)
            {
                // KB
                return (long)(number / 1000) + "K";
            }
            else
            {
                return number.ToString(CultureInfo.CurrentCulture);
            }
        }

        /// <summary>
        /// Calculates the parameter passed to the nearest power of 1000, then rounds it to the nearest integer.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns>The number formatted and rounded.</returns>
        private static string DisplayRounded(double number)
        {
            if (number >= Math.Pow(1000, 3))
            {
                // GB
                return (long)(number / Math.Pow(1000, 3)) + "G";
            }
            else if (number >= Math.Pow(1000, 2))
            {
                // MB
                return (long)(number / Math.Pow(1000, 2)) + "M";
            }
            else if (number >= 1000)
            {
                // KB
                return (long)(number / 1000) + "K";
            }
            else
            {
                return number.ToString("F3", CultureInfo.CurrentCulture);
            }
        }

        /// <summary>
        /// Calculates uptime to display in a more attractive form.
        /// </summary>
        /// <param name="uptime">The uptime as a number of seconds.</param>
        /// <returns>The uptime as a string.</returns>
        private static string PrettyUptime(long uptime)
        {
            long seconds = uptime % 60;
            long minutes = (uptime % 3600) / 60;
            long hours = (uptime % 86400) / 3600;
            long days = uptime / 86400;
            if (days > 0)
            {
                return string.Format(CultureInfo.CurrentCulture, "{0}d {1}h {2}m {3}s", days, hours, minutes, seconds);
            }
            else if (hours > 0)
            {
                return string.Format(CultureInfo.CurrentCulture, "{0}h {1}m {2}s", hours, minutes, seconds);
            }
            else if (minutes > 0)
            {
                return string.Format(CultureInfo.CurrentCulture, "{0}m {1}s", minutes, seconds);
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture, "{0}s", seconds);
            }
        }

        /// <summary>
        /// Check for supported or end of life MySQL versions.
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This only supports english")]
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
        /// Show enabled storage engines.
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This only supports english")]
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
            foreach (KeyValuePair<string, long> engineStatistic in this.Server.EngineStatistics)
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
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This only supports english")]
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
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This only supports english")]
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "This is complex code")]
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
                this.Calculations.Add("per_thread_buffers", Convert.ToInt64(this.Server.Variables["read_buffer_size"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Variables["read_rnd_buffer_size"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Variables["sort_buffer_size"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Variables["thread_stack"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Variables["join_buffer_size"], CultureInfo.CurrentCulture));
            }
            else
            {
                this.Calculations.Add("per_thread_buffers", Convert.ToInt64(this.Server.Variables["record_buffer"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Variables["record_rnd_buffer"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Variables["sort_buffer"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Variables["thread_stack"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Variables["join_buffer_size"], CultureInfo.CurrentCulture));
            }

            this.Calculations.Add("total_per_thread_buffers", Convert.ToInt64(this.Calculations["per_thread_buffers"], CultureInfo.CurrentCulture) * Convert.ToInt64(this.Server.Variables["max_connections"], CultureInfo.CurrentCulture));
            this.Calculations.Add("max_total_per_thread_buffers", Convert.ToInt64(this.Calculations["per_thread_buffers"], CultureInfo.CurrentCulture) * Convert.ToInt64(this.Server.Status["Max_used_connections"], CultureInfo.CurrentCulture));

            // Server-wide memory
            this.Calculations.Add("max_tmp_table_size", (Convert.ToInt64(this.Server.Variables["tmp_table_size"], CultureInfo.CurrentCulture) > Convert.ToInt64(this.Server.Variables["max_heap_table_size"], CultureInfo.CurrentCulture)) ? Convert.ToInt64(this.Server.Variables["max_heap_table_size"], CultureInfo.CurrentCulture) : Convert.ToInt64(this.Server.Variables["tmp_table_size"], CultureInfo.CurrentCulture));
            this.Calculations.Add("server_buffers", Convert.ToInt64(this.Server.Variables["key_buffer_size"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Calculations["max_tmp_table_size"], CultureInfo.CurrentCulture));
            this.Calculations["server_buffers"] += this.Server.Variables.ContainsKey("innodb_buffer_pool_size") ? Convert.ToInt64(this.Server.Variables["innodb_buffer_pool_size"], CultureInfo.CurrentCulture) : 0;
            this.Calculations["server_buffers"] += this.Server.Variables.ContainsKey("innodb_additional_mem_pool_size") ? Convert.ToInt64(this.Server.Variables["innodb_additional_mem_pool_size"], CultureInfo.CurrentCulture) : 0;
            this.Calculations["server_buffers"] += this.Server.Variables.ContainsKey("innodb_log_buffer_size") ? Convert.ToInt64(this.Server.Variables["innodb_log_buffer_size"], CultureInfo.CurrentCulture) : 0;
            this.Calculations["server_buffers"] += this.Server.Variables.ContainsKey("query_cache_size") ? Convert.ToInt64(this.Server.Variables["query_cache_size"], CultureInfo.CurrentCulture) : 0;

            // Global memory
            this.Calculations.Add("max_used_memory", this.Calculations["server_buffers"] + this.Calculations["max_total_per_thread_buffers"]);
            this.Calculations.Add("total_possible_used_memory", this.Calculations["server_buffers"] + this.Calculations["total_per_thread_buffers"]);
            this.Calculations.Add("pct_physical_memory", Convert.ToInt64((Convert.ToUInt64(this.Calculations["total_possible_used_memory"], CultureInfo.CurrentCulture) * 100) / this.Server.PhysicalMemory, CultureInfo.CurrentCulture));

            // Slow queries
            this.Calculations.Add("pct_slow_queries", (long)Math.Ceiling((Convert.ToDouble(this.Server.Status["Slow_queries"], CultureInfo.CurrentCulture) / Convert.ToDouble(this.Server.Status["Questions"], CultureInfo.CurrentCulture)) * 100D));

            // Connections
            this.Calculations.Add("pct_connections_used", (long)Math.Ceiling((Convert.ToDouble(this.Server.Status["Max_used_connections"], CultureInfo.CurrentCulture) / Convert.ToDouble(this.Server.Variables["max_connections"], CultureInfo.CurrentCulture)) * 100D));
            this.Calculations["pct_connections_used"] = (this.Calculations["pct_connections_used"] > 100) ? 100 : this.Calculations["pct_connections_used"];

            // Key buffers
            if (this.Server.Version.Major > 3 && !(this.Server.Version.Major == 4 && this.Server.Version.Minor == 0))
            {
                this.Calculations.Add("pct_key_buffer_used", (long)Math.Ceiling(1 - ((Convert.ToDouble(this.Server.Status["Key_blocks_unused"], CultureInfo.CurrentCulture) * Convert.ToDouble(this.Server.Variables["key_cache_block_size"], CultureInfo.CurrentCulture)) / Convert.ToDouble(this.Server.Variables["key_buffer_size"], CultureInfo.CurrentCulture))) * 100);
            }

            if (Convert.ToInt64(this.Server.Status["Key_read_requests"], CultureInfo.CurrentCulture) > 0)
            {
                this.Calculations.Add("pct_keys_from_mem", 100 - (long)Math.Ceiling((Convert.ToDouble(this.Server.Status["Key_reads"], CultureInfo.CurrentCulture) / Convert.ToDouble(this.Server.Status["Key_read_requests"], CultureInfo.CurrentCulture)) * 100D));
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
                this.Calculations.Add("query_cache_efficiency", (long)Math.Ceiling((Convert.ToDouble(this.Server.Status["Qcache_hits"], CultureInfo.CurrentCulture) / (Convert.ToDouble(this.Server.Status["Com_select"], CultureInfo.CurrentCulture) + Convert.ToDouble(this.Server.Status["Qcache_hits"], CultureInfo.CurrentCulture))) * 100D));
                if (this.Server.Variables["query_cache_size"] != "0")
                {
                    this.Calculations.Add("pct_query_cache_used", 100 - (long)Math.Ceiling((Convert.ToDouble(this.Server.Status["Qcache_free_memory"], CultureInfo.CurrentCulture) / Convert.ToDouble(this.Server.Variables["query_cache_size"], CultureInfo.CurrentCulture)) * 100));
                }

                if (this.Server.Status["Qcache_lowmem_prunes"] == "0")
                {
                    this.Calculations.Add("query_cache_prunes_per_day", 0);
                }
                else
                {
                    this.Calculations.Add("query_cache_prunes_per_day", Convert.ToInt64(this.Server.Status["Qcache_lowmem_prunes"], CultureInfo.CurrentCulture) / (long)Math.Ceiling(Convert.ToInt64(this.Server.Status["Uptime"], CultureInfo.CurrentCulture) / 86400D));
                }
            }

            // Sorting
            this.Calculations.Add("total_sorts", Convert.ToInt64(this.Server.Status["Sort_scan"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Status["Sort_range"], CultureInfo.CurrentCulture));
            if (this.Calculations["total_sorts"] > 0)
            {
                this.Calculations.Add("pct_temp_sort_table", (long)Math.Ceiling((Convert.ToDouble(this.Server.Status["Sort_merge_passes"], CultureInfo.CurrentCulture) / Convert.ToDouble(this.Calculations["total_sorts"], CultureInfo.CurrentCulture)) * 100D));
            }

            // Joins
            this.Calculations.Add("joins_without_indexes", Convert.ToInt64(this.Server.Status["Select_range_check"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Status["Select_full_join"], CultureInfo.CurrentCulture));
            if (this.Calculations["joins_without_indexes"] > 0)
            {
                this.Calculations.Add("joins_without_indexes_per_day", this.Calculations["joins_without_indexes"] / (long)Math.Ceiling(Convert.ToInt64(this.Server.Status["Uptime"], CultureInfo.CurrentCulture) / 86400D));
            }
            else
            {
                this.Calculations.Add("joins_without_indexes_per_day", 0);
            }

            // Temporary tables
            if (Convert.ToInt64(this.Server.Status["Created_tmp_tables"], CultureInfo.CurrentCulture) > 0)
            {
                if (Convert.ToInt64(this.Server.Status["Created_tmp_disk_tables"], CultureInfo.CurrentCulture) > 0)
                {
                    this.Calculations.Add("pct_temp_disk", (long)Math.Ceiling((Convert.ToDouble(this.Server.Status["Created_tmp_disk_tables"], CultureInfo.CurrentCulture) / (Convert.ToDouble(this.Server.Status["Created_tmp_tables"], CultureInfo.CurrentCulture) + Convert.ToDouble(this.Server.Status["Created_tmp_disk_tables"], CultureInfo.CurrentCulture))) * 100D));
                }
                else
                {
                    this.Calculations.Add("pct_temp_disk", 0);
                }
            }

            // Table cache
            if (Convert.ToInt64(this.Server.Status["Opened_tables"], CultureInfo.CurrentCulture) > 0)
            {
                this.Calculations.Add("table_cache_hit_rate", Convert.ToInt64(this.Server.Status["Open_tables"], CultureInfo.CurrentCulture) * 100 / Convert.ToInt64(this.Server.Status["Opened_tables"], CultureInfo.CurrentCulture));
            }
            else
            {
                this.Calculations.Add("table_cache_hit_rate", 100);
            }

            // Open files
            if (Convert.ToInt64(this.Server.Variables["open_files_limit"], CultureInfo.CurrentCulture) > 0)
            {
                this.Calculations.Add("pct_files_open", Convert.ToInt64(this.Server.Status["Open_files"], CultureInfo.CurrentCulture) * 100 / Convert.ToInt64(this.Server.Variables["open_files_limit"], CultureInfo.CurrentCulture));
            }

            // Table locks
            if (Convert.ToInt64(this.Server.Status["Table_locks_immediate"], CultureInfo.CurrentCulture) > 0)
            {
                if (this.Server.Status["Table_locks_waited"] == "0")
                {
                    this.Calculations.Add("pct_table_locks_immediate", 100);
                }
                else
                {
                    this.Calculations.Add("pct_table_locks_immediate", Convert.ToInt64(this.Server.Status["Table_locks_immediate"], CultureInfo.CurrentCulture) * 100 / (Convert.ToInt64(this.Server.Status["Table_locks_waited"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Status["Table_locks_immediate"], CultureInfo.CurrentCulture)));
                }
            }

            // Thread cache
            this.Calculations.Add("thread_cache_hit_rate", 100 - (long)Math.Ceiling((Convert.ToDouble(this.Server.Status["Threads_created"], CultureInfo.CurrentCulture) / Convert.ToDouble(this.Server.Status["Connections"], CultureInfo.CurrentCulture)) * 100D));

            // Other
            if (Convert.ToInt64(this.Server.Status["Connections"], CultureInfo.CurrentCulture) > 0)
            {
                this.Calculations.Add("pct_aborted_connections", (long)Math.Ceiling((Convert.ToDouble(this.Server.Status["Aborted_connects"], CultureInfo.CurrentCulture) / Convert.ToDouble(this.Server.Status["Connections"], CultureInfo.CurrentCulture)) * 100D));
            }

            if (Convert.ToInt64(this.Server.Status["Questions"], CultureInfo.CurrentCulture) > 0)
            {
                this.Calculations.Add("total_reads", Convert.ToInt64(this.Server.Status["Com_select"], CultureInfo.CurrentCulture));
                this.Calculations.Add("total_writes", Convert.ToInt64(this.Server.Status["Com_delete"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Status["Com_insert"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Status["Com_update"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Status["Com_replace"], CultureInfo.CurrentCulture));
                if (this.Calculations["total_reads"] == 0)
                {
                    this.Calculations.Add("pct_reads", 0);
                    this.Calculations.Add("pct_writes", 100);
                }
                else
                {
                    this.Calculations.Add("pct_reads", (long)Math.Ceiling((Convert.ToDouble(this.Calculations["total_reads"]) / Convert.ToDouble(this.Calculations["total_reads"] + this.Calculations["total_writes"])) * 100D));
                    this.Calculations.Add("pct_writes", 100 - this.Calculations["pct_reads"]);
                }
            }

            // InnoDB
            if (this.Server.Variables["have_innodb"] == "YES")
            {
                this.Calculations.Add("innodb_log_size_pct", Convert.ToInt64(this.Server.Variables["innodb_log_file_size"], CultureInfo.CurrentCulture) * 100 / Convert.ToInt64(this.Server.Variables["innodb_buffer_pool_size"], CultureInfo.CurrentCulture));
            }
        }

        /// <summary>
        /// Print the server stats.
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This only supports english")]
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "This is complex code")]
        [SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode", Justification = "This is complex code")]
        private void MySqlStats()
        {
            // Show uptime, queries per second, connections, traffic stats
            double qps;
            if (Convert.ToInt64(this.Server.Status["Uptime"], CultureInfo.CurrentCulture) > 0)
            {
                qps = Convert.ToInt64(this.Server.Status["Questions"], CultureInfo.CurrentCulture) / Convert.ToInt64(this.Server.Status["Uptime"], CultureInfo.CurrentCulture);
            }
            else
            {
                qps = 0;
            }

            if (Convert.ToInt64(this.Server.Status["Uptime"], CultureInfo.CurrentCulture) < 86400)
            {
                this.Recommendations.Add("MySQL started within last 24 hours - recommendations may be inaccurate");
            }

            this.PrintMessage(Status.Info, "Up for: " + PrettyUptime(Convert.ToInt64(this.Server.Status["Uptime"], CultureInfo.CurrentCulture)) + " (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Questions"], CultureInfo.CurrentCulture)) + " q [" + DisplayRounded(qps) + " qps], " + DisplayRounded(Convert.ToInt64(this.Server.Status["Connections"], CultureInfo.CurrentCulture)) + " conn, TX: " + DisplayRounded(Convert.ToInt64(this.Server.Status["Bytes_sent"], CultureInfo.CurrentCulture)) + ", RX: " + DisplayRounded(Convert.ToInt64(this.Server.Status["Bytes_received"], CultureInfo.CurrentCulture)) + ")");
            this.PrintMessage(Status.Info, "Reads / Writes: " + this.Calculations["pct_reads"] + "% / " + this.Calculations["pct_writes"] + "%");

            // Memory usage
            this.PrintMessage(Status.Info, "Total buffers: " + DisplayBytes(this.Calculations["server_buffers"]) + " global + " + DisplayBytes(this.Calculations["per_thread_buffers"]) + " per thread (" + this.Server.Variables["max_connections"] + " max threads)");
            if (this.Calculations["total_possible_used_memory"] > 2147483648 && this.Server.PhysicalMemory < 2147483648)
            {
                this.PrintMessage(Status.Fail, "Allocating > 2GB RAM on 32-bit systems can cause system instability");
                this.PrintMessage(Status.Fail, "Maximum possible memory usage: " + DisplayBytes(this.Calculations["total_possible_used_memory"]) + " (" + this.Calculations["pct_physical_memory"] + "% of installed RAM)");
            }
            else if (this.Calculations["pct_physical_memory"] > 85)
            {
                this.PrintMessage(Status.Fail, "Maximum possible memory usage: " + DisplayBytes(this.Calculations["total_possible_used_memory"]) + " (" + this.Calculations["pct_physical_memory"] + "% of installed RAM)");
                this.Recommendations.Add("Reduce your overall MySQL memory footprint for system stability");
            }
            else
            {
                this.PrintMessage(Status.Pass, "Maximum possible memory usage: " + DisplayBytes(this.Calculations["total_possible_used_memory"]) + " (" + this.Calculations["pct_physical_memory"] + "% of installed RAM)");
            }

            // Slow queries
            if (this.Calculations["pct_slow_queries"] > 5)
            {
                this.PrintMessage(Status.Fail, "Slow queries: " + this.Calculations["pct_slow_queries"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Slow_queries"], CultureInfo.CurrentCulture)) + "/" + DisplayRounded(Convert.ToInt64(this.Server.Status["Questions"], CultureInfo.CurrentCulture)) + ")");
            }
            else
            {
                this.PrintMessage(Status.Pass, "Slow queries: " + this.Calculations["pct_slow_queries"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Slow_queries"], CultureInfo.CurrentCulture)) + "/" + DisplayRounded(Convert.ToInt64(this.Server.Status["Questions"], CultureInfo.CurrentCulture)) + ")");
            }

            if (Convert.ToDouble(this.Server.Variables["long_query_time"], CultureInfo.CurrentCulture) > 10)
            {
                this.VariablesToAdjust.Add("long_query_time (<= 10)");
            }

            if (this.Server.Variables.ContainsKey("log_slow_queries"))
            {
                if (this.Server.Variables["log_slow_queries"] == "OFF")
                {
                    this.Recommendations.Add("Enable the slow query log to troubleshoot bad queries");
                }
            }

            // Connections
            if (this.Calculations["pct_connections_used"] > 85)
            {
                this.PrintMessage(Status.Fail, "Highest connection usage: " + this.Calculations["pct_connections_used"] + "%  (" + this.Server.Status["Max_used_connections"] + "/" + this.Server.Variables["max_connections"] + ")");
                this.VariablesToAdjust.Add("max_connections (> " + this.Server.Variables["max_connections"] + ")");
                this.VariablesToAdjust.Add("wait_timeout (< " + this.Server.Variables["wait_timeout"] + ")");
                this.VariablesToAdjust.Add("interactive_timeout (< " + this.Server.Variables["interactive_timeout"] + ")");
                this.Recommendations.Add("Reduce or eliminate persistent connections to reduce connection usage");
            }
            else
            {
                this.PrintMessage(Status.Pass, "Highest usage of available connections: " + this.Calculations["pct_connections_used"] + "% (" + this.Server.Status["Max_used_connections"] + "/" + this.Server.Variables["max_connections"] + ")");
            }

            // Key buffer
            if (!this.Calculations.ContainsKey("total_myisam_indexes"))
            {
                this.Recommendations.Add("Unable to calculate MyISAM indexes on remote MySQL server < 5.0.0");
            }
            else if (!this.Calculations.ContainsKey("total_myisam_indexes") || this.Calculations["total_myisam_indexes"] == 0)
            {
                this.PrintMessage(Status.Fail, "Cannot calculate MyISAM index size - please run this program as an Administrator");
            }
            else if (this.Calculations["total_myisam_indexes"] == 0)
            {
                this.PrintMessage(Status.Fail, "None of your MyISAM tables are indexed - add indexes immediately");
            }
            else
            {
                if (Convert.ToInt64(this.Server.Variables["key_buffer_size"], CultureInfo.CurrentCulture) < this.Calculations["total_myisam_indexes"] && this.Calculations["pct_keys_from_mem"] < 95)
                {
                    this.PrintMessage(Status.Fail, "Key buffer size / total MyISAM indexes: " + DisplayBytes(Convert.ToInt64(this.Server.Variables["key_buffer_size"], CultureInfo.CurrentCulture)) + "/" + DisplayBytes(this.Calculations["total_myisam_indexes"]));
                    this.VariablesToAdjust.Add("key_buffer_size (> " + DisplayBytes(this.Calculations["total_myisam_indexes"]) + ")");
                }
                else
                {
                    this.PrintMessage(Status.Pass, "Key buffer size / total MyISAM indexes: " + DisplayBytes(Convert.ToInt64(this.Server.Variables["key_buffer_size"], CultureInfo.CurrentCulture)) + "/" + DisplayBytes(this.Calculations["total_myisam_indexes"]));
                }

                if (Convert.ToInt64(this.Server.Status["Key_read_requests"], CultureInfo.CurrentCulture) > 0)
                {
                    if (this.Calculations["pct_keys_from_mem"] < 95)
                    {
                        this.PrintMessage(Status.Fail, "Key buffer hit rate: " + this.Calculations["pct_keys_from_mem"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Key_read_requests"], CultureInfo.CurrentCulture)) + " cached / " + DisplayRounded(Convert.ToInt64(this.Server.Status["Key_reads"], CultureInfo.CurrentCulture)) + " reads)");
                    }
                    else
                    {
                        this.PrintMessage(Status.Pass, "Key buffer hit rate: " + this.Calculations["pct_keys_from_mem"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Key_read_requests"], CultureInfo.CurrentCulture)) + " cached / " + DisplayRounded(Convert.ToInt64(this.Server.Status["Key_reads"], CultureInfo.CurrentCulture)) + " reads)");
                    }
                }
                else
                {
                    // No queries have run that would use keys
                }
            }

            // Query cache
            if (this.Server.Version.Major < 4)
            {
                // MySQL versions < 4.01 don't support query caching
                this.Recommendations.Add("Upgrade MySQL to version 4+ to utilize query caching");
            }
            else if (Convert.ToInt64(this.Server.Variables["query_cache_size"], CultureInfo.CurrentCulture) < 1)
            {
                this.PrintMessage(Status.Fail, "Query cache is disabled");
                this.VariablesToAdjust.Add("query_cache_size (>= 8M)");
            }
            else if (Convert.ToInt64(this.Server.Status["Com_select"], CultureInfo.CurrentCulture) == 0)
            {
                this.PrintMessage(Status.Fail, "Query cache cannot be analyzed - no SELECT statements executed");
            }
            else
            {
                if (this.Calculations["query_cache_efficiency"] < 20)
                {
                    this.PrintMessage(Status.Fail, "Query cache efficiency: " + this.Calculations["query_cache_efficiency"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Qcache_hits"], CultureInfo.CurrentCulture)) + " cached / " + DisplayRounded(Convert.ToInt64(this.Server.Status["Qcache_hits"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Status["Com_select"], CultureInfo.CurrentCulture)) + " selects)");
                    this.VariablesToAdjust.Add("query_cache_limit (> " + DisplayBytesRounded(Convert.ToInt64(this.Server.Variables["query_cache_limit"], CultureInfo.CurrentCulture)) + ", or use smaller result sets)");
                }
                else
                {
                    this.PrintMessage(Status.Pass, "Query cache efficiency: " + this.Calculations["query_cache_efficiency"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Qcache_hits"], CultureInfo.CurrentCulture)) + " cached / " + DisplayRounded(Convert.ToInt64(this.Server.Status["Qcache_hits"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Status["Com_select"], CultureInfo.CurrentCulture)) + " selects)");
                }

                if (this.Calculations["query_cache_prunes_per_day"] > 98)
                {
                    this.PrintMessage(Status.Fail, "Query cache prunes per day: " + this.Calculations["query_cache_prunes_per_day"]);
                    if (Convert.ToInt64(this.Server.Variables["query_cache_size"], CultureInfo.CurrentCulture) > 128 * 1024 * 1024)
                    {
                        this.Recommendations.Add("Increasing the query_cache size over 128M may reduce performance");
                        this.VariablesToAdjust.Add("query_cache_size (> " + DisplayBytesRounded(Convert.ToInt64(this.Server.Variables["query_cache_size"], CultureInfo.CurrentCulture)) + ") [see warning above]");
                    }
                    else
                    {
                        this.VariablesToAdjust.Add("query_cache_size (> " + DisplayBytesRounded(Convert.ToInt64(this.Server.Variables["query_cache_size"], CultureInfo.CurrentCulture)) + ")");
                    }
                }
                else
                {
                    this.PrintMessage(Status.Pass, "Query cache prunes per day: " + this.Calculations["query_cache_prunes_per_day"]);
                }
            }

            // Sorting
            if (this.Calculations["total_sorts"] == 0)
            {
                // For the sake of space, we will be quiet here
                // No sorts have run yet
            }
            else if (this.Calculations["pct_temp_sort_table"] > 10)
            {
                this.PrintMessage(Status.Fail, "Sorts requiring temporary tables: " + this.Calculations["pct_temp_sort_table"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Sort_merge_passes"], CultureInfo.CurrentCulture)) + " temp sorts / " + DisplayRounded(this.Calculations["total_sorts"]) + " sorts)");
                this.VariablesToAdjust.Add("sort_buffer_size (> " + DisplayBytesRounded(Convert.ToInt64(this.Server.Variables["sort_buffer_size"], CultureInfo.CurrentCulture)) + ")");
                this.VariablesToAdjust.Add("read_rnd_buffer_size (> " + DisplayBytesRounded(Convert.ToInt64(this.Server.Variables["read_rnd_buffer_size"], CultureInfo.CurrentCulture)) + ")");
            }
            else
            {
                this.PrintMessage(Status.Pass, "Sorts requiring temporary tables: " + this.Calculations["pct_temp_sort_table"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Sort_merge_passes"], CultureInfo.CurrentCulture)) + " temp sorts / " + DisplayRounded(this.Calculations["total_sorts"]) + " sorts)");
            }

            // Joins
            if (this.Calculations["joins_without_indexes_per_day"] > 250)
            {
                this.PrintMessage(Status.Fail, "Joins performed without indexes: " + this.Calculations["joins_without_indexes"]);
                this.VariablesToAdjust.Add("join_buffer_size (> " + DisplayBytes(Convert.ToInt64(this.Server.Variables["join_buffer_size"], CultureInfo.CurrentCulture)) + ", or always use indexes with joins)");
                this.Recommendations.Add("Adjust your join queries to always utilize indexes");
            }
            else
            {
                // For the sake of space, we will be quiet here
                // No joins have run without indexes
            }

            // Temporary tables
            if (Convert.ToInt64(this.Server.Status["Created_tmp_tables"], CultureInfo.CurrentCulture) > 0)
            {
                if (this.Calculations["pct_temp_disk"] > 25 && this.Calculations["max_tmp_table_size"] < 256 * 1024 * 1024)
                {
                    this.PrintMessage(Status.Fail, "Temporary tables created on disk: " + this.Calculations["pct_temp_disk"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Created_tmp_disk_tables"], CultureInfo.CurrentCulture)) + " on disk / " + DisplayRounded(Convert.ToInt64(this.Server.Status["Created_tmp_disk_tables"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Status["Created_tmp_tables"], CultureInfo.CurrentCulture)) + " total)");
                    this.VariablesToAdjust.Add("tmp_table_size (> " + DisplayBytesRounded(Convert.ToInt64(this.Server.Variables["tmp_table_size"], CultureInfo.CurrentCulture)) + ")");
                    this.VariablesToAdjust.Add("max_heap_table_size (> " + DisplayBytesRounded(Convert.ToInt64(this.Server.Variables["max_heap_table_size"], CultureInfo.CurrentCulture)) + ")");
                    this.Recommendations.Add("When making adjustments, make tmp_table_size/max_heap_table_size equal");
                    this.Recommendations.Add("Reduce your SELECT DISTINCT queries without LIMIT clauses");
                }
                else if (this.Calculations["pct_temp_disk"] > 25 && this.Calculations["max_tmp_table_size"] >= 256)
                {
                    this.PrintMessage(Status.Fail, "Temporary tables created on disk: " + this.Calculations["pct_temp_disk"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Created_tmp_disk_tables"], CultureInfo.CurrentCulture)) + " on disk / " + DisplayRounded(Convert.ToInt64(this.Server.Status["Created_tmp_disk_tables"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Status["Created_tmp_tables"], CultureInfo.CurrentCulture)) + " total)");
                    this.Recommendations.Add("Temporary table size is already large - reduce result set size");
                    this.Recommendations.Add("Reduce your SELECT DISTINCT queries without LIMIT clauses");
                }
                else
                {
                    this.PrintMessage(Status.Pass, "Temporary tables created on disk: " + this.Calculations["pct_temp_disk"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Created_tmp_disk_tables"], CultureInfo.CurrentCulture)) + " on disk / " + DisplayRounded(Convert.ToInt64(this.Server.Status["Created_tmp_disk_tables"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Status["Created_tmp_tables"], CultureInfo.CurrentCulture)) + " total)");
                }
            }
            else
            {
                // For the sake of space, we will be quiet here
                // No temporary tables have been created
            }

            // Thread cache
            if (Convert.ToInt64(this.Server.Variables["thread_cache_size"], CultureInfo.CurrentCulture) == 0)
            {
                this.PrintMessage(Status.Fail, "Thread cache is disabled");
                this.Recommendations.Add("Set thread_cache_size to 4 as a starting value");
                this.VariablesToAdjust.Add("thread_cache_size (start at 4)");
            }
            else
            {
                if (this.Calculations["thread_cache_hit_rate"] <= 50)
                {
                    this.PrintMessage(Status.Fail, "Thread cache hit rate: " + this.Calculations["thread_cache_hit_rate"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Threads_created"], CultureInfo.CurrentCulture)) + " created / " + DisplayRounded(Convert.ToInt64(this.Server.Status["Connections"], CultureInfo.CurrentCulture)) + " connections)");
                    this.VariablesToAdjust.Add("thread_cache_size (> " + this.Server.Variables["thread_cache_size"] + ")");
                }
                else
                {
                    this.PrintMessage(Status.Pass, "Thread cache hit rate: " + this.Calculations["thread_cache_hit_rate"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Threads_created"], CultureInfo.CurrentCulture)) + " created / " + DisplayRounded(Convert.ToInt64(this.Server.Status["Connections"], CultureInfo.CurrentCulture)) + " connections)");
                }
            }

            // Table cache
            if (Convert.ToInt64(this.Server.Status["Open_tables"], CultureInfo.CurrentCulture) > 0)
            {
                if (this.Calculations["table_cache_hit_rate"] < 20)
                {
                    this.PrintMessage(Status.Fail, "Table cache hit rate: " + this.Calculations["table_cache_hit_rate"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Open_tables"], CultureInfo.CurrentCulture)) + " open / " + DisplayRounded(Convert.ToInt64(this.Server.Status["Opened_tables"], CultureInfo.CurrentCulture)) + " opened)");
                    if (this.Server.Version.Major > 5 || (this.Server.Version.Major == 5 && this.Server.Version.Minor >= 1))
                    {
                        this.VariablesToAdjust.Add("table_cache (> " + this.Server.Variables["table_open_cache"] + ")");
                    }
                    else
                    {
                        this.VariablesToAdjust.Add("table_cache (> " + this.Server.Variables["table_cache"] + ")");
                    }

                    this.Recommendations.Add("Increase table_cache gradually to avoid file descriptor limits");
                }
                else
                {
                    this.PrintMessage(Status.Pass, "Table cache hit rate: " + this.Calculations["table_cache_hit_rate"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Open_tables"], CultureInfo.CurrentCulture)) + " open / " + DisplayRounded(Convert.ToInt64(this.Server.Status["Opened_tables"], CultureInfo.CurrentCulture)) + " opened)");
                }
            }

            // Open files
            if (this.Calculations.ContainsKey("pct_files_open"))
            {
                if (this.Calculations["pct_files_open"] > 85)
                {
                    this.PrintMessage(Status.Fail, "Open file limit used: " + this.Calculations["pct_files_open"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Open_files"], CultureInfo.CurrentCulture)) + "/" + DisplayRounded(Convert.ToInt64(this.Server.Variables["open_files_limit"], CultureInfo.CurrentCulture)) + ")");
                    this.VariablesToAdjust.Add("open_files_limit (> " + this.Server.Variables["open_files_limit"] + ")");
                }
                else
                {
                    this.PrintMessage(Status.Pass, "Open file limit used: " + this.Calculations["pct_files_open"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Open_files"], CultureInfo.CurrentCulture)) + "/" + DisplayRounded(Convert.ToInt64(this.Server.Variables["open_files_limit"], CultureInfo.CurrentCulture)) + ")");
                }
            }

            // Table locks
            if (this.Calculations.ContainsKey("pct_table_locks_immediate"))
            {
                if (this.Calculations["pct_table_locks_immediate"] < 95)
                {
                    this.PrintMessage(Status.Fail, "Table locks acquired immediately: " + this.Calculations["pct_table_locks_immediate"] + "%");
                    this.Recommendations.Add("Optimize queries and/or use InnoDB to reduce lock wait");
                }
                else
                {
                    this.PrintMessage(Status.Pass, "Table locks acquired immediately: " + this.Calculations["pct_table_locks_immediate"] + "% (" + DisplayRounded(Convert.ToInt64(this.Server.Status["Table_locks_immediate"], CultureInfo.CurrentCulture)) + " immediate / " + DisplayRounded(Convert.ToInt64(this.Server.Status["Table_locks_waited"], CultureInfo.CurrentCulture) + Convert.ToInt64(this.Server.Status["Table_locks_immediate"], CultureInfo.CurrentCulture)) + " locks)");
                }
            }

            // Performance options
            if (this.Server.Version.Major < 4 || (this.Server.Version.Major == 4 && this.Server.Version.Minor == 0))
            {
                this.Recommendations.Add("Upgrade to MySQL 4.1+ to use concurrent MyISAM inserts");
            }
            else if (this.Server.Variables["concurrent_insert"] == "NEVER")
            {
                this.Recommendations.Add("Enable concurrent_insert by setting it to 'AUTO' OR 'ALWAYS'");
            }
            else if (this.Server.Variables["concurrent_insert"] == "OFF")
            {
                this.Recommendations.Add("Enable concurrent_insert by setting it to 'ON'");
            }
            else if (this.Server.Variables["concurrent_insert"] != "ON" && this.Server.Variables["concurrent_insert"] != "AUTO" && this.Server.Variables["concurrent_insert"] != "ALWAYS" && Convert.ToInt64(this.Server.Variables["concurrent_insert"], CultureInfo.CurrentCulture) == 0)
            {
                this.Recommendations.Add("Enable concurrent_insert by setting it to 1");
            }

            if (this.Calculations["pct_aborted_connections"] > 5)
            {
                this.PrintMessage(Status.Fail, "Connections aborted: " + this.Calculations["pct_aborted_connections"] + "%");
                this.Recommendations.Add("Your applications are not closing MySQL connections properly");
            }

            // InnoDB
            if (this.Server.Variables.ContainsKey("have_innodb") && this.Server.Variables["have_innodb"] == "YES" && this.Server.EngineStatistics.ContainsKey("InnoDB"))
            {
                if (Convert.ToInt64(this.Server.Variables["innodb_buffer_pool_size"], CultureInfo.CurrentCulture) > this.Server.EngineStatistics["InnoDB"])
                {
                    this.PrintMessage(Status.Pass, "InnoDB data size / buffer pool: " + DisplayBytes(this.Server.EngineStatistics["InnoDB"]) + "/" + DisplayBytes(Convert.ToInt64(this.Server.Variables["innodb_buffer_pool_size"], CultureInfo.CurrentCulture)));
                }
                else
                {
                    this.PrintMessage(Status.Fail, "InnoDB data size / buffer pool: " + DisplayBytes(this.Server.EngineStatistics["InnoDB"]) + "/" + DisplayBytes(Convert.ToInt64(this.Server.Variables["innodb_buffer_pool_size"], CultureInfo.CurrentCulture)));
                    this.VariablesToAdjust.Add("innodb_buffer_pool_size (>= " + DisplayBytesRounded(this.Server.EngineStatistics["InnoDB"]) + ")");
                }
            }
        }
    }
}
