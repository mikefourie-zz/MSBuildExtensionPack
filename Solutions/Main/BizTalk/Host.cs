//-----------------------------------------------------------------------
// <copyright file="Host.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.BizTalk
{
    using System;
    using System.Globalization;
    using System.Management;
    using Microsoft.BizTalk.ExplorerOM;
    using Microsoft.Build.Framework;
    using OM = Microsoft.BizTalk.ExplorerOM;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CheckExists</i> (<b>Required: </b>HostName <b>Optional: </b>MachineName, DatabaseServer, Database <b>Output: </b>Exists)</para>
    /// <para><i>Create</i> (<b>Required: </b>HostName, WindowsGroup <b>Optional: </b>MachineName, DatabaseServer, Database, HostType, Use32BitHostOnly, Trusted, Tracking, Default, AdditionalHostSettings)</para>
    /// <para><i>Delete</i> (<b>Required: </b>HostName <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>Update</i> (<b>Required: </b>HostName, WindowsGroup <b>Optional: </b>MachineName, Database, DatabaseServer, HostType, Use32BitHostOnly, Trusted, Tracking, Default, AdditionalHostSettings)</para>
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
    ///         <!-- Create a Host -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkHost TaskAction="Create" HostName="MSBEPTESTHOST" Tracking="true" WindowsGroup="$(ComputerName)\BizTalk Application Users" HostType="InProcess" Use32BitHostOnly="false"/>
    ///         <!-- Update a Host -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkHost TaskAction="Update" HostName="MSBEPTESTHOST" Tracking="false" WindowsGroup="$(ComputerName)\BizTalk Application Users" HostType="InProcess" Use32BitHostOnly="false" AdditionalHostSettings="MessageDeliverySampleSpaceSize=123;ProcessMemoryThreshold=31"/>
    ///         <!-- Delete a Host -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkHost TaskAction="Delete" HostName="MSBEPTESTHOST"/>   
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.3.0/html/f475a984-7820-8a9a-2a35-d8c3d9aa3f40.htm")]
    public class BizTalkHost : BaseTask
    {
        private const string CheckExistsTaskAction = "CheckExists";
        private const string CreateTaskAction = "Create";
        private const string DeleteTaskAction = "Delete";
        private const string UpdateTaskAction = "Update";
        private const string WmiBizTalkNamespace = @"\root\MicrosoftBizTalkServer";
        private string database = "BizTalkMgmtDb";
        private BtsCatalogExplorer explorer;
        private ManagementObject host;
        private BizTalkHostType hostType = BizTalkHostType.InProcess;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(CheckExistsTaskAction)]
        [DropdownValue(CreateTaskAction)]
        [DropdownValue(DeleteTaskAction)]
        [DropdownValue(UpdateTaskAction)]
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
        [TaskAction(UpdateTaskAction, false)]
        public override string MachineName
        {
            get { return base.MachineName; }
            set { base.MachineName = value; }
        }

        /// <summary>
        /// Sets the DatabaseServer to connect to. Default is MachineName
        /// </summary>
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(DeleteTaskAction, false)]
        [TaskAction(UpdateTaskAction, false)]
        public string DatabaseServer { get; set; }
 
        /// <summary>
        /// Sets the Management Database to connect to. Default is BizTalkMgmtDb
        /// </summary>
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(DeleteTaskAction, false)]
        [TaskAction(UpdateTaskAction, false)]
        public string Database
        {
            get { return this.database; }
            set { this.database = value; }
        }

        /// <summary>
        /// Sets the Host Name. The following characters are not permitted: `~!@#$%^&amp;*()+=[]{}|\/;:\""'&lt;&gt;,.?-&lt;space&gt;
        /// </summary>
        [TaskAction(CheckExistsTaskAction, true)]
        [TaskAction(CreateTaskAction, true)]
        [TaskAction(DeleteTaskAction, true)]
        [TaskAction(UpdateTaskAction, true)]
        [Required]
        public string HostName { get; set; }

        /// <summary>
        /// Sets the Host Type. Supports: InProcess, Isolated. Default is InProcess.
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(UpdateTaskAction, false)]
        public string HostType
        { 
            get { return this.hostType.ToString(); }
            set { this.hostType = (BizTalkHostType)Enum.Parse(typeof(BizTalkHostType), value); }
        }

        /// <summary>
        /// Sets the Host to 32BitOnly. Default is false.
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(UpdateTaskAction, false)]
        public bool Use32BitHostOnly { get; set; }

        /// <summary>
        /// An optional semi-colon delimited list of name value pairs to set additional Host settings, e.g. MessagePublishSampleSpaceSize=1;MessagePublishOverdriveFactor=100. For available settings, see: http://msdn.microsoft.com/en-us/library/aa560307(BTS.10).aspx
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(UpdateTaskAction, false)]
        public string AdditionalHostSettings { get; set; }

        /// <summary>
        /// Gets whether the Host exists
        /// </summary>
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(DeleteTaskAction, false)]
        [TaskAction(UpdateTaskAction, false)]
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Sets the host as Trusted. Default is false.
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(UpdateTaskAction, false)]
        public bool Trusted { get; set; }

        /// <summary>
        /// Sets the host as Tracking. Default is false.
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(UpdateTaskAction, false)]
        public bool Tracking { get; set; }

        /// <summary>
        /// Sets the host as Default. Default is false.
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(UpdateTaskAction, false)]
        public bool Default { get; set; }
        
        /// <summary>
        /// Set the windows group. This may be in the form domain\group
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(UpdateTaskAction, false)]
        public string WindowsGroup { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (!BizTalkHelper.IsValidName(this.HostName, @"[`~!@#\$%\^&\*\(\)\+=\[\]\{}\|\/;:\""'<>,\.\?-]|\s"))
            {
                Log.LogError(@"The HostName contains an invalid character. The following characters are not permitted: `~!@#$%^&*()+=[]{}|\/;:\""'<>,.?-<space>");
                return;
            }

            if (string.IsNullOrEmpty(this.DatabaseServer))
            {
                this.DatabaseServer = this.MachineName;
            }

            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Connecting to BtsCatalogExplorer: Server: {0}. Database: {1}", this.DatabaseServer, this.Database));
            using (this.explorer = new BtsCatalogExplorer())
            {
                this.explorer.ConnectionString = string.Format(CultureInfo.CurrentCulture, "Server={0};Database={1};Integrated Security=SSPI;", this.DatabaseServer, this.Database);
                this.GetManagementScope(WmiBizTalkNamespace);
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "{0} Host: {1} on: {2}", this.TaskAction, this.HostName, this.MachineName));

                switch (this.TaskAction)
                {
                    case CreateTaskAction:
                    case UpdateTaskAction:
                        this.CreateOrUpdate();
                        break;
                    case CheckExistsTaskAction:
                        this.CheckExists();
                        break;
                    case DeleteTaskAction:
                        this.Delete();
                        break;
                    default:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                        return;
                }
            }
        }

        private bool CheckExists()
        {
            string queryString = string.Format(CultureInfo.InvariantCulture, "SELECT * FROM MSBTS_HostSetting WHERE Name = '{0}'", this.HostName);
            ObjectQuery query = new ObjectQuery(queryString);
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.Scope, query, null))
            {
                ManagementObjectCollection objects = searcher.Get();
                if (objects.Count > 0)
                {
                    this.Exists = true;
                    foreach (ManagementObject obj in objects)
                    {
                        this.host = obj;
                        return true;
                    }
                }
            }

            return false;
        }

        private void CreateOrUpdate()
        {
            if (string.IsNullOrEmpty(this.WindowsGroup))
            {
                Log.LogError("WindowsGroup is required.");
                return;
            }
            
            PutOptions options = new PutOptions { Type = PutType.UpdateOrCreate };
            using (ManagementClass instance = new ManagementClass(this.Scope, new ManagementPath("MSBTS_HostSetting"), null))
            {
                ManagementObject btsHostSetting = instance.CreateInstance();
                if (btsHostSetting == null)
                {
                    Log.LogError("There was a failure creating the MSBTS_HostSetting instance");
                    return;
                }

                btsHostSetting["Name"] = this.HostName;
                btsHostSetting["HostType"] = this.hostType;
                btsHostSetting["NTGroupName"] = this.WindowsGroup;
                btsHostSetting["AuthTrusted"] = this.Trusted;
                btsHostSetting["MgmtDbServerOverride"] = this.DatabaseServer;
                btsHostSetting["IsHost32BitOnly"] = this.Use32BitHostOnly;

                if (this.hostType == BizTalkHostType.InProcess)
                {
                    btsHostSetting.SetPropertyValue("HostTracking", this.Tracking);
                    btsHostSetting.SetPropertyValue("IsDefault", this.Default);
                }

                if (!string.IsNullOrEmpty(this.AdditionalHostSettings))
                {
                    string[] additionalproperties = this.AdditionalHostSettings.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in additionalproperties)
                    {
                        string[] property = s.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        btsHostSetting[property[0]] = property[1];
                    }
                }

                btsHostSetting.Put(options);
                this.explorer.SaveChanges();
            }
        }

        private void Delete()
        {
            if (this.CheckExists())
            {
                this.host.Delete();
            }
        }
    }
}