//-----------------------------------------------------------------------
// <copyright file="RoboCopy.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// This task wraps RoboCopy
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
    ///         <MSBuild.ExtensionPack.FileSystem.RoboCopy Source="C:\b" Destination="C:\bbzz" Files="*.*" Options="/MIR"/>
    ///         <MSBuild.ExtensionPack.FileSystem.RoboCopy Source="C:\a" Destination="C:\abzz" Files="*.txt" Options="/e"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.2.0/html/220731f6-6b59-0cde-26ee-d47680f51c10.htm")]
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
        /// Type 'robocopy.exe /?' at the command prompt for all available options
        /// </summary>
        public string Options { get; set; }

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
                case 4:
                    Log.LogMessage("Return Code 4. Some Mismatched files or directories were detected. Examine the output log. Housekeeping is probably necessary.");
                    retVal = 0;
                    break;
                case 8:
                    Log.LogError("Return Code 8. Some files or directories could not be copied (copy errors occurred and the retry limit was exceeded). Check these errors further.");
                    break;
                case 16:
                    Log.LogError("Return Code 16. Serious error. RoboCopy did not copy any files. This is either a usage error or an error due to insufficient access privileges on the source or destination directories.");
                    break;
                default:
                    break;
            }

            return retVal;
        }
    }
}