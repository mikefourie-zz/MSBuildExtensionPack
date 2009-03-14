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
    /// <para><i>CheckBetween</i> (<b>Required: </b>Start, End <b>Output: </b> BoolResult)</para>
    /// <para><i>CheckLater</i> (<b>Required: </b>Start <b>Output: </b> BoolResult)</para>
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
    ///         <!-- Check if its later than a given time -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="CheckLater" Start="14:10">
    ///             <Output TaskParameter="BoolResult" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Later: $(DTResult)"/>
    ///         <!-- Check if the current time is between two times -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="CheckBetween" Start="14:10" End="14:25">
    ///             <Output TaskParameter="BoolResult" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Between: $(DTResult)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.2.0/html/1c668832-24f2-d646-1f66-7ea1f3e76415.htm")]
    public class DateAndTime : BaseTask
    {
        private const string GetTaskAction = "Get";
        private const string GetElapsedTaskAction = "GetElapsed";
        private const string CheckLaterTaskAction = "CheckLater";
        private const string CheckBetweenTaskAction = "CheckBetween";

        [DropdownValue(CheckBetweenTaskAction)]
        [DropdownValue(CheckLaterTaskAction)]
        [DropdownValue(GetTaskAction)]
        [DropdownValue(GetElapsedTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// The start time to use
        /// </summary>
        [TaskAction(CheckBetweenTaskAction, true)]
        [TaskAction(CheckLaterTaskAction, true)]
        [TaskAction(GetElapsedTaskAction, true)]
        public DateTime Start { get; set; }

        /// <summary>
        /// The end time to use for GetElapsed. Default is DateTime.Now
        /// </summary>
        [TaskAction(GetElapsedTaskAction, false)]
        [TaskAction(CheckBetweenTaskAction, true)]
        public DateTime End { get; set; }

        /// <summary>
        /// Format to apply to the Result. For GetTime, Format can be any valid DateTime format. For GetElapsed, Format can be Milliseconds, Seconds, Minutes, Hours, Days or Total. Total returns dd:hh:mm:ss
        /// </summary>
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

        /// <summary>
        /// The output boolean result.
        /// </summary>
        [Output]
        [TaskAction(CheckBetweenTaskAction, false)]
        [TaskAction(CheckLaterTaskAction, false)]
        public bool BoolResult { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            switch (this.TaskAction)
            {
                case GetTaskAction:
                    this.GetDate();
                    break;
                case GetElapsedTaskAction:
                    this.GetElapsed();
                    break;
                case CheckLaterTaskAction:
                    this.CheckLater();
                    break;
                case CheckBetweenTaskAction:
                    this.CheckBetween();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void CheckLater()
        {
            if (this.Start == Convert.ToDateTime("01/01/0001 00:00:00", CultureInfo.CurrentCulture))
            {
                Log.LogError("Start must be specified");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking if: {0} is later than: {1}", DateTime.Now.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture), this.Start.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture)));
            this.BoolResult = DateTime.Now > this.Start;
        }

        private void CheckBetween()
        {
            if (this.Start == Convert.ToDateTime("01/01/0001 00:00:00", CultureInfo.CurrentCulture))
            {
                Log.LogError("Start must be specified");
                return;
            }

            if (this.End == Convert.ToDateTime("01/01/0001 00:00:00", CultureInfo.CurrentCulture))
            {
                Log.LogError("End must be specified");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking if: {0} is between: {1} and: {2}", DateTime.Now.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture), this.Start.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture), this.End.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture)));
            this.BoolResult = DateTime.Now > this.Start && DateTime.Now < this.End;
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
                default:
                    Log.LogError("Format must be specified");
                    return;
            }
        }

        private void GetDate()
        {
            this.LogTaskMessage("Getting Date / Time");
            this.Result = DateTime.Now.ToString(this.Format, CultureInfo.CurrentCulture);
        }
    }
}