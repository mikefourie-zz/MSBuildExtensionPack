//-----------------------------------------------------------------------
// <copyright file="Network.cs">(c) FreeToDev. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System.Globalization;
    using System.Net.NetworkInformation;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Ping</i> (<b>Required: </b> HostName <b>Optional: </b>Timeout, PingCount <b>Output:</b> Exists)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- Ping a host -->
    ///         <MSBuild.ExtensionPack.Computer.Network TaskAction="Ping" HostName="YOURHOSTNAME">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.Network>
    ///         <Message Text="Exists: $(DoesExist)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Network : BaseTask
    {
        private int pingCount = 5;
        private int timeout = 3000;

        /// <summary>
        /// Sets the HostName / IP
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Gets whether the Host Exists
        /// </summary>
        [Output]
        public bool Exists { get; private set; }

        /// <summary>
        /// Sets the number of pings to attempt. Default is 5.
        /// </summary>
        public int PingCount
        {
            get { return this.pingCount; }
            set { this.pingCount = value; }
        }

        /// <summary>
        /// Sets the timeout in ms for a Ping. Default is 3000
        /// </summary>
        public int Timeout
        {
            get { return this.timeout; }
            set { this.timeout = value; }
        }

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
                case "Ping":
                    this.Ping();
                    break;
                default:
                    Log.LogError(string.Format("Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
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
                    this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Pinging {0}", this.HostName));
                    PingReply response = pinger.Send(this.HostName, this.Timeout, buffer, options);
                    if (response != null && response.Status == IPStatus.Success)
                    {
                        this.Exists = true;
                        return;
                    }

                    System.Threading.Thread.Sleep(1000);
                }

                this.Exists = false;
            }
        }
    }
}