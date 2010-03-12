//-----------------------------------------------------------------------
// <copyright file="PowerShellTaskFactory.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// This task is based on code from (http://code.msdn.microsoft.com/PowershellFactory). It is used here with permission.
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.TaskFactory.PowerShell
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using Microsoft.Build.Framework;

    /// <summary>
    /// A task factory that enables inline PowerShell scripts to execute as part of an MSBuild-based build.
    /// </summary>
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

        public string FactoryName
        {
            get { return GetType().Name; }
        }

        public Type TaskType
        {
            get { return typeof(PowerShellTask); }
        }

        public bool Initialize(string taskName, IDictionary<string, TaskPropertyInfo> parameterGroup, string taskBody, IBuildEngine taskFactoryLoggingHost)
        {
            Contract.Requires(!string.IsNullOrEmpty(taskName));
            Contract.Requires(parameterGroup != null);
            Contract.Requires(taskBody != null);
            Contract.Requires(taskFactoryLoggingHost != null);

            this.paramGroup = parameterGroup;
            this.script = taskBody;

            return true;
        }

        public ITask CreateTask(IBuildEngine taskFactoryLoggingHost)
        {
            return new PowerShellTask(this.script);
        }

        public void CleanupTask(ITask task)
        {
            Contract.Requires(task != null);

            IDisposable disposableTask = task as IDisposable;
            if (disposableTask != null)
            {
                disposableTask.Dispose();
            }
        }

        public TaskPropertyInfo[] GetTaskParameters()
        {
            return this.paramGroup.Values.ToArray();
        }
    }
}
