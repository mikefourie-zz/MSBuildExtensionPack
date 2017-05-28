//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="Data.cs">(c) 2017 Mike Fourie and Contributors (http://www.MSBuildExtensionPack.com) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Crm
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Microsoft.Build.Framework;
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Client;
    using Microsoft.Xrm.Client.Services;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using MSBuild.ExtensionPack.Crm.Entities;
    
    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Import</i> (<b>Required: </b> OrganizationUrl, DataMapName, FilePath, SourceEntityName, TargetEntityName <b>Optional: </b> Overwrite, ConnectionTimeout)</para>
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
    ///         <!-- Import a data file -->
    ///         <MSBuild.ExtensionPack.Crm.Data TaskAction="Import" OrganizationUrl="http://crm/organization1" DataMapName="DataMap1" SourceEntityName="Entity1" TargetEntityName="Entity1" FilePath="DataImportFile.csv" />
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class Data : BaseTask
    {
        private const string ImportTaskAction = "Import";
        private const int WaitIntervalInMilliseconds = 60000;
        private const int DataImportStatusSuccess = 4;
        private const int DataImportStatusFailed = 5;
        private int timeoutInMinutes = 20;
        private TimeSpan connectionTimeOut = new TimeSpan(0, 3, 0);

        /// <summary>
        /// Sets the Url of the Organization, whose setting needs to be changed.
        /// </summary>
        [Required]
        public string OrganizationUrl { get; set; }

        /// <summary>
        /// Sets the Name of the data map.
        /// </summary>
        [Required]
        public string DataMapName { get; set; }

        /// <summary>
        /// Sets the DataMap import file path.
        /// </summary>
        [Required]
        public string FilePath { get; set; }

        /// <summary>
        /// Sets the name of the source entity where the data file was produced from.
        /// </summary>
        [Required]
        public string SourceEntityName { get; set; }

        /// <summary>
        /// Sets the name of the target entity where the data imported to.
        /// </summary>
        [Required]
        public string TargetEntityName { get; set; }

        /// <summary>
        /// The time in minutes for which the task would wait for the new organization to be created. Default is 20.
        /// </summary>
        public int Timeout
        {
            get
            {
                return this.timeoutInMinutes;
            }

            set
            {
                this.timeoutInMinutes = value;
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
                    this.ImportData();
                    break;

                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid Task Action passed: {0}", this.TaskAction));
                    break;
            }
        }
        
        private static int GetImportStatus(CrmOrganizationServiceContext serviceContext, Guid importId)
        {
            var importEntity = serviceContext.Retrieve("import", importId, new ColumnSet(new[] { "statuscode" }));
            var attributeValue = importEntity.GetAttributeValue<OptionSetValue>("statuscode");
            return (attributeValue == null) ? 0 : attributeValue.Value;
        }

        private static Guid CreateImportEntity(CrmOrganizationServiceContext serviceContext, string sourceEntityName)
        {
            var import = new Import
            {
                Name = "Import of " + sourceEntityName,
                ModeCode = new OptionSetValue(0)
            };

            return serviceContext.Create(import);
        }

        private void ImportData()
        {
            if (!File.Exists(this.FilePath))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Could not find import data file {0}", this.FilePath));
                return;
            }

            Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Connecting to Organization {0}.", this.OrganizationUrl));
            string connectionString = string.Format(CultureInfo.CurrentCulture, "Server={0};Timeout={1}", this.OrganizationUrl, this.ConnectionTimeout);
            var connection = CrmConnection.Parse(connectionString);

            string content = File.ReadAllText(this.FilePath);

            using (var serviceContext = new CrmOrganizationServiceContext(connection))
            {
                try
                {
                    var dataMap = serviceContext.CreateQuery("importmap").FirstOrDefault(s => s.GetAttributeValue<string>("name") == this.DataMapName);
                    if (dataMap == null)
                    {
                        Log.LogError(string.Format(CultureInfo.CurrentCulture, "Could not find data map {0} in the organization with Url {1}", this.DataMapName, this.OrganizationUrl));
                        return;
                    }

                    var importId = CreateImportEntity(serviceContext, this.SourceEntityName);
                    this.CreateImportFileEntity(serviceContext, content, importId, dataMap.Id);

                    Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Importing data from File {0} to entity {1}", this.FilePath, this.TargetEntityName));
                    serviceContext.Execute(new ParseImportRequest { ImportId = importId });
                    serviceContext.Execute(new TransformImportRequest { ImportId = importId });
                    serviceContext.Execute(new ImportRecordsImportRequest { ImportId = importId });
                    serviceContext.TryAccessCache(delegate(IOrganizationServiceCache cache)
                    {
                        cache.Mode = OrganizationServiceCacheMode.Disabled; 
                    });

                    int waitCount = 0;
                    bool importCompleted = false;
                    do
                    {
                        int statusCode = GetImportStatus(serviceContext, importId);
                        switch (statusCode)
                        {
                            case DataImportStatusSuccess:
                                Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Successfully imported data file {0} to entity {1}.", this.FilePath, this.TargetEntityName));
                                importCompleted = true;
                                break;

                            case DataImportStatusFailed:
                                Log.LogError(string.Format(CultureInfo.CurrentCulture, "Import of data file {0} to entity {1} failed.", this.FilePath, this.TargetEntityName));
                                importCompleted = true;
                                break;
                        }

                        if (!importCompleted)
                        {
                            Log.LogMessage("Importing...");

                            Thread.Sleep(WaitIntervalInMilliseconds);
                            if (++waitCount > this.timeoutInMinutes)
                            {
                                Log.LogError("Import failed to complete during the maximum allocated time");
                                break;
                            }                            
                        }
                    } 
                    while (!importCompleted);
                }
                catch (Exception exception)
                {
                    Log.LogError(string.Format(
                                    CultureInfo.CurrentCulture,
                                    "An error occurred while importing Data file {0} to Entity {1} for Organization with Url {2}. [{3}]",
                                    this.FilePath,
                                    this.TargetEntityName,
                                    this.OrganizationUrl,
                                    exception.Message));
                }
            }
        }

        private void CreateImportFileEntity(CrmOrganizationServiceContext serviceContext, string content, Guid importId, Guid dataMapId)
        {
            var importFile = new ImportFile
            {
                Name = Path.GetFileName(this.FilePath),
                Source = this.FilePath,
                Content = content,
                SourceEntityName = this.SourceEntityName,
                TargetEntityName = this.TargetEntityName,
                FileTypeCode = new OptionSetValue(0),
                DataDelimiterCode = new OptionSetValue(1),
                FieldDelimiterCode = new OptionSetValue(2),
                IsFirstRowHeader = new bool?(true),
                EnableDuplicateDetection = new bool?(false),
                ProcessCode = new OptionSetValue(1),
                ImportId = new EntityReference("import", importId),
                ImportMapId = new EntityReference("importmap", dataMapId)
            };

            serviceContext.Create(importFile);
        }
    }
}
