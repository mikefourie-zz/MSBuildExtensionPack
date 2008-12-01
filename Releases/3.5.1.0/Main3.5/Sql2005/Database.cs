//-----------------------------------------------------------------------
// <copyright file="Database.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Sql2005
{
    using System;
    using System.Globalization;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Smo;
    using SMO = Microsoft.SqlServer.Management.Smo;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Backup</i> (<b>Required: </b>DatabaseItem, DataFilePath <b>Optional: </b>BackupAction, Incremental, NotificationInterval, NoPooling)</para>
    /// <para><i>CheckExists</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling <b>Output:</b> Exists)</para>
    /// <para><i>Create</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling)</para>
    /// <para><i>Delete</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling)</para>
    /// <para><i>DeleteBackupHistory</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling)</para>
    /// <para><i>GetConnectionCount</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling)</para>
    /// <para><i>GetInfo</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling)</para>
    /// <para><i>Rename</i> (<b>Required: </b>DatabaseItem (NewName metadata) <b>Optional: </b>NoPooling)</para>
    /// <para><i>Restore</i> (<b>Required: </b>DatabaseItem, DataFilePath <b>Optional: </b>RestoreAction, Incremental, NotificationInterval, NoPooling)</para>
    /// <para><i>Script</i> (<b>Required: </b>DatabaseItem, OutputFilePath <b>Optional: </b>NoPooling)</para>
    /// <para><i>SetOffline</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling)</para>
    /// <para><i>SetOnline</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling)</para>
    /// <para><i>VerifyBackup</i> (<b>Required: </b>DataFilePath <b>Optional: </b>NoPooling)</para>
    /// <para><b>Remote Execution Support:</b> Yes</para>
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
    ///         <ItemGroup>
    ///             <Database Include="ADatabase">
    ///                 <NewName>ADatabase2</NewName>
    ///             </Database>
    ///             <Database2 Include="ADatabase2">
    ///                 <NewName>ADatabase</NewName>
    ///             </Database2>
    ///         </ItemGroup>
    ///         <!-- Get information on a database -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="GetInfo" DatabaseItem="ADatabase" MachineName="MyServer\SQL2005Instance">
    ///             <Output TaskParameter="Information" ItemName="AllInfo"/>
    ///         </MSBuild.ExtensionPack.Sql2005.Database>
    ///         <!-- All the database information properties are available as metadata on the Infomation item -->
    ///         <Message Text="SpaceAvailable: %(AllInfo.SpaceAvailable)"/>
    ///         <!-- Backup a database -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="Backup" DatabaseItem="ADatabase" MachineName="MyServer\SQL2005Instance" DataFilePath="c:\a\ADatabase2005.bak"/>
    ///         <!-- Verify a database backup -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="VerifyBackup" DataFilePath="c:\a\ADatabase2005.bak" MachineName="MyServer\SQL2005Instance"/>
    ///         <!-- Restore a database -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="Restore" DatabaseItem="ADatabase" MachineName="MyServer\SQL2005Instance" DataFilePath="c:\a\ADatabase2005.bak"/>
    ///         <!-- Create a database -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="Create" DatabaseItem="ADatabase2" MachineName="MyServer\SQL2005Instance"/>
    ///         <!-- Create the database again, using Force to delete the existing database -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="Create" DatabaseItem="ADatabase2" Force="true" MachineName="MyServer\SQL2005Instance"/>
    ///         <!-- Check whether a database exists -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="CheckExists" DatabaseItem="ADatabase2" MachineName="MyServer\SQL2005Instance">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Sql2005.Database>
    ///         <Message Text="Database Exists: $(DoesExist)"/>
    ///         <!-- Delete a database -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="Delete" DatabaseItem="ADatabase2" MachineName="MyServer\SQL2005Instance"/>
    ///         <!-- Check whether a database exists -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="CheckExists" DatabaseItem="ADatabase2" MachineName="MyServer\SQL2005Instance">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Sql2005.Database>
    ///         <Message Text="Database Exists: $(DoesExist)"/>
    ///         <!-- Get the number of active connections to a database -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="GetConnectionCount" DatabaseItem="ADatabase" MachineName="MyServer\SQL2005Instance">
    ///             <Output TaskParameter="ConnectionCount" PropertyName="Count"/>
    ///         </MSBuild.ExtensionPack.Sql2005.Database>
    ///         <Message Text="Database ConnectionCount: $(Count)"/>
    ///         <!-- Delete the backup history for a database -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="DeleteBackupHistory" DatabaseItem="ADatabase" MachineName="MyServer\SQL2005Instance"/>
    ///         <!-- Set a database offline -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="SetOffline" DatabaseItem="ADatabase" MachineName="MyServer\SQL2005Instance"/>
    ///         <!-- Set a database online -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="SetOnline" DatabaseItem="ADatabase" MachineName="MyServer\SQL2005Instance"/>
    ///         <!-- Rename a database -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="Rename" DatabaseItem="@(Database)" MachineName="MyServer\SQL2005Instance"/>
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="Rename" DatabaseItem="@(Database2)" MachineName="MyServer\SQL2005Instance"/>
    ///         <!-- Script a database to file -->
    ///         <MSBuild.ExtensionPack.Sql2005.Database TaskAction="Script" DatabaseItem="ADatabase" OutputFilePath="c:\ADatabaseScript2005.sql" MachineName="MyServer\SQL2005Instance"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.1.0/html/2b1ebce5-3d34-c41b-5fcf-a942f14c9b51.htm")]
    public class Database : BaseTask
    {
        private const string BackupTaskAction = "Backup";
        private const string CheckExistsTaskAction = "CheckExists";
        private const string CreateTaskAction = "Create";
        private const string DeleteTaskAction = "Delete";
        private const string DeleteBackupHistoryTaskAction = "DeleteBackupHistory";
        private const string GetConnectionCountTaskAction = "GetConnectionCount";
        private const string GetInfoTaskAction = "GetInfo";
        private const string RenameTaskAction = "Rename";
        private const string RestoreTaskAction = "Restore";
        private const string ScriptTaskAction = "Script";
        private const string SetOfflineTaskAction = "SetOffline";
        private const string SetOnlineTaskAction = "SetOnline";
        private const string VerifyBackupTaskAction = "VerifyBackup";

        private bool trustedConnection;
        private SMO.Server sqlServer;
        private BackupActionType backupAction = BackupActionType.Database;
        private RestoreActionType restoreAction = RestoreActionType.Database;
        private int notificationInterval = 10;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(BackupTaskAction)]
        [DropdownValue(CheckExistsTaskAction)]
        [DropdownValue(CreateTaskAction)]
        [DropdownValue(DeleteTaskAction)]
        [DropdownValue(DeleteBackupHistoryTaskAction)]
        [DropdownValue(GetConnectionCountTaskAction)]
        [DropdownValue(GetInfoTaskAction)]
        [DropdownValue(RenameTaskAction)]
        [DropdownValue(RestoreTaskAction)]
        [DropdownValue(ScriptTaskAction)]
        [DropdownValue(SetOfflineTaskAction)]
        [DropdownValue(SetOnlineTaskAction)]
        [DropdownValue(VerifyBackupTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Set to true to create a NonPooledConnection to the server. Default is false.
        /// </summary>
        [TaskAction(BackupTaskAction, false)]
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(DeleteTaskAction, false)]
        [TaskAction(DeleteBackupHistoryTaskAction, false)]
        [TaskAction(GetConnectionCountTaskAction, false)]
        [TaskAction(GetInfoTaskAction, false)]
        [TaskAction(RenameTaskAction, false)]
        [TaskAction(RestoreTaskAction, false)]
        [TaskAction(ScriptTaskAction, false)]
        [TaskAction(SetOfflineTaskAction, false)]
        [TaskAction(SetOnlineTaskAction, false)]
        [TaskAction(VerifyBackupTaskAction, false)]
        public bool NoPooling { get; set; }

        /// <summary>
        /// Set to true to restore a database to a new location
        /// </summary>
        public bool ReplaceDatabase { get; set; }

        /// <summary>
        /// Set to true to perform an Incremental backup. Default is false.
        /// </summary>
        [TaskAction(BackupTaskAction, false)]
        [TaskAction(RestoreTaskAction, false)]
        public bool Incremental { get; set; }

        /// <summary>
        /// Set to true to force the creation of a database if it already exists.
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Sets the database name
        /// </summary>
        [TaskAction(BackupTaskAction, true)]
        [TaskAction(CheckExistsTaskAction, true)]
        [TaskAction(CreateTaskAction, true)]
        [TaskAction(DeleteTaskAction, true)]
        [TaskAction(DeleteBackupHistoryTaskAction, true)]
        [TaskAction(GetConnectionCountTaskAction, true)]
        [TaskAction(GetInfoTaskAction, true)]
        [TaskAction(RenameTaskAction, true)]
        [TaskAction(RestoreTaskAction, true)]
        [TaskAction(ScriptTaskAction, true)]
        [TaskAction(SetOfflineTaskAction, true)]
        [TaskAction(SetOnlineTaskAction, true)]
        [TaskAction(VerifyBackupTaskAction, true)]
        public ITaskItem DatabaseItem { get; set; }

        /// <summary>
        /// Sets the Log Name
        /// </summary>
        public string LogName { get; set; }

        /// <summary>
        /// Sets the type of backup action to perform. Supports Database, Files and Log. Default is Database
        /// </summary>
        [TaskAction(BackupTaskAction, false)]
        public string BackupAction
        {
            get { return this.backupAction.ToString(); }
            set { this.backupAction = (BackupActionType)Enum.Parse(typeof(BackupActionType), value); }
        }

        /// <summary>
        /// Sets the type of restore action to perform. Supports Database, Files, Log, OnlineFiles, OnlinePage. Default is Database
        /// </summary>
        [TaskAction(RestoreTaskAction, false)]
        public string RestoreAction
        {
            get { return this.restoreAction.ToString(); }
            set { this.restoreAction = (RestoreActionType)Enum.Parse(typeof(RestoreActionType), value); }
        }

        /// <summary>
        /// Sets the PercentCompleteNotification interval. Defaults to 10.
        /// </summary>
        [TaskAction(BackupTaskAction, false)]
        [TaskAction(RestoreTaskAction, false)]
        public int NotificationInterval
        {
            get { return this.notificationInterval; }
            set { this.notificationInterval = value; }
        }

        /// <summary>
        /// Sets the DataFilePath.
        /// </summary>
        [TaskAction(BackupTaskAction, true)]
        [TaskAction(RestoreTaskAction, true)]
        public ITaskItem DataFilePath { get; set; }

        /// <summary>
        /// Sets the LogFilePath.
        /// </summary>
        public ITaskItem LogFilePath { get; set; }

        /// <summary>
        /// Sets the OutputFilePath.
        /// </summary>
        [TaskAction(ScriptTaskAction, true)]
        public ITaskItem OutputFilePath { get; set; }

        /// <summary>
        /// Gets whether the database exists
        /// </summary>
        [Output]
        [TaskAction(CheckExistsTaskAction, false)]
        public bool Exists { get; set; }

        /// <summary>
        /// Gets the Information TaskItem. Each available property is added as metadata.
        /// </summary>
        [Output]
        public ITaskItem Information { get; set; }

        /// <summary>
        /// Gets the number of connections the database has open
        /// </summary>
        [Output]
        public int ConnectionCount { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (string.IsNullOrEmpty(this.UserName) || string.IsNullOrEmpty(this.UserPassword))
            {
                this.LogTaskMessage(MessageImportance.Low, "Using a Trusted Connection");
                this.trustedConnection = true;
            }

            ServerConnection con = new ServerConnection { LoginSecure = this.trustedConnection, ServerInstance = this.MachineName, NonPooledConnection = this.NoPooling };
            if (!string.IsNullOrEmpty(this.UserName))
            {
                con.Login = this.UserName;
            }

            if (!string.IsNullOrEmpty(this.UserPassword))
            {
                con.Password = this.UserPassword;
            }

            this.sqlServer = new SMO.Server(con);

            switch (this.TaskAction)
            {
                case "GetInfo":
                    this.GetInfo();
                    break;
                case "SetOffline":
                    this.SetOffline();
                    break;
                case "SetOnline":
                    this.SetOnline();
                    break;
                case "GetConnectionCount":
                    this.GetConnectionCount();
                    break;
                case "Backup":
                    this.Backup();
                    break;
                case "Restore":
                    this.Restore();
                    break;
                case "Delete":
                    this.Delete();
                    break;
                case "Script":
                    this.Script();
                    break;
                case "Rename":
                    this.Rename();
                    break;
                case "Create":
                    this.Create();
                    break;
                case "DeleteBackupHistory":
                    this.DeleteBackupHistory();
                    break;
                case "CheckExists":
                    this.CheckExists();
                    break;
                case "VerifyBackup":
                    this.VerifyBackup();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }

            // Release the connection if we are not using pooling.
            if (this.NoPooling)
            {
                this.sqlServer.ConnectionContext.Disconnect();
            }
        }

        private void Script()
        {
            if (!this.VerifyDatabase())
            {
                return;
            }

            if (this.OutputFilePath == null)
            {
                this.Log.LogError("OutputFilePath is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Scripting Database: {0} to: {1}", this.DatabaseItem.ItemSpec, this.OutputFilePath.GetMetadata("FullPath")));
            Microsoft.SqlServer.Management.Smo.Database db = this.sqlServer.Databases[this.DatabaseItem.ItemSpec];

            // Script the database
            ScriptingOptions opt = new ScriptingOptions { Bindings = true, ClusteredIndexes = true, ExtendedProperties = true, FullTextCatalogs = true, FullTextIndexes = true, IncludeDatabaseContext = true, IncludeHeaders = true, Indexes = true, LoginSid = true, Permissions = true, Triggers = true, XmlIndexes = true };
            opt.IncludeHeaders = false;
            opt.ToFileOnly = true;
            opt.NoCollation = false;
            opt.FileName = this.OutputFilePath.GetMetadata("FullPath");
            db.Script(opt);

            // now we append to file
            opt.AppendToFile = true;

            foreach (Login o in this.sqlServer.Logins)
            {
                if (!o.IsSystemObject)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Scripting Login: {0}", o.Name));
                    o.Script(opt);
                }
            }

            foreach (Table o in db.Tables)
            {
                if (!o.IsSystemObject)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Scripting Table: {0}", o.Name));
                    o.Script(opt);
                }
            }

            foreach (Rule o in db.Rules)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Scripting Rule: {0}", o.Name));
                o.Script(opt);
            }

            foreach (Default o in db.Defaults)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Scripting Default: {0}", o.Name));
                o.Script(opt);
            }

            foreach (StoredProcedure o in db.StoredProcedures)
            {
                if (!o.IsSystemObject)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Scripting StoredProcedure: {0}", o.Name));
                    o.Script(opt);
                }
            }

            foreach (View o in db.Views)
            {
                if (!o.IsSystemObject)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Scripting View: {0}", o.Name));
                    o.Script(opt);
                }
            }
        }

        private void Rename()
        {
            if (!this.VerifyDatabase())
            {
                return;
            }

            string newName = this.DatabaseItem.GetMetadata("NewName");
            if (string.IsNullOrEmpty(newName))
            {
                this.Log.LogError("Please specify the new name using a NewName metadata item.");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Renaming Database: {0} to: {1}", this.DatabaseItem.ItemSpec, newName));
            SMO.Database db = this.sqlServer.Databases[this.DatabaseItem.ItemSpec];
            db.Rename(newName);
        }

        private void SetOnline()
        {
            if (!this.VerifyDatabase())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting Database Online: {0}", this.DatabaseItem.ItemSpec));
            SMO.Database db = this.sqlServer.Databases[this.DatabaseItem.ItemSpec];
            db.SetOnline();
        }

        private void SetOffline()
        {
            if (!this.VerifyDatabase())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting Database Offline: {0}", this.DatabaseItem.ItemSpec));
            SMO.Database db = this.sqlServer.Databases[this.DatabaseItem.ItemSpec];
            db.SetOffline();
        }

        private void DeleteBackupHistory()
        {
            if (!this.VerifyDatabase())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting Backup History for: {0}", this.DatabaseItem.ItemSpec));
            this.sqlServer.DeleteBackupHistory(this.DatabaseItem.ItemSpec);
        }

        private void GetInfo()
        {
            if (!this.VerifyDatabase())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting Information for: {0}", this.DatabaseItem.ItemSpec));
            this.Information = new TaskItem(this.DatabaseItem.ItemSpec);
            SMO.Database db = this.sqlServer.Databases[this.DatabaseItem.ItemSpec];
            foreach (Property prop in db.Properties)
            {
                this.Information.SetMetadata(prop.Name, prop.Value == null ? string.Empty : prop.Value.ToString());
            }
        }

        private void VerifyBackup()
        {
            if (this.DataFilePath == null)
            {
                this.Log.LogError("DataFilePath is required");
                return;
            }

            if (!File.Exists(this.DataFilePath.GetMetadata("FullPath")))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "DataFilePath not found: {0}", this.DataFilePath.GetMetadata("FullPath")));
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Verifying Backup: {0}", this.DataFilePath.GetMetadata("FullPath")));
            Restore sqlRestore = new Restore();
            sqlRestore.Devices.AddDevice(this.DataFilePath.GetMetadata("FullPath"), DeviceType.File);
            string error;
            bool verified = sqlRestore.SqlVerify(this.sqlServer, out error);
            if (!verified)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Verification failed for: {0}. Error: {1}", this.DataFilePath.GetMetadata("FullPath"), error));
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Backup successfully verified: {0}", this.DataFilePath.GetMetadata("FullPath")));
        }

        private void CheckExists()
        {
            if (this.DatabaseItem == null)
            {
                this.Log.LogError("DatabaseItem is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking whether Database exists: {0}", this.DatabaseItem.ItemSpec));
            this.Exists = this.CheckDatabaseExists();
        }

        private void GetConnectionCount()
        {
            if (!this.VerifyDatabase())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting Connection Count for: {0}", this.DatabaseItem.ItemSpec));
            this.ConnectionCount = this.sqlServer.GetActiveDBConnectionCount(this.DatabaseItem.ItemSpec);
        }

        private bool CheckDatabaseExists()
        {
            this.sqlServer.Refresh();
            return !(this.sqlServer.Databases[this.DatabaseItem.ItemSpec] == null);
        }

        private void Create()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating Database: {0}", this.DatabaseItem.ItemSpec));
            if (this.CheckDatabaseExists())
            {
                if (this.Force)
                {
                    this.Delete();
                }
                else
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Database already exists: {0}. Set Force to true to delete an existing Database.", this.DatabaseItem.ItemSpec));
                    return;
                }
            }

            SMO.Database newDatabase = new SMO.Database(this.sqlServer, this.DatabaseItem.ItemSpec);
            newDatabase.Create();
        }

        private void Delete()
        {
            if (!this.VerifyDatabase())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting Database: {0}", this.DatabaseItem.ItemSpec));
            SMO.Database oldDatabase = new SMO.Database(this.sqlServer, this.DatabaseItem.ItemSpec);
            oldDatabase.Refresh();
            oldDatabase.Drop();
        }

        private void Restore()
        {
            if (this.DatabaseItem == null)
            {
                this.Log.LogError("DatabaseItem is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Restoring SQL {2}: {0} from {1}", this.DatabaseItem.ItemSpec, this.DataFilePath.GetMetadata("FullPath"), this.RestoreAction));
            Restore sqlRestore = new Restore { Database = this.DatabaseItem.ItemSpec, Action = this.restoreAction };
            sqlRestore.Devices.AddDevice(this.DataFilePath.GetMetadata("FullPath"), DeviceType.File);
            sqlRestore.PercentCompleteNotification = this.NotificationInterval;
            sqlRestore.ReplaceDatabase = true;
            sqlRestore.PercentComplete += this.ProgressEventHandler;
            if (this.ReplaceDatabase)
            {
                sqlRestore.ReplaceDatabase = true;
                if (string.IsNullOrEmpty(this.LogName) || this.LogFilePath == null)
                {
                    this.Log.LogError("LogName and LogFilePath must be specified if ReplaceDatabase is true.");
                    return;
                }

                sqlRestore.RelocateFiles.Add(new RelocateFile(this.DatabaseItem.ItemSpec, this.DataFilePath.GetMetadata("FullPath")));
                sqlRestore.RelocateFiles.Add(new RelocateFile(this.LogName, this.LogFilePath.GetMetadata("FullPath")));
            }

            sqlRestore.SqlRestore(this.sqlServer);
        }

        private void Backup()
        {
            if (!this.VerifyDatabase())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Backing up SQL {2}: {0} to: {1}", this.DatabaseItem.ItemSpec, this.DataFilePath.GetMetadata("FullPath"), this.BackupAction));
            Backup sqlBackup = new Backup();
            sqlBackup.Devices.AddDevice(this.DataFilePath.GetMetadata("FullPath"), DeviceType.File);
            sqlBackup.Database = this.DatabaseItem.ItemSpec;
            sqlBackup.Incremental = this.Incremental;
            sqlBackup.Action = this.backupAction;
            sqlBackup.Initialize = true;
            sqlBackup.PercentCompleteNotification = this.NotificationInterval;
            sqlBackup.PercentComplete += this.ProgressEventHandler;
            sqlBackup.SqlBackup(this.sqlServer);
        }

        private bool VerifyDatabase()
        {
            if (this.DatabaseItem == null)
            {
                this.Log.LogError("DatabaseItem is required");
                return false;
            }

            if (!this.CheckDatabaseExists())
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Database not found: {0}", this.DatabaseItem.ItemSpec));
                return false;
            }

            return true;
        }

        private void ProgressEventHandler(object sender, PercentCompleteEventArgs e)
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "{0}% done", e.Percent));
        }
    }
}