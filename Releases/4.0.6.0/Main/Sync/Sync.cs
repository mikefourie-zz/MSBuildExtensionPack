//-----------------------------------------------------------------------
// <copyright file="Sync.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Synchronization;
    using Microsoft.Synchronization.Files;

    /// <summary>
    /// Uses the Microsoft Sync Framework to provide folder synchronisation
    /// <para/>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>SyncFolders</i> (<b>Required: </b>Destination, Source <b>Optional: </b>IdFileName, ShowOutput, ExclusionFilters, SyncOptions, Direction)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
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
    ///             <DontSync Include="c:\ASource\*.txt"/>
    ///             <Options Include="ExplicitDetectChanges"/>
    ///             <Options Include="RecycleConflictLoserFiles"/>
    ///         </ItemGroup>
    ///         <!-- Sync two folders and specify an exclusion filter -->
    ///         <MSBuild.ExtensionPack.FileSystem.Sync TaskAction="SyncFolders" ExclusionFilters="@(DontSync)" Source="c:\ASource" Destination="C:\ADest"/>
    ///         <!-- Sync two folders and specify sync options -->
    ///         <MSBuild.ExtensionPack.FileSystem.Sync TaskAction="SyncFolders" SyncOptions="@(Options)" ShowOutput="true" Source="c:\ASource2" Destination="C:\ADest2"/>
    ///     </Target>   
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/4.0.5.0/html/ddceb2b2-f01b-6e3b-c0dd-28d0a1c8957e.htm")]
    public class Sync : BaseTask
    {
        private const string SyncFoldersTaskAction = "SyncFolders";
        private FileSyncOptions syncOptions = FileSyncOptions.ExplicitDetectChanges | FileSyncOptions.RecycleDeletedFiles;
        private SyncDirectionOrder direction = SyncDirectionOrder.UploadAndDownload;
        private string idFileName = "File.ID";
        private bool showOutput = true;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(SyncFoldersTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Set the ID file name. Defaults to "File.ID"
        /// </summary>
        [TaskAction(SyncFoldersTaskAction, false)]
        public string IdFileName
        {
            get { return this.idFileName; }
            set { this.idFileName = value; }
        }

        /// <summary>
        /// Sets a value indicating whether to ShowOutput. Default is true
        /// </summary>
        [TaskAction(SyncFoldersTaskAction, false)]
        public bool ShowOutput
        {
            get { return this.showOutput; }
            set { this.showOutput = value; }
        }

        /// <summary>
        /// Set the ExclusionFilters collection
        /// </summary>
        [TaskAction(SyncFoldersTaskAction, false)]
        public ITaskItem[] ExclusionFilters { get; set; }

        /// <summary>
        /// Set the Source to synchronise from
        /// </summary>
        [Required]
        [TaskAction(SyncFoldersTaskAction, true)]
        public ITaskItem Source { get; set; }

        /// <summary>
        /// Set the Destination to synchronise to
        /// </summary>
        [Required]
        [TaskAction(SyncFoldersTaskAction, true)]
        public ITaskItem Destination { get; set; }

        /// <summary>
        /// Set the SyncOptions collection. Default is ExplicitDetectChanges | RecycleDeletedFiles
        /// </summary>
        [TaskAction(SyncFoldersTaskAction, false)]
        public ITaskItem[] SyncOptions { get; set; }

        /// <summary>
        /// Set the direction to sync. Default is UploadAndDownload
        /// </summary>
        [TaskAction(SyncFoldersTaskAction, false)]
        public string Direction
        {
            get { return this.direction.ToString(); }
            set { this.direction = (SyncDirectionOrder)Enum.Parse(typeof(SyncDirectionOrder), value); }
        }

        /// <summary>
        /// InternalExecute
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            // Check that the Source exists
            if (!Directory.Exists(this.Source.GetMetadata("FullPath")))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Source Folder does not exist: {0}", this.Source.GetMetadata("FullPath")));
                return;
            }

            // Set the sync options
            if (this.SyncOptions != null)
            {
                FileSyncOptions fso = this.SyncOptions.Aggregate(new FileSyncOptions(), (current, opt) => current | (FileSyncOptions)Enum.Parse(typeof(FileSyncOptions), opt.ItemSpec));
                this.syncOptions = fso;
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "SyncOptions set: {0}", this.syncOptions));
            }

            switch (this.TaskAction)
            {
                case "SyncFolders":
                    this.SyncFolders();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private static Guid GetSyncId(string idFilePath)
        {
            if (File.Exists(idFilePath))
            {
                using (StreamReader sr = File.OpenText(idFilePath))
                {
                    string strGuid = sr.ReadLine();
                    if (!string.IsNullOrEmpty(strGuid))
                    {
                        return new Guid(strGuid);
                    }
                }
            }

            using (FileStream idFile = File.Open(idFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                StreamWriter sw = null;
                Guid replicaId;
                try
                {
                    sw = new StreamWriter(idFile);
                    replicaId = Guid.NewGuid();
                    sw.WriteLine(replicaId.ToString("D"));
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Close();
                    }
                }

                return replicaId;
            }
        }

        private void DetectChanges(Guid syncId, string path, FileSyncScopeFilter filter)
        {
            using (FileSyncProvider provider = new FileSyncProvider(syncId, path, filter, this.syncOptions))
            {
                provider.DetectChanges();
            }
        }

        private void SyncFolders()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Syncing Folders: {0} and {1}. Direction: {2}", this.Source.GetMetadata("FullPath"), this.Destination.GetMetadata("FullPath"), this.Direction));
            if (!Directory.Exists(this.Destination.GetMetadata("FullPath")))
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Creating Destination Folder: {0}", this.Destination.GetMetadata("FullPath")));
                Directory.CreateDirectory(this.Destination.GetMetadata("FullPath"));    
            }

            Guid sourceSyncId = GetSyncId(Path.Combine(this.Source.GetMetadata("FullPath"), this.IdFileName));
            Guid destinationSyncId = GetSyncId(Path.Combine(this.Destination.GetMetadata("FullPath"), this.IdFileName));
            FileSyncScopeFilter filter = new FileSyncScopeFilter();

            // Exclude the IdFileName by default
            filter.FileNameExcludes.Add(this.IdFileName);

            // add any other exclusions
            if (this.ExclusionFilters != null)
            {
                foreach (ITaskItem exfilter in this.ExclusionFilters)
                {
                    FileInfo f = new FileInfo(exfilter.ItemSpec);
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Adding ExclusionFilter: {0}", f.Name));
                    filter.FileNameExcludes.Add(f.Name);
                }
            }

            // Detect Changes
            this.DetectChanges(sourceSyncId, this.Source.GetMetadata("FullPath"), filter);
            this.DetectChanges(destinationSyncId, this.Destination.GetMetadata("FullPath"), filter);

            // Synchronise
            this.SyncFiles(sourceSyncId, destinationSyncId, filter);
        }

        private void SyncFiles(Guid sourceSyncId, Guid destinationSyncId, FileSyncScopeFilter filter)
        {
            using (FileSyncProvider sourceProvider = new FileSyncProvider(sourceSyncId, this.Source.GetMetadata("FullPath"), filter, this.syncOptions))
            {
                using (FileSyncProvider destinationProvider = new FileSyncProvider(destinationSyncId, this.Destination.GetMetadata("FullPath"), filter, this.syncOptions))
                {
                    if (this.ShowOutput)
                    {
                        // Hook up some events so the user can see what is happening
                        destinationProvider.AppliedChange += this.OnAppliedChange;
                        destinationProvider.SkippedChange += this.OnSkippedChange;
                        sourceProvider.AppliedChange += this.OnAppliedChange;
                        sourceProvider.SkippedChange += this.OnSkippedChange;
                    }

                    SyncOrchestrator agent = new SyncOrchestrator { LocalProvider = sourceProvider, RemoteProvider = destinationProvider, Direction = this.direction };
                    agent.Synchronize();
                }
            }
        }
 
        private void OnAppliedChange(object sender, AppliedChangeEventArgs args)
        {
            switch (args.ChangeType)
            {
                case ChangeType.Create:
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "{0} : {1}", args.ChangeType.ToString().ToUpper(CultureInfo.CurrentUICulture), args.NewFilePath));
                    break;
                case ChangeType.Delete:
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "{0} : {1}", args.ChangeType.ToString().ToUpper(CultureInfo.CurrentUICulture), args.OldFilePath));
                    break;
                case ChangeType.Update:
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "{0} : {1}", args.ChangeType.ToString().ToUpper(CultureInfo.CurrentUICulture), args.OldFilePath));
                    break;
                case ChangeType.Rename:
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "{0} : {1}", args.ChangeType.ToString().ToUpper(CultureInfo.CurrentUICulture), args.NewFilePath));
                    break;
            }
        }

        private void OnSkippedChange(object sender, SkippedChangeEventArgs args)
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "SKIPPED {0} for {1}", args.ChangeType.ToString().ToUpper(CultureInfo.CurrentUICulture), !string.IsNullOrEmpty(args.CurrentFilePath) ? args.CurrentFilePath : args.NewFilePath));
            if (args.Exception != null)
            {
                this.Log.LogErrorFromException(args.Exception, this.LogExceptionStack, true, null);
            }
        }
    }
}