//-----------------------------------------------------------------------
// <copyright file="Zip.cs">(c) FreeToDev. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
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
    /// <para><b>Remote Support:</b> NA</para>
    /// <para><b>This task requires:</b></para>
    ///     <para>Microsoft Visual J# 2.0 Redistributable Package – Second Edition (x86)</para>
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
    ///         <ItemGroup>
    ///             <!-- Set the collection of files to Zip-->
    ///             <FilesToZip Include="C:\hotfixes\**\*"/>
    ///         </ItemGroup>
    ///         <!-- Create a zip file based on the FilesToZip collection -->
    ///         <MSBuild.ExtensionPack.Compression.Zip TaskAction="Create" CompressFiles="@(FilesToZip)" RemoveRoot="C:\hotfixes" ZipFileName="C:\newZipByFile.zip"/>
    ///         <!-- Create a zip file based on a Path -->
    ///         <MSBuild.ExtensionPack.Compression.Zip TaskAction="Create" CompressPath="C:\hotfixes" RemoveRoot="C:\hotfixes" ZipFileName="C:\newZipByPath.zip"/>
    ///         <!-- Extract a zip file-->
    ///         <MSBuild.ExtensionPack.Compression.Zip TaskAction="Extract" ExtractPath="C:\aaa11" ZipFileName="C:\newZipByPath.zip"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class Zip : BaseTask
    {
        private ZipOutputStream zos;

        /// <summary>
        /// Sets the files to Compress
        /// </summary>
        public ITaskItem[] CompressFiles { set; get; }

        /// <summary>
        /// Sets the Path to Zip.
        /// </summary>
        public string CompressPath { get; set; }

        /// <summary>
        /// Sets the root to remove from the zip path
        /// </summary>
        public string RemoveRoot { get; set; }

        /// <summary>
        /// Sets the name of the Zip File
        /// </summary>
        [Required]
        public string ZipFileName { get; set; }

        /// <summary>
        /// Path to extract the zip file to
        /// </summary>
        public string ExtractPath { get; set; }

        /// <summary>
        /// This is the main InternalExecute method that all tasks should implement
        /// </summary>
        /// <remarks>
        /// LogError should be thrown in the event of errors
        /// </remarks>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case "Create":
                    this.Create();
                    break;
                case "Extract":
                    this.Extract();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
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
            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Creating ZipFile: {0}", this.ZipFileName));
            this.zos = new ZipOutputStream(new java.io.FileOutputStream(this.ZipFileName));
            try
            {
                if (this.CompressFiles != null)
                {
                    foreach (ITaskItem f in this.CompressFiles)
                    {
                        string filePath = f.ItemSpec;
                        string zipentry = string.Empty;
                        if (!string.IsNullOrEmpty(this.RemoveRoot))
                        {
                            zipentry = Path.Combine(Path.GetDirectoryName(filePath).Substring(Path.GetFullPath(this.RemoveRoot).Length), Path.GetFileName(filePath));
                            if (zipentry[0] == Path.DirectorySeparatorChar)
                            {
                                zipentry = zipentry.Substring(1);
                            }
                        }

                        ZipEntry z = new ZipEntry(zipentry);
                        z.setMethod(ZipEntry.DEFLATED);
                        this.zos.putNextEntry(z);
                        try
                        {
                            this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Adding File: {0}", zipentry));
                            java.io.FileInputStream s = new java.io.FileInputStream(filePath);
                            try
                            {
                                CopyStream(s, this.zos);
                            }
                            finally
                            {
                                s.close();
                            }
                        }
                        finally
                        {
                            this.zos.closeEntry();
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(this.CompressPath))
                {
                    DirectoryInfo rootDirectory = new DirectoryInfo(this.CompressPath);

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
                this.zos.close();
            }
        }

        private void ProcessFolder(IEnumerable<FileSystemInfo> filseSysInfo)
        {
            // Iterate through each item.
            foreach (FileSystemInfo i in filseSysInfo)
            {
                // Check to see if this is a DirectoryInfo object.
                if (i is DirectoryInfo)
                {
                    // add the folder
                    string filePath = i.FullName;
                    string zipentry = string.Empty;
                    if (!string.IsNullOrEmpty(this.RemoveRoot))
                    {
                        zipentry = Path.Combine(Path.GetDirectoryName(filePath).Substring(Path.GetFullPath(this.RemoveRoot).Length), Path.GetFileName(filePath));
                        if (zipentry[0] == Path.DirectorySeparatorChar)
                        {
                            zipentry = zipentry.Substring(1);
                        }
                    }

                    // add the / so that empty folders are added too
                    zipentry += @"/";
                    ZipEntry z = new ZipEntry(zipentry);
                    z.setMethod(ZipEntry.DEFLATED);
                    this.zos.putNextEntry(z);
                    this.zos.closeEntry();

                    // Cast the object to a DirectoryInfo object.
                    DirectoryInfo dirInfo = (DirectoryInfo)i;

                    // Iterate through all sub-directories.
                    this.ProcessFolder(dirInfo.GetFileSystemInfos("*"));
                }
                else if (i is FileInfo)
                {
                    string filePath = i.FullName;
                    string zipentry = string.Empty;
                    if (!string.IsNullOrEmpty(this.RemoveRoot))
                    {
                        zipentry = Path.Combine(Path.GetDirectoryName(filePath).Substring(Path.GetFullPath(this.RemoveRoot).Length), Path.GetFileName(filePath));
                        if (zipentry[0] == Path.DirectorySeparatorChar)
                        {
                            zipentry = zipentry.Substring(1);
                        }
                    }

                    ZipEntry z = new ZipEntry(zipentry);
                    z.setMethod(ZipEntry.DEFLATED);
                    this.zos.putNextEntry(z);
                    try
                    {
                        this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Adding File: {0}", zipentry));
                        java.io.FileInputStream s = new java.io.FileInputStream(filePath);
                        try
                        {
                            CopyStream(s, this.zos);
                        }
                        finally
                        {
                            s.close();
                        }
                    }
                    finally
                    {
                        this.zos.closeEntry();
                    }
                }
            }
        }

        private void Extract()
        {
            if (!File.Exists(this.ZipFileName))
            {
                Log.LogError(string.Format(CultureInfo.InvariantCulture, "ZipFileName not found: {0}", this.ZipFileName));
                return;
            }

            if (string.IsNullOrEmpty(this.ExtractPath))
            {
                Log.LogError("ExtractPath is required");
                return;
            }

            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Extracting ZipFile: {0} to: {1}", this.ZipFileName, this.ExtractPath));
            ZipFile zf = new ZipFile(this.ZipFileName);
            foreach (ZipEntry zipEntry in new EnumerationWrapper(zf.entries()))
            {
                java.io.InputStream s = zf.getInputStream(zipEntry);
                try
                {
                    string fname = System.IO.Path.GetFileName(zipEntry.getName());
                    string newpath = System.IO.Path.Combine(this.ExtractPath, System.IO.Path.GetDirectoryName(zipEntry.getName()));
                    Directory.CreateDirectory(newpath);
                    if (!zipEntry.isDirectory())
                    {
                        java.io.FileOutputStream dest = new java.io.FileOutputStream(System.IO.Path.Combine(newpath, fname));
                        try
                        {
                            this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Extracting File: {0}", System.IO.Path.Combine(newpath, fname)));
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