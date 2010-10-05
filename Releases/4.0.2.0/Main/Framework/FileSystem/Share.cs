//-----------------------------------------------------------------------
// <copyright file="Share.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// Portions of this task are based on the http://www.codeplex.com/sdctasks. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Management;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CheckExists</i> (<b>Required: </b> ShareName <b>Output:</b> Exists)</para>
    /// <para><i>Create</i> (<b>Required: </b> ShareName, SharePath <b>Optional: </b>Description, MaximumAllowed, CreateSharePath, AllowUsers, DenyUsers)</para>
    /// <para><i>Delete</i> (<b>Required: </b> ShareName)</para>
    /// <para><i>SetPermissions</i> (<b>Required: </b> ShareName <b>Optional: </b>AllowUsers, DenyUsers)</para>
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
    ///         <ItemGroup>
    ///             <Allow Include="ADomain\ADomainUser"/>
    ///             <Allow Include="AMachine\ALocalReadUser">
    ///                 <Permission>Read</Permission>
    ///             </Allow>
    ///             <Allow Include="AMachine\ALocalChangeUser">
    ///                 <Permission>Change</Permission>
    ///             </Allow>
    ///         </ItemGroup>
    ///         <!-- Delete shares -->
    ///         <MSBuild.ExtensionPack.FileSystem.Share TaskAction="Delete" ShareName="MSBEPS1"/>
    ///         <MSBuild.ExtensionPack.FileSystem.Share TaskAction="Delete" ShareName="MSBEPS2"/>
    ///         <!-- Create a share and specify users. The share path will be created if it doesnt exist. -->
    ///         <MSBuild.ExtensionPack.FileSystem.Share TaskAction="Create" AllowUsers="@(Allow)" CreateSharePath="true" SharePath="C:\fff1" ShareName="MSBEPS1" Description="A Description of MSBEPS1"/>
    ///         <!-- Create a share. Defaults to full permission for Everyone. -->
    ///         <MSBuild.ExtensionPack.FileSystem.Share TaskAction="Create" SharePath="C:\fffd" ShareName="MSBEPS2" Description="A Description of MSBEPS2"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example> 
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.2.0/html/c9f431c3-c240-26ab-32da-74fc81894a72.htm")]
    public class Share : BaseTask
    {
        private const string CheckExistsTaskAction = "CheckExists";
        private const string DeleteTaskAction = "Delete";
        private const string CreateTaskAction = "Create";
        private const string SetPermissionsTaskAction = "SetPermissions";

        #region enums
        private enum ReturnCode : uint
        {
            /// <summary>
            /// Success
            /// </summary>
            Success = 0,

            /// <summary>
            /// AccessDenied
            /// </summary>
            AccessDenied = 2,

            /// <summary>
            /// UnknownFailure
            /// </summary>
            UnknownFailure = 8,

            /// <summary>
            /// InvalidName
            /// </summary>
            InvalidName = 9,

            /// <summary>
            /// InvalidLevel
            /// </summary>
            InvalidLevel = 10,

            /// <summary>
            /// InvalidParameter
            /// </summary>
            InvalidParameter = 21,

            /// <summary>
            /// ShareAlreadyExists
            /// </summary>
            ShareAlreadyExists = 22,

            /// <summary>
            /// RedirectedPath
            /// </summary>
            RedirectedPath = 23,

            /// <summary>
            /// UnknownDeviceOrDirectory
            /// </summary>
            UnknownDeviceOrDirectory = 24,

            /// <summary>
            /// NetNameNotFound
            /// </summary>
            NetNameNotFound = 25
        }
        #endregion

        [DropdownValue(CheckExistsTaskAction)]
        [DropdownValue(CreateTaskAction)]
        [DropdownValue(DeleteTaskAction)]
        [DropdownValue(SetPermissionsTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the desctiption for the share
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public string Description { get; set; }

        /// <summary>
        /// Sets the share name
        /// </summary>
        [Required]
        [TaskAction(CheckExistsTaskAction, true)]
        [TaskAction(CreateTaskAction, true)]
        [TaskAction(DeleteTaskAction, true)]
        [TaskAction(SetPermissionsTaskAction, true)]
        public string ShareName { get; set; }

        /// <summary>
        /// Sets the share path
        /// </summary>
        [TaskAction(CreateTaskAction, true)]
        public string SharePath { get; set; }

        /// <summary>
        /// Sets the maximum number of allowed users for the share
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public int MaximumAllowed { get; set; }

        /// <summary>
        /// Sets whether to create the SharePath if it doesnt exist. Default is false
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public bool CreateSharePath { get; set; }

        /// <summary>
        /// Gets whether the share exists
        /// </summary>
        [Output]
        [TaskAction(CheckExistsTaskAction, false)]
        public bool Exists { get; set; }

        /// <summary>
        /// Sets a collection of users allowed to access the share. Use the Permission metadata tag to specify permissions. Default is Full.
        /// <para/>
        /// <code lang="xml"><![CDATA[
        /// <Allow Include="AUser">
        ///     <Permission>Read or Change etc</Permission>
        /// </Allow>
        /// ]]></code>    
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(SetPermissionsTaskAction, false)]
        public ITaskItem[] AllowUsers { get; set; }

        /// <summary>
        /// Sets a collection of users not allowed to access the share
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        [TaskAction(SetPermissionsTaskAction, false)]
        public ITaskItem[] DenyUsers { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case CreateTaskAction:
                    this.Create();
                    break;
                case DeleteTaskAction:
                    this.Delete();
                    break;
                case CheckExistsTaskAction:
                    this.CheckExists();
                    break;
                case SetPermissionsTaskAction:
                    this.SetPermissions();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private static ManagementObject GetSecurityIdentifier(ManagementBaseObject account)
        {
            // get the sid
            string sidString = (string)account.Properties["SID"].Value;
            string sidPathString = string.Format(CultureInfo.InvariantCulture, "Win32_SID.SID='{0}'", sidString);
            ManagementPath sidPath = new ManagementPath(sidPathString);
            ManagementObject returnSid;
            using (ManagementObject sid = new ManagementObject(sidPath))
            {
                try
                {
                    sid.Get();
                    returnSid = sid;
                }
                catch (ManagementException ex)
                {
                    throw new Exception(string.Format(CultureInfo.InvariantCulture, @"Could not find SID '{0}' for account '{1}\{2}'.", sidString, account.Properties["Domain"].Value, account.Properties["Name"].Value), ex);
                }
            }

            return returnSid;
        }

        private void CheckExists()
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Checking whether share: {0} exists on: {1}", this.ShareName, this.MachineName));
            this.GetManagementScope(@"\root\cimv2");
            ManagementPath fullSharePath = new ManagementPath("Win32_Share.Name='" + this.ShareName + "'");
            using (ManagementObject shareObject = new ManagementObject(this.Scope, fullSharePath, null))
            {
                // try bind to the share to see if it exists
                try
                {
                    shareObject.Get();
                    this.Exists = true;
                }
                catch
                {
                    this.Exists = false;
                }
            }
        }

        private void Delete()
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Deleting share: {0} on: {1}", this.ShareName, this.MachineName));
            this.GetManagementScope(@"\root\cimv2");
            ManagementPath fullSharePath = new ManagementPath("Win32_Share.Name='" + this.ShareName + "'");
            using (ManagementObject shareObject = new ManagementObject(this.Scope, fullSharePath, null))
            {
                // try bind to the share to see if it exists
                try
                {
                    shareObject.Get();
                }
                catch
                {
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Did not find share: {0} on: {1}", this.ShareName, this.MachineName));
                    return;
                }

                // execute the method and check the return code
                ManagementBaseObject outputParams = shareObject.InvokeMethod("Delete", null, null);
                ReturnCode returnCode = (ReturnCode)Convert.ToUInt32(outputParams.Properties["ReturnValue"].Value, CultureInfo.InvariantCulture);
                if (returnCode != ReturnCode.Success)
                {
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Failed to delete the share. ReturnCode: {0}.", returnCode));
                    return;
                }
            }
        }

        private void SetPermissions()
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Set Permissions for share: {0} on: {1}", this.ShareName, this.MachineName));
            this.GetManagementScope(@"\root\cimv2");
            ManagementPath fullSharePath = new ManagementPath("Win32_Share.Name='" + this.ShareName + "'");
            using (ManagementObject shareObject = new ManagementObject(this.Scope, fullSharePath, null))
            {
                // try bind to the share to see if it exists
                try
                {
                    shareObject.Get();
                }
                catch
                {
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Did not find share: {0} on: {1}", this.ShareName, this.MachineName));
                    return;
                }

                // Set the input parameters
                ManagementBaseObject inParams = shareObject.GetMethodParameters("SetShareInfo");
                inParams["Access"] = this.SetAccessPermissions();

                // execute the method and check the return code
                ManagementBaseObject outputParams = shareObject.InvokeMethod("SetShareInfo", inParams, null);
                ReturnCode returnCode = (ReturnCode)Convert.ToUInt32(outputParams.Properties["ReturnValue"].Value, CultureInfo.InvariantCulture);
                if (returnCode != ReturnCode.Success)
                {
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Failed to delete the share. ReturnCode: {0}.", returnCode));
                    return;
                }
            }
        }
        
        private void Create()
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Creating share: {0} on: {1}", this.ShareName, this.MachineName));

            if (!Directory.Exists(this.SharePath))
            {
                if (this.CreateSharePath)
                {
                    Directory.CreateDirectory(this.SharePath);
                }
                else
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "SharePath not found: {0}. Set CreateSharePath to true to create a SharePath that does not exist.", this.SharePath));
                    return;
                }
            }

            this.GetManagementScope(@"\root\cimv2");
            ManagementPath path = new ManagementPath("Win32_Share");
            using (ManagementClass managementClass = new ManagementClass(this.Scope, path, null))
            {
                // Set the input parameters
                ManagementBaseObject inParams = managementClass.GetMethodParameters("Create");
                inParams["Description"] = this.Description;
                inParams["Name"] = this.ShareName;
                inParams["Path"] = this.SharePath;

                // build the access permissions
                if (this.AllowUsers != null | this.DenyUsers != null)
                {
                    inParams["Access"] = this.SetAccessPermissions();
                }

                // Disk Drive
                inParams["Type"] = 0x0;

                if (this.MaximumAllowed > 0)
                {
                    inParams["MaximumAllowed"] = this.MaximumAllowed;
                }

                ManagementBaseObject outParams = managementClass.InvokeMethod("Create", inParams, null);
                ReturnCode returnCode = (ReturnCode)Convert.ToUInt32(outParams.Properties["ReturnValue"].Value, CultureInfo.InvariantCulture);
                switch (returnCode)
                {
                    case ReturnCode.Success:
                        break;
                    case ReturnCode.AccessDenied:
                        this.Log.LogError("Access Denied");
                        break;
                    case ReturnCode.UnknownFailure:
                        this.Log.LogError("Unknown Failure");
                        break;
                    case ReturnCode.InvalidName:
                        this.Log.LogError("Invalid Name");
                        break;
                    case ReturnCode.InvalidLevel:
                        this.Log.LogError("Invalid Level");
                        break;
                    case ReturnCode.InvalidParameter:
                        this.Log.LogError("Invalid Parameter");
                        break;
                    case ReturnCode.RedirectedPath:
                        this.Log.LogError("Redirected Path");
                        break;
                    case ReturnCode.UnknownDeviceOrDirectory:
                        this.Log.LogError("Unknown Device or Directory");
                        break;
                    case ReturnCode.NetNameNotFound:
                        this.Log.LogError("Net name not found");
                        break;
                    case ReturnCode.ShareAlreadyExists:
                        this.LogTaskWarning(string.Format(CultureInfo.CurrentCulture, "The share already exists: {0}", this.ShareName));
                        break;
                }
            }
        }

        private ManagementObject SetAccessPermissions()
        {
            // build the security descriptor
            ManagementPath securityDescriptorPath = new ManagementPath("Win32_SecurityDescriptor");
            ManagementObject returnSecurityDescriptor;
            using (ManagementObject securityDescriptor = new ManagementClass(this.Scope, securityDescriptorPath, null).CreateInstance())
            {
                // default owner | default group | DACL present | default SACL
                securityDescriptor.Properties["ControlFlags"].Value = 0x1U | 0x2U | 0x4U | 0x20U;
                securityDescriptor.Properties["DACL"].Value = this.BuildAccessControlList();
                returnSecurityDescriptor = securityDescriptor;
            }

            return returnSecurityDescriptor;
        }

        private ManagementObject[] BuildAccessControlList()
        {
            List<ManagementObject> acl = new List<ManagementObject>();

            if (this.AllowUsers != null)
            {
                foreach (ITaskItem user in this.AllowUsers)
                {
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Allowing user: {0}", user.ItemSpec));
                    ManagementObject trustee = this.BuildTrustee(user.ItemSpec);
                    acl.Add(this.BuildAccessControlEntry(user, trustee, false));
                }
            }

            if (this.DenyUsers != null)
            {
                foreach (ITaskItem user in this.DenyUsers)
                {
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Denying user: {0}", user.ItemSpec));
                    ManagementObject trustee = this.BuildTrustee(user.ItemSpec);
                    acl.Add(this.BuildAccessControlEntry(user, trustee, true));
                }
            }

            return acl.ToArray();
        }

        private ManagementObject BuildTrustee(string userName)
        {
            if (!userName.Contains(@"\"))
            {
                // default to local user
                userName = Environment.MachineName + @"\" + userName;
            }

            // build the trustee
            string[] userNameParts = userName.Split('\\');
            string domain = userNameParts[0];
            string alias = userNameParts[1];
            ManagementObject account = this.GetAccount(domain, alias);
            ManagementObject sid = GetSecurityIdentifier(account);
            ManagementPath trusteePath = new ManagementPath("Win32_Trustee");
            ManagementObject returnedTrustee;
            using (ManagementObject trustee = new ManagementClass(this.Scope, trusteePath, null).CreateInstance())
            {
                trustee.Properties["Domain"].Value = domain;
                trustee.Properties["Name"].Value = alias;
                trustee.Properties["SID"].Value = sid.Properties["BinaryRepresentation"].Value;
                trustee.Properties["SidLength"].Value = sid.Properties["SidLength"].Value;
                trustee.Properties["SIDString"].Value = sid.Properties["SID"].Value;
                returnedTrustee = trustee;
            }

            return returnedTrustee;
        }

        private ManagementObject GetAccount(string domain, string alias)
        {
            // get the account - try to get it by searching those on the machine first which gets local accounts
            string queryString = string.Format(CultureInfo.InvariantCulture, "select * from Win32_Account where Name = '{0}' and Domain='{1}'", alias, domain);
            ObjectQuery query = new ObjectQuery(queryString);
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.Scope, query))
            {
                foreach (ManagementObject returnedAccount in searcher.Get())
                {
                    return returnedAccount;
                }
            }

            // didn't find it on the machine so we'll try to bind to it using a path - this works for domain accounts
            string accountPathString = string.Format(CultureInfo.InvariantCulture, "Win32_Account.Name='{0}',Domain='{1}'", alias, domain);
            ManagementPath accountPath = new ManagementPath(accountPathString);
            ManagementObject returnAccount;
            using (ManagementObject account = new ManagementObject(accountPath))
            {
                try
                {
                    account.Get();
                    returnAccount = account;
                }
                catch (ManagementException ex)
                {
                    throw new Exception(string.Format(CultureInfo.InvariantCulture, @"Could not find account '{0}\{1}'.", domain, alias), ex);
                }
            }

            return returnAccount;
        }

        private ManagementObject BuildAccessControlEntry(ITaskItem user, ManagementObject trustee, bool deny)
        {
            ManagementPath acePath = new ManagementPath("Win32_ACE");
            ManagementObject returnedAce;
            using (ManagementObject ace = new ManagementClass(this.Scope, acePath, null).CreateInstance())
            {
                string permissions = user.GetMetadata("Permission");

                if (string.IsNullOrEmpty(permissions))
                {
                    // apply all permissions
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Setting Full permission for: {0}", user.ItemSpec));
                    ace.Properties["AccessMask"].Value = 0x1U | 0x2U | 0x4U | 0x8U | 0x10U | 0x20U | 0x40U | 0x80U | 0x100U | 0x10000U | 0x20000U | 0x40000U | 0x80000U | 0x100000U;
                }
                else
                {
                    if (permissions.IndexOf("Read", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // readonly permission
                        this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Setting Read permission for: {0}", user.ItemSpec));
                        ace.Properties["AccessMask"].Value = 0x1U | 0x2U | 0x4U | 0x8U | 0x10U | 0x20U | 0x40U | 0x80U | 0x100U | 0x20000U | 0x40000U | 0x80000U | 0x100000U;
                    }

                    if (permissions.IndexOf("Change", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // change permission
                        this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Setting Change permission for: {0}", user.ItemSpec));
                        ace.Properties["AccessMask"].Value = 0x1U | 0x2U | 0x4U | 0x8U | 0x10U | 0x20U | 0x40U | 0x80U | 0x100U | 0x10000U | 0x20000U | 0x40000U | 0x100000U;
                    }
                }

                // no flags
                ace.Properties["AceFlags"].Value = 0x0U;

                // 0 = allow, 1 = deny
                ace.Properties["AceType"].Value = deny ? 1U : 0U;
                ace.Properties["Trustee"].Value = trustee;
                returnedAce = ace;
            }

            return returnedAce;
        }
    }
}
