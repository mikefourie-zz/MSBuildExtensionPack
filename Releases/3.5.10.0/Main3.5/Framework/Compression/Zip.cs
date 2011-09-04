//-----------------------------------------------------------------------
// <copyright file="Zip.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
#pragma warning disable 618
namespace MSBuild.ExtensionPack.Compression
{
    using System;
    using System.Globalization;
    using System.IO;
    using Ionic.Zip;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddFiles</i> (<b>Required: </b> ZipFileName, CompressFiles or Path <b>Optional: </b>CompressionLevel, Password; RemoveRoot) Existing files will be updated</para>
    /// <para><i>Create</i> (<b>Required: </b> ZipFileName, CompressFiles or Path <b>Optional: </b>CompressionLevel, Password; RemoveRoot)</para>
    /// <para><i>Extract</i> (<b>Required: </b> ZipFileName, ExtractPath <b>Optional:</b> Password)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// <para/>
    /// This task uses http://dotnetzip.codeplex.com v1.9.1.8 for compression.
    /// <para/>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///   </PropertyGroup>
    ///   <Import Project="$(TPath)"/>
    ///   <Target Name="Default" DependsOnTargets="Sample1;Sample2"/>
    ///   <Target Name="Sample1">
    ///     <ItemGroup>
    ///       <!-- Set the collection of files to Zip-->
    ///       <FilesToZip Include="C:\Patches\**\*"/>
    ///     </ItemGroup>
    ///     <!-- Create a zip file based on the FilesToZip collection -->
    ///     <MSBuild.ExtensionPack.Compression.Zip TaskAction="Create" CompressFiles="@(FilesToZip)" ZipFileName="C:\newZipByFile.zip"/>
    ///     <MSBuild.ExtensionPack.Compression.Zip TaskAction="Create" Password="apassword" CompressionLevel="BestCompression" RemoveRoot="C:\Patches" CompressFiles="@(FilesToZip)" ZipFileName="C:\newZipByFileBestCompression.zip"/>
    ///     <!-- Create a zip file based on a Path -->
    ///     <MSBuild.ExtensionPack.Compression.Zip TaskAction="Create" CompressPath="C:\Patches" ZipFileName="C:\newZipByPath.zip"/>
    ///     <!-- Extract a zip file-->
    ///     <MSBuild.ExtensionPack.Compression.Zip TaskAction="Extract" ExtractPath="C:\aaa11\1" ZipFileName="C:\newZipByFile.zip"/>
    ///     <MSBuild.ExtensionPack.Compression.Zip TaskAction="Extract" ExtractPath="C:\aaa11\2" ZipFileName="C:\newZipByPath.zip"/>
    ///     <MSBuild.ExtensionPack.Compression.Zip TaskAction="Extract" Password="apassword"  ExtractPath="C:\aaa11\3" ZipFileName="C:\newZipByFileBestCompression.zip"/>
    ///   </Target>
    ///   <Target Name="Sample2">
    ///     <PropertyGroup>
    ///       <SourceDirectory>MotorData\</SourceDirectory>
    ///     </PropertyGroup>
    ///     <ItemGroup>
    ///       <Files Include="$(SourceDirectory)*" Exclude="$(SourceDirectory).XYZ\**\*">
    ///         <Group>Common</Group>
    ///       </Files>
    ///       <Files Include="$(SourceDirectory)Cars\*" Exclude="$(SourceDirectory)Cars\.XYZ\**\*">
    ///         <Group>Cars</Group>
    ///       </Files>
    ///       <Files Include="$(SourceDirectory)Trucks\*" Exclude="$(SourceDirectory)Trucks\.XYZ\**\*">
    ///         <Group>Trucks</Group>
    ///       </Files>
    ///     </ItemGroup>
    ///     <!-- Create the output folder -->
    ///     <ItemGroup>
    ///       <OutputDirectory Include="output\"/>
    ///     </ItemGroup>
    ///     <MakeDir Directories="@(OutputDirectory)"/>
    ///     <PropertyGroup>
    ///       <WorkingDir>%(OutputDirectory.Fullpath)</WorkingDir>
    ///     </PropertyGroup>
    ///     <!-- Zip files based on the group they belong to -->
    ///     <MSBuild.ExtensionPack.Compression.Zip TaskAction="Create" CompressFiles="@(Files)" ZipFileName="$(WorkingDir)%(Files.Group).zip"/>
    ///   </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    [HelpUrl("")]
    public class Zip : BaseTask
    {
        private const string CreateTaskAction = "Create";
        private const string ExtractTaskAction = "Extract";
        private const string AddFilesTaskAction = "AddFiles";
        private Ionic.Zlib.CompressionLevel compressLevel = Ionic.Zlib.CompressionLevel.Default;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(CreateTaskAction)]
        [DropdownValue(ExtractTaskAction)]
        [DropdownValue(AddFilesTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the root to remove from the zip path. Note that this should be part of the file to compress path, not the target path of the ZipFileName
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public ITaskItem RemoveRoot { get; set; }

        /// <summary>
        /// Sets the files to Compress
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public ITaskItem[] CompressFiles { get; set; }

        /// <summary>
        /// Sets the Path to Zip.
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public ITaskItem CompressPath { get; set; }

        /// <summary>
        /// Sets the name of the Zip File
        /// </summary>
        [Required]
        [TaskAction(CreateTaskAction, true)]
        [TaskAction(ExtractTaskAction, true)]
        [TaskAction(AddFilesTaskAction, true)]
        public ITaskItem ZipFileName { get; set; }

        /// <summary>
        /// Path to extract the zip file to
        /// </summary>
        [TaskAction(ExtractTaskAction, true)]
        public ITaskItem ExtractPath { get; set; }

        /// <summary>
        /// Sets the Password to be used
        /// </summary>
        [TaskAction(CreateTaskAction, true)]
        [TaskAction(ExtractTaskAction, true)]
        public string Password { get; set; }

        /// <summary>
        /// Sets the CompressionLevel to use. Default is Default, also supports BestSpeed and BestCompression
        /// </summary>
        [TaskAction(CreateTaskAction, true)]
        public string CompressionLevel
        {
            get { return this.compressLevel.ToString(); }
            set { this.compressLevel = (Ionic.Zlib.CompressionLevel)Enum.Parse(typeof(Ionic.Zlib.CompressionLevel), value); }
        }

        /// <summary>
        /// This is the main InternalExecute method that all tasks should implement
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            switch (this.TaskAction)
            {
                case CreateTaskAction:
                    this.Create();
                    break;
                case ExtractTaskAction:
                    this.Extract();
                    break;
                case AddFilesTaskAction:
                    this.AddFiles();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void AddFiles()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding files to ZipFile: {0}", this.ZipFileName));
            if (this.CompressFiles != null)
            {
                using (ZipFile zip = ZipFile.Read(this.ZipFileName.ItemSpec))
                {
                    zip.ParallelDeflateThreshold = -1;
                    zip.UseUnicodeAsNecessary = true;
                    zip.CompressionLevel = this.compressLevel;
                    if (!string.IsNullOrEmpty(this.Password))
                    {
                        zip.Password = this.Password;
                    }

                    foreach (ITaskItem f in this.CompressFiles)
                    {
                        if (this.RemoveRoot != null)
                        {
                            string location = (f.GetMetadata("RootDir") + f.GetMetadata("Directory")).Replace(this.RemoveRoot.GetMetadata("FullPath"), string.Empty);
                            zip.UpdateFile(f.GetMetadata("FullPath"), location);
                        }
                        else
                        {
                            zip.UpdateFile(f.GetMetadata("FullPath"));
                        }
                    }

                    zip.Save();
                }
            }
            else if (this.CompressPath != null)
            {
                using (ZipFile zip = ZipFile.Read(this.ZipFileName.ItemSpec))
                {
                    zip.ParallelDeflateThreshold = -1;
                    zip.UseUnicodeAsNecessary = true;
                    zip.CompressionLevel = this.compressLevel;
                    if (!string.IsNullOrEmpty(this.Password))
                    {
                        zip.Password = this.Password;
                    }

                    if (this.RemoveRoot != null)
                    {
                        DirectoryInfo d = new DirectoryInfo(this.CompressPath.ItemSpec);
                        string location = d.FullName.Replace(this.RemoveRoot.GetMetadata("FullPath"), string.Empty);
                        zip.AddDirectory(this.CompressPath.ItemSpec, location);
                    }
                    else
                    {
                        zip.UpdateDirectory(this.CompressPath.ItemSpec);
                    }

                    zip.Save();
                }
            }
            else
            {
                Log.LogError("CompressFiles or CompressPath must be specified");
                return;
            }
        }

        private void Create()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating ZipFile: {0}", this.ZipFileName));
            if (this.CompressFiles != null)
            {
                using (ZipFile zip = new ZipFile())
                {
                    zip.ParallelDeflateThreshold = -1;
                    zip.UseUnicodeAsNecessary = true;
                    zip.CompressionLevel = this.compressLevel;
                    if (!string.IsNullOrEmpty(this.Password))
                    {
                        zip.Password = this.Password;
                    }

                    foreach (ITaskItem f in this.CompressFiles)
                    {
                        if (this.RemoveRoot != null)
                        {
                            string location = (f.GetMetadata("RootDir") + f.GetMetadata("Directory")).Replace(this.RemoveRoot.GetMetadata("FullPath"), string.Empty);
                            zip.AddFile(f.GetMetadata("FullPath"), location);
                        }
                        else
                        {
                            zip.AddFile(f.GetMetadata("FullPath"));
                        }
                    }

                    zip.Save(this.ZipFileName.ItemSpec);
                }
            }
            else if (this.CompressPath != null)
            {
                using (ZipFile zip = new ZipFile())
                {
                    zip.ParallelDeflateThreshold = -1;
                    zip.UseUnicodeAsNecessary = true;
                    zip.CompressionLevel = this.compressLevel;
                    if (!string.IsNullOrEmpty(this.Password))
                    {
                        zip.Password = this.Password;
                    }

                    if (this.RemoveRoot != null)
                    {
                        DirectoryInfo d = new DirectoryInfo(this.CompressPath.ItemSpec);
                        string location = d.FullName.Replace(this.RemoveRoot.GetMetadata("FullPath"), string.Empty);
                        zip.AddDirectory(this.CompressPath.ItemSpec, location);
                    }
                    else
                    {
                        DirectoryInfo d = new DirectoryInfo(this.CompressPath.ItemSpec);
                        zip.AddDirectory(this.CompressPath.ItemSpec, d.Name);
                    }

                    zip.Save(this.ZipFileName.ItemSpec);
                }
            }
            else
            {
                Log.LogError("CompressFiles or CompressPath must be specified");
                return;
            }
        }

        private void Extract()
        {
            if (!File.Exists(this.ZipFileName.GetMetadata("FullPath")))
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "ZipFileName not found: {0}", this.ZipFileName));
                return;
            }

            if (string.IsNullOrEmpty(this.ExtractPath.GetMetadata("FullPath")))
            {
                Log.LogError("ExtractPath is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Extracting ZipFile: {0} to: {1}", this.ZipFileName, this.ExtractPath));

            using (ZipFile zip = ZipFile.Read(this.ZipFileName.GetMetadata("FullPath")))
            {
                if (!string.IsNullOrEmpty(this.Password))
                {
                    zip.Password = this.Password;
                }

                foreach (ZipEntry e in zip)
                {
                    e.Extract(this.ExtractPath.GetMetadata("FullPath"), ExtractExistingFileAction.OverwriteSilently);
                }
            }
        }
    }
}