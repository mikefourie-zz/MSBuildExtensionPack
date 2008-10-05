//-----------------------------------------------------------------------
// <copyright file="BaseTask.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack
{
    using System;
    using System.Globalization;
    using System.Management;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Provides a common task for all the MSBuildExtensionPack Tasks
    /// </summary>
    public abstract class BaseTask : Task
    {
        private string machineName;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        public string TaskAction { get; set; }

        /// <summary>
        /// Sets the MachineName.
        /// </summary>
        public string MachineName
        {
            get { return this.machineName ?? Environment.MachineName; }
            set { this.machineName = value; }
        }

        /// <summary>
        /// Sets the UserName
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Sets the UserPassword.
        /// </summary>
        public string UserPassword { get; set; }

        /// <summary>
        /// Sets the authority to be used to authenticate the specified user.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Set to true to log the full Exception Stack to the console.
        /// </summary>
        public bool LogExceptionStack { get; set; }

        internal ManagementScope Scope { get; set; }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        /// <returns>bool</returns>
        public override sealed bool Execute()
        {
            try
            {
                this.InternalExecute();
                return !this.Log.HasLoggedErrors;
            }
            catch (Exception ex)
            {
                this.GetLoggingVerbosity();
                this.Log.LogErrorFromException(ex, this.LogExceptionStack, true, null);
                return !this.Log.HasLoggedErrors;
            }
        }

        /// <summary>
        /// Determines whether the task is targeting the local machine
        /// </summary>
        /// <returns>bool</returns>
        internal bool TargetingLocalMachine()
        {
            if (string.Compare(this.MachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "This task does not support remote execution. Please remove the MachineName: {0}", this.MachineName));
                return false;
            }

            return true;
        }

        internal void LogTaskWarning(string message)
        {
            this.Log.LogWarning(message);
        }

        internal void GetManagementScope(string wmiNamespace)
        {
            this.Log.LogMessage(MessageImportance.High, string.Format(CultureInfo.CurrentCulture, "ManagementScope: {0}", "\\\\" + this.MachineName + wmiNamespace));
            if (string.Compare(this.MachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                this.Scope = new ManagementScope("\\\\" + this.MachineName + wmiNamespace);
            }
            else
            {
                ConnectionOptions options = new ConnectionOptions
                {
                    Username = this.UserName,
                    Password = this.UserPassword,
                    Authority = this.Authority
                };
                this.Scope = new ManagementScope("\\\\" + this.MachineName + wmiNamespace, options);
            }
        }

        /// <summary>
        /// This is the main InternalExecute method that all tasks should implement
        /// </summary>
        /// <remarks>
        /// LogError should be thrown in the event of errors
        /// </remarks>
        protected abstract void InternalExecute();

        private void GetLoggingVerbosity()
        {
            string s = Environment.GetEnvironmentVariable("LogExceptionStack", EnvironmentVariableTarget.Machine);

            if (!string.IsNullOrEmpty(s))
            {
                this.LogExceptionStack = Convert.ToBoolean(s, CultureInfo.CurrentCulture);
            }
        }
    }
}