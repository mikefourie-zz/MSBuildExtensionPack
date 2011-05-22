//-----------------------------------------------------------------------
// <copyright file="FxCop.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.CodeQuality
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using Microsoft.Build.Framework;

    /// <summary>
    /// The FxCop task provides a basic wrapper over FxCopCmd.exe. See http://msdn.microsoft.com/en-gb/library/bb429449(VS.80).aspx for more details.
    /// <para/>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Analyse</i> (<b>Required: </b> Project and / or Files, OutputFile <b>Optional: </b>DependencyDirectories, Imports, Rules, ShowSummary, UpdateProject, Verbose, UpdateProject, LogToConsole, Types, FxCopPath, ReportXsl, OutputFile, ConsoleXsl, Project, SearchGac, IgnoreInvalidTargets, Quiet, ForceOutput, AspNetOnly, IgnoreGeneratedCode, OverrideRuleVisibilities, FailOnMissingRules, SuccessFile, Dictionary <b>Output: </b>AnalysisFailed, OutputText, ExitCode)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
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
    ///     <!--- Need to add to the dependencies because MSBuild.ExtensionPack.CodeQuality.StyleCop.dll references StyleCop -->
    ///     <DependencyDirectories Include="c:\Program Files (x86)\MSBuild\Microsoft\StyleCop\v4.4"/>
    ///     <!-- Define a bespoke set of rules to run. Prefix the Rules path with ! to treat warnings as errors -->
    ///     <Rules Include="C:\Program Files (x86)\Microsoft Fxcop 10.0\Rules\DesignRules.dll"/>
    ///     <Files Include="C:\Projects\MSBuildExtensionPack\Releases\4.0.1.0\Main\BuildBinaries\MSBuild.ExtensionPack.StyleCop.dll"/>
    ///   </ItemGroup>
    ///   <Target Name="Default">
    ///     <!-- Call the task using a collection of files and all default rules -->
    ///     <MSBuild.ExtensionPack.CodeQuality.FxCop TaskAction="Analyse" Files="@(Files)" OutputFile="c:\fxcoplog1.txt">
    ///       <Output TaskParameter="AnalysisFailed" PropertyName="Result"/>
    ///     </MSBuild.ExtensionPack.CodeQuality.FxCop>
    ///     <Message Text="CA1 Failed: $(Result)"/>
    ///     <!-- Call the task using a project file -->
    ///     <MSBuild.ExtensionPack.CodeQuality.FxCop TaskAction="Analyse" Files="@(Files)" Project="C:\Projects\MSBuildExtensionPack\Releases\4.0.1.0\Main\Framework\XmlSamples\FXCop.FxCop" DependencyDirectories="@(DependencyDirectories)" OutputFile="c:\fxcoplog2.txt">
    ///       <Output TaskParameter="AnalysisFailed" PropertyName="Result"/>
    ///     </MSBuild.ExtensionPack.CodeQuality.FxCop>
    ///     <Message Text="CA2 Failed: $(Result)"/>
    ///     <!-- Call the task using a collection of files and bespoke rules. We can access the exact failure message using OutputText -->
    ///     <MSBuild.ExtensionPack.CodeQuality.FxCop TaskAction="Analyse" Rules="@(Rules)" Files="@(Files)"  OutputFile="c:\fxcoplog3.txt" LogToConsole="true">
    ///       <Output TaskParameter="AnalysisFailed" PropertyName="Result"/>
    ///       <Output TaskParameter="OutputText" PropertyName="Text"/>
    ///     </MSBuild.ExtensionPack.CodeQuality.FxCop>
    ///     <Message Text="CA3 Failed: $(Result)"/>
    ///     <Message Text="Failure Text: $(Text)" Condition="$(Result) == 'true'"/>
    ///   </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.3.0/html/a111be65-19a8-05e0-5787-c187c3ee65f2.htm")]
    public class FxCop : BaseTask
    {
        private const string AnalyseTaskAction = "Analyse";
        private bool logToConsole = true;
        private bool showSummary = true;

        [DropdownValue(AnalyseTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the Item Collection of assemblies to analyse (/file option)
        /// </summary>
        [TaskAction(AnalyseTaskAction, true)]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// Sets the DependencyDirectories :(/directory option)
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public ITaskItem[] DependencyDirectories { get; set; }

        /// <summary>
        /// Sets the name of an analysis report or project file to import (/import option)
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public ITaskItem[] Imports { get; set; }

        /// <summary>
        /// Sets the location of rule libraries to load (/rule option). Prefix the Rules path with ! to treat warnings as errors
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public ITaskItem[] Rules { get; set; }

        /// <summary>
        /// Set to true to display a summary (/summary option). Default is true
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool ShowSummary
        {
            get { return this.showSummary; }
            set { this.showSummary = value; }
        }

        /// <summary>
        /// Set to true to search the GAC for missing assembly references (/gac option). Default is false
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool SearchGac { get; set; }
        
        /// <summary>
        /// Set to true to create .lastcodeanalysissucceeded file in output report directory if no build-breaking messages occur during analysis. Default is false
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool SuccessFile { get; set; }
        
        /// <summary>
        /// Set to true to run all overridable rules against all targets. Default is false
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool OverrideRuleVisibilities { get; set; }

        /// <summary>
        /// Set the override timeout for analysis deadlock detection. Analysis will be aborted when analysis of a single item by a single rule exceeds the specified amount of time. Default is 0 to disable deadlock detection.
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public int Timeout { get; set; }

        /// <summary>
        /// Set to true to treat missing rules or rule sets as an error and halt execution. Default is false
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool FailOnMissingRules { get; set; }
        
        /// <summary>
        /// Set to true to suppress analysis results against generated code. Default is false
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool IgnoreGeneratedCode { get; set; }
        
        /// <summary>
        /// Set to true to analyze only ASP.NET-generated binaries and honor global suppressions in App_Code.dll for all assemblies under analysis. Default is false
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool AspNetOnly { get; set; }
        
        /// <summary>
        /// Set to true to silently ignore invalid target files. Default is false
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool IgnoreInvalidTargets { get; set; }
        
        /// <summary>
        /// Set to true to suppress all console output other than the reporting implied by /console or /consolexsl. Default is false
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool Quiet { get; set; }
        
        /// <summary>
        /// Set to true to write output XML and project files even in the case where no violations occurred. Default is false
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool ForceOutput { get; set; }
        
        /// <summary>
        /// Set to true to output verbose information during analysis (/verbose option)
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool Verbose { get; set; }

        /// <summary>
        /// Saves the results of the analysis in the project file. This option is ignored if the /project option is not specified (/update option)
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool UpdateProject { get; set; }

        /// <summary>
        /// Set to true to direct analysis output to the console (/console option). Default is true
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public bool LogToConsole
        {
            get { return this.logToConsole; }
            set { this.logToConsole = value; }
        }

        /// <summary>
        /// Specifies the types to analyze
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public string Types { get; set; }

        /// <summary>
        /// Sets the path to FxCopCmd.exe. Default is [Program Files]\Microsoft FxCop 1.36\FxCopCmd.exe
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public string FxCopPath { get; set; }
        
        /// <summary>
        /// Sets the ReportXsl (/outXsl: option)
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public string ReportXsl { get; set; }
        
        /// <summary>
        /// Set the name of the file for the analysis report
        /// </summary>
        [Required]
        [TaskAction(AnalyseTaskAction, false)]
        public string OutputFile { get; set; }

        /// <summary>
        /// Sets the ConsoleXsl (/consoleXsl option)
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public string ConsoleXsl { get; set; }

        /// <summary>
        /// Sets the custom dictionary used by spelling rules.Default is no custom dictionary
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public ITaskItem Dictionary { get; set; }

        /// <summary>
        /// Set the name of the .fxcop project to use
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        public string Project { get; set; }

        /// <summary>
        /// Gets AnalysisFailed. True if FxCop logged Code Analysis errors to the Output file.
        /// </summary>
        [Output]
        [TaskAction(AnalyseTaskAction, false)]
        public bool AnalysisFailed { get; set; }

        /// <summary>
        /// The exit code returned from FxCop
        /// </summary>
        [Output]
        public int ExitCode { get; set; }

        /// <summary>
        /// Gets the OutputText emitted during analysis
        /// </summary>
        [Output]
        [TaskAction(AnalyseTaskAction, false)]
        public string OutputText { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            if (string.IsNullOrEmpty(this.FxCopPath))
            {
                string programFilePath = Environment.GetEnvironmentVariable("ProgramFiles");
                if (string.IsNullOrEmpty(programFilePath))
                {
                    Log.LogError("Failed to read a value from the ProgramFiles Environment Variable");
                    return;
                }

                if (System.IO.File.Exists(programFilePath + @"\Microsoft FxCop 1.36\FxCopCmd.exe"))
                {
                    this.FxCopPath = programFilePath + @"\Microsoft FxCop 1.36\FxCopCmd.exe";
                }
                else if (System.IO.File.Exists(programFilePath + @"\Microsoft FxCop 10.0\FxCopCmd.exe"))
                {
                    this.FxCopPath = programFilePath + @"\Microsoft FxCop 10.0\FxCopCmd.exe";
                }
                else
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "FxCopCmd.exe was not found in the default location. Use FxCopPath to specify it. Searched at: {0}", programFilePath + @"\Microsoft FxCop 1.36 and \Microsoft FxCop 10.0"));
                    return;
                }
            }

            switch (this.TaskAction)
            {
                case "Analyse":
                    this.Analyse();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Analyse()
        {
            string arguments = string.Empty;

            if (!string.IsNullOrEmpty(this.ReportXsl))
            {
                arguments += "/applyoutXsl /outXsl:\"" + this.ReportXsl + "\""; 
            }

            if (this.LogToConsole)
            {
                arguments += " /console";

                if (!string.IsNullOrEmpty(this.ConsoleXsl))
                {
                    arguments += " /consoleXsl:\"" + this.ConsoleXsl + "\"";
                }
            }

            if (this.UpdateProject)
            {
                arguments += " /update";
            }

            if (this.SearchGac)
            {
                arguments += " /gac";
            }

            if (this.SuccessFile)
            {
                arguments += " /successfile";
            }

            if (this.FailOnMissingRules)
            {
                arguments += " /failonmissingrules";
            }

            if (this.IgnoreGeneratedCode)
            {
                arguments += " /ignoregeneratedcode";
            }

            if (this.OverrideRuleVisibilities)
            {
                arguments += " /overriderulevisibilities";
            }
            
            if (this.AspNetOnly)
            {
                arguments += " /aspnet";
            }

            if (this.IgnoreInvalidTargets)
            {
                arguments += " /ignoreinvalidtargets";
            }

            if (this.Timeout > 0)
            {
                arguments += " /timeout:" + this.Timeout;
            }

            if (this.Quiet)
            {
                arguments += " /quiet";
            }

            if (this.ForceOutput)
            {
                arguments += " /forceoutput";
            }

            if (this.Dictionary != null)
            {
                arguments += "/dictionary:\"" + this.Dictionary.GetMetadata("FullPath") + "\"";
            }

            if (this.ShowSummary)
            {
                arguments += " /summary";
            }

            if (this.Verbose)
            {
                arguments += " /verbose";
            }          

            if (!string.IsNullOrEmpty(this.Types))
            {
                arguments += " /types:\"" + this.Types + "\"";
            }

            if (this.DependencyDirectories != null)
            {
                foreach (ITaskItem i in this.DependencyDirectories)
                {
                    string path = i.ItemSpec;
                    if (path.EndsWith(@"\", StringComparison.OrdinalIgnoreCase) || path.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                    {
                        path = path.Substring(0, path.Length - 1);
                    }

                    arguments += " /directory:\"" + path + "\"";
                }
            }

            if (this.Imports != null)
            {
                arguments = this.Imports.Aggregate(arguments, (current, i) => current + (" /import:\"" + i.ItemSpec + "\""));
            }

            if (this.Rules != null)
            {
                arguments = this.Rules.Aggregate(arguments, (current, i) => current + (" /rule:\"" + i.ItemSpec + "\""));
            }

            if (string.IsNullOrEmpty(this.Project) && this.Files == null)
            {
                Log.LogError("A Project and / or Files collection must be passed.");
                return;
            }

            if (!string.IsNullOrEmpty(this.Project))
            {
                arguments += " /project:\"" + this.Project + "\"";
            }

            if (this.Files != null)
            {
                arguments = this.Files.Aggregate(arguments, (current, i) => current + (" /file:\"" + i.ItemSpec + "\""));
            }

            arguments += " /out:\"" + this.OutputFile + "\"";

            // if the output file exists, delete it.
            if (System.IO.File.Exists(this.OutputFile))
            {
                System.IO.File.Delete(this.OutputFile);
            }

            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = this.FxCopPath;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.Arguments = arguments;
                this.LogTaskMessage("Running " + proc.StartInfo.FileName + " " + proc.StartInfo.Arguments);
                proc.Start();

                string outputStream = proc.StandardOutput.ReadToEnd();
                if (outputStream.Length > 0)
                {
                    this.LogTaskMessage(outputStream);
                    this.OutputText = outputStream;
                }

                string errorStream = proc.StandardError.ReadToEnd();
                if (errorStream.Length > 0)
                {
                    Log.LogError(errorStream);
                }

                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    this.ExitCode = proc.ExitCode;
                    Log.LogError(proc.ExitCode.ToString(CultureInfo.CurrentCulture));
                    this.AnalysisFailed = true;
                    return;
                }

                this.AnalysisFailed = System.IO.File.Exists(this.OutputFile);
            }
        }
    }
}