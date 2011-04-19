//-----------------------------------------------------------------------
// <copyright file="WshShell.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.Globalization;
    using System.IO;
    using IWshRuntimeLibrary;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CreateShortcut</i> (<b>Required: </b> Name, FilePath <b>Optional: </b>Arguments, ShortcutPath, Description, WorkingDirectory, IconLocation)</para>
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
    ///         <!-- Create a shortcut -->
    ///         <MSBuild.ExtensionPack.Computer.WshShell TaskAction="CreateShortcut" Name="My Calculator.lnk" FilePath="C:\Windows\System32\calc.exe"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.9.0/html/9a20ad72-05ea-dd67-7070-94d265a35b80.htm")]
    public class WshShell : BaseTask
    {
        private const string CreateShortcutTaskAction = "CreateShortcut";

        [DropdownValue(CreateShortcutTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the FilePath
        /// </summary>
        [TaskAction(CreateShortcutTaskAction, true)]
        public string FilePath { get; set; }

        /// <summary>
        /// Sets the ShortcutPath. For CreateShortcut defaults defaults to Desktop of the current user
        /// </summary>
        [TaskAction(CreateShortcutTaskAction, true)]
        public string ShortcutPath { get; set; }

        /// <summary>
        /// Sets the Name
        /// </summary>
        [TaskAction(CreateShortcutTaskAction, true)]
        public string Name { get; set; }

        /// <summary>
        /// Sets the IconLocation
        /// </summary>
        [TaskAction(CreateShortcutTaskAction, true)]
        public string IconLocation { get; set; }

        /// <summary>
        /// Sets the Description. For CreateShortcut defaults to 'Launch [Name]'
        /// </summary>
        [TaskAction(CreateShortcutTaskAction, true)]
        public string Description { get; set; }

        /// <summary>
        /// Sets the Arguments for the shortcut
        /// </summary>
        [TaskAction(CreateShortcutTaskAction, true)]
        public string Arguments { get; set; }

        /// <summary>
        /// Sets the WorkingDirectory
        /// </summary>
        [TaskAction(CreateShortcutTaskAction, true)]
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Sets the WindowStyle.
        /// <para/>
        /// 1 - Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position.
        /// <para/>
        /// 3 - Activates the window and displays it as a maximized window.
        /// <para/>
        /// 7 - Minimizes the window and activates the next top-level window.
        /// </summary>
        [TaskAction(CreateShortcutTaskAction, true)]
        public int WindowStyle { get; set; }
        
        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case CreateShortcutTaskAction:
                    this.CreateShortcut();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void CreateShortcut()
        {
            if (string.IsNullOrEmpty(this.FilePath))
            {
                Log.LogError("FilePath is requried.");
                return;
            }

            if (string.IsNullOrEmpty(this.Name))
            {
                Log.LogError("Name is requried.");
                return;
            }

            if (string.IsNullOrEmpty(this.ShortcutPath))
            {
                this.ShortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            if (string.IsNullOrEmpty(this.Description))
            {
                this.Description = string.Format(CultureInfo.InvariantCulture, "Launch {0}", this.Name.Replace(".lnk", string.Empty));
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating Shortcut: {0}", Path.Combine(this.ShortcutPath, this.Name)));
            WshShellClass shell = new WshShellClass();
            IWshShortcut shortcutToCreate = shell.CreateShortcut(Path.Combine(this.ShortcutPath, this.Name)) as IWshShortcut;
            if (shortcutToCreate != null)
            {
                shortcutToCreate.TargetPath = this.FilePath;
                shortcutToCreate.Description = this.Description;
                
                if (!string.IsNullOrEmpty(this.Arguments))                   
                {
                    shortcutToCreate.Arguments = this.Arguments;
                }

                if (!string.IsNullOrEmpty(this.IconLocation))
                {
                    if (!System.IO.File.Exists(this.IconLocation))
                    {
                        Log.LogError(string.Format(CultureInfo.InvariantCulture, "IconLocation: {0} does not exist.", this.IconLocation));
                        return;
                    }

                    shortcutToCreate.IconLocation = this.IconLocation;
                }

                if (!string.IsNullOrEmpty(this.WorkingDirectory))
                {
                    if (!System.IO.Directory.Exists(this.WorkingDirectory))
                    {
                        Log.LogError(string.Format(CultureInfo.InvariantCulture, "WorkingDirectory: {0} does not exist.", this.WorkingDirectory));
                        return;
                    }

                    shortcutToCreate.WorkingDirectory = this.WorkingDirectory;
                }

                if (this.WindowStyle > 0)
                {
                    shortcutToCreate.WindowStyle = this.WindowStyle;
                }
                
                shortcutToCreate.Save();
            }
        }
    }
}