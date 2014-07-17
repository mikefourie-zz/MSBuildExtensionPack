//-----------------------------------------------------------------------
// <copyright file="Iis7Website.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Web
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Management;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Web.Administration;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddApplication</i> (<b>Required: </b> Name, Applications <b>Optional: AppPool, EnabledProtocols, AnonymousAuthentication, BasicAuthentication, DigestAuthentication, WindowsAuthentication, sslFlags</b>Force)</para>
    /// <para><i>AddMimeType</i> (<b>Required: </b> Name, MimeTypes)</para>
    /// <para><i>AddResponseHeaders</i> (<b>Required: </b> Name, HttpResponseHeaders)</para>
    /// <para><i>AddVirtualDirectory</i> (<b>Required: </b> Name, VirtualDirectories <b>Optional: </b>Force)</para>
    /// <para><i>CheckExists</i> (<b>Required: </b> Name <b>Output:</b> Exists)</para>
    /// <para><i>CheckVirtualDirectoryExists</i> (<b>Required: </b> Name, VirtualDirectories <b>Output:</b> Exists)</para>
    /// <para><i>Create</i> (<b>Required: </b> Name, Path, Port <b>Optional: </b>Identifier, Force, Applications, VirtualDirectories, AppPool, EnabledProtocols, LogExtFileFlags, LogDirectory, LogFormat, AnonymousAuthentication, BasicAuthentication, DigestAuthentication, WindowsAuthentication, ServerAutoStart, ConnectAsUser, ConnectAsUserPassword)</para>
    /// <para><i>Delete</i> (<b>Required: </b> Name)</para>
    /// <para><i>DeleteVirtualDirectory</i> (<b>Required: </b> Name, VirtualDirectories)</para>
    /// <para><i>GetInfo</i> (<b>Required: </b> Name <b>Output: </b>SiteInfo, SiteId)</para>
    /// <para><i>ModifyPath</i> (<b>Required: </b> Name, Path <b>Output: </b>SiteId)</para>
    /// <para><i>ModifyLogDirectory</i> (<b>Required: </b> Name, LogDirectory)</para>    
    /// <para><i>SetWindowsAuthenticationProviders</i> (<b>Required: </b> Name, Providers <b>Optional: </b>UseKernelMode)</para>
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
    ///             <WindowsAuthentication>true</WindowsAuthentication>
    ///             <AnonymousAuthentication>false</AnonymousAuthentication>
    ///         </Application>
    ///         <Application Include="/photos2">
    ///             <PhysicalPath>C:\photos2</PhysicalPath>
    ///         </Application>
    ///         <VirtualDirectory Include="/photosToo">
    ///             <ApplicationPath>/photos2</ApplicationPath>
    ///             <PhysicalPath>C:\photos2</PhysicalPath>
    ///         </VirtualDirectory>
    ///         <HttpResponseHeaders Include="DemoHeader">
    ///             <Value>DemoHeaderValue</Value>
    ///         </HttpResponseHeaders>
    ///         <MimeTypes Include=".test">
    ///             <Value>test/test1</Value>
    ///         </MimeTypes>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Create a site with a virtual directory -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="Create" Name="NewSite" Path="c:\demo" Port="86" Force="true" Applications="@(Application)" VirtualDirectories="@(VirtualDirectory)">
    ///             <Output TaskParameter="SiteId" PropertyName="NewSiteId"/>
    ///         </MSBuild.ExtensionPack.Web.Iis7Website>
    ///         <Message Text="NewSite SiteId: $(NewSiteId)"/>
    ///         <!-- GetInfo -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="GetInfo" Name="NewSite">
    ///             <Output TaskParameter="SiteInfo" ItemName="SiteInfo"/>
    ///         </MSBuild.ExtensionPack.Web.Iis7Website>
    ///         <Message Text="Id: %(SiteInfo.Id)"/>
    ///         <Message Text="ApplicationPoolName: %(SiteInfo.ApplicationPoolName)"/>
    ///         <!-- Add HTTP Response Headers -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="AddResponseHeaders" Name="NewSite" HttpResponseHeaders="@(HttpResponseHeaders)"/>
    ///         <!-- Add Mime Types -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="AddMimeType" Name="NewSite" MimeTypes="@(MimeTypes)"/>
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
    ///         <!-- Set Windows Authentication Providers -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Website TaskAction="SetWindowsAuthenticationProviders" Name="NewSite" Providers="Negotiate" UseKernelMode="False"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class Iis7Website : BaseTask
    {
        private const string AddApplicationTaskAction = "AddApplication";
        private const string AddMimeTypeTaskAction = "AddMimeType";
        private const string AddVirtualDirectoryTaskAction = "AddVirtualDirectory";
        private const string AddResponseHeadersTaskAction = "AddResponseHeaders";
        private const string CheckExistsTaskAction = "CheckExists";
        private const string CreateTaskAction = "Create";
        private const string DeleteTaskAction = "Delete";
        private const string GetInfoTaskAction = "GetInfo";
        private const string ModifyPathTaskAction = "ModifyPath";
        private const string ModifyLogDirectoryAction = "ModifyLogDirectory";
        private const string StartTaskAction = "Start";
        private const string StopTaskAction = "Stop";
        private const string CheckVirtualDirectoryExistsTaskAction = "CheckVirtualDirectoryExists";
        private const string DeleteVirtualDirectoryTaskAction = "DeleteVirtualDirectory";
        private const string SetWindowsAuthenticationProvidersTaskAction = "SetWindowsAuthenticationProviders";
        private bool anonymousAuthentication = true;
        private bool serverAutoStart = true;
        private bool useKernelMode = true;
        private ServerManager iisServerManager;
        private Site website;

        /// <summary>
        /// Sets the name of the Website
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// ITaskItem of Applications. Use AppPool, PhysicalPath and EnabledProtocols metadata to specify applicable values
        /// </summary>
        public ITaskItem[] Applications { get; set; }
        
        /// <summary>
        /// ITaskItem of VirtualDirectories. Use PhysicalPath metadata to specify applicable values
        /// </summary>
        public ITaskItem[] VirtualDirectories { get; set; }

        /// <summary>
        /// A collection of headers to add. Specify Identity as name and add Value metadata
        /// </summary>
        public ITaskItem[] HttpResponseHeaders { get; set; }

        /// <summary>
        /// A collection of MimeTypes. Specify Identity as name and add Value metadata
        /// </summary>
        public ITaskItem[] MimeTypes { get; set; }

        /// <summary>
        /// Sets the path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Sets the user of the application (instead of pass-throught authentication).
        /// </summary>
        public string ConnectAsUser { get; set; }

        /// <summary>
        /// Sets the password of the application user  (instead of pass-throught authentication).
        /// </summary>
        public string ConnectAsUserPassword { get; set; }

        /// <summary>
        /// Sets the directory the website writes logfiles to.
        /// </summary>
        public ITaskItem LogDirectory { get; set; }

        /// <summary>
        /// Sets the app pool.
        /// </summary>
        public string AppPool { get; set; }

        /// <summary>
        /// Sets the Enabled Protocols for the website
        /// </summary>
        public string EnabledProtocols { get; set; }

        /// <summary>
        /// Sets the SSl Flag for the website
        /// </summary>
        public string SslFlags { get; set; }

        /// <summary>
        /// Sets AnonymousAuthentication for the website. Default is true
        /// </summary>
        public bool AnonymousAuthentication
        {
            get { return this.anonymousAuthentication; }
            set { this.anonymousAuthentication = value; }
        }

        /// <summary>
        /// Sets DigestAuthentication for the website. Default is false;
        /// </summary>
        public bool DigestAuthentication { get; set; }

        /// <summary>
        /// Sets BasicAuthentication for the website. Default is false;
        /// </summary>
        public bool BasicAuthentication { get; set; }

        /// <summary>
        /// Sets ServerAutoStart for the website. Default is true.
        /// </summary>
        public bool ServerAutoStart
        {
            get { return this.serverAutoStart; }
            set { this.serverAutoStart = value; }
        }

        /// <summary>
        /// Sets whether Windows authentication is done in kernel mode. Default is true.
        /// </summary>
        public bool UseKernelMode
        {
            get { return this.useKernelMode; }
            set { this.useKernelMode = value; }
        }

        /// <summary>
        /// Sets the authentication providers. The authentication providers must be passed in a semicolon separated string.
        /// </summary>
        public string Providers { get; set; }

        /// <summary>
        /// Sets WindowsAuthentication for the website. Default is false;
        /// </summary>
        public bool WindowsAuthentication { get; set; }

        /// <summary>
        /// Sets the port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Sets the LogExtFileFlags. Default is 1414 (logextfiletime | logextfileclientip |logextfilemethod | logextfileuristem | logextfilehttpstatus)
        /// </summary>
        public string LogExtFileFlags { get; set; }

        /// <summary>
        /// Sets the LogExtFileFlags. Default is W3c.
        /// </summary>
        public string LogFormat { get; set; }

        /// <summary>
        /// Set to true to force the creation of a website, even if it exists.
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Gets the site id. [Output]
        /// </summary>
        [Output]
        public long SiteId { get; set; }

        /// <summary>
        /// Gets the SiteInfo Item. Identity = Name, MetaData = ApplicationPoolName, PhysicalPath, Id, State
        /// </summary>
        [Output]
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
        public bool Exists { get; set; }

        /// <summary>
        /// Gets or sets the Identifier for the website. If specified for Create and the Identifier already exists, an error is logged.
        /// </summary>
        [Output]
        public int Identifier { get; set; }

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
                    case AddResponseHeadersTaskAction:
                        this.AddResponseHeaders();
                        break;
                    case AddApplicationTaskAction:
                        this.AddApplication();
                        break;
                    case AddMimeTypeTaskAction:
                        this.AddMimeType();
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
                    case ModifyLogDirectoryAction:
                        this.ModifyLogDirectory();
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
                    case SetWindowsAuthenticationProvidersTaskAction:
                        this.SetWindowsAuthenticationProviders();
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

        private void SetWindowsAuthenticationProviders()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            if (string.IsNullOrWhiteSpace(this.Providers))
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "No authentication providers were specified for website {0} on {1}", this.Name, this.MachineName));
                return;
            }

            string[] providers = this.Providers.Trim().Split(new[] { ';' });
            Configuration config = this.iisServerManager.GetApplicationHostConfiguration();
            ConfigurationSection windowsAuthenticationSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication", this.Name);
            ConfigurationElementCollection providersCollection = windowsAuthenticationSection.GetCollection("providers");

            for (int index = providersCollection.Count - 1; index >= 0; index--)
            {
                var existingProvider = providersCollection[index];
                if (!providers.Contains(existingProvider["value"]))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing provider {0}", existingProvider["value"]));
                    providersCollection.Remove(existingProvider);
                }
                else
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Keeping provider {0}", existingProvider["value"]));
                }
            }

            windowsAuthenticationSection["useKernelMode"] = this.UseKernelMode;            
            this.iisServerManager.CommitChanges();
        }        

        private void AddMimeType()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            Configuration config = this.iisServerManager.GetWebConfiguration(this.Name);
            foreach (ITaskItem mimetype in this.MimeTypes)
            {
                ConfigurationSection staticContentSection = config.GetSection("system.webServer/staticContent");
                ConfigurationElementCollection staticContentCollection = staticContentSection.GetCollection();
                ConfigurationElement mimeMapElement = staticContentCollection.CreateElement("mimeMap");
                mimeMapElement["fileExtension"] = mimetype.ItemSpec;
                mimeMapElement["mimeType"] = mimetype.GetMetadata("Value");
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding MimeType: {0} to: {1} on: {2}", mimetype.ItemSpec, this.Name, this.MachineName));
                bool typeExists = staticContentCollection.Any(obj => obj.Attributes["fileExtension"].Value.ToString() == mimetype.ItemSpec);
                if (!typeExists)
                {
                    staticContentCollection.Add(mimeMapElement);
                    this.iisServerManager.CommitChanges();
                }
            }
        }

        private void AddResponseHeaders()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            Configuration config = this.iisServerManager.GetWebConfiguration(this.Name);
            ConfigurationSection httpProtocolSection = config.GetSection("system.webServer/httpProtocol");
            ConfigurationElementCollection customHeadersCollection = httpProtocolSection.GetCollection("customHeaders");
            foreach (ITaskItem header in this.HttpResponseHeaders)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding HttpResponseHeader: {0} to: {1} on: {2}", header.ItemSpec, this.Name, this.MachineName));
                ConfigurationElement addElement = customHeadersCollection.CreateElement("add");
                addElement["name"] = header.ItemSpec;
                addElement["value"] = header.GetMetadata("Value");
                bool headerExists = customHeadersCollection.Any(obj => obj.Attributes["name"].Value.ToString() == header.ItemSpec);
                if (!headerExists)
                {
                    customHeadersCollection.Add(addElement);
                    this.iisServerManager.CommitChanges();
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
                }

                this.iisServerManager.CommitChanges();
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

        private bool ApplicationExists(string name)
        {
            return this.website.Applications[name] != null;
        }

        private void ProcessApplications()
        {
            foreach (ITaskItem app in this.Applications)
            {
                string physicalPath = System.IO.Path.GetFullPath(app.GetMetadata("PhysicalPath")); 
                this.CreateDirectoryIfNecessary(physicalPath);
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding Application: {0}", app.ItemSpec));

                if (this.ApplicationExists(app.ItemSpec))
                {
                    if (!this.Force)
                    {
                        Log.LogError(string.Format(CultureInfo.CurrentCulture, "The application: {0} already exists on: {1}. Use Force=\"true\" to remove the existing application.", app.ItemSpec, this.Name));
                        return;
                    }

                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "...Removing existing Application: {0}", app.ItemSpec));

                    this.website.Applications[app.ItemSpec].Delete();
                    this.iisServerManager.CommitChanges();
                    this.website = this.iisServerManager.Sites[this.Name];
                }

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

                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting additional settings for Application: {0}", app.ItemSpec));

                // Set the authentification parameters
                Configuration config = this.iisServerManager.GetApplicationHostConfiguration();
                if (!string.IsNullOrEmpty(this.SslFlags))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting SslFlags for Application: {0} - {1}", app.ItemSpec, this.SslFlags));
                    ConfigurationSection accessSection = config.GetSection("system.webServer/security/access", this.Name + app.ItemSpec);
                    accessSection["sslFlags"] = this.SslFlags;
                }

                if (!string.IsNullOrEmpty(app.GetMetadata("WindowsAuthentication")))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting WindowsAuthentication for Application: {0}", app.ItemSpec));
                    ConfigurationSection windowsAuthenticationSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication", this.Name + app.ItemSpec);
                    windowsAuthenticationSection["enabled"] = app.GetMetadata("WindowsAuthentication");
                }

                if (!string.IsNullOrEmpty(app.GetMetadata("AnonymousAuthentication")))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting AnonymousAuthentication for Application: {0}", app.ItemSpec));
                    ConfigurationSection anonyAuthentication = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", this.Name + app.ItemSpec);
                    anonyAuthentication["enabled"] = app.GetMetadata("AnonymousAuthentication");
                }

                if (!string.IsNullOrEmpty(app.GetMetadata("DigestAuthentication")))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting DigestAuthentication for Application: {0}", app.ItemSpec));
                    ConfigurationSection digestAuthentication = config.GetSection("system.webServer/security/authentication/digestAuthentication", this.Name + app.ItemSpec);
                    digestAuthentication["enabled"] = app.GetMetadata("DigestAuthentication");
                }

                if (!string.IsNullOrEmpty(app.GetMetadata("BasicAuthentication")))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting BasicAuthentication for Application: {0}", app.ItemSpec));
                    ConfigurationSection basicAuthentication = config.GetSection("system.webServer/security/authentication/basicAuthentication", this.Name + app.ItemSpec);
                    basicAuthentication["enabled"] = app.GetMetadata("BasicAuthentication");
                }
            }
        }

        private void CreateDirectoryIfNecessary(string directoryPath)
        {
            if (!this.TargetingLocalMachine(true))
            {
                this.GetManagementScope(@"\root\cimv2");

                // we need to operate remotely
                string fullQuery = @"Select * From Win32_Directory Where Name = '" + directoryPath.Replace("\\", "\\\\") + "'";
                ObjectQuery query1 = new ObjectQuery(fullQuery);
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.Scope, query1))
                {
                    ManagementObjectCollection queryCollection = searcher.Get();
                    if (queryCollection.Count == 0)
                    {
                        this.LogTaskMessage(MessageImportance.Low, "Attempting to create remote folder for share");
                        ManagementPath path2 = new ManagementPath("Win32_Process");
                        using (ManagementClass managementClass2 = new ManagementClass(this.Scope, path2, null))
                        {
                            ManagementBaseObject inParams1 = managementClass2.GetMethodParameters("Create");
                            string tex = "cmd.exe /c md \"" + directoryPath + "\"";
                            inParams1["CommandLine"] = tex;

                            ManagementBaseObject outParams1 = managementClass2.InvokeMethod("Create", inParams1, null);
                            if (outParams1 != null)
                            {
                                uint rc = Convert.ToUInt32(outParams1.Properties["ReturnValue"].Value, CultureInfo.InvariantCulture);
                                if (rc != 0)
                                {
                                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Non-zero return code attempting to create remote share location: {0}", rc));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // we are working locally
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);

                    // adding a sleep as it may take a while to register.
                    System.Threading.Thread.Sleep(1000);
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
            this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.CurrentCulture, "Creating website: {0} on: {1}", this.Name, this.MachineName));
            if (this.SiteExists())
            {
                if (!this.Force)
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} already exists on: {1}", this.Name, this.MachineName));
                    return;
                }

                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Website exists. Deleting website: {0} on: {1}", this.Name, this.MachineName));
                this.iisServerManager.Sites.Remove(this.website);
                this.iisServerManager.CommitChanges();
            }

            this.CreateDirectoryIfNecessary(this.Path);

            this.website = this.iisServerManager.Sites.Add(this.Name, this.Path, this.Port);            
            if (this.Identifier > 0)
            {    
                this.website.Id = this.Identifier;
            }

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

            if (!string.IsNullOrEmpty(this.ConnectAsUser))
            {
                this.website.VirtualDirectoryDefaults.UserName = this.ConnectAsUser;
            }

            if (!string.IsNullOrEmpty(this.ConnectAsUserPassword))
            {
                this.website.VirtualDirectoryDefaults.Password = this.ConnectAsUserPassword;
            }

            if (!string.IsNullOrEmpty(this.LogExtFileFlags))
            {
                this.website.LogFile.LogExtFileFlags = (LogExtFileFlags)Enum.Parse(typeof(LogExtFileFlags), this.LogExtFileFlags);
            }

            if (!string.IsNullOrEmpty(this.LogFormat))
            {
                this.website.LogFile.LogFormat = (LogFormat)Enum.Parse(typeof(LogFormat), this.LogFormat);
            }

            if (this.LogDirectory != null)
            {
                this.CreateDirectoryIfNecessary(this.LogDirectory.ItemSpec);
                this.website.LogFile.Directory = this.LogDirectory.ItemSpec;
            }
            
            this.website.ServerAutoStart = this.serverAutoStart;
      
            Configuration config = this.iisServerManager.GetApplicationHostConfiguration();
            ConfigurationSection windowsAuthenticationSection = config.GetSection("system.webServer/security/authentication/windowsAuthentication", this.Name);
            windowsAuthenticationSection["enabled"] = this.WindowsAuthentication;
            ConfigurationSection anonyAuthentication = config.GetSection("system.webServer/security/authentication/anonymousAuthentication", this.Name);
            anonyAuthentication["enabled"] = this.AnonymousAuthentication;
            ConfigurationSection digestAuthentication = config.GetSection("system.webServer/security/authentication/digestAuthentication", this.Name);
            digestAuthentication["enabled"] = this.DigestAuthentication;
            ConfigurationSection basicAuthentication = config.GetSection("system.webServer/security/authentication/basicAuthentication", this.Name);
            basicAuthentication["enabled"] = this.BasicAuthentication;
            this.iisServerManager.CommitChanges();
            this.SiteId = this.website.Id;
        }

        private void ProcessVirtualDirectories()
        {
            foreach (ITaskItem virDir in this.VirtualDirectories)
            {
                string physicalPath = virDir.GetMetadata("PhysicalPath");
                this.CreateDirectoryIfNecessary(physicalPath);

                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding VirtualDirectory: {0} to: {1}", virDir.ItemSpec, virDir.GetMetadata("ApplicationPath")));
                if (this.website.Applications[virDir.GetMetadata("ApplicationPath")].VirtualDirectories.Any(v => v.Path.Equals(virDir.ItemSpec.ToUpperInvariant(), StringComparison.CurrentCultureIgnoreCase)))
                {
                    if (!this.Force)
                    {
                        Log.LogError(string.Format(CultureInfo.CurrentCulture, "The VirtualDirectory: {0} already exists on: {1}. Use Force=\"true\" to remove the existing VirtualDirectory.", virDir.ItemSpec, virDir.GetMetadata("ApplicationPath")));
                        return;
                    }

                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "...Removing existing VirtualDirectory: {0}", virDir.ItemSpec));

                    this.website.Applications[virDir.GetMetadata("ApplicationPath")].VirtualDirectories.Remove(this.website.Applications[virDir.GetMetadata("ApplicationPath")].VirtualDirectories[virDir.ItemSpec]);
                    this.iisServerManager.CommitChanges();
                    this.website = this.iisServerManager.Sites[this.Name];
                }

                VirtualDirectory virtualDirectory = this.website.Applications[virDir.GetMetadata("ApplicationPath")].VirtualDirectories.Add(virDir.ItemSpec, physicalPath);
                if (!string.IsNullOrEmpty(virDir.GetMetadata("UserName")))
                {
                    virtualDirectory.LogonMethod = AuthenticationLogonMethod.Batch;
                    virtualDirectory.UserName = virDir.GetMetadata("UserName");
                    virtualDirectory.Password = virDir.GetMetadata("Password");
                }
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
            this.CreateDirectoryIfNecessary(this.Path);
            
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

        private void ModifyLogDirectory()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Modifying LogDirectory for website: {0} on: {1}", this.Name, this.MachineName));
            this.CreateDirectoryIfNecessary(this.LogDirectory.ItemSpec);
            this.website.LogFile.Directory = this.LogDirectory.ItemSpec;
            this.iisServerManager.CommitChanges();
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
            if (this.website.Applications[0] != null)
            {
                isite.SetMetadata("ApplicationPoolName", this.website.Applications[0].ApplicationPoolName);
            }

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