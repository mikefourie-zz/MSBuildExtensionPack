//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="SendHandler.cs">(c) 2017 Mike Fourie and Contributors (https://github.com/mikefourie/MSBuildExtensionPack) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.BizTalk
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Management;
    using System.Text.RegularExpressions;
    using Microsoft.BizTalk.ExplorerOM;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CheckExists</i> (<b>Required: </b>HostName, AdapterName <b>Optional: </b>MachineName, DatabaseServer, Database<b>Output: </b>Exists)</para>
    /// <para><i>Create</i> (<b>Required: </b>HostName, AdapterName <b>Optional: </b>MachineName, Default, DatabaseServer, Database, CustomCfg, Force)</para>
    /// <para><i>Delete</i> (<b>Required: </b>HostName, AdapterName <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>Get</i> (<b>Optional: </b>HostName, AdapterName, MachineName, DatabaseServer, Database<b>Output: </b>SendHandlers)</para>
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
    ///         <!-- Create a SendHandler (note force is true)-->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkSendHandler TaskAction="Create" HostName="MSBEPTESTHOST" AdapterName="MQSeries" Force="true"/>
    ///         <!-- Check a SendHandler exists (it should) -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkSendHandler TaskAction="CheckExists" HostName="MSBEPTESTHOST" AdapterName="MQSeries">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkSendHandler>
    ///         <Message Text="BizTalkSendHandler  Exists: $(DoesExist) "/>
    ///         <!-- Get all Send Handlers -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkSendHandler TaskAction="Get" HostName="Biz">
    ///             <Output TaskParameter="SendHandlers" ItemName="SH"/>
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkSendHandler>
    ///         <Message Text="%(SH.Identity) - %(SH.AdapterName) - %(SH.CustomCfg)"/>
    ///         <!-- Delete a SendHandler -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkSendHandler TaskAction="Delete" HostName="MSBEPTESTHOST" AdapterName="MQSeries"/>
    ///         <!-- Check a SendHandler exists (it shouldn't) -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkSendHandler TaskAction="CheckExists" HostName="MSBEPTESTHOST" AdapterName="MQSeries">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkSendHandler>
    ///         <Message Text="BizTalkSendHandler  Exists: $(DoesExist) "/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class BizTalkSendHandler : BaseTask
    {
        private const string CheckExistsTaskAction = "CheckExists";
        private const string CreateTaskAction = "Create";
        private const string DeleteTaskAction = "Delete";
        private const string GetTaskAction = "Get";
        private const string WmiBizTalkNamespace = @"\root\MicrosoftBizTalkServer";
        private string database = "BizTalkMgmtDb";
        private BtsCatalogExplorer explorer;
        private ManagementObject sendHandler;

        /// <summary>
        /// Sets the DatabaseServer to connect to. Default is MachineName
        /// </summary>
        public string DatabaseServer { get; set; }

        /// <summary>
        /// Sets the Management Database to connect to. Default is BizTalkMgmtDb
        /// </summary>
        public string Database
        {
            get { return this.database; }
            set { this.database = value; }
        }

        /// <summary>
        /// Set to true to delete an existing Send Handler when Create is called.
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Sets the Host Name. For TaskAction="Get", a regular expression may be provided
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Sets the CustomCfg for the SendHandler. See <a href="http://msdn.microsoft.com/en-us/library/aa559911(v=BTS.20).aspx">Configuration Properties for Integrated BizTalk Adapters</a>
        /// </summary>
        public string CustomCfg { get; set; }

        /// <summary>
        /// Gets whether the Application exists
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Sets the SendHanlder as Default. Default is false.
        /// </summary>
        public bool Default { get; set; }

        /// <summary>
        /// Gets the list of Send Handlers. Identity is HostName. Metadata includes AdapterName, MgmtDbNameOverride, MgmtDbServerOverride, CustomCfg, Description, Caption.
        /// </summary>
        [Output]
        public ITaskItem[] SendHandlers { get; set; }

        /// <summary>
        /// Sets the AdapterName
        /// </summary>
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
                if (string.IsNullOrEmpty(this.HostName) && this.TaskAction != GetTaskAction)
                {
                    this.Log.LogError("HostName is required.");
                    return;
                }

                this.explorer.ConnectionString = string.Format(CultureInfo.CurrentCulture, "Server={0};Database={1};Integrated Security=SSPI;", this.DatabaseServer, this.Database);
                this.GetManagementScope(WmiBizTalkNamespace);
                this.LogTaskMessage(this.TaskAction != GetTaskAction ? string.Format(CultureInfo.CurrentCulture, "{0} {1} SendHandler for: {2} on {3}", this.TaskAction, this.AdapterName, this.HostName, this.MachineName) : string.Format(CultureInfo.CurrentCulture, "Get SendHandlers for Adaptor: {0} matching HostName: {1} on {2}", this.AdapterName, this.HostName, this.MachineName));

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
                    case GetTaskAction:
                        this.Get();
                        break;
                    default:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                        return;
                }
            }
        }

        private void Get()
        {
            string queryString = string.IsNullOrEmpty(this.AdapterName) ? "SELECT * FROM MSBTS_SendHandler2" : string.Format(CultureInfo.InvariantCulture, "SELECT * FROM MSBTS_SendHandler2 WHERE AdapterName = '{0}'", this.AdapterName);

            ObjectQuery query = new ObjectQuery(queryString);
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.Scope, query, null))
            {
                ManagementObjectCollection objects = searcher.Get();
                if (objects.Count > 0)
                {
                    this.SendHandlers = new ITaskItem[objects.Count];
                    int i = 0;
                    foreach (ManagementObject obj in objects)
                    {
                        if (string.IsNullOrEmpty(this.HostName))
                        {
                            ITaskItem sendhandlerItem = new TaskItem(obj["HostName"].ToString());
                            sendhandlerItem.SetMetadata("AdapterName", obj["AdapterName"].ToString());
                            sendhandlerItem.SetMetadata("MgmtDbNameOverride", obj["MgmtDbNameOverride"] == null ? string.Empty : obj["MgmtDbNameOverride"].ToString());
                            sendhandlerItem.SetMetadata("MgmtDbServerOverride", obj["MgmtDbServerOverride"] == null ? string.Empty : obj["MgmtDbServerOverride"].ToString());
                            sendhandlerItem.SetMetadata("CustomCfg", obj["CustomCfg"] == null ? string.Empty : obj["CustomCfg"].ToString());
                            sendhandlerItem.SetMetadata("Description", obj["Description"] == null ? string.Empty : obj["Description"].ToString());
                            sendhandlerItem.SetMetadata("Caption", obj["Caption"] == null ? string.Empty : obj["Caption"].ToString());
                            this.SendHandlers[i] = sendhandlerItem;

                            i++;
                        }
                        else
                        {
                            Regex filter = new Regex(this.HostName, RegexOptions.Compiled);
                            if (filter.IsMatch(obj["HostName"].ToString()))
                            {
                                ITaskItem sendhandlerItem = new TaskItem(obj["HostName"].ToString());
                                sendhandlerItem.SetMetadata("AdapterName", obj["AdapterName"].ToString());
                                sendhandlerItem.SetMetadata("MgmtDbNameOverride", obj["MgmtDbNameOverride"] == null ? string.Empty : obj["MgmtDbNameOverride"].ToString());
                                sendhandlerItem.SetMetadata("MgmtDbServerOverride", obj["MgmtDbServerOverride"] == null ? string.Empty : obj["MgmtDbServerOverride"].ToString());
                                sendhandlerItem.SetMetadata("CustomCfg", obj["CustomCfg"] == null ? string.Empty : obj["CustomCfg"].ToString());
                                sendhandlerItem.SetMetadata("Description", obj["Description"] == null ? string.Empty : obj["Description"].ToString());
                                sendhandlerItem.SetMetadata("Caption", obj["Caption"] == null ? string.Empty : obj["Caption"].ToString());
                                this.SendHandlers[i] = sendhandlerItem;
                                i++;
                            }
                        }
                    }
                }
            }
        }

        private bool CheckExists()
        {
            string queryString = string.Format(CultureInfo.InvariantCulture, "SELECT * FROM MSBTS_SendHandler2 WHERE AdapterName = '{0}'", this.AdapterName);
            ObjectQuery query = new ObjectQuery(queryString);
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.Scope, query, null))
            {
                ManagementObjectCollection objects = searcher.Get();
                if (objects.Count > 0)
                {
                    foreach (ManagementObject obj in objects.Cast<ManagementObject>().Where(obj => string.Compare(obj["HostName"].ToString(), this.HostName, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        this.Exists = true;
                        this.sendHandler = obj;
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
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "SendHandler: {0} already exists for: {1}. Set Force to true to delete the SendHandler.", this.AdapterName, this.HostName));
                    return;
                }
            }

            PutOptions options = new PutOptions { Type = PutType.CreateOnly };
            using (ManagementClass instance = new ManagementClass(this.Scope, new ManagementPath("MSBTS_SendHandler2"), null))
            {
                ManagementObject btsHostSetting = instance.CreateInstance();
                if (btsHostSetting == null)
                {
                    Log.LogError("There was a failure creating the MSBTS_SendHandler2 instance");
                    return;
                }

                btsHostSetting["HostName"] = this.HostName;
                btsHostSetting["AdapterName"] = this.AdapterName;
                btsHostSetting["IsDefault"] = this.Default;
                btsHostSetting["CustomCfg"] = this.CustomCfg;
                btsHostSetting["MgmtDbServerOverride"] = this.DatabaseServer;

                btsHostSetting.Put(options);
                this.explorer.SaveChanges();
            }
        }

        private void Delete()
        {
            if (!this.CheckExists())
            {
                return;
            }

            string queryString = string.Format(CultureInfo.InvariantCulture, "SELECT * FROM MSBTS_SendHandler2 WHERE AdapterName = '{0}'", this.AdapterName);
            ObjectQuery query = new ObjectQuery(queryString);
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.Scope, query, null))
            {
                ManagementObjectCollection objects = searcher.Get();
                ManagementObject newDefault = null;
                try
                {
                    if (objects.Count > 0)
                    {
                        newDefault = objects.Cast<ManagementObject>().FirstOrDefault(obj => !Convert.ToBoolean(obj["IsDefault"], CultureInfo.InvariantCulture));
                        foreach (ManagementObject obj in objects)
                        {
                            if (string.Compare(obj["HostName"].ToString(), this.HostName, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                this.sendHandler = obj;
                                if (Convert.ToBoolean(this.sendHandler["IsDefault"], CultureInfo.InvariantCulture))
                                {
                                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Changing Default Send Handler to: {0}", newDefault["HostName"]));
                                    newDefault["IsDefault"] = true;
                                    newDefault.Put(new PutOptions { Type = PutType.UpdateOnly });
                                    this.explorer.SaveChanges();
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        this.sendHandler.Delete();
                    }
                }
                finally
                {
                    if (newDefault != null)
                    {
                        newDefault.Dispose();
                    }
                }
            }
        }
    }
}