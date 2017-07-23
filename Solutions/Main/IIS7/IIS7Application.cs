//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="Iis7Application.cs">(c) 2017 Mike Fourie and Contributors (https://github.com/mikefourie/MSBuildExtensionPack) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Web
{
    using System.Globalization;
    using Microsoft.Build.Framework;
    using Microsoft.Web.Administration;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CheckExists</i> (<b>Required: </b> Website, Applications <b>Output:</b> Exists)</para>
    /// <para><i>Delete</i> (<b>Required: </b> Website, Applications)</para>
    /// <para><b>Remote Execution Support:</b> Yes</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <ItemGroup>
    ///         <Application Include="/photos"/>
    ///         <Application Include="/photos2"/>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Delete Applications -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Application TaskAction="Delete" Website="NewSite" Applications="@(Application)"/>
    ///         <!-- Check whether applications exist -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Application TaskAction="CheckExists" Website="NewSite" Applications="/photos">
    ///             <Output TaskParameter="Exists" PropertyName="AppExists"/>
    ///         </MSBuild.ExtensionPack.Web.Iis7Application>
    ///         <Message Text="photos2 Exists: $(AppExists)"/>
    ///         <MSBuild.ExtensionPack.Web.Iis7Application TaskAction="CheckExists" Website="NewSite" Applications="/photos3">
    ///             <Output TaskParameter="Exists" PropertyName="AppExists"/>
    ///         </MSBuild.ExtensionPack.Web.Iis7Application>
    ///         <Message Text="photos2 Exists: $(AppExists)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>  
    public class Iis7Application : BaseTask
    {
        private const string CheckExistsTaskAction = "CheckExists";
        private const string DeleteTaskAction = "Delete";
        private ServerManager iisServerManager;
        private Site website;

        /// <summary>
        /// Sets the name of the Website
        /// </summary>
        [Required]
        public string Website { get; set; }

        /// <summary>
        /// ITaskItem of Applications
        /// </summary>
        public ITaskItem[] Applications { get; set; }

        /// <summary>
        /// Gets whether the application exists
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        protected override void InternalExecute()
        {
            try
            {
                this.iisServerManager = System.Environment.MachineName != this.MachineName ? ServerManager.OpenRemote(this.MachineName) : new ServerManager();
                if (!this.SiteExists())
                {
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Website, this.MachineName));
                    return;
                }

                switch (this.TaskAction)
                {
                    case DeleteTaskAction:
                        this.Delete();
                        break;
                    case CheckExistsTaskAction:
                        this.CheckExists();
                        break;
                    default:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                        return;
                }
            }
            finally
            {
                if (this.iisServerManager != null)
                {
                    this.iisServerManager.Dispose();
                }
            }
        }

        private void CheckExists()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking whether application: {0} exists in {1} on: {2}", this.Applications[0].ItemSpec, this.Website, this.MachineName));
            this.Exists = this.ApplicationExists(this.Applications[0].ItemSpec);
        }

        private bool ApplicationExists(string name)
        {
            return this.website.Applications[name] != null;
        }

        private void Delete()
        {
            if (this.Applications != null)
            {
                foreach (ITaskItem app in this.Applications)
                {
                    if (this.website.Applications[app.ItemSpec] != null)
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting Application: {0}", app.ItemSpec));
                        this.website.Applications.Remove(this.website.Applications[app.ItemSpec]);
                    }
                }

                this.iisServerManager.CommitChanges();
            }
        }

        private bool SiteExists()
        {
            this.website = this.iisServerManager.Sites[this.Website];
            return this.website != null;
        }
    }
}