//-----------------------------------------------------------------------
// <copyright file="Application.cs">(c) FreeToDev. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.BizTalk
{
    using System.Globalization;
    using Microsoft.BizTalk.ApplicationDeployment;
    using Microsoft.BizTalk.ExplorerOM;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using OM = Microsoft.BizTalk.ExplorerOM;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddReference</i> (<b>Required: </b>Application, References <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>RemoveReference</i> (<b>Required: </b>Application, References <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>CheckExists</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>Create</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>Delete</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>EnableAllReceiveLocations</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>Get</i> (<b>Optional: </b>MachineName, Database)</para>
    /// <para><i>StartAll</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>StartAllOrchestrations</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>StartAllSendPortGroups</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>StartAllSendPorts</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>StartReferencedApplications</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>StopAll</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>DisableAllReceiveLocations</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>UndeployAllPolicies</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>UnenlistAllOrchestrations</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>UnenlistAllSendPortGroups</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>UnenlistAllSendPorts</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><i>StopReferencedApplications</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, Database)</para>
    /// <para><b>Remote Support:</b> Yes</para>
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
    ///             <Apps Include="An Application"/>
    ///             <NewApps Include="NewExtensionPackApp">
    ///                 <!--<Default>true</Default>-->
    ///                 <Description>New ExtensionPack App</Description>
    ///             </NewApps>
    ///             <Reference Include="Another Application"/>
    ///         </ItemGroup>
    ///         <!-- Get a list of BizTalk Applications -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="Get">
    ///             <Output TaskParameter="Applications" ItemName="ApplicationList"/>
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkApplication>
    ///         <Message Text="%(ApplicationList.Identity)"/>
    ///         <!-- Add a Reference -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="AddReference" Application="An Application" References="@(Reference)"/>
    ///         <!-- Remove a Reference -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="RemoveReference" Application="An Application" References="@(Reference)"/>
    ///         <!-- Check if the Applications in the Apps collection exist -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="CheckExists" Applications="@(Apps)"/>
    ///         <!-- Execute a StartAll on the Apps Application collection -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="StartAll" Applications="@(Apps)"/>
    ///         <!-- Execute a StopAll on the Apps Application collection -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="StopAll" Applications="@(Apps)"/>
    ///         <!-- Force the creation of the Applications in the NewApps collection -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="Create" Applications="@(NewApps)" Force="true"/>
    ///         <!-- Delete the Applications in the NewApps collection-->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="Delete" Applications="@(NewApps)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class BizTalkApplication : BaseTask
    {
        private string database = "BizTalkMgmtDb";
        private BtsCatalogExplorer explorer;
        private OM.Application app;

        /// <summary>
        /// Get or sets the Application Item Collection
        /// </summary>
        [Output]
        public ITaskItem[] Applications { get; set; }

        /// <summary>
        /// Sets the Referenced Applications
        /// </summary>
        public ITaskItem[] References { get; set; }

        /// <summary>
        /// Sets the Application Name
        /// </summary>
        public string Application { get; set; }

        /// <summary>
        /// Sets the Application description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Sets whether the Application is the default application
        /// </summary>
        public bool Default { get; set; }

        /// <summary>
        /// Sets the Management Database to connect to. Default is BizTalkMgmtDb
        /// </summary>
        public string Database
        {
            get { return this.database; }
            set { this.database = value; }
        }

        /// <summary>
        /// Gets whether the Application exists
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Set to true to delete an existing Application when Create is called.
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Connecting to BtsCatalogExplorer: Server: {0}. Database: {1}", this.MachineName, this.Database));
            this.explorer = new BtsCatalogExplorer { ConnectionString = string.Format(CultureInfo.InvariantCulture, "Server={0};Database={1};Integrated Security=SSPI;", this.MachineName, this.Database) };

            switch (this.TaskAction)
            {
                case "Create":
                    this.Create();
                    break;
                case "Get":
                    this.GetApplications();
                    break;
                case "CheckExists":
                    this.CheckApplicationsExist();
                    break;
                case "StartAll":
                case "EnableAllReceiveLocations":
                case "StartAllOrchestrations":
                case "StartAllSendPortGroups":
                case "StartAllSendPorts":
                case "StartReferencedApplications":
                    this.StartApplication();
                    break;
                case "StopAll":
                case "DisableAllReceiveLocations":
                case "UndeployAllPolicies":
                case "UnenlistAllOrchestrations":
                case "UnenlistAllSendPortGroups":
                case "UnenlistAllSendPorts":
                case "StopReferencedApplications":
                    this.StopApplication();
                    break;
                case "Delete":
                    this.Delete();
                    break;
                case "RemoveReference":
                    this.ConfigureReference();
                    break;
                case "AddReference":
                    this.ConfigureReference();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void ConfigureReference()
        {
            if (string.IsNullOrEmpty(this.Application))
            {
                this.Log.LogError("Application is required");
                return;
            }

            if (!this.CheckExists(this.Application))
            {
                this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Application not found: {0}", this.Application));
                return;
            }

            this.app = this.explorer.Applications[this.Application];
            foreach (ITaskItem item in this.References)
            {
                OM.Application refApp = this.explorer.Applications[item.ItemSpec];
                if (refApp != null)
                {
                    switch (this.TaskAction)
                    {
                        case "RemoveReference":
                            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Removing Referenced Application: {0} from: {1}", item.ItemSpec, this.Application));
                            this.app.RemoveReference(refApp);
                            break;
                        case "AddReference":
                            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Adding Referenced Application: {0} from: {1}", item.ItemSpec, this.Application));
                            this.app.AddReference(refApp);
                            break;
                    }
                }
                else
                {
                    Log.LogError(string.Format(CultureInfo.InvariantCulture, "Referenced Application not found: {0}", item.ItemSpec));
                    return;
                }
            }

            this.explorer.SaveChanges();
        }

        private void CheckApplicationsExist()
        {
            foreach (ITaskItem appl in this.Applications)
            {
                if (!this.CheckExists(appl.ItemSpec))
                {
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Application not found: {0}", appl.ItemSpec));
                }
            }
        }

        private void Create()
        {
            foreach (ITaskItem appl in this.Applications)
            {
                this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Creating Application: {0}", appl.ItemSpec));
                if (this.CheckExists(appl.ItemSpec))
                {
                    if (this.Force)
                    {
                        this.DeleteApplication(appl);
                        this.explorer.Refresh();
                    }
                    else
                    {
                        this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Application already exists: {0}. Set Force to true to delete the Application.", appl.ItemSpec));
                        return;
                    }
                }

                OM.Application newapp = this.explorer.AddNewApplication();
                newapp.Name = appl.ItemSpec;
                newapp.Description = appl.GetMetadata("Description");
                if (appl.GetMetadata("Default") == "true")
                {
                    this.explorer.DefaultApplication = newapp;
                }
            }
            
            this.explorer.SaveChanges();
        }

        private void Delete()
        {
            foreach (ITaskItem appl in this.Applications)
            {
                this.DeleteApplication(appl);
            }
        }

        private void DeleteApplication(ITaskItem application)
        {
            if (!this.CheckExists(application.ItemSpec))
            {
                return;
            }

            using (Group group = new Group())
            {
                group.DBName = this.Database;
                group.DBServer = this.MachineName;

                Microsoft.BizTalk.ApplicationDeployment.ApplicationCollection apps = group.Applications;
                apps.UiLevel = 2;

                Microsoft.BizTalk.ApplicationDeployment.Application deadapp = apps[application.ItemSpec];
                this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Deleting Application: {0}", application.ItemSpec));
                apps.Remove(deadapp);
            }

            this.explorer.SaveChanges();
        }

        private void StopApplication()
        {
            foreach (ITaskItem appl in this.Applications)
            {
                if (!this.CheckExists(appl.ItemSpec))
                {
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Application not found: {0}", appl.ItemSpec));
                    return;
                }

                OM.ApplicationStopOption option = OM.ApplicationStopOption.StopAll;
                switch (this.TaskAction)
                {
                    case "StopAll":
                        option = OM.ApplicationStopOption.StopAll;
                        break;
                    case "DisableAllReceiveLocations":
                        option = OM.ApplicationStopOption.DisableAllReceiveLocations;
                        break;
                    case "UndeployAllPolicies":
                        option = OM.ApplicationStopOption.UndeployAllPolicies;
                        break;
                    case "UnenlistAllOrchestrations":
                        option = OM.ApplicationStopOption.UnenlistAllOrchestrations;
                        break;
                    case "UnenlistAllSendPortGroups":
                        option = OM.ApplicationStopOption.UnenlistAllSendPortGroups;
                        break;
                    case "UnenlistAllSendPorts":
                        option = OM.ApplicationStopOption.UnenlistAllSendPorts;
                        break;
                    case "StopReferencedApplications":
                        option = OM.ApplicationStopOption.StopReferencedApplications;
                        break;
                }

                this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Stopping Application: {0}", appl.ItemSpec));
                this.explorer.SaveChanges();
                this.app.Stop(option);
                this.explorer.SaveChanges();
            }
        }

        private void StartApplication()
        {
            foreach (ITaskItem appl in this.Applications)
            {
                this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Manage Application: {0}. Action: {1}", appl.ItemSpec, this.TaskAction));

                if (!this.CheckExists(appl.ItemSpec))
                {
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Application not found: {0}", appl.ItemSpec));
                    return;
                }

                switch (this.TaskAction)
                {
                    case "StartAll":
                        this.app.Start(OM.ApplicationStartOption.StartAll);
                        break;
                    case "EnableAllReceiveLocations":
                        this.app.Start(OM.ApplicationStartOption.EnableAllReceiveLocations);
                        break;
                    case "StartAllOrchestrations":
                        this.app.Start(OM.ApplicationStartOption.StartAllOrchestrations);
                        break;
                    case "StartAllSendPortGroups":
                        this.app.Start(OM.ApplicationStartOption.StartAllSendPortGroups);
                        break;
                    case "StartAllSendPorts":
                        this.app.Start(OM.ApplicationStartOption.StartAllSendPorts);
                        break;
                    case "StartReferencedApplications":
                        this.app.Start(OM.ApplicationStartOption.StartReferencedApplications);
                        break;
                }

                this.explorer.SaveChanges();
            }
        }

        private bool CheckExists(string applicationName)
        {
            this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Checking whether Application exists: {0}", applicationName));
            this.app = this.explorer.Applications[applicationName];
            if (this.app != null)
            {
                this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Application exists: {0}", applicationName));
                this.Exists = true;
                return true;
            }

            this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Application does not exist: {0}", applicationName));
            return false;
        }

        private void GetApplications()
        {
            this.Log.LogMessage("Getting Applications");

            this.Applications = new TaskItem[this.explorer.Applications.Count];
            int i = 0;
            foreach (OM.Application a in this.explorer.Applications)
            {
                ITaskItem appl = new TaskItem(a.Name);
                this.Applications[i] = appl;
                i++;
            }
        }
    }
}