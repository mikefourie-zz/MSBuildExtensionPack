//-----------------------------------------------------------------------
// <copyright file="Detokenise.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// This task is a derivative of the task posted here: http://freetodev.spaces.live.com/blog/cns!EC3C8F2028D842D5!244.entry
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Evaluation;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Analyse</i> (<b>Required: </b>TargetFiles or TargetPath <b>Optional: </b> CommandLineValues, DisplayFiles, TextEncoding, ForceWrite, ReplacementValues, Separator, TokenPattern, TokenExtractionPattern <b>Output: </b>FilesProcessed)</para>
    /// <para><i>Detokenise</i> (<b>Required: </b>TargetFiles or TargetPath <b>Optional: </b> SearchAllStores, IgnoreUnknownTokens, CommandLineValues, DisplayFiles, TextEncoding, ForceWrite, ReplacementValues, Separator, TokenPattern, TokenExtractionPattern <b>Output: </b>FilesProcessed, FilesDetokenised)</para>
    /// <para><i>Report</i> (<b>Required: </b>TargetFiles or TargetPath <b>Optional: </b> DisplayFiles, TokenPattern, ReportUnusedTokens <b>Output: </b>FilesProcessed, TokenReport, UnusedTokens)</para>
    /// <para><b>Remote Execution Support:</b> No</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default;Report" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <PropertyGroup>
    ///         <PathToDetokenise>C:\Demo\*</PathToDetokenise>
    ///         <CPHome>http://www.msbuildextensionpack.com</CPHome>
    ///         <Title>A New Title</Title>
    ///         <clv>hello=hello#~#hello1=how#~#hello2=are#~#Configuration=debug</clv>
    ///         <Configuration>debug</Configuration>
    ///         <Platform>x86</Platform>
    ///         <HiImAnUnsedToken>TheReportWillFindMe</HiImAnUnsedToken>
    ///     </PropertyGroup>
    ///     <Target Name="Default">
    ///         <ItemGroup>
    ///             <FileCollection Include="C:\Demo1\TestFile.txt"/>
    ///             <FileCollection2 Include="C:\Demo1\TestFile2.txt"/>
    ///             <FileCollection3 Include="C:\Demo1\TestFile3.txt"/>
    ///         </ItemGroup>
    ///         <ItemGroup>
    ///             <TokenValues Include="Title">
    ///                 <Replacement>ANewTextString</Replacement>
    ///             </TokenValues >
    ///             <TokenValues Include="ProjectHome">
    ///                 <Replacement>http://www.msbuildextensionpack.com</Replacement>
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
    ///         <!-- 6 Detokenise using values that can be passed in via the command line -->
    ///         <MSBuild.ExtensionPack.FileSystem.Detokenise TaskAction="Detokenise" TargetFiles="@(FileCollection3)" CommandLineValues="$(clv)"/>
    ///     </Target>
    ///     <!--- Generate a report of files showing which tokens are used in files -->
    ///     <Target Name="Report" DependsOnTargets="GetFiles">
    ///         <CallTarget Targets="List"/>
    ///     </Target>
    ///     <Target Name="List" Inputs="@(Report1)" Outputs="%(Identity)">
    ///         <Message Text="Token: @(Report1)"/>
    ///         <Message Text="%(Report1.Files)"/>
    ///     </Target>
    ///     <Target Name="GetFiles">
    ///         <MSBuild.ExtensionPack.FileSystem.Detokenise TaskAction="Report" TargetPath="C:\Demo1*"  DisplayFiles="true" ReportUnusedTokens="true">
    ///             <Output TaskParameter="TokenReport" ItemName="Report1"/>
    ///             <Output TaskParameter="UnusedTokens" ItemName="Unused"/>
    ///         </MSBuild.ExtensionPack.FileSystem.Detokenise>
    ///         <Message Text="Unused Token - %(Unused.Identity)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class Detokenise : BaseTask
    {
        private const string AnalyseTaskAction = "Analyse";
        private const string DetokeniseTaskAction = "Detokenise";
        private const string ReportTaskAction = "Report";
        private string tokenExtractionPattern = @"(?<=\$\()[0-9a-zA-Z-._]+(?=\))";
        private string tokenPattern = @"\$\([0-9a-zA-Z-._]+\)";
        private Project project;
        private Encoding fileEncoding = Encoding.UTF8;
        private Regex parseRegex;
        private bool analyseOnly, report;
        private string separator = "#~#";
        private Dictionary<string, string> commandLineDictionary;
        private SortedDictionary<string, string> tokenDictionary;
        private SortedDictionary<string, string> unusedTokens;
        private string activeFile;

        // this bool is used to indicate what mode we are in.
        // if true, then the task has been configured to use a passed in collection
        // to use as replacement tokens. If false, then it will use the msbuild
        // proj file for replacement tokens
        private bool collectionMode = true;

        // this bool is used to track whether the file needs to be re-written.
        private bool tokenMatched;

        /// <summary>
        /// Set to true for files being processed to be output to the console.
        /// </summary>
        public bool DisplayFiles { get; set; }

        /// <summary>
        /// Specifies the regular expression format of the token to look for. The default pattern is \$\([0-9a-zA-Z-._]+\) which equates to $(token)
        /// </summary>
        public string TokenPattern
        {
            get { return this.tokenPattern; }
            set { this.tokenPattern = value; }
        }

        /// <summary>
        /// Specifies the regular expression to use to extract the token name from the TokenPattern provided. The default pattern is (?&lt;=\$\()[0-9a-zA-Z-._]+(?=\)), i.e it will extract token from $(token)
        /// </summary>
        public string TokenExtractionPattern
        {
            get { return this.tokenExtractionPattern; }
            set { this.tokenExtractionPattern = value; }
        }

        /// <summary>
        /// Sets the replacement values.
        /// </summary>
        public ITaskItem[] ReplacementValues { get; set; }

        /// <summary>
        /// Sets the replacement values provided via the command line. The format is token1=value1#~#token2=value2 etc.
        /// </summary>
        public string CommandLineValues { get; set; }

        /// <summary>
        /// Sets the separator to use to split the CommandLineValues. The default is #~#
        /// </summary>
        public string Separator
        {
            get { return this.separator; }
            set { this.separator = value; }
        }

        /// <summary>
        /// Sets the MSBuild file to load for token matching. Defaults to BuildEngine.ProjectFileOfTaskNode
        /// </summary>
        public ITaskItem ProjectFile { get; set; }

        /// <summary>
        /// If this is set to true, then the file is re-written, even if no tokens are matched.
        /// this may be used in the case when the user wants to ensure all file are written
        /// with the same encoding.
        /// </summary>
        public bool ForceWrite { get; set; }

        /// <summary>
        /// Specifies whether to search in the ReplacementValues, CommandLineValues and the ProjectFile for token values. Default is false.
        /// </summary>
        public bool SearchAllStores { get; set; }

        /// <summary>
        /// Specifies whether to ignore tokens which are not matched. Default is false.
        /// </summary>
        public bool IgnoreUnknownTokens { get; set; }

        /// <summary>
        /// Sets the TargetPath.
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// Sets the TargetFiles.
        /// </summary>
        public ITaskItem[] TargetFiles { get; set; }

        /// <summary>
        /// The file encoding to write the new file in. The task will attempt to default to the current file encoding. If TargetFiles is specified, individual encodings can be specified by providing an Encoding metadata value.
        /// </summary>
        public string TextEncoding { get; set; }

        /// <summary>
        /// Gets the files processed count. [Output]
        /// </summary>
        [Output]
        public int FilesProcessed { get; set; }

        /// <summary>
        /// Gets the files detokenised count. [Output]
        /// </summary>
        [Output]
        public int FilesDetokenised { get; set; }

        /// <summary>
        /// ItemGroup containing the Tokens (Identity) and Files metadata containing all the files in which the token can be found.
        /// </summary>
        [Output]
        public ITaskItem[] TokenReport { get; set; }

        /// <summary>
        /// Itemgroup containing the tokens which have been provided but not found in the files scanned. ReportUnusedTokens must be set to true to use this.
        /// </summary>
        [Output]
        public ITaskItem[] UnusedTokens { get; set; }

        /// <summary>
        /// Set to true when running a Report to see which tokens are not used in any files scanned. Default is false.
        /// </summary>
        public bool ReportUnusedTokens { get; set; }

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
                case AnalyseTaskAction:
                    this.analyseOnly = true;
                    break;
                case DetokeniseTaskAction:
                    break;
                case ReportTaskAction:
                    this.analyseOnly = true;
                    this.report = true;
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }

            this.tokenDictionary = new SortedDictionary<string, string>();
            this.DoDetokenise();

            if (this.tokenDictionary.Count > 0 && this.report)
            {
                this.TokenReport = new TaskItem[this.tokenDictionary.Count];
                int i = 0;
                foreach (var s in this.tokenDictionary)
                {
                    ITaskItem t = new TaskItem(s.Key);
                    t.SetMetadata("Files", s.Value);
                    this.TokenReport[i] = t;
                    i++;
                }

                if (this.ReportUnusedTokens)
                {
                    this.unusedTokens = new SortedDictionary<string, string>();

                    // Find unused tokens.
                    if (this.collectionMode)
                    {
                        if (this.ReplacementValues != null)
                        {
                            // we need to look in the ReplacementValues for a match
                            foreach (ITaskItem token in this.ReplacementValues)
                            {
                                if (!this.tokenDictionary.ContainsKey(token.ToString()) && !this.unusedTokens.ContainsKey(token.ToString()))
                                {
                                    this.unusedTokens.Add(token.ToString(), string.Empty);
                                }
                            }
                        }

                        if (this.commandLineDictionary != null)
                        {
                            foreach (string s in this.commandLineDictionary.Keys)
                            {
                                if (!this.tokenDictionary.ContainsKey(s) && !this.unusedTokens.ContainsKey(s))
                                {
                                    this.unusedTokens.Add(s, string.Empty);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (ProjectProperty pp in this.project.Properties)
                        {
                            if (!this.tokenDictionary.ContainsKey(pp.Name) && !this.unusedTokens.ContainsKey(pp.Name))
                            {
                                this.unusedTokens.Add(pp.Name, string.Empty);
                            }
                        }
                    }

                    this.UnusedTokens = new TaskItem[this.unusedTokens.Count];
                    i = 0;
                    foreach (var s in this.unusedTokens)
                    {
                        ITaskItem t = new TaskItem(s.Key);
                        this.UnusedTokens[i] = t;
                        i++;
                    }
                }
            }
        }

        private static Encoding GetTextEncoding(string enc)
        {
            switch (enc)
            {
                case "DEFAULT":
                    return Encoding.Default;
                case "ASCII":
                    return Encoding.ASCII;
                case "Unicode":
                    return Encoding.Unicode;
                case "UTF7":
                    return Encoding.UTF7;
                case "UTF8":
                    return Encoding.UTF8;
                case "UTF32":
                    return Encoding.UTF32;
                case "BigEndianUnicode":
                    return Encoding.BigEndianUnicode;
                default:
                    return !string.IsNullOrEmpty(enc) ? Encoding.GetEncoding(enc) : null;
            }
        }

        private void DoDetokenise()
        {
            try
            {
                this.LogTaskMessage("Detokenise Task Execution Started [" + DateTime.Now.ToString("HH:MM:ss", CultureInfo.CurrentCulture) + "]");

                // if the ReplacementValues collection and the CommandLineValues are null, then we need to load
                // the project file that called this task to get it's properties.
                if (this.ReplacementValues == null && string.IsNullOrEmpty(this.CommandLineValues))
                {
                    this.collectionMode = false;
                }
                else if (!string.IsNullOrEmpty(this.CommandLineValues))
                {
                    string[] commandLineValuesArray = this.CommandLineValues.Split(new[] { this.Separator }, StringSplitOptions.RemoveEmptyEntries);
                    this.commandLineDictionary = new Dictionary<string, string>();
                    foreach (string s in commandLineValuesArray)
                    {
                        string[] temp = s.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        this.commandLineDictionary.Add(temp[0], temp[1]);
                    }
                }

                if (this.project == null && (!this.collectionMode || this.SearchAllStores))
                {
                    // Read the project file to get the tokens
                    string projectFile = this.ProjectFile == null ? this.BuildEngine.ProjectFileOfTaskNode : this.ProjectFile.ItemSpec;
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Loading Project: {0}", projectFile));
                    this.project = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(projectFile).FirstOrDefault();
                    if (this.project == null)
                    {
                        ProjectCollection.GlobalProjectCollection.LoadProject(projectFile);
                        this.project = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(projectFile).FirstOrDefault();
                    }
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
                var info = i as DirectoryInfo;
                if (info != null)
                {
                    // Cast the object to a DirectoryInfo object.
                    DirectoryInfo dirInfo = info;

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

            this.LogTaskMessage(this.analyseOnly ? string.Format(CultureInfo.CurrentCulture, "Analysing Collection: {0} files", this.TargetFiles.Length) : string.Format(CultureInfo.CurrentCulture, "Detokenising Collection: {0} files", this.TargetFiles.Length));
            foreach (ITaskItem file in this.TargetFiles)
            {
                this.tokenMatched = false;
                this.DetokeniseFileProvided(file.ItemSpec, true, GetTextEncoding(file.GetMetadata("Encoding")));
            }
        }

        private void DetokeniseFileProvided(string file, bool checkExists, Encoding enc)
        {
            this.FilesProcessed++;
            this.activeFile = file;

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
            if ((this.tokenMatched || this.ForceWrite) && !this.analyseOnly)
            {
                // First make sure the file is writable.
                bool changedAttribute = false;
                FileAttributes fileAttributes = System.IO.File.GetAttributes(file);

                // If readonly attribute is set, reset it.
                if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    this.LogTaskMessage(MessageImportance.Low, "Making file writable");
                    System.IO.File.SetAttributes(file, fileAttributes ^ FileAttributes.ReadOnly);
                    changedAttribute = true;
                }

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

                if (changedAttribute)
                {
                    this.LogTaskMessage(MessageImportance.Low, "Making file readonly");
                    System.IO.File.SetAttributes(file, FileAttributes.ReadOnly);
                }
            }
        }

        private string FindReplacement(Group regexMatch)
        {
            // Get the match.
            string propertyFound = regexMatch.Captures[0].ToString();

            // Extract the keyword from the match.
            string extractedProperty = Regex.Match(propertyFound, this.TokenExtractionPattern).Captures[0].ToString();

            // Find the replacement property
            if (this.collectionMode)
            {
                if (this.ReplacementValues != null)
                {
                    // we need to look in the ReplacementValues for a match
                    foreach (ITaskItem token in this.ReplacementValues)
                    {
                        if (token.ToString() == extractedProperty)
                        {
                            // set the bool so we can write the new file content
                            this.tokenMatched = true;
                            if (this.report)
                            {
                                this.UpdateTokenDictionary(extractedProperty);
                            }

                            return token.GetMetadata("Replacement");
                        }
                    }

                    if (!this.report && !this.SearchAllStores && !this.IgnoreUnknownTokens)
                    {
                        Log.LogError(string.Format(CultureInfo.CurrentCulture, "Property not found: {0}", extractedProperty));
                        throw new ArgumentException("Review error log");
                    }
                }

                // we need to look in the CommandLineValues
                if (this.commandLineDictionary != null)
                {
                    try
                    {
                        string replacement = this.commandLineDictionary[extractedProperty];

                        // set the bool so we can write the new file content
                        this.tokenMatched = true;
                        if (this.report)
                        {
                            this.UpdateTokenDictionary(extractedProperty);
                        }

                        return replacement;
                    }
                    catch
                    {
                        if (!this.report && !this.SearchAllStores && !this.IgnoreUnknownTokens)
                        {
                            Log.LogError(string.Format(CultureInfo.CurrentCulture, "Property not found: {0}", extractedProperty));
                            throw new ArgumentException("Review error log");
                        }
                    }
                }
            }

            // we need to look in the calling project's properties collection
            if (this.project == null || this.project.GetProperty(extractedProperty) == null)
            {
                if (!this.report && !this.IgnoreUnknownTokens)
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "Property not found: {0}", extractedProperty));
                    throw new ArgumentException("Review error log");
                }

                if (this.IgnoreUnknownTokens)
                {
                    return string.Format(CultureInfo.InvariantCulture, "$({0})", extractedProperty);
                }
            }

            // set the bool so we can write the new file content
            this.tokenMatched = true;
            if (this.report)
            {
                this.UpdateTokenDictionary(extractedProperty);
            }

            return this.report ? string.Empty : (from p in this.project.Properties where string.Equals(p.Name, extractedProperty, StringComparison.OrdinalIgnoreCase) select p.EvaluatedValue).FirstOrDefault();
        }

        private void UpdateTokenDictionary(string extractedProperty)
        {
            if (this.tokenDictionary.ContainsKey(extractedProperty))
            {
                if (!this.tokenDictionary[extractedProperty].Contains(this.activeFile))
                {
                    this.tokenDictionary[extractedProperty] += this.activeFile + ";";
                }

                return;
            }

            this.tokenDictionary.Add(extractedProperty, this.activeFile + ";");
        }
    }
}