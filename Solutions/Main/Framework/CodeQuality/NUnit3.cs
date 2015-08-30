//-----------------------------------------------------------------------
// <copyright file="NUnit.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.CodeQuality
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Executes Test Cases using NUnit (Tested using v3.0.5610.33199)
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///   </PropertyGroup>
    ///   <Import Project="$(TPath)"/>
    ///   <PropertyGroup>
    ///     <ToolPath>D:\Program Files (x86)\NUnit 3.0.5610\bin\net-4.0</ToolPath>
    ///   </PropertyGroup>
    ///   <Target Name="Default">
    ///     <ItemGroup>
    ///       <Assemblies Include="d:\a\*.dll"/>
    ///     </ItemGroup>
    ///     <!-- Run an NUnit Project -->
    ///     <MSBuild.ExtensionPack.CodeQuality.NUnit Assemblies="d:\a\Project1.nunit" ToolPath="$(ToolPath)">
    ///       <Output TaskParameter="Total" PropertyName="ResultTotal"/>
    ///       <Output TaskParameter="NotRun" PropertyName="ResultNotRun"/>
    ///       <Output TaskParameter="Failures" PropertyName="ResultFailures"/>
    ///       <Output TaskParameter="Errors" PropertyName="ResultErrors"/>
    ///       <Output TaskParameter="Inconclusive" PropertyName="ResultInconclusive"/>
    ///       <Output TaskParameter="Ignored" PropertyName="ResultIgnored"/>
    ///       <Output TaskParameter="Skipped" PropertyName="ResultSkipped"/>
    ///       <Output TaskParameter="Invalid" PropertyName="ResultInvalid"/>
    ///     </MSBuild.ExtensionPack.CodeQuality.NUnit>
    ///     <Message Text="ResultTotal: $(ResultTotal)"/>
    ///     <Message Text="ResultNotRun: $(ResultNotRun)"/>
    ///     <Message Text="ResultFailures: $(ResultFailures)"/>
    ///     <Message Text="ResultErrors: $(ResultErrors)"/>
    ///     <Message Text="ResultInconclusive: $(ResultInconclusive)"/>
    ///     <Message Text="ResultIgnored: $(ResultIgnored)"/>
    ///     <Message Text="ResultSkipped: $(ResultSkipped)"/>
    ///     <Message Text="ResultInvalid: $(ResultInvalid)"/>
    ///     <!--- Run NUnit over a collection of assemblies -->
    ///     <MSBuild.ExtensionPack.CodeQuality.NUnit Assemblies="@(Assemblies)" ToolPath="$(ToolPath)" OutputXmlFile="D:\a\NunitResults2.xml">
    ///       <Output TaskParameter="Total" PropertyName="ResultTotal"/>
    ///       <Output TaskParameter="NotRun" PropertyName="ResultNotRun"/>
    ///       <Output TaskParameter="Failures" PropertyName="ResultFailures"/>
    ///       <Output TaskParameter="Errors" PropertyName="ResultErrors"/>
    ///       <Output TaskParameter="Inconclusive" PropertyName="ResultInconclusive"/>
    ///       <Output TaskParameter="Ignored" PropertyName="ResultIgnored"/>
    ///       <Output TaskParameter="Skipped" PropertyName="ResultSkipped"/>
    ///       <Output TaskParameter="Invalid" PropertyName="ResultInvalid"/>
    ///     </MSBuild.ExtensionPack.CodeQuality.NUnit>
    ///     <Message Text="ResultTotal: $(ResultTotal)"/>
    ///     <Message Text="ResultNotRun: $(ResultNotRun)"/>
    ///     <Message Text="ResultFailures: $(ResultFailures)"/>
    ///     <Message Text="ResultErrors: $(ResultErrors)"/>
    ///     <Message Text="ResultInconclusive: $(ResultInconclusive)"/>
    ///     <Message Text="ResultIgnored: $(ResultIgnored)"/>
    ///     <Message Text="ResultSkipped: $(ResultSkipped)"/>
    ///     <Message Text="ResultInvalid: $(ResultInvalid)"/>
    ///   </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class NUnit3 : ToolTask
    {
        private string version = "3.0.5610";

        /// <summary>
        /// The version of NUnit to run. Default is 3.0.5610
        /// </summary>
        public string Version
        {
            get { return this.version; }
            set { this.version = value; }
        }

        /// <summary>
        /// Gets or sets the assemblies.
        /// </summary>
        /// <value>The assemblies.</value>
        [Required]
        public ITaskItem[] Assemblies { get; set; }

        /// <summary>
        /// Run tests in an x86 process on 64 bit systems.
        /// </summary>
        public bool Use32Bit { get; set; }

        /// <summary>
        /// Set to true to fail the task if this.Failures > 0. Helps for batching purposes. Default is false.
        /// </summary>
        public bool FailOnFailures { get; set; }

        /// <summary>
        /// Comma separated list of categories to include.
        /// </summary>
        public string IncludeCategory { get; set; }

        /// <summary>
        /// Comma separated list of categories to exclude.
        /// </summary>
        public string ExcludeCategory { get; set; }

        /// <summary>
        /// Sets the OutputXmlFile name
        /// </summary>
        public ITaskItem OutputXmlFile { get; set; }

        /// <summary>
        /// Sets the File to receive test error output
        /// </summary>
        public ITaskItem ErrorOutputFile { get; set; }

        /// <summary>
        /// File to receive test output
        /// </summary>
        public ITaskItem OutputFile { get; set; }

        /// <summary>
        /// Number of worker threads to be used in running tests. If not specified, defaults to
        /// 2 or the number of processors, whichever is
        /// greater.
        /// </summary>
        public int WorkerThreads { get; set; }

        /// <summary>
        /// Gets the Failures count
        /// </summary>
        [Output]
        public int Failures { get; set; }

        /// <summary>
        /// Gets the NotRun count
        /// </summary>
        [Output]
        public int NotRun { get; set; }

        /// <summary>
        /// Gets the Total count
        /// </summary>
        [Output]
        public int Total { get; set; }

        /// <summary>
        /// Gets the Errors count
        /// </summary>
        [Output]
        public int Errors { get; set; }

        /// <summary>
        /// Gets the Inconclusive count
        /// </summary>
        [Output]
        public int Inconclusive { get; set; }

        /// <summary>
        /// Gets the Ignored count
        /// </summary>
        [Output]
        public int Ignored { get; set; }

        /// <summary>
        /// Gets the Skipped count
        /// </summary>
        [Output]
        public int Skipped { get; set; }

        /// <summary>
        /// Gets the Invalid count
        /// </summary>
        [Output]
        public int Invalid { get; set; }

        /// <summary>
        /// Disable shadow copy when running in separate domain. Default is false.
        /// </summary>
        public bool NoShadow { get; set; }

        /// <summary>
        /// Sets the Project configuration (e.g.: Debug) to load
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// PROCESS isolation for test assemblies. Values: Single, Separate, Multiple. If not 
        /// specified, defaults to Separate for a single assembly or Multiple for more than one.
        /// </summary>
        public string Process { get; set; }

        /// <summary>
        /// DOMAIN isolation for test assemblies. Values: None, Single, Multiple. If not
        /// specified, defaults to Separate for a single assembly or Multiple for more than one.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// FRAMEWORK type/version to use for tests. Examples: mono, net-3.5, v4.0, 2.0, mono-4.0.
        /// If not specified, tests will run under the framework they are compiled with.
        /// </summary>
        public string Framework { get; set; }

        /// <summary>
        /// Set timeout for each test case in milliseconds
        /// </summary>
        public int TestTimeout { get; set; }

        /// <summary>
        /// Specify whether to write test case names to the output. Values: Off, On, All
        /// </summary>
        public string Labels { get; set; }

        /// <summary>
        /// Name of the test case(s), fixture(s) or namespace(s) to run
        /// </summary>
        public string Test { get; set; }

        /// <summary>
        /// Turns on use of TeamCity service messages.
        /// </summary>
        public bool TeamCity { get; set; }

        protected override string ToolName
        {
            get { return "nunit-console.exe"; }
        }

        protected override string GenerateFullPathToTool()
        {
            if (string.IsNullOrEmpty(this.ToolPath))
            {
                this.ToolPath = string.Format(CultureInfo.InvariantCulture, System.Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Nunit {0}\bin"), this.Version);
            }

            return string.IsNullOrEmpty(this.ToolPath) ? this.ToolName : Path.Combine(this.ToolPath, this.ToolName);
        }

        protected override string GenerateCommandLineCommands()
        {
            CommandLineBuilder builder = new CommandLineBuilder();
            builder.AppendFileNamesIfNotNull(this.Assemblies, " ");
            if (this.Use32Bit)
            {
                builder.AppendSwitch("--x86");
            }
            if (this.TeamCity)
            {
                builder.AppendSwitch("--teamcity");
            }
            if (!this.NoShadow)
            {
                builder.AppendSwitch("--shadowcopy");
            }
            if (this.WorkerThreads > 0)
            {
                builder.AppendSwitch("--workers=" + this.WorkerThreads);
            }
            if (this.TestTimeout > 0)
            {
                builder.AppendSwitch("--timeout=" + this.TestTimeout);
            }
            builder.AppendSwitchIfNotNull("--labels=", this.Labels);            
            builder.AppendSwitchIfNotNull("--test=", this.Test);
            builder.AppendSwitchIfNotNull("--config=", this.Configuration);
            builder.AppendSwitchIfNotNull("--include=", this.IncludeCategory);
            builder.AppendSwitchIfNotNull("--exclude=", this.ExcludeCategory);
            builder.AppendSwitchIfNotNull("--process=", this.Process);
            builder.AppendSwitchIfNotNull("--domain=", this.Domain);
            builder.AppendSwitchIfNotNull("--framework=", this.Framework);
            if (this.OutputXmlFile != null)
            {
                builder.AppendSwitch("--result=" + this.OutputXmlFile + ";format=nunit2");
            } 
            else
            {
                builder.AppendSwitch("--result=TestResult.xml;format=nunit2");
            }
            builder.AppendSwitchIfNotNull("--err=", this.ErrorOutputFile);
            builder.AppendSwitchIfNotNull("--out=", this.OutputFile);
            return builder.ToString();
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
            this.ProcessXmlResultsFile();
            if (this.FailOnFailures && this.Failures > 0)
            {
                return 1;
            }

            return 0;
        }
        
        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            this.Log.LogMessage(MessageImportance.Normal, singleLine);
        }

        private static int GetAttributeInt32Value(string name, XmlNode node)
        {
            if (node.Attributes[name] != null)
            {
                return Convert.ToInt32(node.Attributes[name].Value, CultureInfo.InvariantCulture);
            }

            return 0;
        }

        /// <summary>
        /// Processes the nunit results
        /// </summary>
        private void ProcessXmlResultsFile()
        {
            string filename = "TestResult.xml";
            if (this.OutputXmlFile != null && File.Exists(this.OutputXmlFile.ItemSpec))
            {
                filename = this.OutputXmlFile.ItemSpec;
            }

            if (File.Exists(filename))
            {
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.Load(filename);
                }
                catch (Exception ex)
                {
                    this.Log.LogError(ex.Message);
                    return;
                }

                XmlNode root = doc.DocumentElement;
                if (root == null)
                {
                    this.Log.LogError("Failed to load the OutputXmlFile");
                    return;
                }

                this.Failures = GetAttributeInt32Value("failures", root);
                this.Total = GetAttributeInt32Value("total", root);
                this.NotRun = GetAttributeInt32Value("not-run", root);
                this.Errors = GetAttributeInt32Value("errors", root);
                this.Inconclusive = GetAttributeInt32Value("inconclusive", root);
                this.Ignored = GetAttributeInt32Value("ignored", root);
                this.Skipped = GetAttributeInt32Value("skipped", root);
                this.Invalid = GetAttributeInt32Value("invalid", root);
            }
        }
    }
}
