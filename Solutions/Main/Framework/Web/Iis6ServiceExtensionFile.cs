//-----------------------------------------------------------------------
// <copyright file="Iis6ServiceExtensionFile.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Web
{
    using System;
    using System.DirectoryServices;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Add</i> (<b>Required: </b> Path <b>Optional:</b> Deletable, Force Description, GroupId, Permission)</para>
    /// <para><i>CheckExists</i> (<b>Required: </b> Path <b>Output: </b>Exists)</para>
    /// <para><i>Delete</i> (<b>Required: </b> Path</para>
    /// <para><b>Remote Execution Support:</b> Yes. Please note that the machine you execute from must have IIS installed.</para>
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
    ///         <!-- Add an extension file -->
    ///         <MSBuild.ExtensionPack.Web.Iis6ServiceExtensionFile TaskAction="Add" Path="C:\Demo1\MyExtensionFile.dll" Description="My Extension Service" Deletable="true" GroupID="myext01" Permission="Allowed"/>
    ///         <!-- Delete an extension file -->
    ///         <MSBuild.ExtensionPack.Web.Iis6ServiceExtensionFile TaskAction="Delete" Path="C:\Demo1\MyExtensionFile.dll"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Iis6ServiceExtensionFile : BaseTask
    {     
        private ExtensionPermission permission = ExtensionPermission.Allowed;
       
        /// <summary>
        /// ExtensionPermission.
        /// </summary>
        internal enum ExtensionPermission
        {
            /// <summary>
            /// Prohibited
            /// </summary>
            Prohibited = 0,

            /// <summary>
            /// Allowed
            /// </summary>
            Allowed = 1,
        }

        /// <summary>
        /// Sets the Path to the web extension service file.
        /// </summary>
        [Required]
        public string Path { get; set; }

        /// <summary>
        /// Sets whether the file can be deleted from the Web Service Extension Restriction List.
        /// </summary>
        public bool Deletable { get; set; }

        /// <summary>
        /// Sets the Description of the web service extension being added
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// A unique text ID associated with one or more ISAPIs or CGIs required for enabling the group
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// Sets whether the extension is Allowed or Prohibited. Default is Allowed.
        /// </summary>
        public string Permission
        { 
            get { return this.permission.ToString(); }
            set { this.permission = (ExtensionPermission)Enum.Parse(typeof(ExtensionPermission), value); }
        }

            /// <summary>
        /// Set to true to delete an existing extension of the same name. Default is false.
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Gets whether the service extension file exists. Output
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        public bool FileExists()
        {
            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Checking whether File exists: {0}", this.Path));

            bool result = false;
            using (DirectoryEntry web = new DirectoryEntry("IIS://" + this.MachineName + "/W3SVC"))
            {
                ComWrapper ws = new ComWrapper(web.NativeObject);
                Array extensionFiles = (Array)ws.CallMethod("ListExtensionFiles");
                if (extensionFiles != null)
                {
                    result = extensionFiles.Cast<string>().Any(extension => extension.Trim().ToUpperInvariant() == this.Path.Trim().ToUpperInvariant());
                }
            }

            return result;
        }

        public void DeleteFile()
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Delete File: {0}", this.Path));
            if (this.FileExists())
            {
                using (DirectoryEntry web = new DirectoryEntry("IIS://" + this.MachineName + "/W3SVC"))
                {
                    ComWrapper ws = new ComWrapper(web.NativeObject);

                    try
                    {
                        ws.CallMethod("DeleteExtensionFileRecord", new object[] { this.Path });
                    }
                    catch (COMException ex)
                    {
                        throw new ApplicationException(string.Format(CultureInfo.InvariantCulture, "Unable to delete web service extension of '{0}'", this.Path), ex);
                    }
                }
            }
        }

        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case "Add":
                    this.AddFile();
                    break;
                case "Delete":
                    this.DeleteFile();
                    break;
                case "CheckExists":
                    this.Exists = this.FileExists();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void AddFile()
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Adding File: {0}", this.Path));
            if (this.FileExists())
            {
                if (this.Force)
                {
                    this.DeleteFile();
                }
                else
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "The File already exists: {0}", this.Path));
                    return;
                }
            }

            using (DirectoryEntry web = new DirectoryEntry("IIS://" + this.MachineName + "/W3SVC"))
            {
                ComWrapper ws = new ComWrapper(web.NativeObject);
                try
                {
                    ws.CallMethod("AddExtensionFile", new object[] { this.Path, this.permission == ExtensionPermission.Allowed, this.GroupId, this.Deletable, this.Description });
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(string.Format(CultureInfo.InvariantCulture, "Unable to create the extension: {0}", this.Path), ex);
                }
            }
        }
    }
}