//-----------------------------------------------------------------------
// <copyright file="Zip.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
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
    /// <para><i>AddFiles</i> (<b>Required: </b> ZipFileName, CompressFiles or Path <b>Optional: </b>CompressionLevel, MaxOutputSegmentSize, Password; RemoveRoot, UseZip64WhenSaving) Existing files will be updated</para>
    /// <para><i>Create</i> (<b>Required: </b> ZipFileName, CompressFiles or Path <b>Optional: </b>CompressionLevel, MaxOutputSegmentSize, Password; RemoveRoot, UseZip64WhenSaving, PreserveAttributes)</para>
    /// <para><i>Extract</i> (<b>Required: </b> ZipFileName, ExtractPath <b>Optional:</b> Password)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// <para/>
    /// This task uses http://dotnetzip.codeplex.com v1.9.1.8 for compression.
    /// <para/>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
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
    public class Zip : BaseTask
    {
        private const string CreateTaskAction = "Create";
        private const string ExtractTaskAction = "Extract";
        private const string AddFilesTaskAction = "AddFiles";
        private Ionic.Zlib.CompressionLevel compressLevel = Ionic.Zlib.CompressionLevel.Default;
        private Zip64Option useZip64WhenSaving = Zip64Option.Default;
        private bool preserveAttributes = true;

        /// <summary>
        /// Sets the root to remove from the zip path. Note that this should be part of the file to compress path, not the target path of the ZipFileName
        /// </summary>
        public ITaskItem RemoveRoot { get; set; }

        /// <summary>
        /// Sets the files to Compress
        /// </summary>
        public ITaskItem[] CompressFiles { get; set; }

        /// <summary>
        /// Sets the Path to Zip.
        /// </summary>
        public ITaskItem CompressPath { get; set; }

        /// <summary>
        /// Sets the name of the Zip File
        /// </summary>
        [Required]
        public ITaskItem ZipFileName { get; set; }

        /// <summary>
        /// Path to extract the zip file to
        /// </summary>
        public ITaskItem ExtractPath { get; set; }

        /// <summary>
        /// Sets the Password to be used
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Sets the CompressionLevel to use. Default is Default, also supports BestSpeed and BestCompression
        /// </summary>
        public string CompressionLevel
        {
            get { return this.compressLevel.ToString(); }
            set { this.compressLevel = (Ionic.Zlib.CompressionLevel)Enum.Parse(typeof(Ionic.Zlib.CompressionLevel), value); }
        }

        /// <summary>
        /// Sets the maximum output segment size, which typically results in a split archive (an archive split into multiple files).
        /// This value is not required and if not set or set to 0 the resulting archive will not be split.
        /// For more details see the DotNetZip documentation.
        /// </summary>
        public int MaxOutputSegmentSize { get; set; }

        /// <summary>
        /// Sets the UseZip64WhenSaving output of the DotNetZip library.
        /// For more details see the DotNetZip documentation.
        /// </summary>
        public string UseZip64WhenSaving
        {
            get { return this.useZip64WhenSaving.ToString(); }
            set { this.useZip64WhenSaving = (Zip64Option)Enum.Parse(typeof(Zip64Option), value, true); }
        }

        /// <summary>
        /// This is only applicable when the <b>TaskAction</b> is <i>Create</i> or <i>AddFiles</i>. Specifies whether file (folder) attributes like <i>Hidden</i> or <i>Read-only</i>
        /// should be left intact during adding to the archive. The default is <c>true</c>.
        /// </summary>
        public bool PreserveAttributes
        {
            get { return this.preserveAttributes; }
            set { this.preserveAttributes = value; }
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
                        ZipEntry updatedEntry;
                        if (this.RemoveRoot != null)
                        {
                            string location = (f.GetMetadata("RootDir") + f.GetMetadata("Directory")).Replace(this.RemoveRoot.GetMetadata("FullPath"), string.Empty);
                            updatedEntry = zip.UpdateFile(f.GetMetadata("FullPath"), location);
                        }
                        else
                        {
                            updatedEntry = zip.UpdateFile(f.GetMetadata("FullPath"));
                        }

                        if (!this.preserveAttributes)
                        {
                            updatedEntry.Attributes = FileAttributes.Normal;
                        }
                    }

                    if (this.MaxOutputSegmentSize > 0)
                    {
                        zip.MaxOutputSegmentSize = this.MaxOutputSegmentSize;
                    }

                    zip.UseZip64WhenSaving = this.useZip64WhenSaving;
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

                    ZipEntry archiveEntry;
                    if (this.RemoveRoot != null)
                    {
                        DirectoryInfo d = new DirectoryInfo(this.CompressPath.ItemSpec);
                        string location = d.FullName.Replace(this.RemoveRoot.GetMetadata("FullPath"), string.Empty);
                        archiveEntry = zip.AddDirectory(this.CompressPath.ItemSpec, location);
                    }
                    else
                    {
                        archiveEntry = zip.UpdateDirectory(this.CompressPath.ItemSpec);
                    }

                    if (!this.preserveAttributes)
                    {
                        archiveEntry.Attributes = FileAttributes.Normal;
                    }

                    if (this.MaxOutputSegmentSize > 0)
                    {
                        zip.MaxOutputSegmentSize = this.MaxOutputSegmentSize;
                    }

                    zip.UseZip64WhenSaving = this.useZip64WhenSaving;
                    zip.Save();
                }
            }
            else
            {
                Log.LogError("CompressFiles or CompressPath must be specified");
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
                        ZipEntry addedEntry;
                        if (this.RemoveRoot != null)
                        {
                            string location = (f.GetMetadata("RootDir") + f.GetMetadata("Directory")).Replace(this.RemoveRoot.GetMetadata("FullPath"), string.Empty);
                            addedEntry = zip.AddFile(f.GetMetadata("FullPath"), location);
                        }
                        else
                        {
                            addedEntry = zip.AddFile(f.GetMetadata("FullPath"));
                        }

                        if (!this.preserveAttributes)
                        {
                            addedEntry.Attributes = FileAttributes.Normal;
                        }
                    }

                    if (this.MaxOutputSegmentSize > 0)
                    {
                        zip.MaxOutputSegmentSize = this.MaxOutputSegmentSize;
                    }

                    zip.UseZip64WhenSaving = this.useZip64WhenSaving;
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

                    ZipEntry addedDirectory;
                    if (this.RemoveRoot != null)
                    {
                        DirectoryInfo d = new DirectoryInfo(this.CompressPath.ItemSpec);
                        string location = d.FullName.Replace(this.RemoveRoot.GetMetadata("FullPath"), string.Empty);
                        addedDirectory = zip.AddDirectory(this.CompressPath.ItemSpec, location);
                    }
                    else
                    {
                        DirectoryInfo d = new DirectoryInfo(this.CompressPath.ItemSpec);
                        addedDirectory = zip.AddDirectory(this.CompressPath.ItemSpec, d.Name);
                    }

                    if (!this.preserveAttributes)
                    {
                        addedDirectory.Attributes = FileAttributes.Normal;
                    }

                    if (this.MaxOutputSegmentSize > 0)
                    {
                        zip.MaxOutputSegmentSize = this.MaxOutputSegmentSize;
                    }

                    zip.UseZip64WhenSaving = this.useZip64WhenSaving;
                    zip.Save(this.ZipFileName.ItemSpec);
                }
            }
            else
            {
                Log.LogError("CompressFiles or CompressPath must be specified");
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