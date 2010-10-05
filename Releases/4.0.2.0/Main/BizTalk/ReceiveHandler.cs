//-----------------------------------------------------------------------
// <copyright file="ReceiveHandler.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.BizTalk
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Management;
    using Microsoft.BizTalk.ExplorerOM;
    using Microsoft.Build.Framework;
    using OM = Microsoft.BizTalk.ExplorerOM;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CheckExists</i> (<b>Required: </b>HostName, AdapterName <b>Optional: </b>MachineName, DatabaseServer, Database<b>Output: </b>Exists)</para>
    /// <para><i>Create</i> (<b>Required: </b>HostName, AdapterName <b>Optional: </b>MachineName, DatabaseServer, Database, CustomCfg, Force)</para>
    /// <para><i>Delete</i> (<b>Required: </b>HostName, AdapterName <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
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
    ///         <!-- Create a ReceiveHandler (note force is true)-->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkReceiveHandler TaskAction="Create" HostName="MSBEPTESTHOST" AdapterName="MQSeries" Force="true"/>
    ///         <!-- Check a ReceiveHandler exists (it should) -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkReceiveHandler TaskAction="CheckExists" HostName="MSBEPTESTHOST" AdapterName="MQSeries">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkReceiveHandler>
    ///         <Message Text="BizTalkReceiveHandler  Exists: $(DoesExist) "/>
    ///         <!-- Delete a ReceiveHandler -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkReceiveHandler TaskAction="Delete" HostName="MSBEPTESTHOST" AdapterName="MQSeries"/>
    ///         <!-- Check a ReceiveHandler exists (it shouldn't) -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkReceiveHandler TaskAction="CheckExists" HostName="MSBEPTESTHOST" AdapterName="MQSeries">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkReceiveHandler>
    ///         <Message Text="BizTalkReceiveHandler  Exists: $(DoesExist) "/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.2.0/html/d68a23c3-2b99-3d68-ae8d-cbbfdee0b984.htm")]
    public class BizTalkReceiveHandler : BaseTask
    {
        private const string CheckExistsTaskAction = "CheckExists";
        private const string CreateTaskAction = "Create";
        private const string DeleteTaskAction = "Delete";
        private const string WmiBizTalkNamespace = @"\root\MicrosoftBizTalkServer";
        private string database = "BizTalkMgmtDb";
        private BtsCatalogExplorer explorer;
        private ManagementObject receiveHandler;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(CheckExistsTaskAction)]
        [DropdownValue(CreateTaskAction)]
        [DropdownValue(DeleteTaskAction)]
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
        public string DatabaseServer { get; set; }
 
        /// <summary>
        /// Sets the Management Database to connect to. Default is BizTalkMgmtDb
        /// </summary>
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(DeleteTaskAction, false)]
        public string Database
        {
            get { return this.database; }
            set { this.database = value; }
        }

        /// <summary>
        /// Set to true to delete an existing Receive Handler when Create is called.
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public bool Force { get; set; }

        /// <summary>
        /// Sets the Host Name.
        /// </summary>
        [TaskAction(CheckExistsTaskAction, true)]
        [TaskAction(CreateTaskAction, true)]
        [TaskAction(DeleteTaskAction, true)]
        [Required]
        public string HostName { get; set; }

        /// <summary>
        /// Sets the CustomCfg for the ReceiveHandler. See <a href="http://msdn.microsoft.com/en-us/library/aa559911(v=BTS.20).aspx">Configuration Properties for Integrated BizTalk Adapters</a>
        /// </summary>
        public string CustomCfg { get; set; }

        /// <summary>
        /// Gets whether the Application exists
        /// </summary>
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(DeleteTaskAction, false)]
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Sets the AdapterName
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(DeleteTaskAction, false)]
        public string AdapterName { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (string.IsNullOrEmpty(this.DatabaseServer))
            {
                this.DatabaseServer = this.MachineName;
            }

            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Connecting to BtsCatalogExplorer: Server: {0}. Database: {1}", this.DatabaseServer, this.Database));
            using (this.explorer = new BtsCatalogExplorer())
            {
                this.explorer.ConnectionString = string.Format(CultureInfo.CurrentCulture, "Server={0};Database={1};Integrated Security=SSPI;", this.DatabaseServer, this.Database);
                this.GetManagementScope(WmiBizTalkNamespace);
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "{0} {1} ReceiveHandler for: {2} on {3}", this.TaskAction, this.AdapterName, this.HostName, this.MachineName));

                switch (this.TaskAction)
                {
                    case CreateTaskAction:
                        this.Create();
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
            string queryString = string.Format(CultureInfo.InvariantCulture, "SELECT * FROM MSBTS_ReceiveHandler WHERE AdapterName = '{0}'", this.AdapterName);
            ObjectQuery query = new ObjectQuery(queryString);
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.Scope, query, null))
            {
                ManagementObjectCollection objects = searcher.Get();
                if (objects.Count > 0)
                {
                    foreach (ManagementObject obj in objects.Cast<ManagementObject>().Where(obj => string.Compare(obj["HostName"].ToString(), this.HostName, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        this.Exists = true;
                        this.receiveHandler = obj;
                        return true;
                    }
                }
            }

            return false;
        }

        private void Create()
        {
            if (this.CheckExists())
            {
                if (this.Force)
                {
                    this.Delete();
                }
                else
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "ReceiveHandler: {0} already exists for: {1}. Set Force to true to delete the ReceiveHandler.", this.AdapterName, this.HostName));
                    return;
                }
            }

            PutOptions options = new PutOptions { Type = PutType.CreateOnly };
            using (ManagementClass instance = new ManagementClass(this.Scope, new ManagementPath("MSBTS_ReceiveHandler"), null))
            {
                ManagementObject btsHostSetting = instance.CreateInstance();
                if (btsHostSetting == null)
                {
                    Log.LogError("There was a failure creating the MSBTS_ReceiveHandler instance");
                    return;
                }

                btsHostSetting["HostName"] = this.HostName;
                btsHostSetting["AdapterName"] = this.AdapterName;
                btsHostSetting["CustomCfg"] = this.CustomCfg;
                btsHostSetting["MgmtDbServerOverride"] = this.DatabaseServer;

                btsHostSetting.Put(options);
                this.explorer.SaveChanges();
            }
        }

        private void Delete()
        {
            if (this.CheckExists())
            {
                this.receiveHandler.Delete();
            }
        }
    }
}