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
        private PowerShell powerShell;

        internal PowerShellTask(string script)
        {
            // ExecutionPolicy Bypass
            InitialSessionState initial = InitialSessionState.CreateDefault();

            // Replace PSAuthorizationManager with a null manager
            // which ignores execution policy
            initial.AuthorizationManager = new AuthorizationManager("Microsoft.PowerShell");

            var runspace = RunspaceFactory.CreateRunspace(initial);
            runspace.Open();
            runspace.SessionStateProxy.SetVariable("log", this.Log);

            powerShell = PowerShell.Create();
            powerShell.Runspace = runspace;
            powerShell.Streams.Information.DataAdded += StreamOnDataAdded;
            powerShell.Streams.Verbose.DataAdded += StreamOnDataAdded;
            powerShell.Streams.Debug.DataAdded += StreamOnDataAdded;
            powerShell.Streams.Warning.DataAdded += StreamOnDataAdded;
            powerShell.Streams.Error.DataAdded += StreamOnDataAdded;
            powerShell.AddScript(script);
        }

        private void StreamOnDataAdded(object sender, DataAddedEventArgs e) 
        {
            switch (sender)
            {
                case PSDataCollection<WarningRecord> c:
                    this.Log.LogWarning(c[e.Index].Message);
                    break;
                case PSDataCollection<DebugRecord> c:
                    this.Log.LogMessage(MessageImportance.Low, c[e.Index].Message);
                    break;
                case PSDataCollection<VerboseRecord> c:
                    this.Log.LogMessage(MessageImportance.Normal, c[e.Index].Message);
                    break;
                case PSDataCollection<InformationRecord> c:
                    this.Log.LogMessage(MessageImportance.High, c[e.Index].MessageData.ToString());
                    break;
                case PSDataCollection<ErrorRecord> c:
                    this.Log.LogError(c[e.Index].ErrorDetails?.Message ?? c[e.Index].Exception.Message, c[e.Index].Exception);
                    break;
            }
        }

        public object GetPropertyValue(TaskPropertyInfo property)
        {
            return this.powerShell.Runspace.SessionStateProxy.GetVariable(property.Name);
        }

        public void SetPropertyValue(TaskPropertyInfo property, object value)
        {
            this.powerShell.Runspace.SessionStateProxy.SetVariable(property.Name, value);
        }

        public override bool Execute()
        {
            this.powerShell.Invoke();
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
                if (this.powerShell.Runspace != null)
                {
                    this.powerShell.Runspace.Dispose();
                    this.powerShell = null;
                }
            }
        }
    }
}
