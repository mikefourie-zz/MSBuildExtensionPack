//-----------------------------------------------------------------------
// <copyright file="Database.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Sql2012
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
    /// <para><i>Backup</i> (<b>Required: </b>DatabaseItem, DataFilePath <b>Optional: </b>BackupAction, CompressionOption, Incremental, NotificationInterval, NoPooling, StatementTimeout, CopyOnly)</para>
    /// <para><i>CheckExists</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling, StatementTimeout <b>Output:</b> Exists)</para>
    /// <para><i>Create</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>Collation, NoPooling, DataFilePath, LogName, LogFilePath, FileGroupName, StatementTimeout)</para>
    /// <para><i>Delete</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling, StatementTimeout)</para>
    /// <para><i>DeleteBackupHistory</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling, StatementTimeout)</para>
    /// <para><i>GetConnectionCount</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling, StatementTimeout)</para>
    /// <para><i>GetInfo</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling, StatementTimeout)</para>
    /// <para><i>Rename</i> (<b>Required: </b>DatabaseItem (NewName metadata) <b>Optional: </b>NoPooling, StatementTimeout)</para>
    /// <para><i>Restore</i> (<b>Required: </b>DatabaseItem, DataFilePath <b>Optional: </b>ReplaceDatabase, NewDataFilePath, RestoreAction, Incremental, NotificationInterval, NoPooling, LogName, LogFilePath, PrimaryDataFileName, SecondaryDataFileName, SecondaryDataFilePath, StatementTimeout)</para>
    /// <para><i>Script</i> (<b>Required: </b>DatabaseItem, OutputFilePath <b>Optional: </b>NoPooling, StatementTimeout)</para>
    /// <para><i>SetOffline</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling, StatementTimeout)</para>
    /// <para><i>SetOnline</i> (<b>Required: </b>DatabaseItem <b>Optional: </b>NoPooling, StatementTimeout)</para>
    /// <para><i>VerifyBackup</i> (<b>Required: </b>DataFilePath <b>Optional: </b>NoPooling, StatementTimeout)</para>
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
    ///         <ItemGroup>
    ///             <Database Include="ADatabase">
    ///                 <NewName>ADatabase2</NewName>
    ///             </Database>
    ///             <Database2 Include="ADatabase2">
    ///                 <NewName>ADatabase</NewName>
    ///             </Database2>
    ///         </ItemGroup>
    ///         <!-- Get information on a database -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="GetInfo" DatabaseItem="ADatabase">
    ///             <Output TaskParameter="Information" ItemName="AllInfo"/>
    ///         </MSBuild.ExtensionPack.Sql2012.Database>
    ///         <!-- All the database information properties are available as metadata on the Infomation item -->
    ///         <Message Text="SpaceAvailable: %(AllInfo.SpaceAvailable)"/>
    ///         <!-- Backup a database -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="Backup" DatabaseItem="ADatabase" DataFilePath="c:\a\ADatabase.bak"/>
    ///         <!-- Verify a database backup -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="VerifyBackup" DataFilePath="c:\a\ADatabase.bak"/>
    ///         <!-- Restore a database -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="Restore" DatabaseItem="ADatabase" DataFilePath="c:\a\ADatabase.bak"/>
    ///         <!-- Restore a database to a different location-->
    ///         <MSBuild.ExtensionPack.Sql2012.Database MachineName="Desktop\Sql2012" TaskAction="Restore" DatabaseItem="ADatabase" DataFilePath="c:\a\ADatabase.bak" NewDataFilePath="c:\k\ADatabase2.mdf" LogFilePath="c:\a\ADatabase2_log.LDF"/>
    ///         <!-- Create a database -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="Create" DatabaseItem="ADatabase2"/>
    ///         <!-- Create the database again, using Force to delete the existing database -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="Create" DatabaseItem="ADatabase2" Collation="Latin1_General_CI_AI" Force="true"/>
    ///         <!-- Check whether a database exists -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="CheckExists" DatabaseItem="ADatabase2">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Sql2012.Database>
    ///         <Message Text="Database Exists: $(DoesExist)"/>
    ///         <!-- Delete a database -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="Delete" DatabaseItem="ADatabase2"/>
    ///         <!-- Check whether a database exists -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="CheckExists" DatabaseItem="ADatabase2">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Sql2012.Database>
    ///         <Message Text="Database Exists: $(DoesExist)"/>
    ///         <!-- Get the number of active connections to a database -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="GetConnectionCount" DatabaseItem="ADatabase">
    ///             <Output TaskParameter="ConnectionCount" PropertyName="Count"/>
    ///         </MSBuild.ExtensionPack.Sql2012.Database>
    ///         <Message Text="Database ConnectionCount: $(Count)"/>
    ///         <!-- Delete the backup history for a database -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="DeleteBackupHistory" DatabaseItem="ADatabase"/>
    ///         <!-- Set a database offline -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="SetOffline" DatabaseItem="ADatabase"/>
    ///         <!-- Set a database online -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="SetOnline" DatabaseItem="ADatabase"/>
    ///         <!-- Rename a database -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="Rename" DatabaseItem="@(Database)"/>
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="Rename" DatabaseItem="@(Database2)"/>
    ///         <!-- Script a database to file -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="Script" DatabaseItem="ReportServer" OutputFilePath="c:\ADatabaseScript.sql"/>
    ///         <!-- Restore a database to a new Name -->
    ///         <MSBuild.ExtensionPack.Sql2012.Database TaskAction="Restore" MachineName="$(SqlServerName)" DatabaseItem="$(DatabaseName)" DataFilePath="$(DbDataFilePath)" PrimaryDataFileName="SomeDatabase" LogName="SomeDatabase_log" SecondaryDataFileName="SomeDatabase_CDC" NewDataFilePath="$(OSFilePath)$(DatabaseName).mdf" SecondaryDataFilePath="$(OSFilePath)$(DatabaseName)_CDC.ndf" LogFilePath="$(OSFilePath)\$(DatabaseName)_log.ldf" ReplaceDatabase="True" />
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Database : BaseTask
    {
        private bool trustedConnection;
        private SMO.Server sqlServer;
        private BackupActionType backupAction = BackupActionType.Database;
        private RestoreActionType restoreAction = RestoreActionType.Database;
        private int notificationInterval = 10;
        private string fileGroupName = "PRIMARY";
        private int statementTimeout = -1;
        private BackupCompressionOptions compressionOption = BackupCompressionOptions.Default;

        /// <summary>
        /// Set to true to create a NonPooledConnection to the server. Default is false.
        /// </summary>
        public bool NoPooling { get; set; }

        /// <summary>
        /// A Boolean value that specifies whether a new image of the restored database will be created. If True, a new image of the database is created. The image is created regardless of the presence of an existing database with the same name. If False (default), a new image of the database is not created by the restore operation. The database targeted by the restore operation must exist on an instance of Microsoft SQL Server. 
        /// </summary>
        public bool ReplaceDatabase { get; set; }

        /// <summary>
        /// Set to true to perform an Incremental backup. Default is false.
        /// </summary>
        public bool Incremental { get; set; }

        /// <summary>
        /// Set to true to force the creation of a database if it already exists.
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Sets the database name
        /// </summary>
        public ITaskItem DatabaseItem { get; set; }

        /// <summary>
        /// Sets the compression option for the backup. Supports On, Off and Default. Default is Default.
        /// </summary>
        public string CompressionOption
        {
            get { return this.compressionOption.ToString(); }
            set { this.compressionOption = (BackupCompressionOptions)Enum.Parse(typeof(BackupCompressionOptions), value); }
        }

        /// <summary>
        /// Sets the primary data file name.
        /// </summary>
        public string PrimaryDataFileName { get; set; }

        /// <summary>
        /// Sets the Log Name. Defaults DatabaseItem.ItemSpec + "_log"
        /// </summary>
        public string LogName { get; set; }

        /// <summary>
        /// Sets the secondary data file name. No default value.
        /// </summary>
        public string SecondaryDataFileName { get; set; }

        /// <summary>
        /// Sets whether the backup is a copy-only backup. Default is false.
        /// </summary>
        public bool CopyOnly { get; set; }

        /// <summary>
        /// Sets the collation of the database.
        /// </summary>
        public string Collation { get; set; }
        
        /// <summary>
        /// Sets the type of backup action to perform. Supports Database, Files and Log. Default is Database
        /// </summary>
        public string BackupAction
        {
            get { return this.backupAction.ToString(); }
            set { this.backupAction = (BackupActionType)Enum.Parse(typeof(BackupActionType), value); }
        }

        /// <summary>
        /// Sets the type of restore action to perform. Supports Database, Files, Log, OnlineFiles, OnlinePage. Default is Database
        /// </summary>
        public string RestoreAction
        {
            get { return this.restoreAction.ToString(); }
            set { this.restoreAction = (RestoreActionType)Enum.Parse(typeof(RestoreActionType), value); }
        }

        /// <summary>
        /// Sets the PercentCompleteNotification interval. Defaults to 10.
        /// </summary>
        public int NotificationInterval
        {
            get { return this.notificationInterval; }
            set { this.notificationInterval = value; }
        }

        /// <summary>
        /// Sets the FileGroupName. Defaults to PRIMARY
        /// </summary>
        public string FileGroupName
        {
            get { return this.fileGroupName; }
            set { this.fileGroupName = value; }
        }

        /// <summary>
        /// Sets the DataFilePath.
        /// </summary>
        public ITaskItem DataFilePath { get; set; }

        /// <summary>
        /// Sets the NewDataFilePath.
        /// </summary>
        public ITaskItem NewDataFilePath { get; set; }
        
        /// <summary>
        /// Sets the SecondaryDataFilePath.
        /// </summary>
        public ITaskItem SecondaryDataFilePath { get; set; }
        
        /// <summary>
        /// Sets the LogFilePath.
        /// </summary>
        public ITaskItem LogFilePath { get; set; }

        /// <summary>
        /// Sets the OutputFilePath.
        /// </summary>
        public ITaskItem OutputFilePath { get; set; }

        /// <summary>
        /// Sets the number of seconds before an operation times out. The default is not to specify this property on the connection.
        /// </summary>
        public int StatementTimeout
        {
            get
            {
                return this.statementTimeout;
            }

            set 
            {
                this.statementTimeout = value;
            }
        }

        /// <summary>
        /// Gets whether the database exists
        /// </summary>
        [Output]
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
            if (string.IsNullOrEmpty(this.UserName))
            {
                this.LogTaskMessage(MessageImportance.Low, "Using a Trusted Connection");
                this.trustedConnection = true;
            }

            ServerConnection con = new ServerConnection { LoginSecure = this.trustedConnection, ServerInstance = this.MachineName, NonPooledConnection = this.NoPooling };
            if (this.statementTimeout >= 0)
            {
                con.StatementTimeout = this.statementTimeout;
            }

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
            ScriptingOptions opt = new ScriptingOptions { Bindings = true, ClusteredIndexes = true, ExtendedProperties = true, FullTextCatalogs = true, FullTextIndexes = true, IncludeDatabaseContext = true, IncludeDatabaseRoleMemberships = true, IncludeHeaders = true, Indexes = true, LoginSid = true, Permissions = true, Triggers = true, XmlIndexes = true };
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
            return this.sqlServer.Databases[this.DatabaseItem.ItemSpec] != null;
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
            if (this.DataFilePath != null)
            {
                FileGroup fileGroup = new FileGroup(newDatabase, this.FileGroupName);
                DataFile dataFile = new DataFile(fileGroup, this.DatabaseItem.ItemSpec, this.DataFilePath.GetMetadata("FullPath"));
                fileGroup.Files.Add(dataFile);
                newDatabase.FileGroups.Add(fileGroup);
            }

            if (this.LogFilePath != null)
            {
                if (string.IsNullOrEmpty(this.LogName))
                {
                    this.LogName = this.DatabaseItem.ItemSpec + "_log";
                }

                LogFile logFile = new LogFile(newDatabase, this.LogName, this.LogFilePath.GetMetadata("FullPath"));
                newDatabase.LogFiles.Add(logFile);
            }
            
            if (!string.IsNullOrEmpty(this.Collation))
            {
                newDatabase.Collation = this.Collation;
            }

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

            if (this.ReplaceDatabase && this.LogFilePath == null)
            {
                this.Log.LogError("LogFilePath must be specified if ReplaceDatabase is true.");
                return;
            }

            string primaryDataFileName = (!string.IsNullOrEmpty(this.PrimaryDataFileName)) ? this.PrimaryDataFileName : this.DatabaseItem.ItemSpec;
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Restoring SQL {2}: {0} from {1}", this.DatabaseItem.ItemSpec, this.DataFilePath.GetMetadata("FullPath"), this.RestoreAction));
            Restore sqlRestore = new Restore { Database = this.DatabaseItem.ItemSpec, Action = this.restoreAction, PercentCompleteNotification = this.NotificationInterval, ReplaceDatabase = this.ReplaceDatabase };
            sqlRestore.Devices.AddDevice(this.DataFilePath.GetMetadata("FullPath"), DeviceType.File);
            sqlRestore.PercentComplete += this.ProgressEventHandler;

            if (string.IsNullOrEmpty(this.LogName))
            {
                this.LogName = primaryDataFileName + "_log";
            }

            // add primary data file
            sqlRestore.RelocateFiles.Add(new RelocateFile(primaryDataFileName, (this.NewDataFilePath != null) ? this.NewDataFilePath.GetMetadata("FullPath") : this.DataFilePath.GetMetadata("FullPath")));

            // add log file, if path provided
            if (this.LogFilePath != null)
            {
                sqlRestore.RelocateFiles.Add(new RelocateFile(this.LogName, this.LogFilePath.GetMetadata("FullPath")));
            }

            // add secondary data file, if name and path provided
            if (!string.IsNullOrEmpty(this.SecondaryDataFileName) && this.SecondaryDataFilePath != null)
            {
                sqlRestore.RelocateFiles.Add(new RelocateFile(this.SecondaryDataFileName, this.SecondaryDataFilePath.GetMetadata("FullPath")));
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
            sqlBackup.CopyOnly = this.CopyOnly;
            sqlBackup.CompressionOption = this.compressionOption;
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