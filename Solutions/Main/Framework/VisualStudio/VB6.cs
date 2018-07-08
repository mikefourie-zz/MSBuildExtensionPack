//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="VB6.cs">(c) 2017 Mike Fourie and Contributors (https://github.com/mikefourie/MSBuildExtensionPack) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.VisualStudio
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Build.Framework;
    using MSBuild.ExtensionPack.VisualStudio.Extended;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Build</i> (<b>Required: </b> Projects <b>Optional: </b>VB6Path, StopOnError)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// <para/>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///   </PropertyGroup>
    ///   <Import Project="$(TPath)"/>
    ///   <ItemGroup>
    ///     <ProjectsToBuild Include="C:\MyVB6Project.vbp">
    ///       <OutDir>c:\output</OutDir>
    ///       <!-- Note the special use of ChgPropVBP metadata to change project properties at Build Time -->
    ///       <ChgPropVBP>RevisionVer=4;CompatibleMode="0"</ChgPropVBP>
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
    public class VB6 : BaseTask
    {
        private const char Separator = ';';

        /// <summary>
        /// Sets the VB6Path. Default is [Program Files]\Microsoft Visual Studio\VB98\VB6.exe
        /// </summary>
        public string VB6Path { get; set; }

        /// <summary>
        /// Set to true to stop processing when a project in the Projects collection fails to compile. Default is false.
        /// </summary>
        public bool StopOnError { get; set; }

        /// <summary>
        /// Only build if any referenced source file is newer then the build output
        /// </summary>
        public bool IfModificationExists { get; set; }

        /// <summary>
        /// Sets the projects. Use an 'OutDir' metadata item to specify the output directory. The OutDir will be created if it does not exist.
        /// </summary>
        [Required]
        public ITaskItem[] Projects { get; set; }

        /// <summary>
        /// Defines conditional compilation constants. Format is const=value{[:constN=valueN]}
        /// </summary>
        public string ConditionalCompilationConstants { get; set; }

        /// <summary>
        /// Make command line parameters
        /// </summary>
        public string MakeCommandLine { get; set; }

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
                    this.Log.LogError("Failed to read a value from the ProgramFiles Environment Variable");
                    return;
                }

                if (File.Exists(programFilePath + @"\Microsoft Visual Studio\VB98\VB6.exe"))
                {
                    this.VB6Path = programFilePath + @"\Microsoft Visual Studio\VB98\VB6.exe";
                }
                else
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "VB6.exe was not found in the default location. Use VB6Path to specify it. Searched at: {0}", programFilePath + @"\Microsoft Visual Studio\VB98\VB6.exe"));
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
                this.Log.LogError("The collection passed to Projects is empty");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Building Projects Collection: {0} projects", this.Projects.Length));
            if (this.Projects.Any(project => !this.BuildProject(project) && this.StopOnError))
            {
                this.LogTaskMessage("BuildVB6 Task Execution Failed [" + DateTime.Now.ToString("HH:MM:ss", CultureInfo.CurrentCulture) + "] Stopped by StopOnError set on true");
                return;
            }

            this.LogTaskMessage("BuildVB6 Task Execution Completed [" + DateTime.Now.ToString("HH:MM:ss", CultureInfo.CurrentCulture) + "]");
        }

        private bool BuildProject(ITaskItem project)
        {
            using (Process proc = new Process())
            {
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

                FileInfo artifactFileInfo = null;
                if (this.IfModificationExists)
                {
                    this.LogTaskMessage("START - Checking for modified files");
                    bool doBuild = false;
                    VBPProject projectVBP = new VBPProject(project.ItemSpec);
                    if (projectVBP.Load())
                    {
                        FileInfo projectFileInfo = new FileInfo(projectVBP.ProjectFile);
                        artifactFileInfo = projectVBP.ArtifactFile;
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "artifactFile '{0}', LastWrite: {1}'", artifactFileInfo.FullName, artifactFileInfo.LastWriteTime));

                        if (projectFileInfo.LastWriteTime > artifactFileInfo.LastWriteTime)
                        {
                            this.LogTaskMessage(MessageImportance.High, $"File '{projectFileInfo.Name}' is newer then '{artifactFileInfo.Name}'");
                            doBuild = true;
                        }
                        else
                        {
                            foreach (var file in projectVBP.GetFiles())
                            {
                                this.LogTaskMessage($"File '{file.FullName}', LastWrite: {file.LastWriteTime}'");

                                if (file.LastWriteTime > artifactFileInfo.LastWriteTime)
                                {
                                    this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.CurrentCulture, "File '{0}' is newer then '{1}'", file.Name, artifactFileInfo.Name));
                                    doBuild = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!doBuild)
                    {
                        this.LogTaskMessage(MessageImportance.High, "Build skipped, because no modifications exists.");
                        return true;
                    }

                    this.LogTaskMessage("END - Checking for modified files");
                }

                proc.StartInfo.FileName = this.VB6Path;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                var arguments = new StringBuilder(string.Format(CultureInfo.InvariantCulture, @"/MAKE ""{0}"" /OUT ""{0}.log""", project.ItemSpec));
                if (!string.IsNullOrEmpty(project.GetMetadata("OutDir")))
                {
                    if (!Directory.Exists(project.GetMetadata("OutDir")))
                    {
                        Directory.CreateDirectory(project.GetMetadata("OutDir"));
                    }

                    arguments.AppendFormat(CultureInfo.InvariantCulture, @" /OUTDIR ""{0}""", project.GetMetadata("OutDir"));
                }

                if (!string.IsNullOrEmpty(this.ConditionalCompilationConstants))
                {
                    arguments.AppendFormat(CultureInfo.InvariantCulture, @" /D {0}", this.ConditionalCompilationConstants.Replace(" ", string.Empty));
                }

                if (!string.IsNullOrEmpty(this.MakeCommandLine))
                {
                    arguments.AppendFormat(CultureInfo.InvariantCulture, @" /C {0}", this.MakeCommandLine);
                }

                proc.StartInfo.Arguments = arguments.ToString();

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
                    this.Log.LogError(errorStream);
                }

                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    this.Log.LogError("Non-zero exit code from VB6.exe: " + proc.ExitCode);
                    try
                    {
                        using (FileStream myStreamFile = new FileStream(project.ItemSpec + ".log", FileMode.Open))
                        {
                            StreamReader myStream = new System.IO.StreamReader(myStreamFile);
                            string myBuffer = myStream.ReadToEnd();
                            this.Log.LogError(myBuffer);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Unable to open log file: '{0}'. Exception: {1}", project.ItemSpec + ".log", ex.Message));
                    }

                    return false;
                }

                if (artifactFileInfo != null)
                {
                    var myNow = DateTime.Now;
                    artifactFileInfo.LastWriteTime = myNow;
                    artifactFileInfo.LastAccessTime = myNow;
                }

                return true;
            }
        }
    }
}