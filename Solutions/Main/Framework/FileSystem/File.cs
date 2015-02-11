//-----------------------------------------------------------------------
// <copyright file="File.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.AccessControl;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddAttributes</i> (<b>Required: </b>Files)</para>
    /// <para><i>AddSecurity</i> (<b>Required: Users, AccessType, Path or Files</b> Optional: Permission</para>
    /// <para><i>CheckContainsContent</i> (<b>Required: </b>Files, RegexPattern <b>Optional: </b>RegexOptionList <b>Output: </b>Result)</para>
    /// <para><i>Concatenate</i> (<b>Required: </b>Files,  TargetPath)</para>
    /// <para><i>CountLines</i> (<b>Required: </b>Files <b>Optional: </b>CommentIdentifiers, MazSize, MinSize <b>Output: </b>TotalLinecount, CommentLinecount, EmptyLinecount, CodeLinecount, TotalFilecount, IncludedFilecount, IncludedFiles, ExcludedFilecount, ExcludedFiles, ElapsedTime)</para>
    /// <para><i>Create</i> (<b>Required: </b>Files <b>Optional: Size</b>). Creates file(s)</para>
    /// <para><i>GetChecksum</i> (<b>Required: </b>Path <b>Output: </b>Checksum)</para>
    /// <para><i>GetTempFileName</i> (<b>Output: </b>Path)</para>
    /// <para><i>FilterByContent</i> (<b>Required: </b>Files, RegexPattern <b>Optional: </b>RegexOptionList <b>Output: </b>IncludedFiles, IncludedFilecount, ExcludedFilecount, ExcludedFiles)</para>
    /// <para><i>Move</i> (<b>Required: </b>Path, TargetPath)</para>
    /// <para><i>RemoveAttributes</i> (<b>Required: </b>Files)</para>
    /// <para><i>RemoveLines</i> (<b>Required: </b>Files, Lines <b>Optional: </b>RegexOptionList, AvoidRegex, MatchWholeLine). This will remove lines from a file. Lines is a regular expression unless AvoidRegex is specified</para>
    /// <para><i>RemoveSecurity</i> (<b>Required: Users, AccessType, Path or Files</b> Optional: Permission</para>
    /// <para><i>Replace</i> (<b>Required: </b>RegexPattern <b>Optional: </b>Replacement, Path, TextEncoding, Files, RegexOptionList)</para>
    /// <para><i>SetAttributes</i> (<b>Required: </b>Files)</para>
    /// <para><i>WriteLines</i> (<b>Required: </b>Files, Lines). This will add lines to a file if the file does NOT contain them. The match is case insensitive.</para>
    /// <para><b>Remote Execution Support:</b> No</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
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
    ///         <AtFiles2 Include="c:\demo\file1.txt">
    ///             <Attributes>Normal</Attributes>
    ///         </AtFiles2>
    ///         <MyFiles Include="C:\demo\**\*.csproj"/>
    ///         <FilesToSecure Include="C:\demo\file1.txt" />
    ///         <FilesToSecure Include="C:\demo\file2.txt" />
    ///         <Users Include="MyUser" />
    ///         <UsersWithPermissions Include="MyUser">
    ///             <Permission>Read,Write</Permission>
    ///         </UsersWithPermissions>
    ///         <FilesToWriteTo Include="C:\a\hosts"/>
    ///         <LinesToRemove Include="192\.156\.236\.25 www\.myurl\.com"/>
    ///         <LinesToRemove Include="192\.156\.234\.25 www\.myurl\.com"/>
    ///         <LinesToRemove Include="192\.156\.23sss4\.25 www\.myurl\.com"/>
    ///         <Lines Include="192.156.236.25 www.myurl.com"/>
    ///         <Lines Include="192.156.234.25 www.myurl.com"/>
    ///         <FilesToCreate Include="d:\a\File1-100.txt"/>
    ///         <FilesToCreate Include="d:\a\File2-100.txt"/>
    ///         <FilesToCreate Include="d:\a\File3-5000000.txt">
    ///             <size>5000000</size>
    ///         </FilesToCreate>
    ///         <FilesToCreate Include="d:\a\File4-100.txt"/>
    ///         <FilesToCheck Include="d:\a\*.*"/>
    ///         <FilesToConcatenate Include="c:\a\*.proj"/>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Concatenate Files -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="Concatenate" Files="@(FilesToConcatenate)" TargetPath="c:\concatenatedfile.txt"/>
    ///         <!-- Check whether files contain matching content -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="CheckContainsContent" Files="@(FilesToCheck)" RegexPattern="Hello">
    ///             <Output TaskParameter="Result" PropertyName="TheResult"/>
    ///         </MSBuild.ExtensionPack.FileSystem.File>
    ///         <Message Text="$(TheResult)"/>
    ///         <!-- Create some files. Defaults the size to 1000 bytes, but one file overrides this using metadata -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="Create" Files="@(FilesToCreate)" Size="1000"/>
    ///         <!-- Write lines to a file. Lines only added if file does not contain them -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="WriteLines" Files="@(FilesToWriteTo)" Lines="@(Lines)"/>
    ///         <!-- Remove lines from a file based on regular expressions -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="RemoveLines" Files="@(FilesToWriteTo)" Lines="@(LinesToRemove)"/>
    ///         <!-- adding security -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="AddSecurity" Path="C:\demo\file3.txt" Users="@(Users)" AccessType="Allow" Permission="Read,Write" />
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="AddSecurity" Files="@(FilesToSecure)" Users="@(UsersWithPermissions)" AccessType="Deny" />
    ///         <!-- remove security -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="RemoveSecurity" Path="C:\demo\file4.txt" Users="@(Users)" AccessType="Allow" Permission="Read,Write" />
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="RemoveSecurity" Files="@(FilesToSecure)" Users="@(UsersWithPermissions)" AccessType="Deny" />
    ///         <!-- Get a temp file -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="GetTempFileName">
    ///             <Output TaskParameter="Path" PropertyName="TempPath"/>
    ///         </MSBuild.ExtensionPack.FileSystem.File>
    ///         <Message Text="TempPath: $(TempPath)"/>
    ///         <!-- Filter a collection of files based on their content -->
    ///         <Message Text="MyProjects %(MyFiles.Identity)"/>
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="FilterByContent" RegexPattern="Microsoft.WebApplication.targets" Files="@(MyFiles)">
    ///             <Output TaskParameter="IncludedFiles" ItemName="WebProjects"/>
    ///             <Output TaskParameter="ExcludedFiles" ItemName="NonWebProjects"/>
    ///             <Output TaskParameter="IncludedFileCount" PropertyName="WebProjectsCount"/>
    ///             <Output TaskParameter="ExcludedFileCount" PropertyName="NonWebProjectsCount"/>
    ///         </MSBuild.ExtensionPack.FileSystem.File>
    ///         <Message Text="WebProjects: %(WebProjects.Identity)"/>
    ///         <Message Text="NonWebProjects: %(NonWebProjects.Identity)"/>
    ///         <Message Text="WebProjectsCount: $(WebProjectsCount)"/>
    ///         <Message Text="NonWebProjectsCount: $(NonWebProjectsCount)"/>
    ///         <!-- Get the checksum of a file -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="GetChecksum" Path="C:\Projects\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll">
    ///             <Output TaskParameter="Checksum" PropertyName="chksm"/>
    ///         </MSBuild.ExtensionPack.FileSystem.File>
    ///         <Message Text="$(chksm)"/>
    ///         <!-- Replace file content using a regular expression -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="Replace" RegexPattern="regex" RegexOptionList="IgnoreCase|Singleline" Replacement="iiiii" Files="@(FilesToParse)"/>
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="Replace" RegexPattern="regex" Replacement="idi" Path="c:\Demo*"/>
    ///         <!-- Count the number of lines in a file and exclude comments -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="CountLines" Files="@(FilesToCount)" CommentIdentifiers="//">
    ///             <Output TaskParameter="CodeLinecount" PropertyName="csharplines"/>
    ///             <Output TaskParameter="IncludedFiles" ItemName="MyIncludedFiles"/>
    ///             <Output TaskParameter="ExcludedFiles" ItemName="MyExcludedFiles"/>
    ///         </MSBuild.ExtensionPack.FileSystem.File>
    ///         <Message Text="C# CodeLinecount: $(csharplines)"/>
    ///         <Message Text="MyIncludedFiles: %(MyIncludedFiles.Identity)"/>
    ///         <Message Text="MyExcludedFiles: %(MyExcludedFiles.Identity)"/>
    ///         <!-- Count all lines in a file -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="CountLines" Files="@(AllFilesToCount)">
    ///             <Output TaskParameter="TotalLinecount" PropertyName="AllLines"/>
    ///         </MSBuild.ExtensionPack.FileSystem.File>
    ///         <Message Text="All Files TotalLinecount: $(AllLines)"/>
    ///         <!-- Set some attributes -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="SetAttributes" Files="@(AtFiles)"/>
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="SetAttributes" Files="@(AtFiles2)"/>
    ///         <!-- Move a file -->
    ///         <MSBuild.ExtensionPack.FileSystem.File TaskAction="Move" Path="c:\demo\file.txt" TargetPath="c:\dddd\d\oo\d\mee.txt"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class File : BaseTask
    {
        private const string ConcatenateTaskAction = "Concatenate";
        private const string CountLinesTaskAction = "CountLines";
        private const string CreateTaskAction = "Create";
        private const string GetChecksumTaskAction = "GetChecksum";
        private const string FilterByContentTaskAction = "FilterByContent";
        private const string CheckContainsContentTaskAction = "CheckContainsContent";
        private const string ReplaceTaskAction = "Replace";
        private const string SetAttributesTaskAction = "SetAttributes";
        private const string AddAttributesTaskAction = "AddAttributes";
        private const string MoveTaskAction = "Move";
        private const string RemoveAttributesTaskAction = "RemoveAttributes";
        private const string GetTempFileNameTaskAction = "GetTempFileName";
        private const string AddSecurityTaskAction = "AddSecurity";
        private const string RemoveSecurityTaskAction = "RemoveSecurity";
        private const string RemoveLinesTaskAction = "RemoveLines";
        private const string WriteLinesTaskAction = "WriteLines";

        private Encoding fileEncoding = Encoding.UTF8;
        private string replacement = string.Empty;
        private RegexOptions regexOptions = RegexOptions.Compiled;
        private Regex parseRegex;
        private string[] commentIdentifiers;
        private List<ITaskItem> excludedFiles;
        private List<ITaskItem> includedFiles;
        private AccessControlType accessType;

        /// <summary>
        /// Set to true to avoid using Regular Expressions. This may increase performance for certain operations against large files.
        /// </summary>
        public bool AvoidRegex { get; set; }

        /// <summary>
        /// Used with AvoidRegex. Set to true to match the whole line. The default is false i.e. a line.Contains operation is used.
        /// </summary>
        public bool MatchWholeLine { get; set; }
    
        /// <summary>
        /// Set the AccessType. Can be Allow or Deny. Default is Allow.
        /// </summary>
        public string AccessType
        {
            get { return this.accessType.ToString(); }
            set { this.accessType = (AccessControlType)Enum.Parse(typeof(AccessControlType), value); }
        }

        /// <summary>
        /// A comma-separated list of <a href="http://msdn.microsoft.com/en-us/library/942f991b.aspx">FileSystemRights</a>.
        /// </summary>
        /// <remarks>If Permission is not set, the task will look for Permission meta-data on each user item.</remarks>
        public string Permission { get; set; }

        /// <summary>
        /// Sets the users collection. Use the Permission metadata tag to specify permissions. Separate pemissions with a comma.
        /// <remarks>
        /// The Permission metadata is only used if the Permission property is not set.
        /// <code lang="xml"><![CDATA[
        /// <UsersCol Include="AUser">
        ///     <Permission>Read,etc</Permission>
        /// </UsersCol>
        /// ]]></code>
        /// </remarks>
        /// </summary>
        public ITaskItem[] Users { get; set; }

        /// <summary>
        /// Sets the Lines to use. For WriteLines this is interpreted as plain text. For RemoveLines this is interpreted as a regular expression
        /// </summary>
        public ITaskItem[] Lines { get; set; }

        /// <summary>
        /// Sets the regex pattern.
        /// </summary>
        public string RegexPattern { get; set; }

        /// <summary>
        /// The replacement text to use. Default is string.Empty
        /// </summary>
        public string Replacement
        {
            get { return this.replacement; }
            set { this.replacement = value; }
        }

        /// <summary>
        /// Sets the Regular Expression options, e.g. None|IgnoreCase|Multiline|ExplicitCapture|Compiled|Singleline|IgnorePatternWhitespace|RightToLeft|RightToLeft|ECMAScript|CultureInvariant  Default is RegexOptions.Compiled
        /// </summary>
        public string RegexOptionList
        {
            get
            {
                return null;
            }

            set
            {
                if (string.IsNullOrEmpty(value) || value == "None")
                {
                    return;
                }

                this.regexOptions = new RegexOptions();

                var strTemp = value.Split('|');
                if (strTemp.Contains("IgnoreCase"))
                {
                    this.regexOptions |= RegexOptions.IgnoreCase;
                }

                if (strTemp.Contains("Multiline"))
                {
                    this.regexOptions |= RegexOptions.Multiline;
                }

                if (strTemp.Contains("ExplicitCapture"))
                {
                    this.regexOptions |= RegexOptions.ExplicitCapture;
                }

                if (strTemp.Contains("Compiled"))
                {
                    this.regexOptions |= RegexOptions.Compiled;
                }

                if (strTemp.Contains("Singleline"))
                {
                    this.regexOptions |= RegexOptions.Singleline;
                }

                if (strTemp.Contains("IgnorePatternWhitespace"))
                {
                    this.regexOptions |= RegexOptions.IgnorePatternWhitespace;
                }

                if (strTemp.Contains("RightToLeft"))
                {
                    this.regexOptions |= RegexOptions.RightToLeft;
                }

                if (strTemp.Contains("ECMAScript"))
                {
                    this.regexOptions |= RegexOptions.ECMAScript;
                }

                if (strTemp.Contains("CultureInvariant"))
                {
                    this.regexOptions |= RegexOptions.CultureInvariant;
                }
            }
        }

        /// <summary>
        /// A path to process or get. Use * for recursive folder processing. For the GetChecksum TaskAction, this indicates the path to the file to create a checksum for.
        /// </summary>
        [Output]
        public ITaskItem Path { get; set; }

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
        /// An ItemList of files to process. If calling SetAttributes, RemoveAttributes or AddAttributes, include the attributes in an Attributes metadata tag, separated by a semicolon.
        /// </summary>
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// Sets the TargetPath for a renamed file or to save concatenated files
        /// </summary>
        public ITaskItem TargetPath { get; set; }

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
        /// Sets the minimum size of files to count
        /// </summary>
        public int MinSize { get; set; }

        /// <summary>
        /// Sets the size of the file in bytes for TaskAction="Create". This can be overridden by using a metadata tag called size on the Files items.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets the time taken to count the files. Value in seconds.
        /// </summary>
        [Output]
        public string ElapsedTime { get; set; }

        /// <summary>
        /// Gets the result
        /// </summary>
        [Output]
        public bool Result { get; set; }

        /// <summary>
        /// Gets the file checksum
        /// </summary>
        [Output]
        public string Checksum { get; set; }

        /// <summary>
        /// Item collection of files Excluded from the count.
        /// </summary>
        [Output]
        public ITaskItem[] ExcludedFiles
        {
            get { return this.excludedFiles == null ? null : this.excludedFiles.ToArray(); }
            set { this.excludedFiles = new List<ITaskItem>(value); }
        }

        /// <summary>
        /// Item collection of files included after filtering operations
        /// </summary>
        [Output]
        public ITaskItem[] IncludedFiles
        {
            get { return this.includedFiles == null ? null : this.includedFiles.ToArray(); }
            set { this.includedFiles = new List<ITaskItem>(value); }
        }

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
                case CountLinesTaskAction:
                    this.CountLines();
                    break;
                case CreateTaskAction:
                    this.Create();
                    break;
                case FilterByContentTaskAction:
                    this.FilterByContent();
                    break;
                case CheckContainsContentTaskAction:
                    this.CheckContainsContent();
                    break;
                case GetChecksumTaskAction:
                    this.GetChecksum();
                    break;
                case ReplaceTaskAction:
                    this.Replace();
                    break;
                case SetAttributesTaskAction:
                case AddAttributesTaskAction:
                case RemoveAttributesTaskAction:
                    this.SetAttributes();
                    break;
                case GetTempFileNameTaskAction:
                    this.LogTaskMessage("Getting temp file name");
                    this.Path = new TaskItem(System.IO.Path.GetTempFileName());
                    break;
                case MoveTaskAction:
                    this.Move();
                    break;
                case AddSecurityTaskAction:
                case RemoveSecurityTaskAction:
                    this.SetSecurity();
                    break;
                case RemoveLinesTaskAction:
                    this.RemoveLinesFromFile();
                    break;
                case ConcatenateTaskAction:
                    this.Concatenate();
                    break;
                case WriteLinesTaskAction:
                    this.WriteLinesToFile();
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

        private void CheckContainsContent()
        {
            if (this.Files == null)
            {
                Log.LogError("Files is required");
                return;
            }

            if (string.IsNullOrEmpty(this.RegexPattern))
            {
                Log.LogError("RegexPattern is required.");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking files contain content matching: {0}", this.RegexPattern));

            foreach (ITaskItem f in this.Files)
            {
                string entireFile;

                using (StreamReader streamReader = new StreamReader(f.ItemSpec))
                {
                    entireFile = streamReader.ReadToEnd();
                }

                // Load the regex to use
                this.parseRegex = new Regex(this.RegexPattern, this.regexOptions);

                // Match the regular expression pattern against a text string.
                Match m = this.parseRegex.Match(entireFile);
                if (m.Success)
                {
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Found in: {0}", f.ItemSpec));
                    this.Result = true;
                    return;
                }

                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Not found in: {0}", f.ItemSpec));
            }
        }
        
        private void Concatenate()
        {
            this.LogTaskMessage("Concatenating Files");

            if (this.Files == null)
            {
                Log.LogError("Files is required");
                return;
            }

            if (this.TargetPath == null)
            {
                Log.LogError("TargetPath is required");
                return;
            }

            if (this.TargetPath != null)
            {
                using (Stream output = System.IO.File.OpenWrite(this.TargetPath.ItemSpec))
                {
                    foreach (ITaskItem file in this.Files)
                    {
                        this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Reading File: {0}", file.ItemSpec));
                        using (Stream input = System.IO.File.OpenRead(file.ItemSpec))
                        {
                            input.CopyTo(output);
                        }
                    }
                }
            }
        }

        private void Create()
        {
            if (this.Files == null)
            {
                Log.LogError("Files is required");
                return;
            }

            foreach (ITaskItem file in this.Files)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating File: {0}", file.ItemSpec));
                System.IO.File.WriteAllBytes(file.ItemSpec, !string.IsNullOrEmpty(file.GetMetadata("size")) ? new byte[Convert.ToInt32(file.GetMetadata("size"), CultureInfo.CurrentCulture)] : new byte[this.Size]);
            }
        }

        private void WriteLinesToFile()
        {
            if (this.Files == null)
            {
                Log.LogError("Files is required");
                return;
            }

            if (this.Lines == null)
            {
                Log.LogError("Lines is required");
                return;
            }

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

            foreach (ITaskItem file in this.Files)
            {
                this.WriteLines(file.ItemSpec, true);
            }
        }

        private void WriteLines(string parseFile, bool checkExists)
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Writing Lines to File: {0}", parseFile));
            if (checkExists && System.IO.File.Exists(parseFile) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "The file does not exist: {0}", parseFile));
                return;
            }

            // Open the file and attempt to read the encoding from the BOM.
            using (StreamReader streamReader = new StreamReader(parseFile, this.fileEncoding, true))
            {
                streamReader.Read();
                if (this.fileEncoding == null)
                {
                    this.fileEncoding = streamReader.CurrentEncoding;
                }
            }

            List<string> fileLineList = System.IO.File.ReadAllLines(parseFile).ToList();
            List<string> newlines = fileLineList;
            bool linesAdded = false;
            foreach (ITaskItem line in this.Lines)
            {
                bool match = fileLineList.Any(fileLine => string.Compare(fileLine, line.ItemSpec, StringComparison.OrdinalIgnoreCase) == 0);

                if (!match)
                {
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Writing line {0}", line.ItemSpec));
                    newlines.Add(line.ItemSpec);
                    linesAdded = true;
                }
            }

            if (linesAdded)
            {
                FileAttributes fileAttributes = System.IO.File.GetAttributes(parseFile);
                bool changedAttribute = false;

                // If readonly attribute is set, reset it.
                if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Making File Writeable: {0}", parseFile));
                    System.IO.File.SetAttributes(parseFile, fileAttributes ^ FileAttributes.ReadOnly);
                    changedAttribute = true;
                }

                System.IO.File.WriteAllLines(parseFile, newlines.ToArray(), this.fileEncoding);
                if (changedAttribute)
                {
                    this.LogTaskMessage(MessageImportance.Low, "Making file readonly");
                    System.IO.File.SetAttributes(parseFile, FileAttributes.ReadOnly);
                }
            }
        }

        private void RemoveLinesFromFile()
        {
            if (this.Files == null)
            {
                Log.LogError("Files is required");
                return;
            }

            if (this.Lines == null)
            {
                Log.LogError("Lines is required");
                return;
            }

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

            foreach (ITaskItem file in this.Files)
            {
                this.RemoveLines(file.ItemSpec, true);
            }
        }

        private void RemoveLines(string parseFile, bool checkExists)
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing Lines from File: {0}", parseFile));
            if (checkExists && System.IO.File.Exists(parseFile) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "The file does not exist: {0}", parseFile));
                return;
            }

            FileAttributes fileAttributes = System.IO.File.GetAttributes(parseFile);
            bool changedAttribute = false;

            // If readonly attribute is set, reset it.
            if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Making File Writeable: {0}", parseFile));
                System.IO.File.SetAttributes(parseFile, fileAttributes ^ FileAttributes.ReadOnly);
                changedAttribute = true;
            }

            // Open the file and attempt to read the encoding from the BOM.
            using (StreamReader streamReader = new StreamReader(parseFile, this.fileEncoding, true))
            {
                streamReader.Read();
                if (this.fileEncoding == null)
                {
                    this.fileEncoding = streamReader.CurrentEncoding;
                }
            }

            List<string> fileLineList = System.IO.File.ReadAllLines(parseFile).ToList();
            List<string> newlines = new List<string>();
            bool linesRemoved = false;

            if (this.AvoidRegex)
            {
                foreach (string fileLine in fileLineList)
                {
                    bool match = this.MatchWholeLine ? this.Lines.Any(line => fileLine == line.ItemSpec) : this.Lines.Any(line => fileLine.Contains(line.ItemSpec));
                    if (!match)
                    {
                        newlines.Add(fileLine);
                    }
                    else
                    {
                        linesRemoved = true;
                        this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Removing line {0}", this.parseRegex));
                    }
                }
            }
            else
            {
                foreach (string fileLine in fileLineList)
                {
                    bool match = false;
                    foreach (ITaskItem line in this.Lines)
                    {
                        this.parseRegex = new Regex(line.ItemSpec, this.regexOptions);
                        Match m = this.parseRegex.Match(fileLine);
                        if (m.Success)
                        {
                            match = true;
                            break;
                        }
                    }

                    if (!match)
                    {
                        newlines.Add(fileLine);
                    }
                    else
                    {
                        linesRemoved = true;
                        this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Removing line {0}", this.parseRegex));
                    }
                }
            }

            if (linesRemoved)
            {
                System.IO.File.WriteAllLines(parseFile, newlines.ToArray(), this.fileEncoding);
            }

            if (changedAttribute)
            {
                this.LogTaskMessage(MessageImportance.Low, "Making file readonly");
                System.IO.File.SetAttributes(parseFile, FileAttributes.ReadOnly);
            }
        }

        private void SetSecurity()
        {
            var files = (this.Path == null) ? this.Files : new[] { this.Path };

            if (files == null || files.Length == 0)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Please supply a value for either the Path or Files property."));
                return;
            }

            if (this.Users == null || this.Users.Length == 0)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Please supply a value for the Users property."));
                return;
            }

            foreach (ITaskItem fileTaskItem in files)
            {
                var fileInfo = new FileInfo(fileTaskItem.GetMetadata("FullPath"));

                if (System.IO.File.Exists(fileInfo.FullName) == false)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "The file does not exist: {0}", fileInfo.FullName));
                    return;
                }

                FileSecurity currentSecurity = fileInfo.GetAccessControl();

                foreach (ITaskItem user in this.Users)
                {
                    string userName = user.ItemSpec;

                    string[] permissions = string.IsNullOrEmpty(this.Permission) ? user.GetMetadata("Permission").Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries) : this.Permission.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    FileSystemRights userRights = permissions.Aggregate(new FileSystemRights(), (current, s) => current | (FileSystemRights)Enum.Parse(typeof(FileSystemRights), s));

                    var accessRule = new FileSystemAccessRule(userName, userRights, InheritanceFlags.None, PropagationFlags.None, this.accessType);
                    if (this.TaskAction == AddSecurityTaskAction)
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding security for user: {0} on {1}", userName, fileInfo.FullName));
                        currentSecurity.AddAccessRule(accessRule);
                    }
                    else
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing security for user: {0} on {1}", userName, fileInfo.FullName));
                        if (permissions.Length == 0)
                        {
                            currentSecurity.RemoveAccessRuleAll(accessRule);
                        }
                        else
                        {
                            currentSecurity.RemoveAccessRule(accessRule);
                        }
                    }
                }

                // Set the new access settings.
                fileInfo.SetAccessControl(currentSecurity);
            }
        }

        private void FilterByContent()
        {
            if (this.Files == null)
            {
                Log.LogError("Files is required");
                return;
            }

            if (string.IsNullOrEmpty(this.RegexPattern))
            {
                Log.LogError("RegexPattern is required.");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Filter file collection by content: {0}", this.RegexPattern));

            this.includedFiles = new List<ITaskItem>();
            this.excludedFiles = new List<ITaskItem>();
            foreach (ITaskItem f in this.Files)
            {
                string entireFile;

                using (StreamReader streamReader = new StreamReader(f.ItemSpec))
                {
                    entireFile = streamReader.ReadToEnd();
                }

                // Load the regex to use
                this.parseRegex = new Regex(this.RegexPattern, this.regexOptions);

                // Match the regular expression pattern against a text string.
                Match m = this.parseRegex.Match(entireFile);
                if (m.Success)
                {
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Included: {0}", f.ItemSpec));
                    this.includedFiles.Add(f);
                }
                else
                {
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Excluded: {0}", f.ItemSpec));
                    this.excludedFiles.Add(f);
                }
            }

            this.IncludedFilecount = this.includedFiles.Count;
            this.ExcludedFilecount = this.excludedFiles.Count;
        }

        private void SetAttributes()
        {
            if (this.Files == null)
            {
                Log.LogError("Files is required");
                return;
            }

            switch (this.TaskAction)
            {
                case SetAttributesTaskAction:
                    this.LogTaskMessage("Setting file attributes");
                    foreach (ITaskItem f in this.Files)
                    {
                        FileInfo afile = new FileInfo(f.ItemSpec) { Attributes = SetAttributes(f.GetMetadata("Attributes").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) };
                    }

                    break;
                case AddAttributesTaskAction:
                    this.LogTaskMessage("Adding file attributes");
                    foreach (ITaskItem f in this.Files)
                    {
                        FileInfo file = new FileInfo(f.ItemSpec);
                        file.Attributes = file.Attributes | SetAttributes(f.GetMetadata("Attributes").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                    }

                    break;
                case RemoveAttributesTaskAction:
                    this.LogTaskMessage("Removing file attributes");
                    foreach (ITaskItem f in this.Files)
                    {
                        FileInfo file = new FileInfo(f.ItemSpec);
                        file.Attributes = file.Attributes & ~SetAttributes(f.GetMetadata("Attributes").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                    }

                    break;
            }
        }

        private void GetChecksum()
        {
            if (!System.IO.File.Exists(this.Path.GetMetadata("FullPath")))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid File passed: {0}", this.Path));
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting Checksum for file: {0}", this.Path));
            using (FileStream fs = System.IO.File.OpenRead(this.Path.GetMetadata("FullPath")))
            {
                using (MD5CryptoServiceProvider csp = new MD5CryptoServiceProvider())
                {
                    byte[] hash = csp.ComputeHash(fs);
                    this.Checksum = BitConverter.ToString(hash).Replace("-", string.Empty).ToUpperInvariant();
                }
            }
        }

        private void Move()
        {
            if (!System.IO.File.Exists(this.Path.GetMetadata("FullPath")))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid File passed: {0}", this.Path));
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Moving File: {0} to: {1}", this.Path, this.TargetPath));

            // If the TargetPath has multiple folders, then we need to create the parent
            DirectoryInfo f = new DirectoryInfo(this.TargetPath.GetMetadata("FullPath"));
            string parentPath = this.TargetPath.GetMetadata("FullPath").Replace(@"\" + f.Name, string.Empty);
            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }
            else if (System.IO.File.Exists(this.TargetPath.GetMetadata("FullPath")))
            {
                System.IO.File.Delete(this.TargetPath.GetMetadata("FullPath"));
            }

            System.IO.File.Move(this.Path.GetMetadata("FullPath"), this.TargetPath.GetMetadata("FullPath"));
        }

        private void CountLines()
        {
            if (this.Files == null)
            {
                Log.LogError("Files is required");
                return;
            }

            this.LogTaskMessage("Counting Lines");
            DateTime start = DateTime.Now;
            this.excludedFiles = new List<ITaskItem>();
            this.includedFiles = new List<ITaskItem>();
            
            foreach (ITaskItem f in this.Files)
            {
                if (this.MaxSize > 0 || this.MinSize > 0)
                {
                    FileInfo thisFile = new FileInfo(f.ItemSpec);
                    if (this.MaxSize > 0 && thisFile.Length / 1024 > this.MaxSize)
                    {
                        this.excludedFiles.Add(f);
                        break;
                    }

                    if (this.MinSize > 0 && thisFile.Length / 1024 < this.MinSize)
                    {
                        this.excludedFiles.Add(f);
                        break;
                    }
                }
                
                this.IncludedFilecount++;
                this.includedFiles.Add(f);
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
                this.ExcludedFilecount = this.excludedFiles.Count;
            }
            
            TimeSpan t = DateTime.Now - start;
            this.ElapsedTime = t.Seconds.ToString(CultureInfo.CurrentCulture);
            this.CodeLinecount = this.TotalLinecount - this.CommentLinecount - this.EmptyLinecount;
            this.TotalFilecount = this.IncludedFilecount + this.ExcludedFilecount;
        }

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
            this.parseRegex = new Regex(this.RegexPattern, this.regexOptions);

            // Check to see if we are processing a file collection or a path
            if (this.Path != null)
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

        private void ProcessPath()
        {
            bool recursive = false;
            if (this.Path.ItemSpec.EndsWith("*", StringComparison.OrdinalIgnoreCase))
            {
                this.Path.ItemSpec = this.Path.ItemSpec.Remove(this.Path.ItemSpec.Length - 1, 1);
                recursive = true;
            }

            // Validation
            if (Directory.Exists(this.Path.ItemSpec) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Path not found: {0}", this.Path.ItemSpec));
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Processing Path: {0} with RegEx: {1}, ReplacementText: {2}", this.Path, this.RegexPattern, this.Replacement));

            // Check if we need to do a recursive search
            if (recursive)
            {
                // We have to do a recursive search
                // Create a new DirectoryInfo object.
                DirectoryInfo dir = new DirectoryInfo(this.Path.ItemSpec);

                if (!dir.Exists)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "The directory does not exist: {0}", this.Path.ItemSpec));
                    return;
                }

                // Call the GetFileSystemInfos method.
                FileSystemInfo[] infos = dir.GetFileSystemInfos("*");
                this.ProcessFolder(infos);
            }
            else
            {
                DirectoryInfo dir = new DirectoryInfo(this.Path.ItemSpec);

                if (!dir.Exists)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "The directory does not exist: {0}", this.Path.ItemSpec));
                    return;
                }

                FileInfo[] fileInfo = dir.GetFiles();

                foreach (FileInfo f in fileInfo)
                {
                    this.ParseAndReplaceFile(f.FullName, false);
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

        private void ProcessCollection()
        {
            if (this.Files == null)
            {
                this.Log.LogError("No file collection has been passed");
                return;
            }

            this.LogTaskMessage("Processing File Collection");

            foreach (ITaskItem file in this.Files)
            {
                this.ParseAndReplaceFile(file.ItemSpec, true);
            }
        }

        private void ParseAndReplaceFile(string parseFile, bool checkExists)
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Processing File: {0}", parseFile));
            if (checkExists && System.IO.File.Exists(parseFile) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "The file does not exist: {0}", parseFile));
                return;
            }

            // Open the file and attempt to read the encoding from the BOM.
            string entireFile;

            using (StreamReader streamReader = new StreamReader(parseFile, this.fileEncoding, true))
            {
                entireFile = streamReader.ReadToEnd();
                if (this.fileEncoding == null)
                {
                    this.fileEncoding = streamReader.CurrentEncoding;
                }
            }

            // Parse the entire file.
            string newFile = this.parseRegex.Replace(entireFile, this.Replacement);

            if (newFile != entireFile)
            {
                // First make sure the file is writable.
                FileAttributes fileAttributes = System.IO.File.GetAttributes(parseFile);
                bool changedAttribute = false;

                // If readonly attribute is set, reset it.
                if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Making File Writeable: {0}", parseFile));
                    System.IO.File.SetAttributes(parseFile, fileAttributes ^ FileAttributes.ReadOnly);
                    changedAttribute = true;
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

                if (changedAttribute)
                {
                    this.LogTaskMessage(MessageImportance.Low, "Making file readonly");
                    System.IO.File.SetAttributes(parseFile, FileAttributes.ReadOnly);
                }
            }
        }
    }
}