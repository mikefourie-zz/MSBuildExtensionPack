//-----------------------------------------------------------------------
// <copyright file="HttpWebRequest.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Web
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>GetResponse</i> (<b>Required: </b> Url <b>Optional: </b>Timeout, SkipSslCertificateValidation <b>Output:</b> Response, Status)</para>
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
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.6.0/html/7e2d4a1e-f79a-1b80-359a-445ffdea2ac5.htm")]
    public class HttpWebRequest : BaseTask
    {
        private const string GetResponseTaskAction = "GetResponse";
        private int timeout = 100000;

        [DropdownValue(GetResponseTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the number of milliseconds to wait before the request times out. The default value is 100,000 milliseconds (100 seconds).
        /// </summary>
        [TaskAction(GetResponseTaskAction, false)]
        public int Timeout
        {
            get { return this.timeout; }
            set { this.timeout = value; }
        }

        /// <summary>
        /// Sets the name of the AppPool. Required.
        /// </summary>
        [Required]
        [TaskAction(GetResponseTaskAction, true)]
        public string Url { get; set; }

        /// <summary>
        /// Set to true to accept all SSL certificates.
        /// </summary>
        [TaskAction(GetResponseTaskAction, true)]
        public bool SkipSslCertificateValidation { get; set; }

        [Output]
        public ITaskItem Response { get; set; }

        /// <summary>
        /// Contains the StatusDescription for successful requests. Contains the Status when encountering a WebException.
        /// </summary>
        [Output]
        public string Status { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case GetResponseTaskAction:
                    this.GetResponse();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }
        
        private void GetResponse()
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Executing HttpRequest against: {0}", this.Url));
            System.Net.HttpWebRequest request = WebRequest.Create(new Uri(this.Url)) as System.Net.HttpWebRequest;
            if (request != null)
            {
                request.Timeout = this.Timeout;
                if (this.SkipSslCertificateValidation)
                {
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) => true);
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
                    }
                }
                catch (WebException ex)
                {
                    this.Log.LogError("{0}. Status: {1}", ex.Message, ex.Status);
                    this.Status = ex.Status.ToString();
                    if (ex.Response != null)
                    {
                        using (StreamReader responseReader = new StreamReader(ex.Response.GetResponseStream()))
                        {
                            this.Log.LogError(responseReader.ReadToEnd());
                        }
                    }
                }
            }
        }
    }
}