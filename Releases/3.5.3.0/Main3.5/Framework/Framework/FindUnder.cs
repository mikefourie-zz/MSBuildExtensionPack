//-----------------------------------------------------------------------
// <copyright file="Guid.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System.IO;
    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>FindFiles</i></para>
    /// <para><i>FindDirectories</i></para>
    /// <para><i>FindFilesAndDirectories</i></para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// For any of these TaskAction values the <c>Path</c> parameter is <b>Required</b>.
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003" DefaultTargets="Demo" ToolsVersion="3.5">
    /// 
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///   </PropertyGroup>
    ///   
    ///   <Import Project="$(TPath)"/>
    /// 
    ///   <ItemGroup>
    ///     <Server Include="dev01;dev02;dev03">
    ///       <DbServer>dev-db01</DbServer>
    ///     </Server>
    ///   </ItemGroup>
    /// 
    ///   <Target Name="Demo">
    ///     <!-- Only finds files -->
    ///     <MSBuild.ExtensionPack.Framework.FindUnder
    ///                   TaskAction="FindFiles"
    ///                   Path="$(MSBuildProjectDirectory)">
    ///       <Output ItemName="AllFoundFiles" TaskParameter="FoundItems"/>
    ///     </MSBuild.ExtensionPack.Framework.FindUnder>
    /// 
    ///     <Message Text="===== Found Files =====" Importance="high"/>
    ///     <Message Text="AllFoundFiles:%0d%0a@(AllFoundFiles,'%0d%0a')"/>
    /// 
    ///     <!-- Only finds directories -->
    ///     <MSBuild.ExtensionPack.Framework.FindUnder
    ///                   TaskAction="FindDirectories"
    ///                   Path="$(MSBuildProjectDirectory)\..\">
    ///       <Output ItemName="AllFoundDirectories" TaskParameter="FoundItems"/>
    ///     </MSBuild.ExtensionPack.Framework.FindUnder>
    /// 
    ///     <Message Text="===== Found Directories =====" Importance="high"/>
    ///     <Message Text="AllFoundDirectories:%0d%0a@(AllFoundDirectories,'%0d%0a')"/>
    /// 
    ///     <!-- Find both files and directories -->
    ///     <MSBuild.ExtensionPack.Framework.FindUnder
    ///                   TaskAction="FindFilesAndDirectories"
    ///                   Path="$(MSBuildProjectDirectory)\..\">
    ///       <Output ItemName="AllFoundItems" TaskParameter="FoundItems"/>
    ///     </MSBuild.ExtensionPack.Framework.FindUnder>
    /// 
    ///     <Message Text="===== Found Files and Directories =====" Importance="high"/>
    ///     <Message Text="AllFoundItems:%0d%0a@(AllFoundItems,'%0d%0a')"/>
    ///     
    ///    <!-- Find both files with SearchPattern = "F*" -->
    ///     <MSBuild.ExtensionPack.Framework.FindUnder
    ///                   TaskAction="FindFiles"
    ///                   Path="$(MSBuildProjectDirectory)\..\"
    ///                   SearchPattern="F*">
    ///       <Output ItemName="AllFilesStartingWithF" TaskParameter="FoundItems"/>
    ///     </MSBuild.ExtensionPack.Framework.FindUnder>
    /// 
    ///     <Message Text="===== Found Files Starting with 'F' =====" Importance="high"/>
    ///     <Message Text="AllFilesStartingWithF:%0d%0a@(AllFilesStartingWithF,'%0d%0a')"/>	
    ///   </Target>
    /// 
    /// </Project>
    /// ]]></code>
    /// </example>
    [HelpUrl("TODO")]
    public class FindUnder : BaseTask
    {
        private const string DefaultSearchPattern = "*";

        private const string FindFilesTaskAction = "FindFiles";
        private const string FindDirectoriesTaskAction = "FindDirectories";
        private const string FindFilesAndDirectoriesTaskAction = "FindFilesAndDirectories";

        public FindUnder()
            : base()
        {
            FindFiles = false;
            SearchPattern = DefaultSearchPattern;
        }



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
        /// The path that the <c>FindUnder</c> will be executed against.
        /// This is a <b>Required</b> value.
        /// </summary>
        [Required]
        public ITaskItem Path
        { get; set; }
        /// <summary>
        /// The list of items (files and or directories) which were found.
        /// </summary>
        [Output]
        public ITaskItem[] FoundItems
        { get; set; }
        /// <summary>
        /// This in an optional input property. This will set the <c>SearchPattern</c>
        /// to be used in the search.<br/>
        /// The default value for this is <c>"*"</c>;<br/>
        /// This value is passed to either the System.IO.DirectoryInfo.GetDirectories method and/or the
        /// System.IO.FileInfo.GetFiles method. See that documentation for usage guidlines.
        /// <see cref="T:System.FileInfo.GetFiles"/>
        /// <see cref="T:System.FileInfo.GetDirectories"/>
        /// </summary>
        public string SearchPattern
        { get; set; }


        #region Non-public properties
        /// <summary>
        /// Gets or sets a value indicating if files should be included in the result.<br/>
        /// The default value for this is <c>false</c>.<br/>
        /// Both <c>FindFiles</c> and <c>FindDirectories</c> cannot be <c>false</c>, atleast
        /// one <b>must</b> be <c>true</c>.
        /// </summary>
        /// <value><c>true</c> if file should be included in the find result; otherwise, <c>false</c>.</value>
        protected bool FindFiles
        { get; set; }
        /// <summary>
        /// Gets or sets a value indicating if directories should be included in the result.<br/>
        /// The default value for this is <c>false</c>.
        /// Both <c>FindFiles</c> and <c>FindDirectories</c> cannot be <c>false</c>, atleast
        /// one <b>must</b> be <c>true</c>.
        /// </summary>
        /// <value><c>true</c> if directories should be included in the find result; otherwise, <c>false</c>.</value>
        protected bool FindDirectories
        { get; set; }
        #endregion
        


        protected override void InternalExecute()
        {
            if (string.Compare(FindFilesTaskAction, TaskAction, StringComparison.OrdinalIgnoreCase) == 0)
            {
                FindFiles = true;
                FindDirectories = false;
            }
            else if (string.Compare(FindDirectoriesTaskAction, TaskAction, StringComparison.OrdinalIgnoreCase) == 0)
            {
                FindFiles = false;
                FindDirectories = true;
            }
            else if (string.Compare(FindFilesAndDirectoriesTaskAction, TaskAction, StringComparison.OrdinalIgnoreCase) == 0)
            {
                FindFiles = true;
                FindDirectories = true;
            }
            else
            {
                string message = string.Format("Unknown TaskAction [{0}]",TaskAction);
                throw new ArgumentException(message);
            }

            string fullPath = this.Path.GetMetadata("Fullpath");
            Log.LogMessage(string.Format("Searching under path [{0}]", fullPath), null);
            if (string.IsNullOrEmpty(fullPath) || !Directory.Exists(fullPath))
            {
                string message = string.Format("Path specified {0} doesn't exist", fullPath);
                throw new ArgumentException(message);
            }
            if (!FindFiles && !FindDirectories)
            {
                string message = "Either FindFiles or FindDirectories must be true";
                throw new ArgumentException(message);
            }

            DirectoryInfo dir = new DirectoryInfo(fullPath);

            FileInfo[] files = new FileInfo[0];
            DirectoryInfo[] subDirs = new DirectoryInfo[0];

            if (FindFiles)
            {
                files = dir.GetFiles(SearchPattern, SearchOption.AllDirectories);
            }
            if (FindDirectories)
            {
                subDirs = dir.GetDirectories(SearchPattern, SearchOption.AllDirectories);
            }

            List<ITaskItem> items = new List<ITaskItem>();
            foreach (FileInfo fInfo in files)
            {
                items.Add(new TaskItem(fInfo.FullName));
            }
            foreach (DirectoryInfo dInfo in subDirs)
            {
                TaskItem item = new TaskItem(dInfo.FullName);
                item.SetMetadata("DirectoryName", dInfo.Name);
                items.Add(item);
            }

            this.FoundItems = items.ToArray();
        }
    }
}