//-----------------------------------------------------------------------
// <copyright file="StyleCop.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.CodeQuality
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.StyleCop;

    /// <summary>
    /// Wraps the StyleCopConsole class to provide a mechanism for scanning files for StyleCop compliance.
    /// <para/>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Scan</i> (<b>Required: </b>SourceFiles <b>Optional: </b>ShowOutput, ForceFullAnalysis, CacheResults, logFile, SettingsFile <b>Output: </b>Succeeded, ViolationCount, FailedFiles)</para>
    /// <para><b>Remote Execution Support:</b> No</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- Create a collection of files to scan -->
    ///         <CreateItem Include="C:\Demo\**\*.cs">
    ///             <Output TaskParameter="Include" ItemName="StyleCopFiles"/>
    ///         </CreateItem>
    ///         <!-- Run the StyleCop MSBuild task -->
    ///         <MSBuild.ExtensionPack.CodeQuality.StyleCop TaskAction="Scan" SourceFiles="@(StyleCopFiles)" ShowOutput="true" ForceFullAnalysis="true" CacheResults="false" logFile="C:\StyleCopLog.txt" SettingsFile="C:\Program Files (x86)\MSBuild\Microsoft\StyleCop\v4.3\Settings.StyleCop">
    ///             <Output TaskParameter="Succeeded" PropertyName="AllPassed"/>
    ///             <Output TaskParameter="ViolationCount" PropertyName="Violations"/>
    ///             <Output TaskParameter="FailedFiles" ItemName="Failures"/>
    ///         </MSBuild.ExtensionPack.CodeQuality.StyleCop>
    ///         <Message Text="Succeeded: $(AllPassed), Violations: $(Violations)"/>
    ///         <!-- FailedFile format is:
    ///         <ItemGroup>
    ///             <FailedFile Include="filename">
    ///                 <CheckId>SA Rule Number</CheckId>
    ///                 <RuleDescription>Rule Description</RuleDescription>
    ///                 <RuleName>Rule Name</RuleName>
    ///                 <LineNumber>Line the violation appears on</LineNumber>
    ///                 <Message>SA violation message</Message>
    ///             </FailedFile>
    ///         </ItemGroup>-->
    ///         <Message Text="%(Failures.Identity) - Failed on Line %(Failures.LineNumber). %(Failures.CheckId): %(Failures.Message)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class StyleCop : BaseTask
    {
        private List<ITaskItem> failedFiles = new List<ITaskItem>();
        private bool fullAnalysis = true;
        private bool succeeded = true;

        /// <summary>
        /// Gets the violation count.
        /// </summary>
        [Output]
        public int ViolationCount { get; set; }

        /// <summary>
        /// Sets the log file.
        /// </summary>
        public string LogFile { get; set; }

        /// <summary>
        /// Gets whether the scan succeeded.
        /// </summary>
        [Output]
        public bool Succeeded
        {
            get { return this.succeeded; }
            set { this.succeeded = value; }
        }

        /// <summary>
        /// Sets a value indicating whether StyleCop should write cache files to disk after performing an analysis. Default is false.
        /// </summary>
        public bool CacheResults { get; set; }

        /// <summary>
        /// Sets a value indicating whether to ShowOutput. Default is false
        /// </summary>
        public bool ShowOutput { get; set; }

        /// <summary>
        /// Sets a value indicating whether StyleCop should ignore cached results and perform a clean analysis. 
        /// </summary>        
        public bool ForceFullAnalysis
        {
            get { return this.fullAnalysis; }
            set { this.fullAnalysis = value; }
        }

        /// <summary>
        /// Sets the source files collection
        /// </summary>
        [Required]
        public ITaskItem[] SourceFiles { get; set; }

        /// <summary>
        /// Gets the failed files collection
        /// </summary>
        [Output]
        public ITaskItem[] FailedFiles
        {
            get { return this.failedFiles.ToArray(); }
            set { this.failedFiles = new List<ITaskItem>(value); }
        }

        /// <summary>
        /// Sets the path to the settings file to load.
        /// </summary>
        public string SettingsFile { get; set; }

        /// <summary>
        /// InternalExecute
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            switch (this.TaskAction)
            {
                case "Scan":
                    this.Scan();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Scan()
        {
            this.Log.LogMessage("Performing StyleCop scan...");
            if (File.Exists(this.SettingsFile) == false)
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The Settings file was not found: {0}", this.SettingsFile));
                return;
            }

            this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "SourceFiles count is: {0}", this.SourceFiles.Length));
            List<string> addinPaths = new List<string>();
            StyleCopConsole console = new StyleCopConsole(this.SettingsFile, this.CacheResults, null, addinPaths, true);
            Configuration configuration = new Configuration(new string[0]);
            CodeProject project = new CodeProject(DateTime.Now.ToLongTimeString().GetHashCode(), null, configuration);
            foreach (ITaskItem item2 in this.SourceFiles)
            {
                if (this.ShowOutput)
                {
                    this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Adding file: {0}", item2.ItemSpec));
                }

                if (!console.Core.Environment.AddSourceCode(project, item2.ItemSpec, null))
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "Failed to add file: {0}", item2.ItemSpec));
                    return;
                }
            }

            try
            {
                if (this.ShowOutput)
                {
                    console.OutputGenerated += this.OnOutputGenerated;
                }

                console.ViolationEncountered += this.OnViolationEncountered;
                CodeProject[] projects = new[] { project };
                console.Start(projects, this.ForceFullAnalysis);
            }
            finally
            {
                if (this.ShowOutput)
                {
                    console.OutputGenerated -= this.OnOutputGenerated;
                }

                console.ViolationEncountered -= this.OnViolationEncountered;
            }

            // log the results to disk if there have been failures AND LogFile is specified
            if (string.IsNullOrEmpty(this.LogFile) == false && this.Succeeded == false)
            {
                using (StreamWriter streamWriter = new StreamWriter(this.LogFile, false, Encoding.UTF8))
                {
                    foreach (ITaskItem i in this.FailedFiles)
                    {
                        streamWriter.WriteLine(i.ItemSpec + " (" + i.GetMetadata("CheckId") + ": " + i.GetMetadata("Message") + " Line: " + i.GetMetadata("LineNumber") + ")");
                    }
                }
            }

            // set the ViolationCount
            this.ViolationCount = this.failedFiles.Count;
        }

        /// <summary>
        /// Called when [output generated].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Microsoft.StyleCop.OutputEventArgs"/> instance containing the event data.</param>
        private void OnOutputGenerated(object sender, OutputEventArgs e)
        {
            lock (this)
            {
                Log.LogMessage(e.Output.Trim(), new object[0]);
            }
        }

        /// <summary>
        /// Called when [violation encountered].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Microsoft.StyleCop.ViolationEventArgs"/> instance containing the event data.</param>
        private void OnViolationEncountered(object sender, ViolationEventArgs e)
        {
            this.Succeeded = false;
            string file = string.Empty;
            if (((e.SourceCode != null) && (e.SourceCode.Path != null)) && (e.SourceCode.Path.Length > 0))
            {
                file = e.SourceCode.Path;
            }
            else if (((e.Element != null) && (e.Element.Document != null)) && ((e.Element.Document.SourceCode != null) && (e.Element.Document.SourceCode.Path != null)))
            {
                file = e.Element.Document.SourceCode.Path;
            }

            ITaskItem item = new TaskItem(file);
            item.SetMetadata("CheckId", e.Violation.Rule.CheckId);
            item.SetMetadata("RuleDescription", e.Violation.Rule.Description);
            item.SetMetadata("RuleName", e.Violation.Rule.Name);
            item.SetMetadata("RuleGroup", e.Violation.Rule.RuleGroup);
            item.SetMetadata("LineNumber", e.LineNumber.ToString(CultureInfo.CurrentCulture));
            item.SetMetadata("Message", e.Message);
            this.failedFiles.Add(item);
        }
    }
}