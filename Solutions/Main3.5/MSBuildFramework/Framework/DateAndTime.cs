//-----------------------------------------------------------------------
// <copyright file="DateAndTime.cs">(c) FreeToDev. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
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
    /// <para><b>Remote Support:</b> NA</para>
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
    ///         <Start>17 Nov 1976</Start>
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
    ///         <!-- Get the time in the specified format -->
    ///         <MSBuild.ExtensionPack.Framework.DateAndTime TaskAction="Get" Format="dd MMM yy">
    ///             <Output TaskParameter="Result" PropertyName="DTResult"/>
    ///         </MSBuild.ExtensionPack.Framework.DateAndTime>
    ///         <Message Text="Date / Time: $(DTResult)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class DateAndTime : BaseTask
    {
         /// <summary>
         /// The start time to use for GetElapsed
         /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// The end time to use for GetElapsed. Default is DateTime.Now
        /// </summary>
        public DateTime End { get; set; }

        /// <summary>
        /// Format to apply to the Result. For GetTime, Format can be any valid DateTime format. For GetElapsed, Format can be Milliseconds, Seconds, Minutes, Hours or Days
        /// </summary>
        [Required]
        public string Format { get; set; }

        /// <summary>
        /// The output Result
        /// </summary>
        [Output]
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
                    this.Log.LogError(string.Format(CultureInfo.InvariantCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void GetElapsed()
        {
            if (this.Start == Convert.ToDateTime("01/01/0001 00:00:00"))
            {
                Log.LogError("Start must be specified");
                return;
            }

            if (this.End == Convert.ToDateTime("01/01/0001 00:00:00"))
            {
                this.End = DateTime.Now;
            }
            
            this.Log.LogMessage(string.Format(CultureInfo.InvariantCulture, "Getting Elapsed: {0}", this.Format));
            TimeSpan t = this.End - this.Start;

            switch (this.Format)
            {
                case "MilliSeconds":
                    this.Result = t.TotalMilliseconds.ToString();
                    break;
                case "Seconds":
                    this.Result = t.TotalSeconds.ToString();
                    break;
                case "Minutes":
                    this.Result = t.TotalMinutes.ToString();
                    break;
                case "Hours":
                    this.Result = t.TotalHours.ToString();
                    break;
                case "Days":
                    this.Result = t.TotalDays.ToString();
                    break;
            }
        }

        private void GetDate()
        {
            this.Log.LogMessage("Getting Date / Time");
            this.Result = DateTime.Now.ToString(this.Format);
        }
    }
}