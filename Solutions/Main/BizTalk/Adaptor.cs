//-----------------------------------------------------------------------
// <copyright file="Adaptor.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
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
    /// <para><i>CheckExists</i> (<b>Required: </b>AdaptorName <b>Optional: </b>MachineName, DatabaseServer, Database <b>Output: </b>Exists, Comment)</para>
    /// <para><i>Create</i> (<b>Required: </b>AdaptorName, MgmtCLSID<b>Optional: </b>MachineName, DatabaseServer, Database, Comment, Constraints)</para>
    /// <para><i>Delete</i> (<b>Required: </b>AdaptorName <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><b>Remote Execution Support:</b> Yes</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///   </PropertyGroup>
    ///   <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- Check an Adaptor Exists -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkAdaptor TaskAction="CheckExists" AdaptorName="WCF-SQL">
    ///             <Output TaskParameter="Exists" PropertyName="AdaptorExists" />
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkAdaptor>
    ///         <Message Text="WCF-SQL Exists: $(AdaptorExists)"/>
    ///         <!-- Delete an Adaptor -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkAdaptor TaskAction="Delete" AdaptorName="WCF-SQL"/>
    ///         <!-- Check an Adaptor Exists -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkAdaptor TaskAction="CheckExists" AdaptorName="WCF-SQL">
    ///             <Output TaskParameter="Exists" PropertyName="AdaptorExists" />
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkAdaptor>
    ///         <Message Text="WCF-SQL Exists: $(AdaptorExists)"/>
    ///         <!-- Create an Adaptor -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkAdaptor TaskAction="Create" AdaptorName="WCF-SQL" MgmtCLSID="{59b35d03-6a06-4734-a249-ef561254ecf7}" Comment="WCF-SQL adapter" Constraints="15369"/>
    ///         <!-- Check an Adaptor Exists -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkAdaptor TaskAction="CheckExists" AdaptorName="WCF-SQL">
    ///             <Output TaskParameter="Exists" PropertyName="AdaptorExists" />
    ///             <Output TaskParameter="Comment" PropertyName="AdaptorComment" />
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkAdaptor>
    ///         <Message Text="WCF-SQL Exists: $(AdaptorExists)"/>
    ///         <Message Text="WCF-SQL Comment: $(AdaptorComment)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class BizTalkAdaptor : BaseTask
    {
        private const string CheckExistsTaskAction = "CheckExists";
        private const string CreateTaskAction = "Create";
        private const string DeleteTaskAction = "Delete";
        private const string WmiBizTalkNamespace = @"\root\MicrosoftBizTalkServer";
        private string database = "BizTalkMgmtDb";
        private BtsCatalogExplorer explorer;
        private ManagementObject adaptor;

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
        /// Sets the Adaptor Name.
        /// </summary>
        [Required]
        public string AdaptorName { get; set; }

        /// <summary>
        /// Sets the Adaptor comment.
        /// </summary>
        [Output]
        public string Comment { get; set; }

        /// <summary>
        /// Sets the MgmtCLSID guid
        /// </summary>
        public string MgmtCLSID { get; set; }

        /// <summary>
        /// Sets the Constraints
        /// </summary>
        public int Constraints { get; set; }

        /// <summary>
        /// Gets whether the Adaptor exists
        /// </summary>
        [Output]
        public bool Exists { get; set; }

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
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "{0} Adaptor: {1} on: {2}", this.TaskAction, this.AdaptorName, this.MachineName));

                switch (this.TaskAction)
                {
                    case CreateTaskAction:
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
            string queryString = string.Format(CultureInfo.InvariantCulture, "SELECT * FROM MSBTS_AdapterSetting WHERE Name = '{0}'", this.AdaptorName);
            ObjectQuery query = new ObjectQuery(queryString);
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.Scope, query, null))
            {
                ManagementObjectCollection objects = searcher.Get();
                if (objects.Count > 0)
                {
                    this.Exists = true;
                    foreach (ManagementObject obj in objects)
                    {
                        this.adaptor = obj;
                        this.Comment = this.adaptor["Comment"].ToString();
                        return true;
                    }
                }
            }

            return false;
        }

        private void CreateOrUpdate()
        {
            PutOptions options = new PutOptions { Type = PutType.UpdateOrCreate };
            using (ManagementClass instance = new ManagementClass(this.Scope, new ManagementPath("MSBTS_AdapterSetting"), null))
            {
                ManagementObject btsHostSetting = instance.CreateInstance();
                if (btsHostSetting == null)
                {
                    Log.LogError("There was a failure creating the MSBTS_AdapterSetting instance");
                    return;
                }

                btsHostSetting["Name"] = this.AdaptorName;
                btsHostSetting["Comment"] = this.Comment ?? string.Empty;
                btsHostSetting["MgmtCLSID"] = this.MgmtCLSID;

                if (this.Constraints > 0)
                {
                    btsHostSetting["Constraints"] = Convert.ToUInt32(this.Constraints);
                }

                btsHostSetting.Put(options);
                this.explorer.SaveChanges();
            }
        }

        private void Delete()
        {
            if (this.CheckExists())
            {
                this.adaptor.Delete();
            }
        }
    }
}