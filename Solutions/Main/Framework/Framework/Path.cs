//-----------------------------------------------------------------------
// <copyright file="Path.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System.Globalization;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>ChangeExtension</i> (<b>Required: </b> Filepath, Extension <b>Output: </b>Value)</para>
    /// <para><i>Combine</i> (<b>Required: </b> Filepath, Filepath2 <b>Output: </b>Value)</para>
    /// <para><i>GetDirectoryName</i> (<b>Required: </b> Filepath <b>Output: </b>Value)</para>
    /// <para><i>GetExtension</i> (<b>Required: </b> Filepath <b>Output: </b>Value)</para>
    /// <para><i>GetFileName</i> (<b>Required: </b> Filepath <b>Output: </b>Value)</para>
    /// <para><i>GetFileNameWithoutExtension</i> (<b>Required: </b> Filepath <b>Output: </b>Value)</para>
    /// <para><i>GetFullPath</i> (<b>Required: </b> Filepath <b>Output: </b>Value)</para>
    /// <para><i>GetPathRoot</i> (<b>Required: </b> Filepath <b>Output: </b>Value)</para>
    /// <para><i>GetRandomFileName</i> (<b>Output: </b>Value)</para>
    /// <para><i>GetTempPath</i> (<b>Output: </b>Value)</para>
    /// <para><i>HasExtension</i> (<b>Required: </b> Filepath <b>Output: </b>Value)</para>
    /// <para><i>IsPathRooted</i> (<b>Required: </b> Filepath <b>Output: </b>Value)</para>
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
    ///         <MSBuild.ExtensionPack.Framework.Path TaskAction="ChangeExtension" Filepath="c:\temp\filename.txt" Extension="log">
    ///             <Output TaskParameter="Value" PropertyName="NewFilename" />
    ///         </MSBuild.ExtensionPack.Framework.Path>
    ///         <Message Text="NewFilename = $(NewFilename)"/>
    ///         <MSBuild.ExtensionPack.Framework.Path TaskAction="Combine" Filepath="c:\temp" Filepath2="filename.txt">
    ///             <Output TaskParameter="Value" PropertyName="CombinedFilename" />
    ///         </MSBuild.ExtensionPack.Framework.Path>
    ///         <Message Text="CombinedFilename = $(CombinedFilename)"/>
    ///         <MSBuild.ExtensionPack.Framework.Path TaskAction="GetDirectoryName" Filepath="c:\temp\filename.txt">
    ///             <Output TaskParameter="Value" PropertyName="JustTheDirectory" />
    ///         </MSBuild.ExtensionPack.Framework.Path>
    ///         <Message Text="JustTheDirectory = $(JustTheDirectory)"/>
    ///         <MSBuild.ExtensionPack.Framework.Path TaskAction="GetExtension" Filepath="c:\temp\filename.txt">
    ///             <Output TaskParameter="Value" PropertyName="JustTheExtension" />
    ///         </MSBuild.ExtensionPack.Framework.Path>
    ///         <Message Text="JustTheExtension = $(JustTheExtension)"/>
    ///         <MSBuild.ExtensionPack.Framework.Path TaskAction="GetFileName" Filepath="c:\temp\filename.txt">
    ///             <Output TaskParameter="Value" PropertyName="JustTheFilename" />
    ///         </MSBuild.ExtensionPack.Framework.Path>
    ///         <Message Text="JustTheFilename = $(JustTheFilename)"/>
    ///         <MSBuild.ExtensionPack.Framework.Path TaskAction="GetFileNameWithoutExtension" Filepath="c:\temp\filename.txt">
    ///             <Output TaskParameter="Value" PropertyName="JustTheFilename" />
    ///         </MSBuild.ExtensionPack.Framework.Path>
    ///         <Message Text="JustTheFilename = $(JustTheFilename)"/>
    ///         <MSBuild.ExtensionPack.Framework.Path TaskAction="GetFullPath" Filepath="c:\temp\filename.txt">
    ///             <Output TaskParameter="Value" PropertyName="FullPath" />
    ///         </MSBuild.ExtensionPack.Framework.Path>
    ///         <Message Text="FullPath = $(FullPath)"/>
    ///         <MSBuild.ExtensionPack.Framework.Path TaskAction="GetPathRoot" Filepath="c:\temp\filename.txt">
    ///             <Output TaskParameter="Value" PropertyName="PathRoot" />
    ///         </MSBuild.ExtensionPack.Framework.Path>
    ///         <Message Text="PathRoot = $(PathRoot)"/>
    ///         <MSBuild.ExtensionPack.Framework.Path TaskAction="GetRandomFileName">
    ///             <Output TaskParameter="Value" PropertyName="RandomFilename" />
    ///         </MSBuild.ExtensionPack.Framework.Path>
    ///         <Message Text="RandomFilename = $(RandomFilename)"/>
    ///         <MSBuild.ExtensionPack.Framework.Path TaskAction="GetTempPath">
    ///             <Output TaskParameter="Value" PropertyName="TempPath" />
    ///         </MSBuild.ExtensionPack.Framework.Path>
    ///         <Message Text="TempPath = $(TempPath)"/>
    ///         <MSBuild.ExtensionPack.Framework.Path TaskAction="HasExtension" Filepath="c:\temp\filename.txt">
    ///             <Output TaskParameter="Value" PropertyName="FileHasAnExtension" />
    ///         </MSBuild.ExtensionPack.Framework.Path>
    ///         <Message Text="FileHasAnExtension = $(FileHasAnExtension)"/>
    ///         <MSBuild.ExtensionPack.Framework.Path TaskAction="IsPathRooted" Filepath="c:\temp\filename.txt">
    ///             <Output TaskParameter="Value" PropertyName="FileIsRooted" />
    ///         </MSBuild.ExtensionPack.Framework.Path>
    ///         <Message Text="FileIsRooted = $(FileIsRooted)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Path : BaseTask
    {
        public const string ChangeExtensionTaskAction = "ChangeExtension";
        public const string CombineTaskAction = "Combine";
        public const string GetDirectoryNameTaskAction = "GetDirectoryName";
        public const string GetExtensionTaskAction = "GetExtension";
        public const string GetFileNameTaskAction = "GetFileName";
        public const string GetFileNameWithoutExtensionTaskAction = "GetFileNameWithoutExtension";
        public const string GetFullPathTaskAction = "GetFullPath";
        public const string GetPathRootTaskAction = "GetPathRoot";
        public const string GetRandomFileNameTaskAction = "GetRandomFileName";
        public const string GetTempPathTaskAction = "GetTempPath";
        public const string HasExtensionTaskAction = "HasExtension";
        public const string IsPathRootedTaskAction = "IsPathRooted";

        /// <summary>
        /// The file path to use
        /// </summary>
        public string Filepath { get; set; }

        /// <summary>
        /// The file path to use for the second filepath parameter for the Combine task
        /// </summary>
        public string Filepath2 { get; set; }

        /// <summary>
        /// The file extension to use for the ChangeExtension task
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// Gets value returned from the invoked Path method
        /// </summary>
        [Output]
        public string Value { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            if (!string.IsNullOrEmpty(this.Filepath))
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Invoking: {0} on: {1}", this.TaskAction, this.Filepath));
            }

            switch (this.TaskAction)
            {
                case ChangeExtensionTaskAction:
                    this.Value = System.IO.Path.ChangeExtension(this.Filepath, this.Extension);
                    break;
                case CombineTaskAction:
                    this.Value = System.IO.Path.Combine(this.Filepath, this.Filepath2);
                    break;
                case GetDirectoryNameTaskAction:
                    this.Value = System.IO.Path.GetDirectoryName(this.Filepath);
                    break;
                case GetExtensionTaskAction:
                    this.Value = System.IO.Path.GetExtension(this.Filepath);
                    break;
                case GetFileNameTaskAction:
                    this.Value = System.IO.Path.GetFileName(this.Filepath);
                    break;
                case GetFileNameWithoutExtensionTaskAction:
                    this.Value = System.IO.Path.GetFileNameWithoutExtension(this.Filepath);
                    break;
                case GetFullPathTaskAction:
                    this.Value = System.IO.Path.GetFullPath(this.Filepath);
                    break;
                case GetPathRootTaskAction:
                    this.Value = System.IO.Path.GetPathRoot(this.Filepath);
                    break;
                case GetRandomFileNameTaskAction:
                    this.LogTaskMessage("Getting Random File Name");
                    this.Value = System.IO.Path.GetRandomFileName();
                    break;
                case GetTempPathTaskAction:
                    this.LogTaskMessage("Getting Temp Path");
                    this.Value = System.IO.Path.GetTempPath();
                    break;
                case HasExtensionTaskAction:
                    this.Value = System.IO.Path.HasExtension(this.Filepath).ToString(CultureInfo.InvariantCulture);
                    break;
                case IsPathRootedTaskAction:
                    this.Value = System.IO.Path.IsPathRooted(this.Filepath).ToString(CultureInfo.InvariantCulture);
                    break;
                default:
                    this.LogError("Invalid TaskAction passed: {0}", this.TaskAction);
                    return;
            }
        }

        protected virtual void LogError(string format, params object[] args)
        {
            Log.LogError(string.Format(CultureInfo.CurrentCulture, format, args));
        }
    }
}