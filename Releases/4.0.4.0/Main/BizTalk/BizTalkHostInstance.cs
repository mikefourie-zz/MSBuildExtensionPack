//-----------------------------------------------------------------------
// <copyright file="BizTalkHostInstance.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.BizTalk
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Management;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CheckExists</i> (<b>Required: </b>HostName <b>Optional: </b>MachineName <b>Output: </b>Exists, State)</para>
    /// <para><i>Create</i> (<b>Required: </b>HostName, AccountName, AccountPassword <b>Optional: </b>MachineName, Force)</para>
    /// <para><i>Delete</i> (<b>Required: </b>HostName <b>Optional: </b>MachineName)</para>
    /// <para><i>GetState</i> (<b>Required: </b>HostName <b>Optional: </b>MachineName <b>Output:</b> State)</para>
    /// <para><i>Start</i> (<b>Required: </b>HostName <b>Optional: </b>MachineName)</para>
    /// <para><i>Stop</i> (<b>Required: </b>HostName <b>Optional: </b>MachineName)</para>
    /// <para><b>Remote Execution Support:</b> Yes</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- Create a Host Instance -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance TaskAction="Create" HostName="MSBEPTESTHOST" AccountName="yourserviceaccount" AccountPassword="yourpassword" Force="True"/>
    ///         <!-- Check a Host Instance exists (it should), and get it's state-->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance TaskAction="CheckExists" HostName="MSBEPTESTHOST">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///             <Output TaskParameter="State" PropertyName="InstanceState"/>
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance>
    ///         <Message Text="BizTalkHostInstance Exists: $(DoesExist) and is $(InstanceState)"/>
    ///         <!-- Start a Host Instance -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance TaskAction="Start" HostName="MSBEPTESTHOST"/>
    ///         <!-- Get a Host Instance state-->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance TaskAction="GetState" HostName="MSBEPTESTHOST">
    ///             <Output TaskParameter="State" PropertyName="InstanceState"/>
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance>
    ///         <Message Text="BizTalkHostInstance state: $(InstanceState)"/>
    ///         <!-- Stop a Host Instance -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance TaskAction="Stop" HostName="MSBEPTESTHOST"/>
    ///         <!-- Get a Host Instance state-->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance TaskAction="GetState" HostName="MSBEPTESTHOST">
    ///             <Output TaskParameter="State" PropertyName="InstanceState"/>
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance>
    ///         <Message Text="BizTalkHostInstance state: $(InstanceState)"/>
    ///         <!-- Delete a Host Instance -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance TaskAction="Delete" HostName="MSBEPTESTHOST"/>
    ///         <!-- Check a Host Instance exists again (it shouldn't)-->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance TaskAction="CheckExists" HostName="MSBEPTESTHOST">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance>
    ///         <Message Text="BizTalkHostInstance Exists: $(DoesExist)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.3.0/html/97edac8b-db9c-f0e9-2c24-76f6b873b4cf.htm")]
    public class BizTalkHostInstance : BaseTask
    {
        private const string CheckExistsTaskAction = "CheckExists";
        private const string CreateTaskAction = "Create";
        private const string DeleteTaskAction = "Delete";
        private const string GetStateTaskAction = "GetState";
        private const string StartTaskAction = "Start";
        private const string StopTaskAction = "Stop";
        private const string WmiBizTalkNamespace = @"\root\MicrosoftBizTalkServer";
        private ManagementObject hostInstance;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(CheckExistsTaskAction)]
        [DropdownValue(CreateTaskAction)]
        [DropdownValue(DeleteTaskAction)]
        [DropdownValue(StartTaskAction)]
        [DropdownValue(StopTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the MachineName.
        /// </summary>
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(DeleteTaskAction, false)]
        [TaskAction(StartTaskAction, false)]
        [TaskAction(StopTaskAction, false)]
        public override string MachineName
        {
            get { return base.MachineName; }
            set { base.MachineName = value; }
        }

        /// <summary>
        /// The parent hostname for the host instance
        /// </summary>
        [TaskAction(CheckExistsTaskAction, true)]
        [TaskAction(CreateTaskAction, true)]
        [TaskAction(DeleteTaskAction, true)]
        [TaskAction(StartTaskAction, true)]
        [TaskAction(StopTaskAction, true)]
        [Required]
        public string HostName { get; set; }

        /// <summary>
        /// Gets whether the host instance exists
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Set to true to delete an existing host instance when Create is called.
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public bool Force { get; set; }

        /// <summary>
        /// The logon account to use for the Host Instance
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public string AccountName { get; set; }

        /// <summary>
        /// The logon password to use for the Host Instance 
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public string AccountPassword { get; set; }

        /// <summary>
        /// Gets the state of the host instance
        /// </summary>
        [Output]
        public string State { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            this.GetManagementScope(WmiBizTalkNamespace);

            switch (this.TaskAction)
            {
                case StartTaskAction:
                    this.Start();
                    break;
                case StopTaskAction:
                    this.Stop();
                    break;
                case CreateTaskAction:
                    this.Create();
                    break;
                case DeleteTaskAction:
                    this.Delete();
                    break;
                case GetStateTaskAction:
                    this.GetState();
                    break;
                case CheckExistsTaskAction:
                    this.CheckExists();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Start()
        {
            if (!this.GetHostInstanceByHostName())
            {
                return;
            }

            if ((uint)this.hostInstance["HostType"] == (uint)BizTalkHostType.InProcess && (uint)this.hostInstance["ServiceState"] != (uint)HostState.Running)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Starting Host Instance: {0} on: {1}", this.HostName, this.MachineName));
                this.hostInstance.InvokeMethod("Start", null);
            }
        }

        private void GetState()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting Host Instance: {0} state on: {1}", this.HostName, this.MachineName));

            if (!this.GetHostInstanceByHostName())
            {
                return;
            }

            HostState s = (HostState)Convert.ToInt32(this.hostInstance["ServiceState"], CultureInfo.InvariantCulture);
            this.State = s.ToString();
        }

        private void Stop()
        {
            if (!this.GetHostInstanceByHostName())
            {
                return;
            }

            if ((uint)this.hostInstance["HostType"] == (uint)BizTalkHostType.InProcess && (uint)this.hostInstance["ServiceState"] != (uint)HostState.Stopped)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Stopping Host Instance: {0} on: {1}", this.HostName, this.MachineName));
                this.hostInstance.InvokeMethod("Stop", null);
            }
        }

        private void Delete()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting Host Instance: {0} on: {1}", this.HostName, this.MachineName));
            if (!this.GetHostInstanceByHostName())
            {
                return;
            }

            if ((uint)this.hostInstance["HostType"] == (uint)BizTalkHostType.InProcess && (uint)this.hostInstance["ServiceState"] != (uint)HostState.Stopped)
            {
                this.hostInstance.InvokeMethod("Stop", null);
            }

            this.hostInstance.InvokeMethod("UnInstall", null);

            // unmap the host instance from the host
            ManagementObject host = this.CreateHost();
            host.InvokeMethod("UnMap", null);
        }

        private void Create()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating Host Instance: {0} on: {1}", this.HostName, this.MachineName));

            if (this.CheckExists())
            {
                if (this.Force)
                {
                    this.Delete();
                }
                else
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Host Instance: {0} already exists on: {1}. Set Force to true to delete the Host Instance.", this.HostName, this.MachineName));
                    return;
                }
            }

            if (string.IsNullOrEmpty(this.AccountName) || string.IsNullOrEmpty(this.AccountPassword))
            {
                Log.LogError("AccountName and AccountPassword are required");
                return;
            }

            // map the host instance to the host
            ManagementObject host = this.CreateHost();
            host.InvokeMethod("Map", null);

            using (ManagementClass instance = new ManagementClass(this.Scope, new ManagementPath("MSBTS_HostInstance"), null))
            {
                ManagementObject hostInstanceSettings = instance.CreateInstance();
                if (hostInstanceSettings == null)
                {
                    Log.LogError("There was a failure creating the MSBTS_HostInstance instance");
                    return;
                }

                string hostInstanceName = string.Format(CultureInfo.CurrentCulture, "Microsoft BizTalk Server {0} {1}", this.HostName, this.MachineName);
                hostInstanceSettings["Name"] = hostInstanceName;

                object[] args = new object[2];
                args[0] = this.AccountName;
                args[1] = this.AccountPassword;

                hostInstanceSettings.InvokeMethod("Install", args);
            }
        }

        private bool CheckExists()
        {
            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Checking whether Host Instance exists: {0}", this.HostName));
            this.GetHostInstanceByHostName();
            if (this.hostInstance != null)
            {
                this.Exists = true;
                HostState s = (HostState)Convert.ToInt32(this.hostInstance["ServiceState"], CultureInfo.InvariantCulture);
                this.State = s.ToString();
                return true;
            }

            return false;
        }

        private bool GetHostInstanceByHostName()
        {
            EnumerationOptions wmiEnumerationOptions = new EnumerationOptions { ReturnImmediately = false };
            ObjectQuery wmiQuery = new ObjectQuery(string.Format(CultureInfo.CurrentCulture, "SELECT * FROM MSBTS_HostInstance WHERE HostName = '{0}'", this.HostName));
            using (ManagementObjectSearcher wmiSearcher = new ManagementObjectSearcher(this.Scope, wmiQuery, wmiEnumerationOptions))
            {
                ManagementObjectCollection hostInstanceCollection = wmiSearcher.Get();
                foreach (ManagementObject instance in hostInstanceCollection.Cast<ManagementObject>().Where(instance => instance["Name"].ToString().IndexOf(this.MachineName, StringComparison.OrdinalIgnoreCase) > 0))
                {
                    this.hostInstance = instance;
                    return true;
                }
            }

            return false;
        }

        private ManagementObject CreateHost()
        {
            using (ManagementClass hostFactory = new ManagementClass(this.Scope, new ManagementPath("MSBTS_ServerHost"), null))
            {
                ManagementObject host = hostFactory.CreateInstance();
                if (host == null)
                {
                    Log.LogError("There was a failure creating the MSBTS_ServerHost instance");
                    throw new Exception("There was a failure creating the MSBTS_ServerHost instance");
                }

                host["ServerName"] = this.MachineName;
                host["HostName"] = this.HostName;

                return host;
            }
        }
    }
}
