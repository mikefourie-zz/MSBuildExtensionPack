//-----------------------------------------------------------------------
// <copyright file="ActiveDirectory.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
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

    internal enum PrivilegeType
    {
        /// <summary>
        /// SeInteractiveLogonRight
        /// </summary>
        SeInteractiveLogonRight,

        /// <summary>
        /// SeNetworkLogonRight
        /// </summary>
        SeNetworkLogonRight,

        /// <summary>
        /// SeBatchLogonRight
        /// </summary>
        SeBatchLogonRight,

        /// <summary>
        /// SeServiceLogonRight
        /// </summary>
        SeServiceLogonRight,

        /// <summary>
        /// SeDenyInteractiveLogonRight
        /// </summary>
        SeDenyInteractiveLogonRight,

        /// <summary>
        /// SeDenyNetworkLogonRight
        /// </summary>
        SeDenyNetworkLogonRight,

        /// <summary>
        /// SeDenyBatchLogonRight
        /// </summary>
        SeDenyBatchLogonRight,

        /// <summary>
        /// SeDenyServiceLogonRight
        /// </summary>
        SeDenyServiceLogonRight,

        /// <summary>
        /// SeRemoteInteractiveLogonRight
        /// </summary>
        SeRemoteInteractiveLogonRight,

        /// <summary>
        /// SeDenyRemoteInteractiveLogonRight
        /// </summary>
        SeDenyRemoteInteractiveLogonRight,

        /// <summary>
        /// SeIncreaseQuotaPrivilege
        /// </summary>
        SeIncreaseQuotaPrivilege,

        /// <summary>
        /// SeAuditPrivilege
        /// </summary>
        SeAuditPrivilege,

        /// <summary>
        /// SeAssignPrimaryTokenPrivilege
        /// </summary>
        SeAssignPrimaryTokenPrivilege
    }

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddUser</i> (<b>Required: </b> User <b>Optional: </b>Domain, FullName, Description, Password, PasswordExpired, PasswordNeverExpires, FirstName, LastName)</para>
    /// <para><i>AddGroup</i> (<b>Required: </b> Group <b>Optional: </b>Domain, Description, GroupType)</para>
    /// <para><i>AddGroupToGroup</i> (<b>Required: </b> Parent, Group). Windows Server 2008 only.</para>
    /// <para><i>AddUserToGroup</i> (<b>Required: </b> User, Group)</para>
    /// <para><i>CheckUserExists</i> (<b>Required: </b> User <b>Output:</b> Exists)</para>
    /// <para><i>CheckUserPassword</i> (<b>Required: </b> User, Password <b>Optional:</b> BindingContextOptions, ContextTypeStore, Domain <b>Output:</b> Exists)</para>
    /// <para><i>CheckGroupExists</i> (<b>Required: </b> Group <b>Output:</b> Exists)</para>
    /// <para><i>DeleteUser</i> (<b>Required: </b> User)</para>
    /// <para><i>DeleteGroup</i> (<b>Required: </b> Group)</para>
    /// <para><i>DeleteUserFromGroup</i> (<b>Required: </b> User, Group)</para>
    /// <para><i>GetGroupMembers</i> (<b>Required: </b> Group <b>Optional: </b>GetFullMemberName <b>Output:</b> Members)</para>
    /// <para><i>GetUserPassword</i> (<b>Required: </b>User  <b>Optional: </b>BindingContextOptions, ContextTypeStore, Domain, ErrorOnCancel<b>Output:</b> Password)</para>
    /// <para><i>GrantPrivilege</i> (<b>Required: </b>User, Privilege  <b>Optional: </b>Domain)</para>
    /// <para><i>RemoveGroupFromGroup</i> (<b>Required: </b> Parent, Group). Windows Server 2008 only.</para>
    /// <para><b>Remote Execution Support:</b> Yes</para>
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
    ///         <!-- Check a user Exists -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="CheckUserExists" User="JudgeJS1">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///         <Message Text="JudgeJS1 Exists: $(DoesExist)"/>
    ///         <!-- Add local Users -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddUser" User="JudgeJS1" Description="Elgnt" Password="123546fdfdRERF$" PasswordNeverExpires="true"/>
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddUser" User="JudgeJS2" Description="Elgnt" PasswordNeverExpires="true"/>
    ///         <!-- Grant a user a privilege local Users -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="GrantPrivilege" User="JudgeJS1" Privilege="SeServiceLogonRight"/>
    ///         <!-- Check a user Exists -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="CheckUserExists" User="JudgeJS1">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///         <Message Text="JudgeJS1 Exists: $(DoesExist)"/>
    ///         <!-- Check a Group Exists -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="CheckGroupExists" Group="NewGroup1">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///         <Message Text="NewGroup1 Exists: $(DoesExist)"/>
    ///         <!-- Add local Groups -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroup" Group="NewGroup1" Description="Elgnt"/>
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroup" Group="NewGroup2" Description="Elgnt"/>
    ///         <!-- Check a Group Exists -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="CheckGroupExists" Group="NewGroup1">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///         <Message Text="NewGroup1 Exists: $(DoesExist)"/>
    ///         <!-- Add the users to the Groups -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddUserToGroup" User="JudgeJS1;JudgeJS2" Group="NewGroup1;NewGroup2"/>
    ///         <!-- To add domain user(s) to a group, prefix name with the user's domain -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddUserToGroup" User="ADOMAIN\JudgeJS1" Group="Group1"/>
    ///         <!-- Delete Users from Groups -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="DeleteUserFromGroup" User="JudgeJS1" Group="NewGroup1;NewGroup2"/>
    ///         <!-- Delete local Users -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="DeleteUser" User="JudgeJS1;JudgeJS2"/>
    ///         <!-- Delete local Groups -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="DeleteGroup" Group="NewGroup1;NewGroup2"/>
    ///         <!-- Add a remote User -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddUser" User="JudgeJS1" MachineName="D420-7" Description="Elgnt" Password="123546fdfdRERF$" PasswordNeverExpires="true"/>
    ///         <!-- Add a remote Group -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroup" Group="RemoteGroup1" MachineName="D420-7" Description="na"/>
    ///         <!-- Add a Domain User -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddUser" User="JudgeJS1" Domain="mydomain" Description="Elgnt" Password="123546fdfdRERF$" PasswordNeverExpires="true"/>
    ///         <!-- Add a Domain Group -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroup" Group="DomainGroup1" Domain="mydomain" Description="na"/>
    ///         <!-- Get a user's password-->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="GetUserPassword" User="Michael" ContextTypeStore="Machine">
    ///             <Output TaskParameter="Password" PropertyName="Pass"/>
    ///         </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///         <Message Text="User Password: $(Pass)"/>
    ///         <!-- Check a user's password-->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="CheckUserPassword" User="Michael" ContextTypeStore="Machine" Password="$(Pass)">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///         <Message Text="User Exists: $(DoesExist)"/>
    ///         <!-- Get Group Members -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="GetGroupMembers" Group="Performance Monitor Users;Users">
    ///             <Output TaskParameter="Members" ItemName="Groups"/>
    ///         </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///         <Message Text="%(Groups.Identity)"/>
    ///         <!-- Get Group Members including Parent -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="GetGroupMembers" GetFullMemberName="true" Group="Performance Monitor Users;Users">
    ///             <Output TaskParameter="Members" ItemName="FullGroups"/>
    ///         </MSBuild.ExtensionPack.Computer.ActiveDirectory>
    ///         <Message Text="FULL %(FullGroups.Identity)"/>
    ///         <!-- Group Group Operations -->
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroup" Group="NewGroup1" Description="Elgnt" GroupType="Global"/>
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroup" Group="NewGroup2" Description="Elgnt" GroupType="Global"/>
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroupToGroup" ParentGroup="NewGroup1" Group="NewGroup2"/>
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="RemoveGroupFromGroup" ParentGroup="NewGroup1" Group="NewGroup2"/>
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroupToGroup" ParentGroup="NewGroup1" Group="NewGroup2"/>
    ///         <MSBuild.ExtensionPack.Computer.ActiveDirectory TaskAction="AddGroupToGroup" ParentGroup="NewGroup1" Group="NewGroup2"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
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
        private const string GetGroupMembersTaskAction = "GetGroupMembers";
        private const string GrantPrivilegeTaskAction = "GrantPrivilege";
        private const string RemovePrivilegeTaskAction = "RemovePrivilege";
        private const string AddGroupToGroupTaskAction = "AddGroupToGroup";
        private const string RemoveGroupFromGroupTaskAction = "RemoveGroupFromGroup";
        private string target;
        private string domain;
        private int passwordExpired;
        private DirectoryEntry activeDirEntry;
        private ADGroupType groupType;
        private ContextOptions bindingContextOptions = ContextOptions.Negotiate;
        private ContextType contextType = ContextType.Domain;
        private PrivilegeType privilege;

        /// <summary>
        /// Sets the User name. Supports DirectoryPath metadata for AddUserToGroup. Use this to supply different domain users.
        /// </summary>
        public ITaskItem[] User { get; set; }

        /// <summary>
        /// Sets the Group name
        /// </summary>
        public ITaskItem[] Group { get; set; }

        /// <summary>
        /// Sets the User's full name
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Set the User's First name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Sets the User's Last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Sets the User's or Group's description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Sets the User's password
        /// </summary>
        [Output]
        public string Password { get; set; }

        /// <summary>
        /// Sets whether to extract the domain name when using GetGroupMembers. Default is false.
        /// </summary>
        public bool GetFullMemberName { get; set; }

        /// <summary>
        /// Sets the User's password to expired. Default is false
        /// </summary>
        public bool PasswordExpired
        {
            get { return this.passwordExpired == 1; }
            set { this.passwordExpired = value ? 1 : 0; }
        }

        /// <summary>
        /// Sets the User's password to never expire. Default is false
        /// </summary>
        public bool PasswordNeverExpires { get; set; }

        /// <summary>
        /// Sets the domain to operate against.
        /// </summary>
        public string Domain
        {
            get { return this.domain; }
            set { this.domain = value; }
        }

        /// <summary>
        /// Sets the GroupType. For non domains the default is Local. For Domains the default is Global. Supports Global, Local, Universal
        /// </summary>
        public string GroupType
        {
            get { return this.groupType.ToString(); }
            set { this.groupType = (ADGroupType)Enum.Parse(typeof(ADGroupType), value); }
        }

        /// <summary>
        /// Specifies the store to use. Supports Machine and Domain. Default is Domain.
        /// </summary>
        public string ContextTypeStore
        {
            get { return this.contextType.ToString(); }
            set { this.contextType = (ContextType)Enum.Parse(typeof(ContextType), value); }
        }

        /// <summary>
        /// Set to true to raise an error if the user clicks cancel on GetPassword form.
        /// </summary>
        public bool ErrorOnCancel { get; set; }
        
        /// <summary>
        /// Specifies the options that are used for binding to the server. Default is Negotiate
        /// </summary>
        public ITaskItem[] BindingContextOptions
        {
            set { this.bindingContextOptions = SetBindingOptions(value); }
        }

        /// <summary>
        /// The Privilege to grant. See http://msdn.microsoft.com/en-us/library/bb545671(VS.85).aspx
        /// </summary>
        public string Privilege
        {
            get { return this.privilege.ToString(); }
            set { this.privilege = (PrivilegeType)Enum.Parse(typeof(PrivilegeType), value); }
        }

        /// <summary>
        /// Gets whether the User or Group exists
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Gets the members of a group
        /// </summary>
        [Output]
        public ITaskItem[] Members { get; set; }
        
        /// <summary>
        /// The domain the user is in.  If not set, defaults to Domain.
        /// </summary>
        public string UserDomain { get; set; }

        /// <summary>
        /// Sets the Parent group
        /// </summary>
        public string ParentGroup { get; set; }

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
                    case AddGroupToGroupTaskAction:
                    case RemoveGroupFromGroupTaskAction:
                        this.GroupGroup();
                        break;
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
                        this.CheckExistsForUser(this.User[0].ItemSpec);
                        break;
                    case CheckUserPasswordTaskAction:
                        this.CheckUserPassword();
                        break;
                    case GetUserPasswordTaskAction:
                        this.GetUserPassword();
                        break;
                    case CheckGroupExistsTaskAction:
                        this.CheckExists("group", this.Group[0].ItemSpec);
                        break;
                    case GrantPrivilegeTaskAction:
                        this.GrantUserPrivilege();
                        break;
                    case RemovePrivilegeTaskAction:
                        // Not implemented this.RemoveUserPrivilege();
                        break;
                    case GetGroupMembersTaskAction:
                        this.GetGroupMembers();
                        break;
                    default:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                        return;
                }
            }
        }

        private static LSA_UNICODE_STRING CreateLsaString(string inputString)
        {
            LSA_UNICODE_STRING lsaString = new LSA_UNICODE_STRING();
            if (inputString == null)
            {
                lsaString.Buffer = IntPtr.Zero;
                lsaString.Length = 0;
                lsaString.MaximumLength = 0;
            }
            else
            {
                lsaString.Buffer = Marshal.StringToHGlobalAuto(inputString);
                lsaString.Length = (ushort)(inputString.Length * UnicodeEncoding.CharSize);
                lsaString.MaximumLength = (ushort)((inputString.Length + 1) * UnicodeEncoding.CharSize);
            }

            return lsaString;
        }

        private static ContextOptions SetBindingOptions(IEnumerable<ITaskItem> value)
        {
            return value.Aggregate(new ContextOptions(), (current, option) => current | (ContextOptions)Enum.Parse(typeof(ContextOptions), option.ItemSpec));
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

        /// <summary>
        /// Returns the fully qualified Domain name of the current domain
        /// </summary>
        /// <returns>The fully qualified domain name</returns>
        private static string GetFullyQualifiedDomainName()
        {
          string fullyQualifiedDomainName = string.Empty;
          using (DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE"))
          {
            string domainContext = rootDSE.Properties["defaultNamingContext"] != null ? rootDSE.Properties["defaultNamingContext"].Value.ToString() : string.Empty;
            if (string.IsNullOrWhiteSpace(domainContext))
            {
              return string.Empty;
            }

            var domainParts = domainContext.Split(new[] { ',' });
            foreach (var domainPart in domainParts)
            {
              if (domainPart.Contains("DC="))
              {
                if (string.IsNullOrWhiteSpace(fullyQualifiedDomainName))
                {
                  fullyQualifiedDomainName = domainPart.Replace("DC=", string.Empty);
                }
                else
                {
                  fullyQualifiedDomainName += "." + domainPart.Replace("DC=", string.Empty);
                }
              }
            }
          }
                    
          return fullyQualifiedDomainName;
        }

        private void GroupGroup()
        {
            if (this.Group == null)
            {
                Log.LogError("Group is required");
                return;
            }

            if (this.ParentGroup == null)
            {
                Log.LogError("ParentGroup is required");
                return;
            }

            this.CheckExists("group", this.ParentGroup);
            if (!this.Exists)
            {
                this.Log.LogError("Parent Group not found");
                return;
            }

            foreach (ITaskItem g in this.Group)
            {
                DirectoryEntry child;
                try
                {
                    child = this.activeDirEntry.Children.Find(g.ItemSpec, "group");
                }
                catch
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "Group not found: {0}", g.ItemSpec));
                    return;
                }

                DirectoryEntry parent = this.activeDirEntry.Children.Find(this.ParentGroup, "group");
                try
                {
                    switch (this.TaskAction)
                    {
                        case AddGroupToGroupTaskAction:
                            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding {0} to {1}", g.ItemSpec, this.ParentGroup));
                            parent.Invoke("Add", new object[] { child.Path });
                            break;
                        case RemoveGroupFromGroupTaskAction:
                            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing {0} from {1}", g.ItemSpec, this.ParentGroup));
                            parent.Invoke("Remove", new object[] { child.Path });
                            break;
                    }
                }
                catch
                {
                    // do nothing
                }
            }
        }

        private void GetGroupMembers()
        {
            if (this.Group == null)
            {
                Log.LogError("Group is required");
                return;
            }

            var taskItems = new List<ITaskItem>();
            foreach (ITaskItem g in this.Group)
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

                object members = grp.Invoke("members", null);
                foreach (object groupMember in (IEnumerable)members)
                {
                    using (DirectoryEntry member = new DirectoryEntry(groupMember))
                    {
                        TaskItem memberGroup = this.GetFullMemberName ? new TaskItem(member.Parent.Name + @"\" + member.Name) : new TaskItem(member.Name);
                        taskItems.Add(memberGroup);
                    }
                }
            }

            this.Members = taskItems.ToArray();
        }

        private void GrantUserPrivilege()
        {
            if (this.User == null)
            {
                Log.LogError("User is required");
                return;
            }

            if (this.Privilege == null)
            {
                Log.LogError("Privilege is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Granting Privilege to User: {0} - {1}", this.User[0].ItemSpec, this.Privilege));

            int sidInt = 0;
            IntPtr sid = IntPtr.Zero;
            int domainNameInt = 0;
            int use = 0;
            IntPtr policyHandle = new IntPtr();

            try
            {
                StringBuilder domainNameInternal = new StringBuilder(this.Domain);
                ActiveDirectoryNativeMethods.LookupAccountName(this.MachineName, this.User[0].ItemSpec, sid, ref sidInt, domainNameInternal, ref domainNameInt, ref use);
                domainNameInternal = new StringBuilder(domainNameInt);
                sid = Marshal.AllocHGlobal(sidInt);
                int returnValue = ActiveDirectoryNativeMethods.LookupAccountName(this.MachineName, this.User[0].ItemSpec, sid, ref sidInt, domainNameInternal, ref domainNameInt, ref use);
                if (returnValue == 0)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Error looking up account name: {0}", returnValue));
                    return;
                }

                LSA_OBJECT_ATTRIBUTES objectAttributes = new LSA_OBJECT_ATTRIBUTES { Length = 0, RootDirectory = IntPtr.Zero, Attributes = 0, SecurityDescriptor = IntPtr.Zero, SecurityQualityOfService = IntPtr.Zero };
                LSA_UNICODE_STRING machineNameLSA = CreateLsaString(this.MachineName);
                uint result = ActiveDirectoryNativeMethods.LsaOpenPolicy(ref machineNameLSA, ref objectAttributes, ActiveDirectoryNativeMethods.POLICY_CREATE_SECRET, out policyHandle);
                if (result != 0)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Error running LsaOpenPolicy: {0}", returnValue));
                    return;
                }

                LSA_UNICODE_STRING privilegeString = CreateLsaString(this.Privilege);
                result = ActiveDirectoryNativeMethods.LsaAddAccountRights(policyHandle, sid, ref privilegeString, 1);
                if (result != 0)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Error running LsaAddAccountRights: {0}", returnValue));
                }
            }
            finally
            {
                ActiveDirectoryNativeMethods.LsaClose(policyHandle);
                Marshal.FreeHGlobal(sid);
            }
        }

        private void GetUserPassword()
        {
            using (GetPasswordForm form = new GetPasswordForm(this.User[0].ItemSpec, this.Domain, this.contextType, this.bindingContextOptions))
            {
                form.ShowDialog();
                this.Password = form.Password;

                if (form.UserCanceled && this.ErrorOnCancel)
                {
                    this.Log.LogError("User Cancelled");
                }

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
            catch (Exception ex)
            {
                this.LogTaskMessage(ex.ToString());
                this.Exists = false;
            }
        }

        private void CheckExists(string type, string name)
        {
            try
            {
                using (DirectoryEntry entity = this.activeDirEntry.Children.Find(name, type))
                {
                    this.Exists = true;
                }
            }
            catch
            {
                this.Exists = false;
            }
        }

        private void CheckExistsForUser(string userName)
        {
          PrincipalContext principalContext = null;
          try
          {
            bool isLocalMachineUser = string.IsNullOrEmpty(this.Domain) || string.Compare(this.Domain, this.MachineName, StringComparison.OrdinalIgnoreCase) == 0;
            principalContext = isLocalMachineUser ? new PrincipalContext(ContextType.Machine) : new PrincipalContext(ContextType.Domain);
            this.Exists = UserPrincipal.FindByIdentity(principalContext, userName) != null;
          }
          catch
          {
            this.Exists = false;
          }
          finally
          {
            if (principalContext != null)
            {
              principalContext.Dispose();
            }
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
                var username = u.ItemSpec;
                var userAdEntry = this.activeDirEntry;
                var userDirPath = u.GetMetadata("DirectoryPath");

                if (!string.IsNullOrEmpty(userDirPath))
                {
                    userAdEntry = new DirectoryEntry(userDirPath);
                }
                else if (username.Contains(@"\"))
                {
                    var userDomain = username.Substring(0, username.IndexOf(@"\", StringComparison.OrdinalIgnoreCase));
                    username = username.Substring(username.IndexOf(@"\", StringComparison.OrdinalIgnoreCase) + 1);
                    userAdEntry = new DirectoryEntry("WinNT://" + userDomain + ",domain");
                }

                foreach (ITaskItem g in this.Group)
                {
                    DirectoryEntry user;
                    try
                    {
                        user = userAdEntry.Children.Find(username, "User");
                    }
                    catch
                    {
                        user = null;
                    }

                    if (user == null)
                    {
                        try
                        {
                            user = userAdEntry.Children.Find(username, "Group");
                        }
                        catch (Exception)
                        {
                            Log.LogError(string.Format(CultureInfo.CurrentCulture, "User not found: {0} in: {1}", u.ItemSpec, userAdEntry.Path));
                            return;
                        }
                    }

                    DirectoryEntry grp;
                    if (this.groupType == ADGroupType.Local)
                    {
                        DirectoryEntry groupDir;
                        using (groupDir = new DirectoryEntry("WinNT://" + this.MachineName + ",computer"))
                        {
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
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Updating Group: {0} on {1}", this.Group[0].ItemSpec, this.target));
                group = this.activeDirEntry.Children.Find(this.Group[0].ItemSpec, "group");
            }
            catch
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding Group: {0} on {1}", this.Group[0].ItemSpec, this.target));
                group = this.activeDirEntry.Children.Add(this.Group[0].ItemSpec, "group");
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

            PrincipalContext principalContext = null;
            UserPrincipal userPrincipal  = null;
            try
            {
              bool isLocalMachineUser = string.IsNullOrEmpty(this.Domain) || string.Compare(this.Domain, this.MachineName, StringComparison.OrdinalIgnoreCase) == 0;
              principalContext = isLocalMachineUser ? new PrincipalContext(ContextType.Machine) : new PrincipalContext(ContextType.Domain);
              userPrincipal = UserPrincipal.FindByIdentity(principalContext, this.User[0].ItemSpec);

              if (userPrincipal == null)
              {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding User: {0}", this.User[0].ItemSpec));
                userPrincipal = new UserPrincipal(principalContext);
              }
              else
              {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Updating User: {0}", this.User[0].ItemSpec));
              }

              userPrincipal.SamAccountName = this.User[0].ItemSpec;
              userPrincipal.DisplayName = this.FullName;
              userPrincipal.Description = this.Description;
              userPrincipal.PasswordNeverExpires = this.PasswordNeverExpires;
              userPrincipal.Enabled = true;
              if (this.PasswordExpired)
              {
                userPrincipal.ExpirePasswordNow();
              }

              if (!string.IsNullOrEmpty(this.Password))
              {
                userPrincipal.SetPassword(this.Password);
              }

              if (!isLocalMachineUser)
              {
                string fullyQualifiedDomainName = GetFullyQualifiedDomainName();
                userPrincipal.UserPrincipalName = !string.IsNullOrWhiteSpace(fullyQualifiedDomainName) ? this.User[0].ItemSpec + "@" + fullyQualifiedDomainName : this.User[0].ItemSpec;
              }

              userPrincipal.Save();

              if (!(string.IsNullOrWhiteSpace(this.FirstName) && string.IsNullOrWhiteSpace(this.LastName)))
              {
                if (!isLocalMachineUser)
                {
                    if (!string.IsNullOrWhiteSpace(this.FirstName))
                    {
                        userPrincipal.GivenName = this.FirstName;
                    }

                    if (!string.IsNullOrWhiteSpace(this.LastName))
                    {
                        userPrincipal.Surname = this.LastName;
                    }
                    
                    userPrincipal.Save();
                }
                else
                {
                    this.LogTaskWarning(string.Format(CultureInfo.CurrentCulture, "Cannot set First Name or Last Name for the user {0}. The operation is not supported for local users.", this.User[0].ItemSpec));
                }
              }
            }
            finally
            {
              if (userPrincipal != null)
              {
                userPrincipal.Dispose();
              }
              
              if (principalContext != null)
              {
                principalContext.Dispose();
              }
            }
        }
    }
}