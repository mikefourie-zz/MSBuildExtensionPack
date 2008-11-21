//-----------------------------------------------------------------------
// <copyright file="Iis6AppPool.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Web
{
    using System;
    using System.DirectoryServices;
    using System.Globalization;
    using System.Security.Permissions;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Create</i> (<b>Required: </b> Name <b>Optional:</b> Properties)</para>
    /// <para><i>CheckExists</i> (<b>Required: </b> Name <b>Output: </b>Exists)</para>
    /// <para><i>Delete</i> (<b>Required: </b> Name)</para>
    /// <para><i>Modify</i> (<b>Required: </b> Name, Properties)</para>
    /// <para><i>Start</i> (<b>Required: </b> Name)</para>
    /// <para><i>Stop</i> (<b>Required: </b> Name)</para>
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
    ///         <!-- Delete an AppPool -->
    ///         <MSBuild.ExtensionPack.Web.Iis6AppPool TaskAction="Delete" Name="AnAppPool"/>
    ///         <!-- Create an AppPool -->
    ///         <MSBuild.ExtensionPack.Web.Iis6AppPool TaskAction="Create" Name="AnAppPool" Properties="AppPoolAutoStart=TRUE;PeriodicRestartTime=0;PeriodicRestartRequests=0;PeriodicRestartMemory=0;PeriodicRestartPrivateMemory=0;PeriodicRestartSchedule=04:00;IdleTimeout=0;AppPoolQueueLength=2000;CPULimit=0;CPUResetInterval=5;CPUAction=0;MaxProcesses=1;PingingEnabled=TRUE;PingInterval=60;PingResponseTime=90;RapidFailProtection=FALSE;RapidFailProtectionMaxCrashes=5;RapidFailProtectionInterval=5;StartupTimeLimit=60;ShutdownTimeLimit=60;AppPoolIdentityType=3;"/>
    ///         <!-- Modify an AppPool -->
    ///         <MSBuild.ExtensionPack.Web.Iis6AppPool TaskAction="Modify" Name="AnAppPool" Properties="AppPoolAutoStart=TRUE;PeriodicRestartTime=0;PeriodicRestartRequests=0;PeriodicRestartMemory=0;PeriodicRestartPrivateMemory=0;PeriodicRestartSchedule=04:00;IdleTimeout=0;AppPoolQueueLength=1952;CPULimit=0;CPUResetInterval=5;CPUAction=0;MaxProcesses=6;PingingEnabled=TRUE;PingInterval=60;PingResponseTime=90;RapidFailProtection=FALSE;RapidFailProtectionMaxCrashes=5;RapidFailProtectionInterval=5;StartupTimeLimit=60;ShutdownTimeLimit=60;AppPoolIdentityType=3;"/>
    ///         <!-- Check whether an AppPool exists -->
    ///         <MSBuild.ExtensionPack.Web.Iis6AppPool TaskAction="CheckExists" Name="AnAppPool">
    ///             <Output PropertyName="DoesExist" TaskParameter="Exists"/>
    ///         </MSBuild.ExtensionPack.Web.Iis6AppPool>
    ///         <Message Text="AnAppPool exists: $(DoesExist)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Iis6AppPool : BaseTask
    {
        private string properties;

        /// <summary>
        /// Sets the app pool properties. This is a semicolon seperated list, e.g. AppPoolAutoStart=TRUE;PeriodicRestartTime=0
        /// </summary>
        public string Properties
        {
            get { return System.Web.HttpUtility.HtmlDecode(this.properties); }
            set { this.properties = value; }
        }

        /// <summary>
        /// Sets the name of the AppPool. Required.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets whether the app pool exists. Output
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        internal string IISPath
        {
            get { return "IIS://" + this.MachineName + "/W3SVC"; }
        }

        internal string AppPoolsPath
        {
            get { return "IIS://" + this.MachineName + "/W3SVC/AppPools"; }
        }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case "Create":
                    this.Create();
                    break;
                case "Modify":
                    this.Modify();
                    break;
                case "Start":
                case "Delete":
                case "Stop":
                    this.ControlAppPool(this.TaskAction);
                    break;
                case "CheckExists":
                    this.Exists = this.CheckExists();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        private void UpdateMetabaseProperty(DirectoryEntry entry, string metabasePropertyName, string metabaseProperty)
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Applying Property: {0}({1})", metabasePropertyName, metabaseProperty));

            if (metabaseProperty.IndexOf('|') == -1)
            {
                entry.Properties[metabasePropertyName].Value = metabaseProperty;
            }
            else
            {
                entry.Properties[metabasePropertyName].Value = string.Empty;
                string[] metabaseProperties = metabaseProperty.Split('|');
                foreach (string metabasePropertySplit in metabaseProperties)
                {
                    entry.Properties[metabasePropertyName].Add(metabasePropertySplit);
                }

                entry.CommitChanges();
            }
        }

        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        private void Modify()
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Modifying AppPool: {0}", this.Name));

            // We'll try and find the app pool first.
            using (DirectoryEntry appPoolEntry = this.LoadAppPool(this.Name))
            {
                if (appPoolEntry == null)
                {
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "The AppPool does not exist: {0}", this.Name));
                    return;
                }

                if (string.IsNullOrEmpty(this.Properties) == false)
                {
                    string[] propList = this.Properties.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string s in propList)
                    {
                        string[] propPair = s.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        string propValue = propPair.Length > 1 ? propPair[1] : string.Empty;
                        this.UpdateMetabaseProperty(appPoolEntry, propPair[0], propValue);
                    }

                    appPoolEntry.CommitChanges();
                }
            }
        }

        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        private bool CheckExists()
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Checking AppPool Exists: {0}", this.Name));
            return DirectoryEntry.Exists(this.AppPoolsPath + @"/" + this.Name);
        }

        private DirectoryEntry LoadAppPools()
        {
            string poolsPath = string.Format(CultureInfo.InvariantCulture, "{0}/AppPools", this.IISPath);
            DirectoryEntry appPools = new DirectoryEntry(poolsPath);
            if (appPools == null)
            {
                throw new ApplicationException(string.Format(CultureInfo.CurrentUICulture, "IIS DirectoryServices unavailable: {0}", poolsPath));
            }

            return appPools;
        }

        private DirectoryEntry LoadAppPool(string appPoolName)
        {
            using (DirectoryEntry appPoolsEntry = this.LoadAppPools())
            {
                DirectoryEntries appPools = appPoolsEntry.Children;

                foreach (DirectoryEntry appPool in appPools)
                {
                    if (appPool.SchemaClassName == "IIsApplicationPool")
                    {
                        if (string.Compare(appPoolName, appPool.Name, StringComparison.CurrentCultureIgnoreCase) == 0)
                        {
                            return appPool;
                        }
                    }

                    appPool.Dispose();
                }

                return null;
            }
        }

        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        private void ControlAppPool(string appPoolAction)
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "AppPool: {0} - Action: {1}", this.Name, appPoolAction));

            // First locate the app pool.
            using (DirectoryEntry appPoolEntry = this.LoadAppPool(this.Name))
            {
                if (appPoolEntry != null)
                {
                    switch (appPoolAction)
                    {
                        case "Delete":
                            using (DirectoryEntry appPoolsEntry = this.LoadAppPools())
                            {
                                appPoolsEntry.Invoke("Delete", "IIsApplicationPool", appPoolEntry.Name);
                            }

                            break;
                        case "Stop":
                            appPoolEntry.Invoke("Stop");
                            break;
                        case "Start":
                            appPoolEntry.Invoke("Start");
                            break;
                    }
                }
            }
        }

        [EnvironmentPermissionAttribute(SecurityAction.LinkDemand, Unrestricted = true)]
        private void Create()
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Creating AppPool: {0}", this.Name));

            // We'll try and find the app pool first.
            using (DirectoryEntry appPoolEntry = this.LoadAppPool(this.Name))
            {
                if (appPoolEntry != null)
                {
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "The AppPool already exists: {0}", this.Name));
                    return;
                }
            }

            using (DirectoryEntry appPoolsEntry = new DirectoryEntry(this.AppPoolsPath))
            using (DirectoryEntry appPoolEntry = appPoolsEntry.Children.Add(this.Name, "IIsApplicationPool"))
            {
                if (string.IsNullOrEmpty(this.Properties) == false)
                {
                    string[] propList = this.Properties.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string s in propList)
                    {
                        string[] propPair = s.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        string propValue = propPair.Length > 1 ? propPair[1] : string.Empty;
                        this.UpdateMetabaseProperty(appPoolEntry, propPair[0], propValue);
                    }

                    appPoolEntry.CommitChanges();
                }
            }
        }
    }
}