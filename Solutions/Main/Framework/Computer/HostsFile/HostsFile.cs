//-----------------------------------------------------------------------
// <copyright file="HostsFile.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>SetHostEntry</i> (<b>Required: </b> HostName, IPAddress<b> Optional: </b>Comment, PathToHostsFile)</para>
    /// <para><i>Update</i> (<b>Required: </b>HostEntries <b>Optional: </b>PathToHostsFile, Truncate</para>
    /// <para><b>Remote Execution Support:</b> No</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>    
    ///     <ItemGroup>
    ///         <HostEntries Include="MyWebService">
    ///             <IPAddress>10.0.0.1</IPAddress>
    ///             <Comment>The IP address for MyWebService</Comment>
    ///         </HostEntries>
    ///         <HostEntries Include="MyWebSite">
    ///             <IPAddress>10.0.0.2</IPAddress>
    ///         </HostEntries>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Update the current machine's hosts file.  With Truncate=True, any hosts entries not in @(HostEntries) will
    ///        be deleted (except for the default localhost/127.0.0.1 entry). -->
    ///         <MSBuild.ExtensionPack.Computer.HostsFile TaskAction="Update"
    ///                                                   HostEntries="@(HostEntries)"
    ///                                                   Truncate="True" />
    ///         <!-- Update a hosts file in a custom location.  -->
    ///         <MSBuild.ExtensionPack.Computer.HostsFile TaskAction="Update"
    ///                                                   HostEntries="@(HostEntries)"
    ///                                                   PathToHostsFile="\\SDG-WKS1348\a\hosts" />
    ///         <!-- Update a single host entry.  If the entry doesn't exist, it will be created. -->
    ///         <MSBuild.ExtensionPack.Computer.HostsFile TaskAction="SetHostEntry"
    ///                                                   HostName="MyInternalHost"
    ///                                                   IPAddress="10.0.0.3"
    ///                                                   Comment="This points to the MyInternalHost server." />
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public sealed class HostsFile : BaseTask
    {
        internal const string SetHostEntryTaskAction = "SetHostEntry";
        internal const string UpdateTaskAction = "Update";
        private readonly IHostsFileReader hostsFileReader;
        private readonly IHostsFileWriter hostsFileWriter;

        public HostsFile() : this(new HostsFileReader(), new HostsFileWriter())
        {
        }

        internal HostsFile(IHostsFileReader hostsFileReader, IHostsFileWriter hostsFileWriter)
        {
            this.hostsFileReader = hostsFileReader;
            this.hostsFileWriter = hostsFileWriter;
        }

        /// <summary>
        /// The comment after the hosts entry.  Only used by the SetHostEntry task action.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// The hostname to alias.  Only used by the SetHostEntry task action.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// The IP address for the hosts entry being aliased.  Required.  Only used by the SetHostEntry task action.
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// If True, any host entry not in the HostEntries item group will be removed from the hosts file.  Default is
        /// False.  Only used by the Update task action.
        /// </summary>
        public bool Truncate { get; set; }

        /// <summary>
        /// The list of hosts entries.  The identity should be the host name.  The IP address should be in the IPAddress
        /// metadata.  A comment about the entry should be in the Comment metadata.  The task will fail if the identity or
        /// IPAddress metadata are empty.
        /// </summary>
        public ITaskItem[] HostEntries { get; set; }

        /// <summary>
        /// The path to the hosts file to update.  Defaults to %SYSTEMROOT%\system32\drivers\etc\hosts.  Task will fail
        /// if this file doesn't exist.
        /// </summary>
        public string PathToHostsFile { get; set; }

        protected override void InternalExecute()
        {
            var pathToHostsFile = this.PathToHostsFile;
            if (string.IsNullOrEmpty(pathToHostsFile))
            {
                pathToHostsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Path to hosts file is empty; defaulting to {0}.", pathToHostsFile));
            }

            if (!File.Exists(pathToHostsFile))
            {
                this.Log.LogError("Unable to find hosts file at '{0}'.", pathToHostsFile);
                return;
            }

            List<ITaskItem> hostEntries;
            var truncate = this.Truncate;

            switch (this.TaskAction)
            {
                case UpdateTaskAction:
                    if (this.HostEntries == null || this.HostEntries.Length == 0)
                    {
                        this.Log.LogError("HostsEntries property is empty or missing.");
                        return;
                    }

                    hostEntries = new List<ITaskItem>(this.HostEntries);
                    break;
                case SetHostEntryTaskAction:
                    // not allowed to truncate when only updating one host entry
                    truncate = false;
                    var hostEntry = new TaskItem(this.HostName);
                    hostEntry.SetMetadata("IPAddress", this.IPAddress);
                    if (!string.IsNullOrEmpty(this.Comment))
                    {
                        hostEntry.SetMetadata("Comment", this.Comment);
                    }

                    hostEntries = new List<ITaskItem>(new[] { hostEntry });
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }

            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Opening hosts file {0}.", pathToHostsFile));
            var hostsFile = this.hostsFileReader.Read(pathToHostsFile, truncate);

            if (hostEntries.Any(hostEntry => !this.SetHostEntry(hostEntry, hostsFile)))
            {
                return;
            }

            bool changedAttribute = false;

            try
            {
                this.LogTaskMessage(MessageImportance.Normal, string.Format(CultureInfo.InvariantCulture, "Writing changes to {0}.", pathToHostsFile));

                FileAttributes fileAttributes = System.IO.File.GetAttributes(pathToHostsFile);

                // If readonly attribute is set, reset it.
                if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    this.LogTaskMessage(MessageImportance.Low, "Making file writable");
                    System.IO.File.SetAttributes(pathToHostsFile, fileAttributes ^ FileAttributes.ReadOnly);
                    changedAttribute = true;
                }

                this.hostsFileWriter.Write(pathToHostsFile, hostsFile);
            }
            catch (Exception ex)
            {
                this.Log.LogError("An error occurred updating the host file at '{0}'.", this.PathToHostsFile);
                this.Log.LogErrorFromException(ex);
            }
            finally
            {
                if (changedAttribute)
                {
                    this.LogTaskMessage(MessageImportance.Low, "Making file readonly");
                    System.IO.File.SetAttributes(pathToHostsFile, FileAttributes.ReadOnly);
                }
            }
        }

        private bool SetHostEntry(ITaskItem hostEntry, IHostsFile hostsFile)
        {
            var ipAddress = hostEntry.GetMetadata("IPAddress");
            var hostName = hostEntry.ItemSpec;
            var comment = hostEntry.GetMetadata("Comment");

            if (string.IsNullOrEmpty(hostName))
            {
                this.Log.LogError("HostName is null or empty.");
                return false;
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                this.Log.LogError("IPAddress is null or empty for hostname '{0}.", hostName);
                return false;
            }

            IPAddress parsedIPAddress;
            if (!System.Net.IPAddress.TryParse(ipAddress, out parsedIPAddress))
            {
                this.Log.LogError("Invalid IP address ({0}) for hostname '{1}'.", ipAddress, hostName);
                return false;
            }

            this.LogTaskMessage(MessageImportance.Normal, string.Format(CultureInfo.InvariantCulture, "Updating hosts entry for host {0} to IP address {1}.", hostName, ipAddress));
            hostsFile.SetHostEntry(hostName, ipAddress, comment);
            return true;
        }
    }
}
