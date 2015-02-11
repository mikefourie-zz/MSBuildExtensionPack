//-----------------------------------------------------------------------
// <copyright file="DateAndTime.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Globalization;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddDays</i> (<b>Required: </b>Format, Value <b>Optional: </b>Start <b>Output: </b> Result)</para>
    /// <para><i>AddHours</i> (<b>Required: </b>Format, Value <b>Optional: </b>Start <b>Output: </b> Result)</para>
    /// <para><i>AddMilliseconds</i> (<b>Required: </b>Format, Value <b>Optional: </b>Start <b>Output: </b> Result)</para>
    /// <para><i>AddMinutes</i> (<b>Required: </b>Format, Value <b>Optional: </b>Start <b>Output: </b> Result)</para>
    /// <para><i>AddMonths</i> (<b>Required: </b>Format, Value <b>Optional: </b>Start <b>Output: </b> Result)</para>
    /// <para><i>AddSeconds</i> (<b>Required: </b>Format, Value <b>Optional: </b>Start <b>Output: </b> Result)</para>
    /// <para><i>AddTicks</i> (<b>Required: </b>Format, Value <b>Optional: </b>Start <b>Output: </b> Result)</para>
    /// <para><i>AddYears</i> (<b>Required: </b>Format, Value <b>Optional: </b>Start <b>Output: </b> Result)</para>
    /// <para><i>CheckBetween</i> (<b>Required: </b>Start, End <b>Optional:</b> UseUtc <b>Output: </b> BoolResult)</para>
    /// <para><i>CheckLater</i> (<b>Required: </b>Start <b>Optional:</b> UseUtc <b>Output: </b> BoolResult)</para>
    /// <para><i>Get</i> (<b>Required: </b>Format <b>Optional:</b> UseUtc <b>Output: </b> Result)</para>
    /// <para><i>GetElapsed</i> (<b>Required: </b>Format, Start <b>Optional: </b>End, UseUtc <b>Output: </b> Result)</para>
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
    ///     <PropertyGroup>
    ///         <Start>1 Jan 2009</Start>
    ///     </PropertyGroup>
    ///     <Target Name="Default">
    ///         <!-- Let's Time how long it takes to perform a certain group of tasks -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="Get" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="MyStartTime"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="I'm sleeping..."/>
    ///         <MSBuild.ExtensionPack.Framework.Thread TaskAction="Sleep" Timeout="2000"/>
    ///         <Message Text="Sleep Over!"/>
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="GetElapsed" Start="$(MyStartTime)" Format="Seconds">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Slept For: $(DTResult)"/>
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
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="Get" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Date / Time: $(DTResult)"/>
    ///         <!-- Get the UTC time in the specified format -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="Get" Format="dd MMM yy HH:mm:ss" UseUtc="true">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="UTC Date / Time: $(DTResult)"/>
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
    ///         <!-- Test Add time targets based on start time provided in AddTimeStart. -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="Get" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="AddTimeStart"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Add Time To: $(AddTimeStart)"/>
    ///         <!-- Add days -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddDays" Start="$(Start)" Value="30" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Add 30 days: $(DTResult)"/>
    ///         <!-- Verify add days -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="GetElapsed" Start="$(Start)" End="$(DTResult)" Format="Days">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Days Since $(Start): $(DTResult)"/>
    ///         <!-- Add hours -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddHours" Start="$(Start)" Value="30" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="30 hours from time: $(DTResult)"/>
    ///         <!-- Verify add hours -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="GetElapsed" Start="$(Start)" End="$(DTResult)" Format="Hours">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Hours Since $(Start): $(DTResult)"/>
    ///         <!-- Add milliseconds -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddMilliseconds" Start="$(Start)" Value="3000" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="3000 Millisecond from time: $(DTResult)"/>
    ///         <!-- Verify add milliseconds -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="GetElapsed" Start="$(Start)" End="$(DTResult)" Format="MilliSeconds">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Milliseconds Since $(Start): $(DTResult)"/>
    ///         <!-- Add minutes -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddMinutes" Start="$(Start)" Value="30" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="30 minutes from time: $(DTResult)"/>
    ///         <!-- Verify add minutes -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="GetElapsed" Start="$(Start)" End="$(DTResult)" Format="Minutes">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Minutes Since $(Start): $(DTResult)"/>
    ///         <!-- Add months -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddMonths" Start="$(Start)" Value="30" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="30 months from time: $(DTResult)"/>
    ///         <!-- Add seconds -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddSeconds" Start="$(Start)" Value="30" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="30 Seconds from time: $(DTResult)"/>
    ///         <!-- Verify add seconds -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="GetElapsed" Start="$(Start)" End="$(DTResult)" Format="Seconds">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Seconds Since $(Start): $(DTResult)"/>
    ///         <!-- Add ticks -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddTicks" Start="$(Start)" Value="3000" Format="dd MMM yy HH:mm:ss:fff">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="3000 ticks from time: $(DTResult)"/>
    ///         <!-- Add years -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddYears" Start="$(Start)" Value="30" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="30 Years from time: $(DTResult)"/>
    ///         <!-- Test Add time targets based current time. -->
    ///         <!-- Add days -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddDays" Value="30" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="30 days from time: $(DTResult)"/>
    ///         <!-- Add hours -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddHours" Value="30" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="30 hours from time: $(DTResult)"/>
    ///         <!-- Add milliseconds -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddMilliseconds" Value="3000" Format="dd MMM yy HH:mm:ss:fff">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="3000 Millisecond from time: $(DTResult)"/>
    ///         <!-- Add minutes -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddMinutes" Value="30" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="30 minutes from time: $(DTResult)"/>
    ///         <!-- Add months -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddMonths" Value="30" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="30 months from time: $(DTResult)"/>
    ///         <!-- Add seconds -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddSeconds" Value="30" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="30 Seconds from time: $(DTResult)"/>
    ///         <!-- Add ticks -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddTicks" Value="3000" Format="dd MMM yy HH:mm:ss:fff">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="3000 ticks from time: $(DTResult)"/>
    ///         <!-- Add years -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="AddYears" Value="30" Format="dd MMM yy HH:mm:ss">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="30 Years from time: $(DTResult)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class DateAndTime : BaseTask
    {
        private const string GetTaskAction = "Get";
        private const string GetElapsedTaskAction = "GetElapsed";
        private const string CheckLaterTaskAction = "CheckLater";
        private const string CheckBetweenTaskAction = "CheckBetween";
        private const string AddDaysTaskAction = "AddDays";
        private const string AddHoursTaskAction = "AddHours";
        private const string AddMillisecondsTaskAction = "AddMilliseconds";
        private const string AddMinutesTaskAction = "AddMinutes";
        private const string AddMonthsTaskAction = "AddMonths";
        private const string AddSecondsTaskAction = "AddSeconds";
        private const string AddTicksTaskAction = "AddTicks";
        private const string AddYearsTaskAction = "AddYears";

        /// <summary>
        /// The start time to use
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// The end time to use for GetElapsed. Default is DateTime.Now
        /// </summary>
        public DateTime End { get; set; }

        /// <summary>
        /// Format to apply to the Result. For GetTime, Format can be any valid DateTime format. For GetElapsed, Format can be Milliseconds, Seconds, Minutes, Hours, Days or Total. Total returns dd:hh:mm:ss
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Specifies the value to operate with
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// The output Result
        /// </summary>
        [Output]
        public string Result { get; set; }

        /// <summary>
        /// The output boolean result.
        /// </summary>
        [Output]
        public bool BoolResult { get; set; }

        /// <summary>
        /// Set to true to use UTC Date / Time for the TaskAction. Default is false.
        /// </summary>
        public bool UseUtc { get; set; }

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
                case AddDaysTaskAction:
                    this.AddDays();
                    break;
                case AddHoursTaskAction:
                    this.AddHours();
                    break;
                case AddMillisecondsTaskAction:
                    this.AddMilliseconds();
                    break;
                case AddMinutesTaskAction:
                    this.AddMinutes();
                    break;
                case AddMonthsTaskAction:
                    this.AddMonths();
                    break;
                case AddSecondsTaskAction:
                    this.AddSeconds();
                    break;
                case AddTicksTaskAction:
                    this.AddTicks();
                    break;
                case AddYearsTaskAction:
                    this.AddYears();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private static DateTime GetDefaultOrUserStartTime(DateTime startTime)
        {
            // Default to current time if caller did not specify a time.
            if (startTime == Convert.ToDateTime("01/01/0001 00:00:00", CultureInfo.CurrentCulture))
            {
                return DateTime.Now;
            }

            return startTime;
        }

        private void CheckLater()
        {
            if (this.Start == Convert.ToDateTime("01/01/0001 00:00:00", CultureInfo.CurrentCulture))
            {
                Log.LogError("Start must be specified");
                return;
            }

            if (this.UseUtc)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking if: {0} is later than: {1}", DateTime.UtcNow.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture), this.Start.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture)));
                this.BoolResult = DateTime.UtcNow > this.Start;
            }
            else
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking if: {0} is later than: {1}", DateTime.Now.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture), this.Start.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture)));
                this.BoolResult = DateTime.Now > this.Start;
            }
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

            if (this.UseUtc)
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking if: {0} is between: {1} and: {2}", DateTime.UtcNow.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture), this.Start.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture), this.End.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture)));
                this.BoolResult = DateTime.UtcNow > this.Start && DateTime.UtcNow < this.End;
            }
            else
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking if: {0} is between: {1} and: {2}", DateTime.Now.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture), this.Start.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture), this.End.ToString("dd MMM yyyy HH:mm:ss", CultureInfo.CurrentCulture)));
                this.BoolResult = DateTime.Now > this.Start && DateTime.Now < this.End;
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
                this.End = this.UseUtc ? DateTime.UtcNow : DateTime.Now;
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
            this.Result = this.UseUtc ? DateTime.UtcNow.ToString(this.Format, CultureInfo.CurrentCulture) : DateTime.Now.ToString(this.Format, CultureInfo.CurrentCulture);
        }

        private void AddDays()
        {
            this.LogTaskMessage("Add Days");
            this.Result = GetDefaultOrUserStartTime(this.Start).AddDays(this.Value).ToString(this.Format, CultureInfo.CurrentCulture);
        }

        private void AddHours()
        {
            this.LogTaskMessage("Add Hours");
            this.Result = GetDefaultOrUserStartTime(this.Start).AddHours(this.Value).ToString(this.Format, CultureInfo.CurrentCulture);
        }

        private void AddMilliseconds()
        {
            this.LogTaskMessage("Add Milliseconds");
            this.Result = GetDefaultOrUserStartTime(this.Start).AddMilliseconds(this.Value).ToString(this.Format, CultureInfo.CurrentCulture);
        }

        private void AddMinutes()
        {
            this.LogTaskMessage("Add Minutes");
            this.Result = GetDefaultOrUserStartTime(this.Start).AddMinutes(this.Value).ToString(this.Format, CultureInfo.CurrentCulture);
        }

        private void AddMonths()
        {
            this.LogTaskMessage("Add Months");
            this.Result = GetDefaultOrUserStartTime(this.Start).AddMonths((int)this.Value).ToString(this.Format, CultureInfo.CurrentCulture);
        }

        private void AddSeconds()
        {
            this.LogTaskMessage("Add Seconds");
            this.Result = GetDefaultOrUserStartTime(this.Start).AddSeconds(this.Value).ToString(this.Format, CultureInfo.CurrentCulture);
        }

        private void AddTicks()
        {
            this.LogTaskMessage("Add Ticks");
            this.Result = GetDefaultOrUserStartTime(this.Start).AddTicks((long)this.Value).ToString(this.Format, CultureInfo.CurrentCulture);
        }

        private void AddYears()
        {
            this.LogTaskMessage("Add Years");
            this.Result = GetDefaultOrUserStartTime(this.Start).AddYears((int)this.Value).ToString(this.Format, CultureInfo.CurrentCulture);
        }
    }
}