//-----------------------------------------------------------------------
// <copyright file="Cab.cs">(c) FreeToDev. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Compression
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Management;
    using System.Text;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddFile</i> (<b>Required: </b>NewFile, CabFile, CabExePath, ExtractExePath, NewFileDestination)</para>
    /// <para><i>Create</i> (<b>Required: </b>PathToCab or FilesToCab, CabFile, ExePath. <b>Optional: </b>PreservePaths, StripPrefixes, Recursive)</para>
    /// <para><i>Extract</i> (<b>Required: </b>CabFile, ExtractExePath, ExtractTo <b>Optional:</b> ExtractFile)</para>
    /// <para><b>Compatible with:</b></para>
    ///     <para>Microsoft (R) Cabinet Tool (cabarc.exe) - Version 5.2.3790.0</para>
    ///     <para>Microsoft (R) CAB File Extract Utility (extrac32.exe)- Version 5.2.3790.0</para>
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
    ///     <Target Name="Default">
    ///         <ItemGroup>
    ///             <!-- Create a collection of files to CAB -->
    ///             <Files Include="C:\aaa\**\*"/>
    ///         </ItemGroup>
    ///         <!-- Create the CAB using the File collection and preserve the paths whilst stripping a prefix -->
    ///         <MSBuild.ExtensionPack.Compression.Cab TaskAction="Create" FilesToCab="@(Files)" CabExePath="D:\BuildTools\CabArc.Exe" CabFile="C:\newcabbyitem.cab" PreservePaths="true" StripPrefixes="aaa\"/>
    ///         <!-- Create the same CAB but this time based on the Path. Note that Recursive is required -->
    ///         <MSBuild.ExtensionPack.Compression.Cab TaskAction="Create" PathToCab="C:\aaa\*" CabExePath="D:\BuildTools\CabArc.Exe" CabFile="C:\newcabbypath.cab" PreservePaths="true" StripPrefixes="aaa\" Recursive="true"/>
    ///         <!-- Add a file to the CAB -->
    ///         <MSBuild.ExtensionPack.Compression.Cab TaskAction="AddFile" NewFile="c:\New Text Document.txt" CabExePath="D:\BuildTools\CabArc.Exe" ExtractExePath="D:\BuildTools\Extrac32.EXE" CabFile="C:\newcabbyitem.cab" NewFileDestination="\Any Path"/>
    ///         <!-- Extract a CAB-->
    ///         <MSBuild.ExtensionPack.Compression.Cab TaskAction="Extract" ExtractTo="c:\a111" ExtractExePath="D:\BuildTools\Extrac32.EXE" CabFile="C:\newcabbyitem.cab"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Cab : BaseTask
    {
        private string extractFile = "/E";

        /// <summary>
        /// Sets the path to extract to
        /// </summary>
        public string ExtractTo { get; set; }

        /// <summary>
        /// Sets the CAB file. Required.
        /// </summary>
        [Required]
        public string CabFile { get; set; }

        /// <summary>
        /// Sets the path to cab
        /// </summary>
        public string PathToCab { get; set; }

        /// <summary>
        /// Sets whether to add files and folders recursively if PathToCab is specified.
        /// </summary>
        public bool Recursive { get; set; }
        
        /// <summary>
        /// Sets the files to cab
        /// </summary>
        public ITaskItem[] FilesToCab { get; set; }

        /// <summary>
        /// Sets the path to CabArc.Exe
        /// </summary>
        public string CabExePath { get; set; }

        /// <summary>
        /// Sets the path to extrac32.exe
        /// </summary>
        public string ExtractExePath { get; set; }

        /// <summary>
        /// Sets the files to extract. Default is /E, which is all.
        /// </summary>
        public string ExtractFile
        {
            get { return this.extractFile; }
            set { this.extractFile = value; }
        }

        /// <summary>
        /// Sets a value indicating whether [preserve paths]
        /// </summary>
        public bool PreservePaths { get; set; }

        /// <summary>
        /// Sets the prefixes to strip. Delimit with ';'
        /// </summary>
        public string StripPrefixes { get; set; }

        /// <summary>
        /// Sets the new file to add to the Cab File
        /// </summary>
        public string NewFile { get; set; }

        /// <summary>
        /// Sets the path to add the file to
        /// </summary>
        public string NewFileDestination { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            // Resolve TaskAction
            switch (this.TaskAction)
            {
                case "Create":
                    this.Create();
                    break;
                case "Extract":
                    this.Extract();
                    break;
                case "AddFile":
                    this.AddFile();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        /// <summary>
        /// Adds the file.
        /// </summary>
        private void AddFile()
        {
            // Validation
            if (!this.ValidateExtract())
            {
                return;
            }

            if (!System.IO.File.Exists(this.NewFile))
            {
                this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "New File not found: {0}", this.NewFile));
                return;
            }

            FileInfo f = new FileInfo(this.NewFile);

            this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Adding File: {0} to Cab: {1}", this.NewFile, this.CabFile));
            string tempFolderName = System.Guid.NewGuid() + "\\";

            DirectoryInfo dirInfo = new DirectoryInfo(Path.Combine(Path.GetTempPath(), tempFolderName));
            Directory.CreateDirectory(dirInfo.FullName);

            if (dirInfo.Exists)
            {
                this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Created: {0}", dirInfo.FullName));
            }
            else
            {
                Log.LogError(string.Format("Failed to create temp folder: {0}", dirInfo.FullName));
                return;
            }

            // configure the process we need to run
            using (Process cabProcess = new Process())
            {
                this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Extracting Cab: {0}", this.CabFile));
                cabProcess.StartInfo.FileName = this.ExtractExePath;
                cabProcess.StartInfo.UseShellExecute = true;
                cabProcess.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, @"/Y /L ""{0}"" ""{1}"" ""{2}""", dirInfo.FullName, this.CabFile, "/E");
                this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Calling {0} with {1}", this.ExtractExePath, cabProcess.StartInfo.Arguments));
                cabProcess.Start();
                cabProcess.WaitForExit();
            }

            Directory.CreateDirectory(dirInfo.FullName + "\\" + this.NewFileDestination);

            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Copying new File: {0} to {1}", this.NewFile, dirInfo.FullName + "\\" + this.NewFileDestination + "\\" + f.Name));
            System.IO.File.Copy(this.NewFile, dirInfo.FullName + this.NewFileDestination + @"\" + f.Name, true);

            using (Process cabProcess = new Process())
            {
                this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Creating Cab: {0}", this.CabFile));
                cabProcess.StartInfo.FileName = this.CabExePath;
                cabProcess.StartInfo.UseShellExecute = false;
                cabProcess.StartInfo.RedirectStandardOutput = true;

                StringBuilder options = new StringBuilder();
                options.Append("-r -p");
                options.AppendFormat(" -P \"{0}\"\\", dirInfo.FullName.Remove(dirInfo.FullName.Length - 1).Replace(@"C:\", string.Empty));
                cabProcess.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, @"{0} N ""{1}"" {2}", options, this.CabFile, "\"" + dirInfo.FullName + "*.*\"" + " ");
                this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Calling {0} with {1}", this.CabExePath, cabProcess.StartInfo.Arguments));
                
                // start the process
                cabProcess.Start();

                // Read any messages from CABARC...and log them
                string output = cabProcess.StandardOutput.ReadToEnd();
                cabProcess.WaitForExit();

                if (output.Contains("Completed successfully"))
                {
                    this.Log.LogMessage(output);
                }
                else
                {
                    this.Log.LogError(output);
                }
            }

            string dirObject = string.Format("win32_Directory.Name='{0}'", dirInfo.FullName.Remove(dirInfo.FullName.Length - 1));
            using (ManagementObject mdir = new ManagementObject(dirObject))
            {
                this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.InvariantCulture, "Deleting Temp Folder: {0}", dirObject));
                mdir.Get();
                ManagementBaseObject outParams = mdir.InvokeMethod("Delete", null, null);

                // ReturnValue should be 0, else failure
                if (Convert.ToInt32(outParams.Properties["ReturnValue"].Value) != 0)
                {
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Directory deletion error: ReturnValue: {0}", outParams.Properties["ReturnValue"].Value));
                    return;
                }
            }
        }

        /// <summary>
        /// Extracts this instance.
        /// </summary>
        private void Extract()
        {
            // Validation
            if (this.ValidateExtract() == false)
            {
                return;
            }

            if (string.IsNullOrEmpty(this.ExtractTo))
            {
                this.Log.LogError("ExtractTo required.");
                return;
            }

            // configure the process we need to run
            using (Process cabProcess = new Process())
            {
                this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Extracting Cab: {0}", this.CabFile));
                cabProcess.StartInfo.FileName = this.ExtractExePath;
                cabProcess.StartInfo.UseShellExecute = true;
                cabProcess.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, @"/Y /L ""{0}"" ""{1}"" ""{2}""", this.ExtractTo, this.CabFile, this.ExtractFile);
                this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Calling {0} with {1}", this.ExtractExePath, cabProcess.StartInfo.Arguments));
                cabProcess.Start();
                cabProcess.WaitForExit();
            }
        }

        /// <summary>
        /// Validates the extract.
        /// </summary>
        /// <returns>bool</returns>
        private bool ValidateExtract()
        {
            // Validation
            if (System.IO.File.Exists(this.CabFile) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "CAB file not found: {0}", this.CabFile));
                return false;
            }

            if (string.IsNullOrEmpty(this.ExtractExePath))
            {
                if (System.IO.File.Exists(Environment.SystemDirectory + "extrac32.exe"))
                {
                    this.ExtractExePath = Environment.SystemDirectory + "extrac32.exe";
                }
                else
                {
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Executable not found: {0}", this.ExtractExePath));
                    return false;
                }
            }
            else
            {
                if (System.IO.File.Exists(this.ExtractExePath) == false)
                {
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Executable not found: {0}", this.ExtractExePath));
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates this instance.
        /// </summary>
        private void Create()
        {
            // Validation
            if (System.IO.File.Exists(this.CabExePath) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Executable not found: {0}", this.CabExePath));
                return;
            }

            using (Process cabProcess = new Process())
            {
                this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Creating Cab: {0}", this.CabFile));
                cabProcess.StartInfo.FileName = this.CabExePath;
                cabProcess.StartInfo.UseShellExecute = false;
                cabProcess.StartInfo.RedirectStandardOutput = true;

                StringBuilder options = new StringBuilder();
                if (this.PreservePaths)
                {
                    options.Append("-p");
                }

                if (!string.IsNullOrEmpty(this.PathToCab) && this.Recursive)
                {
                    options.Append(" -r ");
                }

                // Could be more than one prefix to strip...
                if (string.IsNullOrEmpty(this.StripPrefixes) == false)
                {
                    string[] prefixes = this.StripPrefixes.Split(';');
                    foreach (string prefix in prefixes)
                    {
                        options.AppendFormat(" -P {0}", prefix);
                    }
                }

                string files = string.Empty;
                if ((this.FilesToCab == null || this.FilesToCab.Length == 0) && string.IsNullOrEmpty(this.PathToCab))
                {
                    Log.LogError("FilesToCab or PathToCab must be supplied");
                    return;
                }

                if (!string.IsNullOrEmpty(this.PathToCab))
                {
                    files = this.PathToCab;
                }
                else
                {
                    foreach (ITaskItem file in this.FilesToCab)
                    {
                        files += "\"" + file.ItemSpec + "\"" + " ";
                    }
                }

                cabProcess.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, @"{0} N ""{1}"" {2}", options, this.CabFile, files);
                this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Calling {0} with {1}", this.CabExePath, cabProcess.StartInfo.Arguments));

                // start the process
                cabProcess.Start();

                // Read any messages from CABARC...and log them
                string output = cabProcess.StandardOutput.ReadToEnd();
                cabProcess.WaitForExit();

                if (output.Contains("Completed successfully"))
                {
                    this.Log.LogMessage(MessageImportance.Low, output);
                }
                else
                {
                    this.Log.LogError(output);
                }
            }
        }
    }
}