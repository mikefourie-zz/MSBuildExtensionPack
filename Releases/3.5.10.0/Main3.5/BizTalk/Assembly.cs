//-----------------------------------------------------------------------
// <copyright file="Assembly.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.BizTalk
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Management;
    using Microsoft.Build.Framework;
    using Microsoft.XLANGs.BaseTypes;
    using MSBuild.ExtensionPack.Framework;
    using Deployment = Microsoft.BizTalk.ApplicationDeployment;
    using Reflection = System.Reflection;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Add</i> (<b>Required: </b>Application, Assemblies <b>Optional: </b>MachineName, DatabaseServer, DeploymentPath, Database, Gac, Force, GacOnAddResource, GacOnMSIFileImport, GacOnMSIFileInstall, DestinationLocation) </para>
    /// <para><i>Remove</i> (<b>Required: </b>Application, Assemblies <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>CheckExists</i> (<b>Required: </b>Application, Assemblies <b>Optional: </b>MachineName, DatabaseServer, Database <b>Output: </b>Exists)</para>
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
    ///             <BizTalkAssemblies Include="YOURASSEMBLY"/>
    ///         </ItemGroup>
    ///         <!-- Add the Assemblies -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkAssembly TaskAction="Add" Force="true" Gac="true" MachineName="$(COMPUTERNAME)" Application="BizTalk Application 1" Assemblies="@(BizTalkAssemblies)" />
    ///         <!-- Check an Assembly Exists -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkAssembly TaskAction="CheckExists" MachineName="$(COMPUTERNAME)" Application="BizTalk Application 1" Assemblies="@(BizTalkAssemblies)">
    ///             <Output TaskParameter="Exists" PropertyName="AssemblyExists" />
    ///         </MSBuild.ExtensionPack.BizTalk.BizTalkAssembly>
    ///         <Message Text="ASSEMBLY EXISTS: $(AssemblyExists)"/>
    ///         <!-- Remove the Assemblies -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkAssembly TaskAction="Remove" MachineName="$(COMPUTERNAME)" Application="BizTalk Application 1" Assemblies="@(BizTalkAssemblies)" />
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.9.0/html/fda37dc3-683d-7a9e-226c-4fad63709c02.htm")]
    public class BizTalkAssembly : BaseTask
    {
        private const string CheckExistsTaskAction = "CheckExists";
        private const string AddTaskAction = "Add";
        private const string RemoveTaskAction = "Remove";
        private string database = "BizTalkMgmtDb";
        private List<BizTalkResource> resources = new List<BizTalkResource>();
        private bool gac = true;
        private bool gacOnAddResource = true;
        private bool gacOnMsiFileImport = true;

        /// <summary>
        /// Sets whether to GAC the assembly on file install. Default is false
        /// </summary>
        public bool GacOnMsiFileInstall { get; set; }

        /// <summary>
        /// Sets whether to GAC the assembly on file import. Default is true
        /// </summary>
        public bool GacOnMsiFileImport
        {
            get { return this.gacOnMsiFileImport; }
            set { this.gacOnMsiFileImport = value; }
        }

        /// <summary>
        /// Sets whether to GAC the assembly on add resource. Default is true
        /// </summary>
        public bool GacOnAddResource
        {
            get { return this.gacOnAddResource; }
            set { this.gacOnAddResource = value; }
        }

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(AddTaskAction)]
        [DropdownValue(CheckExistsTaskAction)]
        [DropdownValue(RemoveTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the MachineName.
        /// </summary>
        [TaskAction(AddTaskAction, false)]
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(RemoveTaskAction, false)]
        public override string MachineName
        {
            get { return base.MachineName; }
            set { base.MachineName = value; }
        }

        /// <summary>
        /// Sets the DatabaseServer to connect to. Default is MachineName
        /// </summary>
        [TaskAction(AddTaskAction, false)]
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(RemoveTaskAction, false)]
        public string DatabaseServer { get; set; }

        /// <summary>
        /// Sets the DestinationLocation
        /// </summary>
        [TaskAction(AddTaskAction, false)]
        public string DestinationLocation { get; set; }

        /// <summary>
        /// Sets the Application Name
        /// </summary>
        [Required]
        [TaskAction(AddTaskAction, true)]
        [TaskAction(CheckExistsTaskAction, true)]
        [TaskAction(RemoveTaskAction, true)]
        public string Application { get; set; }

        /// <summary>
        /// Sets the deployment path for assemblies
        /// </summary>
        [TaskAction(AddTaskAction, false)]
        public string DeploymentPath { get; set; }

        /// <summary>
        /// Sets the Management Database to connect to. Default is BizTalkMgmtDb
        /// </summary>
        [TaskAction(AddTaskAction, false)]
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(RemoveTaskAction, false)]
        public string Database
        {
            get { return this.database; }
            set { this.database = value; }
        }

        /// <summary>
        /// Sets the list of Assemblies
        /// </summary>
        [Required]
        [TaskAction(AddTaskAction, true)]
        [TaskAction(CheckExistsTaskAction, true)]
        [TaskAction(RemoveTaskAction, true)]
        public ITaskItem[] Assemblies { get; set; }

        /// <summary>
        /// Gets whether the assembly / assemblies exist in BizTalk
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Set to true to gac the biztalk assemblies. Default is true. Note that if you set GacOnMSIFileInstall to true, the assembly will also be added to the gac.
        /// </summary>
        [TaskAction(AddTaskAction, false)]
        public bool Gac
        {
            get { return this.gac; }
            set { this.gac = value; }
        }

        /// <summary>
        /// Set to true to overwrite existing assemblies when deploy is called
        /// </summary>
        [TaskAction(AddTaskAction, false)]
        public bool Force { get; set; }

        private List<BizTalkResource> Resources
        {
            get { return this.resources; }
            set { this.resources = value; }
        }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            this.ParseAssemblyList();

            if (string.IsNullOrEmpty(this.DatabaseServer))
            {
                this.DatabaseServer = this.MachineName;
            }

            switch (this.TaskAction)
            {
                case AddTaskAction:
                    this.Add();
                    break;
                case RemoveTaskAction:
                    this.Remove();
                    break;
                case CheckExistsTaskAction:
                    this.CheckExists();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private Dictionary<string, object> GetResourceProperties(string assemblyPath)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();

            // gac on deploying,  set to false as we handle this ourselves (Microsoft.BizTalk.ApplicationDeployment does not support remote gac)
            properties.Add("Gacutil", this.GacOnMsiFileInstall);

            // gac on msi install 
            properties.Add("UpdateGac", this.GacOnAddResource);

            // gac on msi import 
            properties.Add("UpdateGacOnImport", this.GacOnMsiFileImport);

            // DestinationLocation 
            properties.Add("DestinationLocation", this.DestinationLocation);

            // source location of assembly
            properties.Add("SourceLocation", Path.Combine(Path.GetDirectoryName(assemblyPath), Path.GetFileName(assemblyPath)));

            return properties;
        }

        private BizTalkResource CreateBizTalkResource(string assemblyPath, int order)
        {
            Reflection.Assembly assembly = Reflection.Assembly.LoadFile(assemblyPath);
            object[] bizTalkAssemblyAttribute = null;
            try
            {
                // if the assembly has this attribute, then it's a BizTalk assembly - otherwise it's a standard .NET assembly
                // if an exception is thrown, we assume it is a standard .NET assembly - see http://msbuildextensionpack.codeplex.com/workitem/9177
                bizTalkAssemblyAttribute = assembly.GetCustomAttributes(typeof(BizTalkAssemblyAttribute), false);
            }
            catch
            {
                // do nothing
            }

            return new BizTalkResource
            {
                FullName = assembly.FullName,
                Properties = this.GetResourceProperties(assemblyPath),
                Dependencies = assembly.GetReferencedAssemblies().Select(a => a.FullName).ToList(),
                Order = order,
                SourcePath = assemblyPath,
                DeploymentPath = Path.Combine(Path.GetDirectoryName(this.DeploymentPath) ?? string.Empty, Path.GetFileName(assemblyPath)),
                ResourceType = (bizTalkAssemblyAttribute == null || bizTalkAssemblyAttribute.Length == 0) ? "System.BizTalk:Assembly" : "System.BizTalk:BizTalkAssembly"
            };
        }

        private void Add()
        {
            using (Deployment.Group btsGroup = new Deployment.Group())
            {
                try
                {
                    btsGroup.DBName = this.Database;
                    btsGroup.DBServer = this.DatabaseServer;
                    Deployment.Application btsApplication = btsGroup.Applications[this.Application];
                    btsApplication.Log += this.DeploymentLog;
                    btsApplication.UILevel = 1;

                    this.SortResources();
                    if (this.Gac)
                    {
                        // gac asssemblies
                        this.Resources.ForEach(r => this.AddAssembly(r));
                    }

                    this.Resources.ForEach(r => btsApplication.AddResource(r.ResourceType, r.FullName, r.Properties, this.Force));
                }
                catch
                {
                    btsGroup.Abort();
                    throw;
                }
            }
        }

        private void Remove()
        {
            // resources with dependencies must be removed first
            this.SortResources();

            foreach (BizTalkResource r in this.resources)
            {
                using (Deployment.Group btsGroup = new Deployment.Group())
                {
                    try
                    {
                        btsGroup.DBName = this.Database;
                        btsGroup.DBServer = this.DatabaseServer;
                        Deployment.Application btsApplication = btsGroup.Applications[this.Application];
                        btsApplication.Log += this.DeploymentLog;
                        btsApplication.UILevel = 2;
                        btsApplication.RemoveResource(string.Empty, r.FullName);
                    }
                    catch
                    {
                        btsGroup.Abort();
                        throw;
                    }
                }
            }
        }

        private void CheckExists()
        {
            using (Deployment.Group btsGroup = new Deployment.Group())
            {
                try
                {
                    btsGroup.DBName = this.Database;
                    btsGroup.DBServer = this.DatabaseServer;

                    Deployment.Application btsApplication = btsGroup.Applications[this.Application];
                    btsApplication.Log += this.DeploymentLog;
                    btsApplication.UILevel = 2;

                    if (this.Resources.Any(talkResource => !btsApplication.ResourceCollection.ToList().Exists(r => r.Luid == talkResource.FullName)))
                    {
                        this.Exists = false;
                        return;
                    }

                    this.Exists = true;
                }
                catch
                {
                    btsGroup.Abort();
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the IAssemblyCache interface.
        /// </summary>
        /// <returns>
        /// An IAssemblyCache interface.
        /// </returns>
        private NativeMethods.IAssemblyCache GetIAssemblyCache()
        {
            // Get the IAssemblyCache interface
            NativeMethods.IAssemblyCache assemblyCache;
            int result = NativeMethods.CreateAssemblyCache(out assemblyCache, 0);

            // If the result is not zero throw an exception
            if (result != 0)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Failed to get the IAssemblyCache interface. Result Code: {0}", result));
                return null;
            }

            // Return the IAssemblyCache interface
            return assemblyCache;
        }

        private void Install(string path, bool force)
        {
            // Get the IAssemblyCache interface
            NativeMethods.IAssemblyCache assemblyCache = this.GetIAssemblyCache();

            // Set the flag depending on the value of force
            int flag = force ? 2 : 1;

            // Install the assembly in the cache
            int result = assemblyCache.InstallAssembly(flag, path, IntPtr.Zero);

            // If the result is not zero throw an exception
            if (result != 0)
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "Failed to install assembly into the global assembly cache. Result Code: {0}", result));
                return;
            }
        }

        private void AddAssembly(BizTalkResource resource)
        {
            if (!System.IO.File.Exists(resource.SourcePath))
            {
                throw new Exception(string.Format(CultureInfo.CurrentCulture, "The AssemblyPath was not found: {0}", resource.SourcePath));
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "GAC Assembly: {0} on: {1}", resource.DeploymentPath, this.MachineName));
            if (string.Compare(this.MachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                this.Install(resource.SourcePath, this.Force);
            }
            else
            {
                if (string.IsNullOrEmpty(resource.DeploymentPath))
                {
                    throw new Exception("DeploymentPath is required for remote deployment");
                }

                // the assembly needs to be copied to the remote server for gaccing.
                if (System.IO.File.Exists(resource.DeploymentPath))
                {
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Deleting old remote Assembly: {0}", resource.DeploymentPath));
                    System.IO.File.Delete(resource.DeploymentPath);
                }

                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Copying Assembly from: {0} to: {1}", resource.SourcePath, resource.DeploymentPath));
                System.IO.File.Copy(resource.SourcePath, resource.DeploymentPath);
                this.GetManagementScope(@"\root\cimv2");
                using (ManagementClass m = new ManagementClass(this.Scope, new ManagementPath("Win32_Process"), new ObjectGetOptions(null, System.TimeSpan.MaxValue, true)))
                {
                    ManagementBaseObject methodParameters = m.GetMethodParameters("Create");
                    methodParameters["CommandLine"] = @"gacutil.exe /i " + resource.DeploymentPath;
                    ManagementBaseObject outParams = m.InvokeMethod("Create", methodParameters, null);

                    if (outParams != null)
                    {
                        if (int.Parse(outParams["returnValue"].ToString(), CultureInfo.InvariantCulture) != 0)
                        {
                            this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Remote AddAssembly returned non-zero returnValue: {0}", outParams["returnValue"]));
                            return;
                        }

                        this.LogTaskMessage(MessageImportance.Low, "Process ReturnValue: " + outParams["returnValue"]);
                        this.LogTaskMessage(MessageImportance.Low, "Process ID: " + outParams["processId"]);
                    }
                    else
                    {
                        this.Log.LogError("Remote Create returned null");
                        return;
                    }
                }
            }
        }

        private void SortResources()
        {
            this.Resources.ForEach(r => r.IsProcessed = false);
            this.Resources.Where(r => !r.IsProcessed).Aggregate(0, (current, r) => this.SortResourcesExecute(r, current));
            this.Resources = this.TaskAction == AddTaskAction ? this.Resources.OrderBy(r => r.Order).ToList() : this.Resources.OrderByDescending(r => r.Order).ToList();
        }

        private int SortResourcesExecute(BizTalkResource root, int order)
        {
            order = root.Dependencies.Select(name => this.Resources.Find(r => r.FullName == name)).Where(dependency => dependency != null).Where(dependency => !dependency.IsProcessed).Aggregate(order, (current, dependency) => this.SortResourcesExecute(dependency, current));

            root.IsProcessed = true;
            root.Order = order;
            return ++order;
        }

        private void ParseAssemblyList()
        {
            int sortSeed = 0;

            foreach (ITaskItem item in this.Assemblies)
            {
                this.Resources.Add(this.CreateBizTalkResource(item.GetMetadata("FullPath"), sortSeed));
                sortSeed++;
            }
        }

        private void DeploymentLog(object sender, Microsoft.BizTalk.Log.LogEventArgs e)
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, e.LogEntry.Message));
        }

        private class BizTalkResource
        {
            public string FullName { get; set; }

            public List<string> Dependencies { get; set; }

            public int Order { get; set; }

            public Dictionary<string, object> Properties { get; set; }

            public string SourcePath { get; set; }

            public string DeploymentPath { get; set; }

            public bool IsProcessed { get; set; }

            public string ResourceType { get; set; }
        }
    }
}
