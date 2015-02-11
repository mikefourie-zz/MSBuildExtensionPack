//-----------------------------------------------------------------------
// <copyright file="TfsVersion.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.VisualStudio
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>GetVersion</i> (<b>Required: </b> TfsBuildNumber, VersionFormat <b>Optional:</b>Major, Minor, BuildNumberRegex, PaddingCount, PaddingDigit, StartDate, DateFormat, BuildName, Delimiter, Build, Revision, VersionTemplateFormat, CombineBuildAndRevision, UseUtcDate<b>Output: </b>Version, Major, Minor, Build, Revision)</para>
    /// <para><b>Please Note:</b> The output of GetVersion should not be used to change the $(BuildNumber). For guidance, see: http://freetodev.spaces.live.com/blog/cns!EC3C8F2028D842D5!404.entry</para>
    /// <para><i>SetVersion</i> (<b>Required: </b> Version, Files <b>Optional:</b> TextEncoding, SetAssemblyVersion, AssemblyVersion, SetAssemblyFileVersion, ForceSetVersion</para>
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
    ///     <ItemGroup>
    ///         <FilesToVersion Include="C:\Demo\CommonAssemblyInfo.cs"/>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Get a version number based on the elapsed days since a given date -->
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsVersion TaskAction="GetVersion" BuildName="YOURBUILD" TfsBuildNumber="YOURBUILD_20080703.1" VersionFormat="Elapsed" StartDate="17 Nov 1976" PaddingCount="4" PaddingDigit="1" Major="3" Minor="5">
    ///             <Output TaskParameter="Version" PropertyName="NewVersion" />
    ///         </MSBuild.ExtensionPack.VisualStudio.TfsVersion>
    ///         <Message Text="Elapsed Version is $(NewVersion)"/>
    ///         <!-- Get a version number based on the format of a given datetime -->
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsVersion TaskAction="GetVersion" BuildName="YOURBUILD" TfsBuildNumber="YOURBUILD_20080703.1" VersionFormat="DateTime" DateFormat="MMdd" PaddingCount="5" PaddingDigit="1" Major="3" Minor="5">
    ///             <Output TaskParameter="Version" PropertyName="NewVersion" />
    ///         </MSBuild.ExtensionPack.VisualStudio.TfsVersion>
    ///         <Message Text="Date Version is $(NewVersion)"/>
    ///         <!-- Set the version in a collection of files -->
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsVersion TaskAction="SetVersion" Files="%(FilesToVersion.Identity)" Version="$(NewVersion)"/>
    ///         <!-- Set the version in a collection of files, forcing AssemblyFileVersion to be inserted even if it was not present in the affected file -->
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsVersion TaskAction="SetVersion" Files="%(FilesToVersion.Identity)" Version="$(NewVersion)" ForceSetVersion="true"/>
    ///         <!-- Get a version number based on the elapsed days since a given date and use a comma as the delimiter -->
    ///         <MSBuild.ExtensionPack.VisualStudio.TfsVersion TaskAction="GetVersion" Delimiter="," BuildName="YOURBUILD" TfsBuildNumber="YOURBUILD_20080703.1" VersionFormat="Elapsed" StartDate="17 Nov 1976" PaddingCount="4" PaddingDigit="1" Major="3" Minor="5">
    ///             <Output TaskParameter="Version" PropertyName="NewcppVersion" />
    ///         </MSBuild.ExtensionPack.VisualStudio.TfsVersion>
    ///         <Message Text="C++ Version: $(NewcppVersion)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example> 
    public class TfsVersion : BaseTask
    {
        private const string AppendAssemblyVersionFormat = "\n[assembly: System.Reflection.AssemblyVersion(\"{0}\")]";
        private const string VBAppendAssemblyVersionFormat = "\n<assembly: System.Reflection.AssemblyVersion(\"{0}\")>";
        private const string AppendAssemblyFileVersionFormat = "\n[assembly: System.Reflection.AssemblyFileVersion(\"{0}\")]";
        private const string VBAppendAssemblyFileVersionFormat = "\n<assembly: System.Reflection.AssemblyFileVersion(\"{0}\")>";
        private bool setAssemblyFileVersion = true;
        private Regex regexExpression;
        private Regex regexAssemblyVersion;
        private Encoding fileEncoding = Encoding.UTF8;
        private string delimiter = ".";
        private string buildnumberRegex = @"\d+\.\d+\.\d+\.\d+";

        /// <summary>
        /// Sets the BuildNumberRegex to determine the verison number from the BuildNumber when using in Synced mode. Default is \d+\.\d+\.\d+\.\d+
        /// </summary>
        public string BuildNumberRegex
        {
            get { return this.buildnumberRegex; }
            set { this.buildnumberRegex = value; }
        }

        /// <summary>
        /// Set to True to set the AssemblyVersion when calling SetVersion. Default is false.
        /// </summary>
        public bool SetAssemblyVersion { get; set; }

        /// <summary>
        /// Set to True to get the elapsed calculation using UTC Date Time. Default is false
        /// </summary>
        public bool UseUtcDate { get; set; }
        
        /// <summary>
        /// Set to True to set the AssemblyFileVersion when calling SetVersion. Default is true.
        /// </summary>
        public bool SetAssemblyFileVersion
        {
            get { return this.setAssemblyFileVersion; }
            set { this.setAssemblyFileVersion = value; }
        }

        /// <summary>
        /// Set to true to force SetVersion action to update files that do not have AssemblyVersion | AssemblyFileVersion
        /// present.  Default is false.  ForceSetVersion does not affect AssemblyVersion when SetAssemblyVersion is false.
        /// </summary>
        public bool ForceSetVersion { get; set; }

        /// <summary>
        /// Sets the file encoding. Default is UTF8
        /// </summary>
        public string TextEncoding { get; set; }

        /// <summary>
        /// Sets the files to version
        /// </summary>
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// Sets the Tfs Build Number. Use $(BuildNumber) for Tfs 2005 and 2008.
        /// </summary>
        public string TfsBuildNumber { get; set; }

        /// <summary>
        /// Gets or Sets the Version
        /// </summary>
        [Output]
        public string Version { get; set; }

        /// <summary>
        /// Sets the AssemblyVersion. Defaults to Version if not set.
        /// </summary>
        public string AssemblyVersion { get; set; }

        /// <summary>
        /// Sets the number of padding digits to use, e.g. 4
        /// </summary>
        public int PaddingCount { get; set; }

        /// <summary>
        /// Sets the padding digit to use, e.g. 0
        /// </summary>
        public char PaddingDigit { get; set; }

        /// <summary>
        /// Sets the start date to use when using VersionFormat="Elapsed"
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Sets the date format to use when using VersionFormat="DateTime". e.g. MMdd
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// Sets the name of the build. For Tfs 2005 use $(BuildType), for Tfs 2008 use $(BuildDefinition)
        /// </summary>
        public string BuildName { get; set; }

        /// <summary>
        /// Sets the Version Format. Valid VersionFormats are Elapsed, DateTime, Synced
        /// </summary>
        public string VersionFormat { get; set; }

        /// <summary>
        /// Sets the minor version
        /// </summary>
        [Output]
        public string Minor { get; set; }

        /// <summary>
        /// Sets the major version
        /// </summary>
        [Output]
        public string Major { get; set; }

        /// <summary>
        /// Gets or Sets the Build version
        /// </summary>
        [Output]
        public string Build { get; set; }

        /// <summary>
        /// Gets or Sets the Revision version
        /// </summary>
        [Output]
        public string Revision { get; set; }

        /// <summary>
        /// Sets whether to make the revision a combination of the Build and Revision.
        /// </summary>
        public bool CombineBuildAndRevision { get; set; }

        /// <summary>
        /// Sets the Delimiter to use in the version number. Default is .
        /// </summary>
        public string Delimiter
        {
            get { return this.delimiter; }
            set { this.delimiter = value; }
        }

        /// <summary>
        /// Specify the format of the build number. A format for each part must be specified or left blank, e.g. "00.000.00.000", "..0000.0"
        /// </summary>
        public string VersionTemplateFormat { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            switch (this.TaskAction)
            {
                case "GetVersion":
                    this.GetVersion();
                    break;
                case "SetVersion":
                    this.SetVersion();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void GetVersion()
        {
            if (string.IsNullOrEmpty(this.TfsBuildNumber))
            {
                Log.LogError("TfsBuildNumber is required");
                return;
            }

            if (string.IsNullOrEmpty(this.VersionFormat))
            {
                Log.LogError("VersionFormat is required");
                return;
            }

            this.LogTaskMessage("Getting Version");
            if (this.VersionFormat == "Synced")
            {
                Regex r = new Regex(this.BuildNumberRegex, RegexOptions.Compiled);
                var s = r.Match(this.TfsBuildNumber).Value;
                this.Version = s;
            }
            else
            {
                if (string.IsNullOrEmpty(this.BuildName))
                {
                    Log.LogError("BuildName is required");
                    return;
                }

                string buildstring = this.TfsBuildNumber.Replace(this.BuildName + "_", string.Empty);
                string[] buildParts = buildstring.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                DateTime t = new DateTime(Convert.ToInt32(buildParts[0].Substring(0, 4), CultureInfo.CurrentCulture), Convert.ToInt32(buildParts[0].Substring(4, 2), CultureInfo.CurrentCulture), Convert.ToInt32(buildParts[0].Substring(6, 2), CultureInfo.InvariantCulture));

                DateTime baseTimeToUse = DateTime.Now;
                if (this.UseUtcDate)
                {
                    baseTimeToUse = DateTime.UtcNow;
                }

                if (string.IsNullOrEmpty(this.Revision))
                {
                    if (this.CombineBuildAndRevision)
                    {
                        switch (this.VersionFormat.ToUpperInvariant())
                        {
                            case "ELAPSED":
                                TimeSpan elapsed = baseTimeToUse - Convert.ToDateTime(this.StartDate);
                                this.Revision = elapsed.Days.ToString(CultureInfo.CurrentCulture).PadLeft(this.PaddingCount, this.PaddingDigit) + buildParts[1];
                                break;
                            case "DATETIME":
                                this.Revision = t.ToString(this.DateFormat, CultureInfo.CurrentCulture).PadLeft(this.PaddingCount, this.PaddingDigit) + buildParts[1];
                                break;
                        }
                    }
                    else
                    {
                        this.Revision = buildParts[1];
                    }
                }

                switch (this.VersionFormat.ToUpperInvariant())
                {
                    case "ELAPSED":
                        TimeSpan elapsed = baseTimeToUse - Convert.ToDateTime(this.StartDate);
                        if (string.IsNullOrEmpty(this.Build))
                        {
                            this.Build = elapsed.Days.ToString(CultureInfo.CurrentCulture).PadLeft(this.PaddingCount, this.PaddingDigit);
                        }

                        this.Version = string.Format(CultureInfo.CurrentCulture, "{0}{4}{1}{4}{2}{4}{3}", this.Major, this.Minor, this.Build, this.Revision, this.delimiter);
                        break;
                    case "DATETIME":
                        if (string.IsNullOrEmpty(this.Build))
                        {
                            this.Build = t.ToString(this.DateFormat, CultureInfo.CurrentCulture).PadLeft(this.PaddingCount, this.PaddingDigit);
                        }

                        this.Version = string.Format(CultureInfo.CurrentCulture, "{0}{4}{1}{4}{2}{4}{3}", this.Major, this.Minor, this.Build, this.Revision, this.delimiter);
                        break;
                    default:
                        Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid VersionFormat provided: {0}. Valid Formats are Elapsed, DateTime", this.VersionFormat));
                        return;
                }
            }

            // Check if format is provided
            if (!string.IsNullOrEmpty(this.VersionTemplateFormat))
            {
                // get the current version number parts
                int[] buildparts = this.Version.Split(char.Parse(this.delimiter)).Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();

                // get the format parts
                string[] formatparts = this.VersionTemplateFormat.Split(char.Parse(this.delimiter));

                // format each part
                string[] newparts = new string[4];
                for (int i = 0; i <= 3; i++)
                {
                    newparts[i] = buildparts[i].ToString(formatparts[i], CultureInfo.InvariantCulture);
                }

                this.Major = newparts[0];
                this.Minor = newparts[1];
                this.Build = newparts[2];
                this.Revision = newparts[3];

                // reset the version to the required format
                this.Version = string.Format(CultureInfo.CurrentCulture, "{0}{4}{1}{4}{2}{4}{3}", newparts[0], newparts[1], newparts[2], newparts[3], this.delimiter);
            }
        }

        private void SetVersion()
        {
            // Set the file encoding if necessary
            if (!string.IsNullOrEmpty(this.TextEncoding) && !this.SetFileEncoding())
            {
                return;
            }

            if (string.IsNullOrEmpty(this.Version))
            {
                Log.LogError("Version is required");
                return;
            }

            if (this.Files == null)
            {
                Log.LogError("No Files specified. Pass an Item Collection of files to the Files property.");
                return;
            }

            if (string.IsNullOrEmpty(this.AssemblyVersion))
            {
                this.AssemblyVersion = this.Version;
            }

            // Load the regex to use
            this.regexExpression = new Regex(@"AssemblyFileVersion.*\(.*""" + ".*" + @""".*\)", RegexOptions.Compiled);
            if (this.SetAssemblyVersion)
            {
                this.regexAssemblyVersion = new Regex(@"AssemblyVersion.*\(.*""" + ".*" + @""".*\)", RegexOptions.Compiled);
            }

            foreach (ITaskItem file in this.Files)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Versioning {0} at {1}", file.ItemSpec, this.Version));
                bool changedAttribute = false;

                // First make sure the file is writable.
                FileAttributes fileAttributes = File.GetAttributes(file.ItemSpec);

                // If readonly attribute is set, reset it.
                if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    this.LogTaskMessage(MessageImportance.Low, "Making file writable");
                    File.SetAttributes(file.ItemSpec, fileAttributes ^ FileAttributes.ReadOnly);
                    changedAttribute = true;
                }

                // Open the file
                string entireFile;
                using (StreamReader streamReader = new StreamReader(file.ItemSpec, true))
                {
                    entireFile = streamReader.ReadToEnd();
                }

                // Parse the entire file.
                string newFile = this.regexExpression.Replace(entireFile, @"AssemblyFileVersion(""" + this.Version + @""")");

                if (this.SetAssemblyFileVersion)
                {
                    if (this.ForceSetVersion && newFile.Equals(entireFile, StringComparison.OrdinalIgnoreCase) && newFile.IndexOf("AssemblyFileVersion", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        switch (file.GetMetadata("Extension"))
                        {
                            case ".cs":
                                newFile = newFile.AppendFormat(AppendAssemblyFileVersionFormat, this.Version);
                                break;
                            case ".vb":
                                newFile = newFile.AppendFormat(VBAppendAssemblyFileVersionFormat, this.Version);
                                break;
                        }
                    }
                }

                if (this.SetAssemblyVersion)
                {
                    string originalFile = newFile;
                    newFile = this.regexAssemblyVersion.Replace(newFile, @"AssemblyVersion(""" + this.AssemblyVersion + @""")");
                    if (this.ForceSetVersion && newFile.Equals(originalFile, StringComparison.OrdinalIgnoreCase) && newFile.IndexOf("AssemblyVersion", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        switch (file.GetMetadata("Extension"))
                        {
                            case ".cs":
                                newFile = newFile.AppendFormat(AppendAssemblyVersionFormat, this.AssemblyVersion);
                                break;
                            case ".vb":
                                newFile = newFile.AppendFormat(VBAppendAssemblyVersionFormat, this.AssemblyVersion);
                                break;
                        }
                    }
                }

                // Write out the new contents.
                using (StreamWriter streamWriter = new StreamWriter(file.ItemSpec, false, this.fileEncoding))
                {
                    streamWriter.Write(newFile);
                }

                if (changedAttribute)
                {
                    this.LogTaskMessage(MessageImportance.Low, "Making file readonly");
                    File.SetAttributes(file.ItemSpec, FileAttributes.ReadOnly);
                }
            }
        }

        /// <summary>
        /// Sets the file encoding.
        /// </summary>
        /// <returns>bool</returns>
        private bool SetFileEncoding()
        {
            switch (this.TextEncoding)
            {
                case "ASCII":
                    this.fileEncoding = System.Text.Encoding.ASCII;
                    break;
                case "Unicode":
                    this.fileEncoding = System.Text.Encoding.Unicode;
                    break;
                case "UTF7":
                    this.fileEncoding = System.Text.Encoding.UTF7;
                    break;
                case "UTF8":
                    this.fileEncoding = System.Text.Encoding.UTF8;
                    break;
                case "BigEndianUnicode":
                    this.fileEncoding = System.Text.Encoding.BigEndianUnicode;
                    break;
                case "UTF32":
                    this.fileEncoding = System.Text.Encoding.UTF32;
                    break;
                default:
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "Encoding not supported: {0}", this.TextEncoding));
                    return false;
            }

            return true;
        }
    }
}