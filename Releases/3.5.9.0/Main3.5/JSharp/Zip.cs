//-----------------------------------------------------------------------
// <copyright file="Zip.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Compression
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using java.util.zip;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Create</i> (<b>Required: </b> ZipFileName, CompressFiles or Path <b>Optional: </b>RemoveRoot)</para>
    /// <para><i>Extract</i> (<b>Required: </b> ZipFileName, ExtractPath)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// <para><b>This task requires:</b></para>
    ///     <para>Microsoft Visual J# 2.0 Redistributable Package – Second Edition (x86) or (x64)</para>
    ///     <para/>
    ///     <para>Please note that file attributes are not maintained when using these tasks.</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default" DependsOnTargets="Sample1;Sample2"/>
    ///     <Target Name="Sample1">
    ///         <ItemGroup>
    ///             <!-- Set the collection of files to Zip-->
    ///             <FilesToZip Include="C:\hotfixes\**\*"/>
    ///         </ItemGroup>
    ///         <!-- Create a zip file based on the FilesToZip collection -->
    ///         <MSBuild.ExtensionPack.Compression.Zip TaskAction="Create" CompressFiles="@(FilesToZip)" RemoveRoot="C:\hotfixes\" ZipFileName="C:\newZipByFile.zip"/>
    ///         <!-- Create a zip file based on a Path -->
    ///         <MSBuild.ExtensionPack.Compression.Zip TaskAction="Create" CompressPath="C:\hotfixes" RemoveRoot="C:\hotfixes\" ZipFileName="C:\newZipByPath.zip"/>
    ///         <!-- Extract a zip file-->
    ///         <MSBuild.ExtensionPack.Compression.Zip TaskAction="Extract" ExtractPath="C:\aaa11" ZipFileName="C:\newZipByPath.zip"/>
    ///     </Target>
    ///     <Target Name="Sample2">
    ///         <PropertyGroup>
    ///             <SourceDirectory>MotorData\</SourceDirectory>
    ///         </PropertyGroup>
    ///         <ItemGroup>
    ///             <Files Include="$(SourceDirectory)*" Exclude="$(SourceDirectory).XYZ\**\*">
    ///                 <Group>Common</Group>
    ///             </Files>
    ///             <Files Include="$(SourceDirectory)Cars\*" Exclude="$(SourceDirectory)Cars\.XYZ\**\*">
    ///                 <Group>Cars</Group>
    ///             </Files>
    ///             <Files Include="$(SourceDirectory)Trucks\*" Exclude="$(SourceDirectory)Trucks\.XYZ\**\*">
    ///                 <Group>Trucks</Group>
    ///             </Files>
    ///         </ItemGroup>
    ///         <!-- Create the output folder -->
    ///         <ItemGroup>
    ///             <OutputDirectory Include="output\"/>
    ///         </ItemGroup>
    ///         <MakeDir Directories="@(OutputDirectory)"/>
    ///         <PropertyGroup>
    ///             <WorkingDir>%(OutputDirectory.Fullpath)</WorkingDir>
    ///         </PropertyGroup>
    ///         <!-- Zip files based on the group they belong to -->
    ///         <MSBuild.ExtensionPack.Compression.Zip TaskAction="Create" CompressFiles="@(Files)" ZipFileName="$(WorkingDir)%(Files.Group).zip"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.9.0/html/f2118b59-554e-d745-5859-126a82b1df81.htm")]
    public class Zip : BaseTask
    {
        private const string CreateTaskAction = "Create";
        private const string ExtractTaskAction = "Extract";
        
        private ZipOutputStream zipOutStream;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(CreateTaskAction)]
        [DropdownValue(ExtractTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

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
        /// Sets the root to remove from the zip path. Note that this should be part of the file to compress path, not the target path of the ZipFileName.
        /// If this is not provided, you may get unexpected results.
        /// </summary>
        [TaskAction(CreateTaskAction, false)]
        public ITaskItem RemoveRoot { get; set; }

        /// <summary>
        /// Sets the name of the Zip File
        /// </summary>
        [Required]
        [TaskAction(CreateTaskAction, true)]
        [TaskAction(ExtractTaskAction, true)]
        public ITaskItem ZipFileName { get; set; }

        /// <summary>
        /// Path to extract the zip file to
        /// </summary>
        [TaskAction(ExtractTaskAction, true)]
        public ITaskItem ExtractPath { get; set; }

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
                case "Create":
                    this.Create();
                    break;
                case "Extract":
                    this.Extract();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private static void CopyStream(java.io.InputStream from, java.io.OutputStream to)
        {
            sbyte[] buffer = new sbyte[8192];
            int got;
            while ((got = from.read(buffer, 0, buffer.Length)) > 0)
            {
                to.write(buffer, 0, got);
            }
        }

        private void Create()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating ZipFile: {0}", this.ZipFileName));
            this.zipOutStream = new ZipOutputStream(new java.io.FileOutputStream(this.ZipFileName.GetMetadata("FullPath")));
            try
            {
                if (this.CompressFiles != null)
                {
                    foreach (ITaskItem f in this.CompressFiles)
                    {
                        string filePath = f.GetMetadata("FullPath");
                        string zipentry;
                        if (this.RemoveRoot != null)
                        {
                            zipentry = Path.Combine(Path.GetDirectoryName(filePath).Substring(Path.GetFullPath(this.RemoveRoot.GetMetadata("FullPath")).Length - 1), Path.GetFileName(filePath));
                            if (zipentry[0] == Path.DirectorySeparatorChar)
                            {
                                zipentry = zipentry.Substring(1);
                            }
                        }
                        else
                        {
                            zipentry = Path.GetFileName(filePath);
                        }

                        ZipEntry z = new ZipEntry(zipentry);
                        z.setMethod(ZipEntry.DEFLATED);
                        this.zipOutStream.putNextEntry(z);
                        try
                        {
                            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Adding File: {0}", zipentry));
                            java.io.FileInputStream s = new java.io.FileInputStream(filePath);
                            try
                            {
                                CopyStream(s, this.zipOutStream);
                            }
                            finally
                            {
                                s.close();
                            }
                        }
                        finally
                        {
                            this.zipOutStream.closeEntry();
                        }
                    }
                }
                else if (this.CompressPath != null)
                {
                    DirectoryInfo rootDirectory = new DirectoryInfo(this.CompressPath.GetMetadata("FullPath"));
                    FileSystemInfo[] infos = rootDirectory.GetFileSystemInfos("*");
                    this.ProcessFolder(infos);
                }
                else
                {
                    Log.LogError("CompressFiles or CompressPath must be specified");
                    return;
                }
            }
            finally
            {
                this.zipOutStream.close();
            }
        }

        private void ProcessFolder(IEnumerable<FileSystemInfo> fileSysInfo)
        {
            // Iterate through each item.
            foreach (FileSystemInfo i in fileSysInfo)
            {
                // Check to see if this is a DirectoryInfo object.
                if (i is DirectoryInfo)
                {
                    // add the folder
                    string filePath = i.FullName;
                    string zipentry;
                    if (this.RemoveRoot != null)
                    {
                        zipentry = Path.Combine(Path.GetDirectoryName(filePath).Substring(this.RemoveRoot.GetMetadata("FullPath").Length - 1), Path.GetFileName(filePath));
                        if (zipentry[0] == Path.DirectorySeparatorChar)
                        {
                            zipentry = zipentry.Substring(1);
                        }
                    }
                    else
                    {
                        zipentry = Path.GetFileName(filePath);
                    }

                    // add the / so that empty folders are added too
                    zipentry += @"/";
                    ZipEntry z = new ZipEntry(zipentry);
                    z.setMethod(ZipEntry.DEFLATED);
                    this.zipOutStream.putNextEntry(z);
                    this.zipOutStream.closeEntry();

                    // Cast the object to a DirectoryInfo object.
                    DirectoryInfo dirInfo = (DirectoryInfo)i;

                    // Iterate through all sub-directories.
                    this.ProcessFolder(dirInfo.GetFileSystemInfos("*"));
                }
                else if (i is FileInfo)
                {
                    string filePath = i.FullName;
                    string zipentry;
                    if (this.RemoveRoot != null)
                    {
                        zipentry = Path.Combine(Path.GetDirectoryName(filePath).Substring(this.RemoveRoot.GetMetadata("FullPath").Length - 1), Path.GetFileName(filePath));
                        if (zipentry[0] == Path.DirectorySeparatorChar)
                        {
                            zipentry = zipentry.Substring(1);
                        }
                    }
                    else
                    {
                        zipentry = Path.GetFileName(filePath);
                    }

                    ZipEntry z = new ZipEntry(zipentry);
                    z.setMethod(ZipEntry.DEFLATED);
                    this.zipOutStream.putNextEntry(z);
                    try
                    {
                        this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Adding File: {0}", zipentry));
                        java.io.FileInputStream s = new java.io.FileInputStream(filePath);
                        try
                        {
                            CopyStream(s, this.zipOutStream);
                        }
                        finally
                        {
                            s.close();
                        }
                    }
                    finally
                    {
                        this.zipOutStream.closeEntry();
                    }
                }
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
            ZipFile zf = new ZipFile(this.ZipFileName.GetMetadata("FullPath"));
            foreach (ZipEntry zipEntry in new EnumerationWrapperCollection(zf.entries()))
            {
                java.io.InputStream s = zf.getInputStream(zipEntry);
                try
                {
                    string fname = System.IO.Path.GetFileName(zipEntry.getName());
                    string newpath = System.IO.Path.Combine(this.ExtractPath.GetMetadata("FullPath"), System.IO.Path.GetDirectoryName(zipEntry.getName()));
                    Directory.CreateDirectory(newpath);
                    if (!zipEntry.isDirectory())
                    {
                        java.io.FileOutputStream dest = new java.io.FileOutputStream(System.IO.Path.Combine(newpath, fname));
                        try
                        {
                            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Extracting File: {0}", System.IO.Path.Combine(newpath, fname)));
                            CopyStream(s, dest);
                        }
                        finally
                        {
                            dest.close();
                        }
                    }
                }
                finally
                {
                    s.close();
                }
            }

            zf.close();
        }
    }
}