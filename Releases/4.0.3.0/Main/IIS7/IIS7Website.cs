//-----------------------------------------------------------------------
// <copyright file="Iis7Website.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Web
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Web.Administration;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddApplication</i> (<b>Required: </b> Name, Applications)</para>
    /// <para><i>AddVirtualDirectory</i> (<b>Required: </b> Name, VirtualDirectories)</para>
    /// <para><i>CheckExists</i> (<b>Required: </b> Name <b>Output:</b> Exists)</para>
    /// <para><i>CheckVirtualDirectoryExists</i> (<b>Required: </b> Name, VirtualDirectories <b>Output:</b> Exists)</para>
    /// <para><i>Create</i> (<b>Required: </b> Name, Path, Port <b>Optional: </b>Force, Applications, VirtualDirectories, AppPool, EnabledProtocols)</para>
    /// <para><i>Delete</i> (<b>Required: </b> Name)</para>
    /// <para><i>DeleteVirtualDirectory</i> (<b>Required: </b> Name, VirtualDirectories)</para>
    /// <para><i>GetInfo</i> (<b>Required: </b> Name <b>Output: </b>SiteInfo, SiteId)</para>
    /// <para><i>ModifyPath</i> (<b>Required: </b> Name, Path <b>Output: </b>SiteId)</para>
    /// <para><i>Start</i> (<b>Required: </b> Name)</para>
    /// <para><i>Stop</i> (<b>Required: </b> Name)</para>
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
    ///     <ItemGroup>
    ///         <Application Include="/photos">
    ///             <PhysicalPath>C:\photos</PhysicalPath>
    ///             <AppPool>NewAppPool100</AppPool>
    ///         </Application>
    ///         <Application Include="/photos2">
    ///             <PhysicalPath>C:\photos2</PhysicalPath>
    ///         </Application>
    ///         <VirtualDirectory Include="/photosToo">
    ///             <ApplicationPath>/photos2</ApplicationPath>
    ///             <PhysicalPath>C:\photos2</PhysicalPath>
    ///         </VirtualDirectory>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Create a site with a virtual directory -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="Create" Name="NewSite" Path="c:\demo" Port="86" Force="true" Applications="@(Application)" VirtualDirectories="@(VirtualDirectory)">
    ///             <Output TaskParameter="SiteId" PropertyName="NewSiteId"/>
    ///         </MSBuild.ExtensionPack.Web.Iis7Website>
    ///         <Message Text="NewSite SiteId: $(NewSiteId)"/>
    ///         <!-- Check whether the virtual directory exists -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="CheckVirtualDirectoryExists" Name="NewSite" VirtualDirectories="@(VirtualDirectory)">
    ///             <Output TaskParameter="Exists" PropertyName="VDirExists"/>
    ///         </MSBuild.ExtensionPack.Web.Iis7Website>
    ///         <Message Text="VDirExists Exists: $(VDirExists)"/>
    ///         <!-- Start a site -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="Start" Name="NewSite2"/>
    ///         <!-- Check if the site exists -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="CheckExists" Name="NewSite2">
    ///             <Output TaskParameter="Exists" PropertyName="SiteExists"/>
    ///         </MSBuild.ExtensionPack.Web.Iis7Website>
    ///         <Message Text="NewSite2 SiteExists: $(SiteExists)"/>
    ///         <!-- Create a basic site -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="Create" Name="NewSite2" Path="c:\demo2" Port="84" Force="true">
    ///             <Output TaskParameter="SiteId" PropertyName="NewSiteId2"/>
    ///         </MSBuild.ExtensionPack.Web.Iis7Website>
    ///         <Message Text="NewSite2 SiteId: $(NewSiteId2)"/>
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="CheckExists" Name="NewSite2">
    ///             <Output TaskParameter="Exists" PropertyName="SiteExists"/>
    ///         </MSBuild.ExtensionPack.Web.Iis7Website>
    ///         <Message Text="NewSite2 SiteExists: $(SiteExists)"/>
    ///         <!-- Stop a site -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="Stop" Name="NewSite2"/>
    ///         <!-- Start a site -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="Start" Name="NewSite2"/>
    ///         <!-- Delete a site -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="Delete" Name="NewSite2"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.3.0/html/243a8320-e40b-b525-07d6-76fc75629364.htm")]
    public class Iis7Website : BaseTask
    {
        private const string AddApplicationTaskAction = "AddApplication";
        private const string AddVirtualDirectoryTaskAction = "AddVirtualDirectory";
        private const string CheckExistsTaskAction = "CheckExists";
        private const string CreateTaskAction = "Create";
        private const string DeleteTaskAction = "Delete";
        private const string GetInfoTaskAction = "GetInfo";
        private const string ModifyPathTaskAction = "ModifyPath";
        private const string StartTaskAction = "Start";
        private const string StopTaskAction = "Stop";
        private const string CheckVirtualDirectoryExistsTaskAction = "CheckVirtualDirectoryExists";
        private const string DeleteVirtualDirectoryTaskAction = "DeleteVirtualDirectory";

        private ServerManager iisServerManager;
        private Site website;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(AddApplicationTaskAction)]
        [DropdownValue(AddVirtualDirectoryTaskAction)]
        [DropdownValue(CheckExistsTaskAction)]
        [DropdownValue(CreateTaskAction)]
        [DropdownValue(DeleteTaskAction)]
        [DropdownValue(GetInfoTaskAction)]
        [DropdownValue(ModifyPathTaskAction)]
        [DropdownValue(StartTaskAction)]
        [DropdownValue(StopTaskAction)]
        [DropdownValue(CheckVirtualDirectoryExistsTaskAction)]
        [DropdownValue(DeleteVirtualDirectoryTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the name of the Website
        /// </summary>
        [Required]
        [TaskAction(AddApplicationTaskAction, true)]
        [TaskAction(AddVirtualDirectoryTaskAction, true)]
        [TaskAction(CheckExistsTaskAction, true)]
        [TaskAction(CreateTaskAction, true)]
        [TaskAction(DeleteTaskAction, true)]
        [TaskAction(GetInfoTaskAction, true)]
        [TaskAction(ModifyPathTaskAction, true)]
        [TaskAction(StartTaskAction, true)]
        [TaskAction(StopTaskAction, true)]
        [TaskAction(CheckVirtualDirectoryExistsTaskAction, true)]
        [TaskAction(DeleteVirtualDirectoryTaskAction, true)]
        public string Name { get; set; }

        /// <summary>
        /// ITaskItem of Applications. Use AppPool, PhysicalPath and EnabledProtocols metadata to specify applicable values
        /// </summary>
        [TaskAction(AddApplicationTaskAction, true)]
        [TaskAction(CreateTaskAction, false)]
        public ITaskItem[] Applications { get; set; }

        /// <summary>
        /// ITaskItem of VirtualDirectories. Use PhysicalPath metadata to specify applicable values
        /// </summary>
        [TaskAction(AddVirtualDirectoryTaskAction, true)]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(CheckVirtualDirectoryExistsTaskAction, true)]
        [TaskAction(DeleteVirtualDirectoryTaskAction, true)]
        public ITaskItem[] VirtualDirectories { get; set; }

        /// <summary>
        /// Sets the path.
        /// </summary>
        [TaskAction(CreateTaskAction, true)]
        public string Path { get; set; }

        /// <summary>
        /// Sets the app pool.
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public string AppPool { get; set; }

        /// <summary>
        /// Sets the Enabled Protocols for the website
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public string EnabledProtocols { get; set; }

        /// <summary>
        /// Sets the port.
        /// </summary>
        [TaskAction(CreateTaskAction, true)]
        public int Port { get; set; }

        /// <summary>
        /// Set to true to force the creation of a website, even if it exists.
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public bool Force { get; set; }

        /// <summary>
        /// Gets the site id. [Output]
        /// </summary>
        [Output]
        [TaskAction(GetInfoTaskAction, false)]
        public long SiteId { get; set; }

        /// <summary>
        /// Gets the SiteInfo Item. Identity = Name, MetaData = ApplicationPoolName, PhysicalPath, Id, State
        /// </summary>
        [Output]
        [TaskAction(GetInfoTaskAction, false)]
        public ITaskItem SiteInfo { get; set; }

        /// <summary>
        /// Gets the site physical path. [Output]
        /// </summary>
        [Output]
        public string PhysicalPath { get; set; }

        /// <summary>
        /// Gets whether the website exists
        /// </summary>
        [Output]
        [TaskAction(CheckExistsTaskAction, false)]
        public bool Exists { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        protected override void InternalExecute()
        {
            try
            {
                this.iisServerManager = System.Environment.MachineName != this.MachineName ? ServerManager.OpenRemote(this.MachineName) : new ServerManager();

                switch (this.TaskAction)
                {
                    case AddApplicationTaskAction:
                        this.AddApplication();
                        break;
                    case AddVirtualDirectoryTaskAction:
                        this.AddVirtualDirectory();
                        break;
                    case CreateTaskAction:
                        this.Create();
                        break;
                    case ModifyPathTaskAction:
                        this.ModifyPath();
                        break;
                    case GetInfoTaskAction:
                        this.GetInfo();
                        break;
                    case DeleteTaskAction:
                        this.Delete();
                        break;
                    case CheckExistsTaskAction:
                        this.CheckExists();
                        break;
                    case CheckVirtualDirectoryExistsTaskAction:
                        this.CheckVirtualDirectoryExists();
                        break;
                    case DeleteVirtualDirectoryTaskAction:
                        this.DeleteVirtualDirectory();
                        break;
                    case StartTaskAction:
                    case StopTaskAction:
                        this.ControlWebsite();
                        break;
                    default:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                        return;
                }
            }
            finally
            {
                if (this.iisServerManager != null)
                {
                    this.iisServerManager.Dispose();
                }
            }
        }

        private void CheckVirtualDirectoryExists()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            if (this.VirtualDirectories != null)
            {
                foreach (ITaskItem virDir in this.VirtualDirectories)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking whether VirtualDirectory: {0} exists on: {1}", virDir.ItemSpec, virDir.GetMetadata("ApplicationPath")));
                    if (this.website.Applications[virDir.GetMetadata("ApplicationPath")].VirtualDirectories.Any(v => v.Path.Equals(virDir.ItemSpec.ToUpperInvariant(), StringComparison.CurrentCultureIgnoreCase)))
                    {
                        this.Exists = true;
                        return;
                    }
                }
            }
        }

        private void DeleteVirtualDirectory()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            if (this.VirtualDirectories != null)
            {
                foreach (ITaskItem virDir in this.VirtualDirectories.Where(virDir => this.website.Applications[virDir.GetMetadata("ApplicationPath")].VirtualDirectories.Any(v => v.Path.Equals(virDir.ItemSpec, StringComparison.CurrentCultureIgnoreCase))))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing VirtualDirectory: {0} from: {1}", virDir.ItemSpec, virDir.GetMetadata("ApplicationPath")));
                    this.website.Applications[virDir.GetMetadata("ApplicationPath")].VirtualDirectories.Remove(this.website.Applications[virDir.GetMetadata("ApplicationPath")].VirtualDirectories[virDir.ItemSpec]);
                    this.iisServerManager.CommitChanges();
                    break;
                }
            }
        }

        private void CheckExists()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking whether website: {0} exists on: {1}", this.Name, this.MachineName));
            this.Exists = this.SiteExists();
        }

        private void AddApplication()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            if (this.Applications != null)
            {
                this.ProcessApplications();
                this.iisServerManager.CommitChanges();
            }
        }

        private void ProcessApplications()
        {
            foreach (ITaskItem app in this.Applications)
            {
                string physicalPath = app.GetMetadata("PhysicalPath");
                if (!Directory.Exists(physicalPath))
                {
                    Directory.CreateDirectory(physicalPath);
                }

                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding Application: {0}", app.ItemSpec));
                this.website.Applications.Add(app.ItemSpec, physicalPath);

                // Set Application Pool if given
                if (!string.IsNullOrEmpty(app.GetMetadata("AppPool")))
                {
                    ApplicationPool pool = this.iisServerManager.ApplicationPools[app.GetMetadata("AppPool")];
                    if (pool == null)
                    {
                        Log.LogError(string.Format(CultureInfo.CurrentCulture, "The Application Pool: {0} specified for: {1} was not found", app.GetMetadata("AppPool"), app.ItemSpec));
                        return;
                    }

                    this.website.Applications[app.ItemSpec].ApplicationPoolName = app.GetMetadata("AppPool");
                }

                // Set EnabledProtocols if given
                if (!string.IsNullOrEmpty(app.GetMetadata("EnabledProtocols")))
                {
                    this.website.Applications[app.ItemSpec].EnabledProtocols = app.GetMetadata("EnabledProtocols");
                }
            }
        }

        private void AddVirtualDirectory()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            if (this.VirtualDirectories != null)
            {
                this.ProcessVirtualDirectories(); 
                this.iisServerManager.CommitChanges();
            }
        }

        private void Delete()
        {
            if (!this.SiteExists())
            {
                this.LogTaskWarning(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting website: {0} on: {1}", this.Name, this.MachineName));
            this.iisServerManager.Sites.Remove(this.website);
            this.iisServerManager.CommitChanges();
        }

        private void ControlWebsite()
        {
            if (!this.SiteExists())
            {
                this.LogTaskWarning(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            switch (this.TaskAction)
            {
                case "Start":
                    this.website.Start();
                    break;
                case "Stop":
                    this.website.Stop();
                    break;
            }
        }

        private void Create()
        {
            if (this.SiteExists())
            {
                if (!this.Force)
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} already exists on: {1}", this.Name, this.MachineName));
                    return;
                }

                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting website: {0} on: {1}", this.Name, this.MachineName));
                this.iisServerManager.Sites.Remove(this.website);
                this.iisServerManager.CommitChanges();
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating website: {0} on: {1}", this.Name, this.MachineName));
            if (!Directory.Exists(this.Path))
            {
                Directory.CreateDirectory(this.Path);
            }

            this.website = this.iisServerManager.Sites.Add(this.Name, this.Path, this.Port);
            if (!string.IsNullOrEmpty(this.AppPool))
            {
                this.website.ApplicationDefaults.ApplicationPoolName = this.AppPool;
            }

            if (this.Applications != null)
            {
                this.ProcessApplications();
            }

            if (this.VirtualDirectories != null)
            {
                this.ProcessVirtualDirectories();
            }

            if (!string.IsNullOrEmpty(this.EnabledProtocols))
            {
               this.website.ApplicationDefaults.EnabledProtocols = this.EnabledProtocols;
            }

            this.iisServerManager.CommitChanges();
            this.SiteId = this.website.Id;
        }

        private void ProcessVirtualDirectories()
        {
            foreach (ITaskItem virDir in this.VirtualDirectories)
            {
                string physicalPath = virDir.GetMetadata("PhysicalPath");
                if (!Directory.Exists(physicalPath))
                {
                    Directory.CreateDirectory(physicalPath);
                }

                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding VirtualDirectory: {0} to: {1}", virDir.ItemSpec, virDir.GetMetadata("ApplicationPath")));
                this.website.Applications[virDir.GetMetadata("ApplicationPath")].VirtualDirectories.Add(virDir.ItemSpec, physicalPath);
            }
        }

        private void ModifyPath()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Modifying website: {0} on: {1}", this.Name, this.MachineName));
            if (!Directory.Exists(this.Path))
            {
                Directory.CreateDirectory(this.Path);
            }

            Application app = this.website.Applications["/"];
            if (app != null)
            {
                VirtualDirectory vdir = app.VirtualDirectories["/"];
                if (vdir != null)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting physical path: {0} on: {1}", this.Path, vdir.Path));
                    vdir.PhysicalPath = this.Path;
                }
            }

            this.iisServerManager.CommitChanges();
            this.SiteId = this.website.Id;
        }

        private void GetInfo()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} does not exist on: {1}", this.Name, this.MachineName));
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting info for website: {0} on: {1}", this.Name, this.MachineName));
            ITaskItem isite = new TaskItem(this.Name);

            isite.SetMetadata("ApplicationPoolName", this.website.ApplicationDefaults.ApplicationPoolName);
            Application app = this.website.Applications["/"];
            if (app != null)
            {
                VirtualDirectory vdir = app.VirtualDirectories["/"];
                if (vdir != null)
                {
                    isite.SetMetadata("PhysicalPath", vdir.PhysicalPath);
                    this.PhysicalPath = vdir.PhysicalPath;
                }
            }

            isite.SetMetadata("Id", this.website.Id.ToString(CultureInfo.CurrentCulture));
            isite.SetMetadata("State", this.website.State.ToString());
            this.SiteInfo = isite;
            this.SiteId = this.website.Id;
        }
    
        private bool SiteExists()
        {
            this.website = this.iisServerManager.Sites[this.Name];
            return this.website != null;
        }
    }
}
