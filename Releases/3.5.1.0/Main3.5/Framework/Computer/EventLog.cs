//-----------------------------------------------------------------------
// <copyright file="EventLog.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CheckExists</i> (<b>Required: </b>LogName <b>Optional: </b>MachineName <b>Output: </b>Exists)</para>
    /// <para><i>Clear</i> (<b>Required: </b> LogName <b>Optional: </b>MachineName)</para>
    /// <para><i>Create</i> (<b>Required: </b>LogName <b>Optional: </b>MaxSize, Retention, MachineName)</para>
    /// <para><i>Delete</i> (<b>Required: </b>LogName <b>Optional: </b>MachineName)</para>
    /// <para><i>Modify</i> (<b>Required: </b>LogName <b>Optional: </b>MaxSize, Retention, MachineName)</para>
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
    ///         <!-- Delete an eventlog -->
    ///         <MSBuild.ExtensionPack.Computer.EventLog TaskAction="Delete" LogName="DemoEventLog"/>
    ///         <!-- Check whether an eventlog exists -->
    ///         <MSBuild.ExtensionPack.Computer.EventLog TaskAction="CheckExists" LogName="DemoEventLog">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.EventLog>
    ///         <Message Text="DemoEventLog Exists: $(DoesExist)"/>
    ///         <!-- Create whether an eventlog -->
    ///         <MSBuild.ExtensionPack.Computer.EventLog TaskAction="Create" LogName="DemoEventLog"  MaxSize="20" Retention="14"/>
    ///         <MSBuild.ExtensionPack.Computer.EventLog TaskAction="CheckExists" LogName="DemoEventLog">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.EventLog>
    ///         <Message Text="DemoEventLog Exists: $(DoesExist)"/>
    ///         <!-- Various other quick tasks -->
    ///         <MSBuild.ExtensionPack.Computer.EventLog TaskAction="Clear" LogName="DemoEventLog"/>
    ///         <MSBuild.ExtensionPack.Computer.EventLog TaskAction="Modify" LogName="DemoEventLog"  MaxSize="55" Retention="25"/>
    ///         <MSBuild.ExtensionPack.Computer.EventLog TaskAction="Delete" LogName="DemoEventLog"/>
    ///         <MSBuild.ExtensionPack.Computer.EventLog TaskAction="CheckExists" LogName="DemoEventLog">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.EventLog>
    ///         <Message Text="Exists: $(DoesExist)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class EventLog : BaseTask
    {
        private const string CheckExistsTaskAction = "CheckExists";
        private const string ClearTaskAction = "Clear";
        private const string CreateTaskAction = "Create";
        private const string DeleteTaskAction = "Delete";
        private const string ModifyTaskAction = "Modify";

        [DropdownValue(CheckExistsTaskAction)]
        [DropdownValue(ClearTaskAction)]
        [DropdownValue(CreateTaskAction)]
        [DropdownValue(DeleteTaskAction)]
        [DropdownValue(ModifyTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the size of the max.
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(ModifyTaskAction, false)]
        public int MaxSize { get; set; }

        /// <summary>
        /// Sets the retention. Any value > 0 is interpreted as days to retain. Use -1 for 'Overwrite as needed'. Use -2 for 'Never Overwrite'
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(ModifyTaskAction, false)]
        public int Retention { get; set; }

        /// <summary>
        /// Sets the name of the Event Log
        /// </summary>
        [Required]
        [TaskAction(CheckExistsTaskAction, true)]
        [TaskAction(ClearTaskAction, true)]
        [TaskAction(CreateTaskAction, true)]
        [TaskAction(DeleteTaskAction, true)]
        [TaskAction(ModifyTaskAction, true)]
        public string LogName { get; set; }

        /// <summary>
        /// Gets a value indicating whether the event log exists.
        /// </summary>
        [Output]
        [TaskAction(CheckExistsTaskAction, false)]
        public bool Exists { get; set; }

        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(ClearTaskAction, false)]
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(DeleteTaskAction, false)]
        [TaskAction(ModifyTaskAction, false)]
        public override string MachineName
        {
            get { return base.MachineName; }
            set { base.MachineName = value; }
        }
        
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
                case "Clear":
                    this.Clear();
                    break;
                case "Modify":
                    this.Modify();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Modify()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Modifying EventLog: {0} on {1}", this.LogName, this.MachineName));
            if (System.Diagnostics.EventLog.Exists(this.LogName, this.MachineName))
            {
                System.Diagnostics.EventLog el = new System.Diagnostics.EventLog(this.LogName, this.MachineName);
                this.ConfigureEventLog(el);
            }
            else
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "EventLog does not exist: {0}", this.LogName));
            }
        }

        private void Delete()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting EventLog: {0} on: {1}", this.LogName, this.MachineName));
            if (System.Diagnostics.EventLog.Exists(this.LogName, this.MachineName))
            {
                System.Diagnostics.EventLog.Delete(this.LogName, this.MachineName);
            }
        }

        private void CheckExists()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking EventLog exists: {0} on: {1}", this.LogName, this.MachineName));
            this.Exists = System.Diagnostics.EventLog.Exists(this.LogName, this.MachineName);
        }

        private void Create()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating EventLog: {0} on: {1}", this.LogName, this.MachineName));
            if (!System.Diagnostics.EventLog.Exists(this.LogName, this.MachineName))
            {
                EventSourceCreationData ecd = new EventSourceCreationData(this.LogName, this.LogName) { MachineName = this.MachineName };
                System.Diagnostics.EventLog.CreateEventSource(ecd);

                System.Diagnostics.EventLog el = new System.Diagnostics.EventLog(this.LogName, this.MachineName);

                this.ConfigureEventLog(el);
            }
            else
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "EventLog already exists: {0} on: {1}", this.LogName, this.MachineName));
            }
        }

        private void ConfigureEventLog(System.Diagnostics.EventLog el)
        {
            if (this.MaxSize > 0)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting EventLog Size: {0}Mb", this.MaxSize));
                el.MaximumKilobytes = this.MaxSize * 1024;
            }

            if (this.Retention > 0)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting Retention: {0} days", this.Retention));
                el.ModifyOverflowPolicy(OverflowAction.OverwriteOlder, this.Retention);
            }
            else if (this.Retention == -1)
            {
                this.LogTaskMessage("Setting Retention to 'Overwrite As Needed'");
                el.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 0);
            }
            else if (this.Retention == -2)
            {
                this.LogTaskMessage("Setting Retention to 'Do Not Overwrite'");
                el.ModifyOverflowPolicy(OverflowAction.DoNotOverwrite, 0);
            }
        }

        private void Clear()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Clearing EventLog: {0}", this.LogName));
            if (System.Diagnostics.EventLog.Exists(this.LogName, this.MachineName))
            {
                using (System.Diagnostics.EventLog targetLog = new System.Diagnostics.EventLog(this.LogName, this.MachineName))
                {
                    targetLog.Clear();
                }
            }
            else
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid LogName Supplied: {0}", this.LogName));
            }
        }
    }
}