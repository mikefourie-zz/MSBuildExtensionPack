//-----------------------------------------------------------------------
// <copyright file="File.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CountLines</i> (<b>Required: </b>Files <b>Optional: </b> CommentIdentifiers, MazSize, MinSize <b>Output: </b>TotalLinecount, CommentLinecount, EmptyLinecount, CodeLinecount, TotalFilecount, IncludedFilecount, ExcludedFilecount, ExcludedFiles, ElapsedTime)</para>
    /// <para><i>GetChecksum</i> (<b>Required: </b>Path <b>Output: </b> Checksum)</para>
    /// <para><i>Replace</i> (<b>Required: </b>RegexPattern <b>Optional: </b> Replacement, Path, TextEncoding, Files)</para>
    /// <para><i>SetAttributes</i> (<b>Required: </b>Files)</para>
    /// <para><b>Remote Execution Support:</b> No</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    ///  <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <ItemGroup>
    ///         <FilesToParse Include="c:\demo\file.txt"/>
    ///         <FilesToCount Include="C:\Demo\**\*.cs"/>
    ///         <AllFilesToCount Include="C:\Demo\**\*"/>
    ///         <AtFiles Include="c:\demo\file1.txt">
    ///             <Attributes>ReadOnly;Hidden</Attributes>
    ///         </AtFiles>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Set some attributes -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="SetAttributes" Files="@(AtFiles)"/>
    ///         <!-- Get the checksum of a file -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="GetChecksum" Path="C:\Projects\CodePlex\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll">
    ///             <Output TaskParameter="Checksum" PropertyName="chksm"/>
    ///         </MSBuild.ExtensionPack.FileSystem.File>
    ///         <Message Text="$(chksm)"/>
    ///         <!-- Replace file content using a regular expression -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="Replace" RegexPattern="regex" Replacement="iiiii" Files="@(FilesToParse)"/>
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="Replace" RegexPattern="regex" Replacement="idi" Path="c:\Demo*"/>
    ///         <!-- Count the number of lines in a file and exclude comments -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="CountLines" Files="@(FilesToCount)" CommentIdentifiers="//">
    ///             <Output TaskParameter="CodeLinecount" PropertyName="csharplines"/>
    ///         </MSBuild.ExtensionPack.FileSystem.File>
    ///         <Message Text="C# CodeLinecount: $(csharplines)"/>
    ///         <!-- Count all lines in a file -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="CountLines" Files="@(AllFilesToCount)">
    ///             <Output TaskParameter="TotalLinecount" PropertyName="AllLines"/>
    ///         </MSBuild.ExtensionPack.FileSystem.File>
    ///         <Message Text="All Files TotalLinecount: $(AllLines)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class File : BaseTask
    {
        private Encoding fileEncoding = Encoding.UTF8;
        private Regex parseRegex;
        private string[] commentIdentifiers;

        /// <summary>
        /// Sets the regex pattern.
        /// </summary>
        public string RegexPattern { get; set; }

        /// <summary>
        /// The replacement text to use
        /// </summary>
        public string Replacement { get; set; }

        /// <summary>
        /// A path to process. Use * for recursive folder processing. For the GetChecksum TaskAction, this indicates the path to the file to create a checksum for.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The file encoding to write the new file in. The task will attempt to default to the current file encoding.
        /// </summary>
        public string TextEncoding { get; set; }

        /// <summary>
        /// Sets characters to be interpreted as comment identifiers. Semi-colon delimited. Only single line comments are currently supported.
        /// </summary>
        public string CommentIdentifiers
        { 
            set { this.commentIdentifiers = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries); }
        }

        /// <summary>
        /// An ItemList of files to process. If calling SetAttributes, include the attributes in an Attributes metadata tag, separated by a semicolon.
        /// </summary>
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// Gets the total number of lines counted
        /// </summary>
        [Output]
        public int TotalLinecount { get; set; }

        /// <summary>
        /// Gets the number of comment lines counted
        /// </summary>
        [Output]
        public int CommentLinecount { get; set; }

        /// <summary>
        /// Gets the number of empty lines countered. Whitespace is ignored.
        /// </summary>
        [Output]
        public int EmptyLinecount { get; set; }
      
        /// <summary>
        /// Gets the number of files counted
        /// </summary>
        [Output]
        public int TotalFilecount { get; set; }

        /// <summary>
        /// Gets the number of code lines countered. This is calculated as Total - Comment - Empty
        /// </summary>
        [Output]
        public int CodeLinecount { get; set; }

        /// <summary>
        /// Gets the number of excluded files
        /// </summary>
        [Output]
        public int ExcludedFilecount { get; set; }

        /// <summary>
        /// Gets the number of included files
        /// </summary>
        [Output]
        public int IncludedFilecount { get; set; }

        /// <summary>
        /// Sets the maximum size of files to count
        /// </summary>
        public int MaxSize { get; set; }

        /// <summary>
        /// sets the minimum size of files to count
        /// </summary>
        public int MinSize { get; set; }

        /// <summary>
        /// Gets the time taken to count the files. Value in seconds.
        /// </summary>
        [Output]
        public string ElapsedTime { get; set; }

        /// <summary>
        /// Gets the file checksum
        /// </summary>
        [Output]
        public string Checksum { get; set; }

        /// <summary>
        /// Item collection of files Excluded from the count.
        /// </summary>
        [Output]
        public Collection<ITaskItem> ExcludedFiles { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            switch (this.TaskAction)
            {
                case "CountLines":
                    this.CountLines();
                    break;
                case "Replace":
                    this.Replace();
                    break;
                case "GetChecksum":
                    this.GetChecksum();
                    break;
                case "SetAttributes":
                    this.SetAttributes();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private static FileAttributes SetAttributes(string[] attributes)
        {
            FileAttributes flags = new FileAttributes();
            if (Array.IndexOf(attributes, "Archive") >= 0)
            {
                flags |= FileAttributes.Archive;
            }

            if (Array.IndexOf(attributes, "Compressed") >= 0)
            {
                flags |= FileAttributes.Compressed;
            }

            if (Array.IndexOf(attributes, "Encrypted") >= 0)
            {
                flags |= FileAttributes.Encrypted;
            }

            if (Array.IndexOf(attributes, "Hidden") >= 0)
            {
                flags |= FileAttributes.Hidden;
            }

            if (Array.IndexOf(attributes, "Normal") >= 0)
            {
                flags |= FileAttributes.Normal;
            }

            if (Array.IndexOf(attributes, "ReadOnly") >= 0)
            {
                flags |= FileAttributes.ReadOnly;
            }

            if (Array.IndexOf(attributes, "System") >= 0)
            {
                flags |= FileAttributes.System;
            }

            return flags;
        }

        private void SetAttributes()
        {
            if (this.Files == null)
            {
                Log.LogError("Files is required");
                return;
            }

            this.Log.LogMessage("Setting file attributes");
            foreach (ITaskItem f in this.Files)
            {
                FileInfo afile = new FileInfo(f.ItemSpec) { Attributes = SetAttributes(f.GetMetadata("Attributes").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) };
            }
        }

        private void GetChecksum()
        {
            if (!System.IO.File.Exists(this.Path))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid File passed: {0}", this.Path));
                return;
            }

            this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Getting Checksum for file: {0}", this.Path));
            using (FileStream fs = System.IO.File.OpenRead(this.Path))
            {
                MD5CryptoServiceProvider csp = new MD5CryptoServiceProvider();
                byte[] hash = csp.ComputeHash(fs);
                this.Checksum = BitConverter.ToString(hash).Replace("-", string.Empty).ToUpperInvariant();
                fs.Close();
            }
        }

        private void CountLines()
        {
            if (this.Files == null)
            {
                Log.LogError("Files is required");
                return;
            }

            this.Log.LogMessage("Counting Lines");
            DateTime start = DateTime.Now;
            this.ExcludedFiles = new Collection<ITaskItem>();
            
            foreach (ITaskItem f in this.Files)
            {
                if (this.MaxSize > 0 || this.MinSize > 0)
                {
                    FileInfo thisFile = new FileInfo(f.ItemSpec);
                    if (this.MaxSize > 0 && thisFile.Length / 1024 > this.MaxSize)
                    {
                        this.ExcludedFiles.Add(f);
                        break;
                    }

                    if (this.MinSize > 0 && thisFile.Length / 1024 < this.MinSize)
                    {
                        this.ExcludedFiles.Add(f);
                        break;
                    }
                }
                
                this.IncludedFilecount++;
                using (StreamReader re = System.IO.File.OpenText(f.ItemSpec))
                {
                    string input;
                    while ((input = re.ReadLine()) != null)
                    {
                        input = input.Trim();

                        if (string.IsNullOrEmpty(input))
                        {
                            this.EmptyLinecount++;
                        }
                        else if (this.commentIdentifiers != null)
                        {
                            foreach (string s in this.commentIdentifiers)
                            {
                                if (input.StartsWith(s, StringComparison.OrdinalIgnoreCase))
                                {
                                    this.CommentLinecount++;
                                }
                            }
                        }

                        this.TotalLinecount++;
                    }
                }
            }

            if (this.ExcludedFiles != null)
            {
                this.ExcludedFilecount = this.ExcludedFiles.Count;
            }
            
            TimeSpan t = DateTime.Now - start;
            this.ElapsedTime = t.Seconds.ToString(CultureInfo.CurrentCulture);
            this.CodeLinecount = this.TotalLinecount - this.CommentLinecount - this.EmptyLinecount;
            this.TotalFilecount = this.IncludedFilecount + this.ExcludedFilecount;
        }

        /// <summary>
        /// Replace File
        /// </summary>
        private void Replace()
        {
            if (!string.IsNullOrEmpty(this.TextEncoding))
            {
                try
                {
                    this.fileEncoding = Encoding.GetEncoding(this.TextEncoding);
                }
                catch (ArgumentException)
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "{0} is not a supported encoding name.", this.TextEncoding));
                    return;
                }
            }

            if (string.IsNullOrEmpty(this.RegexPattern))
            {
                Log.LogError("RegexPattern is required.");
                return;
            }

            // Load the regex to use
            this.parseRegex = new Regex(this.RegexPattern, RegexOptions.Compiled);

            // Check to see if we are processing a file collection or a path
            if (string.IsNullOrEmpty(this.Path) != true)
            {
                // we need to process a path
                this.ProcessPath();
            }
            else
            {
                // we need to process a collection
                this.ProcessCollection();
            }
        }

        /// <summary>
        /// Processes the path.
        /// </summary>
        private void ProcessPath()
        {
            string originalPath = this.Path;
            string rootPath = this.Path.Replace("*", string.Empty);

            // Validation
            if (Directory.Exists(rootPath) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Path not found: {0}", rootPath));
                return;
            }

            this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Processing Path: {0} with RegEx: {1}, ReplacementText: {2}", this.Path, this.RegexPattern, this.Replacement));

            // Check if we need to do a recursive search
            if (originalPath.Contains("*"))
            {
                // We have to do a recursive search
                // Create a new DirectoryInfo object.
                DirectoryInfo dir = new DirectoryInfo(rootPath);

                if (!dir.Exists)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "The directory does not exist: {0}", rootPath));
                    return;
                }

                // Call the GetFileSystemInfos method.
                FileSystemInfo[] infos = dir.GetFileSystemInfos("*");
                this.ProcessFolder(infos);
            }
            else
            {
                DirectoryInfo dir = new DirectoryInfo(originalPath);

                if (!dir.Exists)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "The directory does not exist: {0}", originalPath));
                    return;
                }

                FileInfo[] fileInfo = dir.GetFiles();

                foreach (FileInfo f in fileInfo)
                {
                    this.ParseAndReplaceFile(f.FullName, false);
                }
            }
        }

        /// <summary>
        /// Processes the folder.
        /// </summary>
        /// <param name="filseSysInfo">The FS info.</param>
        private void ProcessFolder(IEnumerable<FileSystemInfo> filseSysInfo)
        {
            // Iterate through each item.
            foreach (FileSystemInfo i in filseSysInfo)
            {
                // Check to see if this is a DirectoryInfo object.
                if (i is DirectoryInfo)
                {
                    // Cast the object to a DirectoryInfo object.
                    DirectoryInfo dirInfo = new DirectoryInfo(i.FullName);

                    // Iterate through all sub-directories.
                    this.ProcessFolder(dirInfo.GetFileSystemInfos("*"));
                }
                else if (i is FileInfo)
                {
                    // Check to see if this is a FileInfo object.
                    this.ParseAndReplaceFile(i.FullName, false);
                }
            }
        }

        /// <summary>
        /// Processes the collection.
        /// </summary>
        private void ProcessCollection()
        {
            if (this.Files == null)
            {
                this.Log.LogError("No file collection has been passed");
                return;
            }

            this.Log.LogMessage("Processing File Collection");

            foreach (ITaskItem file in this.Files)
            {
                this.ParseAndReplaceFile(file.ItemSpec, true);
            }
        }

        /// <summary>
        /// Parses the and replace file.
        /// </summary>
        /// <param name="parseFile">The parse file.</param>
        /// <param name="checkExists">if set to <c>true</c> [check exists].</param>
        private void ParseAndReplaceFile(string parseFile, bool checkExists)
        {
            this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Processing File: {0}", parseFile));
            if (checkExists && System.IO.File.Exists(parseFile) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "The file does not exist: {0}", parseFile));
                return;
            }

            // Open the file and attempt to read the encoding from the BOM.
            string entireFile;

            using (StreamReader streamReader = new StreamReader(parseFile, this.fileEncoding, true))
            {
                if (this.fileEncoding == null)
                {
                    this.fileEncoding = streamReader.CurrentEncoding;
                }

                entireFile = streamReader.ReadToEnd();
            }

            // Parse the entire file.
            string newFile = this.parseRegex.Replace(entireFile, this.Replacement);

            // First make sure the file is writable.
            FileAttributes fileAttributes = System.IO.File.GetAttributes(parseFile);

            // If readonly attribute is set, reset it.
            if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Making File Writeable: {0}", parseFile));
                System.IO.File.SetAttributes(parseFile, fileAttributes ^ FileAttributes.ReadOnly);
            }

            // Set TextEncoding if it was specified.
            if (string.IsNullOrEmpty(this.TextEncoding) == false)
            {
                try
                {
                    this.fileEncoding = System.Text.Encoding.GetEncoding(this.TextEncoding);
                }
                catch (ArgumentException)
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "{0} is not a supported encoding name.", this.TextEncoding));
                    return;
                }
            }

            // Write out the new file.
            using (StreamWriter streamWriter = new StreamWriter(parseFile, false, this.fileEncoding))
            {
                streamWriter.Write(newFile);
            }
        }
    }
}