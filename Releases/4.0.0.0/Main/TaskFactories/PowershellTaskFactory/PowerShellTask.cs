//-----------------------------------------------------------------------
// <copyright file="PowerShellTask.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// This task is based on code from (http://code.msdn.microsoft.com/PowershellFactory). It is used here with permission.
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.TaskFactory.PowerShell
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Management.Automation.Runspaces;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// A task that executes a Windows PowerShell script.
    /// </summary>
    internal class PowerShellTask : Task, IGeneratedTask, IDisposable
    {
        /// <summary>
        /// The context that the Windows PowerShell script will run under.
        /// </summary>        
        private Pipeline pipeline;

        internal PowerShellTask(string script)
        {
            Contract.Requires(script != null);
            this.pipeline = RunspaceFactory.CreateRunspace().CreatePipeline();
            this.pipeline.Commands.AddScript(script);
            this.pipeline.Runspace.Open();
            this.pipeline.Runspace.SessionStateProxy.SetVariable("log", this.Log);
        }

        public object GetPropertyValue(TaskPropertyInfo property)
        {
            Contract.Requires(property != null);

            return this.pipeline.Runspace.SessionStateProxy.GetVariable(property.Name);
        }

        public void SetPropertyValue(TaskPropertyInfo property, object value)
        {
            Contract.Requires(property != null);
            Contract.Requires(value != null);
            this.pipeline.Runspace.SessionStateProxy.SetVariable(property.Name, value);
        }

        public override bool Execute()
        {
            this.pipeline.Invoke();
            return !Log.HasLoggedErrors;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.pipeline.Runspace != null)
                {
                    this.pipeline.Runspace.Dispose();
                    this.pipeline = null;
                }
            }
        }
    }
}
