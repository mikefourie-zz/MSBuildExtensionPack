//-----------------------------------------------------------------------
// <copyright file="Application.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.BizTalk
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.BizTalk.ApplicationDeployment;
    using Microsoft.BizTalk.Deployment;
    using Microsoft.BizTalk.Deployment.Binding;
    using Microsoft.BizTalk.ExplorerOM;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using OM = Microsoft.BizTalk.ExplorerOM;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddReference</i> (<b>Required: </b>Application, References <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>CheckExists</i> (<b>Required: </b>Application <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>Create</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>Delete</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>DisableAllReceiveLocations</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>EnableAllReceiveLocations</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>DisableReceiveLocations</i> (<b>Required: </b>Applications, ReceiveLocations <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>EnableReceiveLocations</i> (<b>Required: </b>Applications, ReceiveLocations <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>ExportBindings</i> (<b>Required: </b>BindingFile <b>Optional: </b>Application, MachineName, DatabaseServer, Database)</para>
    /// <para><i>ExportToMsi</i> (<b>Required: </b>Application, MsiPath <b>Optional: </b>MachineName, DatabaseServer, Database, IncludeGlobalPartyBinding, Resources)</para>
    /// <para><i>ImportBindings</i> (<b>Required: </b>BindingFile <b>Optional: </b>Application, MachineName, DatabaseServer, Database)</para>
    /// <para><i>ImportFromMsi</i> (<b>Required: </b>MsiPath <b>Optional: </b>MachineName, DatabaseServer, Database, Application, Overwrite)</para>
    /// <para><i>Get</i> (<b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>RemoveReference</i> (<b>Required: </b>Application, References <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>StartAll</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>StartAllOrchestrations</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>StartAllSendPortGroups</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>StartAllSendPorts</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>StartReferencedApplications</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>StopAll</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>StopReferencedApplications</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>UndeployAllPolicies</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>UnenlistAllOrchestrations</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>UnenlistAllSendPortGroups</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
    /// <para><i>UnenlistAllSendPorts</i> (<b>Required: </b>Applications <b>Optional: </b>MachineName, DatabaseServer, Database)</para>
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
    ///         <!-- Export an Application to an MSI -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="ExportToMsi" Application="An Application" MsiPath="C:\AnApplication.msi" IncludeGlobalPartyBinding="true"/>
    ///         <!-- Import an Application from an MSI -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="ImportFromMsi" Application="An Application" MsiPath="C:\AnApplication.msi" Overwrite="true" Environment="DEV" />
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
    ///         <!-- Imports the specified bindings file into a BizTalk application -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="ImportBindings" BindingFile="C:\BindingInfo.xml" Application="An Application" />
    ///         <!-- Exports a BizTalk application bindings to the specified file -->
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="ExportBindings" BindingFile="C:\BindingInfo.xml" Application="An Application" />
    ///     </Target>
    ///     <!-- Export an Application to a partial/incremental MSI -->
    ///     <Target Name="ExportToMsi">
    ///         <MSBuild Projects="@(Compile)" Targets="Build">
    ///             <Output TaskParameter="TargetOutputs" ItemName="CompiledAssemblies" />
    ///         </MSBuild>
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="GetInfo" NetAssembly="%(CompiledAssemblies.Identity)">
    ///             <Output TaskParameter="OutputItems" ItemName="NetAssemblies" />
    ///         </MSBuild.ExtensionPack.Framework.Assembly>
    ///         <ItemGroup>
    ///             <Resources Include="@(NetAssemblies->'%(FullName)')" />
    ///             <Resources Include="Application/$(BtsApplicationName)" Condition=" '$(ExportBindings)'=='True' " />
    ///         </ItemGroup>
    ///         <MSBuild.ExtensionPack.BizTalk.BizTalkApplication TaskAction="ExportToMsi" Application="$(BtsApplicationName)" Resources="@(Resources)" MsiPath="$(TargetMsi)" IncludeGlobalPartyBinding="$(ExportBindings)"/>
    ///     </Target>
    /// </Project>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.11.0/html/b4a8b403-3659-cea7-e8c6-645d46814f98.htm")]
    public class BizTalkApplication : BaseTask
    {
        private const string AddReferenceTaskAction = "AddReference";
        private const string CheckExistsTaskAction = "CheckExists";
        private const string CreateTaskAction = "Create";
        private const string DeleteTaskAction = "Delete";
        private const string DisableAllReceiveLocationsTaskAction = "DisableAllReceiveLocations";
        private const string DisableReceiveLocationsTaskAction = "DisableReceiveLocations";
        private const string ExportToMsiTaskAction = "ExportToMsi";
        private const string ImportFromMsiTaskAction = "ImportFromMsi";
        private const string EnableAllReceiveLocationsTaskAction = "EnableAllReceiveLocations";
        private const string EnableReceiveLocationsTaskAction = "EnableReceiveLocations";
        private const string GetTaskAction = "Get";
        private const string RemoveReferenceTaskAction = "RemoveReference";
        private const string StartAllTaskAction = "StartAll";
        private const string StartAllOrchestrationsTaskAction = "StartAllOrchestrations";
        private const string StartAllSendPortGroupsTaskAction = "StartAllSendPortGroups";
        private const string StartAllSendPortsTaskAction = "StartAllSendPorts";
        private const string StartReferencedApplicationsTaskAction = "StartReferencedApplications";
        private const string StopAllTaskAction = "StopAll";
        private const string StopReferencedApplicationsTaskAction = "StopReferencedApplications";
        private const string UndeployAllPoliciesTaskAction = "UndeployAllPolicies";
        private const string UnenlistAllOrchestrationsTaskAction = "UnenlistAllOrchestrations";
        private const string UnenlistAllSendPortGroupsTaskAction = "UnenlistAllSendPortGroups";
        private const string UnenlistAllSendPortsTaskAction = "UnenlistAllSendPorts";
        private const string ImportBindingsTaskAction = "ImportBindings";
        private const string ExportBindingsTaskAction = "ExportBindings";
        private string database = "BizTalkMgmtDb";
        private BtsCatalogExplorer explorer;
        private OM.Application app;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(AddReferenceTaskAction)]
        [DropdownValue(CheckExistsTaskAction)]
        [DropdownValue(CreateTaskAction)]
        [DropdownValue(DeleteTaskAction)]
        [DropdownValue(DisableAllReceiveLocationsTaskAction)]
        [DropdownValue(DisableReceiveLocationsTaskAction)]
        [DropdownValue(EnableReceiveLocationsTaskAction)]
        [DropdownValue(EnableAllReceiveLocationsTaskAction)]
        [DropdownValue(GetTaskAction)]
        [DropdownValue(RemoveReferenceTaskAction)]
        [DropdownValue(StartAllTaskAction)]
        [DropdownValue(StartAllOrchestrationsTaskAction)]
        [DropdownValue(StartAllSendPortGroupsTaskAction)]
        [DropdownValue(StartAllSendPortsTaskAction)]
        [DropdownValue(StartReferencedApplicationsTaskAction)]
        [DropdownValue(StopAllTaskAction)]
        [DropdownValue(StopReferencedApplicationsTaskAction)]
        [DropdownValue(UndeployAllPoliciesTaskAction)]
        [DropdownValue(UnenlistAllOrchestrationsTaskAction)]
        [DropdownValue(UnenlistAllSendPortGroupsTaskAction)]
        [DropdownValue(UnenlistAllSendPortsTaskAction)]
        [DropdownValue(ImportFromMsiTaskAction)]
        [DropdownValue(ExportToMsiTaskAction)]
        [DropdownValue(ImportBindingsTaskAction)]
        [DropdownValue(ExportBindingsTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the MachineName.
        /// </summary>
        [TaskAction(AddReferenceTaskAction, false)]
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(DeleteTaskAction, false)]
        [TaskAction(DisableAllReceiveLocationsTaskAction, false)]
        [TaskAction(DisableReceiveLocationsTaskAction, false)]
        [TaskAction(EnableReceiveLocationsTaskAction, false)]
        [TaskAction(EnableAllReceiveLocationsTaskAction, false)]
        [TaskAction(GetTaskAction, false)]
        [TaskAction(RemoveReferenceTaskAction, false)]
        [TaskAction(StartAllTaskAction, false)]
        [TaskAction(StartAllOrchestrationsTaskAction, false)]
        [TaskAction(StartAllSendPortGroupsTaskAction, false)]
        [TaskAction(StartAllSendPortsTaskAction, false)]
        [TaskAction(StartReferencedApplicationsTaskAction, false)]
        [TaskAction(StopAllTaskAction, false)]
        [TaskAction(StopReferencedApplicationsTaskAction, false)]
        [TaskAction(UndeployAllPoliciesTaskAction, false)]
        [TaskAction(UnenlistAllOrchestrationsTaskAction, false)]
        [TaskAction(UnenlistAllSendPortGroupsTaskAction, false)]
        [TaskAction(UnenlistAllSendPortsTaskAction, false)]
        [TaskAction(ImportFromMsiTaskAction, false)]
        [TaskAction(ExportToMsiTaskAction, false)]
        [TaskAction(ImportBindingsTaskAction, false)]
        [TaskAction(ExportBindingsTaskAction, false)]
        public override string MachineName
        {
            get { return base.MachineName; }
            set { base.MachineName = value; }
        }

        /// <summary>
        /// Sets the DatabaseServer to connect to. Default is MachineName
        /// </summary>
        [TaskAction(AddReferenceTaskAction, false)]
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(DeleteTaskAction, false)]
        [TaskAction(DisableAllReceiveLocationsTaskAction, false)]
        [TaskAction(DisableReceiveLocationsTaskAction, false)]
        [TaskAction(EnableReceiveLocationsTaskAction, false)]
        [TaskAction(EnableAllReceiveLocationsTaskAction, false)]
        [TaskAction(GetTaskAction, false)]
        [TaskAction(RemoveReferenceTaskAction, false)]
        [TaskAction(StartAllTaskAction, false)]
        [TaskAction(StartAllOrchestrationsTaskAction, false)]
        [TaskAction(StartAllSendPortGroupsTaskAction, false)]
        [TaskAction(StartAllSendPortsTaskAction, false)]
        [TaskAction(StartReferencedApplicationsTaskAction, false)]
        [TaskAction(StopAllTaskAction, false)]
        [TaskAction(StopReferencedApplicationsTaskAction, false)]
        [TaskAction(UndeployAllPoliciesTaskAction, false)]
        [TaskAction(UnenlistAllOrchestrationsTaskAction, false)]
        [TaskAction(UnenlistAllSendPortGroupsTaskAction, false)]
        [TaskAction(UnenlistAllSendPortsTaskAction, false)]
        [TaskAction(ImportFromMsiTaskAction, false)]
        [TaskAction(ExportToMsiTaskAction, false)]
        [TaskAction(ImportBindingsTaskAction, false)]
        [TaskAction(ExportBindingsTaskAction, false)]
        public string DatabaseServer { get; set; }

        /// <summary>
        /// Get or sets the Application Item Collection
        /// </summary>
        [Output]
        [TaskAction(CreateTaskAction, true)]
        [TaskAction(DeleteTaskAction, true)]
        [TaskAction(DisableAllReceiveLocationsTaskAction, true)]
        [TaskAction(DisableReceiveLocationsTaskAction, true)]
        [TaskAction(EnableReceiveLocationsTaskAction, true)]
        [TaskAction(EnableAllReceiveLocationsTaskAction, true)]
        [TaskAction(StartAllTaskAction, true)]
        [TaskAction(StartAllOrchestrationsTaskAction, true)]
        [TaskAction(StartAllSendPortGroupsTaskAction, true)]
        [TaskAction(StartAllSendPortsTaskAction, true)]
        [TaskAction(StartReferencedApplicationsTaskAction, true)]
        [TaskAction(StopAllTaskAction, true)]
        [TaskAction(StopReferencedApplicationsTaskAction, true)]
        [TaskAction(UndeployAllPoliciesTaskAction, true)]
        [TaskAction(UnenlistAllOrchestrationsTaskAction, true)]
        [TaskAction(UnenlistAllSendPortGroupsTaskAction, true)]
        [TaskAction(UnenlistAllSendPortsTaskAction, true)]
        public ITaskItem[] Applications { get; set; }

        /// <summary>
        /// Sets the Referenced Applications
        /// </summary>
        [TaskAction(AddReferenceTaskAction, true)]
        [TaskAction(RemoveReferenceTaskAction, true)]
        public ITaskItem[] References { get; set; }

        /// <summary>
        /// sets the ReceiveLocations to operate on.
        /// </summary>
        [TaskAction(DisableReceiveLocationsTaskAction, true)]
        [TaskAction(EnableReceiveLocationsTaskAction, true)]
        public ITaskItem[] ReceiveLocations { get; set; }

        /// <summary>
        /// Sets the Application Name
        /// </summary>
        [TaskAction(AddReferenceTaskAction, true)]
        [TaskAction(CheckExistsTaskAction, true)]
        [TaskAction(RemoveReferenceTaskAction, true)]
        [TaskAction(ImportFromMsiTaskAction, false)]
        [TaskAction(ExportToMsiTaskAction, true)]
        [TaskAction(ImportBindingsTaskAction, false)]
        [TaskAction(ExportBindingsTaskAction, false)]
        public string Application { get; set; }

        /// <summary>
        /// Sets the Resources to export. If not supplied, all resources are exported.
        /// </summary>
        [TaskAction(ExportToMsiTaskAction, false)]
        public ITaskItem[] Resources { get; set; }

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
        [TaskAction(AddReferenceTaskAction, false)]
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(DeleteTaskAction, false)]
        [TaskAction(DisableAllReceiveLocationsTaskAction, false)]
        [TaskAction(DisableReceiveLocationsTaskAction, false)]
        [TaskAction(EnableAllReceiveLocationsTaskAction, false)]
        [TaskAction(EnableReceiveLocationsTaskAction, false)]
        [TaskAction(GetTaskAction, false)]
        [TaskAction(RemoveReferenceTaskAction, false)]
        [TaskAction(StartAllTaskAction, false)]
        [TaskAction(StartAllOrchestrationsTaskAction, false)]
        [TaskAction(StartAllSendPortGroupsTaskAction, false)]
        [TaskAction(StartAllSendPortsTaskAction, false)]
        [TaskAction(StartReferencedApplicationsTaskAction, false)]
        [TaskAction(StopAllTaskAction, false)]
        [TaskAction(StopReferencedApplicationsTaskAction, false)]
        [TaskAction(UndeployAllPoliciesTaskAction, false)]
        [TaskAction(UnenlistAllOrchestrationsTaskAction, false)]
        [TaskAction(UnenlistAllSendPortGroupsTaskAction, false)]
        [TaskAction(UnenlistAllSendPortsTaskAction, false)]
        [TaskAction(ImportFromMsiTaskAction, false)]
        [TaskAction(ExportToMsiTaskAction, false)]
        [TaskAction(ImportBindingsTaskAction, false)]
        [TaskAction(ExportBindingsTaskAction, false)]
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
        /// Set the path to export the Application MSI to. The directory path must exist and have appropriate permissions to write to.
        /// </summary>
        [TaskAction(ImportFromMsiTaskAction, true)]
        [TaskAction(ExportToMsiTaskAction, true)]
        public ITaskItem MsiPath { get; set; }

        /// <summary>
        /// Set to true to export the global party information. Default is false.
        /// </summary>
        public bool IncludeGlobalPartyBinding { get; set; }

        /// <summary>
        /// Update existing resources. If not specified and resource exists, import will fail. Default is false.
        /// </summary>
        [TaskAction(ImportFromMsiTaskAction, false)]
        public bool Overwrite { get; set; }

        /// <summary>
        /// The environment to deploy.
        /// </summary>
        [TaskAction(ImportFromMsiTaskAction, false)]
        public string Environment { get; set; }

        /// <summary>
        /// The Binding File to Import / Export
        /// </summary>
        [TaskAction(ImportBindingsTaskAction, true)]
        [TaskAction(ExportBindingsTaskAction, true)]
        public ITaskItem BindingFile { get; set; }

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
                switch (this.TaskAction)
                {
                    case CreateTaskAction:
                        this.Create();
                        break;
                    case GetTaskAction:
                        this.GetApplications();
                        break;
                    case CheckExistsTaskAction:
                        this.CheckApplicationExists();
                        break;
                    case StartAllTaskAction:
                    case EnableAllReceiveLocationsTaskAction:
                    case StartAllOrchestrationsTaskAction:
                    case StartAllSendPortGroupsTaskAction:
                    case StartAllSendPortsTaskAction:
                    case StartReferencedApplicationsTaskAction:
                        this.StartApplication();
                        break;
                    case StopAllTaskAction:
                    case DisableAllReceiveLocationsTaskAction:
                    case UndeployAllPoliciesTaskAction:
                    case UnenlistAllOrchestrationsTaskAction:
                    case UnenlistAllSendPortGroupsTaskAction:
                    case UnenlistAllSendPortsTaskAction:
                    case StopReferencedApplicationsTaskAction:
                        this.StopApplication();
                        break;
                    case EnableReceiveLocationsTaskAction:
                    case DisableReceiveLocationsTaskAction:
                        this.ControlReceiveLocations();
                        break;
                    case DeleteTaskAction:
                        this.Delete();
                        break;
                    case ExportToMsiTaskAction:
                        this.ExportToMsi();
                        break;
                    case ImportFromMsiTaskAction:
                        this.ImportFromMsi();
                        break;
                    case RemoveReferenceTaskAction:
                    case AddReferenceTaskAction:
                        this.ConfigureReference();
                        break;
                    case ImportBindingsTaskAction:
                        this.ImportBindings();
                        break;
                    case ExportBindingsTaskAction:
                        this.ExportBindings();
                        break;
                    default:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                        return;
                }
            }
        }

        private void ControlReceiveLocations()
        {
            if (this.ReceiveLocations == null)
            {
                Log.LogError("ReceiveLocations is required");
                return;
            }

            foreach (ITaskItem item in this.ReceiveLocations)
            {
                foreach (Microsoft.BizTalk.ExplorerOM.ReceivePort rport in this.explorer.ReceivePorts)
                {
                    foreach (Microsoft.BizTalk.ExplorerOM.ReceiveLocation rl in rport.ReceiveLocations.Cast<Microsoft.BizTalk.ExplorerOM.ReceiveLocation>().Where(rl => string.Compare(rl.Name, item.ItemSpec, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        switch (this.TaskAction)
                        {
                            case EnableReceiveLocationsTaskAction:
                                this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Enabling {1} for {0}", rport.Name, rl.Name));
                                rl.Enable = true;
                                break;
                            case DisableReceiveLocationsTaskAction:
                                this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Disabling {1} for {0}", rport.Name, rl.Name));
                                rl.Enable = false;
                                break;
                        }
                    }
                }
            }

            this.explorer.SaveChanges();
        }

        private void ImportBindings()
        {
            // not supported, only import bindings at application level
            // -GroupLevel        Optional. If specified, ports in the binding file are imported into their associated applications. If not specified, all ports are imported
            // into the specified application or default application if an application name is not specified.
            if (this.BindingFile == null)
            {
                // -Source            Required. The path and file name of the XML binding file to read.
                this.Log.LogError("BindingFile is required");
                return;
            }

            if (!File.Exists(this.BindingFile.ItemSpec))
            {
                // -Source            Required. The path and file name of the XML binding file to read.
                this.Log.LogError("File {0} not found", this.BindingFile.ItemSpec);
                return;
            }

            if (string.IsNullOrEmpty(this.Application))
            {
                // -ApplicationName   Optional. The name of the BizTalk application.
                this.Application = this.explorer.DefaultApplication.Name;
                this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Using default application {0}", this.Application));
            }

            using (DeployerComponent dc = new DeployerComponent())
            {
                string resultMessage = string.Empty;

                switch (dc.ImportBindingWithValidation(this.explorer.ConnectionString, this.BindingFile.ItemSpec, this.Application, false, ref resultMessage))
                {
                    case ImportBindingError.Succeeded:
                        // resultMessage is unchanged (string.Empty), use custom message
                        this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Imported {0} into application {1}", this.BindingFile.ItemSpec, this.Application));
                        break;
                    case ImportBindingError.SucceededWithWarning:
                        // log message returned by ImportBindingWithValidation()
                        this.Log.LogWarning(resultMessage);
                        break;
                    case ImportBindingError.Failed:
                        // this is only returned if the app is not found, which will never happen since we use the default app
                        // if there are any problems with ImportBindingWithValidation, an error will be logged anyway
                        this.Log.LogError(resultMessage);
                        break;
                }
            }
        }

        private void ExportBindings()
        {
            // not supported, only export bindings at application level
            //  -GroupLevel        Optional. If specified, all bindings in the current group are exported.
            //  -GlobalParties     Optional. If specified, the global party information for the group is exported.
            //  -AssemblyName      Optional. The full name of the BizTalk assembly.
            if (this.BindingFile == null)
            {
                // -Destination       Required. Path and file name of the XML binding file to write.
                this.Log.LogError("BindingFile is required");
                return;
            }

            // use default app if no app name is provided
            if (string.IsNullOrEmpty(this.Application))
            {
                // -ApplicationName   Optional. The name of the BizTalk application.
                this.Application = this.explorer.DefaultApplication.Name;
                this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Using default application {0}", this.Application));
            }

            // create dir if it doesn't exist
            string dir = Path.GetDirectoryName(Path.GetFullPath(this.BindingFile.ItemSpec));

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Created directory {0}", dir));
            }

            using (SqlConnection sqlConnection = new SqlConnection(this.explorer.ConnectionString))
            {
                using (BindingInfo info = new BindingInfo())
                {
                    BindingParameters bindingParameters = new BindingParameters(new Version(info.Version)) { BindingItems = BindingParameters.BindingItemTypes.All, BindingScope = BindingParameters.BindingScopeType.Application };
                    info.AddApplicationRef(sqlConnection, this.Application);
                    info.Select(sqlConnection, bindingParameters);
                    info.SaveXml(this.BindingFile.ItemSpec);

                    this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Exported {0} bindings to {1}", this.Application, this.BindingFile.ItemSpec));
                }
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
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Application not found: {0}", this.Application));
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
                        case RemoveReferenceTaskAction:
                            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing Referenced Application: {0} from: {1}", item.ItemSpec, this.Application));
                            this.app.RemoveReference(refApp);
                            break;
                        case AddReferenceTaskAction:
                            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding Referenced Application: {0} from: {1}", item.ItemSpec, this.Application));
                            this.app.AddReference(refApp);
                            break;
                    }
                }
                else
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "Referenced Application not found: {0}", item.ItemSpec));
                    return;
                }
            }

            this.explorer.SaveChanges();
        }

        private void CheckApplicationExists()
        {
            if (string.IsNullOrEmpty(this.Application))
            {
                this.Log.LogError("Application is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking whether Application exists: {0}", this.Application));
            this.Exists = this.CheckExists(this.Application);
        }

        private void ExportToMsi()
        {
            if (string.IsNullOrEmpty(this.Application))
            {
                this.Log.LogError("Application is required");
                return;
            }

            if (this.MsiPath == null)
            {
                this.Log.LogError("Destination is required");
                return;
            }

            if (this.Resources == null)
            {
                this.Resources = new ITaskItem[] { };
            }

            if (!this.CheckExists(this.Application))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Application does not exist: {0}", this.Application));
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Exporting Application {0} to {1}", this.Application, this.MsiPath));
            using (Group group = new Group())
            {
                group.DBName = this.Database;
                group.DBServer = this.DatabaseServer;

                Microsoft.BizTalk.ApplicationDeployment.ApplicationCollection apps = group.Applications;
                apps.UiLevel = 2;

                Microsoft.BizTalk.ApplicationDeployment.Application appl = apps[this.Application];
                List<Resource> exportedResources = new List<Resource>();

                foreach (Resource resource in appl.ResourceCollection.Cast<Resource>().Where(resource => !resource.Properties.ContainsKey("IsSystem") || !((bool)resource.Properties["IsSystem"])))
                {
                    if (this.IncludeGlobalPartyBinding && resource.Luid.Equals("Application/" + this.Application, StringComparison.OrdinalIgnoreCase))
                    {
                        resource.Properties["IncludeGlobalPartyBinding"] = this.IncludeGlobalPartyBinding;
                    }

                    // only export specified resources
                    if (this.Resources.Length != 0 && !this.Resources.Any(item => item.ItemSpec == resource.Luid))
                    {
                        continue;
                    }

                    this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Exporting Resource {0}", resource.Luid));
                    exportedResources.Add(resource);
                }

                appl.Export(this.MsiPath.ItemSpec, exportedResources);
            }
        }

        private void ImportFromMsi()
        {
            if (this.MsiPath == null)
            {
                // -Package           Required. The path and file name of the Windows Installer package.
                this.Log.LogError("MSI source is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Importing from {0}", this.MsiPath.ItemSpec));

            if (string.IsNullOrEmpty(this.Application))
            {
                // -ApplicationName   Optional. The name of the BizTalk application.
                this.Application = this.explorer.DefaultApplication.Name;
                this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Using default application {0}", this.Application));
            }

            // create application if it doesn't exist
            if (!this.CheckExists(this.Application))
            {
                OM.Application newapp = this.explorer.AddNewApplication();
                newapp.Name = this.Application;
                this.explorer.SaveChanges();
                this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Creating new application {0}", this.Application));
            }

            using (Group group = new Group())
            {
                group.DBName = this.Database;
                group.DBServer = this.DatabaseServer;

                Microsoft.BizTalk.ApplicationDeployment.Application appl = group.Applications[this.Application];

                // used to specify custom properties for import, i.e. TargetEnvironment
                IDictionary<string, object> requestProperties = null;
                if (!string.IsNullOrEmpty(this.Environment))
                {
                    // -Environment       Optional. The environment to deploy.
                    requestProperties = new Dictionary<string, object> { { "TargetEnvironment", this.Environment } };
                    this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Target environment {0} specified", this.Environment));
                }

                // the overload that takes request properties also requires this
                IInstallPackage package = DeploymentUnit.ScanPackage(this.MsiPath.ItemSpec);
                ICollection<string> applicationReferences = package.References;

                // -Overwrite         Optional. Update existing resources. If not specified and resource exists, import will fail.
                appl.Import(this.MsiPath.ItemSpec, requestProperties, applicationReferences, this.Overwrite);
                this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Successfully imported {0} into application {1}", this.MsiPath.ItemSpec, this.Application));
            }
        }

        private void Create()
        {
            if (this.Applications == null)
            {
                this.Log.LogError("Applications is required");
                return;
            }

            foreach (ITaskItem appl in this.Applications)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Application: {0} - {1}", appl.ItemSpec, this.TaskAction));
                if (this.CheckExists(appl.ItemSpec))
                {
                    if (this.Force)
                    {
                        this.DeleteApplication(appl);
                        this.explorer.Refresh();
                    }
                    else
                    {
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Application already exists: {0}. Set Force to true to delete the Application.", appl.ItemSpec));
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
            if (this.Applications == null)
            {
                this.Log.LogError("Applications is required");
                return;
            }

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
                group.DBServer = this.DatabaseServer;

                Microsoft.BizTalk.ApplicationDeployment.ApplicationCollection apps = group.Applications;
                apps.UiLevel = 2;

                Microsoft.BizTalk.ApplicationDeployment.Application deadapp = apps[application.ItemSpec];
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Application: {0} - {1}", application.ItemSpec, this.TaskAction));
                apps.Remove(deadapp);
            }

            this.explorer.SaveChanges();
        }

        private void StopApplication()
        {
            if (this.Applications == null)
            {
                this.Log.LogError("Applications is required");
                return;
            }

            foreach (ITaskItem appl in this.Applications)
            {
                if (!this.CheckExists(appl.ItemSpec))
                {
                    this.LogTaskWarning(string.Format(CultureInfo.CurrentCulture, "Application not found: {0}", appl.ItemSpec));
                }
                else
                {
                    OM.ApplicationStopOption option = OM.ApplicationStopOption.StopAll;
                    switch (this.TaskAction)
                    {
                        case StopAllTaskAction:
                            option = OM.ApplicationStopOption.StopAll;
                            break;
                        case DisableAllReceiveLocationsTaskAction:
                            option = OM.ApplicationStopOption.DisableAllReceiveLocations;
                            break;
                        case UndeployAllPoliciesTaskAction:
                            option = OM.ApplicationStopOption.UndeployAllPolicies;
                            break;
                        case UnenlistAllOrchestrationsTaskAction:
                            option = OM.ApplicationStopOption.UnenlistAllOrchestrations;
                            break;
                        case UnenlistAllSendPortGroupsTaskAction:
                            option = OM.ApplicationStopOption.UnenlistAllSendPortGroups;
                            break;
                        case UnenlistAllSendPortsTaskAction:
                            option = OM.ApplicationStopOption.UnenlistAllSendPorts;
                            break;
                        case StopReferencedApplicationsTaskAction:
                            option = OM.ApplicationStopOption.StopReferencedApplications;
                            break;
                    }

                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Application: {0} - {1}", appl.ItemSpec, this.TaskAction));
                    this.explorer.SaveChanges();
                    this.app.Stop(option);
                    this.explorer.SaveChanges();
                }
            }
        }

        private void StartApplication()
        {
            if (this.Applications == null)
            {
                this.Log.LogError("Applications is required");
                return;
            }

            foreach (ITaskItem appl in this.Applications)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Application: {0} - {1}", appl.ItemSpec, this.TaskAction));
                if (!this.CheckExists(appl.ItemSpec))
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Application not found: {0}", appl.ItemSpec));
                }
                else
                {
                    switch (this.TaskAction)
                    {
                        case StartAllTaskAction:
                            this.app.Start(OM.ApplicationStartOption.StartAll);
                            break;
                        case EnableAllReceiveLocationsTaskAction:
                            this.app.Start(OM.ApplicationStartOption.EnableAllReceiveLocations);
                            break;
                        case StartAllOrchestrationsTaskAction:
                            this.app.Start(OM.ApplicationStartOption.StartAllOrchestrations);
                            break;
                        case StartAllSendPortGroupsTaskAction:
                            this.app.Start(OM.ApplicationStartOption.StartAllSendPortGroups);
                            break;
                        case StartAllSendPortsTaskAction:
                            this.app.Start(OM.ApplicationStartOption.StartAllSendPorts);
                            break;
                        case StartReferencedApplicationsTaskAction:
                            this.app.Start(OM.ApplicationStartOption.StartReferencedApplications);
                            break;
                    }

                    this.explorer.SaveChanges();
                }
            }
        }

        private bool CheckExists(string applicationName)
        {
            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Checking whether Application exists: {0}", applicationName));
            this.app = this.explorer.Applications[applicationName];
            if (this.app != null)
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Application exists: {0}", applicationName));
                this.Exists = true;
                return true;
            }

            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Application does not exist: {0}", applicationName));
            return false;
        }

        private void GetApplications()
        {
            this.LogTaskMessage("Getting Applications");

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