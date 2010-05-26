//-----------------------------------------------------------------------
// <copyright file="DlrTaskFactory.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// This task is based on code from (http://github.com/jredville/DlrTaskFactory). It is used here with permission.
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.TaskFactory
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.Build.Framework;

    /// <summary>
    /// A task factory that enables inline scripts to execute as part of an MSBuild-based build.
    /// </summary>
    /// <remarks>
    /// A more complete example of a task factory, that allows for Windows PowerShell scripts
    /// inside project files, can be found on MSDN Code Gallery:
    /// http://code.msdn.microsoft.com/PowershellFactory
    /// </remarks>
    public class DlrTaskFactory : ITaskFactory
    {
        /// <summary>
        /// The in and out parameters of the generated tasks.
        /// </summary>
        private IDictionary<string, TaskPropertyInfo> parameterGroup;

        /// <summary>
        /// The body of the script to execute.
        /// </summary>
        private XElement taskXml;

        /// <summary>
        /// Gets the name of the factory.
        /// </summary>
        /// <value>The name of the factory.</value>
        public string FactoryName
        {
            get { return GetType().Name; }
        }

        /// <summary>
        /// Gets the type of the task.
        /// </summary>
        /// <value>The type of the task.</value>
        public Type TaskType
        {
            get { return typeof(DlrTask); }
        }

        /// <summary>
        /// Initializes the factory for creating a task with the given script.
        /// </summary>
        /// <param name="taskName">Name of the task.</param>
        /// <param name="parameterGroup">The parameter group.</param>
        /// <param name="taskBody">The task body.</param>
        /// <param name="taskFactoryLoggingHost">The task factory logging host.</param>
        /// <returns>bool</returns>
        public bool Initialize(string taskName, IDictionary<string, TaskPropertyInfo> parameterGroup, string taskBody, IBuildEngine taskFactoryLoggingHost)
        {
            Contract.Requires(!string.IsNullOrEmpty(taskName));
            Contract.Requires(this.parameterGroup != null);
            Contract.Requires(taskBody != null);
            Contract.Requires(taskFactoryLoggingHost != null);

            this.parameterGroup = parameterGroup;
            this.taskXml = XElement.Parse(taskBody);

            return true;
        }

        /// <summary>
        /// Creates the task.
        /// </summary>
        /// <param name="taskFactoryLoggingHost">The task factory logging host.</param>
        /// <returns>ITask item</returns>
        public ITask CreateTask(IBuildEngine taskFactoryLoggingHost)
        {
            return new DlrTask(this, this.taskXml, taskFactoryLoggingHost);
        }

        /// <summary>
        /// Cleans up the task.
        /// </summary>
        /// <param name="task">The task.</param>
        public void CleanupTask(ITask task)
        {
            Contract.Requires(task != null);

            IDisposable disposableTask = task as IDisposable;
            if (disposableTask != null)
            {
                disposableTask.Dispose();
            }
        }

        /// <summary>
        /// Gets the task parameters.
        /// </summary>
        /// <returns>TaskPropertyInfo[]</returns>
        public TaskPropertyInfo[] GetTaskParameters()
        {
            return this.parameterGroup.Values.ToArray();
        }
    }
}