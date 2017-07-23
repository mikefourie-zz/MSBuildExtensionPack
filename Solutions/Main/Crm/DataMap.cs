//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="DataMap.cs">(c) 2017 Mike Fourie and Contributors (https://github.com/mikefourie/MSBuildExtensionPack) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Crm
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Client;
    using Microsoft.Xrm.Sdk;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Import</i> (<b>Required: </b> OrganizationUrl, Name, FilePath <b>Optional: </b> Overwrite, ConnectionTimeout)</para>
    /// <para><i>Delete</i> (<b>Required: </b> OrganizationUrl, Name)</para>
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
    ///         <!-- Import a Data Map -->
    ///         <MSBuild.ExtensionPack.Crm.DataMap TaskAction="Import" OrganizationUrl="http://crm/organization1" Name="organization1" FilePath="DataImportFile.xml" />
    ///     </Target>
    ///     <Target Name="Delete">
    ///         <!-- Delete a Data Map -->
    ///         <MSBuild.ExtensionPack.Crm.DataMap TaskAction="Delete" OrganizationUrl="http://crm/organization1" Name="organization1" />
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class DataMap : BaseTask
    {
        private const string ImportTaskAction = "Import";
        private const string DeleteTaskAction = "Delete";
        private bool overwrite = true;
        private TimeSpan connectionTimeOut = new TimeSpan(0, 3, 0);

        /// <summary>
        /// Sets the Url of the Organization, whose setting needs to be changed.
        /// </summary>
        [Required]
        public string OrganizationUrl { get; set; }

        /// <summary>
        /// Sets the Name of the Data Map.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Sets the Data Map import file path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Sets whether to overwrite existing Data Map. Default is true
        /// </summary>
        public bool Overwrite
        {
            get
            {
                return this.overwrite;
            }

            set
            {
                this.overwrite = value;
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
        /// When overridden in a derived class, executes the task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case ImportTaskAction:
                    this.ImportDataMap();
                    break;

                case DeleteTaskAction:
                    this.Delete();
                    break;

                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid Task Action passed: {0}", this.TaskAction));
                    break;
            }
        }

        private static Guid DeleteDataMap(CrmOrganizationServiceContext serviceContext, string dataMapName)
        {
            Entity entity = serviceContext.CreateQuery("importmap").FirstOrDefault(s => s.GetAttributeValue<string>("name") == dataMapName);
            if (entity == null)
            {
                return Guid.Empty;
            }

            Guid dataMapId = entity.Id;
            serviceContext.Delete("importmap", dataMapId);
            return dataMapId;
        }

        private void ImportDataMap()
        {
            if (string.IsNullOrWhiteSpace(this.FilePath))
            {
                this.Log.LogError("Required parameter missing: FilePath");
                return;
            }

            if (!File.Exists(this.FilePath))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Could not find Data Map file {0}", this.FilePath));
                return;
            }

            Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Connecting to Organization {0}.", this.OrganizationUrl));
            string connectionString = string.Format(CultureInfo.CurrentCulture, "Server={0};Timeout={1}", this.OrganizationUrl, this.ConnectionTimeout);
            var connection = CrmConnection.Parse(connectionString);
            var mapText = File.ReadAllText(this.FilePath);
            using (var serviceContext = new CrmOrganizationServiceContext(connection))
            {
                try
                {
                    var request = new ImportMappingsImportMapRequest
                    {
                        MappingsXml  = mapText,
                        ReplaceIds = true
                    };

                    if (this.overwrite)
                    {
                        DeleteDataMap(serviceContext, this.Name);
                    }

                    Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Importing Data Map {0}", this.Name));
                    serviceContext.Execute(request);
                    Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Successfully imported Data Map {0} to organization with Url {1}.", this.Name, this.OrganizationUrl));
                }
                catch (Exception exception)
                {
                    Log.LogError(string.Format(
                                    CultureInfo.CurrentCulture,
                                    "An error occurred while importing Data Map {0} to Organization with Url {1}. [{2}]",
                                    this.Name,
                                    this.OrganizationUrl,
                                    exception.Message));
                }
            }
        }

        private void Delete()
        {
            Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Connecting to the Organization {0}.", this.OrganizationUrl));
            string connectionString = string.Format(CultureInfo.CurrentCulture, "Server={0};Timeout={1}", this.OrganizationUrl, this.ConnectionTimeout);
            var connection = CrmConnection.Parse(connectionString);
            using (var serviceContext = new CrmOrganizationServiceContext(connection))
            {
                try
                {
                    var deleteDataMapId = DeleteDataMap(serviceContext, this.Name);
                    if (deleteDataMapId == Guid.Empty)
                    {
                        Log.LogWarning(string.Format(CultureInfo.CurrentCulture, "No Data Map with name {0} was found in Organization with Url {1}.", this.Name, this.OrganizationUrl));
                    }
                    else
                    {
                        Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Successfully deleted Data Map {0} from Organization with Url {1}.", this.Name, this.OrganizationUrl));
                    }
                }
                catch (Exception exception)
                {
                    Log.LogError(string.Format(
                                    CultureInfo.CurrentCulture,
                                    "An error occurred while deleting Data Map {0} from Organization with Url {1}. [{2}]",
                                    this.Name,
                                    this.OrganizationUrl,
                                    exception.Message));
                }
            }
        }
    }
}
