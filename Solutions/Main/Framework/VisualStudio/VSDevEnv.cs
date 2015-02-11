//-----------------------------------------------------------------------
// <copyright file="VSDevEnv.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.VisualStudio
{
    using System;
    using System.Globalization;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Win32;

    /// <summary>
    /// This task provides a lightweight wrapper over Devenv.exe
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
    ///       <MSBuild.ExtensionPack.VisualStudio.VSDevEnv FilePath="C:\a New Folder\WindowsFormsApplication1.sln" Configuration="Debug|Any CPU" Rebuild="true">
    ///         <Output TaskParameter="ExitCode" PropertyName="Exit" />
    ///       </MSBuild.ExtensionPack.VisualStudio.VSDevEnv>
    ///       <Message Text="ExitCode: $(Exit)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class VSDevEnv : ToolTask
    {
        private string version = "9.0";

        /// <summary>
        /// The Path to the solution or Project to build
        /// </summary>
        [Required]
        public ITaskItem FilePath { get; set; }

        /// <summary>
        /// The Configuration to Build.
        /// </summary>
        [Required]
        public string Configuration { get; set; }

        /// <summary>
        /// The version of Visual Studio to run, e.g. 8.0, 9.0, 10.0. Default is 9.0
        /// </summary>
        public string Version
        {
            get { return this.version; }
            set { this.version = value; }
        }

        /// <summary>
        /// Specifies whether Clean and then build the solution or project with the specified configuration. Default is false
        /// </summary>
        public bool Rebuild { get; set; }

        /// <summary>
        /// Specifies the File to log all output to. Defaults to the [Path.Dir]\Output\[Path.FileName].[Configuration].txt
        /// </summary>
        public ITaskItem OutputFile { get; set; }

        /// <summary>
        /// Specifies the output folder to log to. Default is [Path.Dir]\Output\
        /// </summary>
        public ITaskItem OutputFolder { get; set; }

        protected override string ToolName
        {
            get { return "devenv.exe"; }
        }

        protected override string GenerateFullPathToTool()
        {
            using (RegistryKey sw = Utilities.SoftwareRegistry32Bit)
            {
                if (sw != null)
                {
                    RegistryKey key = sw.OpenSubKey(@"Microsoft\VisualStudio\" + this.Version);
                    if (key != null)
                    {
                        string path = Convert.ToString(key.GetValue("InstallDir"), CultureInfo.InvariantCulture);
                        key.Close();
                        return System.IO.Path.Combine(path, this.ToolName);
                    }
                }
            }

            throw new Exception(string.Format(CultureInfo.InvariantCulture, "Visual Studio Registry Key not found: {0}", @"SOFTWARE\Microsoft\VisualStudio\" + this.Version));
        }

        protected override string GenerateCommandLineCommands()
        {
            DirectoryInfo outputFolder;
            FileInfo outputfile;
            if (this.OutputFolder == null)
            {
                if (this.OutputFile == null)
                {
                    outputFolder = new DirectoryInfo(this.FilePath.GetMetadata("RootDir") + this.FilePath.GetMetadata("Directory") + @"\Output");
                    outputfile = new FileInfo(outputFolder.FullName + string.Format(CultureInfo.InvariantCulture, @"\{0}.{1}.txt", this.FilePath.GetMetadata("Filename"), this.Configuration.Replace("|", " ")));
                }
                else
                {
                    outputfile = new FileInfo(this.OutputFile.ItemSpec);
                }
            }
            else
            {
                outputFolder = new DirectoryInfo(this.OutputFolder.ItemSpec);
                outputfile = this.OutputFile == null ? new FileInfo(outputFolder.FullName + string.Format(CultureInfo.InvariantCulture, @"\{0}.{1}.txt", this.FilePath.GetMetadata("Filename"), this.Configuration.Replace("|", " "))) : new FileInfo(outputFolder.FullName + @"\" + this.OutputFile.GetMetadata("FileName") + this.OutputFile.GetMetadata("Extension"));
            }

            if (outputfile.Exists)
            {
                outputfile.Delete();
            }

            CommandLineBuilder builder = new CommandLineBuilder();
            builder.AppendSwitch(this.Rebuild ? "/Rebuild" : "/Build");
            builder.AppendSwitch("\"" + this.Configuration + "\"");
            builder.AppendSwitch("/out \"" + outputfile.FullName + "\"");
            builder.AppendSwitch("\"" + this.FilePath.GetMetadata("FullPath") + "\"");
            return builder.ToString();
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            Log.LogMessage("Running " + pathToTool + " " + commandLineCommands);
            return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            this.Log.LogMessage(MessageImportance.Normal, singleLine);
        }
    }
}
