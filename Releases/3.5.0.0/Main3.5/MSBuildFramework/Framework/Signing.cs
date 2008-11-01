//-----------------------------------------------------------------------
// <copyright file="Signing.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
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
    /// <para><i>Sign</i> (<b>Required: </b> Assemblies, KeyFile <b>Optional: </b> ToolPath)</para>
    /// <para><i>AddSkipVerification</i> (<b>Required: </b> PublicKeyToken <b>Optional: </b> ToolPath)</para>
    /// <para><i>RemoveAllSkipVerification</i> (<b>Optional: </b> ToolPath)</para>
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
        public string KeyFile { get; set; }

        /// <summary>
        /// Sets the folder path to sn.exe
        /// </summary>
        public string ToolPath { get; set; }

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
            this.Log.LogMessage("Removing all SkipVerification");
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

            this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Adding SkipVerification for: {0}", this.PublicKeyToken));
            CommandLineBuilder commandLine = new CommandLineBuilder();
            commandLine.AppendSwitch("-q -Vr");
            commandLine.AppendSwitch("*," + this.PublicKeyToken);
            this.Run(commandLine.ToString());
        }

        private void Sign()
        {
            if (!System.IO.File.Exists(this.KeyFile))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "KeyFile not found: {0}", this.KeyFile));
                return;   
            }

            foreach (ITaskItem assembly in this.Assemblies)
            {
                FileInfo fi = new FileInfo(assembly.ItemSpec);
                if (fi.Exists)
                {
                    this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Signing Assembly: {0}", assembly.ItemSpec));

                    CommandLineBuilder commandLine = new CommandLineBuilder();
                    commandLine.AppendSwitch("-q -R");
                    commandLine.AppendFileNameIfNotNull(assembly);
                    commandLine.AppendFileNameIfNotNull(this.KeyFile);
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
            Process proc = new Process { StartInfo = { FileName = Path.Combine(this.ToolPath, ToolName), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true } };
            proc.StartInfo.Arguments = args;
            this.Log.LogMessage(MessageImportance.Low, "Running " + proc.StartInfo.FileName + " " + proc.StartInfo.Arguments);
            proc.Start();
            string outputStream = proc.StandardOutput.ReadToEnd();
            if (outputStream.Length > 0)
            {
                this.Log.LogMessage(MessageImportance.Low, outputStream);
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
                return;
            }
        }
    }
}