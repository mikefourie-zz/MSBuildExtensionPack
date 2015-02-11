//-----------------------------------------------------------------------
// <copyright file="FileVersion.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.FileSystem
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Increment</i> (<b>Required: </b>File <b>Optional: </b>Increment <b>Output: </b>Value)</para>
    /// <para><i>Reset</i> (<b>Required: </b>File <b>Optional: </b>Value <b>Output: </b>Value)</para>
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
    ///     <Target Name="Default">
    ///         <!-- Perform a default increment of 1 -->
    ///         <MSBuild.ExtensionPack.FileSystem.FileVersion TaskAction="Increment" File="C:\a\MyVersionfile.txt">
    ///             <Output TaskParameter="Value" PropertyName="NewValue"/>
    ///         </MSBuild.ExtensionPack.FileSystem.FileVersion>
    ///         <Message Text="$(NewValue)"/>
    ///         <!-- Perform an increment of 5 -->
    ///         <MSBuild.ExtensionPack.FileSystem.FileVersion TaskAction="Increment" File="C:\a\MyVersionfile2.txt" Increment="5">
    ///             <Output TaskParameter="Value" PropertyName="NewValue"/>
    ///         </MSBuild.ExtensionPack.FileSystem.FileVersion>
    ///         <Message Text="$(NewValue)"/>
    ///         <!-- Reset a file value -->
    ///         <MSBuild.ExtensionPack.FileSystem.FileVersion TaskAction="Reset" File="C:\a\MyVersionfile3.txt" Value="10">
    ///             <Output TaskParameter="Value" PropertyName="NewValue"/>
    ///         </MSBuild.ExtensionPack.FileSystem.FileVersion>
    ///         <Message Text="$(NewValue)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    public class FileVersion : BaseTask
    {
        private const string IncrementTaskAction = "Increment";
        private const string ResetTaskAction = "Reset";
        private FileInfo versionFile;
        private int increment = 1;
        private bool changedAttribute;
        private Encoding fileEncoding = Encoding.UTF8;

        /// <summary>
        /// The file to store the incrementing version in.
        /// </summary>
        public ITaskItem File { get; set; }

        /// <summary>
        /// Value to increment by. Default is 1.
        /// </summary>
        public int Increment
        {
            get { return this.increment; }
            set { this.increment = value; }
        }

        /// <summary>
        /// Gets value returned from the file, or used to reset the value in the file. Default is 0.
        /// </summary>
        [Output]
        public int Value { get; set; }
      
        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            this.versionFile = new FileInfo(this.File.ItemSpec);

            // Create the file if it doesn't exist
            if (!this.versionFile.Exists)
            {
                using (FileStream fs = this.versionFile.Create())
                {
                }
            }

            // First make sure the file is writable.
            FileAttributes fileAttributes = System.IO.File.GetAttributes(this.versionFile.FullName);

            // If readonly attribute is set, reset it.
            if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Making File Writeable: {0}", this.versionFile.FullName));
                System.IO.File.SetAttributes(this.versionFile.FullName, fileAttributes ^ FileAttributes.ReadOnly);
                this.changedAttribute = true;
            }
            
            switch (this.TaskAction)
            {
                case IncrementTaskAction:
                    this.IncrementValue();
                    break;
                case ResetTaskAction:
                    this.ResetValue();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }

            if (this.changedAttribute)
            {
                this.LogTaskMessage(MessageImportance.Low, "Making file readonly");
                System.IO.File.SetAttributes(this.versionFile.FullName, FileAttributes.ReadOnly);
            }
        }

        private void ResetValue()
        {
            this.WriteFile();
        }

        private void IncrementValue()
        {
            int currentValue;

            using (StreamReader streamReader = new StreamReader(this.versionFile.FullName, this.fileEncoding, true))
            {
                currentValue = Convert.ToInt32(streamReader.ReadLine(), CultureInfo.InvariantCulture);
                if (this.fileEncoding == null)
                {
                    this.fileEncoding = streamReader.CurrentEncoding;
                }
            }

            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Read: {0} from: {1}", currentValue, this.versionFile.FullName));
            this.Value = currentValue + this.Increment;
            if (currentValue != this.Value)
            {
                this.WriteFile();
            }
        }

        private void WriteFile()
        {
            // Write out the new file.
            using (StreamWriter streamWriter = new StreamWriter(this.versionFile.FullName, false, this.fileEncoding))
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Writing: {0} to: {1}", this.Value, this.versionFile.FullName));
                streamWriter.Write(this.Value);
            }    
        }
    }
}