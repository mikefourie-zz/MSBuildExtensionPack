//-----------------------------------------------------------------------
// <copyright file="TfsSource.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.VisualStudio
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// AutoArg enumeration
    /// </summary>
    public enum AutoArg
    {
        /// <summary>
        /// AcceptMerge
        /// </summary>
        AcceptMerge,

        /// <summary>
        /// AcceptTheirs
        /// </summary>
        AcceptTheirs,

        /// <summary>
        /// AcceptYours
        /// </summary>
        AcceptYours,

        /// <summary>
        /// OverwriteLocal
        /// </summary>
        OverwriteLocal,

        /// <summary>
        /// DeleteConflict
        /// </summary>
        DeleteConflict,

        /// <summary>
        /// AcceptYoursRenameTheirs
        /// </summary>
        AcceptYoursRenameTheirs
    }

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Add</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Login, Server, Version, WorkingDirectory, Recursive <b>Output:</b> ExitCode)</para>
    /// <para><i>AddLabel</i> (<b>Required: </b>LabelName, ItemPath or ItemCol <b>Optional: </b>Login, Server, LabelScope, Recursive, VersionSpec, Comments<b>Output:</b> ExitCode)</para>
    /// <para><i>Checkin</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Login, Server, Comments, Notes, Version, WorkingDirectory, Recursive, Bypass <b>Output:</b> ExitCode)</para>
    /// <para><i>Checkout</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Login, Server, Version, WorkingDirectory, Recursive <b>Output:</b> ExitCode)</para>
    /// <para><i>Delete</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Login, Server, Version, WorkingDirectory, Recursive <b>Output:</b> ExitCode)</para>
    /// <para><i>DeleteLabel</i> (<b>Required: </b>LabelName<b>Optional: </b>Login, Server, LabelScope<b>Output:</b> ExitCode)</para>
    /// <para><i>Get</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Login, Server, Version, WorkingDirectory, Recursive, Force, Overwrite, All <b>Output:</b> ExitCode)</para>
    /// <para><i>GetChangeset</i> (<b>Required: </b>VersionSpec <b>Optional: </b>Login, Server, WorkingDirectory <b>Output:</b> ExitCode, Changeset)</para>
    /// <para><i>GetWorkingChangeset</i> (<b>Required: </b>ItemPath <b>Optional: </b>Login, Server, WorkingDirectory, Recursive <b>Output:</b> ExitCode, Changeset)</para>
    /// <para><i>Merge</i> (<b>Required: </b>ItemPath, Destination <b>Optional: </b>Login, Server, Recursive, VersionSpec, Version, Baseless, Force <b>Output:</b> ExitCode)</para>
    /// <para><i>Resolve</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Login, Server, Recursive, Version, Auto, NewName)</para>
    /// <para><i>GetPendingChanges</i> (<b>Required: </b>ItemPath <b>Optional: </b>Login, Server, Recursive, Version, User <b>Output: </b>PendingChanges, PendingChangesExist <b>Output:</b> ExitCode, PendingChangesExistItem)</para>
    /// <para><i>UndoCheckout</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Login, Server, Version, WorkingDirectory, Recursive <b>Output:</b> ExitCode)</para>
    /// <para><i>Undelete</i> (<b>Required: </b>ItemPath or ItemCol <b>Optional: </b>Login, Server, Version, WorkingDirectory, Recursive <b>Output:</b> ExitCode)</para>
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
    ///     <ItemGroup>
    ///         <FilesToAdd Include="C:\Projects\SpeedCMMI\Demo1\*"/>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Check for pending changes -->
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="GetPendingChanges" ItemPath="$/AProject/APath" WorkingDirectory="C:\Projects\SpeedCMMI" User="$USERNAME">
    ///             <Output TaskParameter="PendingChanges" PropertyName="PendingChangesText" />
    ///             <Output TaskParameter="PendingChangesExist" PropertyName="DoChangesExist" />
    ///         </MSBuild.ExtensionPack.VisualStudio.TfsSource>
    ///         <Message Text="Pending Changes Report: $(PendingChangesText)"/>
    ///         <Message Text="Pending Changes Exist: $(DoChangesExist)"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="GetPendingChanges" ItemPath="$/AProject/ADifferentPath" WorkingDirectory="C:\Projects\SpeedCMMI">
    ///             <Output TaskParameter="PendingChangesExistItem" ItemName="PendingChangesExistItem3" />
    ///         </MSBuild.ExtensionPack.VisualStudio.TfsSource>
    ///         <!-- Get a summary of whether changes exist using the PendingChangesExistItem -->
    ///         <Message Text="%(PendingChangesExistItem3.Identity) = %(PendingChangesExistItem3.PendingChangesExist)"/>
    ///         <!-- Perfrom various other source control operations -->
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Checkout" ItemPath="C:\projects\SpeedCMMI\Demo1" Version="2008" WorkingDirectory="C:\projects\SpeedCMMI"/>
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Checkin" ItemPath="C:\projects\SpeedCMMI\Demo1" WorkingDirectory="C:\projects\SpeedCMMI" Comments="Testing" Notes="Code reviewer=buildrobot" OverrideText="Justdoit" />
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
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsSource TaskAction="Checkin" ItemPath="C:\projects\SpeedCMMI\Client" WorkingDirectory="C:\projects\SpeedCMMI" Comments="Testing" Notes="Code reviewer=buildrobot" OverrideText="Justdoit" />
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class TfsSource : BaseTask
    {
        private const string AddTaskAction = "Add";
        private const string AddLabelTaskAction = "AddLabel";
        private const string CheckinTaskAction = "Checkin";
        private const string CheckoutTaskAction = "Checkout";
        private const string DeleteTaskAction = "Delete";
        private const string DeleteLabelTaskAction = "DeleteLabel";
        private const string GetTaskAction = "Get";
        private const string MergeTaskAction = "Merge";
        private const string GetPendingChangesTaskAction = "GetPendingChanges";
        private const string ResolveTaskAction = "Resolve";
        private const string GetChangesetTaskAction = "GetChangeset";
        private const string GetWorkingChangesetTaskAction = "GetWorkingChangeset";
        private const string UndoCheckoutTaskAction = "UndoCheckout";
        private const string UndeleteTaskAction = "Undelete";
        private string teamFoundationExe;
        private string version = "2013";
        private bool recursive = true;
        private ShellWrapper shellWrapper;
        private string itemSpec = string.Empty;
        private string returnOutput;

        /// <summary>
        /// Sets the version spec for Get or changeset number for GetChangeset. If no VersionSpec is provided for GetChangeset, then /latest is used.
        /// </summary>
        public string VersionSpec { get; set; }

        /// <summary>
        /// Resolves outstanding conflicts between different versions of specified items in the current workspace 
        /// AcceptMerge, AcceptTheirs, AcceptYours, OverwriteLocal, DeleteConflict, AcceptYoursRenameTheirs 
        /// </summary>
        public string Auto { get; set; }

        /// <summary>
        /// Used to resolve a name collision conflict. Can only be used in conjunction with AcceptMerge and AcceptYoursRenameTheirs. With AcceptMerge, /newname is only valid with conflicts that involve rename and/or undelete. If used, you must supply a new path.
        /// </summary>
        public string NewName { get; set; }

        /// <summary>
        /// Forces all files to be retrieved, not just those that are out-of-date.
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Bypasses a gated check-in requirement. Default is false.
        /// </summary>
        public bool Bypass { get; set; }

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
        /// Sets the version of Tfs. Default is 2013
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
        /// Sets the Label Name.
        /// </summary>
        public string LabelName { get; set; }

        /// <summary>
        /// Sets the Label Scope
        /// </summary>
        public string LabelScope { get; set; }

        /// <summary>
        /// Sets the notes.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Sets the Login. TFS2010 and greater only.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Set the user
        /// </summary>
        public string User { get; set; }

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
        /// Gets the Changeset details
        /// </summary>
        [Output]
        public string Changeset { get; set; }

        /// <summary>
        /// Lets you set text to override check-in policies
        /// </summary>
        public string OverrideText { get; set; }

        /// <summary>
        /// Gets the ExitCode
        /// </summary>
        [Output]
        public int ExitCode { get; set; }

        /// <summary>
        /// Task Item stores whether changes exist for the given ItemPath. Identity stores the path, PendingChangesExist metadata stores boolean.
        /// </summary>
        [Output]
        public ITaskItem[] PendingChangesExistItem { get; set; }

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
                case AddTaskAction:
                    this.Add();
                    break;
                case CheckinTaskAction:
                    this.Checkin();
                    break;
                case CheckoutTaskAction:
                    this.Checkout();
                    break;
                case GetTaskAction:
                    this.GetFiles();
                    break;
                case GetPendingChangesTaskAction:
                    this.GetPendingChanges();
                    break;
                case GetWorkingChangesetTaskAction:
                    this.GetWorkingChangesetDetails();
                    break;
                case GetChangesetTaskAction:
                    this.GetChangesetDetails();
                    break;
                case DeleteTaskAction:
                    this.Delete();
                    break;
                case AddLabelTaskAction:
                    this.AddLabel();
                    break;
                case DeleteLabelTaskAction:
                    this.DeleteLabel();
                    break;
                case MergeTaskAction:
                    this.Merge();
                    break;
                case ResolveTaskAction:
                    this.Resolve();
                    break;
                case UndoCheckoutTaskAction:
                    this.UndoCheckout();
                    break;
                case UndeleteTaskAction:
                    this.Undelete();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void GetWorkingChangesetDetails()
        {
            this.ExecuteCommand("history", string.IsNullOrEmpty(this.ItemPath) ? "." : this.ItemPath, "/recursive /noprompt /stopafter:1 /version:W");

            if (this.returnOutput.StartsWith("Changeset", StringComparison.OrdinalIgnoreCase))
            {
                this.Changeset = this.returnOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[2].Split(new[] { "\t", " " }, StringSplitOptions.RemoveEmptyEntries)[0];
            }
        }

        private void GetChangesetDetails()
        {
            if (string.IsNullOrEmpty(this.VersionSpec))
            {
                this.ExecuteCommand("changeset", string.Empty, "/latest /noprompt");
            }
            else
            {
                this.ExecuteCommand("changeset", this.VersionSpec, string.Empty);
            }

            if (this.returnOutput.StartsWith("Changeset:", StringComparison.OrdinalIgnoreCase))
            {
                this.Changeset = this.returnOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[0].Replace("Changeset:", string.Empty).Trim();
            }
        }

        private void GetPendingChanges()
        {
            string user = "*";
            if (!string.IsNullOrEmpty(this.User))
            {
                user = this.User;
            }

            this.ExecuteCommand("status", string.Format(CultureInfo.CurrentCulture, "/user:\"{0}\"", user), "/Format:detailed /recursive");
            this.PendingChanges = this.returnOutput;
            this.PendingChangesExistItem = new TaskItem[1];
            ITaskItem t = new TaskItem(this.ItemPath);
            if (this.returnOutput.IndexOf("There are no pending changes", StringComparison.OrdinalIgnoreCase) < 0)
            {
                this.PendingChangesExist = true;
                t.SetMetadata("PendingChangesExist", "true");
            }
            else
            {
                t.SetMetadata("PendingChangesExist", "false");
            }

            this.PendingChangesExistItem[0] = t;
        }

        private void AddLabel()
        {
            if (string.IsNullOrEmpty(this.LabelName))
            {
                this.Log.LogError("LabelName is required for AddLabel");
                return;
            }

            string args = string.Empty;

            if (!string.IsNullOrEmpty(this.LabelScope))
            {
                this.LabelName += "@" + this.LabelScope;
            }

            if (!string.IsNullOrEmpty(this.VersionSpec))
            {
                args += " /version:" + "\"" + this.VersionSpec + "\"";
            }

            if (!string.IsNullOrEmpty(this.Comments))
            {
                args += string.Format(CultureInfo.CurrentCulture, "/comment:\"{0}\"", this.Comments);
            }

            this.ExecuteCommand("label " + this.LabelName, args, "/noprompt /recursive");
        }

        private void DeleteLabel()
        {
            if (string.IsNullOrEmpty(this.LabelName))
            {
                this.Log.LogError("LabelName is required for DeleteLabel");
                return;
            }

            string args = string.Empty;

            if (!string.IsNullOrEmpty(this.LabelScope))
            {
                this.LabelName += "@" + this.LabelScope;
            }

            if (!string.IsNullOrEmpty(this.VersionSpec))
            {
                args += " /version:" + "\"" + this.VersionSpec + "\"";
            }

            this.ExecuteCommand("label /delete " + this.LabelName, args, "/noprompt");
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
                this.Log.LogError("ItemPath is required for Merge");
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
                args += " \"" + this.Destination + "\" ";
            }
            else
            {
                this.Log.LogError("Destination is required for Merge");
                return;
            }

            this.ExecuteCommand("merge", args, "/noprompt /recursive");
        }

        private void Resolve()
        {
            StringBuilder args = new StringBuilder();
            if (!string.IsNullOrEmpty(this.Auto))
            {
                AutoArg auto;
                try
                {
                    auto = (AutoArg)Enum.Parse(typeof(AutoArg), this.Auto, true);
                }
                catch (ArgumentException)
                {
                    this.Log.LogError("Auto is restricted to these values: AcceptMerge, AcceptTheirs, AcceptYours, OverwriteLocal, DeleteConflict, AcceptYoursRenameTheirs");
                    return;
                }

                args.AppendFormat(" /auto:{0}", auto);
                if ((auto == AutoArg.AcceptMerge) || (auto == AutoArg.AcceptYoursRenameTheirs))
                {
                    if (string.IsNullOrEmpty(this.NewName))
                    {
                        this.Log.LogError("ItemPath is required for Merge");
                        return;
                    }

                    args.AppendFormat(" /newname:\"{0}\"", this.NewName);
                }
            }

            this.ExecuteCommand("resolve", args.ToString(), " /recursive");
        }

        private void Checkin()
        {
            string comment = string.Empty;
            if (!string.IsNullOrEmpty(this.Comments))
            {
                comment = string.Format(CultureInfo.CurrentCulture, "/comment:\"{0}\"", this.Comments);
            }

            string note = string.Empty;
            if (!string.IsNullOrEmpty(this.Notes))
            {
                note = string.Format(CultureInfo.CurrentCulture, "/notes:\"{0}\"", this.Notes);
            }

            this.ExecuteCommand("checkin", string.Format(CultureInfo.CurrentCulture, "{0} {1}", comment, note), "/noprompt /recursive");
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
            this.itemSpec = string.Empty;
            if ((this.TaskAction != GetChangesetTaskAction) && (this.TaskAction != GetWorkingChangesetTaskAction) && (this.TaskAction != DeleteLabelTaskAction) && !this.DetermineItemSpec())
            {
                return;
            }

            string arguments = string.Format(CultureInfo.CurrentCulture, "{0} {1} {2}", action, this.itemSpec, options);
            if (string.IsNullOrEmpty(this.OverrideText) == false)
            {
                arguments += " /override:\"" + this.OverrideText + "\"";
            }

            if (this.Bypass)
            {
                arguments += " /bypass";
            }

            if (string.IsNullOrEmpty(this.Server) == false)
            {
                arguments += " /s:" + this.Server;
            }

            if (string.IsNullOrEmpty(this.Login) == false)
            {
                arguments += " /login:" + this.Login;
            }

            if (!this.Recursive)
            {
                lastOptions = lastOptions.Replace("/recursive", string.Empty);
            }

            arguments += " " + lastOptions;

            this.shellWrapper = new ShellWrapper(this.teamFoundationExe, arguments);
            if (string.IsNullOrEmpty(this.WorkingDirectory) == false)
            {
                this.shellWrapper.WorkingDirectory = this.WorkingDirectory;
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "WorkingDirectory set to: {0}", this.WorkingDirectory));
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Executing {0} {1}", this.shellWrapper.Executable, this.shellWrapper.Arguments));
            this.ExitCode = this.shellWrapper.Execute();
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
                    this.itemSpec += "\"" + i.ItemSpec + "\" ";
                }
            }
            else
            {
                this.itemSpec = "\"" + this.ItemPath + "\"";
            }

            return true;
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
            this.LogTaskMessage(MessageImportance.Low, "Resolve TF.exe path");

            string vstools = string.Empty;
            switch (this.Version)
            {
                case "2015":
                    vstools = Environment.GetEnvironmentVariable("VS140COMNTOOLS");
                    break;
                case "2013":
                    vstools = Environment.GetEnvironmentVariable("VS120COMNTOOLS");
                    break;
                case "2012":
                    vstools = Environment.GetEnvironmentVariable("VS110COMNTOOLS");
                    break;
                case "2010":
                    vstools = Environment.GetEnvironmentVariable("VS100COMNTOOLS");
                    break;
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
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "TF.exe path resolved to: {0}", this.teamFoundationExe));
            }

            if (!File.Exists(this.teamFoundationExe))
            {
                this.teamFoundationExe = "tf.exe";
                this.LogTaskMessage(MessageImportance.Low, "Unable to resolve TF.exe path. Assuming it is in the PATH environment variable.");
            }
        }
    }
}