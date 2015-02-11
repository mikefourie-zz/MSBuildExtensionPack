//-----------------------------------------------------------------------
// <copyright file="SmartExec.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using Microsoft.Build.Framework;

    /// <summary>
    /// Runs a specified program or command without blocking the UI. This is similar to
    /// the Exec Task: http://msdn.microsoft.com/en-us/library/x8zx72cd.aspx.
    /// <para/>This task is useful when you need to run a long command-line task during the build process.
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///   </PropertyGroup>
    ///   <Import Project="$(TPath)"/>
    ///   <Target Name="Default">
    ///     <MSBuild.ExtensionPack.Framework.SmartExec Command="iisreset.exe"/>
    ///     <MSBuild.ExtensionPack.Framework.SmartExec Command="copy &quot;d:\a\*&quot; &quot;d:\b\&quot; /Y"/>
    ///   </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class SmartExec : BaseAppDomainIsolatedTask
    {
        private Process process;

        protected delegate void DataReceivedHandler();

        /// <summary>
        /// Gets or sets the command(s) to run. These can be system commands,
        /// such as attrib, or an executable, such as program.exe, runprogram.bat, or setup.msi.
        /// This parameter can contain multiple lines of commands (each command on a new-line).
        /// Alternatively, you can place multiple commands in a batch file and run it using this parameter.
        /// </summary>
        [Required]
        public string Command { get; set; }
        
        /// <summary>
        /// Gets or sets the success exit code for the command. Default is zero (0).
        /// </summary>
        /// <remarks>No Exec Equivalent</remarks>
        public int SuccessExitCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore the command exit code.
        /// If true, the task ignores the exit code provided by the executed command.
        /// Otherwise, the task returns false if the executed command returns an exit code
        /// that does not match <see cref="SuccessExitCode"/>.
        /// </summary>
        /// <remarks>Exec Equivalent: IgnoreExitCode</remarks>
        public bool IgnoreExitCode { get; set; }

        protected override void InternalExecute()
        {
            string[] tokens = this.Command.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            List<string> commands = new List<string>();
            foreach (string command in tokens.Select(token => token.Trim()).Where(command => !string.IsNullOrEmpty(command)))
            {
                commands.Add(command);
                this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.CurrentCulture, "Command: {0}", command));
            }

            if (commands.Count == 0)
            {
                this.LogTaskMessage("Fatal input error: no command(s) specified");
                return;
            }

            // Execute commands and collect input
            foreach (string fileName in commands.Select(command => HasCommandArguments(command) ? CreateBatchProgram(command) : command))
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Execute: {0}", fileName));
                ProcessStartInfo startInfo = GetCommandLine(fileName);
                using (BackgroundWorker worker = new BackgroundWorker())
                {
                    worker.DoWork += (s, e) =>
                    {
                        this.process = Process.Start(startInfo);

                        // Invoke stdOut and stdErr readers - each has its own thread to guarantee that they aren't
                        // blocked by, or cause a block to, the actual process running (or the gui).
                        DataReceivedHandler stdOutHandler = this.ReadStdOut;
                        stdOutHandler.BeginInvoke(null, null);
                        DataReceivedHandler stdErrHandler = this.ReadStdErr;
                        stdErrHandler.BeginInvoke(null, null);

                        this.process.WaitForExit();
                    };
                    worker.RunWorkerAsync();
                    while (worker.IsBusy)
                    {
                        Application.DoEvents();
                    }

                    int exitCode = this.process.ExitCode;
                    this.process.Close();
                    if (!(this.IgnoreExitCode || (exitCode == this.SuccessExitCode)))
                    {
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "{0} failed with exit code: {1}", fileName, exitCode));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the command arguments from the command string.
        /// </summary>
        /// <param name="command">The full command string with arguments</param>
        /// <returns>True if the command has arguments, false otherwise</returns>
        private static bool HasCommandArguments(string command)
        {
            string result = string.Empty;
            Regex regex = new Regex(@"(?imnx-s:^((\""[^\""]+\"")|([^\ ]+))(?<Arguments>.*))");
            if (regex.IsMatch(command))
            {
                result = regex.Match(command).Groups["Arguments"].Value.Trim();
            }

            return !string.IsNullOrEmpty(result);
        }

        /// <summary>
        /// Creates a batch program file containing the command.
        /// </summary>
        /// <param name="command">The full command string with arguments</param>
        /// <returns>The batch program file path</returns>
        private static string CreateBatchProgram(string command)
        {
            string tmpFilePath = System.IO.Path.GetTempFileName();
            string batFilePath = string.Format(CultureInfo.InvariantCulture, "{0}.bat", tmpFilePath);
            File.Move(tmpFilePath, batFilePath);
            File.WriteAllText(batFilePath, command);
            return batFilePath;
        }

        /// <summary>
        /// Gets a command process object with the command specified.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>Returns a command prompt start information that is ready to start</returns>
        private static ProcessStartInfo GetCommandLine(string command)
        {
            return new ProcessStartInfo
            {
                FileName = command,
                Arguments = string.Empty,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                ErrorDialog = false
            };
        }

        /// <summary>
        /// Handles reading of stdout and firing an event for
        /// every line read
        /// </summary>
        private void ReadStdOut()
        {
            try
            {
                string str;
                while ((str = this.process.StandardOutput.ReadLine()) != null)
                {
                    this.LogTaskMessage(MessageImportance.High, str);
                }
            }
            catch (IOException)
            {
            }
            catch (OutOfMemoryException)
            {
            }
        }

        /// <summary>
        /// Handles reading of stdErr
        /// </summary>
        private void ReadStdErr()
        {
            try
            {
                string str;
                while ((str = this.process.StandardError.ReadLine()) != null)
                {
                    Log.LogError(str);
                }
            }
            catch (IOException)
            {
            }
            catch (OutOfMemoryException)
            {
            }
        }
    }
}