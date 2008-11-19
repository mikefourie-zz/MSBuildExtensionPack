//-----------------------------------------------------------------------
// <copyright file="ShellWrapper.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// ShellExecute.
    /// </summary>
    internal sealed class ShellWrapper
    {
        private static StringBuilder stdOut;
        private System.Collections.Specialized.NameValueCollection envars = new NameValueCollection();

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

        public NameValueCollection EnvironmentVariables
        {
            get { return this.envars; }
        }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        /// <returns>int</returns>
        public int Execute()
        {
            Process proc = new Process();
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(this.Executable, this.Arguments)
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

                // Set event handler to asynchronously read the output. We need to do this to avoid deadlock conditions.
                stdOut = new StringBuilder(string.Empty);
                proc.OutputDataReceived += SortOutputHandler;

                proc.StartInfo = startInfo;
                proc.Start();
                proc.BeginOutputReadLine();

                // its ok to read the one stream synchronously
                this.StandardError = proc.StandardError.ReadToEnd();

                // wait for exit after reading the streams to avoid deadlock
                proc.WaitForExit(Int32.MaxValue);

                // now we can read all the output.
                this.StandardOutput = stdOut.ToString();
            }
            finally
            {
                // get the exit code and release the process handle
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

            return this.ExitCode;
        }

        private static void SortOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                stdOut.Append(Environment.NewLine + outLine.Data);
            }
        }
    }
}