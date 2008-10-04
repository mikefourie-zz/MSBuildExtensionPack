//-----------------------------------------------------------------------
// <copyright file="EventSource.cs">(c) FreeToDev. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Create</i> (<b>Required: </b>Source, LogName <b>Optional: </b>Force, MachineName)</para>
    /// <para><i>CheckExists</i> (<b>Required: </b>Source <b>Optional: </b>MachineName <b>Output: </b>Exists)</para>
    /// <para><i>Delete</i> (<b>Required: </b>Source <b>Optional: </b>MachineName)</para>
    /// <para><i>Log</i> (<b>Required: </b> Source, Description, LogType, EventId <b>Optional: </b>MachineName)</para>
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
    ///         <!-- Delete an event source -->
    ///         <MSBuild.ExtensionPack.Computer.EventSource TaskAction="Delete" Source="MyEventSource" LogName="Application"/>
    ///         <!-- Check an event source exists -->
    ///         <MSBuild.ExtensionPack.Computer.EventSource TaskAction="CheckExists" Source="MyEventSource" LogName="Application">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.EventSource>
    ///         <Message Text="Exists: $(DoesExist)"/>
    ///         <!-- Create an event source -->
    ///         <MSBuild.ExtensionPack.Computer.EventSource TaskAction="Create" Source="MyEventSource" LogName="Application"/>
    ///         <MSBuild.ExtensionPack.Computer.EventSource TaskAction="CheckExists" Source="MyEventSource" LogName="Application">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.EventSource>
    ///         <Message Text="Exists: $(DoesExist)"/>
    ///         <!-- Log an event -->
    ///         <MSBuild.ExtensionPack.Computer.EventSource TaskAction="Log" Source="MyEventSource" Description="Hello" LogType="Information" EventId="222"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class EventSource : BaseTask
    {
        private System.Diagnostics.EventLogEntryType logType = System.Diagnostics.EventLogEntryType.Error;

        /// <summary>
        /// Sets the event id.
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        /// Sets the Event Log Entry Type. Possible values are: Error, FailureAudit, Information, SuccessAudit, Warning.
        /// </summary>
        public string LogType
        {
            get { return this.logType.ToString(); }
            set { this.logType = (System.Diagnostics.EventLogEntryType) Enum.Parse(typeof(System.Diagnostics.EventLogEntryType), value); }
        }

        /// <summary>
        /// Sets the description for the logentry
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Sets the source name
        /// </summary>
        [Required]
        public string Source { get; set; }

        /// <summary>
        /// Sets the name of the log the source's entries are written to, e.g Application, Security, System, YOUREVENTLOG.
        /// </summary>
        public string LogName { get; set; }

        /// <summary>
        /// Set to true to delete any existing matching eventsource when creating 
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Gets a value indicating whether the EventSource exists.
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case "Create":
                    this.Create();
                    break;
                case "CheckExists":
                    this.CheckExists();
                    break;
                case "Delete":
                    this.Delete();
                    break;
                case "Log":
                    this.LogEvent();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Delete()
        {
            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Deleting EventSource: {0}", this.Source));
            if (System.Diagnostics.EventLog.SourceExists(this.Source, this.MachineName))
            {
                System.Diagnostics.EventLog.DeleteEventSource(this.Source, this.MachineName);
            }
        }

        private void LogEvent()
        {
            // Validation
            if (string.IsNullOrEmpty(this.EventId))
            {
                this.Log.LogError("EventId must be specified");
                return;
            }

            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Logging to EventSource: {0}", this.Source));

            if (!System.Diagnostics.EventLog.SourceExists(this.Source, this.MachineName))
            {
                this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "The EventSource does not exist: {0} on {1}", this.Source, this.MachineName));
            }
            else
            {
                using (System.Diagnostics.EventLog log = new System.Diagnostics.EventLog("Application", this.MachineName, this.Source))
                {
                    log.WriteEntry(this.Description, this.logType, Int32.Parse(this.EventId));
                }
            }
        }

        private void CheckExists()
        {
            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Checking EventSource exists: {0}", this.Source));
            this.Exists = System.Diagnostics.EventLog.SourceExists(this.Source, this.MachineName);
        }

        private void Create()
        {
            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Creating EventSource: {0}", this.Source));
            EventSourceCreationData data = new EventSourceCreationData(this.Source, this.LogName) { MachineName = this.MachineName };
            if (!System.Diagnostics.EventLog.SourceExists(this.Source, this.MachineName))
            {
                System.Diagnostics.EventLog.CreateEventSource(data);
            }
            else
            {
                if (this.Force)
                {
                    this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "The event source already exists. Force is true, attempting to delete: {0}", this.Source));
                    System.Diagnostics.EventLog.DeleteEventSource(this.Source, this.MachineName);
                    this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Creating EventSource: {0}", this.Source));
                    System.Diagnostics.EventLog.CreateEventSource(data);
                }
                else
                {
                    this.Log.LogError("The event source already exists. Use Force to delete and create.");
                    return;
                }
            }
        }
    }
}