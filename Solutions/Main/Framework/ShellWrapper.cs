//-----------------------------------------------------------------------
// <copyright file="ShellWrapper.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;

    /// <summary>
    /// ShellExecute.
    /// </summary>
    internal sealed class ShellWrapper
    {
        private readonly System.Collections.Specialized.NameValueCollection environmentVars = new NameValueCollection();
        private readonly System.Text.StringBuilder stdOut = new System.Text.StringBuilder();
        private readonly System.Text.StringBuilder stdError = new System.Text.StringBuilder();

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
        /// A proxy for <see cref="Process.OutputDataReceived"/>.
        /// </summary>
        public event DataReceivedEventHandler OutputDataReceived;

        /// <summary>
        /// A proxy for <see cref="Process.ErrorDataReceived"/>.
        /// </summary>
        public event DataReceivedEventHandler ErrorDataReceived;

        /// <summary>
        /// Gets the standard output.
        /// </summary>
        public string StandardOutput
        {
            get { return this.stdOut.ToString(); }
        }

        /// <summary>
        /// Gets the standard error.
        /// </summary>
        public string StandardError
        {
            get { return this.stdError.ToString(); }
        }

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
            get { return this.environmentVars; }
        }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        /// <returns>int</returns>
        public int Execute()
        {
            using (Process proc = new Process())
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

                // Set event handlers to asynchronously read the output. We need to do this to avoid deadlock conditions.
                proc.OutputDataReceived += this.StandardOutHandler;
                proc.ErrorDataReceived += this.StandardErrorHandler;

                proc.StartInfo = startInfo;
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                // wait for exit after reading the streams to avoid deadlock
                proc.WaitForExit(int.MaxValue);

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
            }

            return this.ExitCode;
        }

        private void StandardErrorHandler(object sendingProcess, DataReceivedEventArgs lineReceived)
        {
            // Collect the error output.
            if (!string.IsNullOrEmpty(lineReceived.Data))
            {
                // Add the text to the collected errors.
                this.stdError.AppendLine(lineReceived.Data);
            }

            if (this.ErrorDataReceived != null)
            {
                this.ErrorDataReceived(sendingProcess, lineReceived);
            }
        }

        private void StandardOutHandler(object sendingProcess, DataReceivedEventArgs lineReceived)
        {
            // Collect the command output.
            if (!string.IsNullOrEmpty(lineReceived.Data))
            {
                // Add the text to the collected output.
                this.stdOut.AppendLine(lineReceived.Data);
            }

            if (this.OutputDataReceived != null)
            {
                this.OutputDataReceived(sendingProcess, lineReceived);
            }
        }
    }
}