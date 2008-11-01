//-----------------------------------------------------------------------
// <copyright file="TfsVersion.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.VisualStudio
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>GetVersion</i> (<b>Required: </b> TfsBuildNumber, Major, Minor, VersionFormat <b>Optional:</b> PaddingCount, PaddingDigit, StartDate, DateFormat, BuildName <b>Output: </b>Version)</para>
    /// <para><i>SetVersion</i> (<b>Required: </b> Version, Files <b>Optional:</b> TextEncoding, SetAssemblyVersion</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="3.5" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
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
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class TfsVersion : BaseTask
    {
        private Regex regexExpression;
        private Regex regexAssemblyVersion;
        private Encoding fileEncoding = Encoding.UTF8;

        /// <summary>
        /// Set to True to set the AssemblyVersion when calling SetVersion. Default is false.
        /// </summary>
        public bool SetAssemblyVersion { get; set; }

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
        /// Sets the Version Format. Valid VersionFormats are Elapsed, DateTime
        /// </summary>
        public string VersionFormat { get; set; }

        /// <summary>
        /// Sets the minor version
        /// </summary>
        public int Minor { get; set; }

        /// <summary>
        /// Sets the major version
        /// </summary>
        public int Major { get; set; }

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

            if (string.IsNullOrEmpty(this.BuildName))
            {
                Log.LogError("BuildName is required");
                return;
            }

            if (string.IsNullOrEmpty(this.VersionFormat))
            {
                Log.LogError("VersionFormat is required");
                return;
            }

            this.Log.LogMessage("Getting Version");
            string buildstring = this.TfsBuildNumber.Replace(this.BuildName + "_", string.Empty);
            string[] buildParts = buildstring.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            DateTime t = new DateTime(Convert.ToInt32(buildParts[0].Substring(0, 4), CultureInfo.CurrentCulture), Convert.ToInt32(buildParts[0].Substring(4, 2), CultureInfo.CurrentCulture), Convert.ToInt32(buildParts[0].Substring(6, 2), CultureInfo.InvariantCulture));
            switch (this.VersionFormat.ToUpperInvariant())
            {
                case "ELAPSED":
                    TimeSpan elapsed = DateTime.Today - Convert.ToDateTime(this.StartDate);
                    this.Version = string.Format(CultureInfo.CurrentCulture, "{0}.{1}.{2}.{3}", this.Major, this.Minor, elapsed.Days.ToString(CultureInfo.CurrentCulture).PadLeft(this.PaddingCount, this.PaddingDigit), buildParts[1]);
                    break;
                case "DATETIME":
                    this.Version = string.Format(CultureInfo.CurrentCulture, "{0}.{1}.{2}.{3}", this.Major, this.Minor, t.ToString(this.DateFormat, CultureInfo.CurrentCulture).PadLeft(this.PaddingCount, this.PaddingDigit), buildParts[1]);
                    break;
                default:
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid VersionFormat provided: {0}. Valid Formats are Elapsed, DateTime", this.VersionFormat));
                    return;
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

            // Load the regex to use
            this.regexExpression = new Regex(@"AssemblyFileVersion.*\(.*""" + ".*" + @""".*\)", RegexOptions.Compiled);
            if (this.SetAssemblyVersion)
            {
                this.regexAssemblyVersion = new Regex(@"AssemblyVersion.*\(.*""" + ".*" + @""".*\)", RegexOptions.Compiled);
            }

            foreach (ITaskItem file in this.Files)
            {
                this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Versioning {0} at {1}", file.ItemSpec, this.Version));
                bool changedAttribute = false;

                // First make sure the file is writable.
                FileAttributes fileAttributes = File.GetAttributes(file.ItemSpec);

                // If readonly attribute is set, reset it.
                if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    this.Log.LogMessage(MessageImportance.Low, "Making file writable");
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

                if (this.SetAssemblyVersion)
                {
                    newFile = this.regexAssemblyVersion.Replace(newFile, @"AssemblyVersion(""" + this.Version + @""")");
                }

                // Write out the new contents.
                using (StreamWriter streamWriter = new StreamWriter(file.ItemSpec, false, this.fileEncoding))
                {
                    streamWriter.Write(newFile);
                }

                if (changedAttribute)
                {
                    this.Log.LogMessage(MessageImportance.Low, "Making file readonly");
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