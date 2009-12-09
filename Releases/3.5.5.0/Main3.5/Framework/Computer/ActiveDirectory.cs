//-----------------------------------------------------------------------
// <copyright file="ActiveDirectory.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------

namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Globalization;
    using Microsoft.Build.Framework;
    using MSBuild.ExtensionPack.Computer.Extended;

    internal enum ADGroupType
    {
        /// <summary>
        /// Global
        /// </summary>
        Global = 0x00000002,
        
        /// <summary>
        /// Local
        /// </summary>
        Local = 0x00000004,
        
        /// <summary>
        /// Universal
        /// </summary>
        Universal = 0x00000008
    }

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddUser</i> (<b>Required: </b> User <b>Optional: </b>Domain, FullName, Description, Password, PasswordExpired, PasswordNeverExpires)</para>
    /// <para><i>AddGroup</i> (<b>Required: </b> Group <b>Optional: </b>Domain, Description, GroupType)</para>
    /// <para><i>AddUserToGroup</i> (<b>Required: </b> User, Group)</para>
    /// <para><i>CheckUserExists</i> (<b>Required: </b> User <b>Output:</b> Exists)</para>
    /// <para><i>CheckUserPassword</i> (<b>Required: </b> User, Password <b>Optional:</b> BindingContextOptions, ContextTypeStore, Domain <b>Output:</b> Exists)</para>
    /// <para><i>CheckGroupExists</i> (<b>Required: </b> Group <b>Output:</b> Exists)</para>
    /// <para><i>DeleteUser</i> (<b>Required: </b> User)</para>
    /// <para><i>DeleteGroup</i> (<b>Required: </b> Group)</para>
    /// <para><i>DeleteUserFromGroup</i> (<b>Required: </b> User, Group)</para>
    /// <para><i>GetUserPassword</i> (<b>Required: </b>User  <b>Optional: </b>BindingContextOptions, ContextTypeStore, Domain <b>Output:</b> Password)</para>
    /// <para><b>Remote Execution Support:</b> Yes</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///   </PropertyGroup>
    ///   <Import Project="$(TPath)"/>
    ///   <Target Name="Default">
    ///     <!-- Check a user Exists -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="CheckUserExists" User="JudgeJS1">
    ///       <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///     </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///     <Message Text="JudgeJS1 Exists: $(DoesExist)"/>
    ///     <!-- Add local Users -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddUser" User="JudgeJS1" Description="Elgnt" Password="123546fdfdRERF$" PasswordNeverExpires="true"/>
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddUser" User="JudgeJS2" Description="Elgnt" PasswordNeverExpires="true"/>
    ///     <!-- Check a user Exists -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="CheckUserExists" User="JudgeJS1">
    ///       <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///     </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///     <Message Text="JudgeJS1 Exists: $(DoesExist)"/>
    ///     <!-- Check a Group Exists -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="CheckGroupExists" User="NewGroup1">
    ///       <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///     </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///     <Message Text="NewGroup1 Exists: $(DoesExist)"/>
    ///     <!-- Add local Groups -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroup" Group="NewGroup1" Description="Elgnt"/>
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroup" Group="NewGroup2" Description="Elgnt"/>
    ///     <!-- Check a Group Exists -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="CheckGroupExists" User="NewGroup1">
    ///       <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///     </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///     <Message Text="NewGroup1 Exists: $(DoesExist)"/>
    ///     <!-- Add the users to the Groups -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddUserToGroup" User="JudgeJS1;JudgeJS2" Group="NewGroup1;NewGroup2"/>
    ///     <!-- Delete Users from Groups -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="DeleteUserFromGroup" User="JudgeJS1" Group="NewGroup1;NewGroup2"/>
    ///     <!-- Delete local Users -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="DeleteUser" User="JudgeJS1;JudgeJS2"/>
    ///     <!-- Delete local Groups -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="DeleteGroup" Group="NewGroup1;NewGroup2"/>
    ///     <!-- Add a remote User -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddUser" User="JudgeJS1" MachineName="D420-7" Description="Elgnt" Password="123546fdfdRERF$" PasswordNeverExpires="true"/>
    ///     <!-- Add a remote Group -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroup" Group="RemoteGroup1" MachineName="D420-7" Description="na"/>
    ///     <!-- Add a Domain User -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddUser" User="JudgeJS1" Domain="mydomain" Description="Elgnt" Password="123546fdfdRERF$" PasswordNeverExpires="true"/>
    ///     <!-- Add a Domain Group -->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroup" Group="DomainGroup1" Domain="mydomain" Description="na"/>
    ///     <!-- Get a user's password-->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="GetUserPassword" User="Michael" ContextTypeStore="Machine">
    ///       <Output TaskParameter="Password" PropertyName="Pass"/>
    ///     </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///     <Message Text="User Password: $(Pass)"/>
    ///     <!-- Check a user's password-->
    ///     <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="CheckUserPassword" User="Michael" ContextTypeStore="Machine" Password="$(Pass)">
    ///       <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///     </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///     <Message Text="User Exists: $(DoesExist)"/>
    ///   </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.5.0/html/ad44953a-08cd-5898-fa63-efb8495d2a92.htm")]
    public class ActiveDirectory : BaseTask
    {
        private const string AddUserTaskAction = "AddUser";
        private const string AddGroupTaskAction = "AddGroup";
        private const string AddUserToGroupTaskAction = "AddUserToGroup";
        private const string CheckUserExistsTaskAction = "CheckUserExists";
        private const string CheckUserPasswordTaskAction = "CheckUserPassword";
        private const string CheckGroupExistsTaskAction = "CheckGroupExists";
        private const string GetUserPasswordTaskAction = "GetUserPassword";
        private const string DeleteUserTaskAction = "DeleteUser";
        private const string DeleteGroupTaskAction = "DeleteGroup";
        private const string DeleteUserFromGroupTaskAction = "DeleteUserFromGroup";
        private string target;
        private string domain;
        private int passwordExpired;
        private DirectoryEntry activeDirEntry;
        private ADGroupType groupType;
        private ContextOptions bindingContextOptions = ContextOptions.Negotiate;
        private ContextType contextType = ContextType.Domain;
        
        [DropdownValue(AddUserTaskAction)]
        [DropdownValue(AddGroupTaskAction)]
        [DropdownValue(AddUserToGroupTaskAction)]
        [DropdownValue(CheckUserExistsTaskAction)]
        [DropdownValue(CheckUserPasswordTaskAction)]
        [DropdownValue(CheckGroupExistsTaskAction)]
        [DropdownValue(GetUserPasswordTaskAction)]
        [DropdownValue(DeleteUserTaskAction)]
        [DropdownValue(DeleteGroupTaskAction)]
        [DropdownValue(DeleteUserFromGroupTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        [TaskAction(AddUserTaskAction, false)]
        [TaskAction(AddGroupTaskAction, false)]
        [TaskAction(AddUserToGroupTaskAction, false)]
        [TaskAction(CheckUserExistsTaskAction, false)]
        [TaskAction(CheckGroupExistsTaskAction, false)]
        [TaskAction(DeleteUserTaskAction, false)]
        [TaskAction(DeleteGroupTaskAction, false)]
        [TaskAction(DeleteUserFromGroupTaskAction, false)]
        public override string MachineName
        {
            get { return base.MachineName; }
            set { base.MachineName = value; }
        }

        /// <summary>
        /// Sets the User name
        /// </summary>
        [TaskAction(AddUserTaskAction, true)]
        [TaskAction(AddUserToGroupTaskAction, true)]
        [TaskAction(CheckUserPasswordTaskAction, true)]
        [TaskAction(DeleteUserTaskAction, true)]
        [TaskAction(DeleteUserFromGroupTaskAction, true)]
        [TaskAction(GetUserPasswordTaskAction, true)]
        public ITaskItem[] User { get; set; }

        /// <summary>
        /// Sets the Group name
        /// </summary>
        [TaskAction(AddUserToGroupTaskAction, true)]
        [TaskAction(DeleteGroupTaskAction, true)]
        [TaskAction(DeleteUserFromGroupTaskAction, true)]
        public ITaskItem[] Group { get; set; }

        /// <summary>
        /// Sets the User's full name
        /// </summary>
        [TaskAction(AddUserTaskAction, false)]
        [TaskAction(AddGroupTaskAction, false)]
        public string FullName { get; set; }

        /// <summary>
        /// Sets the User's or Group's description
        /// </summary>
        [TaskAction(AddUserTaskAction, false)]
        [TaskAction(AddGroupTaskAction, false)]
        public string Description { get; set; }

        /// <summary>
        /// Sets the User's password
        /// </summary>
        [TaskAction(AddUserTaskAction, false)]
        [TaskAction(CheckUserPasswordTaskAction, true)]
        [Output]
        public string Password { get; set; }

        /// <summary>
        /// Sets the User's password to expired. Default is false
        /// </summary>
        [TaskAction(AddUserTaskAction, false)]
        public bool PasswordExpired
        {
            get { return this.passwordExpired == 1 ? true : false; }
            set { this.passwordExpired = value ? 1 : 0; }
        }

        /// <summary>
        /// Sets the User's password to never expire. Default is false
        /// </summary>
        [TaskAction(AddUserTaskAction, false)]
        public bool PasswordNeverExpires { get; set; }

        /// <summary>
        /// Sets the domain to operate against.
        /// </summary>
        [TaskAction(AddUserTaskAction, false)]
        [TaskAction(AddGroupTaskAction, false)]
        [TaskAction(GetUserPasswordTaskAction, false)]
        public string Domain
        {
            get { return this.domain; }
            set { this.domain = value; }
        }

        /// <summary>
        /// Sets the GroupType. For non domains the default is Local. For Domains the default is Global. Supports Global, Local, Universal
        /// </summary>
        [TaskAction(AddGroupTaskAction, false)]
        public string GroupType
        {
            get { return this.groupType.ToString(); }
            set { this.groupType = (ADGroupType)Enum.Parse(typeof(ADGroupType), value); }
        }

        /// <summary>
        /// Specifies the store to use. Supports Machine and Domain. Default is Domain.
        /// </summary>
        [TaskAction(CheckUserPasswordTaskAction, false)]
        [TaskAction(GetUserPasswordTaskAction, false)]
        public string ContextTypeStore
        {
            get { return this.contextType.ToString(); }
            set { this.contextType = (ContextType)Enum.Parse(typeof(ContextType), value); }
        }

        /// <summary>
        /// Specifies the options that are used for binding to the server. Default is Negotiate
        /// </summary>
        [TaskAction(CheckUserPasswordTaskAction, false)]
        [TaskAction(GetUserPasswordTaskAction, false)]
        public ITaskItem[] BindingContextOptions
        {
            set { this.bindingContextOptions = SetBindingOptions(value); }
        }

        /// <summary>
        /// Gets whether the User or Group exists
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            string path;
            if (string.Compare(this.Domain, this.MachineName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                this.Domain = string.Empty;    
            }

            if (string.IsNullOrEmpty(this.Domain))
            {
                // Connect to a computer
                path = "WinNT://" + this.MachineName + ",computer";
                this.groupType = ADGroupType.Local;
                this.target = this.MachineName;
            }
            else
            {
                // connect to a domain
                path = "WinNT://" + this.Domain + ",domain";
                if (this.GroupType == "0")
                {
                    this.groupType = ADGroupType.Global;
                }

                this.target = this.domain;
            }

            using (this.activeDirEntry = new DirectoryEntry(path))
            {
                switch (this.TaskAction)
                {
                    case AddUserTaskAction:
                        this.AddUser();
                        break;
                    case DeleteUserTaskAction:
                        this.DeleteEntity("User", this.User);
                        break;
                    case AddGroupTaskAction:
                        this.AddGroup();
                        break;
                    case DeleteGroupTaskAction:
                        this.DeleteEntity("Group", this.Group);
                        break;
                    case AddUserToGroupTaskAction:
                        this.AddUserToGroup();
                        break;
                    case DeleteUserFromGroupTaskAction:
                        this.DeleteUserFromGroup();
                        break;
                    case CheckUserExistsTaskAction:
                        this.CheckExists("User");
                        break;
                    case CheckUserPasswordTaskAction:
                        this.CheckUserPassword();
                        break;
                    case GetUserPasswordTaskAction:
                        this.GetUserPassword();
                        break;
                    case CheckGroupExistsTaskAction:
                        this.CheckExists("group");
                        break;
                    default:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                        return;
                }
            }
        }

        private static ContextOptions SetBindingOptions(IEnumerable<ITaskItem> value)
        {
            ContextOptions s = new ContextOptions();
            foreach (ITaskItem option in value)
            {
                s |= (ContextOptions)Enum.Parse(typeof(ContextOptions), option.ItemSpec);
            }

            return s;
        }

        private static bool IsMember(DirectoryEntry entity, string name)
        {
            object groups = entity.Invoke("Groups");
            foreach (object group in (IEnumerable)groups)
            {
                using (DirectoryEntry groupEntry = new DirectoryEntry(group))
                {
                    if (groupEntry.Name == name)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void InvokeObj(DirectoryEntry dirE, string obj, string value)
        {
            try
            {
                dirE.Invoke("Put", new object[] { obj, value });
            }
            catch
            {
                // ignore exceptions on invoke
            }
        }

        private static void InvokeObj(DirectoryEntry dirE, string obj, int value)
        {
            try
            {
                dirE.Invoke("Put", new object[] { obj, value });
            }
            catch
            {
                // ignore exceptions on invoke
            }
        }

        private void GetUserPassword()
        {
            using (GetPasswordForm form = new GetPasswordForm(this.User[0].ItemSpec, this.Domain, this.contextType, this.bindingContextOptions))
            {
                form.ShowDialog();
                this.Password = form.Password;
                if (form.Exception != null)
                {
                    this.Log.LogErrorFromException(form.Exception, this.LogExceptionStack, true, null);
                }
            }
        }

        private void CheckUserPassword()
        {
            if (this.User == null)
            {
                Log.LogError("User is required");
                return;
            }

            if (string.IsNullOrEmpty(this.Password))
            {
                Log.LogError("Password is required");
                return;
            }
            
            try
            {
                PrincipalContext pcontext = new PrincipalContext(this.contextType, this.Domain);
                using (pcontext)
                {
                    this.Exists = pcontext.ValidateCredentials(this.User[0].ItemSpec, this.Password, this.bindingContextOptions);
                }
            }
            catch (Exception e)
            {
                this.LogTaskMessage(e.ToString());
                this.Exists = false;
            }
        }

        private void CheckExists(string type)
        {
            try
            {
                using (DirectoryEntry entity = this.activeDirEntry.Children.Find(this.User[0].ItemSpec, type))
                {
                    this.Exists = true;
                }
            }
            catch
            {
                this.Exists = false;
            }
        }

        private void DeleteUserFromGroup()
        {
            if (this.User == null)
            {
                Log.LogError("User is required");
                return;
            }

            if (this.Group == null)
            {
                Log.LogError("Group is required");
                return;
            }

            foreach (ITaskItem u in this.User)
            {
                foreach (ITaskItem g in this.Group)
                {
                    DirectoryEntry user;
                    try
                    {
                        user = this.activeDirEntry.Children.Find(u.ItemSpec, "User");
                    }
                    catch
                    {
                        Log.LogError(string.Format(CultureInfo.CurrentCulture, "User not found: {0}", u.ItemSpec));
                        return;
                    }

                    if (IsMember(user, g.ItemSpec))
                    {
                        DirectoryEntry grp;
                        try
                        {
                            grp = this.activeDirEntry.Children.Find(g.ItemSpec, "group");
                        }
                        catch
                        {
                            Log.LogError(string.Format(CultureInfo.CurrentCulture, "Group not found: {0}", g.ItemSpec));
                            return;
                        }

                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing User: {0} from {1}", u.ItemSpec, g.ItemSpec));
                        grp.Invoke("Remove", new object[] { user.Path });
                    }
                }
            }
        }

        private void AddUserToGroup()
        {
            if (this.User == null)
            {
                Log.LogError("User is required");
                return;
            }

            if (this.Group == null)
            {
                Log.LogError("Group is required");
                return;
            }

            foreach (ITaskItem u in this.User)
            {
                foreach (ITaskItem g in this.Group)
                {
                    DirectoryEntry user;
                    try
                    {
                        user = this.activeDirEntry.Children.Find(u.ItemSpec, "User");
                    }
                    catch
                    {
                        Log.LogError(string.Format(CultureInfo.CurrentCulture, "User not found: {0}", u.ItemSpec));
                        return;
                    }

                    DirectoryEntry groupDir;
                    DirectoryEntry grp;
                    if (this.groupType == ADGroupType.Local)
                    {
                        groupDir = new DirectoryEntry("WinNT://" + this.MachineName + ",computer");
                        try
                        {
                            grp = groupDir.Children.Find(g.ItemSpec, "group");
                        }
                        catch
                        {
                            Log.LogError(string.Format(CultureInfo.CurrentCulture, "Group not found: {0}", g.ItemSpec));
                            return;
                        }
                    }
                    else
                    {
                        try
                        {
                            grp = this.activeDirEntry.Children.Find(g.ItemSpec, "group");
                        }
                        catch
                        {
                            Log.LogError(string.Format(CultureInfo.CurrentCulture, "Group not found: {0}", g.ItemSpec));
                            return;
                        }
                    }

                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding User: {0} to {1}", u.ItemSpec, g.ItemSpec));
                    try
                    {
                        grp.Invoke("Add", new object[] { user.Path });
                    }
                    catch
                    {
                        // ignore exceptions on invoke
                    }
                }
            }
        }

        private void DeleteEntity(string type, IEnumerable<ITaskItem> name)
        {
            foreach (ITaskItem i in name)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting {0}: {1} on {2}", type, i.ItemSpec, this.target));

                try
                {
                    DirectoryEntry entity = this.activeDirEntry.Children.Find(i.ItemSpec, type);
                    this.activeDirEntry.Children.Remove(entity);
                    this.activeDirEntry.CommitChanges();
                }
                catch
                {
                    // ignore exceptions on invoke
                }
            }
        }

        private void AddGroup()
        {
            if (this.Group == null)
            {
                Log.LogError("Group is required");
                return;
            }

            DirectoryEntry group;
            try
            {
                group = this.activeDirEntry.Children.Find(this.Group[0].ItemSpec, "group");
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Updating Group: {0} on {1}", this.Group[0].ItemSpec, this.target));
            }
            catch
            {
                group = this.activeDirEntry.Children.Add(this.Group[0].ItemSpec, "group");
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding Group: {0} on {1}", this.Group[0].ItemSpec, this.target));
            }

            InvokeObj(group, "FullName", this.FullName);
            InvokeObj(group, "Description", this.Description);
            InvokeObj(group, "groupType", Convert.ToInt32(this.groupType, CultureInfo.InvariantCulture));
            group.CommitChanges();
            group.Close();
        }

        private void AddUser()
        {
            if (this.User == null)
            {
                Log.LogError("User is required");
                return;
            }

            DirectoryEntry user;
            try
            {
                user = this.activeDirEntry.Children.Find(this.User[0].ItemSpec, "User");
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Updating User: {0} on {1}", this.User[0].ItemSpec, this.MachineName));
            }
            catch
            {
                user = this.activeDirEntry.Children.Add(this.User[0].ItemSpec, "User");
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding User: {0} on {1}", this.User[0].ItemSpec, this.MachineName));
            }

            if (!string.IsNullOrEmpty(this.Password))
            {
                user.Invoke("SetPassword", new object[] { this.Password });
            }

            InvokeObj(user, "FullName", this.FullName);
            InvokeObj(user, "Description", this.Description);
            InvokeObj(user, "PasswordExpired", this.passwordExpired);
            InvokeObj(user, "FullName", this.FullName);

            if (this.PasswordNeverExpires)
            {
                try
                {
                    int flags = Convert.ToInt16(user.InvokeGet("UserFlags"), CultureInfo.InvariantCulture);
                    flags |= 0x10000;
                    user.Invoke("Put", new object[] { "UserFlags", flags });
                }
                catch
                {
                    // ignore exceptions on invoke
                }
            }

            user.CommitChanges();
            user.Close();
        }
    }
}