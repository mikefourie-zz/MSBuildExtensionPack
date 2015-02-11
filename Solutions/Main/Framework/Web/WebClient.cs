//-----------------------------------------------------------------------
// <copyright file="WebClient.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Web
{
    using System.Globalization;
    using System.IO;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>DownloadFile</i> (<b>Required: </b> Url, FileName <b>Optional:</b>Proxy, BypassOnLocal<b>Output:</b> Response)</para>
    /// <para><i>OpenRead</i> (<b>Required: </b> Url <b>Optional:</b> DisplayToConsole, Proxy, BypassOnLocal<b>Output:</b> Data)</para>
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
    ///         <!-- Download a File-->
    ///         <MSBuild.ExtensionPack.Web.WebClient TaskAction="DownloadFile" Url="http://hlstiw.bay.livefilestore.com/y1p7GhsJWeF4ig_Yb-8QXeA1bL0nY_MdOGaRQ3opRZS0YVvfshMfoZYe_cb1wSzPhx4nL_yidkG8Ji9msjRcTt0ew/Team%20Build%202008%20DeskSheet%202.0.pdf?download" FileName="C:\TFS Build 2008 DeskSheet.pdf"/>
    ///         <!-- Download a File using a proxy to connect to the remote server -->
    ///         <MSBuild.ExtensionPack.Web.WebClient TaskAction="DownloadFile" Url="http://download.sysinternals.com/Files/SysinternalsSuite.zip" FileName="MySysinternalsCopy.zip" Proxy="myproxy.fabrikam.com:8080"/>
    ///         <!-- Get the contents of a Url-->
    ///         <MSBuild.ExtensionPack.Web.WebClient TaskAction="OpenRead" Url="http://www.msbuildextensionpack.com">
    ///             <Output TaskParameter="Data" PropertyName="Out"/>
    ///         </MSBuild.ExtensionPack.Web.WebClient>
    ///         <Message Text="$(Out)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class WebClient : BaseTask
    {
        private const string DownloadFileTaskAction = "DownloadFile";
        private const string OpenReadTaskAction = "OpenRead";

        /// <summary>
        /// Sets the name of the Url. Required.
        /// </summary>
        [Required]
        public string Url { get; set; }

        /// <summary>
        /// Sets the URI of a proxy
        /// </summary>
        public string Proxy { get; set; }
        
        /// <summary>
        /// Sets the name of the file
        /// </summary>
        public ITaskItem FileName { get; set; }

        /// <summary>
        /// Sets whether to show Data to the console. Default is false.
        /// </summary>
        public bool DisplayToConsole { get; set; }

        /// <summary>
        /// Sets whether to bypass the proxy for local addresses. Default is false.
        /// </summary>
        public bool BypassOnLocal { get; set; }

        /// <summary>
        /// Gets the Data downloaded.
        /// </summary>
        [Output]
        public string Data { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case OpenReadTaskAction:
                    this.OpenRead();
                    break;
                case DownloadFileTaskAction:
                    this.DownloadFile();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void DownloadFile()
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Downloading: {0} to {1}", this.Url, this.FileName));
           
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                if (!string.IsNullOrEmpty(this.Proxy))
                {
                    client.Proxy = new System.Net.WebProxy(this.Proxy, this.BypassOnLocal);
                }

                client.DownloadFile(this.Url, this.FileName.ItemSpec);              
            }
        }

        private void OpenRead()
        {
            this.LogTaskMessage(string.Format(CultureInfo.InvariantCulture, "Reading: {0}", this.Url));
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                if (!string.IsNullOrEmpty(this.Proxy))
                {
                    client.Proxy = new System.Net.WebProxy(this.Proxy, this.BypassOnLocal);
                }

                Stream myStream = client.OpenRead(this.Url);
                using (StreamReader sr = new StreamReader(myStream))
                {
                    this.Data = sr.ReadToEnd();
                    if (this.DisplayToConsole)
                    {
                        this.LogTaskMessage(MessageImportance.Normal, this.Data);
                    }
                }
            }
        }
    }
}