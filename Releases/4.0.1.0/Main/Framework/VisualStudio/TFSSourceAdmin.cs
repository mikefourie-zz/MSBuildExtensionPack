//-----------------------------------------------------------------------
// <copyright file="TfsSourceAdmin.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.VisualStudio
{
    using System;
    using System.Globalization;
    using System.IO;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Branch</i> (<b>Required: </b>OldItem, NewItem <b>Optional: </b>Version, WorkingDirectory, VersionSpec <b>Output:</b> ExitCode)</para>
    /// <para><i>Rename</i> (<b>Required: </b>OldItem, NewItem <b>Optional: </b>Version, WorkingDirectory, VersionSpec <b>Output:</b> ExitCode)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- Perfrom various source administration operations -->
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSourceAdmin TaskAction="Branch" OldItem="C:\Projects\SpeedCMMI\Demo" NewItem="C:\Projects\SpeedCMMI\Demo1\B4" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Checkin" ItemPath="C:\Projects\SpeedCMMI" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Get" ItemPath="C:\Projects\SpeedCMMI" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSourceAdmin TaskAction="Rename" OldItem="C:\Projects\SpeedCMMI\Demo1\B4\VersionNumber.cs" NewItem="C:\Projects\SpeedCMMI\Demo1\B4\VersionNumberNew.cs" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Checkin" ItemPath="C:\Projects\SpeedCMMI" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.0.0/html/3a47a393-8a00-ad50-a5e7-55b3f131a724.htm")]
    public class TfsSourceAdmin : BaseTask
    {
        private const string BranchTaskAction = "Branch";
        private const string RenameTaskAction = "Rename";
        
        private string teamFoundationExe;
        private string version = "2008";
        private ShellWrapper shellWrapper;

        [DropdownValue(BranchTaskAction)]
        [DropdownValue(RenameTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the version spec for Branch
        /// </summary>
        [TaskAction(BranchTaskAction, false)]
        [TaskAction(RenameTaskAction, false)]
        public string VersionSpec { get; set; }

        /// <summary>
        /// ItemSpec to branch
        /// </summary>
        [TaskAction(BranchTaskAction, true)]
        [TaskAction(RenameTaskAction, true)]
        public string OldItem { get; set; }

        /// <summary>
        /// ItemSpec to branch too
        /// </summary>
        [TaskAction(BranchTaskAction, true)]
        [TaskAction(RenameTaskAction, true)]
        public string NewItem { get; set; }

        /// <summary>
        /// Sets the working directory.
        /// </summary>
        [TaskAction(BranchTaskAction, false)]
        [TaskAction(RenameTaskAction, false)]
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Sets the version of Tfs. Default is 2008
        /// </summary>
        [TaskAction(BranchTaskAction, false)]
        [TaskAction(RenameTaskAction, false)]
        public string Version
        {
            get { return this.version; }
            set { this.version = value; }
        }

        /// <summary>
        /// Gets the ExitCode
        /// </summary>
        [Output]
        public int ExitCode { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            this.ResolveExePath();
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "TF Operation: {0}", this.TaskAction));
            switch (this.TaskAction)
            {
                case "Branch":
                    this.Branch();
                    break;
                case "Rename":
                    this.Rename();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Rename()
        {
            string args = string.Format(CultureInfo.CurrentCulture, "\"{0}\" \"{1}\"", this.OldItem, this.NewItem);
            this.ExecuteCommand("rename ", args);
        }

        private void Branch()
        {
            string args = string.Format(CultureInfo.CurrentCulture, "\"{0}\" \"{1}\" /noprompt /noget", this.OldItem, this.NewItem);
            if (!string.IsNullOrEmpty(this.VersionSpec))
            {
                args += " /version:" + "\"" + this.VersionSpec + "\"";
            }
            
            this.ExecuteCommand("branch", args);
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="options">The options.</param>
        private void ExecuteCommand(string action, string options)
        {
            string arguments = String.Format(CultureInfo.CurrentCulture, "{0} {1}", action, options);

            this.shellWrapper = new ShellWrapper(this.teamFoundationExe, arguments);
            if (string.IsNullOrEmpty(this.WorkingDirectory) == false)
            {
                this.shellWrapper.WorkingDirectory = this.WorkingDirectory;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Executing {0} with {1}", this.shellWrapper.Executable, arguments));
            this.ExitCode = this.shellWrapper.Execute();
            this.LogTaskMessage(MessageImportance.Low, this.shellWrapper.StandardOutput);
            this.SwitchReturnValue(this.shellWrapper.StandardError.Trim());
        }

        private void SwitchReturnValue(string error)
        {
            switch (this.ExitCode)
            {
                case 1:
                    this.LogTaskWarning("Exit Code 1. Partial success: " + error);
                    break;
                case 2:
                    this.Log.LogError("Exit Code 2. Unrecognized command: " + error);
                    break;
                case 100:
                    this.Log.LogError("Exit Code 100. Nothing Succeeded: " + error);
                    break;
            }
        }

        private void ResolveExePath()
        {
            this.LogTaskMessage("Resolve TF.exe path");

            string vstools = string.Empty;
            switch (this.Version)
            {
                case "2008":
                    vstools = Environment.GetEnvironmentVariable("VS90COMNTOOLS");
                    break;
                case "2005":
                    vstools = Environment.GetEnvironmentVariable("VS80COMNTOOLS");
                    break;
            }

            if (!string.IsNullOrEmpty(vstools))
            {
                this.teamFoundationExe = Path.Combine(vstools, @"..\IDE\tf.exe");
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "TF.exe path resolved to: {0}", this.teamFoundationExe));
            }

            if (!File.Exists(this.teamFoundationExe))
            {
                this.teamFoundationExe = "tf.exe";
                this.LogTaskMessage("Unable to resolve TF.exe path. Assuming it is in the PATH environment variable.");
            }
        }
    }
}