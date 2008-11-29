//-----------------------------------------------------------------------
// <copyright file="DevEnv.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// This task is based on the DevEnv task written by Aaron Hallberg (http://blogs.msdn.com/aaronhallberg/archive/2007/07/12/team-build-devenv-task.aspx). It is used here with permission.
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Tfs
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.TeamFoundation.Build.Client;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.Win32;

    /// <summary>
    /// Build non-MSBuild projects in Team Build.<para/>
    /// This task is based on the DevEnv task written by Aaron Hallberg (http://blogs.msdn.com/aaronhallberg/archive/2007/07/12/team-build-devenv-task.aspx). It is used here with permission.<para/>
    /// <para><b>Required: </b>TeamFoundationServerUrl, BuildUri, Solution or Project, SolutionPlatform, SolutionConfiguration, Target <b>Optional: </b>AdditionalCommandLineSwitches, ProjectConfiguration, OutputFile, Version</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <?xml version="1.0" encoding="utf-8"?>
    /// <Project DefaultTargets="DeployFiles" ToolsVersion="3.5" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <!-- Please be aware that the rest of the build file is ommited for brevity -->
    ///     <PropertyGroup>
    ///         <!-- Tell Team Build not to override $(OutDir), so that we can build once from MSBuild and not rebuild when DevEnv.com is executed. -->
    ///         <CustomizableOutDir>true</CustomizableOutDir>
    ///     </PropertyGroup>
    ///     <Target Name="AfterCompileSolution">
    ///         <!-- Use the DevEnv task to build our setup project. -->
    ///         <DevEnv TeamFoundationServerUrl="$(TeamFoundationServerUrl)" BuildUri="$(BuildUri)" Solution="$(Solution)" SolutionConfiguration="$(Configuration)" SolutionPlatform="$(Platform)" Target="Build" Version="9" />
    ///         <!-- Copy all compilation outputs for the solution AND the setup project to the Team Build out dir so that they are copied to the drop location, can be found by unit tests, etc. -->
    ///         <ItemGroup>
    ///             <SolutionOutputs Condition=" '%(CompilationOutputs.Solution)' == '$(Solution)' " Include="%(RootDir)%(Directory)**\*.*" />
    ///             <SolutionOutputs Include="$(SolutionRoot)\Setup1\$(Configuration)\**\*.*" />
    ///         </ItemGroup>
    ///         <Copy SourceFiles="@(SolutionOutputs)" DestinationFolder="$(TeamBuildOutDir)" />
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
	[HelpUrl("http://www.msbuildextensionpack.com/help/3.5.1.0/html/4e3ff893-f5d5-0182-7f2f-f760868aea61.htm")]
    public class DevEnv : ToolTask
    {
        private IBuildDetail build;
        private IBuildStep buildStep;
        private Regex codeAnalysisErrorRegex;
        private Regex codeAnalysisWarningRegex;
        private IConfigurationSummary configurationSummary;
        private bool errorEncountered;
        private Regex errorRegex;
        private Regex projectCompilationRegex;
        private string target = "Build";
        private int version = 9;
        private Regex warningRegex;

        /// <summary>
        /// The Url of the Team Foundation Server.
        /// </summary>
        [Required]
        public Uri TeamFoundationServerUrl { get; set; }

        /// <summary>
        /// The Uri of the Build for which this task is executing.
        /// </summary>
        [Required]
        public Uri BuildUri { get; set; }
        
        /// <summary>
        /// The solution to be built using DevEnv. Either Solution or Project (or both) must be specified.
        /// </summary>
        public string Solution { get; set; }

        /// <summary>
        /// The project to be built using DevEnv. Either Solution *or* Project (or both) must be specified.
        /// </summary>
        public string Project { get; set; }

        /// <summary>
        /// The solution platform to be Built, Rebuilt, Cleaned, or Deployed. For example "Any CPU".
        /// </summary>
        [Required]
        public string SolutionPlatform { get; set; }

        /// <summary>
        /// The solution configuration to be Built, Rebuilt, Cleaned, or Deployed. For example "Debug".
        /// </summary>
        [Required]
        public string SolutionConfiguration { get; set; }

        /// <summary>
        /// The project configuration to be Built, Rebuilt, Cleaned, or Deployed. For example, "Debug" or "Debug|AnyCPU".
        /// </summary>
        public string ProjectConfiguration { get; set; }

        /// <summary>
        /// Sets the output file to append the build log to.
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// A catch-all property for specifying additional command-line switches (e.g. /useenv) as necessary.
        /// </summary>
        public string AdditionalCommandLineSwitches { get; set; }

        /// <summary>
        /// The target to be executed - either Build, Rebuild, Clean, or Deploy. Default is Build.
        /// </summary>
        [Required]
        public string Target
        {
            get { return this.target; }
            set { this.target = value; }
        }

        /// <summary>
        /// The (major) version of DevEnv.com that should be invoked. Default is 9 (2008).
        /// </summary>
        public int Version
        {
            get { return this.version; }
            set { this.version = value; }
        }

        /// <summary>
        /// The name of the tool to execute.
        /// </summary>
        protected override string ToolName
        {
            get { return "DevEnv.com"; }
        }

        /// <summary>
        /// Lazy init property that gives access to the Build specified by BuildUri.
        /// </summary>
        protected IBuildDetail Build
        {
            get
            {
                if (this.build == null)
                {
                    TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(this.TeamFoundationServerUrl.ToString());
                    IBuildServer buildServer = (IBuildServer)tfs.GetService(typeof(IBuildServer));
                    this.build = buildServer.GetAllBuildDetails(new Uri(this.BuildUri.ToString()));
                }

                return this.build;
            }
        }

        /// <summary>
        /// Retrieves the ConfigurationSummary for SolutionPlatform and SolutionConfiguration, or creates a new one.
        /// </summary>
        protected IConfigurationSummary ConfigurationSummary
        {
            get
            {
                if (this.configurationSummary == null)
                {
                    // Try to get the existing configuration summary, if null, create a new one.
                    this.configurationSummary = InformationNodeConverters.GetConfigurationSummary(this.Build, this.SolutionConfiguration, this.SolutionPlatform) ?? InformationNodeConverters.AddConfigurationSummary(this.Build, this.SolutionConfiguration, this.SolutionPlatform, null);
                }

                return this.configurationSummary;
            }
        }

        /// <summary>
        /// The CompilationSummary for the currently compiling project.
        /// </summary>
        protected ICompilationSummary CompilationSummary { get; set; }

        /// <summary>
        /// The main build step for this task.
        /// </summary>
        protected IBuildStep BuildStep
        {
            get { return this.buildStep; }
        }

        /// <summary>
        /// The build step for the currently compiling project.
        /// </summary>
        protected IBuildStep ProjectBuildStep { get; set; }

        /// <summary>
        /// The regular expression that matches project compilation messages.
        /// </summary>
        protected Regex ProjectCompilationRegex
        {
            get
            {
                if (this.projectCompilationRegex == null)
                {
                    this.projectCompilationRegex = new Regex(@"Build started: Project: (?<Project>[^,]+), Configuration:");
                }

                return this.projectCompilationRegex;
            }
        }

        /// <summary>
        /// The regular expression that matches compilation errors.
        /// </summary>
        protected Regex ErrorRegex
        {
            get
            {
                if (this.errorRegex == null)
                {
                    this.errorRegex = new Regex(@"error\s*:?\s*(?<Code>[^\s:]+)\s*:\s*(?<Text>.*)$");
                }

                return this.errorRegex;
            }
        }

        /// <summary>
        /// The regular expression that matches compilation warnings.
        /// </summary>
        protected Regex WarningRegex
        {
            get
            {
                if (this.warningRegex == null)
                {
                    this.warningRegex = new Regex(@"warning\s*:?\s*(?<Code>[^\s:]+)\s*:\s*(?<Text>.*)$");
                }

                return this.warningRegex;
            }
        }

        /// <summary>
        /// The regular expression that matches static analysis errors.
        /// </summary>
        protected Regex StaticAnalysisErrorRegex
        {
            get
            {
                if (this.codeAnalysisErrorRegex == null)
                {
                    this.codeAnalysisErrorRegex = new Regex(@"error\s*:?\s*(?<Code>CA[^\s:]+)\s*:\s*(?<Text>.*)$");
                }

                return this.codeAnalysisErrorRegex;
            }
        }

        /// <summary>
        /// The regular expression that matches static analysis warnings.
        /// </summary>
        protected Regex StaticAnalysisWarningRegex
        {
            get
            {
                if (this.codeAnalysisWarningRegex == null)
                {
                    this.codeAnalysisWarningRegex = new Regex(@"warning\s*:?\s*(?<Code>CA[^\s:]+)\s*:\s*(?<Text>.*)$");
                }

                return this.codeAnalysisWarningRegex;
            }
        }

        /// <summary>
        /// Executes the DevEnv task logic.
        /// </summary>
        /// <returns>True if the task succeeds, false otherwise.</returns>
        public override bool Execute()
        {
            bool returnValue = false;

            try
            {
                this.buildStep = InformationNodeConverters.AddBuildStep(this.Build, "DevEnv Task BuildStep", "Visual Studio is building " + (string.IsNullOrEmpty(this.Solution) ? this.Project : this.Solution));

                // Execute DevEnv.
                returnValue = base.Execute();

                // Update the final project build step, if we have one.
                this.UpdateProjectBuildStep();

                // Save the configuration summary (errors and warnings, etc.)
                this.ConfigurationSummary.Save();

                // Update compilation status if any errors were encountered.
                if (this.errorEncountered)
                {
                    this.Build.CompilationStatus = BuildPhaseStatus.Failed;
                    this.Build.Save();
                }
            }
            catch (Exception e)
            {
                InformationNodeConverters.AddBuildStep(this.Build, "Exception", e.Message, DateTime.Now, BuildStepStatus.Failed);
                throw;
            }
            finally
            {
                // Update our build step.
                this.BuildStep.Status = returnValue ? BuildStepStatus.Succeeded : BuildStepStatus.Failed;
                this.BuildStep.FinishTime = DateTime.Now;
                this.BuildStep.Save();
            }

            return returnValue;
        }

        /// <summary>
        /// Generates the command-line arguments to DevEnv.com. Example: 
        /// "MyProject.sln /Build 'Debug|Any CPU' /Project MyProject.csproj /ProjectConfig 'Release'"
        /// </summary>
        /// <returns>The command-line arguments to DevEnv.com.</returns>
        protected override string GenerateCommandLineCommands()
        {
            StringBuilder commands = new StringBuilder();

            if (!string.IsNullOrEmpty(this.Solution))
            {
                commands.AppendFormat(" \"{0}\"", this.Solution);
            }
            else if (!string.IsNullOrEmpty(this.Project))
            {
                this.Log.LogWarning("No solution was specified. DevEnv.com will look for a .sln file with the same base name as the project file in the parent directory for the project file. If no such .sln file exists, then DevEnv.com will look for a single .sln file that references the project. If no such single .sln file exists, DevEnv.com will create an unsaved solution with a default .sln file name that has the same base name as the project file.");
                commands.AppendFormat(" \"{0}\"", this.Project);
            }
            else
            {
                throw new ArgumentException("Either Solution or Project (or both) must be specified.");
            }

            commands.AppendFormat(" /{0}", this.Target);

            commands.AppendFormat(" \"{0}|{1}\"", this.SolutionConfiguration, this.SolutionPlatform);

            if (!string.IsNullOrEmpty(this.Project))
            {
                commands.AppendFormat(" /Project \"{0}\"", this.Project);

                if (!string.IsNullOrEmpty(this.ProjectConfiguration))
                {
                    commands.AppendFormat(" /ProjectConfig \"{0}\"", this.ProjectConfiguration);
                }
            }

            if (!string.IsNullOrEmpty(this.OutputFile))
            {
                commands.AppendFormat(" /Out \"{0}\"", this.OutputFile);
            }

            commands.AppendFormat(" {0}", this.AdditionalCommandLineSwitches);

            return commands.ToString();
        }

        /// <summary>
        /// Determines the full path to DevEnv.com, if this path has not been explicitly specified by the user.
        /// </summary>
        /// <returns>The full path to DevEnv.com, or just "DevEnv.com" if it's not found.</returns>
        protected override string GenerateFullPathToTool()
        {
            string regKey = string.Format(CultureInfo.InvariantCulture, @"SOFTWARE\Microsoft\VisualStudio\{0}.0", this.version);
            string path = string.Empty;

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regKey))
            {
                if (key != null)
                {
                    path = key.GetValue("InstallDir") as string;
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                return this.ToolExe;
            }

            return Path.Combine(path, this.ToolExe);
        }

        /// <summary>
        /// Log standard error and standard out. Overridden to add build steps for important messages and to detect errors and warnings.
        /// </summary>
        /// <param name="singleLine">A single line of stderr or stdout.</param>
        /// <param name="messageImportance">The importance of the message. Controllable via the StandardErrorImportance and StandardOutImportance properties.</param>
        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            // Add build steps for important messages.
            if (messageImportance == MessageImportance.High)
            {
                this.BuildStep.Add("DevEnv Message", singleLine, DateTime.Now, BuildStepStatus.Succeeded);
            }

            // Detect project compilation and insert a build step and compilation summary.
            Match match = this.ProjectCompilationRegex.Match(singleLine);

            if (match.Success)
            {
                // Update the existing project build step, if we have one.
                this.UpdateProjectBuildStep();

                this.CompilationSummary = this.ConfigurationSummary.AddCompilationSummary();
                this.CompilationSummary.ProjectFile = match.Groups["Project"].Value;

                this.ProjectBuildStep = this.BuildStep.Add(this.CompilationSummary.ProjectFile, "DevEnv is building project " + this.CompilationSummary.ProjectFile, DateTime.Now);
            }
            else if (this.StaticAnalysisErrorRegex.IsMatch(singleLine))
            {
                // Detect static analysis errors and warnings and update the compilation summaries.
                if (this.CompilationSummary != null)
                {
                    this.CompilationSummary.StaticAnalysisErrors++;
                }

                this.errorEncountered = true;
            }
            else if (this.StaticAnalysisWarningRegex.IsMatch(singleLine))
            {
                if (this.CompilationSummary != null)
                {
                    this.CompilationSummary.StaticAnalysisWarnings++;
                }
            }
            else if (this.ErrorRegex.IsMatch(singleLine))
            {
                // Detect errors and warnings and update the compilation summaries.
                if (this.CompilationSummary != null)
                {
                    this.CompilationSummary.CompilationErrors++;
                }

                this.errorEncountered = true;
            }
            else if (this.WarningRegex.IsMatch(singleLine))
            {
                if (this.CompilationSummary != null)
                {
                    this.CompilationSummary.CompilationWarnings++;
                }
            }

            // Call the ToolTask implementation to make sure events get logged to the attached MSBuild loggers.
            base.LogEventsFromTextOutput(singleLine, messageImportance);
        }    

        /// <summary>
        /// Helper method that updates the currently compiling project's build step.
        /// </summary>
        private void UpdateProjectBuildStep()
        {
            if (this.ProjectBuildStep != null)
            {
                if (this.CompilationSummary != null)
                {
                    this.ProjectBuildStep.Status = this.CompilationSummary.CompilationErrors + this.CompilationSummary.StaticAnalysisErrors == 0 ?
                                                                                                                                                     BuildStepStatus.Succeeded :
                                                                                                                                                                                   BuildStepStatus.Failed;
                }
                else
                {
                    this.ProjectBuildStep.Status = BuildStepStatus.Succeeded;
                }

                this.ProjectBuildStep.FinishTime = DateTime.Now;
                this.ProjectBuildStep.Save();
                this.ProjectBuildStep = null;
            }
        }
    }
}