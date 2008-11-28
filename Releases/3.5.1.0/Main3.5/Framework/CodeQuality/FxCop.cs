//-----------------------------------------------------------------------
// <copyright file="FxCop.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.CodeQuality
{
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.Build.Framework;

    /// <summary>
    /// The FxCop task provides a basic wrapper over FxCopCmd.exe. See http://msdn.microsoft.com/en-gb/library/bb429449(VS.80).aspx for more details.
    /// <para/>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Analyse</i> (<b>Required: </b> Project or Files, OutputFile <b>Optional: </b>DependencyDirectories, Imports, Rules, ShowSummary, UpdateProject, Verbose, UpdateProject, LogToConsole, Types, FxCopPath, ReportXsl, OutputFile, ConsoleXsl, Project <b>Output: </b>AnalysisFailed, OutputText)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <ItemGroup>
    ///         <!--- Need to add to the dependencies because MSBuild.ExtensionPack.CodeQuality.StyleCop.dll references StyleCop -->
    ///         <DependencyDirectories Include="c:\Program Files (x86)\MSBuild\Microsoft\StyleCop\v4.3"/>
    ///         <!-- Define a bespoke set of rules to run. Prefix the Rules path with ! to treat warnings as errors -->
    ///         <Rules Include="c:\Program Files (x86)\Microsoft FxCop 1.36\Rules\DesignRules.dll"/>
    ///         <Files Include="C:\Projects\CodePlex\MSBuildExtensionPack\Solutions\Main3.5\BuildBinaries\MSBuild.ExtensionPack.CodeQuality.StyleCop.dll"/>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Call the task using a collection of files and all default rules -->
    ///         <MSBuild.ExtensionPack.CodeQuality.FxCop TaskAction="Analyse" Files="@(Files)" OutputFile="c:\fxcoplog1.txt">
    ///             <Output TaskParameter="AnalysisFailed" PropertyName="Result"/>
    ///         </MSBuild.ExtensionPack.CodeQuality.FxCop>
    ///         <Message Text="CA1 Failed: $(Result)"/>
    ///         <!-- Call the task using a project file -->        
    ///         <MSBuild.ExtensionPack.CodeQuality.FxCop TaskAction="Analyse" Project="c:\Projects\CodePlex\MSBuildExtensionPack\Solutions\Main3.5\MSBuildFramework\XmlSamples\FXCop.FxCop" DependencyDirectories="@(DependencyDirectories)" OutputFile="c:\fxcoplog2.txt">
    ///             <Output TaskParameter="AnalysisFailed" PropertyName="Result"/>            
    ///         </MSBuild.ExtensionPack.CodeQuality.FxCop>
    ///         <Message Text="CA2 Failed: $(Result)"/>
    ///         <!-- Call the task using a collection of files and bespoke rules. We can access the exact failure message using OutputText -->
    ///         <MSBuild.ExtensionPack.CodeQuality.FxCop TaskAction="Analyse" Rules="@(Rules)" Files="@(Files)"  OutputFile="c:\fxcoplog3.txt" LogToConsole="true">
    ///             <Output TaskParameter="AnalysisFailed" PropertyName="Result"/>
    ///             <Output TaskParameter="OutputText" PropertyName="Text"/>
    ///         </MSBuild.ExtensionPack.CodeQuality.FxCop>
    ///         <Message Text="CA3 Failed: $(Result)"/>
    ///         <Message Text="Failure Text: $(Text)" Condition="$(Result) == 'true'"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class FxCop : BaseTask
    {
        private const string cAnalyseTaskAction = "Analyse";
        
        private string fxcopPath;
        private bool logToConsole = true;
        private bool showSummary = true;

        [DropdownValue(cAnalyseTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the Item Collection of assemblies to analyse (/file option)
        /// </summary>
        [TaskAction(cAnalyseTaskAction, true)]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// Sets the DependencyDirectories :(/directory option)
        /// </summary>
        [TaskAction(cAnalyseTaskAction, false)]
        public ITaskItem[] DependencyDirectories { get; set; }

        /// <summary>
        /// Sets the name of an analysis report or project file to import (/import option)
        /// </summary>
        [TaskAction(cAnalyseTaskAction, false)]
        public ITaskItem[] Imports { get; set; }

        /// <summary>
        /// Sets the location of rule libraries to load (/rule option). Prefix the Rules path with ! to treat warnings as errors
        /// </summary>
        [TaskAction(cAnalyseTaskAction, false)]
        public ITaskItem[] Rules { get; set; }

        /// <summary>
        /// Set to true to display a summary (/summary option). Default is true
        /// </summary>
        [TaskAction(cAnalyseTaskAction, false)]
        public bool ShowSummary
        {
            get { return this.showSummary; }
            set { this.showSummary = value; }
        }

        /// <summary>
        /// Set to true to output verbose information during analysis (/verbose option)
        /// </summary>
        [TaskAction(cAnalyseTaskAction, false)]
        public bool Verbose { get; set; }

        /// <summary>
        /// Saves the results of the analysis in the project file. This option is ignored if the /project option is not specified (/update option)
        /// </summary>
        [TaskAction(cAnalyseTaskAction, false)]
        public bool UpdateProject { get; set; }

        /// <summary>
        /// Set to true to direct analysis output to the console (/console option). Default is true
        /// </summary>
        [TaskAction(cAnalyseTaskAction, false)]
        public bool LogToConsole
        {
            get { return this.logToConsole; }
            set { this.logToConsole = value; }
        }

        /// <summary>
        /// Specifies the types to analyze
        /// </summary>
        [TaskAction(cAnalyseTaskAction, false)]
        public string Types { get; set; }

        /// <summary>
        /// Sets the path to FxCopCmd.exe. Default is 32bit: 'c:\Program Files\Microsoft FxCop 1.36\FxCopCmd.exe', 64bit: 'c:\Program Files (x86)\Microsoft FxCop 1.36\FxCopCmd.exe'
        /// </summary>
        [TaskAction(cAnalyseTaskAction, false)]
        public string FxCopPath
        {
            get { return this.fxcopPath; }
            set { this.fxcopPath = value; }
        }
        
        /// <summary>
        /// Sets the ReportXsl (/outXsl: option)
        /// </summary>
        [TaskAction(cAnalyseTaskAction, false)]
        public string ReportXsl { get; set; }
        
        /// <summary>
        /// Set the name of the file for the analysis report
        /// </summary>
        [Required]
        [TaskAction(cAnalyseTaskAction, false)]
        public string OutputFile { get; set; }

        /// <summary>
        /// Sets the ConsoleXsl (/consoleXsl option)
        /// </summary>
        [TaskAction(cAnalyseTaskAction, false)]
        public string ConsoleXsl { get; set; }

        /// <summary>
        /// Set the name of the .fxcop project to use
        /// </summary>
        [TaskAction(cAnalyseTaskAction, false)]
        public string Project { get; set; }

        /// <summary>
        /// Gets AnalysisFailed. True if FxCop logged Code Analysis errors to the Output file.
        /// </summary>
        [Output]
        [TaskAction(cAnalyseTaskAction, false)]
        public bool AnalysisFailed { get; set; }

        /// <summary>
        /// Gets the OutputText emitted during analysis
        /// </summary>
        [Output]
        [TaskAction(cAnalyseTaskAction, false)]
        public string OutputText { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            if (string.IsNullOrEmpty(this.FxCopPath))
            {
                if (System.IO.File.Exists(@"c:\Program Files\Microsoft FxCop 1.36\FxCopCmd.exe"))
                {
                    this.fxcopPath = @"c:\Program Files\Microsoft FxCop 1.36\FxCopCmd.exe";
                }
                else if (System.IO.File.Exists(@"c:\Program Files (x86)\Microsoft FxCop 1.36\FxCopCmd.exe"))
                {
                    this.fxcopPath = @"c:\Program Files (x86)\Microsoft FxCop 1.36\FxCopCmd.exe";
                }
                else
                {
                    Log.LogError("FxCopCmd.exe was not found in the default location. Use FxCopPath to specify it.");
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
                    arguments += " /directory:\"" + i.ItemSpec + "\"";
                }
            }

            if (this.Imports != null)
            {
                foreach (ITaskItem i in this.Imports)
                {
                    arguments += " /import:\"" + i.ItemSpec + "\"";
                }
            }

            if (this.Rules != null)
            {
                foreach (ITaskItem i in this.Rules)
                {
                    arguments += " /rule:\"" + i.ItemSpec + "\"";
                }
            }

            if (this.Files != null)
            {
                foreach (ITaskItem i in this.Files)
                {
                    arguments += " /file:\"" + i.ItemSpec + "\"";
                }
            }
            else if (!string.IsNullOrEmpty(this.Project))
            {
                arguments += " /project:\"" + this.Project + "\"";
            }
            else
            {
                Log.LogError("A Project or Files collection must be passed.");
                return;
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
                    Log.LogError(proc.ExitCode.ToString(CultureInfo.CurrentCulture));
                    this.AnalysisFailed = true;
                    return;
                }

                this.AnalysisFailed = System.IO.File.Exists(this.OutputFile);
            }
        }
    }
}