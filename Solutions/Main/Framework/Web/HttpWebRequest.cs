//-----------------------------------------------------------------------
// <copyright file="HttpWebRequest.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Web
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Thread = System.Threading.Thread;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>GetResponse</i> (<b>Required: </b> Url <b>Optional: </b>ContentType, Timeout, SkipSslCertificateValidation, Retries, RetryInterval, UseIntegratedAuthentication, UserName, UserPassword <b>Output:</b> Response, Status)</para>
    /// <para><i>Post</i> (<b>Required: </b> Url <b>Optional: </b>ContentType, Timeout, RequestContent, SkipSslCertificateValidation, Retries, RetryInterval, UseIntegratedAuthentication, UserName, UserPassword <b>Output:</b> Response, Status)</para>
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
    ///         <MSBuild.ExtensionPack.Web.HttpWebRequest TaskAction="GetResponse" Url="http://www.freetodev.com">
    ///             <Output TaskParameter="Response" ItemName="ResponseDetail"/>
    ///             <Output TaskParameter="Status" PropertyName="ResponseStatus"/>
    ///         </MSBuild.ExtensionPack.Web.HttpWebRequest>
    ///         <Message Text="Status: $(ResponseStatus)"/>
    ///         <Message Text="StatusDescription: %(ResponseDetail.StatusDescription)"/>
    ///         <Message Text="StatusCode: %(ResponseDetail.StatusCode)"/>
    ///         <Message Text="CharacterSet: %(ResponseDetail.CharacterSet)"/>
    ///         <Message Text="ProtocolVersion: %(ResponseDetail.ProtocolVersion)"/>
    ///         <Message Text="ResponseUri: %(ResponseDetail.ResponseUri)"/>
    ///         <Message Text="Server: %(ResponseDetail.Server)"/>
    ///         <Message Text="ResponseText: %(ResponseDetail.ResponseText)"/>        
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class HttpWebRequest : BaseTask
    {
        private const string GetResponseTaskAction = "GetResponse";
        private const string PostTaskAction = "Post";
        private int timeout = 100000;

        /// <summary>
        /// Sets the number of milliseconds to wait before the request times out. The default value is 100,000 milliseconds (100 seconds).
        /// </summary>
        public int Timeout
        {
            get { return this.timeout; }
            set { this.timeout = value; }
        }

        /// <summary>
        /// Sets the name of the AppPool. Required.
        /// </summary>
        [Required]
        public string Url { get; set; }

        /// <summary>
        /// Set to true to accept all SSL certificates.
        /// </summary>
        public bool SkipSslCertificateValidation { get; set; }

        [Output]
        public ITaskItem Response { get; set; }

        /// <summary>
        /// The number of times the request should be retried before failing.
        /// </summary>
        public int Retries { get; set; }

        /// <summary>
        /// The number of milliseconds between retry attempts.  Default is 0.
        /// </summary>
        public int RetryInterval { get; set; }
        
        /// <summary>
        /// The number of milliseconds between retry attempts.  Default is 0.
        /// </summary>
        public bool UseIntegratedAuthentication { get; set; }

        /// <summary>
        /// Contains the StatusDescription for successful requests. Contains the Status when encountering a WebException.
        /// </summary>
        [Output]
        public string Status { get; set; }

        /// <summary>
        /// The content type of the request. By default, it is "application/x-www-form-urlencoded" (used for classic HTTP POST) for Post and null for GetResponse.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The content of the request. For classic HTTP POST, format is several [key]=[value] separated by "&amp;". Could be SOAP for example if ContentType is SOAP.
        /// </summary>
        public string RequestContent { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case GetResponseTaskAction:
                    this.GetResponse(this.CreateRequest);
                    break;
                case PostTaskAction:
                    this.GetResponse(this.CreatePostRequest);
                    break;
                default:
                    this.Log.LogError(
                        string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private System.Net.HttpWebRequest CreatePostRequest()
        {
            if (string.IsNullOrEmpty(this.ContentType))
            {
                this.ContentType = "application/x-www-form-urlencoded";
            }

            // Read Content as byte array
            ASCIIEncoding asciiEncoding = new ASCIIEncoding();
            if (string.IsNullOrEmpty(this.RequestContent))
            {
                this.RequestContent = string.Empty;
            }

            byte[] data = asciiEncoding.GetBytes(this.RequestContent);

            // Create request and add content
            System.Net.HttpWebRequest request = this.CreateRequest();
            request.Method = "Post";
            request.ContentType = this.ContentType;
            request.ContentLength = data.Length;
            Stream contentStream = request.GetRequestStream();
            contentStream.Write(data, 0, data.Length);
            contentStream.Close();

            return request;
        }

        private System.Net.HttpWebRequest CreateRequest()
        {
            var request = WebRequest.Create(new Uri(this.Url)) as System.Net.HttpWebRequest;
            if (request == null)
            {
                return request;
            }

            if (this.UseIntegratedAuthentication)
            {
                request.Credentials = CredentialCache.DefaultCredentials;
            }
            else if (!string.IsNullOrEmpty(this.UserName))
            {
                request.Credentials = new System.Net.NetworkCredential(this.UserName, this.UserPassword);
            }

            request.Timeout = this.Timeout;
            if (!string.IsNullOrEmpty(this.ContentType))
            {
                request.ContentType = this.ContentType;
            }

            if (this.SkipSslCertificateValidation)
            {
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            }

            return request;
        }

        private void GetResponse(Func<System.Net.HttpWebRequest> createRequestMethod)
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Executing {0} HttpRequest against: {1}", this.TaskAction.Replace("TaskAction", string.Empty), this.Url));
            var tries = 0;
            while (tries <= this.Retries)
            {
                tries++;
                var request = createRequestMethod();
                if (request == null)
                {
                    this.Log.LogError("Failed to create request against: {0}.", this.Url);
                    return;
                }

                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        int code = (int)response.StatusCode;
                        StreamReader responseReader = new StreamReader(response.GetResponseStream());
                        this.Response = new TaskItem(this.Url);
                        this.Response.SetMetadata("ResponseText", responseReader.ReadToEnd());
                        this.Status = response.StatusDescription;
                        this.Response.SetMetadata("StatusDescription", response.StatusDescription);
                        this.Response.SetMetadata("StatusCode", code.ToString(CultureInfo.CurrentCulture));
                        this.Response.SetMetadata("CharacterSet", response.CharacterSet);
                        this.Response.SetMetadata("ProtocolVersion", response.ProtocolVersion.ToString());
                        this.Response.SetMetadata("ResponseUri", response.ResponseUri.ToString());
                        this.Response.SetMetadata("Server", response.Server);
                        return;
                    }
                }
                catch (WebException ex)
                {
                    var failureMessage = string.Format(CultureInfo.CurrentCulture, "{0}. Status: {1}", ex.Message, ex.Status);
                    var responseBody = new StringBuilder();
                    if (ex.Response != null)
                    {
                        using (var responseReader = new StreamReader(ex.Response.GetResponseStream()))
                        {
                            responseBody.Append(responseReader.ReadToEnd());
                        }
                    }

                    if (tries < this.Retries)
                    {
                        this.LogTaskWarning(string.Format(CultureInfo.CurrentCulture, "{0}.  Attempt {1} of {2}.  Waiting {3} milliseconds before trying again.", failureMessage, tries, this.Retries, this.RetryInterval));
                        if (responseBody.Length > 0)
                        {
                            this.LogTaskMessage(responseBody.ToString());
                        }

                        Thread.Sleep(this.RetryInterval);
                    }
                    else
                    {
                        this.Log.LogError(failureMessage);
                        if (responseBody.Length > 0)
                        {
                            this.Log.LogError(responseBody.ToString());
                        }
                    }
                }
            }
        }
    }
}