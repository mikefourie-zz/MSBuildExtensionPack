//-----------------------------------------------------------------------
// <copyright file="Folder.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Management;
    using System.Security.AccessControl;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddSecurity</i> (<b>Required: </b> Path, Users <b>Optional: </b>AccessType, Permission)</para>
    /// <para><i>DeleteAll</i> (<b>Required: </b> Path, Match)</para>
    /// <para><i>Get</i> (<b>Required: </b> Path <b>Optional:</b> Match, Recursive)</para>
    /// <para><i>Move</i> (<b>Required: </b> Path, TargetPath)</para>
    /// <para><i>RemoveContent</i> (<b>Required: </b> Path <b>Optional: </b>Force, RetryCount)</para>
    /// <para><i>RemoveSecurity</i> (<b>Required: </b> Path, Users <b>Optional: </b>AccessType)</para>
    /// <para><b>Remote Execution Support:</b> No</para>
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
    ///         <ItemGroup>
    ///             <Users Include="AReadUser">
    ///                 <Permission>ExecuteFile, Read</Permission>
    ///             </Users>
    ///             <Users Include="AChangeUser">
    ///                 <Permission>FullControl</Permission>
    ///             </Users>
    ///             <FoldersToPermission Include="c:\az">
    ///                 <Account>Performance Log Users</Account>
    ///                 <Permission>Read,Write,Modify,Delete</Permission>
    ///                 <AccessType>Allow</AccessType>
    ///             </FoldersToPermission>
    ///             <FoldersToPermission Include="c:\az">
    ///                 <Account>AChangeUser</Account>
    ///                 <Permission>Read,Write,Modify,Delete</Permission>
    ///                 <AccessType>Allow</AccessType>
    ///             </FoldersToPermission>
    ///             <FoldersToRemovePermissions Include="c:\az">
    ///                 <Account>Performance Log Users</Account>
    ///                 <Permission>Read,Write,Modify,Delete</Permission>
    ///             </FoldersToRemovePermissions>
    ///         </ItemGroup>
    ///         <Microsoft.Build.Tasks.MakeDir Directories="c:\Demo2;c:\Demo1;c:\ddd"/>
    ///         <Microsoft.Build.Tasks.RemoveDir Directories="C:\adeeeee"/>
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
    ///         <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="AddSecurity" AccessType="%(FoldersToPermission.AccessType)" Path="%(FoldersToPermission.Identity)" Users="%(FoldersToPermission.Account)" Permission="%(FoldersToPermission.Permission)"/>
    ///         <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="RemoveSecurity" AccessType="%(FoldersToRemovePermissions.AccessType)" Path="%(FoldersToRemovePermissions.Identity)" Users="%(FoldersToRemovePermissions.Account)" Permission="%(FoldersToRemovePermissions.Permission)"/>
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
        private int retryCount = 5;

        /// <summary>
        /// Sets the path to remove content from, or the base path for Delete
        /// </summary>
        [Required]
        public ITaskItem Path { get; set; }

        /// <summary>
        /// Sets the regular expression to match in the name of a folder for Delete. Case is ignored.
        /// </summary>
        public string Match { get; set; }

        /// <summary>
        /// Sets the TargetPath for a renamed folder
        /// </summary>
        public ITaskItem TargetPath { get; set; }

        /// <summary>
        /// Sets a value indicating whether to delete readonly files when performing RemoveContent
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Sets the users collection. Use the Permission metadata tag to specify permissions. Separate pemissions with a comma.
        /// <para/> <UsersCol Include="AUser">
        /// <para/>     <Permission>Read,etc</Permission>
        /// <para/> </UsersCol>
        /// </summary>
        public ITaskItem[] Users { get; set; }

        /// <summary>
        /// A comma-separated list of <a href="http://msdn.microsoft.com/en-us/library/942f991b.aspx">FileSystemRights</a>.
        /// </summary>
        public string Permission { get; set; }

        /// <summary>
        /// Set the AccessType. Can be Allow or Deny. Default is Allow.
        /// </summary>
        public string AccessType
        {
            get { return this.accessType.ToString(); }
            set { this.accessType = (AccessControlType)Enum.Parse(typeof(AccessControlType), value); }
        }

        /// <summary>
        /// Sets a value indicating how many times to retry removing the content, e.g. if files are temporarily locked. Default is 5. The retry occurs every 5 seconds.
        /// </summary>
        public int RetryCount
        {
            get { return this.retryCount; }
            set { this.retryCount = value; }
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

            DirectoryInfo dir = new DirectoryInfo(this.Path.GetMetadata("FullPath"));
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

        private void DelTree(DirectoryInfo root)
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
                
                try
                {
                    FileInfo f = new FileInfo(i.FullName);
                    if (f.Exists)
                    {
                        System.IO.File.Delete(i.FullName);
                    }
                }
                catch (Exception ex)
                {
                    this.LogTaskWarning(ex.Message);
                    bool deleted = false;
                    int count = 1;
                    while (!deleted && count <= this.RetryCount)
                    {
                        this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.InvariantCulture, "Delete failed, trying again in 5 seconds. Attempt {0} of {1}", count, this.RetryCount));
                        System.Threading.Thread.Sleep(5000);
                        count++;
                        try
                        {
                            FileInfo f = new FileInfo(i.FullName);
                            if (f.Exists)
                            {
                                System.IO.File.Delete(i.FullName);
                            }

                            deleted = true;
                        }
                        catch
                        {
                            this.LogTaskWarning(ex.Message);
                        }
                    }

                    if (deleted != true)
                    {
                        throw;
                    }
                }
            }

            foreach (DirectoryInfo d in root.GetDirectories())
            {
                this.DelTree(d);
                try
                {
                    Directory.Delete(d.FullName);
                }
                catch (Exception ex)
                {
                    this.LogTaskWarning(ex.Message);
                    bool deleted = false;
                    int count = 1;
                    while (!deleted && count <= this.RetryCount)
                    {
                        this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.InvariantCulture, "Delete failed, trying again in 5 seconds. Attempt {0} of {1}", count, this.RetryCount));
                        System.Threading.Thread.Sleep(5000);
                        count++;
                        try
                        {
                            Directory.Delete(d.FullName);
                            deleted = true;
                        }
                        catch
                        {
                            this.LogTaskWarning(ex.Message);
                        }
                    }

                    if (deleted != true)
                    {
                        throw;
                    }
                }
            }
        }

        private void GetFolders()
        {
            if (string.IsNullOrEmpty(this.Path.GetMetadata("FullPath")))
            {
                Log.LogError("Path must be specified.");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting Folders from: {0}", this.Path));
            DirectoryInfo dirInfo = new DirectoryInfo(this.Path.GetMetadata("FullPath"));
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
            DirectoryInfo dirInfo = new DirectoryInfo(this.Path.GetMetadata("FullPath"));
            DirectorySecurity currentSecurity = dirInfo.GetAccessControl();

            if (this.Users != null)
            {
                foreach (ITaskItem user in this.Users)
                {
                    string userName = user.ItemSpec;
                    string[] permissions = string.IsNullOrEmpty(this.Permission) ? user.GetMetadata("Permission").Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries) : this.Permission.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    FileSystemRights userRights = permissions.Aggregate(new FileSystemRights(), (current, s) => current | (FileSystemRights)Enum.Parse(typeof(FileSystemRights), s));

                    if (action == "Add")
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding security for user: {0} on {1}", userName, this.Path));
                        currentSecurity.AddAccessRule(new FileSystemAccessRule(userName, userRights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, this.accessType));    
                    }
                    else
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing security for user: {0} on {1}", userName, this.Path));
                        if (permissions.Length == 0)
                        {
                            currentSecurity.RemoveAccessRuleAll(new FileSystemAccessRule(userName, userRights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, this.accessType));
                        }
                        else
                        {
                            currentSecurity.RemoveAccessRule(new FileSystemAccessRule(userName, userRights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, this.accessType));
                        }
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

            DirectoryInfo d = new DirectoryInfo(this.Path.GetMetadata("FullPath"));
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
                    this.DelTree(child);
                    try
                    {
                        Directory.Delete(child.FullName);
                    }
                    catch (Exception ex)
                    {
                        this.LogTaskWarning(ex.Message);
                        bool deleted = false;
                        int count = 1;
                        while (!deleted && count <= this.RetryCount)
                        {
                            this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.InvariantCulture, "Delete failed, trying again in 5 seconds. Attempt {0} of {1}", count, this.RetryCount));
                            System.Threading.Thread.Sleep(5000);
                            count++;
                            try
                            {
                                Directory.Delete(child.FullName);
                                deleted = true;
                            }
                            catch
                            {
                                this.LogTaskWarning(ex.Message);
                            }
                        }

                        if (deleted != true)
                        {
                            throw;
                        }
                    }
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
                var info = i as DirectoryInfo;
                if (info != null)
                {
                    if (this.Force)
                    {
                        // if its a folder path we can use WMI for a quick delete
                        if (info.FullName.Contains(@"\\") == false)
                        {
                            string dirObject = string.Format(CultureInfo.CurrentCulture, "win32_Directory.Name='{0}'", info.FullName);
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
                            this.DelTree(info);
                            try
                            {
                                Directory.Delete(info.FullName, true);
                            }
                            catch (Exception ex)
                            {
                                this.LogTaskWarning(ex.Message);
                                bool deleted = false;
                                int count = 1;
                                while (!deleted && count <= this.RetryCount)
                                {
                                    this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.InvariantCulture, "Delete failed, trying again in 5 seconds. Attempt {0} of {1}", count, this.RetryCount));
                                    System.Threading.Thread.Sleep(5000);
                                    count++;
                                    try
                                    {
                                        if (Directory.Exists(info.FullName))
                                        {
                                            Directory.Delete(info.FullName, true);
                                        }

                                        deleted = true;
                                    }
                                    catch
                                    {
                                        this.LogTaskWarning(ex.Message);
                                    }
                                }

                                if (deleted != true)
                                {
                                    throw;
                                }
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            Directory.Delete(info.FullName, true);
                        }
                        catch (Exception ex)
                        {
                            this.LogTaskWarning(ex.Message);
                            bool deleted = false;
                            int count = 1;
                            while (!deleted && count <= this.RetryCount)
                            {
                                this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.InvariantCulture, "Delete failed, trying again in 5 seconds. Attempt {0} of {1}", count, this.RetryCount));
                                System.Threading.Thread.Sleep(5000);
                                count++;
                                try
                                {
                                    if (Directory.Exists(info.FullName))
                                    {
                                        Directory.Delete(info.FullName, true);
                                    }

                                    deleted = true;
                                }
                                catch
                                {
                                    this.LogTaskWarning(ex.Message);
                                }
                            }

                            if (deleted != true)
                            {
                                throw;
                            }
                        }
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

                    try
                    {
                        if (i.Exists)
                        {
                            System.IO.File.Delete(i.FullName);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.LogTaskWarning(ex.Message);
                        bool deleted = false;
                        int count = 1;
                        while (!deleted && count <= this.RetryCount)
                        {
                            this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.InvariantCulture, "Delete failed, trying again in 5 seconds. Attempt {0} of {1}", count, this.RetryCount));
                            System.Threading.Thread.Sleep(5000);
                            count++;
                            try
                            {
                                if (i.Exists)
                                {
                                    System.IO.File.Delete(i.FullName);
                                }

                                deleted = true;
                            }
                            catch
                            {
                                this.LogTaskWarning(ex.Message);
                            }
                        }

                        if (deleted != true)
                        {
                            throw;
                        }
                    }
                }
            }
        }

        private void Move()
        {
            if (string.IsNullOrEmpty(this.TargetPath.GetMetadata("FullPath")))
            {
                Log.LogError("TargetPath must be specified.");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Moving Folder: {0} to: {1}", this.Path, this.TargetPath));
            
            // If the TargetPath has multiple folders, then we need to create the parent
            DirectoryInfo f = new DirectoryInfo(this.TargetPath.GetMetadata("FullPath"));
            if (f.Parent != null && !f.Parent.Exists)
            {
                Directory.CreateDirectory(f.Parent.FullName);
            }

            Directory.Move(this.Path.GetMetadata("FullPath"), this.TargetPath.GetMetadata("FullPath"));
        }
    }
}