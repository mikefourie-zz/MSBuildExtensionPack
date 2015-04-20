//-----------------------------------------------------------------------
// <copyright file="TeamBuild.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Tfs2013
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.TeamFoundation.Build.Client;
    using Microsoft.TeamFoundation.Client;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>GetLatest</i> (<b>Required: </b>TeamFoundationServerUrl, TeamProject <b>Optional: </b>BuildDefinitionName, Status <b>Output: </b>Info)</para>
    /// <para><i>Queue</i> (<b>Required: </b>TeamFoundationServerUrl, TeamProject, BuildDefinitionName <b>Optional: </b>DropLocation, CommandLineArguments)</para>
    /// <para><i>RelatedChangesets</i> (<b>Required: </b>TeamFoundationServerUrl, TeamProject <b>Optional: </b>BuildUri, BuildDefinitionName <b>Output: </b>Info, RelatedItems)</para>
    /// <para><i>RelatedWorkItems</i> (<b>Required: </b>TeamFoundationServerUrl, TeamProject <b>Optional: </b>BuildUri, BuildDefinitionName <b>Output: </b>Info, RelatedItems)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <PropertyGroup>
    ///         <TeamFoundationServerUrl>http://YOURSERVER:8080/</TeamFoundationServerUrl>
    ///         <TeamProject>YOURPROJECT</TeamProject>
    ///         <BuildUri></BuildUri>
    ///         <BuildDefinitionName>YOURDEF</BuildDefinitionName>
    ///     </PropertyGroup>
    ///     <Target Name="Default">
    ///         <!-- Get information on the latest build -->
    ///         <MSBuild.ExtensionPack.Tfs2013.TeamBuild TaskAction="GetLatest" TeamFoundationServerUrl="$(TeamFoundationServerUrl)" TeamProject="$(TeamProject)" BuildDefinitionName="$(BuildDefinitionName)">
    ///             <Output ItemName="BuildInfo" TaskParameter="Info"/>
    ///         </MSBuild.ExtensionPack.Tfs2013.TeamBuild>
    ///         <Message Text="BuildDefinitionUri: %(BuildInfo.BuildDefinitionUri)"/>
    ///         <Message Text="BuildFinished: %(BuildInfo.BuildFinished)"/>
    ///         <Message Text="BuildNumber: %(BuildInfo.BuildNumber)"/>
    ///         <Message Text="BuildUri: %(BuildInfo.BuildUri)"/>
    ///         <Message Text="CompilationStatus: %(BuildInfo.CompilationStatus)"/>
    ///         <Message Text="CompilationSuccess: %(BuildInfo.CompilationSuccess)"/>
    ///         <Message Text="DropLocation: %(BuildInfo.DropLocation)"/>
    ///         <Message Text="FinishTime: %(BuildInfo.FinishTime)"/>
    ///         <Message Text="KeepForever: %(BuildInfo.KeepForever)"/>
    ///         <Message Text="LabelName: %(BuildInfo.LabelName)"/>
    ///         <Message Text="LastChangedBy: %(BuildInfo.LastChangedBy)"/>
    ///         <Message Text="LastChangedOn: %(BuildInfo.LastChangedOn)"/>
    ///         <Message Text="LogLocation: %(BuildInfo.LogLocation)"/>
    ///         <Message Text="Quality: %(BuildInfo.Quality)"/>
    ///         <Message Text="Reason: %(BuildInfo.Reason)"/>
    ///         <Message Text="RequestedBy: %(BuildInfo.RequestedBy)"/>
    ///         <Message Text="RequestedFor: %(BuildInfo.RequestedFor)"/>
    ///         <Message Text="SourceGetVersion: %(BuildInfo.SourceGetVersion)"/>
    ///         <Message Text="StartTime: %(BuildInfo.StartTime)"/>
    ///         <Message Text="TestStatus: %(BuildInfo.TestStatus)"/>
    ///         <Message Text="TestSuccess: %(BuildInfo.TestSuccess)"/>
    ///         <!-- Queue a new build -->
    ///         <MSBuild.ExtensionPack.Tfs2013.TeamBuild TaskAction="Queue" TeamFoundationServerUrl="$(TeamFoundationServerUrl)" TeamProject="$(TeamProject)" BuildDefinitionName="$(BuildDefinitionName)"/>
    ///         <!-- Retrieve Changesets associated with a given build -->
    ///         <MSBuild.ExtensionPack.Tfs2013.TeamBuild TaskAction="RelatedChangesets" TeamFoundationServerUrl="$(TeamFoundationServerUrl)" TeamProject="$(TeamProject)" BuildUri="$(BuildUri)" BuildDefinitionName="$(BuildDefinitionName)">
    ///             <Output ItemName="Changesets" TaskParameter="RelatedItems"/>
    ///         </MSBuild.ExtensionPack.Tfs2013.TeamBuild>
    ///         <Message Text="ID = %(Changesets.Identity), Checked In By = %(Changesets.CheckedInBy), URI = %(Changesets.ChangesetUri), Comment = %(Changesets.Comment)"/>
    ///         <!-- Retrieve Work Items associated with a given build -->
    ///         <MSBuild.ExtensionPack.Tfs2013.TeamBuild TaskAction="RelatedWorkItems" TeamFoundationServerUrl="$(TeamFoundationServerUrl)" TeamProject="$(TeamProject)" BuildUri="$(BuildUri)" BuildDefinitionName="$(BuildDefinitionName)">
    ///             <Output ItemName="WorkItems" TaskParameter="RelatedItems"/>
    ///         </MSBuild.ExtensionPack.Tfs2013.TeamBuild>
    ///         <Message Text="ID = %(Workitems.Identity), Status = %(Workitems.Status), Title = %(Workitems.Title), Type  = %(Workitems.Type), URI = %(Workitems.WorkItemUri), AssignedTo = %(Workitems.AssignedTo)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class TeamBuild : BaseTask
    {
        private const string GetLatestTaskAction = "GetLatest";
        private const string QueueTaskAction = "Queue";
        private const string RelatedChangesetsTaskAction = "RelatedChangesets";
        private const string RelatedWorkItemsTaskAction = "RelatedWorkItems";

        private TfsTeamProjectCollection tfs;
        private IBuildServer buildServer;
        private IBuildDetail buildDetails;
        private string buildDefinition;
        private string buildStatus;
        private string teamProject;
        private string dropLocation;

        /// <summary>
        /// The Url of the Team Foundation Server.
        /// </summary>
        [Required]
        public string TeamFoundationServerUrl { get; set; }

        /// <summary>
        /// The name of the Team Project containing the build
        /// </summary>
        [Required]
        public string TeamProject
        {
            get { return this.buildDetails != null ? this.buildDetails.BuildDefinition.TeamProject : this.teamProject; }
            set { this.teamProject = value; }
        }

        /// <summary>
        /// The name of the build definition.
        /// </summary>
        public string BuildDefinitionName
        {
            get { return this.buildDetails != null ? this.buildDetails.BuildDefinition.Name : this.buildDefinition; }
            set { this.buildDefinition = value; }
        }

        /// <summary>
        /// The name of the Drop folder
        /// </summary>
        public string DropLocation
        {
            get { return this.buildDetails != null ? this.buildDetails.DropLocation : this.dropLocation; }
            set { this.dropLocation = value; }
        }

        /// <summary>
        /// Build Uri. Defaults to latest build.
        /// </summary>
        public string BuildUri
        {
            get; set;
        }

        /// <summary>
        /// Set the Status property of the build to filter the search. Supports: Failed, InProgress, NotStarted, PartiallySucceeded, Stopped, Succeeded
        /// </summary>
        public string Status
        {
            get { return this.buildDetails != null ? this.buildDetails.Status.ToString() : this.buildStatus; }
            set { this.buildStatus = value; }
        }

        /// <summary>
        /// Gets the Build information
        /// </summary>
        [Output]
        public ITaskItem Info { get; set; }

        /// <summary>
        /// Gets Related items associated with the build
        /// </summary>
        [Output]
        public ITaskItem[] RelatedItems { get; private set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            using (this.tfs = new TfsTeamProjectCollection(new Uri(this.TeamFoundationServerUrl)))
            {
                this.buildServer = (IBuildServer)this.tfs.GetService(typeof(IBuildServer));

                switch (this.TaskAction)
                {
                    case GetLatestTaskAction:
                        this.GetLatestInfo();
                        break;
                    case RelatedChangesetsTaskAction:
                        this.RelatedChangesets();
                        break;
                    case RelatedWorkItemsTaskAction:
                        this.RelatedWorkItems();
                        break;
                    case QueueTaskAction:
                        this.QueueBuild();
                        break;
                    default:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                        return;
                }
            }
        }

        private void RelatedChangesets()
        {
            if (string.IsNullOrEmpty(this.BuildUri))
            {
                this.GetLatestInfo();
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Retrieving changesets related to Build {0}", this.BuildUri));
            var build = this.buildServer.GetAllBuildDetails(new Uri(this.BuildUri));
            var changesets = InformationNodeConverters.GetAssociatedChangesets(build);
            var taskItems = new List<ITaskItem>();

            changesets.ForEach(
                x =>
                    {
                        ITaskItem item = new TaskItem(x.ChangesetId.ToString(CultureInfo.CurrentCulture));
                        item.SetMetadata("CheckedInBy", x.CheckedInBy ?? string.Empty);
                        item.SetMetadata("ChangesetUri", x.ChangesetUri != null ? x.ChangesetUri.ToString() : string.Empty);
                        item.SetMetadata("Comment", x.Comment ?? string.Empty);
                        taskItems.Add(item);
                    });

            this.RelatedItems = taskItems.ToArray();
        }

        private void RelatedWorkItems()
        {
            if (string.IsNullOrEmpty(this.BuildUri))
            {
                this.GetLatestInfo();
            }

            this.LogTaskMessage(string.Format(
                                    CultureInfo.CurrentCulture,
                                    "Retrieving Work Items related to Build {0}",
                                    this.BuildUri));

            var build = this.buildServer.GetAllBuildDetails(new Uri(this.BuildUri));
            var workitemSummaries = InformationNodeConverters.GetAssociatedWorkItems(build);
            var taskItems = new List<ITaskItem>();

            workitemSummaries.ForEach(
                x =>
                    {
                        ITaskItem item = new TaskItem(x.WorkItemId.ToString(CultureInfo.CurrentCulture));
                        item.SetMetadata("Status", x.Status);
                        item.SetMetadata("Title", x.Title ?? string.Empty);
                        item.SetMetadata("Type", x.Type ?? string.Empty);
                        item.SetMetadata("WorkItemUri", x.WorkItemUri != null ? x.WorkItemUri.ToString() : string.Empty);
                        item.SetMetadata("AssignedTo", x.AssignedTo ?? string.Empty);

                        taskItems.Add(item);
                    });

            this.RelatedItems = taskItems.ToArray();
        }

        private void QueueBuild()
        {
            if (string.IsNullOrEmpty(this.BuildDefinitionName))
            {
                Log.LogError("BuildDefinitionName is required to queue a build");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Attempt to queue Build {0} of project {1}", this.BuildDefinitionName, this.TeamProject));
            IBuildDefinition definition = this.buildServer.GetBuildDefinition(this.TeamProject, this.BuildDefinitionName);
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Identified as Build definition Id={0}", definition.Id));
            IBuildRequest request = definition.CreateBuildRequest();
            if (request == null)
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "Unable to create build request on {0}", this.TeamFoundationServerUrl));
                return;
            }

            if (this.DropLocation != null)
            {
                request.DropLocation = this.DropLocation;
            }
            
            // queue the build
            var queuedBuild = this.buildServer.QueueBuild(request, QueueOptions.None);
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "The build is now in state {0}", queuedBuild.Status));

            // After adding the new prams, the Uri started throwing Null exceptions.
            // Added check to handle it
            if (queuedBuild.Build != null && queuedBuild.Build.Uri != null)
            {
                this.BuildUri = queuedBuild.Build.Uri.ToString();
            }
        }

        private void GetLatestInfo()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting Latest Build Information for: {0}", this.BuildDefinitionName));
            IBuildDetailSpec buildDetailSpec = this.buildServer.CreateBuildDetailSpec(this.TeamProject);
            if (this.BuildDefinitionName != null)
            {
                buildDetailSpec.DefinitionSpec.Name = this.BuildDefinitionName;
            }
            
            // Only get latest
            buildDetailSpec.MaxBuildsPerDefinition = 1; 
            buildDetailSpec.QueryOrder = BuildQueryOrder.FinishTimeDescending; 
            if (!string.IsNullOrEmpty(this.Status))
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Filtering on Status: {0}", this.Status));
                buildDetailSpec.Status = (BuildStatus)System.Enum.Parse(typeof(BuildStatus), this.buildStatus);
            }
            
            // do the search and extract the details from the singleton expected result
            IBuildQueryResult results = this.buildServer.QueryBuilds(buildDetailSpec);

            if (results.Failures.Length == 0 && results.Builds.Length >= 1)
            {
                this.buildDetails = results.Builds[0];
                ITaskItem ibuildDef = new TaskItem(this.BuildDefinitionName);
                ibuildDef.SetMetadata("BuildDefinitionUri", this.buildDetails.BuildDefinitionUri.ToString());
                ibuildDef.SetMetadata("BuildFinished", this.buildDetails.BuildFinished.ToString());
                ibuildDef.SetMetadata("BuildNumber", this.buildDetails.BuildNumber ?? string.Empty);
                ibuildDef.SetMetadata("BuildUri", this.buildDetails.Uri.ToString());
                ibuildDef.SetMetadata("CompilationStatus", this.buildDetails.CompilationStatus.ToString());
                ibuildDef.SetMetadata("CompilationSuccess", this.buildDetails.CompilationStatus == BuildPhaseStatus.Succeeded ? "true" : "false");
                ibuildDef.SetMetadata("DropLocation", this.buildDetails.DropLocation ?? string.Empty);
                ibuildDef.SetMetadata("FinishTime", this.buildDetails.FinishTime.ToString());
                ibuildDef.SetMetadata("KeepForever", this.buildDetails.KeepForever.ToString());
                ibuildDef.SetMetadata("LabelName", this.buildDetails.LabelName ?? string.Empty);
                ibuildDef.SetMetadata("LastChangedBy", this.buildDetails.LastChangedBy ?? string.Empty);
                ibuildDef.SetMetadata("LastChangedOn", this.buildDetails.LastChangedOn.ToString());
                ibuildDef.SetMetadata("LogLocation", this.buildDetails.LogLocation ?? string.Empty);
                ibuildDef.SetMetadata("Quality", this.buildDetails.Quality ?? string.Empty);
                ibuildDef.SetMetadata("Reason", this.buildDetails.Reason.ToString());
                ibuildDef.SetMetadata("RequestedBy", this.buildDetails.RequestedBy ?? string.Empty);
                ibuildDef.SetMetadata("RequestedFor", this.buildDetails.RequestedFor ?? string.Empty);
                ibuildDef.SetMetadata("SourceGetVersion", this.buildDetails.SourceGetVersion ?? string.Empty);
                ibuildDef.SetMetadata("StartTime", this.buildDetails.StartTime.ToString() ?? string.Empty);
                ibuildDef.SetMetadata("Status", this.buildDetails.Status.ToString() ?? string.Empty);
                ibuildDef.SetMetadata("TestStatus", this.buildDetails.TestStatus.ToString() ?? string.Empty);
                ibuildDef.SetMetadata("TestSuccess", this.buildDetails.TestStatus == BuildPhaseStatus.Succeeded ? "true" : "false");
                this.Info = ibuildDef;
                this.BuildUri = this.buildDetails.Uri.ToString();
            }
        }
    }
}
