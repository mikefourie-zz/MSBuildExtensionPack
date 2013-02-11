//-----------------------------------------------------------------------
// <copyright file="Twitter.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Communication
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Tweet</i> (<b>Required: </b>Message, ConsumerKey, AccessToken, ConsumerSecret, AccessTokenSecret<b>Optional:</b> TwitterUrl)</para>
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
    ///         <!-- Send a Twitter message-->
    ///         <MSBuild.ExtensionPack.Communication.Twitter TaskAction="Tweet"
    ///                                                      Message="yourMessage"
    ///                                                      ConsumerKey="yourConsumerKey"
    ///                                                      AccessToken="yourAccessToken"
    ///                                                      ConsumerSecret="yourConsumerSecret"
    ///                                                      AccessTokenSecret="yourAccessTokenSecret"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Twitter : BaseTask
    {
        private const string TweetTaskAction = "Tweet";
        private string twitterUrl = "http://api.twitter.com/1/statuses/update.json";

        /// <summary>
        /// Sets the Twitter URL to post to. Defaults to http://api.twitter.com/1/statuses/update.json
        /// </summary>
        public string TwitterUrl
        {
            get { return this.twitterUrl; }
            set { this.twitterUrl = value; }
        }

        /// <summary>
        /// Sets the message to send to Twitter
        /// </summary>
        [Required]
        public string Message { get; set; }

        /// <summary>
        /// Sets the ConsumerKey
        /// </summary>
        [Required]
        public string ConsumerKey { get; set; }

        /// <summary>
        /// Sets the AccessToken (oauth_token)
        /// </summary>
        [Required]
        public string AccessToken { get; set; }

        /// <summary>
        /// Sets the ConsumerSecret
        /// </summary>
        [Required]
        public string ConsumerSecret { get; set; }

        /// <summary>
        /// Sets the AccessTokenSecret
        /// </summary>
        [Required]
        public string AccessTokenSecret { get; set; }

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
            const string Oauth_version = "1.0";
            const string Oauth_signature_method = "HMAC-SHA1";
            if (this.Message.Length > 140)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Message too long: {0}. Maximum length is 140 characters.", this.Message.Length));
                return;
            }

            // TODO: figure out encoding to support sending apostrophes
            this.Message = this.Message.Replace("'", " ");
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Tweeting: {0}", this.Message));
            string postBody = "status=" + Uri.EscapeDataString(this.Message);
            string oauth_consumer_key = this.ConsumerKey;
            string oauth_nonce = Convert.ToBase64String(new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString()));
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            string oauth_timestamp = Convert.ToInt64(ts.TotalSeconds).ToString();
            SortedDictionary<string, string> sd = new SortedDictionary<string, string> { { "status", Uri.EscapeDataString(this.Message) }, { "oauth_version", Oauth_version }, { "oauth_consumer_key", oauth_consumer_key }, { "oauth_nonce", oauth_nonce }, { "oauth_signature_method", Oauth_signature_method }, { "oauth_timestamp", oauth_timestamp }, { "oauth_token", this.AccessToken } };

            string baseString = string.Empty;
            baseString += "POST" + "&";
            baseString += Uri.EscapeDataString(this.TwitterUrl) + "&";
            baseString = sd.Aggregate(baseString, (current, entry) => current + Uri.EscapeDataString(entry.Key + "=" + entry.Value + "&"));
            baseString = baseString.Substring(0, baseString.Length - 3);

            string signingKey = Uri.EscapeDataString(this.ConsumerSecret) + "&" + Uri.EscapeDataString(this.AccessTokenSecret);
            string signatureString;
            using (HMACSHA1 hasher = new HMACSHA1(new ASCIIEncoding().GetBytes(signingKey)))
            {
                signatureString = Convert.ToBase64String(hasher.ComputeHash(new ASCIIEncoding().GetBytes(baseString)));
            }

            ServicePointManager.Expect100Continue = false;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(this.TwitterUrl));
            
            StringBuilder authorizationHeaderParams = new StringBuilder();
            authorizationHeaderParams.Append("OAuth ");
            authorizationHeaderParams.Append("oauth_nonce=" + "\"" + Uri.EscapeDataString(oauth_nonce) + "\",");
            authorizationHeaderParams.Append("oauth_signature_method=" + "\"" + Uri.EscapeDataString(Oauth_signature_method) + "\",");
            authorizationHeaderParams.Append("oauth_timestamp=" + "\"" + Uri.EscapeDataString(oauth_timestamp) + "\",");
            authorizationHeaderParams.Append("oauth_consumer_key=" + "\"" + Uri.EscapeDataString(oauth_consumer_key) + "\",");
            authorizationHeaderParams.Append("oauth_token=" + "\"" + Uri.EscapeDataString(this.AccessToken) + "\",");
            authorizationHeaderParams.Append("oauth_signature=" + "\"" + Uri.EscapeDataString(signatureString) + "\",");
            authorizationHeaderParams.Append("oauth_version=" + "\"" + Uri.EscapeDataString(Oauth_version) + "\"");
            webRequest.Headers.Add("Authorization", authorizationHeaderParams.ToString());
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            using (Stream stream = webRequest.GetRequestStream())
            {
                byte[] bodyBytes = new ASCIIEncoding().GetBytes(postBody);
                stream.Write(bodyBytes, 0, bodyBytes.Length);
                stream.Flush();
            }

            webRequest.Timeout = 3 * 60 * 1000;
            try
            {
                HttpWebResponse rsp = webRequest.GetResponse() as HttpWebResponse;

                Stream responseStream = rsp.GetResponseStream();
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string content = reader.ReadToEnd();
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Response: {0}", content));
                }
            }
            catch (Exception e)
            {
                this.Log.LogError(e.ToString());
            }
        }
    }
}