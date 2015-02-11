//-----------------------------------------------------------------------
// <copyright file="VC6.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.VisualStudio
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Build</i> (<b>Required: </b> Projects <b>Optional: </b>MSDEVPath, StopOnError)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// <para/>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <ItemGroup>
    ///         <!-- This uses $(Platform) and $(Configuration) for all projects in the .dsp file -->
    ///         <ProjectsToBuild Include="C:\MyVC6Project.dsp"/>
    ///         <!-- Uses supplied platform and configuration for all projects in the .dsp file -->
    ///         <ProjectsToBuild Include="C:\MyVC6Project2.dsp">
    ///             <Platform>Win32</Platform>
    ///             <Configuration>Debug</Configuration>
    ///         </ProjectsToBuild>
    ///         <!-- Uses $(Platform) and $(Configuration) for just the specified projects in the .dsw file -->
    ///         <ProjectsToBuild Include="C:\MyVC6Project3.dsw">
    ///             <Projects>Project1;Project2</Projects>
    ///         </ProjectsToBuild>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Build a collection of VC6 projects -->
    ///         <MSBuild.ExtensionPack.VisualStudio.VC6 TaskAction="Build" Projects="@(ProjectsToBuild)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class VC6 : BaseTask
    {
        private const string DefaultMSDEVPath = @"\Microsoft Visual Studio\Common\MSDev98\Bin\MSDEV.EXE";
        private const string BuildTaskAction = "Build";
        private const string CleanTaskAction = "Clean";
        private const string RebuildTaskAction = "Rebuild";
        private const string ProjectsMetadataName = "Projects";
        private const string PlatformMetadataName = "Platform";
        private const string ConfigurationMetadataName = "Configuration";
        private const char Separator = ';';

        /// <summary>
        /// Sets the MSDEV path. Default is [Program Files]\Microsoft Visual Studio\Common\MSDev98\Bin\MSDEV.EXE
        /// </summary>
        public string MSDEVPath { get; set; }

        /// <summary>
        /// Set to true to stop processing when a project in the Projects collection fails to compile. Default is false.
        /// </summary>
        public bool StopOnError { get; set; }

        /// <summary>
        /// Sets the .dsp/.dsw projects to build.
        /// </summary>
        /// <remarks>
        /// An additional Projects metadata item may be specified for each project to indicate which workspace project(s)
        /// to build. If none is supplied, the special-case 'ALL' project name is used to inform MSDEV to build all 
        /// projects contained within the workspace/project.
        /// </remarks>
        [Required]
        public ITaskItem[] Projects { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            if (string.IsNullOrEmpty(this.MSDEVPath))
            {
                string programFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                if (string.IsNullOrEmpty(programFilePath))
                {
                    Log.LogError("Failed to find the special folder 'ProgramFiles'");
                    return;
                }

                if (File.Exists(programFilePath + VC6.DefaultMSDEVPath))
                {
                    this.MSDEVPath = programFilePath + VC6.DefaultMSDEVPath;
                }
                else
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "MSDEV.exe was not found in the default location. Use MSDEVPath to specify it. Searched at: {0}", programFilePath + VC6.DefaultMSDEVPath));
                    return;
                }
            }

            switch (this.TaskAction)
            {
                case BuildTaskAction:
                case CleanTaskAction:
                case RebuildTaskAction:
                    this.Build();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Build()
        {
            if (this.Projects == null)
            {
                Log.LogError("The collection passed to Projects is empty");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Building Projects Collection: {0} projects", this.Projects.Length));
            if (this.Projects.Any(project => !this.BuildProject(project) && this.StopOnError))
            {
                this.LogTaskMessage("VC6 Task Execution Failed [" + DateTime.Now.ToString("HH:MM:ss", CultureInfo.CurrentCulture) + "]. Stopped by StopOnError set on true");
                return;
            }

            this.LogTaskMessage("VC6 Task Execution Completed [" + DateTime.Now.ToString("HH:MM:ss", CultureInfo.CurrentCulture) + "]");
        }

        private bool BuildProject(ITaskItem project)
        {
            string projectNames = project.GetMetadata(ProjectsMetadataName);
            if (string.IsNullOrEmpty(projectNames))
            {
                Log.LogMessage(MessageImportance.Low, "No project names specified. Using 'ALL'.");
                projectNames = "ALL";
            }
            else
            {
                Log.LogMessage(MessageImportance.Low, "Project names '{0}'", projectNames);
            }

            string platformName = project.GetMetadata(PlatformMetadataName);
            if (string.IsNullOrEmpty(platformName))
            {
                Log.LogMessage(MessageImportance.Low, "No platform name specified. Using 'Win32'.");
                platformName = "Win32";
            }
            else
            {
                Log.LogMessage(MessageImportance.Low, "Platform name '{0}'", platformName);
            }

            string configurationName = project.GetMetadata(ConfigurationMetadataName);
            if (string.IsNullOrEmpty(configurationName))
            {
                Log.LogMessage(MessageImportance.Low, "No configuration name specified. Using 'Debug'.");
                configurationName = "Debug";
            }
            else
            {
                Log.LogMessage(MessageImportance.Low, "Configuration names '{0}'", configurationName);
            }

            bool allBuildsSucceeded = true;
            foreach (string projectName in projectNames.Split(Separator))
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = this.MSDEVPath;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;

                    System.Text.StringBuilder argumentsBuilder = new System.Text.StringBuilder();
                    argumentsBuilder.AppendFormat("\"{0}\" /OUT \"{0}.log\" /MAKE ", project.ItemSpec);
                    argumentsBuilder.AppendFormat("\"{0} - {1} {2}\"", projectName, platformName, configurationName);

                    if (this.TaskAction == CleanTaskAction)
                    {
                        argumentsBuilder.Append(" /CLEAN");
                    }
                    else if (this.TaskAction == RebuildTaskAction)
                    {
                        argumentsBuilder.Append(" /REBUILD");
                    }

                    proc.StartInfo.Arguments = argumentsBuilder.ToString();

                    // start the process
                    this.LogTaskMessage("Running " + proc.StartInfo.FileName + " " + proc.StartInfo.Arguments);

                    proc.Start();
                    proc.WaitForExit();

                    string outputStream = proc.StandardOutput.ReadToEnd();
                    if (outputStream.Length > 0)
                    {
                        this.LogTaskMessage(outputStream);
                    }

                    string errorStream = proc.StandardError.ReadToEnd();
                    if (errorStream.Length > 0)
                    {
                        Log.LogError(errorStream);
                    }

                    proc.WaitForExit();
                    if (proc.ExitCode == 0)
                    {
                        continue;
                    }

                    this.Log.LogError("Non-zero exit code from MSDEV.exe: " + proc.ExitCode);
                    try
                    {
                        using (FileStream myStreamFile = new FileStream(project.ItemSpec + ".log", FileMode.Open))
                        {
                            System.IO.StreamReader myStream = new System.IO.StreamReader(myStreamFile);
                            string myBuffer = myStream.ReadToEnd();
                            this.Log.LogError(myBuffer);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log.LogError(string.Format(CultureInfo.CurrentUICulture, "Unable to open log file: '{0}'. Exception: {1}", project.ItemSpec + ".log", ex.Message));
                    }

                    allBuildsSucceeded = false;
                }
            }

            return allBuildsSucceeded;
        }
    }
}
