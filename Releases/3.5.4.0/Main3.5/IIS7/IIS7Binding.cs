//-----------------------------------------------------------------------
// <copyright file="Iis7Binding.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Web
{
    using System;
    using System.Globalization;
    using Microsoft.Build.Framework;
    using Microsoft.Web.Administration;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Add</i> (<b>Required: </b> Name, BindingInformation or (CertificateHash and CertificateStoreName) <b>Optional: </b>BindingProtocol)</para>
    /// <para><i>CheckExists</i> (<b>Required: </b> Name, BindingInformation <b>Optional: </b>BindingProtocol <b>Output:</b> Exists, BindingProtocol</para>
    /// <para><i>Modify</i> (<b>Required: </b> Name, BindingInformation, PreviousBindingProtocol, PreviousBindingInformation)</para>
    /// <para><i>Remove</i> (<b>Required: </b> Name <b>Optional: </b>BindingProtocol)</para>
    /// <para><b>Remote Execution Support:</b> Yes</para>
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
    ///         <!-- Add a binding -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Binding TaskAction="Add" Name="NewSite" BindingInformation="123.123.11.33:234:www.freet2odev.com" BindingProtocol="http"/>
    ///         <!-- Check whether a binding exists-->
    ///         <MSBuild.ExtensionPack.Web.Iis7Binding TaskAction="CheckExists" Name="NewSite" BindingInformation="123.123.11.33:234:www.freet2odev.com" BindingProtocol="http">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Web.Iis7Binding>
    ///         <Message Text="Exists: $(DoesExist)"/>
    ///         <!-- Add another binding -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Binding TaskAction="Add" Name="NewSite" BindingInformation="123.123.33.33:455:www.freet2odev.com" BindingProtocol="http"/>
    ///         <!-- Modify the binding -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Binding TaskAction="Modify" Name="NewSite" PreviousBindingProtocol="http" PreviousBindingInformation="123.123.11.33:234:www.freet2odev.com" BindingInformation="5.5.55.5:111:www.newmod.com" BindingProtocol="http"/>
    ///         <!-- Remove the binding -->
    ///         <MSBuild.ExtensionPack.Web.Iis7Binding TaskAction="Remove" Name="NewSite" BindingInformation="123.123.33.33:455:www.freet2odev.com" BindingProtocol="http"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example> 
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.3.0/html/7a6bc9b8-0852-8ade-d496-d3fbe3d3f94b.htm")]
    public class Iis7Binding : BaseTask
    {
        private const string AddTaskAction = "Add";
        private const string CheckExistsTaskAction = "CheckExists";
        private const string ModifyTaskAction = "Modify";
        private const string RemoveTaskAction = "Remove";
        
        private ServerManager iisServerManager;
        private Site website;
        private string bindingProtocol = "http";

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(AddTaskAction)]
        [DropdownValue(CheckExistsTaskAction)]
        [DropdownValue(ModifyTaskAction)]
        [DropdownValue(RemoveTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the name of the Website
        /// </summary>
        [Required]
        [TaskAction(AddTaskAction, true)]
        [TaskAction(CheckExistsTaskAction, true)]
        [TaskAction(ModifyTaskAction, true)]
        [TaskAction(RemoveTaskAction, true)]
        public string Name { get; set; }

        /// <summary>
        /// Sets the port of the Binding to Modify
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// String containing binding information.
        /// <para/>
        /// Format: ip address:port:hostheader
        /// <para/>
        /// Example: *:80:sample.example.com or : *:443:
        /// </summary>
        [TaskAction(AddTaskAction, false)]
        [TaskAction(CheckExistsTaskAction, true)]
        [TaskAction(ModifyTaskAction, true)]
        [TaskAction(RemoveTaskAction, true)]
        public string BindingInformation { get; set; }

        /// <summary>
        /// Sets the PreviousBindingInformation to use when calling Modify
        /// </summary>
        [TaskAction(ModifyTaskAction, true)]
        public string PreviousBindingInformation { get; set; }

        /// <summary>
        /// Sets the PreviousBindingProtocol to use when calling Modify
        /// </summary>
        [TaskAction(ModifyTaskAction, true)]
        public string PreviousBindingProtocol { get; set; }

        /// <summary>
        /// If HTTPS is used, this is the certificate hash. This is the value of "thumbprint" value of the certificate you want to use.
        /// <para/>
        /// Format: hash encoded string. Hex symbols can be space or dash separated.
        /// <para/>
        /// Example: 0a 0a 0a 0a 0a 0a 0a 0a 0a 0a 0a 0a 0a 0a 0a 0a 0a 0a 0a 0a
        /// </summary>
        [TaskAction(AddTaskAction, false)]
        public string CertificateHash { get; set; }

        /// <summary>
        /// The name of the certificate store. Default is "MY" for the personal store
        /// </summary>
        [TaskAction(AddTaskAction, false)]
        public string CertificateStoreName { get; set; }

        /// <summary>
        /// Gets whether the binding exists
        /// </summary>
        [Output]
        [TaskAction(CheckExistsTaskAction, false)]
        public bool Exists { get; set; }

        /// <summary>
        /// Binding protocol. Example: "http", "https", "ftp". Default is http.
        /// </summary>
        [TaskAction(AddTaskAction, false)]
        [TaskAction(CheckExistsTaskAction, false)]
        [TaskAction(RemoveTaskAction, false)]
        public string BindingProtocol
        {
            get { return this.bindingProtocol; }
            set { this.bindingProtocol = value; }
        }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        protected override void InternalExecute()
        {
            try
            {
                this.iisServerManager = System.Environment.MachineName != this.MachineName ? ServerManager.OpenRemote(this.MachineName) : new ServerManager();

                switch (this.TaskAction)
                {
                    case "Add":
                        this.Add();
                        break;
                    case "CheckExists":
                        this.CheckExists();
                        break;
                    case "Modify":
                        this.Modify();
                        break;
                    case "Remove":
                        this.Remove();
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

        /// <summary>
        /// Parse certificate hash from a string.
        /// </summary>
        /// <remarks>Based on code from: http://www.codeproject.com/KB/recipes/hexencoding.aspx</remarks>
        /// <param name="hexValue">hex values, can be space, dash or not-delimited</param>
        /// <returns>byte[] encoded value</returns>
        private static byte[] HexToData(string hexValue)
        {
            if (hexValue == null)
            {
                return null;
            }

            hexValue = hexValue.Replace(" ", string.Empty);
            hexValue = hexValue.Replace("-", string.Empty);
            if (hexValue.Length % 2 == 1)
            {
                // Up to you whether to pad the first or last byte
                hexValue = '0' + hexValue;
            }

            byte[] data = new byte[hexValue.Length / 2];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Convert.ToByte(hexValue.Substring(i * 2, 2), 16);
            }

            return data;
        }

        private void CheckExists()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            if (string.IsNullOrEmpty(this.BindingInformation))
            {
                Log.LogError("BindingInformation is required.");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Looking for Binding: [{0}] {1} for: {2} on: {3}", this.BindingProtocol, this.BindingInformation, this.Name, this.MachineName));
            foreach (Binding binding in this.website.Bindings)
            {
                if (binding.Protocol.Equals(this.BindingProtocol, StringComparison.OrdinalIgnoreCase) && (binding.BindingInformation == this.BindingInformation))
                {
                    this.Exists = true;
                    return;
                }
            }
        }

        private void Remove()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting BindingInformation: [{0}] {1} from {2} on: {3}", this.BindingProtocol, this.BindingInformation, this.Name, this.MachineName));
            foreach (Binding binding in this.website.Bindings)
            {
                if (binding.Protocol.Equals(this.BindingProtocol, StringComparison.OrdinalIgnoreCase) && binding.BindingInformation == this.BindingInformation)
                {
                    this.website.Bindings.Remove(binding);
                    break;
                }
            }

            this.iisServerManager.CommitChanges();
        }

        private void Add()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} was not found on: {1}", this.Name, this.MachineName));
                return;
            }

            if (string.IsNullOrEmpty(this.BindingInformation))
            {
                Log.LogError("BindingInformation is required.");
                return;
            }

            if (!string.IsNullOrEmpty(this.CertificateHash))
            {
                if (string.IsNullOrEmpty(this.CertificateStoreName))
                {
                    this.CertificateStoreName = "MY";
                }

                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating binding with certificate: thumb print '{0}' in store '{1}'", this.CertificateHash, this.CertificateStoreName));
                this.website.Bindings.Add(this.BindingInformation, HexToData(this.CertificateHash), this.CertificateStoreName);
            }
            else
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding BindingInformation: [{0}] {1} to: {2} on: {3}", this.BindingProtocol, this.BindingInformation, this.Name, this.MachineName));
                foreach (Binding binding in this.website.Bindings)
                {
                    if (binding.Protocol.Equals(this.BindingProtocol, StringComparison.OrdinalIgnoreCase) && binding.BindingInformation == this.BindingInformation)
                    {
                        Log.LogError("A binding with the same ip, port and host header already exists.");
                        return;
                    }
                }

                this.website.Bindings.Add(this.BindingInformation, this.BindingProtocol);
            }

            this.iisServerManager.CommitChanges();
        }

        private void Modify()
        {
            if (!this.SiteExists())
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "The website: {0} does not exists on: {1}", this.Name, this.MachineName));
                return;
            }

            if (string.IsNullOrEmpty(this.BindingInformation))
            {
                Log.LogError("BindingInformation is required.");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Modifying BindingInformation, setting: {0} for: {1} on: {2}", this.BindingInformation, this.Name, this.MachineName));
            foreach (Binding binding in this.website.Bindings)
            {
                if (binding.Protocol.Equals(this.PreviousBindingProtocol, StringComparison.OrdinalIgnoreCase) && binding.BindingInformation == this.PreviousBindingInformation)
                {
                    binding.BindingInformation = this.BindingInformation;
                    binding.Protocol = this.BindingProtocol;
                    break;
                }
            }

            this.iisServerManager.CommitChanges();
        }
    
        private bool SiteExists()
        {
            this.website = this.iisServerManager.Sites[this.Name];
            return this.website != null;
        }
    }
}
