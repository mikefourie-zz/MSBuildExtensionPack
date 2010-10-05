//-----------------------------------------------------------------------
// <copyright file="AsyncExec.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Asynchronously runs a specified program or command with no arguments. This is similar to the
    /// the Exec Task: http://msdn.microsoft.com/en-us/library/x8zx72cd.aspx.
    /// <para/>This task is useful when you need to run a fire-and-forget command-line task during the build process.
    /// <para/>Note that that is a fire and forget call. No errors are handled. To use parameters, consider calling a batch file.
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///   </PropertyGroup>
    ///   <Import Project="$(TPath)"/>
    ///   <Target Name="Default">
    ///     <MSBuild.ExtensionPack.Framework.AsyncExec Command="iisreset.exe"/>
    ///   </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("")]
    public class AsyncExec : Task
    {
        /// <summary>
        /// Required String parameter.
        /// The command(s) to run. These can be any command that does not require arguments to start.
        /// This parameter can contain multiple commands (each command on a new-line).
        /// Alternatively, you can place multiple commands with parameters in a batch file and run it using this parameter.
        /// </summary>
        [Required]
        public string Command { get; set; }

        /// <summary>
        /// Optional Boolean parameter. Gets or sets a value indicating whether to provide verbose logging output.
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Executes the build operation.
        /// </summary>
        /// <returns>true if the operation started with no exceptions; false otherwise.</returns>
        public override bool Execute()
        {
            var tokens = this.Command.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var commands = new List<string>(tokens.Length);
            foreach (string token in tokens)
            {
                string command = token.Trim();
                if (!string.IsNullOrEmpty(command))
                {
                    commands.Add(command);
                    this.Log.LogMessage("Command: {0}", command);
                }
            }

            if (commands.Count == 0)
            {
                this.Log.LogMessage("Fatal input error: no command(s) specified");
                return false;
            }

            // Execute commands and collect input
            foreach (string command in commands)
            {
                string arguments = GetCommandArguments(command);
                if (!string.IsNullOrEmpty(arguments))
                {
                    this.Log.LogMessage("Arguments Removed: {0}", arguments);
                }

                string fileName = GetCommandFileName(command);
                this.Log.LogMessage("Execute: {0}", fileName);
                var startInfo = GetCommandLine(fileName);
                Process process = Process.Start(startInfo);
                process.Start();
            }

            return true;
        }

        /// <summary>
        /// Gets the command file name from the command string.
        /// </summary>
        /// <param name="command">The full command string with arguments</param>
        /// <returns>The file name of the command</returns>
        private static string GetCommandFileName(string command)
        {
            string result = string.Empty;
            Regex regex = new Regex(@"(?imnx-s:^((\""(?<FileName>[^\""]+)\"")|(?<FileName>[^\ ]+)).*)");
            if (regex.IsMatch(command))
            {
                result = regex.Match(command).Groups["FileName"].Value;
            }

            return result;
        }

        /// <summary>
        /// Gets the command arguments from the command string.
        /// </summary>
        /// <param name="command">The full command string with arguments</param>
        /// <returns>The string of arguments supplied to the command</returns>
        private static string GetCommandArguments(string command)
        {
            string result = string.Empty;
            Regex regex = new Regex(@"(?imnx-s:^((\""[^\""]+\"")|([^\ ]+))(?<Arguments>.*))");
            if (regex.IsMatch(command))
            {
                result = regex.Match(command).Groups["Arguments"].Value.Trim();
            }

            return result;
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
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                ErrorDialog = false
            };
        }
    }
}