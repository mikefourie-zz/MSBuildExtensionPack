//-----------------------------------------------------------------------
// <copyright file="DateAndTime.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Globalization;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Get</i> (<b>Required: </b>Format <b>Output: </b> Result)</para>
    /// <para><i>GetElapsed</i> (<b>Required: </b>Format, Start <b>Optional: </b>End <b>Output: </b> Result)</para>
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
    ///     <PropertyGroup>
    ///         <Start>1 Jan 2000</Start>
    ///     </PropertyGroup>
    ///     <Target Name="Default">
    ///         <!-- Get the elapsed days since the start date -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="GetElapsed" Start="$(Start)" Format="Days">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Days Since $(Start): $(DTResult)"/>
    ///         <!-- Get the elapsed minutes since the start date -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="GetElapsed" Start="$(Start)" Format="Minutes">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Minutes Since $(Start): $(DTResult)"/>
    ///         <!-- Get the elapsed hours since the start date -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="GetElapsed" Start="$(Start)" Format="Hours">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Hours Since $(Start): $(DTResult)"/>
    ///         <!-- Get the total elapsed time since the start date -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="GetElapsed" Start="$(Start)" Format="Total">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Total Elapsed Time Since $(Start): $(DTResult)"/>
    ///         <!-- Get the time in the specified format -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="Get" Format="dd MMM yy">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Date / Time: $(DTResult)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.1.0/html/1c668832-24f2-d646-1f66-7ea1f3e76415.htm")]
    public class DateAndTime : BaseTask
    {
        private const string GetTaskAction = "Get";
        private const string GetElapsedTaskAction = "GetElapsed";

        [DropdownValue(GetTaskAction)]
        [DropdownValue(GetElapsedTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// The start time to use for GetElapsed
        /// </summary>
        [TaskAction(GetElapsedTaskAction, true)]
        public DateTime Start { get; set; }

        /// <summary>
        /// The end time to use for GetElapsed. Default is DateTime.Now
        /// </summary>
        [TaskAction(GetElapsedTaskAction, false)]
        public DateTime End { get; set; }

        /// <summary>
        /// Format to apply to the Result. For GetTime, Format can be any valid DateTime format. For GetElapsed, Format can be Milliseconds, Seconds, Minutes, Hours, Days or Total. Total returns dd:hh:mm:ss
        /// </summary>
        [Required]
        [TaskAction(GetTaskAction, true)]
        [TaskAction(GetElapsedTaskAction, true)]
        public string Format { get; set; }

        /// <summary>
        /// The output Result
        /// </summary>
        [Output]
        [TaskAction(GetTaskAction, false)]
        [TaskAction(GetElapsedTaskAction, false)]
        public string Result { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            switch (this.TaskAction)
            {
                case "Get":
                    this.GetDate();
                    break;
                case "GetElapsed":
                    this.GetElapsed();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void GetElapsed()
        {
            if (this.Start == Convert.ToDateTime("01/01/0001 00:00:00", CultureInfo.CurrentCulture))
            {
                Log.LogError("Start must be specified");
                return;
            }

            if (this.End == Convert.ToDateTime("01/01/0001 00:00:00", CultureInfo.CurrentCulture))
            {
                this.End = DateTime.Now;
            }
            
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting Elapsed: {0}", this.Format));
            TimeSpan t = this.End - this.Start;

            switch (this.Format)
            {
                case "MilliSeconds":
                    this.Result = t.TotalMilliseconds.ToString(CultureInfo.CurrentCulture);
                    break;
                case "Seconds":
                    this.Result = t.TotalSeconds.ToString(CultureInfo.CurrentCulture);
                    break;
                case "Minutes":
                    this.Result = t.TotalMinutes.ToString(CultureInfo.CurrentCulture);
                    break;
                case "Hours":
                    this.Result = t.TotalHours.ToString(CultureInfo.CurrentCulture);
                    break;
                case "Days":
                    this.Result = t.TotalDays.ToString(CultureInfo.CurrentCulture);
                    break;
                case "Total":
                    this.Result = string.Format(CultureInfo.CurrentCulture, "{0}:{1}:{2}:{3}", t.Days.ToString("00", CultureInfo.CurrentCulture), t.Hours.ToString("00", CultureInfo.CurrentCulture), t.Minutes.ToString("00", CultureInfo.CurrentCulture), t.Seconds.ToString("00", CultureInfo.CurrentCulture));
                    break;
            }
        }

        private void GetDate()
        {
            this.LogTaskMessage("Getting Date / Time");
            this.Result = DateTime.Now.ToString(this.Format, CultureInfo.CurrentCulture);
        }
    }
}