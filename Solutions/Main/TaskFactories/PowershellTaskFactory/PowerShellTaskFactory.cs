//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="PowerShellTaskFactory.cs">(c) 2017 Mike Fourie and Contributors (http://www.MSBuildExtensionPack.com) under MIT License. See https://opensource.org/licenses/MIT </copyright>
// This task is based on code from (http://code.msdn.microsoft.com/PowershellFactory). It is used here with permission.
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.TaskFactory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Framework;

    /// <summary>
    /// A task factory that enables inline PowerShell scripts to execute as part of an MSBuild-based build.
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <AssemblyFile>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.TaskFactory.PowerShell.dll</AssemblyFile>
    ///         <AssemblyFile Condition="Exists('$(MSBuildProjectDirectory)\..\..\..\BuildBinaries\MSBuild.ExtensionPack.TaskFactory.PowerShell.dll')">$(MSBuildProjectDirectory)\..\..\..\BuildBinaries\MSBuild.ExtensionPack.TaskFactory.PowerShell.dll</AssemblyFile>
    ///     </PropertyGroup>
    ///     <UsingTask TaskFactory="PowershellTaskFactory" TaskName="Add" AssemblyFile="$(AssemblyFile)">
    ///         <ParameterGroup>
    ///             <First Required="true" ParameterType="System.Int32" />
    ///             <Second Required="true" ParameterType="System.Int32" />
    ///             <Sum Output="true" />
    ///         </ParameterGroup>
    ///         <Task>
    ///            <!-- Make this a proper CDATA section before running. -->
    ///           CDATA[
    ///         $log.LogMessage([Microsoft.Build.Framework.MessageImportance]"High", "Hello from PowerShell!  Now adding {0} and {1}.", $first, $second)
    ///         if ($first + $second -gt 100) {
    ///           $log.LogError("Oops!  I can't count that high. :(")
    ///         }
    ///         $sum = $first + $second
    ///       ]]
    ///         </Task>
    ///     </UsingTask>
    ///     <UsingTask TaskFactory="PowershellTaskFactory" TaskName="Subtract" AssemblyFile="$(AssemblyFile)">
    ///         <ParameterGroup>
    ///             <First Required="true" ParameterType="System.Int32" />
    ///             <Second Required="true" ParameterType="System.Int32" />
    ///             <Difference Output="true" />
    ///         </ParameterGroup>
    ///         <Task>
    ///            <!-- Make this a proper CDATA section before running. -->
    ///             CDATA[
    ///         $difference = $first - $second
    ///       ]
    ///         </Task>
    ///     </UsingTask>
    ///     <PropertyGroup>
    ///         <!-- Try making the sum go over 100 to see what happens. -->
    ///         <FirstNumber>5</FirstNumber>
    ///         <SecondNumber>8</SecondNumber>
    ///     </PropertyGroup>
    ///     <Target Name="Build">
    ///         <Add First="$(FirstNumber)" Second="$(SecondNumber)">
    ///             <Output TaskParameter="Sum" PropertyName="MySum" />
    ///         </Add>
    ///         <Message Importance="High" Text="The $(FirstNumber) + $(SecondNumber) = $(MySum)" />
    ///         <Subtract First="$(FirstNumber)" Second="$(SecondNumber)">
    ///             <Output TaskParameter="Difference" PropertyName="MyDifference" />
    ///         </Subtract>
    ///         <Message Importance="High" Text="The $(FirstNumber) - $(SecondNumber) = $(MyDifference)" />
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class PowerShellTaskFactory : ITaskFactory
    {
        /// <summary>
        /// The in and out parameters of the generated tasks.
        /// </summary>
        private IDictionary<string, TaskPropertyInfo> paramGroup;

        /// <summary>
        /// The body of the PowerShell script given by the project file.
        /// </summary>
        private string script;

        /// <summary>
        /// Get the Factory Name
        /// </summary>
        public string FactoryName
        {
            get { return GetType().Name; }
        }

        /// <summary>
        /// The type of Task
        /// </summary>
        public Type TaskType
        {
            get { return typeof(PowerShellTask); }
        }

        /// <summary>
        /// Initialize the Task Factory
        /// </summary>
        /// <param name="taskName">The name of the Task</param>
        /// <param name="parameterGroup">IDictionary</param>
        /// <param name="taskBody">The Task body</param>
        /// <param name="taskFactoryLoggingHost">IBuildEngine</param>
        /// <returns>bool</returns>
        public bool Initialize(string taskName, IDictionary<string, TaskPropertyInfo> parameterGroup, string taskBody, IBuildEngine taskFactoryLoggingHost)
        {
            this.paramGroup = parameterGroup;
            this.script = taskBody;

            return true;
        }

        /// <summary>
        /// Create a Task.
        /// </summary>
        /// <param name="taskFactoryLoggingHost">IBuildEngine</param>
        /// <returns>ITask</returns>
        public ITask CreateTask(IBuildEngine taskFactoryLoggingHost)
        {
            return new PowerShellTask(this.script);
        }

        /// <summary>
        /// Cleanup the Task
        /// </summary>
        /// <param name="task">ITask</param>
        public void CleanupTask(ITask task)
        {
            IDisposable disposableTask = task as IDisposable;
            if (disposableTask != null)
            {
                disposableTask.Dispose();
            }
        }

        /// <summary>
        /// Get the Task Parameters
        /// </summary>
        /// <returns>TaskPropertyInfo</returns>
        public TaskPropertyInfo[] GetTaskParameters()
        {
            return this.paramGroup.Values.ToArray();
        }
    }
}
