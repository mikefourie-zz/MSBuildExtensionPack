//-----------------------------------------------------------------------
// <copyright file="Twitter.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Communication
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Web;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Tweet</i> (<b>Required: </b>Message, UserName, UserPassword <b>Optional:</b> TwitterUrl)</para>
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
    ///         <!-- Send a Twitter message-->
    ///         <MSBuild.ExtensionPack.Communication.Twitter TaskAction="Tweet" Message="Hello Sir, this is your build server letting you know that all is ok." UserName="yourtwitterusername" UserPassword="yourtwitterpassword"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("TODO")]
    public class Twitter : BaseTask
    {
        private const string TweetTaskAction = "Tweet";
        private string twitterUrl = "http://twitter.com/statuses/update.json";

        [DropdownValue(TweetTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the Twitter URL to post to. Defaults to http://twitter.com/statuses/update.json
        /// </summary>
        [TaskAction(TweetTaskAction, false)]
        public string TwitterUrl
        {
            get { return this.twitterUrl; }
            set { this.twitterUrl = value; }
        }

        /// <summary>
        /// Sets the message to send to Twitter
        /// </summary>
        [Required]
        [TaskAction(TweetTaskAction, true)]
        public string Message { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case TweetTaskAction:
                    this.Tweet();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Tweet()
        {
            if (this.Message.Length > 140)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Message too long: {0}. Maximum length is 140 characters.", this.Message.Length));
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Tweeting: {0}", this.Message));
            System.Net.ServicePointManager.Expect100Continue = false;
            Uri newUri = new Uri(this.TwitterUrl);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(newUri);
            
            string post;

            using (TextWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                writer.Write("status={0}", HttpUtility.UrlEncode(this.Message));
                post = writer.ToString();
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Post: {0}", post));
            }

            request.Timeout = 30000;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "MSBuild Extension Pack - Twitter Task";
            request.Credentials = new NetworkCredential(this.UserName, this.UserPassword);

            using (Stream requestStream = request.GetRequestStream())
            {
                using (StreamWriter writer = new StreamWriter(requestStream))
                {
                    writer.Write(post);
                }
            }

            WebResponse response = request.GetResponse();
            string content;
            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    content = reader.ReadToEnd();
                }
            }
            
            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Response: {0}", content));
        }
    }
}