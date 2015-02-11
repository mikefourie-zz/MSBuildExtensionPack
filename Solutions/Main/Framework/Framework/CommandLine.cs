//-----------------------------------------------------------------------
// <copyright file="CommandLine.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Launches command-line executables with robust warning and error message
    /// integration in MSBuild and Visual Studio. This is an expanded version
    /// of the Exec Task: http://msdn.microsoft.com/en-us/library/x8zx72cd.aspx.
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///   </PropertyGroup>
    ///   <Import Project="$(TPath)"/>
    ///   <Target Name="Default" DependsOnTargets="FxCop;JSLint"/>
    ///   <Target Name="FxCop">
    ///     <PropertyGroup Condition="'$(FxCopEnabled)' == 'true'">
    ///       <!--
    ///         Regex for matching FxCop errors and warnings:
    ///           filename(line,column) : warning|error :? CAxxxx : <error message>
    ///           [Location not stored in Pdb] : warning|error :? CAxxxx : <error message>
    ///       -->
    ///       <FxCopErrorRegularExpression>(?imnx-s:^((\[Location\ not\ stored\ in\ Pdb\])|(Project)|((?&lt;File&gt;[^(]+)\((?&lt;Line&gt;\d+),(?&lt;Column&gt;\d+)\)))\s*:\s*\w+\s*:?\s*(?&lt;Message&gt;(?&lt;ErrorCode&gt;CA\d+)\s*:?\s*.*)$)</FxCopErrorRegularExpression>
    ///       <TargetBinaryRoot Condition="'$(TargetBinaryRoot)' == ''"></TargetBinaryRoot>
    ///       <TargetFileName Condition="'$(TargetFileName)' == ''"></TargetFileName>
    ///       <PublicRoot Condition="'$(PublicRoot)' == ''"></PublicRoot>
    ///       <FxCopVersion Condition="'$(FxCopVersion)' == ''">1.36</FxCopVersion>
    ///       <!--
    ///         Build wide rule exclusions (format: -<Namespace>#<CheckId>, e.g., -Microsoft.Design#CA1020):
    ///           None
    ///       -->
    ///       <FxCopRules Condition="'$(FxCopRules)' != ''">&quot;/ruleid:$(FxCopRules)&quot;</FxCopRules>
    ///     </PropertyGroup>
    ///     <!-- Use FxCopCmd.exe /? for information on the command-line switches used -->
    ///     <MSBuild.ExtensionPack.Framework.CommandLine
    ///       Command="&quot;$(PublicRoot)\FxCop\$(FxCopVersion)\FxCopCmd.exe&quot; /console /searchgac &quot;/file:$(TargetBinaryRoot)\$(TargetFileName)&quot; &quot;/directory:$(TargetBinaryRoot)&quot; $(FxCopRules)"
    ///       CustomErrorRegularExpression="$(FxCopErrorRegularExpression)"
    ///       ContinueOnError="true" />
    ///   </Target>
    ///   <ItemDefinitionGroup>
    ///     <JavaScript />
    ///   </ItemDefinitionGroup>
    ///   <Target Name="JSLint">
    ///     <PropertyGroup>
    ///       <!--
    ///       Regex for matching JSLint (http://www.jslint.com) errors and warnings:
    ///       -->
    ///       <JSLintErrorRegularExpression>(?imnx-s:^cscript\s+\"[^"]+\"\s+\&lt;\"(?&lt;File&gt;[^"]+)\".*\bLint\s+[^\d]+(?&lt;Line&gt;\d+)[^\d]+(?&lt;Column&gt;\d+)\:\s+(?&lt;Message&gt;.*\n.*)$)</JSLintErrorRegularExpression>
    ///       <PublicRoot Condition="'$(PublicRoot)' == ''"></PublicRoot>
    ///       <JSLintVersion Condition="'$(JSLintVersion)' == ''">1.0</JSLintVersion>
    ///     </PropertyGroup>
    ///     <ItemGroup>
    ///       <!--
    ///         Include all *.js files under the project folder and sub-folders
    ///         Exclude all *.js files under the project bin, obj, or objd folders and sub-folders
    ///         -->
    ///       <JavaScript Include="**\*.js" Exclude="**\bin\**\*.js;**\obj\**\*.js;**\objd\**\*.js" />
    ///     </ItemGroup>
    ///     <!-- Use cscript (Windows Script Host) to execute jslint.js -->
    ///     <!-- Information on JSLint: http://www.jslint.com/lint.html -->
    ///     <MSBuild.ExtensionPack.Framework.CommandLine
    ///       Command="&quot;cscript $(PublicRoot)\JSLint\$(JSLintVersion)\jslint.js&quot; &lt;&quot;%(JavaScript.FullPath)&quot;"
    ///       CustomErrorRegularExpression="$(JSLintErrorRegularExpression)"
    ///       ContinueOnError="true" />
    ///   </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class CommandLine : Task
    {
        /// <summary>
        /// The standard error format. Included as an error pattern, unless the
        /// <see cref="IgnoreStandardErrorWarningFormat"/> is set to true.
        /// </summary>
        private const string StandardErrorFormat = @"(?i:\berror\b)";

        /// <summary>
        /// The standard warning format. Included as an error pattern, unless the
        /// <see cref="IgnoreStandardErrorWarningFormat"/> is set to true.
        /// </summary>
        private const string StandardWarningFormat = @"(?i:\bwarning\b)";

        /// <summary>
        /// Gets or sets the command(s) to run. These can be system commands,
        /// such as attrib, or an executable, such as program.exe, runprogram.bat, or setup.msi.
        /// This parameter can contain multiple lines of commands (each command on a new-line).
        /// Alternatively, you can place multiple commands in a batch file and run it using this parameter.
        /// </summary>
        /// <remarks>Exec Equivalent: Command</remarks>
        [Required]
        public string Command { get; set; }

        /// <summary>
        /// Gets or sets the error regular expression.
        /// Leaving this unset will result in no output errors.
        /// </summary>
        /// <remarks>
        /// Exec Equivalent: CustomErrorRegularExpression
        ///   The regular expression object has no options set: RegexOptions.None.
        ///   This can be changed in the provided expression by using the regular
        ///   expression options group syntax. For information on .NET Regular Expressions:
        ///     http://msdn.microsoft.com/en-us/library/hs600312%28VS.71%29.aspx
        ///   Capturing groups can be defined for the following values:
        ///     * SubCategory - A description of the error type
        ///     * ErrorCode   - The error code
        ///     * HelpKeyword - Help keyword for the error
        ///     * File        - Path to the file
        ///     * Line        - The line where the error begins
        ///     * Column      - The column where the error begins
        ///     * EndLine     - The end line where the error ends
        ///     * EndColumn   - The end column where the error ends
        ///     * Message     - The error message
        ///   These values are used to integrate in to MSBuild and the IDE.
        ///   Examples:
        ///     NMake errors:
        ///       NMAKE : Nxxxx: {error message}
        ///       NMAKE : fatal error Uxxxx: {error message}
        ///       NMAKE : FXCOPxx: {error message}
        ///     RegEx for matching NMake errors:
        ///       ^NMAKE\s+:\s+(fatal\s+error\s+)?(?&lt;ErrorCode&gt;(FXCOP|U|N)\d+):\s+(?&lt;Message&gt;.*)$
        ///     File specific NMake or CS errors:
        ///       filename(line) : fatal error Uxxxx: {error message}
        ///       filename(line) : error CSxxxx: {error message}
        ///       filename(line,column) : error CSxxxx: {error message}
        ///     RegEx for matching file specific NMake or CS errors:
        ///       ^(?&lt;File&gt;[^(]+)\((?&lt;Line&gt;\d+)(,(?&lt;Column&gt;\d+))?\)\s*:\s+(fatal\s+)?error\s+(?&lt;ErrorCode&gt;(U|CS)\d+):\s+(?&lt;Message&gt;.*)$
        ///     CS reference errors:
        ///        filename: error CSxxxx: {error message}
        ///     RegEx for matching CS reference errors:
        ///        ^(?&lt;File&gt;.+?)\s*:\s+error\s+(?&lt;ErrorCode&gt;CS\d+):\s+(?&lt;Message&gt;.*)$
        /// </remarks>
        public string CustomErrorRegularExpression { get; set; }

        /// <summary>
        /// Gets or sets the warning regular expression.
        /// Leaving this unset will result in no output warnings.
        /// </summary>
        /// <remarks>
        /// Exec Equivalent: CustomWarningRegularExpression
        ///   See CustomErrorRegularExpression for information on regular expression
        ///   matching groups and examples.
        /// </remarks>
        public string CustomWarningRegularExpression { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the output is examined for
        /// standard errors and warnings. This does not override errors and
        /// warnings defined via <see cref="CustomErrorRegularExpression"/> and
        /// <see cref="CustomWarningRegularExpression"/>.
        /// </summary>
        /// <remarks>Exec Equivalent: IgnoreStandardErrorWarningFormat</remarks>
        public bool IgnoreStandardErrorWarningFormat { get; set; }

        /// <summary>
        /// Gets or sets the success exit code for the command. Default is zero (0).
        /// </summary>
        /// <remarks>No Exec Equivalent</remarks>
        public int SuccessExitCode { get; set; }

        /// <summary>
        /// Gets the Int32 exit code provided by the executed command.
        /// </summary>
        /// <remarks>Exec Equivalent: ExitCode</remarks>
        [Output]
        public int ExitCode { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore the command exit code.
        /// If true, the task ignores the exit code provided by the executed command.
        /// Otherwise, the task returns false if the executed command returns an exit code
        /// that does not match <see cref="SuccessExitCode"/>.
        /// </summary>
        /// <remarks>Exec Equivalent: IgnoreExitCode</remarks>
        public bool IgnoreExitCode { get; set; }

        /// <summary>
        /// Gets or sets the output items from the task. The Execute task does not set these itself.
        /// Instead, you can provide them as if it did set them, so that they can be used later in the project.
        /// </summary>
        /// <remarks>Exec Equivalent: Outputs</remarks>
        [Output]
        public ITaskItem[] Outputs { get; set; }

        /// <summary>
        /// Gets or sets the StdErr stream encoding. Specifies the encoding of the captured task standard error stream.
        /// The default is the current console output encoding.
        /// </summary>
        /// <remarks>Exec Equivalent: StdErrEncoding</remarks>
        [Output]
        public string StdErrEncoding { get; set; }

        /// <summary>
        /// Gets or sets the StdOut stream encoding. Specifies the encoding of the captured task standard output stream.
        /// The default is the current console output encoding.
        /// </summary>
        /// <remarks>Exec Equivalent: StdOutEncoding</remarks>
        [Output]
        public string StdOutEncoding { get; set; }

        /// <summary>
        /// Gets or sets the directory in which the command will run.
        /// </summary>
        /// <remarks>Exec Equivalent: WorkingDirectory</remarks>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Gets or sets the warning expression list.
        /// </summary>
        private ICollection<string> WarningExpressionList { get; set; }

        /// <summary>
        /// Gets or sets the error expression list.
        /// </summary>
        private ICollection<string> ErrorExpressionList { get; set; }

        /// <summary>
        /// Gets or sets the collected output from the command-line.
        /// </summary>
        private string CollectedOutput { get; set; }

        /// <summary>
        /// Executes the build operation.
        /// </summary>
        /// <returns>true if the operation succeeded; false otherwise.</returns>
        public override bool Execute()
        {
            // Assemble warning regular expression list
            this.WarningExpressionList = new List<string>();
            if (!this.IgnoreStandardErrorWarningFormat)
            {
                this.WarningExpressionList.Add(StandardWarningFormat);
            }

            if (!string.IsNullOrEmpty(this.CustomWarningRegularExpression))
            {
                this.WarningExpressionList.Add(this.CustomWarningRegularExpression);
            }

            foreach (string expression in this.WarningExpressionList)
            {
                this.Log.LogMessage("Warning RegEx: {0}", expression);
            }

            // Assemble error regular expression list
            this.ErrorExpressionList = new List<string>();
            if (!this.IgnoreStandardErrorWarningFormat)
            {
                this.ErrorExpressionList.Add(StandardErrorFormat);
            }

            if (!string.IsNullOrEmpty(this.CustomErrorRegularExpression))
            {
                this.ErrorExpressionList.Add(this.CustomErrorRegularExpression);
            }

            foreach (string expression in this.ErrorExpressionList)
            {
                this.Log.LogMessage("Error RegEx: {0}", expression);
            }

            // Assemble command list
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
                this.ExitCode = 1;
                return false;
            }

            // Execute commands and collect input
            foreach (string command in commands)
            {
                this.Log.LogMessage("Execute: {0}", command);
                var startInfo = this.GetCommandLine(command, this.WorkingDirectory, this.StdErrEncoding, this.StdOutEncoding);
                using (Process process = Process.Start(startInfo))
                {
                    this.Log.LogMessage("Collect Standard Output Stream");
                    while (!process.StandardOutput.EndOfStream || !process.HasExited)
                    {
                        this.CollectOutputLine(process.StandardOutput.ReadLine());
                    }

                    this.Log.LogMessage("Collect Standard Error Stream");
                    while (!process.StandardError.EndOfStream)
                    {
                        this.CollectOutputLine(process.StandardError.ReadLine());
                    }

                    this.ExitCode = process.ExitCode;
                }
            }

            return this.PerformMatching(this.CollectedOutput) && (this.IgnoreExitCode || this.ExitCode == this.SuccessExitCode);
        }

        /// <summary>
        /// Gets a command process object with the command specified.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="workingDirectory">The command working directory</param>
        /// <param name="standardErrorEncoding">The standard error stream encoding</param>
        /// <param name="standardOutputEncoding">The standard output stream encoding</param>
        /// <returns>Returns a command prompt start information that is ready to start</returns>
        /// <remarks>StdErr and StdOut are always redirected.</remarks>
        private ProcessStartInfo GetCommandLine(string command, string workingDirectory, string standardErrorEncoding, string standardOutputEncoding)
        {
            var process = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = string.Empty,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    ErrorDialog = false
                };

            if (!string.IsNullOrEmpty(standardErrorEncoding))
            {
                try
                {
                    process.StandardErrorEncoding = Encoding.GetEncoding(standardErrorEncoding);
                }
                catch (ArgumentException ex)
                {
                    this.Log.LogMessage("Non-fatal exception caught: invalid encoding specified for standard error stream: {0}", standardErrorEncoding);
                    this.Log.LogWarningFromException(ex);
                }
            }

            if (!string.IsNullOrEmpty(standardOutputEncoding))
            {
                try
                {
                    process.StandardOutputEncoding = Encoding.GetEncoding(standardOutputEncoding);
                }
                catch (ArgumentException ex)
                {
                    this.Log.LogMessage("Non-fatal exception caught: invalid encoding specified for standard output stream: {0}", standardOutputEncoding);
                    this.Log.LogWarningFromException(ex);
                }
            }

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                if (Directory.Exists(workingDirectory))
                {
                    process.WorkingDirectory = workingDirectory;
                }
                else
                {
                    this.Log.LogWarning("Non-fatal input error: provided working directory does not exist: {0}", workingDirectory);
                }
            }

            return process;
        }

        /// <summary>
        /// Performs matching across the entire text block.
        /// </summary>
        /// <param name="text">The block of output text</param>
        /// <returns>True if no error was encountered; false otherwise</returns>
        private bool PerformMatching(string text)
        {
            bool result = this.WarningExpressionList.Aggregate(true, (current, pattern) => current && this.PerformMatch(pattern, text, false));
            return this.ErrorExpressionList.Aggregate(result, (current, pattern) => current && this.PerformMatch(pattern, text, true));
        }

        /// <summary>
        /// Performs the regular expression match; if a match is found then an
        /// error or warning is logged.
        /// </summary>
        /// <param name="pattern">The pattern</param>
        /// <param name="text">The haystack</param>
        /// <param name="error">True if the pattern matches a build error; false otherwise</param>
        /// <returns>True if no error was encountered; false otherwise</returns>
        private bool PerformMatch(string pattern, string text, bool error)
        {
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(text))
            {
                return true;
            }

            bool result = true;
            Match match = Regex.Match(text, pattern, RegexOptions.None);
            while (match.Success)
            {
                // Get match groups (not all exist for every match)
                Group subCategory = match.Groups[@"SubCategory"];
                Group errorCode = match.Groups[@"ErrorCode"];
                Group helpKeyword = match.Groups[@"HelpKeyword"];
                Group file = match.Groups[@"File"];
                Group line = match.Groups[@"Line"];
                Group column = match.Groups[@"Column"];
                Group endLine = match.Groups[@"EndLine"];
                Group endColumn = match.Groups[@"EndColumn"];
                Group message = match.Groups[@"Message"];

                // Get start line number (if exists)
                int lineInt = 0;
                if (line.Success && !int.TryParse(line.Value, out lineInt))
                {
                    lineInt = 0;
                }

                // Get end line number (if exists)
                int endLineInt = 0;
                if (endLine.Success && !int.TryParse(endLine.Value, out endLineInt))
                {
                    endLineInt = 0;
                }

                // Get column number (if exists)
                int columnInt = 0;
                if (column.Success && !int.TryParse(column.Value, out columnInt))
                {
                    columnInt = 0;
                }

                // Get end column number (if exists)
                int endColumnInt = 0;
                if (endColumn.Success && !int.TryParse(column.Value, out columnInt))
                {
                    endColumnInt = 0;
                }

                if (error)
                {
                    // Record Fail
                    result = false;
                    this.Log.LogError(
                        subCategory.Success ? subCategory.Value : string.Empty, // Sub-category
                        errorCode.Success ? errorCode.Value : string.Empty,     // Error code
                        helpKeyword.Success ? helpKeyword.Value : string.Empty, // Help keyword
                        file.Success ? file.Value : string.Empty,               // File
                        lineInt,                                                  // Line number
                        columnInt,                                                // Column number
                        endLineInt,                                               // End line number
                        endColumnInt,                                             // End column number
                        message.Success ? message.Value : string.Empty);        // Message
                }
                else
                {
                    this.Log.LogWarning(
                        subCategory.Success ? subCategory.Value : string.Empty, // Sub-category
                        errorCode.Success ? errorCode.Value : string.Empty,     // Error code
                        helpKeyword.Success ? helpKeyword.Value : string.Empty, // Help keyword
                        file.Success ? file.Value : string.Empty,               // File
                        lineInt,                                                  // Line number
                        columnInt,                                                // Column number
                        endLineInt,                                               // End line number
                        endColumnInt,                                             // End column number
                        message.Success ? message.Value : string.Empty);        // Message
                }

                match = match.NextMatch();
            }

            return result;
        }

        /// <summary>
        /// Collects a line of output.
        /// </summary>
        /// <param name="text">The text line</param>
        private void CollectOutputLine(string text)
        {
            text = string.IsNullOrEmpty(text) ? null : text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                this.CollectedOutput += Environment.NewLine + text;
                this.Log.LogMessage(text);
            }
        }
    }
}