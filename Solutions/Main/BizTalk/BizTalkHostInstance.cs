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
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CheckExists</i> (<b>Required: </b>HostName <b>Optional: </b>MachineName <b>Output: </b>Exists, State)</para>
    /// <para><i>Create</i> (<b>Required: </b>HostName, AccountName, AccountPassword <b>Optional: </b>MachineName, Force)</para>
    /// <para><i>Delete</i> (<b>Required: </b>HostName <b>Optional: </b>MachineName)</para>
    /// <para><i>Get</i> (<b>Optional: </b>MachineName <b>Output:</b> HostInstances)</para>
    /// <para><i>GetState</i> (<b>Required: </b>HostName <b>Optional: </b>MachineName <b>Output:</b> State)</para>
    /// <para><i>Start</i> (<b>Required: </b>HostName or HostInstances <b>Optional: </b>MachineName)</para>
    /// <para><i>Stop</i> (<b>Required: </b>HostName or HostInstances <b>Optional: </b>MachineName)</para>
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
    ///         <!-- Get a list of Host Instances -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance TaskAction="Get">
    ///             <Output TaskParameter="HostInstances" ItemName="His"/>
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkHostInstance>
    ///         <Message Text="HI: %(His.Identity) - ServiceState: %(His.ServiceState)"/>
    ///         <Message Text="HI: %(His.Identity) - HostType: %(His.HostType)"/>
    ///         <Message Text="HI: %(His.Identity) - IsDisabled: %(His.IsDisabled)"/>
    ///         <Message Text="HI: %(His.Identity) - Caption: %(His.Caption)"/>
    ///         <Message Text="HI: %(His.Identity) - ConfigurationState: %(His.ConfigurationState)"/>
    ///         <Message Text="HI: %(His.Identity) - Description: %(His.Description)"/>
    ///         <Message Text="HI: %(His.Identity) - InstallDate: %(His.InstallDate)"/>
    ///         <Message Text="HI: %(His.Identity) - MgmtDbNameOverride: %(His.MgmtDbNameOverride)"/>
    ///         <Message Text="HI: %(His.Identity) - MgmtDbServerOverride: %(His.MgmtDbServerOverride)"/>
    ///         <Message Text="HI: %(His.Identity) - RunningServer: %(His.RunningServer)"/>
    ///         <Message Text="HI: %(His.Identity) - Status: %(His.Status)"/>
    ///         <Message Text="HI: %(His.Identity) - UniqueID: %(His.UniqueID)"/>
    ///         <Message Text="HI: %(His.Identity) - NTGroupName: %(His.NTGroupName)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.6.0/html/97edac8b-db9c-f0e9-2c24-76f6b873b4cf.htm")]
    public class BizTalkHostInstance : BaseTask
    {
        private const string GetTaskAction = "Get";
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
        [DropdownValue(GetStateTaskAction)]
        [DropdownValue(DeleteTaskAction)]
        [DropdownValue(StartTaskAction)]
        [DropdownValue(StopTaskAction)]
        [DropdownValue(GetTaskAction)]
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
        [TaskAction(GetTaskAction, false)]
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
        /// The Host Instances returned by Get. The name of the Host Instance is used as the Identity. Metadata includes: Caption, ConfigurationState, Description, HostType, InstallDate, IsDisabled, MgmtDbNameOverride, MgmtDbServerOverride, NTGroupName, RunningServer, ServiceState, Status, UniqueID. HostInstances may also be used to Stop or Start a group of HostInstances in parallel.
        /// </summary>
        [TaskAction(GetTaskAction, false)]
        [Output]
        public ITaskItem[] HostInstances { get; set; }

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
            if (this.HostInstances == null)
            {
                if (this.TaskAction != GetTaskAction && string.IsNullOrEmpty(this.HostName))
                {
                    this.Log.LogError("HostName is required");
                    return;
                }
            }

            this.GetManagementScope(WmiBizTalkNamespace);

            switch (this.TaskAction)
            {
                case GetTaskAction:
                    this.Get();
                    break;
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

        private void Get()
        {
            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Getting Host Instances from: {0}", this.MachineName));

            EnumerationOptions wmiEnumerationOptions = new EnumerationOptions { ReturnImmediately = false };
            ObjectQuery wmiQuery = new ObjectQuery("SELECT * FROM MSBTS_HostInstance");
            using (ManagementObjectSearcher wmiSearcher = new ManagementObjectSearcher(this.Scope, wmiQuery, wmiEnumerationOptions))
            {
                ManagementObjectCollection hostInstanceCollection = wmiSearcher.Get();
                this.HostInstances = new ITaskItem[hostInstanceCollection.Count];
                int i = 0;
                foreach (ManagementObject instance in hostInstanceCollection.Cast<ManagementObject>().Where(instance => instance["Name"].ToString().IndexOf(this.MachineName, StringComparison.OrdinalIgnoreCase) > 0))
                {
                    ITaskItem hostInstanceItem = new TaskItem(instance["HostName"].ToString());
                    hostInstanceItem.SetMetadata("HostType", instance["HostType"].ToString());
                    hostInstanceItem.SetMetadata("IsDisabled", instance["IsDisabled"].ToString());
                    hostInstanceItem.SetMetadata("ServiceState", instance["ServiceState"].ToString());
                    hostInstanceItem.SetMetadata("Caption", instance["Caption"] == null ? string.Empty : instance["Caption"].ToString());
                    hostInstanceItem.SetMetadata("ConfigurationState", instance["ConfigurationState"] == null ? string.Empty : instance["ConfigurationState"].ToString());
                    hostInstanceItem.SetMetadata("Description", instance["Description"] == null ? string.Empty : instance["Description"].ToString());
                    hostInstanceItem.SetMetadata("InstallDate", instance["InstallDate"] == null ? string.Empty : instance["InstallDate"].ToString());
                    hostInstanceItem.SetMetadata("MgmtDbNameOverride", instance["MgmtDbNameOverride"] == null ? string.Empty : instance["MgmtDbNameOverride"].ToString());
                    hostInstanceItem.SetMetadata("MgmtDbServerOverride", instance["MgmtDbServerOverride"] == null ? string.Empty : instance["MgmtDbServerOverride"].ToString());
                    hostInstanceItem.SetMetadata("RunningServer", instance["RunningServer"] == null ? string.Empty : instance["RunningServer"].ToString());
                    hostInstanceItem.SetMetadata("Status", instance["Status"] == null ? string.Empty : instance["Status"].ToString());
                    hostInstanceItem.SetMetadata("UniqueID", instance["UniqueID"] == null ? string.Empty : instance["UniqueID"].ToString());
                    hostInstanceItem.SetMetadata("NTGroupName", instance["NTGroupName"] == null ? string.Empty : instance["NTGroupName"].ToString());
                    this.HostInstances[i] = hostInstanceItem;
                    i++;
                }
            }
        }

        private void Start()
        {
            if (this.HostInstances == null)
            {
                this.StartLogic(this.HostName);
                return;
            }

            System.Threading.Tasks.Parallel.ForEach(this.HostInstances, instance => this.StartLogic(instance.ItemSpec));
        }

        private void StartLogic(string hostName)
        {
            ManagementObject mo = this.GetHostInstanceObjectByHostName(hostName);
            if (mo == null)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Host Instance: {0} not found on: {1}.", this.HostName, this.MachineName));
                return;
            }

            if ((uint)mo["HostType"] == (uint)BizTalkHostType.InProcess && (uint)mo["ServiceState"] != (uint)HostState.Running)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Starting Host Instance: {0} on: {1}", hostName, this.MachineName));
                mo.InvokeMethod("Start", null);
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
            if (this.HostInstances == null)
            {
                this.StopLogic(this.HostName);
                return;
            }

            System.Threading.Tasks.Parallel.ForEach(this.HostInstances, instance => this.StopLogic(instance.ItemSpec));
        }

        private void StopLogic(string hostName)
        {
            ManagementObject mo = this.GetHostInstanceObjectByHostName(hostName);
            if (mo == null)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Host Instance: {0} not found on: {1}.", this.HostName, this.MachineName));
                return;
            }

            if ((uint)mo["HostType"] == (uint)BizTalkHostType.InProcess && (uint)mo["ServiceState"] != (uint)HostState.Stopped)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Stopping Host Instance: {0} on: {1}", hostName, this.MachineName));
                mo.InvokeMethod("Stop", null);
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

        private ManagementObject GetHostInstanceObjectByHostName(string hostName)
        {
            EnumerationOptions wmiEnumerationOptions = new EnumerationOptions { ReturnImmediately = false };
            ObjectQuery wmiQuery = new ObjectQuery(string.Format(CultureInfo.CurrentCulture, "SELECT * FROM MSBTS_HostInstance WHERE HostName = '{0}'", hostName));
            using (ManagementObjectSearcher wmiSearcher = new ManagementObjectSearcher(this.Scope, wmiQuery, wmiEnumerationOptions))
            {
                ManagementObjectCollection hostInstanceCollection = wmiSearcher.Get();
                foreach (ManagementObject instance in hostInstanceCollection.Cast<ManagementObject>().Where(instance => instance["Name"].ToString().IndexOf(this.MachineName, StringComparison.OrdinalIgnoreCase) > 0))
                {
                    return instance;
                }
            }

            return null;
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
