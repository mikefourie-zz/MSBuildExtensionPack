//-----------------------------------------------------------------------
// <copyright file="Parallel.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>BuildTargetsInParallel</i> (<b>Required: </b> Targets <b>Optional:</b> AdditionalProperties, ProjectFile, WaitAll, WorkingDirectory)</para>
    /// <para><i>BuildTargetSetsInParallel</i> (<b>Required: </b> Targets <b>Optional:</b> AdditionalProperties, ProjectFile, WaitAll, WorkingDirectory)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// ]]></code>    
    /// </example>
    [HelpUrl("")]
    public class Parallel : BaseTask
    {
        private const string BuildTargetsInParallelTaskAction = "BuildTargetsInParallel";
        private const string BuildTargetSetsInParallelTaskAction = "BuildTargetSetsInParallel";
        private bool waitAll = true;

        [DropdownValue(BuildTargetsInParallelTaskAction)]
        [DropdownValue(BuildTargetSetsInParallelTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
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
            var exec = new ShellWrapper("msbuild.exe", "\"" + projectFile + "\" /t:" + item.ItemSpec + properties);
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
                this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Error Code: {0}", exitCode));
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
            this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.CurrentCulture, "Building Target Set: {0} - {1}", item.ItemSpec, item.GetMetadata("Targets")));
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
            var exec = new ShellWrapper("msbuild.exe", "\"" + projectFile + "\" /t:" + item.GetMetadata("Targets") + properties);
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
                this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Error Code: {0}", exitCode));
            }
        }
    }
}