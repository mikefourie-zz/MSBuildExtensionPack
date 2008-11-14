//-----------------------------------------------------------------------
// <copyright file="TfsSource.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.VisualStudio
{
    using System;
    using System.Globalization;
    using System.IO;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Add</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Server, Version, WorkingDirectory, Recursive)</para>
    /// <para><i>Checkin</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Server, Comments, Notes, Version, WorkingDirectory, Recursive)</para>
    /// <para><i>Checkout</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Server, Version, WorkingDirectory, Recursive)</para>
    /// <para><i>Delete</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Server, Version, WorkingDirectory, Recursive)</para>
    /// <para><i>Get</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Server, Version, WorkingDirectory, Recursive, Force, Overwrite, All)</para>
    /// <para><i>Merge</i> (<b>Required: </b>ItemPath, Destination <b>Optional: </b>Server, Recursive, VersionSpec, Version, Baseless, Force)</para>
    /// <para><i>GetPendingChanges</i> (<b>Required: </b>ItemPath <b>Optional: </b>Server, Recursive, Version <b>Output: </b>PendingChanges, PendingChangesExist)</para>
    /// <para><i>UndoCheckout</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Server, Version, WorkingDirectory, Recursive)</para>
    /// <para><i>Undelete</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Server, Version, WorkingDirectory, Recursive)</para>
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
    ///         <FilesToAdd Include="C:\Projects\SpeedCMMI\Demo1\*"/>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Check for pending changes -->
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="GetPendingChanges" ItemPath="$/AProject/APath" WorkingDirectory="C:\Projects\SpeedCMMI">
    ///             <Output TaskParameter="PendingChanges" PropertyName="PendingChangesText" />
    ///             <Output TaskParameter="PendingChangesExist" PropertyName="DoChangesExist" />
    ///         </MSBuild.ExtensionPack.VisualStudio.TfsSource>
    ///         <Message Text="Pending Changes Report: $(PendingChangesText)"/>
    ///         <Message Text="Pending Changes Exist: $(DoChangesExist)"/>
    ///         <!-- Perfrom various other source control operations -->
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Checkout" ItemPath="C:\projects\SpeedCMMI\Demo1" Version="2008" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Checkin" ItemPath="C:\projects\SpeedCMMI\Demo1" WorkingDirectory="C:\projects\SpeedCMMI" Comments="Testing" Notes="&quot;Code reviewer&quot;=&quot;buildrobot&quot;;" OverrideText="Justdoit" />
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Add" ItemPath="C:\projects\SpeedCMMI\Demo1" Version="2008" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Checkin" ItemCol="@(FilesToAdd)" WorkingDirectory="C:\projects\SpeedCMMI" ContinueOnError="true"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Checkout" ItemCol="@(FilesToAdd)" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="UndoCheckout" ItemCol="@(FilesToAdd)" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Delete" ItemCol="@(FilesToAdd)" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Checkin" ItemPath="C:\projects\SpeedCMMI\Demo1" WorkingDirectory="C:\projects\SpeedCMMI" ContinueOnError="true"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Undelete" ItemCol="@(FilesToAdd)" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Checkin" ItemCol="@(FilesToAdd)" WorkingDirectory="C:\projects\SpeedCMMI" ContinueOnError="true"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Get" ItemPath="C:\Projects\SpeedCMMI\Demo1" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Merge" ItemPath="C:\Projects\SpeedCMMI\Client2" Destination="C:\Projects\SpeedCMMI\Client" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Checkin" ItemPath="C:\projects\SpeedCMMI\Client" WorkingDirectory="C:\projects\SpeedCMMI" Comments="Testing" Notes="&quot;Code reviewer&quot;=&quot;buildrobot&quot;;" OverrideText="Justdoit" />
    ///     </Target>
    /// </Project>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class TfsSource : BaseTask
    {
        private string tfexe;
        private string version = "2008";
        private bool recursive = true;
        private ShellWrapper shellWrapper;
        private string itemspec = string.Empty;
        private int returnValue;
        private string returnOutput;

        /// <summary>
        /// Sets the version spec for Get
        /// </summary>
        public string VersionSpec { get; set; }

        /// <summary>
        /// Forces all files to be retrieved, not just those that are out-of-date.
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Overwrites writable files that are not checked out.
        /// </summary>
        public bool Overwrite { get; set; }

        /// <summary>
        /// Implies All and Overwrite.
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Set to true to perform a merge without a basis version
        /// </summary>
        public bool Baseless { get; set; }

        /// <summary>
        /// Sets the TFS Server
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Sets the files or folders to use.
        /// </summary>
        public string ItemPath { get; set; }

        /// <summary>
        /// Sets the Item Collection of files to use.
        /// </summary>
        public ITaskItem[] ItemCol { get; set; }

        /// <summary>
        /// Sets the working directory. If the directory is mapped in a workspace, then there is no need to specify the Server.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Sets the version of Tfs. Default is 2008
        /// </summary>
        public string Version
        {
            get { return this.version; }
            set { this.version = value; }
        }

        /// <summary>
        /// Sets the comments.
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// Sets the Destination for a Merge
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// Sets the notes.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Sets whether the Tfs operation should be recursive. Default is true.
        /// </summary>
        public bool Recursive
        {
            get { return this.recursive; }
            set { this.recursive = value; }
        }

        /// <summary>
        /// Gets the pending changes in the format '/Format:detailed'
        /// </summary>
        [Output]
        public string PendingChanges { get; set; }

        /// <summary>
        /// Gets whether pending changes exist for a given ItemPath
        /// </summary>
        [Output]
        public bool PendingChangesExist { get; set; }

        /// <summary>
        /// Lets you set text to override check-in policies
        /// </summary>
        public string OverrideText { get; set; }

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
                case "Add":
                    this.Add();
                    break;
                case "Checkin":
                    this.Checkin();
                    break;
                case "Checkout":
                    this.Checkout();
                    break;
                case "Get":
                    this.GetFiles();
                    break;
                case "GetPendingChanges":
                    this.GetPendingChanges();
                    break;
                case "Delete":
                    this.Delete();
                    break;
                case "Merge":
                    this.Merge();
                    break;
                case "UndoCheckout":
                    this.UndoCheckout();
                    break;
                case "Undelete":
                    this.Undelete();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void GetPendingChanges()
        {
            this.ExecuteCommand("status", string.Empty, "/Format:detailed /user:* /recursive");
            this.PendingChanges = this.returnOutput;
            if (this.returnOutput.IndexOf("There are no pending changes", StringComparison.OrdinalIgnoreCase) < 0)
            {
                this.PendingChangesExist = true;
            }
        }

        private void GetFiles()
        {
            string args = string.Empty;

            if (!string.IsNullOrEmpty(this.VersionSpec))
            {
                args += " /version:" + "\"" + this.VersionSpec + "\"";
            }

            if (this.Force)
            {
                args += " /force";
            }
            else if (this.Overwrite)
            {
                args += " /overwrite";
            }
            else if (this.All)
            {
                args += " /all";
            }

            this.ExecuteCommand("get", args, "/noprompt /recursive");
        }

        private void Undelete()
        {
            this.ExecuteCommand("undelete", string.Empty, "/noprompt /recursive");
        }

        private void UndoCheckout()
        {
            this.ExecuteCommand("undo", string.Empty, "/noprompt /recursive");
        }

        private void Checkout()
        {
            this.ExecuteCommand("checkout", string.Empty, "/noprompt /recursive");
        }

        private void Merge()
        {
            if (string.IsNullOrEmpty(this.ItemPath))
            {
                Log.LogError("ItemPath is required for Merge");
                return;
            }

            string args = string.Empty;

            if (!string.IsNullOrEmpty(this.VersionSpec))
            {
                args += " /version:" + "\"" + this.VersionSpec + "\"";
            }

            if (this.Force)
            {
                args += " /force";
            }

            if (this.Baseless)
            {
                args += " /baseless";
            }

            if (!string.IsNullOrEmpty(this.Destination))
            {
                args += "\"" + this.Destination + "\" ";
            }
            else
            {
                Log.LogError("Destination is required for Merge");
                return;
            }

            this.ExecuteCommand("merge", args, "/noprompt /recursive");
        }

        private void Checkin()
        {
            this.ExecuteCommand("checkin", String.Format(CultureInfo.CurrentCulture, "/comment:\"{0}\" /notes:{1}", this.Comments, this.Notes), "/noprompt /recursive");
        }

        private void Delete()
        {
            this.ExecuteCommand("delete", string.Empty, "/recursive");
        }

        private void Add()
        {
            this.ExecuteCommand("add", string.Empty, "/noprompt /recursive");
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="options">The options.</param>
        /// <param name="lastOptions">The last options.</param>
        private void ExecuteCommand(string action, string options, string lastOptions)
        {
            if (!this.DetermineItemSpec())
            {
                return;
            }

            string arguments = String.Format(CultureInfo.CurrentCulture, "{0} {1} {2}", action, this.itemspec, options);
            if (string.IsNullOrEmpty(this.OverrideText) == false)
            {
                arguments += " /override:\"" + this.OverrideText + "\"";
            }

            if (string.IsNullOrEmpty(this.Server) == false)
            {
                arguments += " /s:" + this.Server;
            }

            if (!this.Recursive)
            {
                lastOptions += lastOptions.Replace("/recursive", string.Empty);
            }

            arguments += " " + lastOptions;

            this.shellWrapper = new ShellWrapper(this.tfexe, arguments);
            if (string.IsNullOrEmpty(this.WorkingDirectory) == false)
            {
                this.shellWrapper.WorkingDirectory = this.WorkingDirectory;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Executing {0} {1}", this.shellWrapper.Executable, this.shellWrapper.Arguments));
            this.returnValue = this.shellWrapper.Execute();
            this.returnOutput = this.shellWrapper.StandardOutput;
            this.LogTaskMessage(MessageImportance.Low, this.returnOutput);
            this.SwitchReturnValue(this.shellWrapper.StandardError.Trim());
        }

        private bool DetermineItemSpec()
        {
            if (string.IsNullOrEmpty(this.ItemPath))
            {
                if (this.ItemCol == null)
                {
                    this.Log.LogError("ItemCol or ItemPath must be defined");
                    return false;
                }

                foreach (ITaskItem i in this.ItemCol)
                {
                    this.itemspec += "\"" + i.ItemSpec + "\" ";
                }
            }
            else
            {
                this.itemspec = "\"" + this.ItemPath + "\"";
            }

            return true;
        }

        private void SwitchReturnValue(string error)
        {
            switch (this.returnValue)
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
            this.LogTaskMessage(MessageImportance.Low, "Resolve TF.exe path");

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
                this.tfexe = Path.Combine(vstools, @"..\IDE\tf.exe");
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "TF.exe path resolved to: {0}", this.tfexe));
            }

            if (!File.Exists(this.tfexe))
            {
                this.tfexe = "tf.exe";
                this.LogTaskMessage(MessageImportance.Low, "Unable to resolve TF.exe path. Assuming it is in the PATH environment variable.");
            }
        }
    }
}