//-----------------------------------------------------------------------
// <copyright file="Thread.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System.Globalization;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Abort</i> (Warning: use only in exceptional circumstances to force an abort)</para>
    /// <para><i>Sleep</i> (<b>Required: </b> Timeout)</para>
    /// <para><i>SpinWait</i> (<b>Required: </b> Iterations)</para>
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
    ///         <!-- Set a thread to sleep for a period -->
    ///         <MSBuild.ExtensionPack.Framework.Thread TaskAction="Sleep" Timeout="2000"/>
    ///         <!-- Set a thread to spinwait for a period -->
    ///         <MSBuild.ExtensionPack.Framework.Thread TaskAction="SpinWait" Iterations="1000000000"/>
    ///         <!-- Abort a thread. Only use in exceptional circumstances -->
    ///         <!--<MSBuild.ExtensionPack.Framework.Thread TaskAction="Abort"/>-->
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class Thread : BaseTask
    {
        /// <summary>
        /// Number of millseconds to sleep for
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Number of iterations to wait for
        /// </summary>
        public int Iterations { get; set; }

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
                case "Abort":
                    this.LogTaskMessage("Aborting Current Thread");
                    System.Threading.Thread thisThread = System.Threading.Thread.CurrentThread;
                    thisThread.Abort();
                    break;
                case "Sleep":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Sleeping all threads for: {0}ms", this.Timeout));
                    System.Threading.Thread.Sleep(this.Timeout);
                    break;
                case "SpinWait":
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "SpinWait all threads for: {0} iterations", this.Iterations));
                    System.Threading.Thread.SpinWait(this.Iterations);
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }
    }
}