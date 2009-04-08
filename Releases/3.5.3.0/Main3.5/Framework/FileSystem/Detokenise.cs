//-----------------------------------------------------------------------
// <copyright file="Detokenise.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// This task is a derivative of the task posted here: http://freetodev.spaces.live.com/blog/cns!EC3C8F2028D842D5!244.entry
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.BuildEngine;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Analyse</i> (<b>Required: </b>TargetFiles or TargetPath <b>Optional: </b> DisplayFiles, Encoding ,ForceWrite, ReplacementValues, TokenPattern <b>Output: </b>FilesProcessed)</para>
    /// <para><i>Replace</i> (<b>Required: </b>TargetFiles or TargetPath <b>Optional: </b> DisplayFiles, Encoding ,ForceWrite, ReplacementValues, TokenPattern <b>Output: </b>FilesProcessed, FilesDetokenised)</para>
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
    ///     <PropertyGroup>
    ///         <PathToDetokenise>C:\Demo\*</PathToDetokenise>
    ///         <CPHome>www.codeplex.com/MSBuildExtensionPack</CPHome>
    ///         <Title>A New Title</Title>
    ///     </PropertyGroup>
    ///     <Target Name="Default">
    ///         <ItemGroup>
    ///             <FileCollection Include="C:\Demo1\TestFile.txt"/>
    ///             <FileCollection2 Include="C:\Demo1\TestFile2.txt"/>
    ///         </ItemGroup>
    ///         <ItemGroup>
    ///             <TokenValues Include="Title">
    ///                 <Replacement>ANewTextString</Replacement>
    ///             </TokenValues >
    ///             <TokenValues Include="ProjectHome">
    ///                 <Replacement>www.codeplex.com/MSBuildExtensionPack</Replacement>
    ///             </TokenValues >
    ///         </ItemGroup>
    ///         <!-- Analyse a collection of files. This can be used to ensure that all tokens are known. -->
    ///         <MSBuild.ExtensionPack.FileSystem.Detokenise TaskAction="Analyse" TargetFiles="@(FileCollection)" ReplacementValues="@(TokenValues)"/>
    ///         <MSBuild.ExtensionPack.FileSystem.Detokenise TaskAction="Analyse" TargetFiles="@(FileCollection2)"/>
    ///         <!-- 1 Detokenise the files defined in FileCollection and use the TokenValues collection for substitution. -->
    ///         <MSBuild.ExtensionPack.FileSystem.Detokenise TaskAction="Detokenise" TargetFiles="@(FileCollection)" ReplacementValues="@(TokenValues)"/>
    ///         <!-- 2 Detokenise the files defined in FileCollection2 and use the tokens defined by the .proj properties -->
    ///         <MSBuild.ExtensionPack.FileSystem.Detokenise TaskAction="Detokenise" TargetFiles="@(FileCollection2)"/>
    ///         <!-- 3 Detokenise the files at the given TargetPath and perform a recursive search -->
    ///         <MSBuild.ExtensionPack.FileSystem.Detokenise TaskAction="Detokenise" TargetPath="$(PathToDetokenise)"/>
    ///         <!-- 4 This will produce the same result as #3, but no file processing will be logged to the console. Because ForceWrite has been specified, all files will be re-written -->
    ///         <MSBuild.ExtensionPack.FileSystem.Detokenise TaskAction="Detokenise" TargetPath="$(PathToDetokenise)" DisplayFiles="false" ForceWrite="true"/>
    ///         <!-- 5 This will produce the same result as 4, though ForceWrite is false by default so the difference can be displayed using the output parameters -->
    ///         <MSBuild.ExtensionPack.FileSystem.Detokenise TaskAction="Detokenise" TargetPath="$(PathToDetokenise)" DisplayFiles="false">
    ///             <Output TaskParameter="FilesProcessed" ItemName="FilesProcessed"/>
    ///             <Output TaskParameter="FilesDetokenised" ItemName="FilesDetokenised"/>
    ///         </MSBuild.ExtensionPack.FileSystem.Detokenise>
    ///         <Message Text="FilesDetokenised = @(FilesDetokenised), FilesProcessed = @(FilesProcessed)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.2.0/html/348d3976-920f-9aca-da50-380d11ee7cf5.htm")]
    public class Detokenise : BaseTask
    {
        private const string AnalyseTaskAction = "Analyse";
        private const string DetokeniseTaskAction = "Detokenise";
        private const string ParseRegexPatternExtract = @"(?<=\$\()[0-9a-zA-Z-._]+(?=\))";
        private string tokenPattern = @"\$\([0-9a-zA-Z-._]+\)";
        private Project project;
        private Encoding fileEncoding = Encoding.UTF8;
        private Regex parseRegex;
        private bool analyseOnly;

        // this bool is used to indicate what mode we are in.
        // if true, then the task has been configured to use a passed in collection
        // to use as replacement tokens. If false, then it will used the msbuild
        // proj file for replacement tokens
        private bool collectionMode = true;

        // this bool is used to track whether the file needs to be re-written.
        private bool tokenMatched;

        [DropdownValue(AnalyseTaskAction)]
        [DropdownValue(DetokeniseTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Set to true for files being processed to be output to the console.
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        [TaskAction(DetokeniseTaskAction, false)]
        public bool DisplayFiles { get; set; }

        /// <summary>
        /// Specifies the format of the token to look for. The default patterns is $(token)
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        [TaskAction(DetokeniseTaskAction, false)]
        public string TokenPattern
        {
            get { return this.tokenPattern; }
            set { this.tokenPattern = value; }
        }

        /// <summary>
        /// Sets the replacement values.
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        [TaskAction(DetokeniseTaskAction, false)]
        public ITaskItem[] ReplacementValues { get; set; }

        /// <summary>
        /// Sets the MSBuidl file to load for token matching. Defaults to BuildEngine.ProjectFileOfTaskNode
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        [TaskAction(DetokeniseTaskAction, false)]
        public ITaskItem ProjectFile { get; set; }

        /// <summary>
        /// If this is set to true, then the file is re-written, even if no tokens are matched.
        /// this may be used in the case when the user wants to ensure all file are written
        /// with the same encoding.
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        [TaskAction(DetokeniseTaskAction, false)]
        public bool ForceWrite { get; set; }

        /// <summary>
        /// Sets the TargetPath.
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        [TaskAction(DetokeniseTaskAction, false)]
        public string TargetPath { get; set; }

        /// <summary>
        /// Sets the TargetFiles.
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        [TaskAction(DetokeniseTaskAction, false)]
        public ITaskItem[] TargetFiles { get; set; }

        /// <summary>
        /// The file encoding to write the new file in. The task will attempt to default to the current file encoding. If TargetFiles is specified, individual encodings can be specified by providing an Encoding metadata value.
        /// </summary>
        [TaskAction(AnalyseTaskAction, false)]
        [TaskAction(DetokeniseTaskAction, false)]
        public string TextEncoding { get; set; }

        /// <summary>
        /// Gets the files processed count. [Output]
        /// </summary>
        [Output]
        [TaskAction(AnalyseTaskAction, false)]
        [TaskAction(DetokeniseTaskAction, false)]
        public int FilesProcessed { get; set; }

        /// <summary>
        /// Gets the files detokenised count. [Output]
        /// </summary>
        [Output]
        [TaskAction(AnalyseTaskAction, false)]
        [TaskAction(DetokeniseTaskAction, false)]
        public int FilesDetokenised { get; set; }

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
                case "Analyse":
                    this.analyseOnly = true;
                    break;
                case "Detokenise":
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }

            this.DoDetokenise();
        }

        private static Encoding GetTextEncoding(string enc)
        {
            switch (enc)
            {
                case "DEFAULT":
                    return System.Text.Encoding.Default;
                case "ASCII":
                    return System.Text.Encoding.ASCII;
                case "Unicode":
                    return System.Text.Encoding.Unicode;
                case "UTF7":
                    return System.Text.Encoding.UTF7;
                case "UTF8":
                    return System.Text.Encoding.UTF8;
                case "UTF32":
                    return System.Text.Encoding.UTF32;
                case "BigEndianUnicode":
                    return System.Text.Encoding.BigEndianUnicode;
                default:
                    if (!string.IsNullOrEmpty(enc))
                    {
                        return Encoding.GetEncoding(enc);
                    }

                    return null;
            }
        }

        private void DoDetokenise()
        {
            try
            {
                this.LogTaskMessage("Detokenise Task Execution Started [" + DateTime.Now.ToString("HH:MM:ss", CultureInfo.CurrentCulture) + "]");

                // if the ReplacementValues collection is null, then we need to load
                // the project file that called this task to get it's properties.
                if (this.ReplacementValues == null)
                {
                    this.collectionMode = false;

                    // Read the project file to get the tokens
                    this.project = new Project();
                    string projectFile = this.ProjectFile == null ? this.BuildEngine.ProjectFileOfTaskNode : this.ProjectFile.ItemSpec;
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Loading Project: {0}", projectFile));
                    this.project.Load(projectFile);
                }

                if (!string.IsNullOrEmpty(this.TextEncoding))
                {
                    try
                    {
                        this.fileEncoding = GetTextEncoding(this.TextEncoding);
                    }
                    catch (ArgumentException)
                    {
                        Log.LogError(string.Format(CultureInfo.CurrentCulture, "Error, {0} is not a supported encoding name.", this.TextEncoding));
                        return;
                    }
                }

                // Load the regex to use
                this.parseRegex = new Regex(this.TokenPattern, RegexOptions.Compiled);

                // Check to see if we are processing a file collection or a path
                if (string.IsNullOrEmpty(this.TargetPath) != true)
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
            finally
            {
                this.LogTaskMessage("Detokenise Task Execution Completed [" + DateTime.Now.ToString("HH:MM:ss", CultureInfo.CurrentCulture) + "]");
            }
        }

        private void ProcessPath()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Detokenising Path: {0}", this.TargetPath));
            string originalPath = this.TargetPath;
            string rootPath = originalPath.Replace("*", string.Empty);
            
            // Check if we need to do a recursive search
            if (originalPath.Contains("*"))
            {
                // Need to do a recursive search
                DirectoryInfo dir = new DirectoryInfo(rootPath);
                if (!dir.Exists)
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "The directory does not exist: {0}", rootPath));
                    throw new ArgumentException("Review error log");
                }
                
                FileSystemInfo[] infos = dir.GetFileSystemInfos("*");
                this.ProcessFolder(infos);
            }
            else
            {
                // Only need to process the files in the folder provided
                DirectoryInfo dir = new DirectoryInfo(originalPath);
                if (!dir.Exists)
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "The directory does not exist: {0}", rootPath));
                    throw new ArgumentException("Review error log");
                }

                FileInfo[] fileInfo = dir.GetFiles();

                foreach (FileInfo f in fileInfo)
                {
                    this.tokenMatched = false;
                    this.DetokeniseFileProvided(f.FullName, false, null);
                }
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
                    // Cast the object to a DirectoryInfo object.
                    DirectoryInfo dirInfo = (DirectoryInfo)i;

                    // Iterate through all sub-directories.
                    this.ProcessFolder(dirInfo.GetFileSystemInfos("*"));
                }
                else if (i is FileInfo)
                {
                    this.tokenMatched = false;
                    this.DetokeniseFileProvided(i.FullName, false, null);
                }
            }
        }

        private void ProcessCollection()
        {
            if (this.TargetFiles == null)
            {
                Log.LogError("The collection passed to TargetFiles is empty");
                throw new ArgumentException("Review error log");
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Detokenising Collection: {0} files", this.TargetFiles.Length));
            foreach (ITaskItem file in this.TargetFiles)
            {
                this.tokenMatched = false;
                this.DetokeniseFileProvided(file.ItemSpec, true, GetTextEncoding(file.GetMetadata("Encoding")));
            }
        }

        private void DetokeniseFileProvided(string file, bool checkExists, Encoding enc)
        {
            this.FilesProcessed++;

            if (this.DisplayFiles)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Detokenising File: {0}", file));
            }

            Encoding finalEncoding;

            // See if the file exists
            if (checkExists && System.IO.File.Exists(file) == false)
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "File not found: {0}", file));
                throw new ArgumentException("Review error log");
            }

            // Open the file and attempt to read the encoding from the BOM
            string fileContent;
            using (StreamReader streamReader = new StreamReader(file, this.fileEncoding, true))
            {
                // Read the file.
                fileContent = streamReader.ReadToEnd();
                finalEncoding = enc ?? (string.IsNullOrEmpty(this.TextEncoding) ? streamReader.CurrentEncoding : this.fileEncoding);
            }

            // Parse the file.
            MatchEvaluator matchEvaluator = this.FindReplacement;
            string newFile = this.parseRegex.Replace(fileContent, matchEvaluator);

            // Only write out new content if a replacement was done or ForceWrite has been set
            if (this.tokenMatched || this.ForceWrite)
            {
                // First make sure the file is writable.
                FileAttributes fileAttributes = System.IO.File.GetAttributes(file);

                // If readonly attribute is set, reset it.
                if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    System.IO.File.SetAttributes(file, fileAttributes ^ FileAttributes.ReadOnly);
                }

                if (!this.analyseOnly)
                {
                    // Write out the new file.
                    using (StreamWriter streamWriter = new StreamWriter(file, false, finalEncoding))
                    {
                        if (this.DisplayFiles)
                        {
                            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Re-writing file content: {0}", file));
                        }

                        streamWriter.Write(newFile);
                        this.FilesDetokenised++;
                    }
                }
            }
        }

        private string FindReplacement(Group regexMatch)
        {
            // Get the match.
            string propertyFound = regexMatch.Captures[0].ToString();
            
            // Extract the keyword from the match.
            string extractedProperty = Regex.Match(propertyFound, ParseRegexPatternExtract).Captures[0].ToString();
            
            // Find the replacement property
            if (this.collectionMode)
            {
                // we need to look in the ReplacementValues for a match
                foreach (ITaskItem token in this.ReplacementValues)
                {
                    if (token.ToString() == extractedProperty)
                    {
                        // set the bool so we can write the new file content
                        this.tokenMatched = true;
                        return token.GetMetadata("Replacement");
                    }
                }

                Log.LogError(string.Format(CultureInfo.CurrentCulture, "Property not found: {0}", extractedProperty));
                throw new ArgumentException("Review error log");
            }

            // we need to look in the calling project's properties collection
            if (this.project.EvaluatedProperties[extractedProperty] == null)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Property not found: {0}", extractedProperty));
                throw new ArgumentException("Review error log");
            }

            // set the bool so we can write the new file content
            this.tokenMatched = true;
            return this.project.EvaluatedProperties[extractedProperty].FinalValue;
        }
    }
}
