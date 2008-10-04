//-----------------------------------------------------------------------
// <copyright file="Folder.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Management;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>DeleteAll</i> (<b>Required: </b> Path, Match)</para>
    /// <para><i>Move</i> (<b>Required: </b> Path, TargetPath)</para>
    /// <para><i>RemoveContent</i> (<b>Required: </b> Path <b>Optional: </b>Force)</para>
    /// <para><b>Remote Support:</b> No</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///   <Target Name="Default">
    ///       <!-- Delete all folders matching a given name -->
    ///       <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="DeleteAll" Path="c:\Demo2" Match="_svn"/>
    ///       <!-- Remove all content from a folder whilst maintaining the target folder -->
    ///       <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="RemoveContent" Path="c:\Demo"/>
    ///       <!-- Move a folder -->
    ///       <MSBuild.ExtensionPack.FileSystem.Folder TaskAction="Move" Path="c:\Demo1" TargetPath="C:\adeeeee"/>
    ///   </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Folder : BaseTask
    {
        /// <summary>
        /// Sets the path to remove content from, or the base path for Delete
        /// </summary>
        [Required]
        public string Path { get; set; }

        /// <summary>
        /// Sets the regular expression to match in the name of a folder for Delete. Case is ignored.
        /// </summary>
        public string Match { get; set; }

        /// <summary>
        /// Sets the TargetPath for a renamed folder
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// Sets a value indicating whether to delete readonly files when performing RemoveContent
        /// </summary>
        public bool Force { get; set; }

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
                this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "The directory does not exist: {0}", this.Path));
                return;
            }

            switch (this.TaskAction)
            {
                case "RemoveContent":
                    this.RemoveContent(dir);
                    break;
                case "Move":
                    this.Move();
                    break;
                case "DeleteAll":
                    this.DeleteAll();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
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

        private void DeleteAll()
        {
            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Removing all Folders from: {0} that match: {1}", this.Path, this.Match));
            if (string.IsNullOrEmpty(this.Match))
            {
                Log.LogError("Match must be specified.");
                return;
            }
            
            DirectoryInfo d = new DirectoryInfo(this.Path);
            this.ProcessDeleteAll(d);
        }

        private void ProcessDeleteAll(DirectoryInfo d)
        {
            foreach (DirectoryInfo child in d.GetDirectories())
            {
                // Load the regex to use
                Regex reg = new Regex(this.Match, RegexOptions.IgnoreCase | RegexOptions.Compiled);

                // Match the regular expression pattern against a text string.
                Match m = reg.Match(child.Name);
                if (m.Success)
                {
                    this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Removing: {0}", child.FullName));
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
            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Removing Content from Folder: {0}", dir.FullName));

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
                            string dirObject = string.Format("win32_Directory.Name='{0}'", i.FullName);
                            using (ManagementObject mdir = new ManagementObject(dirObject))
                            {
                                mdir.Get();
                                ManagementBaseObject outParams = mdir.InvokeMethod("Delete", null, null);

                                // ReturnValue should be 0, else failure
                                if (outParams != null)
                                {
                                    if (Convert.ToInt32(outParams.Properties["ReturnValue"].Value) != 0)
                                    {
                                        this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Directory deletion error: ReturnValue: {0}", outParams.Properties["ReturnValue"].Value));
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

            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Moving Folder: {0} to: {1}", this.Path, this.TargetPath));
            Directory.Move(this.Path, this.TargetPath);
        }
    }
}