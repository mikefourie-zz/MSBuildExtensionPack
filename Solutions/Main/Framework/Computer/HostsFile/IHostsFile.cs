//-----------------------------------------------------------------------
// <copyright file="IHostsFile.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;

    public interface IHostsFile
    {
        void SetHostEntry(string hostName, string ipAddress);

        void SetHostEntry(string hostName, string ipAddress, string comment);

        void Save(TextWriter sw);
    }

    internal sealed class HostsFileEntries : IHostsFile
    {
        private const string Separator = "   ";
        private static readonly string[] Pads = new[]
                                                    {
                                                        string.Empty,
                                                        " ",
                                                        "  ",
                                                        "   ",
                                                        "    ",
                                                        "     ",
                                                        "      ",
                                                        "       ",
                                                        "        ",
                                                        "         ",
                                                        "          ",
                                                        "           ",
                                                        "            ",
                                                        "             ",
                                                        "              ",
                                                        "               "
                                                    };

        private readonly Regex hostsEntryRegex = new Regex(@"^((\d{1,3}\.){3}\d{1,3})\s+(?<HostName>[^\s#]+)(?<Tail>.*)$");
        private readonly Dictionary<string, HostsEntry> hosts;
        private readonly List<string> hostsFileLines;

        internal HostsFileEntries(string[] hostEntries) : this(hostEntries, false)
        {
        }

        internal HostsFileEntries(string[] hostEntries, bool truncate)
        {
            if (hostEntries == null)
            {
                hostEntries = new string[0];
            }

            this.hosts = new Dictionary<string, HostsEntry>(hostEntries.Length);

            if (truncate)
            {
                this.hostsFileLines = new List<string>();
                foreach (var line in hostEntries)
                {
                    if (line.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                    {
                        this.hostsFileLines.Add(line);
                    }
                    else
                    {
                        break;
                    }
                }

                this.hostsFileLines.Add(string.Empty);
                this.SetHostEntry("localhost", "127.0.0.1");
                return;
            }

            this.hostsFileLines = new List<string>(hostEntries);
            var lineNum = 0;
            foreach (var line in this.hostsFileLines)
            {
                var match = this.hostsEntryRegex.Match(line);
                if (match.Success)
                {
                    var hostsEntry = new HostsEntry(lineNum, match.Groups["HostName"].Value, match.Groups["Tail"].Value);
                    var hostsEntryKey = hostsEntry.HostName.ToLower(CultureInfo.InvariantCulture);
                    if (!this.hosts.ContainsKey(hostsEntryKey))
                    {
                        this.hosts[hostsEntryKey] = hostsEntry;
                    }
                }

                lineNum++;
            }
        }

        public void SetHostEntry(string hostName, string ipAddress)
        {
            this.SetHostEntry(hostName, ipAddress, string.Empty);
        }

        public void SetHostEntry(string hostName, string ipAddress, string comment)
        {
            string hostsKey = hostName.ToLower(CultureInfo.InvariantCulture);
            string tail = string.IsNullOrEmpty(comment) ? null : ("\t# " + comment);
            string hostsLine = PadIPAddress(ipAddress) + Separator + hostName;
            if (this.hosts.ContainsKey(hostsKey))
            {
                HostsEntry hostEntry = this.hosts[hostsKey];
                this.hostsFileLines[hostEntry.LineNumber] = hostsLine + (tail ?? hostEntry.Tail);
            }
            else
            {
                this.hostsFileLines.Add(hostsLine + tail);
                this.hosts[hostsKey] = new HostsEntry(this.hostsFileLines.Count - 1, hostName, tail);
            }
        }

        public void Save(TextWriter sw)
        {
            if (sw != null)
            {
                foreach (string s in this.hostsFileLines)
                {
                    sw.WriteLine(s);
                }
            }
        }
        
        private static string PadIPAddress(string ipAddress)
        {
            int ipLength = (ipAddress == null) ? 0 : ipAddress.Length;
            int numSpaces = 15 - ipLength;
            return ipAddress + Pads[numSpaces];
        }

        private sealed class HostsEntry
        {
            public HostsEntry(int lineNumber, string hostName, string tail)
            {
                this.LineNumber = lineNumber;
                this.HostName = hostName;
                this.Tail = tail;
            }

            public string HostName { get; private set; }

            public int LineNumber { get; private set; }

            public string Tail { get; private set; }
        }
    }
}
