//-----------------------------------------------------------------------
// <copyright file="ShellWrapper.cs">(c) FreeToDev. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// ShellExecute.
    /// </summary>
    internal sealed class ShellWrapper
    {
        public System.Collections.Specialized.NameValueCollection EnvironmentVariables = new NameValueCollection();

        public ShellWrapper(string executable, string arguments)
        {
            this.Executable = executable;
            this.Arguments = arguments;
        }

        public ShellWrapper(string executable)
        {
            this.Executable = executable;
        }

        /// <summary>
        /// Gets the standard output.
        /// </summary>
        public string StandardOutput { get; private set; }

        /// <summary>
        /// Gets the standard error.
        /// </summary>
        public string StandardError { get; private set; }

        /// <summary>
        /// Gets the exit code.
        /// </summary>
        public int ExitCode { get; private set; }

        /// <summary>
        /// Sets the working directory.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Sets the Executable.
        /// </summary>
        public string Executable { get; set; }

        /// <summary>
        /// Sets the arguments.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        /// <returns>int</returns>
        public int Execute()
        {
            Process proc = null;
            try
            {
                string cmdPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
                string commandLine = "/C \"" + cmdPath + " /S /C \"\"" + this.Executable + "\" " + this.Arguments;
                ProcessStartInfo startInfo = new ProcessStartInfo(cmdPath, commandLine)
                                                 {
                                                     UseShellExecute = false,
                                                     CreateNoWindow = true,
                                                     WorkingDirectory = this.WorkingDirectory,
                                                     RedirectStandardOutput = true,
                                                     RedirectStandardError = true
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