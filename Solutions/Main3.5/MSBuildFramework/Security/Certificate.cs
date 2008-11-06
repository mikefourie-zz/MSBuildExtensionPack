//-----------------------------------------------------------------------
// <copyright file="Certificate.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Security
{
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Add</i> (<b>Required: </b>FileName <b>Optional: </b>MachineStore, CertPassword, Exportable, StoreName  <b>Output: </b>Thumbprint)</para>
    /// <para><i>Remove</i> (<b>Required: </b>Thumbprint <b>Optional: </b>MachineStore, StoreName)</para>
    /// <para><b>Remote Execution Support:</b> No</para>
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
    ///         <!-- Add a certificate -->
    ///         <MSBuild.ExtensionPack.Security.Certificate TaskAction="Add" FileName="C:\MyCertificate.cer" CertPassword="PASSW">
    ///             <Output TaskParameter="Thumbprint" PropertyName="TPrint"/>
    ///         </MSBuild.ExtensionPack.Security.Certificate>
    ///         <Message Text="Thumbprint: $(TPrint)"/>
    ///         <!-- Remove a certificate -->
    ///         <MSBuild.ExtensionPack.Security.Certificate TaskAction="Remove" Thumbprint="$(TPrint)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>    
    public class Certificate : BaseTask
    {
        private string storeName = "MY";

        /// <summary>
        /// Sets a value indicating whether to use the MachineStore. Default is false
        /// </summary>
        public bool MachineStore { get; set; }

        /// <summary>
        /// Sets the password for the pfx file from which the certificate is to be imported, defaults to blank
        /// </summary>
        public string CertPassword { get; set; }

        /// <summary>
        /// Sets a value indicating whether the certificate is exportable.
        /// </summary>
        public bool Exportable { get; set; }

        /// <summary>
        /// Gets the thumbprint. Used to uniquely identify certificate in further tasks
        /// </summary>
        [Output]
        public string Thumbprint { get; set; }

        /// <summary>
        /// Sets the name of the store. Defaults to MY
        /// <para/>
        /// AddressBook:          The store for other users<br />
        /// AuthRoot:             The store for third-party certificate authorities<br />
        /// CertificateAuthority: The store for intermediate certificate authorities<br />
        /// Disallowed:           The store for revoked certificates<br />
        /// My:                   The store for personal certificates<br />
        /// Root:                 The store for trusted root certificate authorities <br />
        /// TrustedPeople:        The store for directly trusted people and resources<br />
        /// TrustedPublisher:     The store for directly trusted publishers<br />
        /// </summary>
        public string StoreName
        {
            get { return this.storeName; }
            set { this.storeName = value; }
        }

        /// <summary>
        /// Sets the name of the file.
        /// </summary>
        public ITaskItem FileName { get; set; }

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
                case "Add":
                    this.Add();
                    break;
                case "Remove":
                    this.Remove();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        /// <summary>
        /// Removes a certificate based on the Thumbprint
        /// </summary>
        private void Remove()
        {
            StoreLocation locationFlag = this.MachineStore ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
            X509Store store = new X509Store(this.StoreName, locationFlag);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);

            X509Certificate2Collection storecollection = store.Certificates;
            foreach (X509Certificate2 xcert509 in storecollection)
            {
                if (xcert509.Thumbprint == this.Thumbprint)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing Certificate: {0}", xcert509.Thumbprint));
                    store.Remove(xcert509);
                    break;
                }
            }

            store.Close();
        }

        /// <summary>
        /// Adds a certificate
        /// </summary>
        private void Add()
        {
            if (this.FileName == null)
            {
                this.Log.LogError("FileName not provided");
                return;
            }

            if (System.IO.File.Exists(this.FileName.GetMetadata("FullPath")) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "FileName not found: {0}", this.FileName.GetMetadata("FullPath")));
                return;
            }

            X509Certificate2 cert = new X509Certificate2();
            X509KeyStorageFlags keyflags = this.MachineStore ? X509KeyStorageFlags.MachineKeySet : X509KeyStorageFlags.DefaultKeySet;
            if (this.Exportable)
            {
                keyflags |= X509KeyStorageFlags.Exportable;
            }

            keyflags |= X509KeyStorageFlags.PersistKeySet;
            cert.Import(this.FileName.GetMetadata("FullPath"), this.CertPassword, keyflags);
            StoreLocation locationFlag = this.MachineStore ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding Certificate: {0} to Store: {1}", this.FileName.GetMetadata("FullPath"), this.StoreName));
            X509Store store = new X509Store(this.StoreName, locationFlag);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            store.Add(cert);
            store.Close();
            this.Thumbprint = cert.Thumbprint;
        }
    }
}