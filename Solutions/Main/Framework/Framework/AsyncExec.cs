//-----------------------------------------------------------------------
// <copyright file="AsyncExec.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Asynchronously runs a specified program or command with no arguments. This is similar to
    /// the Exec Task: http://msdn.microsoft.com/en-us/library/x8zx72cd.aspx.
    /// <para/>This task is useful when you need to run a fire-and-forget command-line task during the build process.
    /// <para/>Note that that is a fire and forget call. No errors are handled.
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
    ///     <MSBuild.ExtensionPack.Framework.AsyncExec Command="copy &quot;d:\a\*&quot; &quot;d:\b\&quot; /Y"/>
    ///   </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class AsyncExec : Task
    {
        /// <summary>
        /// Gets or sets the command(s) to run. These can be system commands,
        /// such as attrib, or an executable, such as program.exe, runprogram.bat, or setup.msi.
        /// This parameter can contain multiple lines of commands (each command on a new-line).
        /// Alternatively, you can place multiple commands in a batch file and run it using this parameter.
        /// </summary>
        [Required]
        public string Command { get; set; }

        /// <summary>
        /// Executes the build operation.
        /// </summary>
        /// <returns>true if the operation started with no exceptions; false otherwise.</returns>
        public override bool Execute()
        {
            var tokens = this.Command.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var commands = new List<string>(tokens.Length);
            foreach (string command in tokens.Select(token => token.Trim()).Where(command => !string.IsNullOrEmpty(command)))
            {
                commands.Add(command);
                this.Log.LogMessage("Command: {0}", command);
            }

            if (commands.Count == 0)
            {
                this.Log.LogMessage("Fatal input error: no command(s) specified");
                return false;
            }

            // Execute commands and collect input
            foreach (string fileName in commands.Select(command => HasCommandArguments(command) ? CreateBatchProgram(command) : command))
            {
                this.Log.LogMessage("\tExecute: {0}", fileName);
                var startInfo = GetCommandLine(fileName);
                Process.Start(startInfo);
            }

            return true;
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
            File.Copy(tmpFilePath, batFilePath, true);
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
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                ErrorDialog = false
            };
        }
    }
}