//-----------------------------------------------------------------------
// <copyright file="SqlCmdWrapper.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------

namespace MSBuild.ExtensionPack.SqlServer
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;

    internal sealed class SqlCmdWrapper
    {
        private NameValueCollection envars = new NameValueCollection();

        internal SqlCmdWrapper(string executable, string arguments)
        {
            this.Arguments = arguments;
            this.Executable = executable;
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
            get { return this.envars; }
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
                    proc.WaitForExit(Int32.MaxValue);
                    this.StandardOutput = proc.StandardOutput.ReadToEnd();
                    this.StandardError = proc.StandardError.ReadToEnd();
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