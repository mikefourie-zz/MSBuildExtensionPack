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
    /// <para><i>Build</i> (<b>Required: </b> Projects <b>Optional: </b>VB6Path)</para>
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
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.1.0/html/c68d1d6c-b0bc-c944-e1a2-1ad4f0c28d3c.htm")]
    public class VB6 : BaseTask
    {
        private const string BuildTaskAction = "Build";      
        private string visualBasicPath;

        [DropdownValue(BuildTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the VB6Path. Default is C:\Program Files\Microsoft Visual Studio\VB98\VB6.exe
        /// </summary>
        [TaskAction(BuildTaskAction, false)]
        public string VB6Path { get; set; }

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
                if (File.Exists(@"C:\Program Files\Microsoft Visual Studio\VB98\VB6.exe"))
                {
                    this.visualBasicPath = @"C:\Program Files\Microsoft Visual Studio\VB98\VB6.exe";
                }
                else if (File.Exists(@"C:\Program Files (x86)\Microsoft Visual Studio\VB98\VB6.exe"))
                {
                    this.visualBasicPath = @"C:\Program Files (x86)\Microsoft Visual Studio\VB98\VB6.exe";
                }
                else
                {
                    Log.LogError("VB6.exe was not found in the default location. Use VB6Path to specify it.");
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
                this.BuildProject(project);
            }

            this.LogTaskMessage("BuildVB6 Task Execution Completed [" + DateTime.Now.ToString("HH:MM:ss", CultureInfo.CurrentCulture) + "]");
            return;
        }

        private void BuildProject(ITaskItem project)
        {
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = this.visualBasicPath;
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
                    return;
                }
            }
        }
    }
}
