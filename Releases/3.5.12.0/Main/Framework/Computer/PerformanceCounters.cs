//-----------------------------------------------------------------------
// <copyright file="PerformanceCounters.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Add</i> (<b>Required: </b> CategoryName, CounterList, CategoryHelp <b>Optional: </b> MultiInstance)</para>
    /// <para><i>GetValue</i> (<b>Required: </b> CategoryName, CounterName <b>Output: </b> Value, MachineName)</para>
    /// <para><i>Remove</i> (<b>Required: </b> CategoryName)</para>
    /// <para><b>Remote Execution Support:</b> Partial</para>
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
    ///             <CounterList Include="foobar.A">
    ///                 <CounterName>ACounter</CounterName>
    ///                 <CounterHelp>A Custom Counter</CounterHelp>
    ///                 <CounterType>CounterTimer</CounterType>
    ///             </CounterList>
    ///             <CounterList Include="foobar.A">
    ///                 <CounterName>AnotherCounter</CounterName>
    ///                 <CounterHelp>Another Custom Counter</CounterHelp>
    ///                 <CounterType>CounterTimer</CounterType>
    ///             </CounterList>
    ///         </ItemGroup>
    ///         <MSBuild.ExtensionPack.Computer.PerformanceCounters TaskAction="Add" CategoryName="YourCustomCategory" CategoryHelp="This is a custom performance counter category" CounterList="@(CounterList)" MultiInstance="true" />
    ///         <MSBuild.ExtensionPack.Computer.PerformanceCounters TaskAction="Remove" CategoryName="YourCustomCategory"/>
    ///         <MSBuild.ExtensionPack.Computer.PerformanceCounters TaskAction="GetValue" CategoryName="Memory" CounterName="Available MBytes">
    ///             <Output PropertyName="TheValue" TaskParameter="Value"/>
    ///         </MSBuild.ExtensionPack.Computer.PerformanceCounters>
    ///         <Message Text="Available MBytes: $(TheValue)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.11.0/html/7badb83c-0162-f8c7-afd0-969f571268fe.htm")]
    public class PerformanceCounters : BaseTask
    {
        private const string AddTaskAction = "Add";
        private const string GetValueAction = "GetValue";
        private const string RemoveTaskAction = "Remove";

        [DropdownValue(AddTaskAction)]
        [DropdownValue(GetValueAction)]
        [DropdownValue(RemoveTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the CategoryName
        /// </summary>
        [Required]
        [TaskAction(AddTaskAction, true)]
        [TaskAction(GetValueAction, true)]
        [TaskAction(RemoveTaskAction, true)]
        public string CategoryName { get; set; }

        /// <summary>
        /// Sets the description of the custom category.
        /// </summary>
        [TaskAction(AddTaskAction, true)]
        public string CategoryHelp { get; set; }

        /// <summary>
        /// Gets the value of the counter
        /// </summary>
        [Output]
        [TaskAction(GetValueAction, false)]
        public string Value { get; set; }

        /// <summary>
        /// Sets the name of the counter.
        /// </summary>
        [TaskAction(GetValueAction, true)]
        public string CounterName { get; set; }       

        /// <summary>
        /// Sets a value indicating whether to create a multiple instance performance counter. Default is false
        /// </summary>
        [TaskAction(AddTaskAction, false)]
        public bool MultiInstance { get; set; }

        /// <summary>
        /// Sets the TaskItem[] that specifies the counters to create as part of the new category.
        /// </summary>
        [TaskAction(AddTaskAction, true)]
        public ITaskItem[] CounterList { get; set; }

        [TaskAction(GetValueAction, false)]
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
                case "Add":
                    this.Add();
                    break;
                case "Remove":
                    this.Remove();
                    break;
                case "GetValue":
                    this.GetValue();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void GetValue()
        {
            if (PerformanceCounterCategory.Exists(this.CategoryName))
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Getting CounterName: {0}", this.CounterName));
                using (PerformanceCounter pc = new PerformanceCounter(this.CategoryName, this.CounterName, null, this.MachineName))
                {
                    this.Value = pc.NextValue().ToString(CultureInfo.CurrentCulture);
                }                
            }
            else
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Category not found: {0}", this.CategoryName));
            }
        }

        private void Add()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding Performance Counter: {0}", this.CategoryName));
            CounterCreationDataCollection colCounterCreationData = new CounterCreationDataCollection();
            colCounterCreationData.Clear();
            if (PerformanceCounterCategory.Exists(this.CategoryName))
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Removing Category: {0}", this.CategoryName));
                PerformanceCounterCategory.Delete(this.CategoryName);
            }
            
            for (int taskCount = 0; taskCount < this.CounterList.Length; taskCount++)
            {
                ITaskItem counter = this.CounterList[taskCount];
                string counterName = counter.GetMetadata("CounterName");
                string counterHelp = counter.GetMetadata("CounterHelp");
                PerformanceCounterType counterType = (PerformanceCounterType)Enum.Parse(typeof(PerformanceCounterType), counter.GetMetadata("CounterType"));
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Adding PerformanceCounter: {0}", counterName));
                CounterCreationData objCreateCounter = new CounterCreationData(counterName, counterHelp, counterType);
                colCounterCreationData.Add(objCreateCounter);
            }

            if (colCounterCreationData.Count > 0)
            {
                PerformanceCounterCategoryType categoryType = PerformanceCounterCategoryType.SingleInstance;

                if (this.MultiInstance)
                {
                    categoryType = PerformanceCounterCategoryType.MultiInstance;
                }

                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating Category: {0}", this.CategoryName));
                PerformanceCounterCategory.Create(this.CategoryName, this.CategoryHelp, categoryType, colCounterCreationData);
            }
        }

        private void Remove()
        {
            if (PerformanceCounterCategory.Exists(this.CategoryName))
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing Performance Counter: {0}", this.CategoryName));
                PerformanceCounterCategory.Delete(this.CategoryName);
            }
            else
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Category not found: {0}", this.CategoryName));
            }
        }
    }
}