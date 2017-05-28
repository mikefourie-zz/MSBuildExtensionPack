//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="Organization.cs">(c) 2017 Mike Fourie and Contributors (http://www.MSBuildExtensionPack.com) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Crm
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using Microsoft.Build.Framework;
    using Microsoft.Xrm.Client;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Deployment;
    using Microsoft.Xrm.Sdk.Deployment.Proxy;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
 
    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Create</i> (<b>Required: </b> DeploymentUrl, DisplayName, Name, SqlServerInstance, SsrsUrl <b>Optional: </b> Timeout, ConnectionTimeout)</para>
    /// <para><i>UpdateSettings</i> (<b>Required: </b> OrganizationUrl, Settings) <b>Optional </b>ConnectionTimeout</para>
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
    ///         <!-- Create an Organization -->
    ///         <MSBuild.ExtensionPack.Crm.Organization TaskAction="Create" DeploymentUrl="http://crmwebserver/XRMDeployment/2011/Deployment.svc" Name="organization1" DisplayName="Organization 1" SqlServerInstance="MySqlServer" SsrsUrl="http://reports1/ReportServer" Timeout="20" />
    ///     </Target>
    ///     <Target Name="UpdateSettings">
    ///         <!-- Update an Organization's Settings -->
    ///         <ItemGroup>
    ///             <Settings Include="pricingdecimalprecision">
    ///                 <Value>2</Value>
    ///             </Settings>
    ///             <Settings Include="localeid">
    ///                 <Value>2057</Value>
    ///             </Settings>
    ///             <Settings Include="isauditenabled">
    ///                 <Value>false</Value>
    ///             </Settings>
    ///         </ItemGroup>
    ///         <MSBuild.ExtensionPack.Crm.Organization TaskAction="UpdateSetting" OrganizationUrl="http://crm/organization1" Settings="@(Settings)" />
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class Organization : BaseTask
    {
        private const string CreateOrganizationTaskAction = "Create";
        private const string UpdateSettingsTaskAction = "UpdateSetting";
        private const int WaitIntervalInMilliseconds = 60000;
        private int timeoutInMinutes = 20;
        private TimeSpan connectionTimeOut = new TimeSpan(0, 3, 0);

        /// <summary>
        /// Sets the Url of the Microsoft Dynamics Crm Deployment Service
        /// </summary>
        public string DeploymentUrl { get; set; }

        /// <summary>
        /// Sets the Name of the Organization.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Sets the Display name of the Organization
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Sets the name of the SQL Server instance that will host the database of the new Organization
        /// </summary>
        public string SqlServerInstance { get; set; }

        /// <summary>
        /// Sets the Url of the Organization, whose setting needs to be changed.
        /// </summary>
        public string OrganizationUrl { get; set; }

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
        /// Sets the Organization settings to update.
        /// </summary>
        public ITaskItem[] Settings { get; set; }

        /// <summary>
        /// The time in minutes for which the task would wait for the new Organization to be created. Default is 20.
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
        /// Sets the SSRS Url for the new Organization that is created.
        /// </summary>
        public string SsrsUrl { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case CreateOrganizationTaskAction:
                    this.CreateOrganization();
                    break;

                case UpdateSettingsTaskAction:
                    this.UpdateSettings();
                    break;

                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid Task Action passed: {0}", this.TaskAction));
                    break;
            }            
        }

        private static object ConvertCrmTypeToDotNetType(AttributeTypeCode typeCode, object value)
        {
            switch (typeCode)
            {
                case AttributeTypeCode.Boolean:
                    return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                case AttributeTypeCode.Customer:
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Money:
                case AttributeTypeCode.Owner:
                case AttributeTypeCode.PartyList:
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                case AttributeTypeCode.Uniqueidentifier:
                case AttributeTypeCode.CalendarRules:
                case AttributeTypeCode.Virtual:
                case AttributeTypeCode.ManagedProperty:
                case AttributeTypeCode.EntityName:
                    throw new NotSupportedException();
                case AttributeTypeCode.DateTime:
                case AttributeTypeCode.Decimal:
                    return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                case AttributeTypeCode.Double:
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                case AttributeTypeCode.Integer:
                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                case AttributeTypeCode.Memo:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                case AttributeTypeCode.Picklist:
                    return new OptionSetValue(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                case AttributeTypeCode.String:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                case AttributeTypeCode.BigInt:
                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                default:
                    throw new InvalidOperationException("Unsupported field type: [" + typeCode + "]");
            }
        }

        private void UpdateSettings()
        {
            if (string.IsNullOrWhiteSpace(this.OrganizationUrl) || !Uri.IsWellFormedUriString(this.OrganizationUrl, UriKind.Absolute))
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The Organization Url is not valid. {0}", this.OrganizationUrl));
                return;
            }

            if (this.Settings == null)
            {
                Log.LogError("Required parameter missing: Settings");
                return;
            }

            Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Connecting to Organization {0}.", this.OrganizationUrl));
            string connectionString = string.Format(CultureInfo.CurrentCulture, "Server={0};Timeout={1}", this.OrganizationUrl, this.ConnectionTimeout);
            var connection = CrmConnection.Parse(connectionString);
            using (var serviceContext = new CrmOrganizationServiceContext(connection))
            {
                try
                {
                    var request = new RetrieveEntityRequest
                    {
                        EntityFilters = EntityFilters.Attributes,
                        LogicalName = "organization"
                    };

                    var response = serviceContext.Execute(request) as RetrieveEntityResponse;
                    if (response == null)
                    {
                        Log.LogError(string.Format(
                                        CultureInfo.CurrentCulture,
                                        "No response was received while retrieving settings for Organization with Url {0}",
                                        this.OrganizationUrl));
                        return;
                    }

                    var columnSet = new ColumnSet();
                    foreach (var settingItem in this.Settings)
                    {
                        string settingName = settingItem.ItemSpec;
                        columnSet.AddColumn(settingName);
                        var setting = response.EntityMetadata.Attributes.First(e => e.LogicalName == settingName);
                        if (setting == null || setting.AttributeType == null)
                        {
                            Log.LogError(string.Format(
                                            CultureInfo.CurrentCulture,
                                            "No meta data for setting {0} was found.",
                                            settingName));
                            return;
                        }
                    }

                    var entityCollection = serviceContext.RetrieveMultiple(
                        new QueryExpression("organization")
                        {
                            ColumnSet = columnSet
                        });

                    if (entityCollection == null || entityCollection.Entities.Count == 0)
                    {
                        Log.LogError(string.Format(
                                        CultureInfo.CurrentCulture,
                                        "No setting was found for one of the settings"));
                        return;
                    }

                    var entity = entityCollection.Entities.First();
                    foreach (var settingItem in this.Settings)
                    {
                        string settingName = settingItem.ItemSpec;
                        string settingValue = settingItem.GetMetadata("value");
                        var setting = response.EntityMetadata.Attributes.First(e => e.LogicalName == settingName);
                        if (setting == null || setting.AttributeType == null)
                        {
                            Log.LogError(string.Format(
                                            CultureInfo.CurrentCulture,
                                            "No meta data was found for setting with Name {0} was found.", 
                                            settingName));
                            return;
                        }

                        entity.Attributes[settingName] = ConvertCrmTypeToDotNetType(setting.AttributeType.Value, settingValue);
                    }

                    serviceContext.Update(entity);
                    Log.LogMessage(MessageImportance.High, "The organization settings were updated successfully.");   
                }
                catch (Exception exception)
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "An error occurred while update settings for Organization with Url {0}. [{1}]", this.OrganizationUrl, exception.Message));   
                }
            }
        }

        private void CreateOrganization()
        {
            if (string.IsNullOrWhiteSpace(this.DeploymentUrl) || !Uri.IsWellFormedUriString(this.DeploymentUrl, UriKind.Absolute))
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The Deployment service URL is not valid. {0}", this.DeploymentUrl));
                return;
            }

            if (string.IsNullOrWhiteSpace(this.Name))
            {
                Log.LogError("Missing required parameter: Name");
                return;
            }

            if (string.IsNullOrWhiteSpace(this.DisplayName))
            {
                Log.LogError("Missing required parameter: Display Name");
                return;
            }

            if (string.IsNullOrWhiteSpace(this.SqlServerInstance))
            {
                Log.LogError("Missing required parameter: Sql Server Instance");
                return;
            }

            if (string.IsNullOrWhiteSpace(this.SsrsUrl) || !Uri.IsWellFormedUriString(this.SsrsUrl, UriKind.Absolute))
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The Sql Server Reporting Service URL is not valid. {0}", this.SsrsUrl));
                return;
            }

            Log.LogMessage(MessageImportance.Normal, string.Format(CultureInfo.CurrentCulture, "Connecting to the deployment service {0}.", this.DeploymentUrl));
            using (var service = ProxyClientHelper.CreateClient(new Uri(this.DeploymentUrl)))
            {
                var newOrganization = new Microsoft.Xrm.Sdk.Deployment.Organization
                {
                    FriendlyName = this.DisplayName,
                    UniqueName = this.Name,
                    SqlServerName = this.SqlServerInstance,
                    SrsUrl = this.SsrsUrl
                };

                try
                {
                    var request = new BeginCreateOrganizationRequest
                    {
                        Organization = newOrganization
                    };

                    var response = service.Execute(request) as BeginCreateOrganizationResponse;
                    if (response == null)
                    {
                        Log.LogError(string.Format(CultureInfo.CurrentCulture, "No response was received while creating Organization {0}", this.Name));
                        return;
                    }

                    var operationId = new EntityInstanceId
                    {
                        Id = response.OperationId
                    };

                    int waitCount = 0;
                    var organizationCreationStatus = service.Retrieve(DeploymentEntityType.DeferredOperationStatus, operationId) as DeferredOperationStatus;

                    // Wait for the organization to be created. Checking the stauts repeatedly
                    while (organizationCreationStatus != null && 
                        (organizationCreationStatus.State == DeferredOperationState.Processing || organizationCreationStatus.State == DeferredOperationState.Queued))
                    {
                        Thread.Sleep(WaitIntervalInMilliseconds);
                        Log.LogMessage(MessageImportance.High, "Processing...");
                        organizationCreationStatus = service.Retrieve(DeploymentEntityType.DeferredOperationStatus, operationId) as DeferredOperationStatus;
                        if (++waitCount > this.Timeout)
                        {
                            break;
                        }
                    }

                    if (waitCount >= this.Timeout)
                    {
                        Log.LogMessage(MessageImportance.High, string.Format(CultureInfo.CurrentCulture, "Your request for creation of Organization {0} is still being processed but the task has exceeded its timeout value of {1} minutes.", this.Name, this.Timeout));
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.High, string.Format(CultureInfo.CurrentCulture, "The Organization {0} was created successfully.", this.Name));
                    }
                }
                catch (Exception exception)
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "An error occurred while creating Organization  {0}. [{1}]", this.Name, exception.Message));
                }
            }
        }
    }
}