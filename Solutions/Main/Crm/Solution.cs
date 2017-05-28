//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="Solution.cs">(c) 2017 Mike Fourie and Contributors (http://www.MSBuildExtensionPack.com) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Crm
{
    using System;
    using System.Globalization;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Client;
    using Microsoft.Xrm.Client.Services;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Import</i> (<b>Required: </b>OrganizationUrl, Name, Extension <b>Optional: </b>Path, OverwriteCustomizations, EnableSDKProcessingSteps, ConnectionTimeout)</para>
    /// <para><i>Export</i> (<b>Required: </b>OrganizationUrl, Name, Extension <b>Optional: </b>Path, ExportAsManagedSolution, ConnectionTimeout)</para>
    /// <para><i>GetVersion</i> (<b>Required: </b>OrganizationUrl <b>Optional: </b> ConnectionTimeout)</para>
    /// <para><i>SetVersion</i> (<b>Required: </b>OrganizationUrl, Version <b>Optional</b> ConnectionTimeout)</para>
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
    ///         <!-- Import a Solution to the given Organization -->
    ///         <MSBuild.ExtensionPack.Crm.Solution TaskAction="Import" OrganizationUrl="http://crm/organization1" Name="CrmSolution" Path="C:\Solutions" Extension="zip" OverwriteCustomizations="true" EnableSDKProcessingSteps="True" />
    ///     </Target>
    ///     <Target Name="Export">
    ///         <!-- Export a Solution as an managed or unmanaged Solution to a solution file-->
    ///         <MSBuild.ExtensionPack.Crm.Solution TaskAction="Export" OrganizationUrl="http://crm/organization1" Name="CrmSolution" OverwriteCustomizations="true" EnableSDKProcessingSteps="True" />
    ///     </Target>
    ///     <Target Name="GetVersion">
    ///         <!-- GetVersion for a Solution -->
    ///         <MSBuild.ExtensionPack.Crm.Solution TaskAction="GetVersion" OrganizationUrl="http://crm/organization1" Name="CrmSolution">
    ///             <Output TaskParameter="Version" PropertyName="CrmSolutionVersionNumber" /> 
    ///         </MSBuild.ExtensionPack.Crm.Solution>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class Solution : BaseTask
    {
        private const string ImportSolutionTaskAction = "Import";
        private const string ExportSolutionTaskAction = "Export";
        private const string GetVersionTaskAction = "GetVersion";
        private const string SetVersionTaskAction = "SetVersion";
        private bool overwriteCustomizations = true;
        private bool enableSdkProcessingSteps = true;
        private bool exportAsManagedSolution = true;
        private TimeSpan connectionTimeOut = new TimeSpan(0, 3, 0);

        /// <summary>
        /// Sets the Url of the Organization, where the Solution is imported to.
        /// </summary>
        [Required]
        public string OrganizationUrl { get; set; }

        /// <summary>
        /// Sets the Name of the Solution. While exporting the Solution file will be named same as the Solution's name.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Sets the extension of the Solution file for import or export.
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// Sets the Version of the Solution.
        /// </summary>
        [Output]
        public string Version { get; set; }

        /// <summary>
        /// Sets the directory path where the Solution is imported or exported to. 
        /// If a path is not set, the file is read or written from the MSBuild script's directory path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Sets whether to overwrite any unmanaged customizations or not. Default is true.
        /// </summary>
        public bool OverwriteCustomizations
        {
            get
            {
                return this.overwriteCustomizations;
            }

            set
            {
                this.overwriteCustomizations = value;
            }
        }

        /// <summary>
        /// Sets whether to enable any SDK message processing steps included in the Solution. Default is true.
        /// </summary>
        public bool EnableSdkProcessingSteps
        {
            get
            {
                return this.enableSdkProcessingSteps;
            }

            set
            {
                this.enableSdkProcessingSteps = value;
            }
        }

        /// <summary>
        /// Gets or sets the timeout in minutes for connecting to Crm Service. Default is 3 minutes.
        /// </summary>
        public int ConnectionTimeout
        {
            get
            {
                return (int)this.connectionTimeOut.TotalMinutes;
            }

            set
            {
                this.connectionTimeOut = new TimeSpan(0, value, 0);
            }
        }

        /// <summary>
        /// Sets whether the Solution to be exported as a managed Solution. Default is true.
        /// </summary>
        public bool ExportAsManagedSolution
        {
            get
            {
                return this.exportAsManagedSolution;
            }

            set
            {
                this.exportAsManagedSolution = value;
            }
        }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (!Uri.IsWellFormedUriString(this.OrganizationUrl, UriKind.Absolute))
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The Organization URL is not set correctly. {0}", this.OrganizationUrl));
                return;
            }

            if (string.IsNullOrWhiteSpace(this.Name))
            {
                Log.LogError("Required parameter missing: Name");
                return;
            }

            switch (this.TaskAction)
            {
                case ExportSolutionTaskAction:
                    this.ProcessAction(this.ExportSolution);
                    break;

                case ImportSolutionTaskAction:
                    this.ProcessAction(this.ImportSolution);
                    break;

                case GetVersionTaskAction:
                    this.ProcessAction(this.GetVersion);
                    break;

                case SetVersionTaskAction:
                    this.ProcessAction(this.SetVersion);
                    break;

                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid Task Action passed: {0}", this.TaskAction));
                    break;
            }
        }

        private void ProcessAction(Action<CrmConnection> action)
        {
            Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Connecting to the Organization {0}.", this.OrganizationUrl));
            string connectionString = string.Format(CultureInfo.CurrentCulture, "Server={0};Timeout={1}", this.OrganizationUrl, this.ConnectionTimeout);
            var connection = CrmConnection.Parse(connectionString);
            action(connection);
        }

        private void GetVersion(CrmConnection connection)
        {
            using (var serviceContext = new CrmOrganizationServiceContext(connection))
            {
                var entity = this.GetVersionEntity(serviceContext);
                if (entity != null)
                {
                    this.Version = entity.GetAttributeValue<string>("version");
                    entity["version"] = this.Version;
                    Log.LogMessage(
                        MessageImportance.Low, 
                        string.Format(CultureInfo.CurrentCulture, "Successfully read version of Solution {0} in Organization with Url {1}.", this.Name, this.OrganizationUrl));
                }
            }
        }

        private void SetVersion(CrmConnection connection)
        {
            using (var serviceContext = new CrmOrganizationServiceContext(connection))
            {
                try
                {
                    var entity = this.GetVersionEntity(serviceContext);
                    if (entity != null)
                    {
                        entity["version"] = this.Version;
                        serviceContext.Update(entity);
                        Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Successfully set version of Solution {0} in Organization with Url {1}.", this.Name, this.OrganizationUrl));
                    }
                }
                catch (Exception exception)
                {
                    Log.LogError(string.Format(
                                    CultureInfo.CurrentCulture,
                                    "An error occurred while setting version of Solution {0} in Organization with Url {1}. [{2}]",
                                    this.Name,
                                    this.OrganizationUrl,
                                    exception.Message));
                }
            }
        }

        private Entity GetVersionEntity(CrmOrganizationServiceContext serviceContext)
        {
            Entity returnEntity = null;
            var queryExpression = new QueryExpression("solution")
                                    {
                                        ColumnSet = new ColumnSet(new[] { "version" })
                                    };
            
            queryExpression.Criteria.AddCondition("uniquename", ConditionOperator.Equal, new object[] { this.Name });
            try
            {
                var entityCollection = serviceContext.RetrieveMultiple(queryExpression);
                if (entityCollection == null || entityCollection.Entities.Count == 0)
                {
                    Log.LogError(string.Format(
                        CultureInfo.CurrentCulture,
                        "Unable to retrieve details of Solution {0} from Organization with Url {1}.",
                        this.Name,
                        this.OrganizationUrl));
                }
                else
                {
                    returnEntity = entityCollection.Entities[0];
                }
            }
            catch (Exception exception)
            {
                Log.LogError(string.Format(
                                    CultureInfo.CurrentCulture,
                                    "An error occurred while retrieving version details of Solution {0} from Organization with Url {1}. [{2}]",
                                    this.Name,
                                    this.OrganizationUrl,
                                    exception.Message));
            }

            return returnEntity;
        }

        private void ImportSolution(CrmConnection connection)
        {
            if (string.IsNullOrWhiteSpace(this.Extension))
            {
                Log.LogError("Required parameter missing: Extension");
                return;
            }

            string directoryPath = string.IsNullOrEmpty(this.Path)
                ? System.IO.Path.GetDirectoryName(this.BuildEngine.ProjectFileOfTaskNode)
                : this.Path;

            // ReSharper disable once AssignNullToNotNullAttribute
            string solutioneFile = string.Format(CultureInfo.CurrentCulture, "{0}.{1}", System.IO.Path.Combine(directoryPath, this.Name), this.Extension);
            if (!File.Exists(solutioneFile))
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The given Solution file for import does not exist. {0}", solutioneFile));
                return;
            }

            using (var serviceContext = new CrmOrganizationServiceContext(connection))
            {
                try
                {
                    serviceContext.TryAccessCache(delegate(IOrganizationServiceCache cache)
                    {
                        cache.Mode = OrganizationServiceCacheMode.Disabled;
                    });

                    byte[] customizationFile = File.ReadAllBytes(solutioneFile);
                    var request = new ImportSolutionRequest
                    {
                        CustomizationFile = customizationFile,
                        OverwriteUnmanagedCustomizations = this.overwriteCustomizations,
                        PublishWorkflows = this.EnableSdkProcessingSteps
                    };

                    Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Importing Solution {0}. Please wait...", this.Name));
                    serviceContext.Execute(request);
                    Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Successfully imported Solution {0} to organization with Url {1}.", this.Name, this.OrganizationUrl));
                }
                catch (Exception exception)
                {
                    Log.LogError(string.Format(
                                    CultureInfo.CurrentCulture,
                                    "An error occurred while importing Solution {0} to Organization with Url {1}. [{2}]",
                                    this.Name, 
                                    this.OrganizationUrl,
                                    exception.Message));
                }
            }
        }

        private void ExportSolution(CrmConnection connection)
        {
            if (string.IsNullOrWhiteSpace(this.Extension))
            {
                Log.LogError("Required parameter missing: Extension");
                return;
            }

            string directoryPath = string.IsNullOrEmpty(this.Path)
                ? System.IO.Path.GetDirectoryName(this.BuildEngine.ProjectFileOfTaskNode)
                : this.Path;

            // ReSharper disable once AssignNullToNotNullAttribute
            string solutioneFile = string.Format(CultureInfo.CurrentCulture, "{0}.{1}", System.IO.Path.Combine(directoryPath, this.Name), this.Extension);
            using (var serviceContext = new CrmOrganizationServiceContext(connection))
            {
                try
                {
                    var exportSolutionRequest = new ExportSolutionRequest
                                                {
                                                    SolutionName = this.Name,
                                                    Managed = this.ExportAsManagedSolution
                                                };

                    Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Exporting Solution {0}. Please wait...", this.Name));
                    var response = serviceContext.Execute(exportSolutionRequest) as ExportSolutionResponse;
                    if (response == null)
                    {
                        Log.LogError(string.Format(CultureInfo.CurrentCulture, "An error occurred in in exporting Solution {0} from organization with Url {1}.", this.Name, this.OrganizationUrl));    
                    }
                    else
                    {
                        byte[] exportSolutionFileContentsBytes = response.ExportSolutionFile;
                        File.WriteAllBytes(solutioneFile, exportSolutionFileContentsBytes);
                        Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Successfully exported Solution {0} from organization with Url {1}.", this.Name, this.OrganizationUrl));    
                    }
                }
                catch (Exception exception)
                {
                    Log.LogError(string.Format(
                                    CultureInfo.CurrentCulture,
                                    "An error occurred while exporting Solution {0} from Organization with Url {1}. [{2}]",
                                    this.Name,
                                    this.OrganizationUrl,
                                    exception.Message));
                }
            }
        }
    }
}
