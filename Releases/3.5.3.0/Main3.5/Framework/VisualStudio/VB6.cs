//-----------------------------------------------------------------------
// <copyright file="VB6.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.VisualStudio
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Build</i> (<b>Required: </b> Projects <b>Optional: </b>VB6Path, StopOnError)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///   </PropertyGroup>
    ///   <Import Project="$(TPath)"/>
    ///   <ItemGroup>
    ///     <ProjectsToBuild Include="C:\MyVB6Project.vbp">
    ///       <OutDir>c:\output</OutDir>
    ///     </ProjectsToBuild>
    ///     <ProjectsToBuild Include="C:\MyVB6Project2.vbp"/>
    ///   </ItemGroup>
    ///   <Target Name="Default">
    ///       <!-- Build a collection of VB6 projects -->
    ///     <MSBuild.ExtensionPack.VisualStudio.VB6 TaskAction="Build" Projects="@(ProjectsToBuild)"/>
    ///   </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.2.0/html/c68d1d6c-b0bc-c944-e1a2-1ad4f0c28d3c.htm")]
    public class VB6 : BaseTask
    {
        private const string BuildTaskAction = "Build";
        private const char Separator = ';';

        [DropdownValue(BuildTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the VB6Path. Default is [Program Files]\Microsoft Visual Studio\VB98\VB6.exe
        /// </summary>
        [TaskAction(BuildTaskAction, false)]
        public string VB6Path { get; set; }

        /// <summary>
        /// Set to true to stop processing when a project in the Projects collection fails to compile. Default is false.
        /// </summary>
        [TaskAction(BuildTaskAction, false)]
        public bool StopOnError { get; set; }

        /// <summary>
        /// Sets the projects. Use an 'OutDir' metadata item to specify the output directory. The OutDir will be created if it does not exist.
        /// </summary>
        [Required]
        [TaskAction(BuildTaskAction, true)]
        public ITaskItem[] Projects { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            if (string.IsNullOrEmpty(this.VB6Path))
            {
                string programFilePath = Environment.GetEnvironmentVariable("ProgramFiles");
                if (string.IsNullOrEmpty(programFilePath))
                {
                    Log.LogError("Failed to read a value from the ProgramFiles Environment Variable");
                    return;
                }

                if (File.Exists(programFilePath + @"\Microsoft Visual Studio\VB98\VB6.exe"))
                {
                    this.VB6Path = programFilePath + @"\Microsoft Visual Studio\VB98\VB6.exe";
                }
                else
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "VB6.exe was not found in the default location. Use VB6Path to specify it. Searched at: {0}", programFilePath + @"\Microsoft Visual Studio\VB98\VB6.exe"));
                    return;
                }
            }

            switch (this.TaskAction)
            {
                case "Build":
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
            foreach (ITaskItem project in this.Projects)
            {
                if (!this.BuildProject(project) && this.StopOnError)
                {
                    this.LogTaskMessage("BuildVB6 Task Execution Failed [" + DateTime.Now.ToString("HH:MM:ss", CultureInfo.CurrentCulture) + "] Stopped by StopOnError set on true");
                    return;
                }
            }

            this.LogTaskMessage("BuildVB6 Task Execution Completed [" + DateTime.Now.ToString("HH:MM:ss", CultureInfo.CurrentCulture) + "]");
            return;
        }

        private bool BuildProject(ITaskItem project)
        {
            using (Process proc = new Process())
            {
                // start changing properties
                if (!string.IsNullOrEmpty(project.GetMetadata("ChgPropVBP")))
                {
                    this.LogTaskMessage("START - Changing Properties VBP");

                    VBPProject projectVBP = new VBPProject(project.ItemSpec);
                    if (projectVBP.Load())
                    {
                        string[] linesProperty = project.GetMetadata("ChgPropVBP").Split(Separator);
                        string[] keyProperty = new string[linesProperty.Length];
                        string[] valueProperty = new string[linesProperty.Length];
                        int index;

                        for (index = 0; index <= linesProperty.Length - 1; index++)
                        {
                            if (linesProperty[index].IndexOf("=", StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                keyProperty[index] = linesProperty[index].Substring(0, linesProperty[index].IndexOf("=", StringComparison.OrdinalIgnoreCase));
                                valueProperty[index] = linesProperty[index].Substring(linesProperty[index].IndexOf("=", StringComparison.OrdinalIgnoreCase) + 1);
                            }

                            if (!string.IsNullOrEmpty(keyProperty[index]) && !string.IsNullOrEmpty(valueProperty[index]))
                            {
                                this.LogTaskMessage(keyProperty[index] + " -> New value: " + valueProperty[index]);
                                projectVBP.SetProjectProperty(keyProperty[index], valueProperty[index], false);
                            }
                        }

                        projectVBP.Save();
                    }

                    this.LogTaskMessage("END - Changing Properties VBP");
                }

                // end changing properties
                proc.StartInfo.FileName = this.VB6Path;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                if (string.IsNullOrEmpty(project.GetMetadata("OutDir")))
                {
                    proc.StartInfo.Arguments = @"/MAKE /OUT " + @"""" + project.ItemSpec + ".log" + @""" " + @"""" + project.ItemSpec + @"""";
                }
                else
                {
                    if (!Directory.Exists(project.GetMetadata("OutDir")))
                    {
                        Directory.CreateDirectory(project.GetMetadata("OutDir"));
                    }

                    proc.StartInfo.Arguments = @"/MAKE /OUT " + @"""" + project.ItemSpec + ".log" + @""" " + @"""" + project.ItemSpec + @"""" + " /outdir " + @"""" + project.GetMetadata("OutDir") + @"""";
                }

                // start the process
                this.LogTaskMessage("Running " + proc.StartInfo.FileName + " " + proc.StartInfo.Arguments);

                proc.Start();

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
                if (proc.ExitCode != 0)
                {
                    Log.LogError("Non-zero exit code from VB6.exe: " + proc.ExitCode);
                    try
                    {
                        using (FileStream myStreamFile = new FileStream(project.ItemSpec + ".log", FileMode.Open))
                        using (System.IO.StreamReader myStream = new System.IO.StreamReader(myStreamFile))
                        {
                            string myBuffer = myStream.ReadToEnd();
                            Log.LogError(myBuffer);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogError(string.Format(CultureInfo.CurrentUICulture, "Unable to open log file: '{0}'. Exception: {1}", project.ItemSpec + ".log", ex.Message));
                    }

                    return false;
                }

                return true;
            }
        }
    }
}
