//-----------------------------------------------------------------------
// <copyright file="RoboCopy.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task wraps RoboCopy. Successful non-zero exit codes from Robocopy are set to zero to not break MSBuild. Use the ReturnCode property to access the exit code from Robocopy
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <MSBuild.ExtensionPack.FileSystem.RoboCopy Source="C:\b" Destination="C:\bbzz" Files="*.*" Options="/MIR">
    ///             <Output TaskParameter="ExitCode" PropertyName="Exit" />
    ///             <Output TaskParameter="ReturnCode" PropertyName="Return" />
    ///         </MSBuild.ExtensionPack.FileSystem.RoboCopy>
    ///         <Message Text="ExitCode = $(Exit)"/>
    ///         <Message Text="ReturnCode = $(Return)"/>
    ///         <MSBuild.ExtensionPack.FileSystem.RoboCopy Source="C:\a" Destination="C:\abzz" Files="*.txt" Options="/e">
    ///             <Output TaskParameter="ExitCode" PropertyName="Exit" />
    ///             <Output TaskParameter="ReturnCode" PropertyName="Return" />
    ///         </MSBuild.ExtensionPack.FileSystem.RoboCopy>
    ///         <Message Text="ExitCode = $(Exit)"/>
    ///         <Message Text="ReturnCode = $(Return)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class RoboCopy : ToolTask
    {
        /// <summary>
        /// Source Directory (drive:\path or \\server\share\path).
        /// </summary>
        [Required]
        public ITaskItem Source { get; set; }

        /// <summary>
        /// Destination Dir  (drive:\path or \\server\share\path).
        /// </summary>
        [Required]
        public ITaskItem Destination { get; set; }

        /// <summary>
        /// File(s) to copy  (names/wildcards: default is "*.*").
        /// </summary>
        [Required]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// Gets the Return Code from RoboCopy
        /// </summary>
        [Output]
        public int ReturnCode { get; set; }

        /// <summary>
        /// Type 'robocopy.exe /?' at the command prompt for all available options
        /// </summary>
        public string Options { get; set; }

        /// <summary>
        /// Set to true to log output to the console. Default is false
        /// </summary>
        public bool LogToConsole { get; set; }

        protected override string ToolName
        {
            get { return "RoboCopy.exe"; }
        }

        protected override string GenerateFullPathToTool()
        {
            return string.IsNullOrEmpty(this.ToolPath) ? this.ToolName : Path.Combine(this.ToolPath, this.ToolName);
        }

        protected override string GenerateCommandLineCommands()
        {
            CommandLineBuilder builder = new CommandLineBuilder();
            builder.AppendFileNameIfNotNull(this.Source);
            builder.AppendFileNameIfNotNull(this.Destination);
            builder.AppendFileNamesIfNotNull(this.Files, " ");
            if (!string.IsNullOrEmpty(this.Options))
            {
                builder.AppendSwitch(this.Options);
            }

            return builder.ToString();
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            Log.LogMessage("Running " + pathToTool + " " + commandLineCommands);
            int retVal = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
            this.ReturnCode = retVal;
            switch (retVal)
            {
                case 0:
                    Log.LogMessage("Return Code 0. No errors occurred, and no copying was done. The source and destination directory trees are completely synchronized.");
                    break;
                case 1:
                    Log.LogMessage("Return Code 1. One or more files were copied successfully (that is, new files have arrived).");
                    retVal = 0;
                    break;
                case 2:
                    Log.LogMessage("Return Code 2. Some Extra files or directories were detected. Examine the output log. Some housekeeping may be needed.");
                    retVal = 0;
                    break;
                case 3:
                    Log.LogMessage("Return Code 3. One or more files were copied successfully (that is, new files have arrived). Some Extra files or directories were detected. Examine the output log. Some housekeeping may be needed.");
                    retVal = 0;
                    break;
                case 4:
                    Log.LogMessage("Return Code 4. Some Mismatched files or directories were detected. Examine the output log. Housekeeping is probably necessary.");
                    retVal = 0;
                    break;
                case 5:
                    Log.LogMessage("Return Code 5. One or more files were copied successfully (that is, new files have arrived). Some Mismatched files or directories were detected. Examine the output log. Housekeeping is probably necessary.");
                    retVal = 0;
                    break;
                case 6:
                    Log.LogMessage("Return Code 6. Some Extra files or directories were detected. Some Mismatched files or directories were detected. Examine the output log. Housekeeping is probably necessary.");
                    retVal = 0;
                    break;
                case 7:
                    Log.LogMessage("Return Code 7. One or more files were copied successfully (that is, new files have arrived). Some Extra files or directories were detected. Some Mismatched files or directories were detected. Examine the output log. Housekeeping is probably necessary.");
                    retVal = 0;
                    break;
                case 8:
                    Log.LogError("Return Code 8. Some files or directories could not be copied (copy errors occurred and the retry limit was exceeded). Check these errors further.");
                    break;
                case 9:
                    Log.LogError("Return Code 9. One or more files were copied successfully (that is, new files have arrived). Some files or directories could not be copied (copy errors occurred and the retry limit was exceeded). Check these errors further.");
                    break;
                case 10:
                    Log.LogError("Return Code 10. Some Extra files or directories were detected. Examine the output log. Some housekeeping may be needed. Some files or directories could not be copied (copy errors occurred and the retry limit was exceeded). Check these errors further.");
                    break;
                case 11:
                    Log.LogError("Return Code 11. One or more files were copied successfully (that is, new files have arrived). Some Extra files or directories were detected. Examine the output log. Some housekeeping may be needed. Some files or directories could not be copied (copy errors occurred and the retry limit was exceeded). Check these errors further.");
                    break;
                case 12:
                    Log.LogError("Return Code 12. Some Mismatched files or directories were detected. Examine the output log. Housekeeping is probably necessary. Some files or directories could not be copied (copy errors occurred and the retry limit was exceeded). Check these errors further.");
                    break;
                case 13:
                    Log.LogError("Return Code 13. One or more files were copied successfully (that is, new files have arrived). Some Mismatched files or directories were detected. Examine the output log. Housekeeping is probably necessary. Some files or directories could not be copied (copy errors occurred and the retry limit was exceeded). Check these errors further.");
                    break;
                case 14:
                    Log.LogError("Return Code 14. Some Extra files or directories were detected. Some Mismatched files or directories were detected. Examine the output log. Housekeeping is probably necessary. Some files or directories could not be copied (copy errors occurred and the retry limit was exceeded). Check these errors further.");
                    break;
                case 15:
                    Log.LogError("Return Code 15. One or more files were copied successfully (that is, new files have arrived). Some Extra files or directories were detected. Some Mismatched files or directories were detected. Examine the output log. Housekeeping is probably necessary. Some files or directories could not be copied (copy errors occurred and the retry limit was exceeded). Check these errors further.");
                    break;
                case 16:
                    Log.LogError("Return Code 16. Serious error. RoboCopy did not copy any files. This is either a usage error or an error due to insufficient access privileges on the source or destination directories.");
                    break;
            }

            return retVal;
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            if (this.LogToConsole)
            {
                this.Log.LogMessage(MessageImportance.Normal, singleLine);
            }
        }
    }
}