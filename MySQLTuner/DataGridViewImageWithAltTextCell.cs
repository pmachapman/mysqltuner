// -----------------------------------------------------------------------
// <copyright file="DataGridViewImageWithAltTextCell.cs" company="Peter Chapman">
// Copyright 2018 Peter Chapman. See LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace MySqlTuner
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Windows.Forms;

    /// <summary>
    /// Provides alternate text for copying to the clipboard.
    /// </summary>
    /// <seealso cref="System.Windows.Forms.DataGridViewImageCell" />
    public class DataGridViewImageWithAltTextCell : DataGridViewImageCell
    {
        /// <summary>
        /// Gets or sets the alternate text.
        /// </summary>
        /// <value>
        /// The alternate text.
        /// </value>
        public string AltText { get; set; }

        /// <summary>
        /// Retrieves the formatted value of the cell to copy to the <see cref="T:System.Windows.Forms.Clipboard" />.
        /// </summary>
        /// <param name="rowIndex">The zero-based index of the row containing the cell.</param>
        /// <param name="firstCell"><see langword="true" /> to indicate that the cell is in the first column of the region defined by the selected cells; otherwise, <see langword="false" />.</param>
        /// <param name="lastCell"><see langword="true" /> to indicate that the cell is the last column of the region defined by the selected cells; otherwise, <see langword="false" />.</param>
        /// <param name="inFirstRow"><see langword="true" /> to indicate that the cell is in the first row of the region defined by the selected cells; otherwise, <see langword="false" />.</param>
        /// <param name="inLastRow"><see langword="true" /> to indicate that the cell is in the last row of the region defined by the selected cells; otherwise, <see langword="false" />.</param>
        /// <param name="format">The current format string of the cell.</param>
        /// <returns>
        /// An <see cref="T:System.Object" /> that represents the value of the cell to copy to the <see cref="T:System.Windows.Forms.Clipboard" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">rowIndex</exception>
        protected override object GetClipboardContent(int rowIndex, bool firstCell, bool lastCell, bool inFirstRow, bool inLastRow, string format)
        {
            if (this.DataGridView == null)
            {
                return null;
            }

            // Header Cell classes override this implementation - this implementation is only for inner cells
            if (rowIndex < 0 || rowIndex >= this.DataGridView.Rows.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(rowIndex));
            }

            StringBuilder sb = new StringBuilder(64);
            if (string.Equals(format, DataFormats.Html, StringComparison.OrdinalIgnoreCase))
            {
                if (firstCell)
                {
                    if (inFirstRow)
                    {
                        sb.Append("<TABLE>");
                    }

                    sb.Append("<TR>");
                }

                sb.Append("<TD>");
                if (!string.IsNullOrEmpty(this.AltText))
                {
                    FormatPlainTextAsHtml(this.AltText, new StringWriter(sb, CultureInfo.CurrentCulture));
                }
                else
                {
                    sb.Append("&nbsp;");
                }

                sb.Append("</TD>");
                if (lastCell)
                {
                    sb.Append("</TR>");
                    if (inLastRow)
                    {
                        sb.Append("</TABLE>");
                    }
                }

                return sb.ToString();
            }
            else
            {
                bool csv = string.Equals(format, DataFormats.CommaSeparatedValue, StringComparison.OrdinalIgnoreCase);
                if (csv ||
                    string.Equals(format, DataFormats.Text, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(format, DataFormats.UnicodeText, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(this.AltText))
                    {
                        if (firstCell && lastCell && inFirstRow && inLastRow)
                        {
                            sb.Append(this.AltText);
                        }
                        else
                        {
                            bool escapeApplied = false;
                            int insertionPoint = sb.Length;
                            FormatPlainText(this.AltText, csv, new StringWriter(sb, CultureInfo.CurrentCulture), ref escapeApplied);
                            if (escapeApplied)
                            {
                                sb.Insert(insertionPoint, '"');
                            }
                        }
                    }

                    if (lastCell)
                    {
                        if (!inLastRow)
                        {
                            sb.Append((char)Keys.Return);
                            sb.Append((char)Keys.LineFeed);
                        }
                    }
                    else
                    {
                        sb.Append(csv ? ',' : (char)Keys.Tab);
                    }

                    return sb.ToString();
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Formats the plain text.
        /// </summary>
        /// <param name="s">The text.</param>
        /// <param name="csv">If set to <c>true</c> format as CSV.</param>
        /// <param name="output">The output.</param>
        /// <param name="escapeApplied">If set to <c>true</c> escape was applied.</param>
        private static void FormatPlainText(string s, bool csv, TextWriter output, ref bool escapeApplied)
        {
            if (s == null)
            {
                return;
            }

            int cb = s.Length;
            for (int i = 0; i < cb; i++)
            {
                char ch = s[i];
                switch (ch)
                {
                    case '"':
                        if (csv)
                        {
                            output.Write("\"\"");
                            escapeApplied = true;
                        }
                        else
                        {
                            output.Write('"');
                        }

                        break;
                    case ',':
                        if (csv)
                        {
                            escapeApplied = true;
                        }

                        output.Write(',');
                        break;
                    case '\t':
                        if (!csv)
                        {
                            output.Write(' ');
                        }
                        else
                        {
                            output.Write('\t');
                        }

                        break;
                    default:
                        output.Write(ch);
                        break;
                }
            }

            if (escapeApplied)
            {
                output.Write('"'); // terminating double-quote.
                // the caller is responsible for inserting the opening double-quote.
            }
        }

        /// <summary>
        /// Formats the plain text as HTML.
        /// </summary>
        /// <param name="s">The text.</param>
        /// <param name="output">The output.</param>
        private static void FormatPlainTextAsHtml(string s, TextWriter output)
        {
            if (s == null)
            {
                return;
            }

            int cb = s.Length;
            char prevCh = '\0';

            for (int i = 0; i < cb; i++)
            {
                char ch = s[i];
                switch (ch)
                {
                    case '<':
                        output.Write("&lt;");
                        break;
                    case '>':
                        output.Write("&gt;");
                        break;
                    case '"':
                        output.Write("&quot;");
                        break;
                    case '&':
                        output.Write("&amp;");
                        break;
                    case ' ':
                        if (prevCh == ' ')
                        {
                            output.Write("&nbsp;");
                        }
                        else
                        {
                            output.Write(ch);
                        }

                        break;
                    case '\r':
                        // Ignore \r, only handle \n
                        break;
                    case '\n':
                        output.Write("<br>");
                        break;
                    default:
                        output.Write(ch);
                        break;
                }

                prevCh = ch;
            }
        }
    }
}
