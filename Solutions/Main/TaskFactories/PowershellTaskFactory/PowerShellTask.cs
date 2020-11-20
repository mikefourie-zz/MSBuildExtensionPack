//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="PowerShellTask.cs">(c) 2017 Mike Fourie and Contributors (https://github.com/mikefourie/MSBuildExtensionPack) under MIT License. See https://opensource.org/licenses/MIT </copyright>
// This task is based on code from (http://code.msdn.microsoft.com/PowershellFactory). It is used here with permission.
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.TaskFactory
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Management.Automation;
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
            // ExecutionPolicy Bypass
            InitialSessionState initial = InitialSessionState.CreateDefault();

            // Replace PSAuthorizationManager with a null manager
            // which ignores execution policy
            initial.AuthorizationManager = new AuthorizationManager("Microsoft.PowerShell");

            this.pipeline = RunspaceFactory.CreateRunspace().CreatePipeline();
            this.pipeline.Commands.AddScript(script);
            this.pipeline.Runspace.Open();
            this.pipeline.Runspace.SessionStateProxy.SetVariable("log", this.Log);
            this.pipeline.Output.DataReady += Output_DataReady;
            this.pipeline.Error.DataReady += Error_DataReady;
        }

        private void Error_DataReady(object sender, EventArgs e)
        {
	        var error = sender as PipelineReader<object>;
	        if (error != null)
            {
                while (error.Count > 0)
                {
                    Log.LogError(error.Read().ToString());
                }
            }
        }

        private void Output_DataReady(object sender, EventArgs e)
        {
	        var output = sender as PipelineReader<PSObject>;
	        if (output != null)
            {
                while (output.Count > 0)
                {
                    Log.LogMessage(output.Read().ToString());
                }
            }
        }

        public object GetPropertyValue(TaskPropertyInfo property)
        {
            return this.pipeline.Runspace.SessionStateProxy.GetVariable(property.Name);
        }

        public void SetPropertyValue(TaskPropertyInfo property, object value)
        {
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
