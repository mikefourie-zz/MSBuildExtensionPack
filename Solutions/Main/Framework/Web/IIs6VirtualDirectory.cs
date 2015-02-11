//-----------------------------------------------------------------------
// <copyright file="Iis6VirtualDirectory.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Web
{
    using System;
    using System.DirectoryServices;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CheckExists</i> (<b>Required: </b> Website <b>Optional:</b> Name</para>
    /// <para><i>Create</i> (<b>Required: </b> Website <b>Optional:</b> Name, Parent, RequireApplication, DirectoryType, AppPool, Properties)</para>
    /// <para><i>Delete</i> (<b>Required: </b> Website <b>Optional:</b> Name, Parent</para>
    /// <para><b>Remote Execution Support:</b> Yes. Please note that the machine you execute from must have IIS installed.</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///   <PropertyGroup>
    ///     <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///     <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///   </PropertyGroup>
    ///   <Import Project="$(TPath)"/>
    ///   <Target Name="Default">
    ///     <!-- Create an IIsWebVirtualDir at the ROOT of the website -->
    ///     <MSBuild.ExtensionPack.Web.Iis6VirtualDirectory TaskAction="Create" Website="awebsite" Properties="AccessRead=True;AccessWrite=False;AccessExecute=False;AccessScript=True;AccessSource=False;AspScriptErrorSentToBrowser=False;AspScriptErrorMessage=An error occurred on the server.;AspEnableApplicationRestart=False;DefaultDoc=SubmissionProtocol.aspx;DontLog=False;EnableDefaultDoc=True;HttpExpires=D, 0;HttpErrors=;Path=c:\Demo1;ScriptMaps=.htm,C:\Windows\System32\Inetsrv\Test.dll,5,GET, HEAD, POST"/>
    ///     <!-- Check if a virtual directory exists-->
    ///     <MSBuild.ExtensionPack.Web.Iis6VirtualDirectory TaskAction="CheckExists" Website="awebsite" Name="AVDir" >
    ///       <Output TaskParameter="Exists" PropertyName="CheckExists" />
    ///     </MSBuild.ExtensionPack.Web.Iis6VirtualDirectory>
    ///     <!-- Create another IIsWebVirtualDir -->
    ///     <MSBuild.ExtensionPack.Web.Iis6VirtualDirectory TaskAction="Create" Website="awebsite" Name="AVDir" Properties="Path=c:\Demo2"/>
    ///     <!-- Delete the IIsWebVirtualDir-->
    ///     <MSBuild.ExtensionPack.Web.Iis6VirtualDirectory TaskAction="Delete" Website="awebsite" Name="AVDir"/>
    ///   </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Iis6VirtualDirectory : BaseTask, IDisposable
    {
        private const string CheckExistsTaskAction = "CheckExists";
        private const string CreateTaskAction = "Create";
        private const string DeleteTaskAction = "Delete";

        private DirectoryEntry websiteEntry;
        private string properties;
        private string directoryType = "IIsWebVirtualDir";
        private bool requireApplication = true;
        private string appPool = "DefaultAppPool";
        private string name = "ROOT";
        private string parent = "/ROOT";

        /// <summary>
        /// Gets whether the Virtual Directory exists
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Sets the Parent. Defaults to /ROOT
        /// </summary>
        public string Parent
        {
            get { return this.parent; }
            set { this.parent = value; }
        }

        /// <summary>
        /// Sets whether an Application is required. Defaults to true.
        /// </summary>
        public bool RequireApplication
        {
            get { return this.requireApplication; }
            set { this.requireApplication = value; }
        }

        /// <summary>
        /// Sets the DirectoryType. Supports IIsWebDirectory and IIsWebVirtualDir. Default is IIsWebVirtualDir.
        /// </summary>
        public string DirectoryType
        {
            get { return this.directoryType; }
            set { this.directoryType = value; }
        }

        /// <summary>
        /// Sets the AppPool to run in. Default is 'DefaultAppPool'
        /// </summary>
        public string AppPool
        {
            get { return this.appPool; }
            set { this.appPool = value; }
        }

        /// <summary>
        /// Sets the Properties. Use a semi-colon delimiter. See <a href="http://www.microsoft.com/technet/prodtechnol/WindowsServer2003/Library/IIS/cde669f1-5714-4159-af95-f334251c8cbd.mspx?mfr=true">Metabase Property Reference (IIS 6.0)</a>
        /// If a property contains =, enter #~# as a special sequence which will be replaced with = during processing
        /// </summary>
        public string Properties
        {
            get { return System.Web.HttpUtility.HtmlDecode(this.properties); }
            set { this.properties = value; }
        }

        /// <summary>
        /// Sets the name of the Virtual Directory. Defaults to 'ROOT'
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        /// <summary>
        /// Sets the name of the Website to add the Virtual Directory to.
        /// </summary>
        [Required]
        public string Website { get; set; }

        internal string IisPath
        {
            get { return "IIS://" + this.MachineName + "/W3SVC"; }
        }

        internal string AppPoolsPath
        {
            get { return "IIS://" + this.MachineName + "/W3SVC/AppPools"; }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.websiteEntry.Dispose();
            }
        }

        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case CheckExistsTaskAction:
                    this.CheckExists();
                    break;
                case CreateTaskAction:
                    this.Create();
                    break;
                case DeleteTaskAction:
                    this.Delete();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private static void UpdateMetaBaseProperty(DirectoryEntry entry, string metaBasePropertyName, string metaBaseProperty)
        {
            if (metaBaseProperty.IndexOf('|') == -1)
            {
                string propertyTypeName;
                using (DirectoryEntry di = new DirectoryEntry(entry.SchemaEntry.Parent.Path + "/" + metaBasePropertyName))
                {
                    propertyTypeName = (string)di.Properties["Syntax"].Value;
                }

                if (string.Compare(propertyTypeName, "binary", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    object[] metaBasePropertyBinaryFormat = new object[metaBaseProperty.Length / 2];
                    for (int i = 0; i < metaBasePropertyBinaryFormat.Length; i++)
                    {
                        metaBasePropertyBinaryFormat[i] = metaBaseProperty.Substring(i * 2, 2);
                    }

                    PropertyValueCollection propValues = entry.Properties[metaBasePropertyName];
                    propValues.Clear();
                    propValues.Add(metaBasePropertyBinaryFormat);
                    entry.CommitChanges();
                }
                else
                {
                    if (string.Compare(metaBasePropertyName, "path", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        DirectoryInfo f = new DirectoryInfo(metaBaseProperty);
                        metaBaseProperty = f.FullName;
                    }

                    entry.Invoke("Put", metaBasePropertyName, metaBaseProperty);
                    entry.Invoke("SetInfo");
                }
            }
            else
            {
                entry.Invoke("Put", metaBasePropertyName, string.Empty);
                entry.Invoke("SetInfo");
                string[] metabaseProperties = metaBaseProperty.Split('|');
                foreach (string metabasePropertySplit in metabaseProperties)
                {
                    entry.Properties[metaBasePropertyName].Add(metabasePropertySplit);
                }

                entry.CommitChanges();
            }
        }

        private DirectoryEntry LoadWebService()
        {
            DirectoryEntry webService = new DirectoryEntry(this.IisPath);
            if (webService == null)
            {
                throw new ApplicationException(string.Format(CultureInfo.CurrentUICulture, "Iis DirectoryServices Unavailable: {0}", this.IisPath));
            }

            return webService;
        }

        private DirectoryEntry LoadWebsite(string websiteName)
        {
            DirectoryEntry webService = null;

            try
            {
                webService = this.LoadWebService();
                DirectoryEntries webEntries = webService.Children;

                foreach (DirectoryEntry webEntry in webEntries)
                {
                    if (webEntry.SchemaClassName == "IIsWebServer")
                    {
                        if (string.Compare(websiteName, webEntry.Properties["ServerComment"][0].ToString(), StringComparison.CurrentCultureIgnoreCase) == 0)
                        {
                            return webEntry;
                        }
                    }

                    webEntry.Dispose();
                }

                return null;
            }
            finally
            {
                if (webService != null)
                {
                    webService.Dispose();
                }
            }
        }

        private DirectoryEntry LoadVirtualRoot(string websiteName)
        {
            DirectoryEntry webService = null;
            try
            {
                webService = this.LoadWebService();
                DirectoryEntries webEntries = webService.Children;

                foreach (DirectoryEntry webEntry in webEntries)
                {
                    if (webEntry.SchemaClassName == "IIsWebServer")
                    {
                        if (string.Compare(webEntry.Properties["ServerComment"][0].ToString(), websiteName, StringComparison.CurrentCultureIgnoreCase) == 0)
                        {
                            int websiteIdentifier = int.Parse(webEntry.Name, CultureInfo.InvariantCulture);
                            string rootVdirPath = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/ROOT", this.IisPath, websiteIdentifier);
                            return new DirectoryEntry(rootVdirPath);
                        }
                    }

                    webEntry.Dispose();
                }

                return null;
            }
            finally
            {
                if (webService != null)
                {
                    webService.Dispose();
                }
            }
        }

        private void CheckExists()
        {
            this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.CurrentUICulture, "Checking if Virtual Directory: {0} exists under {1}", this.Name, this.Website));

            // Locate the website.
            this.websiteEntry = this.LoadWebsite(this.Website);
            if (this.websiteEntry == null)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentUICulture, "Website not found: {0}", this.Website));
                return;
            }

            if (this.Name != "ROOT")
            {
                string parentPath = string.Format(CultureInfo.InvariantCulture, "{0}{1}", this.websiteEntry.Path, this.Parent);
                this.websiteEntry = new DirectoryEntry(parentPath);
                if (this.websiteEntry.Children.Cast<DirectoryEntry>().Any(vd => vd.Name == this.Name))
                {
                    this.Exists = true;
                }
            }
        }

        private void Delete()
        {
            this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.CurrentUICulture, "Deleting Virtual Directory: {0} under {1}", this.Name, this.Website));

            // Locate the website.
            this.websiteEntry = this.LoadWebsite(this.Website);
            if (this.websiteEntry == null)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentUICulture, "Website not found: {0}", this.Website));
                return;
            }

            if (this.Name != "ROOT")
            {
                string parentPath = string.Format(CultureInfo.InvariantCulture, "{0}{1}", this.websiteEntry.Path, this.Parent);
                this.websiteEntry = new DirectoryEntry(parentPath);
            }

            this.websiteEntry.Invoke("Delete", new[] { "IIsWebVirtualDir", this.Name });
        }

        private void Create()
        {
            DirectoryEntry vdirEntry = null;

            try
            {
                this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.CurrentUICulture, "Creating Virtual Directory: {0} under {1}", this.Name, this.Website));

                // Locate the website.
                this.websiteEntry = this.LoadWebsite(this.Website);
                if (this.websiteEntry == null)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentUICulture, "Website not found: {0}", this.Website));
                    return;
                }

                if (this.Name == "ROOT")
                {
                    vdirEntry = this.LoadVirtualRoot(this.Website);
                }
                else
                {
                    // Otherwise we create it.
                    string parentPath = string.Format(CultureInfo.InvariantCulture, "{0}{1}", this.websiteEntry.Path, this.Parent);
                    this.websiteEntry = new DirectoryEntry(parentPath);
                    try
                    {
                        vdirEntry = (DirectoryEntry)this.websiteEntry.Invoke("Create", this.DirectoryType, this.Name);
                    }
                    catch (TargetInvocationException ex)
                    {
                        Exception e = ex.InnerException;
                        COMException ce = (COMException)e;
                        if (ce != null)
                        {
                            // HRESULT 0x800700B7, "Cannot create a file when that file already exists. "
                            if (ce.ErrorCode == -2147024713)
                            {
                                // The child already exists, so let's get it.
                                string childPath = string.Format(CultureInfo.InvariantCulture, "{0}/{1}", parentPath, this.Name);
                                vdirEntry = new DirectoryEntry(childPath);
                            }
                            else
                            {
                                throw;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }

                    this.websiteEntry.CommitChanges();
                    vdirEntry.CommitChanges();
                    UpdateMetaBaseProperty(vdirEntry, "AppFriendlyName", this.Name);
                }

                // Now loop through all the metabase properties specified.
                if (string.IsNullOrEmpty(this.Properties) == false)
                {
                    string[] propList = this.Properties.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in propList)
                    {
                        string[] propPair = s.Split(new[] { '=' });
                        string propName = propPair[0];
                        string propValue = propPair.Length > 1 ? propPair[1] : string.Empty;

                        // handle the special character sequence to insert '=' if property requires it
                        propValue = propValue.Replace("#~#", "=");
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "\tAdding Property: {0}({1})", propName, propValue));
                        UpdateMetaBaseProperty(vdirEntry, propName, propValue);
                    }
                }

                vdirEntry.CommitChanges();

                if (this.RequireApplication)
                {
                    if (string.IsNullOrEmpty(this.AppPool))
                    {
                        vdirEntry.Invoke("AppCreate2", 1);
                    }
                    else
                    {
                        if (!DirectoryEntry.Exists(this.AppPoolsPath + @"/" + this.AppPool))
                        {
                            this.Log.LogError(string.Format(CultureInfo.CurrentUICulture, "AppPool not found: {0}", this.AppPool));
                            return;
                        }

                        vdirEntry.Invoke("AppCreate3", 1, this.AppPool, false);
                    }
                }
                else
                {
                    vdirEntry.Invoke("AppDelete");
                }

                vdirEntry.CommitChanges();
            }
            finally
            {
                if (this.websiteEntry != null)
                {
                    this.websiteEntry.Dispose();
                }

                if (vdirEntry != null)
                {
                    vdirEntry.Dispose();
                }
            }
        }
    }
}
