//////-------------------------------------------------------------------------------------------------------------------------------------------------------------------
////// <copyright file="FileTest.cs">(c) 2017 Mike Fourie and Contributors (http://www.MSBuildExtensionPack.com) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//////-------------------------------------------------------------------------------------------------------------------------------------------------------------------
////namespace MSBuild.ExtensionPack.Framework.Tests
////{
////    using System.IO;
////    using System.Security.AccessControl;
////    using System.Security.Principal;
////    using System.Text;
////    using Microsoft.Build.Framework;
////    using Microsoft.Build.Utilities;
////    using Microsoft.VisualStudio.TestTools.UnitTesting;
////    using File = MSBuild.ExtensionPack.FileSystem.File;

////    [TestClass]
////    public sealed class FileTest
////    {
////        private File task;
////        private bool result;

////        public string CurrentUser
////        {
////            get { return WindowsIdentity.GetCurrent().Name; }
////        }

////        [TestInitialize]
////        public void Setup()
////        {
////            this.task = new File();

////            // this.task.Log = new TaskLoggingHelper(new MockBuildEngine(), "Full");
////        }

////        [TestMethod]
////        public void ShouldSetAllowedRights()
////        {
////            var rightsToAdd = new[] { FileSystemRights.Read, FileSystemRights.Write };

////            var paths = new[] { this.GivenAFile() };
////            this.GivenPath(paths[0]);
////            this.GivenUser();
////            this.GivenPermissions(AccessControlType.Allow, rightsToAdd);
////            this.WhenAddingSecurity();
////            this.ThenTaskSucceeded();
////            this.ThenPermissionsGetAdded(paths, AccessControlType.Allow, rightsToAdd);

////            var rightsToRemove = new[] { FileSystemRights.Write };
////            this.GivenPermissions(AccessControlType.Allow, rightsToRemove);
////            this.WhenRemovingSecurity();
////            this.ThenTaskSucceeded();
////            this.ThenPermissionsGetRemoved(paths, AccessControlType.Allow, rightsToRemove);
////        }

////        [TestMethod]
////        public void ShouldSetDeniedRights()
////        {
////            var rightsToAdd = new[] { FileSystemRights.Read, FileSystemRights.Write };
////            var paths = new[] { this.GivenAFile() };

////            this.GivenPath(paths[0]);
////            this.GivenUser();
////            this.GivenPermissions(AccessControlType.Deny, rightsToAdd);
////            this.WhenAddingSecurity();
////            this.ThenTaskSucceeded();
////            this.ThenPermissionsGetAdded(paths, AccessControlType.Deny, rightsToAdd);

////            var rightsToRemove = new[] { FileSystemRights.Write };
////            this.GivenPermissions(AccessControlType.Deny, rightsToRemove);
////            this.WhenRemovingSecurity();
////            this.ThenTaskSucceeded();
////            this.ThenPermissionsGetRemoved(paths, AccessControlType.Deny, rightsToRemove);
////        }

////        [TestMethod]
////        public void ShouldFailIfNoFileGiven()
////        {
////            // no file/path set
////            this.task.Path = null;
////            this.task.Files = null;

////            this.GivenPermissions(AccessControlType.Allow, new[] { FileSystemRights.Read });
////            this.WhenAddingSecurity();
////            this.ThenTaskFailed();
////        }

////        [TestMethod]
////        public void ShouldFailIfNoUserGiven()
////        {
////            this.task.Users = null;

////            this.GivenPermissions(AccessControlType.Allow, new[] { FileSystemRights.Read });
////            this.WhenAddingSecurity();
////            this.ThenTaskFailed();
////        }

////        [TestMethod]
////        public void ShouldSetPermissionsFromUserMetadata()
////        {
////            var rightsToAdd = new[] { FileSystemRights.Read, FileSystemRights.Write };
////            var path = this.GivenAFile();
////            this.GivenPath(path);
////            this.GivenUser();
////            this.GivenUsersPermissions(AccessControlType.Allow, rightsToAdd);
////            this.WhenAddingSecurity();
////            this.ThenTaskSucceeded();
////            this.ThenPermissionsGetAdded(new[] { path }, AccessControlType.Allow, rightsToAdd);
////        }

////        private FileSystemAccessRule GetCurrentRights(string path)
////        {
////            var fileInfo = new FileInfo(path);
////            var acl = fileInfo.GetAccessControl();
////            var accessRules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));
////            var currentUser = WindowsIdentity.GetCurrent();

////            foreach (FileSystemAccessRule rule in accessRules)
////            {
////                var sid = (NTAccount)rule.IdentityReference.Translate(typeof(NTAccount));
////                if (sid.Value == currentUser.Name)
////                {
////                    return rule;
////                }
////            }

////            return null;
////        }

////        private string GivenAFile()
////        {
////            return Path.GetTempFileName();
////        }

////        private void GivenPath(string path)
////        {
////            this.task.Path = new TaskItem(path);
////        }

////        private void GivenFiles(string[] paths)
////        {
////            this.task.Files = new ITaskItem[paths.Length];
////            for (int idx = 0; idx < paths.Length; ++idx)
////            {
////                this.task.Files[idx] = new TaskItem(paths[idx]);
////            }
////        }

////        private void GivenPermissions(AccessControlType aclType, FileSystemRights[] rights)
////        {
////            this.task.Permission = this.RightsToPermissions(rights);
////            this.task.AccessType = aclType.ToString();
////        }

////        private void GivenUsersPermissions(AccessControlType aclType, FileSystemRights[] rights)
////        {
////            var permission = this.RightsToPermissions(rights);
////            foreach (ITaskItem userTaskItem in this.task.Users)
////            {
////                userTaskItem.SetMetadata("Permission", permission);
////            }

////            this.task.AccessType = aclType.ToString();
////        }

////        private void GivenUser()
////        {
////            this.task.Users = new[] { new TaskItem(this.CurrentUser) };
////        }

////        private void GivenUsers(string[] users)
////        {
////            this.task.Users = new ITaskItem[users.Length];
////            for (int idx = 0; idx < users.Length; ++idx)
////            {
////                this.task.Users[idx] = new TaskItem(users[idx]);
////            }
////        }

////        private string RightsToPermissions(FileSystemRights[] rights)
////        {
////            var permission = new StringBuilder();
////            foreach (var right in rights)
////            {
////                permission.Append(right.ToString());
////                permission.Append(",");
////            }

////            return permission.ToString(0, permission.Length - 1);
////        }

////        private void ThenPermissionsGetAdded(string[] paths, AccessControlType aclType, FileSystemRights[] rights)
////        {
////           this.ThenPermissionsGetSet(paths, true, aclType, rights);
////        }

////        private void ThenPermissionsGetRemoved(string[] paths, AccessControlType aclType, FileSystemRights[] rights)
////        {
////            this.ThenPermissionsGetSet(paths, false, aclType, rights);
////        }

////        private void ThenPermissionsGetSet(string[] paths, bool adding, AccessControlType aclType, FileSystemRights[] rights)
////        {
////            FileSystemRights expectedRights = 0;
////            foreach (var right in rights)
////            {
////                expectedRights |= right;
////            }

////            if (aclType == AccessControlType.Allow)
////            {
////                expectedRights |= FileSystemRights.Synchronize;
////            }

////            foreach (string path in paths)
////            {
////                var rule = this.GetCurrentRights(path);
////                Assert.IsNotNull(rule);
////                Assert.AreEqual(aclType, rule.AccessControlType);
////                if (adding)
////                {
////                    Assert.IsTrue(rule.FileSystemRights.HasFlag(expectedRights));
////                }
////                else
////                {
////                    Assert.IsFalse(rule.FileSystemRights.HasFlag(expectedRights));
////                }
////            }
////        }

////        private void ThenTaskFailed()
////        {
////            Assert.IsFalse(this.result);
////        }

////        private void ThenTaskSucceeded()
////        {
////            Assert.IsTrue(this.result);
////        }

////        private void WhenAddingSecurity()
////        {
////            this.WhenTaskRuns("AddSecurity");
////        }

////        private void WhenRemovingSecurity()
////        {
////            this.WhenTaskRuns("RemoveSecurity");
////        }

////        private void WhenTaskRuns(string taskAction)
////        {
////            this.task.TaskAction = taskAction;
////            this.result = this.task.Execute();
////        }
////    }
////}
