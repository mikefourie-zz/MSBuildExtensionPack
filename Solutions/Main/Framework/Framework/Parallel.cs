//-----------------------------------------------------------------------
// <copyright file="Parallel.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>BuildTargetsInParallel</i> (<b>Required: </b> Targets <b>Optional:</b> AdditionalProperties, ProjectFile, WaitAll, WorkingDirectory, MultiLog, MultiLogOpenOnFailure, MultiLogVerbosity, MultiLogResponseVerbosity, MultiProc, MaxCpuCount, NodeReuse)</para>
    /// <para><i>BuildTargetSetsInParallel</i> (<b>Required: </b> Targets <b>Optional:</b> AdditionalProperties, ProjectFile, WaitAll, WorkingDirectory, MultiLog, MultiLogOpenOnFailure, MultiLogVerbosity, MultiLogResponseVerbosity, MultiProc, MaxCpuCount, NodeReuse)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" InitialTargets="Throttle" DefaultTargets="Normal;BuildTargetSetsInParallel;BuildTargetsInParallel" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <ItemGroup>
    ///         <MyTargetSets Include="1">
    ///             <LogFilePath>C:\b</LogFilePath>
    ///             <LogFileName>Target1yahoo.txt</LogFileName>
    ///             <Targets>Target1;Target2</Targets>
    ///             <Properties>MyPropValue=MyPropValue1</Properties>
    ///         </MyTargetSets>
    ///         <MyTargetSets Include="2">
    ///             <Targets>Target3</Targets>
    ///         </MyTargetSets>
    ///         <MyTargets Include="Target1">
    ///             <Properties>MyPropValue=MyPropValue1</Properties>
    ///         </MyTargets>
    ///         <MyTargets Include="Target2;Target3">
    ///             <LogFilePath>C:\b</LogFilePath>
    ///         </MyTargets>
    ///     </ItemGroup>
    ///     <Target Name="Normal" DependsOnTargets="Target1;Target2;Target3"/>
    ///     <Target Name="BuildTargetSetsInParallel">
    ///         <MSBuild.ExtensionPack.Framework.Parallel MultiLog="$(MultiLog)" MultiLogAppend="$(MultiLogAppend)" MultiLogOpenOnFailure="$(MultiLogOpenOnFailure)" TaskAction="BuildTargetSetsInParallel" Targets="@(MyTargetSets)"  AdditionalProperties="SkipInitial=true"/>
    ///     </Target>
    ///     <Target Name="BuildTargetsInParallel">
    ///         <MSBuild.ExtensionPack.Framework.Parallel MultiLog="$(MultiLog)" MultiLogAppend="$(MultiLogAppend)" MultiLogOpenOnFailure="$(MultiLogOpenOnFailure)" TaskAction="BuildTargetsInParallel" Targets="@(MyTargets)" AdditionalProperties="SkipInitial=true"/>
    ///     </Target>
    ///     <Target Name="Target1">
    ///         <MSBuild.ExtensionPack.Framework.Thread TaskAction="Sleep" Timeout="1000"/>
    ///         <Message Text="MyPropValue = $(MyPropValue)" Importance="High"/>
    ///     </Target>
    ///     <Target Name="Target2">
    ///         <MSBuild.ExtensionPack.Framework.Thread TaskAction="Sleep" Timeout="4000"/>
    ///     </Target>
    ///     <Target Name="Target3">
    ///         <MSBuild.ExtensionPack.Framework.Thread TaskAction="Sleep" Timeout="2000"/>
    ///     </Target>
    ///     <Target Name="Throttle" Condition="$(SkipInitial) != 'true'">
    ///         <MSBuild.ExtensionPack.Framework.Thread TaskAction="Sleep" Timeout="1000"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class Parallel : BaseTask
    {
        private const string BuildTargetsInParallelTaskAction = "BuildTargetsInParallel";
        private const string BuildTargetSetsInParallelTaskAction = "BuildTargetSetsInParallel";
        private bool waitAll = true;
        private bool multiLogAppend;
        private LoggerVerbosity multiLogVerbosity = LoggerVerbosity.Diagnostic;
        private LoggerVerbosity multiLogResponseVerbosity = LoggerVerbosity.Minimal;
        private bool nodereuse;
        private string multiprocparameter = string.Empty;

        /// <summary>
        /// Specifies whether or not to use the /m multiproc parameter. If you include this switch without specifying a value for MaxCpuCount, MSBuild will use up to the number of processors in the computer. Default is false.
        /// </summary>
        public bool MultiProc { get; set; }

        /// <summary>
        /// Specifies the maximum number of concurrent processes to use when building. Use this with MultiProc parameter. Default is 0.
        /// </summary>
        public int MaxCpuCount { get; set; }

        /// <summary>
        /// Enable or disable the re-use of MSBuild nodes when using MultiProc. Default is false
        /// </summary>
        public bool NodeReuse
        {
            get { return this.nodereuse; }
            set { this.nodereuse = value; }
        }

        /// <summary>
        /// Specifies whether to wait for all Targets to complete execution before returning to MSBuild or whether to wait for all to complete. Default is true.
        /// </summary>
        public bool WaitAll
        {
            get { return this.waitAll; }
            set { this.waitAll = value; }
        }

        /// <summary>
        /// Specifies the working directory. Default is null and MSBuild is resolved to the Path environment variable.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Specifies whether each parallel execution should log to it's own log file rather than the parent. Default is false.
        /// For BuildTargetsInParallel you can specify a LogFilePath metadata value to define the root path to log to, 
        /// otherwise they are written to the directory of the calling project. The name of the target is used as the file name.
        /// For BuildTargetSetsInParallel you can specify a LogFilePath and a LogFileName metatdatavalue. If LogFileName is not passed, the target name is used.
        /// </summary>
        public bool MultiLog { get; set; }

        /// <summary>
        /// Specifies whether to open the log file containing the error info on failure. Default is false
        /// </summary>
        public bool MultiLogOpenOnFailure { get; set; }

        /// <summary>
        /// Specifies whether to append to existing log files. Default is false
        /// </summary>
        public bool MultiLogAppend
        {
            get { return this.multiLogAppend; }
            set { this.multiLogAppend = value; }
        }

        /// <summary>
        /// Specifies the verbosity to log to the individual files with. Default is Diagnostic. Note this is case sensitive.
        /// </summary>
        public string MultiLogVerbosity
        {
            get { return this.multiLogVerbosity.ToString(); }
            set { this.multiLogVerbosity = (LoggerVerbosity)Enum.Parse(typeof(LoggerVerbosity), value); }
        }

        /// <summary>
        /// Specifies the verbosity of logging fed back to the calling task. Default is Minimal
        /// </summary>
        public string MultiLogResponseVerbosity
        {
            get { return this.multiLogResponseVerbosity.ToString(); }
            set { this.multiLogResponseVerbosity = (LoggerVerbosity)Enum.Parse(typeof(LoggerVerbosity), value); }
        }

        /// <summary>
        /// Speficies the MSBuild project to use. Defaults to the calling MSBuild file.
        /// </summary>
        public ITaskItem ProjectFile { get; set; }

        /// <summary>
        /// Specifies the Targets to execute. Properties and Targets metadata can be set depending on the TaskAction. See the samples.
        /// </summary>
        [Required]
        public ITaskItem[] Targets { get; set; }

        /// <summary>
        /// Specifies additional properties to pass through to the new parallel instances of MSBuild.
        /// </summary>
        public string AdditionalProperties { get; set; }
        
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            if (!this.MultiLog)
            {
                this.multiLogResponseVerbosity = LoggerVerbosity.Normal;
            }

            if (this.MultiProc)
            {
                this.multiprocparameter = " /m";

                if (this.MaxCpuCount > 0)
                {
                    this.multiprocparameter = " /m:" + this.MaxCpuCount;
                }
            }

            switch (this.TaskAction)
            {
                case BuildTargetsInParallelTaskAction:
                    this.BuildTargetsInParallel();
                    break;
                case BuildTargetSetsInParallelTaskAction:
                    this.BuildTargetSetsInParallel();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void BuildTargetsInParallel()
        {
            try
            {
                string targets = this.Targets.Aggregate(string.Empty, (current, t) => current + (t.ItemSpec + ";"));
                this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.CurrentCulture, "Building Targets: {0}", targets.Remove(targets.Length - 1, 1)));

                System.Threading.Tasks.Task[] tasks = new System.Threading.Tasks.Task[this.Targets.Length];
                for (int i = 0; i < this.Targets.Length; i++)
                {
                    int i1 = i;
                    tasks[i] = System.Threading.Tasks.Task.Factory.StartNew(() => this.ExecuteTarget(this.Targets[i1]));
                }

                if (this.WaitAll)
                {
                    // Block until all tasks complete.
                    System.Threading.Tasks.Task.WaitAll(tasks);
                }
                else
                {
                    // Exit after first task completes
                    System.Threading.Tasks.Task.WaitAny(tasks);
                }
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.InnerExceptions)
                {
                    this.Log.LogError(ex.Message);
                }
            }
        }

        private void ExecuteTarget(ITaskItem item)
        {
            string properties = item.GetMetadata("Properties");
            if (!string.IsNullOrEmpty(properties))
            {
                properties = " /p:" + properties;
            }

            if (!string.IsNullOrEmpty(this.AdditionalProperties))
            {
                properties += " /p:" + this.AdditionalProperties;
            }

            string projectFile = this.ProjectFile == null ? this.BuildEngine.ProjectFileOfTaskNode : this.ProjectFile.ItemSpec;
            string logginginfo = string.Empty;
            string logfileName = item.GetMetadata("LogFilePath");
            if (string.IsNullOrEmpty(logfileName))
            {
                logfileName = item.ItemSpec + ".txt";
            }
            else
            {
                if (!Directory.Exists(logfileName))
                {
                    Directory.CreateDirectory(logfileName);
                }

                logfileName = System.IO.Path.Combine(logfileName, item.ItemSpec + ".txt");
            }

            if (this.MultiLog)
            {
                // note there is a bug in MSBuild loggers whereby the logger will append whenever it sees append in the arguments, so you can's say append=false.
                string append = string.Empty;
                if (this.multiLogAppend)
                {
                    append = "append=true;";
                }

                logginginfo = string.Format(CultureInfo.CurrentCulture, "/l:FileLogger,Microsoft.Build.Engine;{0}verbosity={1};logfile=\"{2}\"", append, this.MultiLogVerbosity, logfileName);
            }
            
            var exec = new ShellWrapper("msbuild.exe", "\"" + projectFile + "\" /v:" + this.MultiLogResponseVerbosity + " /t:" + item.ItemSpec + properties + this.multiprocparameter + " /nr:" + this.nodereuse + " " + logginginfo);
            if (string.IsNullOrEmpty(this.WorkingDirectory) == false)
            {
                exec.WorkingDirectory = this.WorkingDirectory;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Executing {0} {1}", exec.Executable, exec.Arguments));

            // stderr is logged as errors
            exec.ErrorDataReceived += (sender, e) =>
                                          {
                                              if (e.Data != null)
                                              {
                                                  this.Log.LogError(e.Data);
                                              }
                                          };

            exec.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        if (this.Log != null)
                        {
                            try
                            {
                                this.Log.LogMessage(MessageImportance.Normal, e.Data);
                            }
                            catch
                            {
                                // do nothing. We have a race condition here with the MSBuild host being killed and the logging still trying to occur.
                            }
                        }
                    }
                };

            // execute the process
            int exitCode = exec.Execute();

            if (exitCode != 0)
            {
                string errorFile = string.Empty;
                if (this.MultiLog)
                {
                    errorFile = "Additional information may be found in: " + logfileName;
                }

                this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Error Code: {0}. {1}", exitCode, errorFile));

                if (this.MultiLog && this.MultiLogOpenOnFailure)
                {
                    FileInfo f = new FileInfo(this.BuildEngine.ProjectFileOfTaskNode);
                    System.Diagnostics.Process.Start(System.IO.Path.Combine(f.Directory.FullName, logfileName));
                }
            }
        }

        private void BuildTargetSetsInParallel()
        {
            try
            {
                System.Threading.Tasks.Task[] tasks = new System.Threading.Tasks.Task[this.Targets.Length];
                for (int i = 0; i < this.Targets.Length; i++)
                {
                    int i1 = i;
                    tasks[i] = System.Threading.Tasks.Task.Factory.StartNew(() => this.ExecuteTargetSet(this.Targets[i1]));
                }

                if (this.WaitAll)
                {
                    // Block until all tasks complete.
                    System.Threading.Tasks.Task.WaitAll(tasks);
                }
                else
                {
                    // Exit after first task completes
                    System.Threading.Tasks.Task.WaitAny(tasks);
                }
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.InnerExceptions)
                {
                    this.Log.LogError(ex.Message);
                }
            }
        }

        private void ExecuteTargetSet(ITaskItem item)
        {
            string resolvedtargets = item.GetMetadata("Targets");
            if (resolvedtargets.EndsWith(";", StringComparison.OrdinalIgnoreCase))
            {
                resolvedtargets = resolvedtargets.Remove(resolvedtargets.Length - 1, 1);
            }

            this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.CurrentCulture, "Building Target Set: {0} - {1}", item.ItemSpec, resolvedtargets));
            string properties = item.GetMetadata("Properties");
            if (!string.IsNullOrEmpty(properties))
            {
                properties = " /p:" + properties;
            }

            if (!string.IsNullOrEmpty(this.AdditionalProperties))
            {
                properties += " /p:" + this.AdditionalProperties;
            }

            string projectFile = this.ProjectFile == null ? this.BuildEngine.ProjectFileOfTaskNode : this.ProjectFile.ItemSpec;
            
            string logginginfo = string.Empty;
            string logfileName = item.GetMetadata("LogFileName");
            if (string.IsNullOrEmpty(logfileName))
            {
                logfileName = item.ItemSpec + ".txt";
            }
            else
            {
                string logfilePath = item.GetMetadata("LogFilePath");
                if (!string.IsNullOrEmpty(logfilePath))
                {
                    if (!Directory.Exists(logfilePath))
                    {
                        Directory.CreateDirectory(logfilePath);
                    }

                    logfileName = System.IO.Path.Combine(logfilePath, logfileName);
                }
            }

            if (this.MultiLog)
            {
                // note there is a bug in MSBuild loggers whereby the logger will append whenever it sees append in the arguments, so you can's say append=false.
                string append = string.Empty;
                if (this.multiLogAppend)
                {
                    append = "append=true;";
                }

                logginginfo = string.Format(CultureInfo.CurrentCulture, "/l:FileLogger,Microsoft.Build.Engine;{0}verbosity={1};logfile=\"{2}\"", append, this.MultiLogVerbosity, logfileName);
            }

            var exec = new ShellWrapper("msbuild.exe", "\"" + projectFile + "\" /v:" + this.MultiLogResponseVerbosity + " /t:" + resolvedtargets + properties + this.multiprocparameter + " /nr:" + this.nodereuse + " " + logginginfo);
            if (string.IsNullOrEmpty(this.WorkingDirectory) == false)
            {
                exec.WorkingDirectory = this.WorkingDirectory;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Executing {0} {1}", exec.Executable, exec.Arguments));

            // stderr is logged as errors
            exec.ErrorDataReceived += (sender, e) =>
                                          {
                                              if (e.Data != null)
                                              {
                                                  this.Log.LogError(e.Data);
                                              }
                                          };

            exec.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        if (this.Log != null)
                        {
                            try
                            {
                                this.Log.LogMessage(MessageImportance.Normal, e.Data);
                            }
                            catch
                            {
                                // do nothing. We have a race condition here with the MSBuild host being killed and the logging still trying to occur.
                            }
                        }
                    }
                };

            // execute the process
            int exitCode = exec.Execute();
            if (exitCode != 0)
            {
                string errorFile = string.Empty;
                if (this.MultiLog)
                {
                    errorFile = "Additional information may be found in: " + logfileName;
                }

                this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Error Code: {0}. {1}", exitCode, errorFile));

                if (this.MultiLog && this.MultiLogOpenOnFailure)
                {
                    FileInfo f = new FileInfo(this.BuildEngine.ProjectFileOfTaskNode);
                    System.Diagnostics.Process.Start(System.IO.Path.Combine(f.Directory.FullName, logfileName));
                }
            }
        }
    }
}