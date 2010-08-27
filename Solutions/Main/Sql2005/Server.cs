//-----------------------------------------------------------------------
// <copyright file="Server.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Sql2005
{
    using System.Globalization;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Smo;
    using SMO = Microsoft.SqlServer.Management.Smo;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>GetConnectionCount</i> (<b>Optional: </b>NoPooling <b>Output: </b>ConnectionCount)</para>
    /// <para><i>GetInfo</i> (<b>Optional: </b>NoPooling <b>Output: </b>Information)</para>
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
    ///         <!-- Get information for a server, not that this defaults to the default instance on the local server -->
    ///         <MSBuild.ExtensionPack.Sql2008.Server TaskAction="GetInfo">
    ///             <Output TaskParameter="Information" ItemName="AllInfo"/>
    ///         </MSBuild.ExtensionPack.Sql2008.Server>
    ///         <!-- All the server information properties are available as metadata on the Infomation item -->
    ///         <Message Text="PhysicalMemory: %(AllInfo.PhysicalMemory)"/>
    ///         <!-- Get all the active connections to the server -->
    ///         <MSBuild.ExtensionPack.Sql2008.Server TaskAction="GetConnectionCount">
    ///             <Output TaskParameter="ConnectionCount" PropertyName="Count"/>
    ///         </MSBuild.ExtensionPack.Sql2008.Server>
    ///         <Message Text="Server ConnectionCount: $(Count)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.1.0/html/fb52629e-5d11-7fcc-7f9d-c0846c0e508e.htm")]
    public class Server : BaseTask
    {
        private const string GetConnectionCountTaskAction = "GetConnectionCount";
        private const string GetInfoTaskAction = "GetInfo";
        
        private bool trustedConnection;
        private SMO.Server sqlServer;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(GetConnectionCountTaskAction)]
        [DropdownValue(GetInfoTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Set to true to create a NonPooledConnection to the server. Default is false.
        /// </summary>
        [TaskAction(GetConnectionCountTaskAction, false)]
        [TaskAction(GetInfoTaskAction, false)]
        public bool NoPooling { get; set; }

        /// <summary>
        /// Gets the number of connections the server has open
        /// </summary>
        [Output]
        [TaskAction(GetConnectionCountTaskAction, false)]
        public int ConnectionCount { get; set; }

        /// <summary>
        /// Gets the Information TaskItem. Each available property is added as metadata.
        /// </summary>
        [Output]
        [TaskAction(GetInfoTaskAction, false)]
        public ITaskItem Information { get; set; }

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
                case "GetConnectionCount":
                    this.GetConnectionCount();
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

        private void GetConnectionCount()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting Connection Count for: {0}", this.MachineName));
            foreach (SMO.Database db in this.sqlServer.Databases)
            {
                this.ConnectionCount += this.sqlServer.GetActiveDBConnectionCount(db.Name);
            }
        }

        private void GetInfo()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting Information for: {0}", this.MachineName));
            this.Information = new TaskItem(this.MachineName);
            foreach (Property prop in this.sqlServer.Information.Properties)
            {
                this.Information.SetMetadata(prop.Name, prop.Value == null ? string.Empty : prop.Value.ToString());
            }
        }
    }
}