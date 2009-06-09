//-----------------------------------------------------------------------
// <copyright file="Folder.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Management;
    using System.Security.AccessControl;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddSecurity</i> (<b>Required: </b> Path, Users <b>Optional: </b>AccessType)</para>
    /// <para><i>DeleteAll</i> (<b>Required: </b> Path, Match)</para>
    /// <para><i>Get</i> (<b>Required: </b> Path <b>Optional:</b> Match, Recursive)</para>
    /// <para><i>Move</i> (<b>Required: </b> Path, TargetPath)</para>
    /// <para><i>RemoveContent</i> (<b>Required: </b> Path <b>Optional: </b>Force)</para>
    /// <para><i>RemoveSecurity</i> (<b>Required: </b> Path, Users <b>Optional: </b>AccessType)</para>
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
    ///         <ItemGroup>
    ///             <Users Include="AReadUser">
    ///                 <Permission>ExecuteFile, Read</Permission>
    ///             </Users>
    ///             <Users Include="AChangeUser">
    ///                 <Permission>FullControl</Permission>
    ///             </Users>
    ///         </ItemGroup>
    ///         <!-- Add security for users -->
    ///         <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="AddSecurity" Path="c:\Demo2" Users="@(Users)"/>
    ///         <!-- Remove security for users -->
    ///         <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="RemoveSecurity" Path="c:\Demo2" Users="@(Users)"/>
    ///         <!-- Add Deny security for users -->
    ///         <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="AddSecurity" AccessType="Deny" Path="c:\Demo2" Users="@(Users)"/>
    ///         <!-- Remove Deny security for users -->
    ///         <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="RemoveSecurity" AccessType="Deny" Path="c:\Demo2" Users="@(Users)"/>
    ///         <!-- Delete all folders matching a given name -->
    ///         <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="DeleteAll" Path="c:\Demo2" Match="_svn"/>
    ///         <!-- Remove all content from a folder whilst maintaining the target folder -->
    ///         <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="RemoveContent" Path="c:\Demo"/>
    ///         <!-- Move a folder -->
    ///         <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="Move" Path="c:\Demo1" TargetPath="C:\adeeeee"/>
    ///         <!-- Lets copy a selection of folders to multiple locations -->
    ///         <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="Get" Path="c:\ddd">
    ///             <Output TaskParameter="Folders" ItemName="FoundFolders"/>
    ///         </MSBuild.ExtensionPack.FileSystem.Folder>
    ///         <Message Text="%(FoundFolders.Identity)"/>
    ///         <ItemGroup>
    ///             <MyWebService Include="C:\a\Dist\**\*.*">
    ///                 <ToDir>%(FoundFolders.Identity)</ToDir>
    ///             </MyWebService>
    ///         </ItemGroup>
    ///         <!-- Copy using the metadata -->
    ///         <Copy SourceFiles="@(MyWebService)" DestinationFolder="%(ToDir)\%(RecursiveDir)" />
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.3.0/html/c0f7dd21-7229-b08d-469c-9e02e66e974b.htm")]
    public class Folder : BaseTask
    {
        private const string AddSecurityTaskAction = "AddSecurity";
        private const string DeleteAllTaskAction = "DeleteAll";
        private const string GetTaskAction = "Get";
        private const string MoveTaskAction = "Move";
        private const string RemoveContentTaskAction = "RemoveContent";
        private const string RemoveSecurityTaskAction = "RemoveSecurity";
        private List<string> foldersFound;
        private AccessControlType accessType = AccessControlType.Allow;

        [DropdownValue(AddSecurityTaskAction)]
        [DropdownValue(DeleteAllTaskAction)]
        [DropdownValue(GetTaskAction)]
        [DropdownValue(MoveTaskAction)]
        [DropdownValue(RemoveContentTaskAction)]
        [DropdownValue(RemoveSecurityTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the path to remove content from, or the base path for Delete
        /// </summary>
        [Required]
        [TaskAction(AddSecurityTaskAction, true)]
        [TaskAction(DeleteAllTaskAction, true)]
        [TaskAction(GetTaskAction, true)]
        [TaskAction(MoveTaskAction, true)]
        [TaskAction(RemoveContentTaskAction, true)]
        [TaskAction(RemoveSecurityTaskAction, true)]
        public string Path { get; set; }

        /// <summary>
        /// Sets the regular expression to match in the name of a folder for Delete. Case is ignored.
        /// </summary>
        [TaskAction(DeleteAllTaskAction, true)]
        [TaskAction(GetTaskAction, false)]
        public string Match { get; set; }

        /// <summary>
        /// Sets the TargetPath for a renamed folder
        /// </summary>
        [TaskAction(MoveTaskAction, true)]
        public string TargetPath { get; set; }

        /// <summary>
        /// Sets a value indicating whether to delete readonly files when performing RemoveContent
        /// </summary>
        [TaskAction(RemoveContentTaskAction, false)]
        public bool Force { get; set; }

        /// <summary>
        /// Sets the users collection. Use the Permission metadata tag to specify permissions. Separate pemissions with a comma.
        /// <para/> <UsersCol Include="AUser">
        /// <para/>     <Permission>Read,etc</Permission>
        /// <para/> </UsersCol>
        /// </summary>
        [TaskAction(AddSecurityTaskAction, true)]
        [TaskAction(RemoveSecurityTaskAction, true)]
        public ITaskItem[] Users { get; set; }

        /// <summary>
        /// Set the AccessType. Can be Allow or Deny. Default is Allow.
        /// </summary>
        [TaskAction(AddSecurityTaskAction, false)]
        [TaskAction(RemoveSecurityTaskAction, false)]
        public string AccessType
        {
            get { return this.accessType.ToString(); }
            set { this.accessType = (AccessControlType)Enum.Parse(typeof(AccessControlType), value); }
        }

        /// <summary>
        /// Set to true to perform a recursive scan. Default is false.
        /// </summary>
        public bool Recursive { get; set; }

        /// <summary>
        /// Gets the folder list
        /// </summary>
        [Output]
        public ITaskItem[] Folders { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        /// <remarks>
        /// LogError should be thrown in the event of errors
        /// </remarks>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            DirectoryInfo dir = new DirectoryInfo(this.Path);
            if (!dir.Exists)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "The directory does not exist: {0}", this.Path));
                return;
            }

            switch (this.TaskAction)
            {
                case AddSecurityTaskAction:
                    this.SetSecurity("Add");
                    break;
                case RemoveSecurityTaskAction:
                    this.SetSecurity("Remove");
                    break;
                case RemoveContentTaskAction:
                    this.RemoveContent(dir);
                    break;
                case MoveTaskAction:
                    this.Move();
                    break;
                case DeleteAllTaskAction:
                    this.DeleteAll();
                    break;
                case GetTaskAction:
                    this.GetFolders();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private static void DelTree(DirectoryInfo root)
        {
            // Delete all files in current folder.
            foreach (FileInfo i in root.GetFiles())
            {
                // First make sure the file is writable.
                FileAttributes fileAttributes = System.IO.File.GetAttributes(i.FullName);

                // If readonly attribute is set, reset it.
                if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    System.IO.File.SetAttributes(i.FullName, fileAttributes ^ FileAttributes.ReadOnly);
                }

                System.IO.File.Delete(i.FullName);
            }

            foreach (DirectoryInfo d in root.GetDirectories())
            {
                DelTree(d);
                Directory.Delete(d.FullName);
            }
        }

        private void GetFolders()
        {
            if (string.IsNullOrEmpty(this.Path))
            {
                Log.LogError("Path must be specified.");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting Folders from: {0}", this.Path));
            DirectoryInfo dirInfo = new DirectoryInfo(this.Path);
            this.foldersFound = new List<string>();
            this.ProcessGetAll(dirInfo);
            this.Folders = new ITaskItem[this.foldersFound.Count];
            int i = 0;
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Folders Found: {0}", this.foldersFound.Count));
            foreach (string s in this.foldersFound)
            {
                ITaskItem newItem = new TaskItem(s);
                this.Folders[i] = newItem;
                i++;
            }
        }

        private void ProcessGetAll(DirectoryInfo dirInfo)
        {
            foreach (DirectoryInfo child in dirInfo.GetDirectories())
            {
                if (string.IsNullOrEmpty(this.Match))
                {
                    this.foldersFound.Add(child.FullName);
                }
                else
                {
                    // Load the regex to use
                    Regex reg = new Regex(this.Match, RegexOptions.IgnoreCase | RegexOptions.Compiled);

                    // Match the regular expression pattern against a text string.
                    Match m = reg.Match(child.Name);
                    if (m.Success)
                    {
                        this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Getting: {0}", child.FullName));
                        this.foldersFound.Add(child.FullName);
                    }
                }

                if (this.Recursive)
                {
                    this.ProcessGetAll(child);
                }
            }
        }

        private void SetSecurity(string action)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(this.Path);
            DirectorySecurity currentSecurity = dirInfo.GetAccessControl();

            if (this.Users != null)
            {
                foreach (ITaskItem user in this.Users)
                {
                    string userName = user.ItemSpec;
                    if (!userName.Contains(@"\"))
                    {
                        // default to local user
                        userName = Environment.MachineName + @"\" + userName;
                    }

                    FileSystemRights userRights = new FileSystemRights();
                    string[] permissions = user.GetMetadata("Permission").Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in permissions)
                    {
                        userRights |= (FileSystemRights)Enum.Parse(typeof(FileSystemRights), s);
                    }

                    if (action == "Add")
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding security for user: {0} on {1}", userName, this.Path));
                        currentSecurity.AddAccessRule(new FileSystemAccessRule(userName, userRights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, this.accessType));    
                    }
                    else
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing security for user: {0} on {1}", userName, this.Path));
                        currentSecurity.RemoveAccessRule(new FileSystemAccessRule(userName, userRights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, this.accessType));
                    }
                }
            }

            // Set the new access settings.
            dirInfo.SetAccessControl(currentSecurity);
        }

        private void DeleteAll()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing all Folders from: {0} that match: {1}", this.Path, this.Match));
            if (string.IsNullOrEmpty(this.Match))
            {
                Log.LogError("Match must be specified.");
                return;
            }
            
            DirectoryInfo d = new DirectoryInfo(this.Path);
            this.ProcessDeleteAll(d);
        }

        private void ProcessDeleteAll(DirectoryInfo dirInfo)
        {
            foreach (DirectoryInfo child in dirInfo.GetDirectories())
            {
                // Load the regex to use
                Regex reg = new Regex(this.Match, RegexOptions.IgnoreCase | RegexOptions.Compiled);

                // Match the regular expression pattern against a text string.
                Match m = reg.Match(child.Name);
                if (m.Success)
                {
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Removing: {0}", child.FullName));
                    DelTree(child);
                    Directory.Delete(child.FullName);
                }
                else
                {
                    this.ProcessDeleteAll(child);
                }
            }
        }

        private void RemoveContent(DirectoryInfo dir)
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing Content from Folder: {0}", dir.FullName));

            FileSystemInfo[] infos = dir.GetFileSystemInfos("*");
            foreach (FileSystemInfo i in infos)
            {
                // Check to see if this is a DirectoryInfo object.
                if (i is DirectoryInfo)
                {
                    if (this.Force)
                    {
                        // if its a folder path we can use WMI for a quick delete
                        if (i.FullName.Contains(@"\\") == false)
                        {
                            string dirObject = string.Format(CultureInfo.CurrentCulture, "win32_Directory.Name='{0}'", i.FullName);
                            using (ManagementObject mdir = new ManagementObject(dirObject))
                            {
                                mdir.Get();
                                ManagementBaseObject outParams = mdir.InvokeMethod("Delete", null, null);

                                // ReturnValue should be 0, else failure
                                if (outParams != null)
                                {
                                    if (Convert.ToInt32(outParams.Properties["ReturnValue"].Value, CultureInfo.CurrentCulture) != 0)
                                    {
                                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Directory deletion error: ReturnValue: {0}", outParams.Properties["ReturnValue"].Value));
                                        return;
                                    }
                                }
                                else
                                {
                                    this.Log.LogError("The ManagementObject call to invoke Delete returned null.");
                                    return;
                                }
                            }
                        }
                        else
                        {
                            // it's a share, so we need to manually check all file attributes and delete
                            DelTree((DirectoryInfo) i);
                            Directory.Delete(i.FullName, true);
                        }
                    }
                    else
                    {
                        Directory.Delete(i.FullName, true);
                    }
                }
                else if (i is FileInfo)
                {
                    if (this.Force)
                    {
                        // First make sure the file is writable.
                        FileAttributes fileAttributes = System.IO.File.GetAttributes(i.FullName);

                        // If readonly attribute is set, reset it.
                        if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            System.IO.File.SetAttributes(i.FullName, fileAttributes ^ FileAttributes.ReadOnly);
                        }
                    }

                    System.IO.File.Delete(i.FullName);
                }
            }
        }

        private void Move()
        {
            if (string.IsNullOrEmpty(this.TargetPath))
            {
                Log.LogError("TargetPath must be specified.");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Moving Folder: {0} to: {1}", this.Path, this.TargetPath));
            
            // If the TargetPath has multiple folders, then we need to create the parent
            DirectoryInfo f = new DirectoryInfo(this.TargetPath);
            string parentPath = this.TargetPath.Replace(@"\" + f.Name, string.Empty);
            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }

            Directory.Move(this.Path, this.TargetPath);
        }
    }
}