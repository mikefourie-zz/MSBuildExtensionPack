//-----------------------------------------------------------------------
// <copyright file="SqlCmdWrapper.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.SqlServer.Extended
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;

    internal sealed class SqlCmdWrapper
    {
        private readonly NameValueCollection environmentVars = new NameValueCollection();

        internal SqlCmdWrapper(string executable, string arguments, string workingDirectory)
        {
            this.Arguments = arguments;
            this.Executable = executable;
            this.WorkingDirectory = workingDirectory;
        }

        /// <summary>
        /// Gets the standard output.
        /// </summary>
        internal string StandardOutput { get; set; }

        /// <summary>
        /// Gets the standard error.
        /// </summary>
        internal string StandardError { get; set; }

        /// <summary>
        /// Gets the exit code.
        /// </summary>
        internal int ExitCode { get; private set; }

        /// <summary>
        /// Sets the working directory.
        /// </summary>
        internal string WorkingDirectory { get; set; }

        /// <summary>
        /// Sets the Executable.
        /// </summary>
        internal string Executable { get; set; }

        /// <summary>
        /// Sets the arguments.
        /// </summary>
        internal string Arguments { get; set; }

        internal NameValueCollection EnvironmentVariables
        {
            get { return this.environmentVars; }
        }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        /// <returns>int</returns>
        public int Execute()
        {
            Process proc = null;

            try
            {
                var startInfo = new ProcessStartInfo(this.Executable, this.Arguments)
                {
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = this.WorkingDirectory
                };

                foreach (string key in this.EnvironmentVariables)
                {
                    startInfo.EnvironmentVariables[key] = this.EnvironmentVariables[key];
                }

                proc = System.Diagnostics.Process.Start(startInfo);
                if (proc != null)
                {
                    this.StandardOutput = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(Int32.MaxValue);
                    this.StandardError = proc.ExitCode != 0 ? proc.StandardError.ReadToEnd() : string.Empty;
                }
            }
            finally
            {
                // get the exit code and release the process handle
                if (proc != null)
                {
                    if (!proc.HasExited)
                    {
                        // not exited yet within our timeout so kill the process
                        proc.Kill();

                        while (!proc.HasExited)
                        {
                            System.Threading.Thread.Sleep(50);
                        }
                    }

                    this.ExitCode = proc.ExitCode;
                    proc.Close();
                }
            }

            return this.ExitCode;
        }
    }
}