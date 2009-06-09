//-----------------------------------------------------------------------
// <copyright file="FindUnder.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>FindFiles</i> (<b>Required: </b> Path <b>Optional: </b>Recursive, SearchPattern <b>Output: </b>FoundItems)</para>
    /// <para><i>FindDirectories</i> (<b>Required: </b> Path <b>Optional: </b>Recursive, SearchPattern <b>Output: </b>FoundItems)</para>
    /// <para><i>FindFilesAndDirectories</i> (<b>Required: </b> Path <b>Optional: </b>Recursive, SearchPattern <b>Output: </b>FoundItems)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" DefaultTargets="Demo" ToolsVersion="3.5">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Demo">
    ///         <!-- Only finds files -->
    ///         <MSBuild.ExtensionPack.FileSystem.FindUnder TaskAction="FindFiles" Path="$(MSBuildProjectDirectory)">
    ///             <Output ItemName="AllFoundFiles" TaskParameter="FoundItems"/>
    ///         </MSBuild.ExtensionPack.FileSystem.FindUnder>
    ///         <Message Text="===== Found Files =====" Importance="high"/>
    ///         <Message Text="AllFoundFiles:%0d%0a@(AllFoundFiles,'%0d%0a')"/>
    ///         <!-- Only finds directories -->
    ///         <MSBuild.ExtensionPack.FileSystem.FindUnder TaskAction="FindDirectories" Path="$(MSBuildProjectDirectory)\..\">
    ///             <Output ItemName="AllFoundDirectories" TaskParameter="FoundItems"/>
    ///         </MSBuild.ExtensionPack.FileSystem.FindUnder>
    ///         <Message Text="===== Found Directories =====" Importance="high"/>
    ///         <Message Text="AllFoundDirectories:%0d%0a@(AllFoundDirectories,'%0d%0a')"/>
    ///         <!-- Find both files and directories -->
    ///         <MSBuild.ExtensionPack.FileSystem.FindUnder TaskAction="FindFilesAndDirectories" Path="$(MSBuildProjectDirectory)\..\">
    ///             <Output ItemName="AllFoundItems" TaskParameter="FoundItems"/>
    ///         </MSBuild.ExtensionPack.FileSystem.FindUnder>
    ///         <Message Text="===== Found Files and Directories =====" Importance="high"/>
    ///         <Message Text="AllFoundItems:%0d%0a@(AllFoundItems,'%0d%0a')"/>
    ///         <!-- Find both files with SearchPattern = "F*" -->
    ///         <MSBuild.ExtensionPack.FileSystem.FindUnder TaskAction="FindFiles" Path="$(MSBuildProjectDirectory)\..\" SearchPattern="F*">
    ///             <Output ItemName="AllFilesStartingWithF" TaskParameter="FoundItems"/>
    ///         </MSBuild.ExtensionPack.FileSystem.FindUnder>
    ///         <Message Text="===== Found Files Starting with 'F' =====" Importance="high"/>
    ///         <Message Text="AllFilesStartingWithF:%0d%0a@(AllFilesStartingWithF,'%0d%0a')"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.3.0/html/ff5a6027-dc80-e7ef-87cd-3c88d9df9492.htm")]
    public class FindUnder : BaseTask
    {
        private const string FindFilesTaskAction = "FindFiles";
        private const string FindDirectoriesTaskAction = "FindDirectories";
        private const string FindFilesAndDirectoriesTaskAction = "FindFilesAndDirectories";
        private string searchPattern = "*";
        private bool recursive = true;

        [Required]
        [DropdownValue(FindFilesTaskAction)]
        [DropdownValue(FindDirectoriesTaskAction)]
        [DropdownValue(FindFilesAndDirectoriesTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets whether the File search is recursive. Default is true
        /// </summary>
        [TaskAction(FindFilesTaskAction, false)]
        [TaskAction(FindFilesAndDirectoriesTaskAction, false)]
        public bool Recursive
        {
            get { return this.recursive; }
            set { this.recursive = value; }
        }

        /// <summary>
        /// The path that the <c>FindUnder</c> will be executed against.
        /// This is a <b>Required</b> value.
        /// </summary>
        [Required]
        [TaskAction(FindFilesTaskAction, true)]
        [TaskAction(FindDirectoriesTaskAction, true)]
        [TaskAction(FindFilesAndDirectoriesTaskAction, true)]
        public ITaskItem Path { get; set; }

        /// <summary>
        /// The list of items (files and or directories) which were found.
        /// </summary>
        [Output]
        public ITaskItem[] FoundItems { get; set; }

        /// <summary>
        /// This in an optional input property. This will set the <c>SearchPattern</c>
        /// to be used in the search.<br/>
        /// The default value for this is <c>"*"</c>;<br/>
        /// This value is passed to either the System.IO.DirectoryInfo.GetDirectories method and/or the
        /// System.IO.FileInfo.GetFiles method. See that documentation for usage guidlines.
        /// </summary>
        [TaskAction(FindFilesTaskAction, false)]
        [TaskAction(FindDirectoriesTaskAction, false)]
        [TaskAction(FindFilesAndDirectoriesTaskAction, false)]
        public string SearchPattern
        {
            get { return this.searchPattern; }
            set { this.searchPattern = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating if files should be included in the result.<br/>
        /// The default value for this is <c>false</c>.<br/>
        /// Both <c>FindFiles</c> and <c>FindDirectories</c> cannot be <c>false</c>, atleast
        /// one <b>must</b> be <c>true</c>.
        /// </summary>
        /// <value><c>true</c> if file should be included in the find result; otherwise, <c>false</c>.</value>
        protected bool FindFiles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if directories should be included in the result.<br/>
        /// The default value for this is <c>false</c>.
        /// Both <c>FindFiles</c> and <c>FindDirectories</c> cannot be <c>false</c>, atleast
        /// one <b>must</b> be <c>true</c>.
        /// </summary>
        /// <value><c>true</c> if directories should be included in the find result; otherwise, <c>false</c>.</value>
        protected bool FindDirectories { get; set; }
        
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case FindFilesTaskAction:
                    this.FindFiles = true;
                    this.FindDirectories = false;
                    break;
                case FindDirectoriesTaskAction:
                    this.FindFiles = false;
                    this.FindDirectories = true;
                    break;
                case FindFilesAndDirectoriesTaskAction:
                    this.FindFiles = true;
                    this.FindDirectories = true;
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }

            string fullPath = this.Path.GetMetadata("Fullpath");
            Log.LogMessage(string.Format(CultureInfo.CurrentUICulture, "Searching under path [{0}]", fullPath), null);
            if (string.IsNullOrEmpty(fullPath) || !Directory.Exists(fullPath))
            {
                Log.LogError(string.Format(CultureInfo.CurrentUICulture, "Path specified {0} doesn't exist", fullPath));
                return;
            }

            if (!this.FindFiles && !this.FindDirectories)
            {
                Log.LogError("Either FindFiles or FindDirectories must be true");
                return;
            }

            DirectoryInfo dir = new DirectoryInfo(fullPath);
            FileInfo[] files = new FileInfo[0];
            DirectoryInfo[] subDirs = new DirectoryInfo[0];

            if (this.FindFiles)
            {
                files = this.Recursive ? dir.GetFiles(this.SearchPattern, SearchOption.AllDirectories) : dir.GetFiles(this.SearchPattern, SearchOption.TopDirectoryOnly);
            }

            if (this.FindDirectories)
            {
                subDirs = this.Recursive ? dir.GetDirectories(this.SearchPattern, SearchOption.AllDirectories) : dir.GetDirectories(this.SearchPattern, SearchOption.TopDirectoryOnly);
            }

            List<ITaskItem> items = new List<ITaskItem>();
            foreach (FileInfo fileInfo in files)
            {
                items.Add(new TaskItem(fileInfo.FullName));
            }

            foreach (DirectoryInfo dirInfo in subDirs)
            {
                TaskItem item = new TaskItem(dirInfo.FullName);
                item.SetMetadata("DirectoryName", dirInfo.Name);
                items.Add(item);
            }

            this.FoundItems = items.ToArray();
        }
    }
}