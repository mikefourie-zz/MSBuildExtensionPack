//-----------------------------------------------------------------------
// <copyright file="Network.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// Parts of this task are based on code from (http://sedodream.codeplex.com). It is used here with permission.
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.Globalization;
    using System.Net;
    using System.Net.NetworkInformation;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>GetDnsHostName</i> (<b>Required: HostName</b> <b>Output:</b> DnsHostName)</para>
    /// <para><i>GetInternalIP</i> (<b>Output:</b> Ip)</para>
    /// <para><i>GetRemoteIP</i> (<b>Required: </b>HostName <b>Output:</b> Ip)</para>
    /// <para><i>Ping</i> (<b>Required: </b> HostName <b>Optional: </b>Timeout, PingCount <b>Output:</b> Exists)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///   </PropertyGroup>
    ///   <Import Project="$(TPath)"/>
    ///   <Target Name="Default">
    ///     <!-- Get the Machine IP Addresses -->
    ///     <MSBuild.ExtensionPack.Computer.Network TaskAction="GetInternalIP">
    ///       <Output TaskParameter="IP" ItemName="TheIP"/>
    ///     </MSBuild.ExtensionPack.Computer.Network>
    ///     <Message Text="The IP: %(TheIP.Identity)"/>
    ///     <!-- Get Remote IP Addresses -->
    ///     <MSBuild.ExtensionPack.Computer.Network TaskAction="GetRemoteIP" HostName="www.freetodev.com">
    ///       <Output TaskParameter="IP" ItemName="TheRemoteIP"/>
    ///     </MSBuild.ExtensionPack.Computer.Network>
    ///     <Message Text="The Remote IP: %(TheRemoteIP.Identity)"/>
    ///     <!-- Ping a host -->
    ///     <MSBuild.ExtensionPack.Computer.Network TaskAction="Ping" HostName="www.freetodev.com">
    ///       <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///     </MSBuild.ExtensionPack.Computer.Network>
    ///     <Message Text="Exists: $(DoesExist)"/>
    ///     <!-- Gets the fully-qualified domain name for a hostname. -->
    ///     <MSBuild.ExtensionPack.Computer.Network TaskAction="GetDnsHostName" HostName="192.168.0.15">
    ///       <Output TaskParameter="DnsHostName" PropertyName="HostEntryName" />
    ///     </MSBuild.ExtensionPack.Computer.Network>
    ///     <Message Text="Host Entry name: $(HostEntryName)" />
    ///   </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.3.0/html/2719abfe-553d-226c-d75f-2964c24f1965.htm")]    
    public class Network : BaseTask
    {
        private const string GetDnsHostNameTaskAction = "GetDnsHostName";
        private const string GetInternalIPTaskAction = "GetInternalIP";
        private const string GetRemoteIPTaskAction = "GetRemoteIP";
        private const string PingTaskAction = "Ping";
        
        private int pingCount = 5;
        private int timeout = 3000;

        [DropdownValue(GetInternalIPTaskAction)]
        [DropdownValue(GetRemoteIPTaskAction)]
        [DropdownValue(PingTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the HostName / IP address
        /// </summary>
        [TaskAction(GetDnsHostNameTaskAction, true)]
        [TaskAction(PingTaskAction, true)]
        public string HostName { get; set; }

        /// <summary>
        /// Gets whether the Host Exists
        /// </summary>
        [Output]
        [TaskAction(PingTaskAction, false)]
        public bool Exists { get; private set; }

        /// <summary>
        /// Sets the number of pings to attempt. Default is 5.
        /// </summary>
        [TaskAction(PingTaskAction, false)]
        public int PingCount
        {
            get { return this.pingCount; }
            set { this.pingCount = value; }
        }

        /// <summary>
        /// Sets the timeout in ms for a Ping. Default is 3000
        /// </summary>
        [TaskAction(PingTaskAction, false)]
        public int Timeout
        {
            get { return this.timeout; }
            set { this.timeout = value; }
        }

        /// <summary>
        /// Gets the IP's
        /// </summary>
        [Output]
        public ITaskItem[] IP { get; set; }

        /// <summary>
        /// Gets the DnsHostName
        /// </summary>
        [Output]
        public string DnsHostName { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            switch (this.TaskAction)
            {
                case PingTaskAction:
                    this.Ping();
                    break;
                case GetInternalIPTaskAction:
                    this.GetInternalIP();
                    break;
                case GetRemoteIPTaskAction:
                    this.GetRemoteIP();
                    break;
                case GetDnsHostNameTaskAction:
                    this.GetDnsHostName();
                    break;
                default:
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void GetDnsHostName()
        {
            if (string.IsNullOrEmpty(this.HostName))
            {
                Log.LogError("HostName is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting host entry name for: {0}", this.HostName));
            var hostEntry = Dns.GetHostEntry(this.HostName);
            this.DnsHostName = hostEntry.HostName;
        }

        private void GetRemoteIP()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Get Remote IP for: {0}", this.HostName));
            IPAddress[] addresslist = Dns.GetHostAddresses(this.HostName);
            this.IP = new ITaskItem[addresslist.Length];
            for (int i = 0; i < addresslist.Length; i++)
            {
                ITaskItem newItem = new TaskItem(addresslist[i].ToString());
                this.IP[i] = newItem;
            }
        }

        private void GetInternalIP()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Get Internal IP for: {0}", Environment.MachineName));
            string hostName = Dns.GetHostName();
            if (string.IsNullOrEmpty(hostName))
            {
                this.LogTaskWarning("Trying to determine IP addresses but Dns.GetHostName() returned an empty value");
                return;
            }

            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            if (hostEntry.AddressList == null || hostEntry.AddressList.Length <= 0)
            {
                this.LogTaskWarning("Trying to determine internal IP addresses but address list is empty");
                return;
            }

            this.IP = new ITaskItem[hostEntry.AddressList.Length];
            for (int i = 0; i < hostEntry.AddressList.Length; i++)
            {
                ITaskItem newItem = new TaskItem(hostEntry.AddressList[i].ToString());
                this.IP[i] = newItem;
            }
        }

        private void Ping()
        {
            const int BufferSize = 32;
            const int TimeToLive = 128;

            byte[] buffer = new byte[BufferSize];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = unchecked((byte)i);
            }

            using (System.Net.NetworkInformation.Ping pinger = new System.Net.NetworkInformation.Ping())
            {
                PingOptions options = new PingOptions(TimeToLive, false);
                for (int i = 0; i < this.PingCount; i++)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Pinging {0}", this.HostName));
                    PingReply response = pinger.Send(this.HostName, this.Timeout, buffer, options);
                    if (response != null && response.Status == IPStatus.Success)
                    {
                        this.Exists = true;
                        return;
                    }
                    
                    if (response != null)
                    {
                        this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Response Status {0}", response.Status));
                    }

                    System.Threading.Thread.Sleep(1000);
                }

                this.Exists = false;
            }
        }
    }
}