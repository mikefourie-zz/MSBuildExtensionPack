//-----------------------------------------------------------------------
// <copyright file="Signing.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddSkipVerification</i> (<b>Required: </b> PublicKeyToken <b>Optional: </b> ToolPath)</para>
    /// <para><i>RemoveAllSkipVerification</i> (<b>Optional: </b> ToolPath)</para>
    /// <para><i>Sign</i> (<b>Required: </b> Assemblies, KeyFile <b>Optional: </b> ToolPath)</para>
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
    ///     <Target Name="Default">
    ///         <ItemGroup>
    ///             <AssemblyToSign Include="C:\AnAssembly.dll"/>
    ///         </ItemGroup>
    ///         <!-- Sign an assembly -->
    ///         <MSBuild.ExtensionPack.Framework.Signing TaskAction="Sign" ToolPath="C:\Program Files\Microsoft SDKs\Windows\v6.0A\Bin" KeyFile="c:\aPrivateKey.snk" Assemblies="@(AssemblyToSign)"/>
    ///         <!-- Add SkipVerification for a public key -->
    ///         <MSBuild.ExtensionPack.Framework.Signing TaskAction="AddSkipVerification" ToolPath="C:\Program Files\Microsoft SDKs\Windows\v6.0A\Bin" PublicKeyToken="119b85861667ee6a"/>
    ///         <!-- Remove all SkipVerification -->
    ///         <MSBuild.ExtensionPack.Framework.Signing TaskAction="RemoveAllSkipVerification" ToolPath="C:\Program Files\Microsoft SDKs\Windows\v6.0A\Bin"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Signing : BaseTask
    {
        private const string ToolName = "sn.exe";

        /// <summary>
        /// Sets the KeyFile to use when Signing the Assemblies
        /// </summary>
        public ITaskItem KeyFile { get; set; }

        /// <summary>
        /// Sets the folder path to sn.exe
        /// </summary>
        public ITaskItem ToolPath { get; set; }

        /// <summary>
        /// Sets the PublicKeyToken for AddSkipVerification
        /// </summary>
        public string PublicKeyToken { get; set; }

        /// <summary>
        /// Sets the Item Collection of Assemblies to sign
        /// </summary>
        public ITaskItem[] Assemblies { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            switch (this.TaskAction)
            {
                case "Sign":
                    this.Sign();
                    break;
                case "AddSkipVerification":
                    this.SkipVerification();
                    break;
                case "RemoveAllSkipVerification":
                    this.RemoveAllSkipVerification();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void RemoveAllSkipVerification()
        {
            this.LogTaskMessage("Removing all SkipVerification");
            CommandLineBuilder commandLine = new CommandLineBuilder();
            commandLine.AppendSwitch("-q -Vx");
            this.Run(commandLine.ToString());
        }

        private void SkipVerification()
        {
            if (string.IsNullOrEmpty(this.PublicKeyToken))
            {
                this.Log.LogError("PublicKeyToken is required");
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding SkipVerification for: {0}", this.PublicKeyToken));
            CommandLineBuilder commandLine = new CommandLineBuilder();
            commandLine.AppendSwitch("-q -Vr");
            commandLine.AppendSwitch("*," + this.PublicKeyToken);
            this.Run(commandLine.ToString());
        }

        private void Sign()
        {
            if (this.KeyFile == null)
            {
                this.Log.LogError("KeyFile not supplied");
                return;
            }

            if (!System.IO.File.Exists(this.KeyFile.GetMetadata("FullPath")))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "KeyFile not found: {0}", this.KeyFile.GetMetadata("FullPath")));
                return;
            }

            if (this.Assemblies == null)
            {
                this.Log.LogError("Assemblies not supplied");
                return;
            }

            foreach (ITaskItem assembly in this.Assemblies)
            {
                FileInfo fi = new FileInfo(assembly.ItemSpec);
                if (fi.Exists)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Signing Assembly: {0}", assembly.ItemSpec));

                    CommandLineBuilder commandLine = new CommandLineBuilder();
                    commandLine.AppendSwitch("-q -R");
                    commandLine.AppendFileNameIfNotNull(assembly);
                    commandLine.AppendFileNameIfNotNull(this.KeyFile.GetMetadata("FullPath"));
                    this.Run(commandLine.ToString());
                    commandLine = new CommandLineBuilder();
                    commandLine.AppendSwitch("-vf");
                    commandLine.AppendFileNameIfNotNull(assembly);
                    this.Run(commandLine.ToString());
                }
                else
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Assembly not found: {0}", assembly.ItemSpec));
                }
            }
        }

        private void Run(string args)
        {
            string fileName = this.ToolPath != null ? System.IO.Path.Combine(this.ToolPath.GetMetadata("FullPath"), ToolName) : ToolName;
            if (!System.IO.File.Exists(fileName))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "sn.exe not found: {0}", fileName));
                return;
            }

            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = fileName;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.Arguments = args;
                this.LogTaskMessage(MessageImportance.Low, "Running " + proc.StartInfo.FileName + " " + proc.StartInfo.Arguments);
                proc.Start();
                string outputStream = proc.StandardOutput.ReadToEnd();
                if (outputStream.Length > 0)
                {
                    this.LogTaskMessage(MessageImportance.Low, outputStream);
                }

                string errorStream = proc.StandardError.ReadToEnd();
                if (errorStream.Length > 0)
                {
                    this.Log.LogError(errorStream);
                }

                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    this.Log.LogError("Non-zero exit code from sn.exe: " + proc.ExitCode);
                }
            }
        }
    }
}