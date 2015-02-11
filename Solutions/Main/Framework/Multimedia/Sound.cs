//-----------------------------------------------------------------------
// <copyright file="Sound.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Multimedia
{
    using System.Globalization;
    using System.IO;
    using System.Media;
    using System.Threading;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Play</i> (<b>Required: </b> SoundFile or SystemSound <b>Optional:</b> Repeat, Interval)</para>
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
    ///         <!-- Play a bunch of sounds with various tones, repeats and durations-->
    ///         <MSBuild.ExtensionPack.Multimedia.Sound TaskAction="Play" SoundFile="C:\Windows\Media\notify.wav" Repeat="10"/>
    ///         <MSBuild.ExtensionPack.Framework.Thread TaskAction="Sleep" Timeout="500"/>
    ///         <MSBuild.ExtensionPack.Multimedia.Sound TaskAction="Play" SystemSound="Asterisk"/>
    ///         <MSBuild.ExtensionPack.Framework.Thread TaskAction="Sleep" Timeout="500"/>
    ///         <MSBuild.ExtensionPack.Multimedia.Sound TaskAction="Play" SystemSound="Beep"/>
    ///         <MSBuild.ExtensionPack.Framework.Thread TaskAction="Sleep" Timeout="500"/>
    ///         <MSBuild.ExtensionPack.Multimedia.Sound TaskAction="Play" SystemSound="Exclamation"/>
    ///         <MSBuild.ExtensionPack.Framework.Thread TaskAction="Sleep" Timeout="500"/>
    ///         <MSBuild.ExtensionPack.Multimedia.Sound TaskAction="Play" SystemSound="Hand"/>
    ///         <MSBuild.ExtensionPack.Framework.Thread TaskAction="Sleep" Timeout="500"/>
    ///         <MSBuild.ExtensionPack.Multimedia.Sound TaskAction="Play" SystemSound="Question"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class Sound : BaseTask
    {
        private int repeat = 1;
        private int interval = 10;

        /// <summary>
        /// Sets the interval between beebs. Default is 10ms. Value must be between 10 and 5000
        /// </summary>
        public int Interval
        {
            get { return this.interval; }
            set { this.interval = value; }
        }

        /// <summary>
        /// Sets the number of times to play the sound. Default is 1. Value must be between 1 and 20
        /// </summary>
        public int Repeat
        {
            get { return this.repeat; }
            set { this.repeat = value; }
        }

        /// <summary>
        /// Sets the sound file to play
        /// </summary>
        public string SoundFile { get; set; }

        /// <summary>
        /// Sets the SystemSound to play. Supports: Asterisk, Beep, Exclamation, Hand, Question. Does not support Repeat or Interval.
        /// </summary>
        public string SystemSound { get; set; }

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
                case "Play":
                    this.Play();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Play()
        {
            if (!string.IsNullOrEmpty(this.SoundFile) && !File.Exists(this.SoundFile))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid File passed: {0}", this.SoundFile));
                return;
            }

            if (this.Repeat < 1 || this.Repeat > 20)
            {
                this.LogTaskWarning(string.Format(CultureInfo.CurrentCulture, "Invalid Repeat: {0}. Value must be between 1 and 20. Using default of 1.", this.Repeat));
                this.Repeat = 1;
            }

            if (this.Interval < 10 || this.Interval > 5000)
            {
                this.LogTaskWarning(string.Format(CultureInfo.CurrentCulture, "Invalid Interval: {0}. Value must be between 10 and 5000. Using default of 10.", this.Interval));
                this.Interval = 10;
            }

            if (!string.IsNullOrEmpty(this.SoundFile))
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Playing Sound: {0}", this.SoundFile));
                using (SoundPlayer player = new SoundPlayer())
                {
                    player.LoadTimeout = 5000;
                    player.SoundLocation = this.SoundFile;
                    for (int i = 1; i <= this.Repeat; i++)
                    {
                        player.PlaySync();
                        Thread.Sleep(this.Interval);
                    }
                }

                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Playing Sound: {0}", this.SystemSound));
            switch (this.SystemSound)
            {
                case "Asterisk":
                    SystemSounds.Asterisk.Play();
                    break;
                case "Beep":
                    SystemSounds.Beep.Play();
                    break;
                case "Exclamation":
                    SystemSounds.Exclamation.Play();
                    break;
                case "Hand":
                    SystemSounds.Hand.Play();
                    break;
                case "Question":
                    SystemSounds.Question.Play();
                    break;
            }
        }
    }
}