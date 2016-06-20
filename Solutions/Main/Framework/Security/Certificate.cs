//-----------------------------------------------------------------------
// <copyright file="Certificate.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Security
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using MSBuild.ExtensionPack.Security.Extended;

    internal enum CryptGetProvParamType
    {
        /// <summary>
        /// PP_ENUMALGS
        /// </summary>
        PP_ENUMALGS = 1,

        /// <summary>
        /// PP_ENUMCONTAINERS
        /// </summary>
        PP_ENUMCONTAINERS = 2,

        /// <summary>
        /// PP_IMPTYPE
        /// </summary>
        PP_IMPTYPE = 3,

        /// <summary>
        /// PP_NAME
        /// </summary>
        PP_NAME = 4,

        /// <summary>
        /// PP_VERSION
        /// </summary>
        PP_VERSION = 5,

        /// <summary>
        /// PP_CONTAINER
        /// </summary>
        PP_CONTAINER = 6,

        /// <summary>
        /// PP_CHANGE_PASSWORD
        /// </summary>
        PP_CHANGE_PASSWORD = 7,

        /// <summary>
        /// PP_KEYSET_SEC_DESCR
        /// </summary>
        PP_KEYSET_SEC_DESCR = 8,

        /// <summary>
        /// PP_CERTCHAIN
        /// </summary>
        PP_CERTCHAIN = 9,

        /// <summary>
        /// PP_KEY_TYPE_SUBTYPE
        /// </summary>
        PP_KEY_TYPE_SUBTYPE = 10,

        /// <summary>
        /// PP_PROVTYPE
        /// </summary>
        PP_PROVTYPE = 16,

        /// <summary>
        /// PP_KEYSTORAGE
        /// </summary>
        PP_KEYSTORAGE = 17,

        /// <summary>
        /// PP_APPLI_CERT
        /// </summary>
        PP_APPLI_CERT = 18,

        /// <summary>
        /// PP_SYM_KEYSIZE
        /// </summary>
        PP_SYM_KEYSIZE = 19,

        /// <summary>
        /// PP_SESSION_KEYSIZE
        /// </summary>
        PP_SESSION_KEYSIZE = 20,

        /// <summary>
        /// PP_UI_PROMPT
        /// </summary>
        PP_UI_PROMPT = 21,

        /// <summary>
        /// PP_ENUMALGS_EX
        /// </summary>
        PP_ENUMALGS_EX = 22,

        /// <summary>
        /// PP_ENUMMANDROOTS
        /// </summary>
        PP_ENUMMANDROOTS = 25,

        /// <summary>
        /// PP_ENUMELECTROOTS
        /// </summary>
        PP_ENUMELECTROOTS = 26,

        /// <summary>
        /// PP_KEYSET_TYPE
        /// </summary>
        PP_KEYSET_TYPE = 27,

        /// <summary>
        /// PP_ADMIN_PIN
        /// </summary>
        PP_ADMIN_PIN = 31,

        /// <summary>
        /// PP_KEYEXCHANGE_PIN
        /// </summary>
        PP_KEYEXCHANGE_PIN = 32,

        /// <summary>
        /// PP_SIGNATURE_PIN
        /// </summary>
        PP_SIGNATURE_PIN = 33,

        /// <summary>
        /// PP_SIG_KEYSIZE_INC
        /// </summary>
        PP_SIG_KEYSIZE_INC = 34,

        /// <summary>
        /// PP_KEYX_KEYSIZE_INC
        /// </summary>
        PP_KEYX_KEYSIZE_INC = 35,

        /// <summary>
        /// PP_UNIQUE_CONTAINER
        /// </summary>
        PP_UNIQUE_CONTAINER = 36,

        /// <summary>
        /// PP_SGC_INFO
        /// </summary>
        PP_SGC_INFO = 37,

        /// <summary>
        /// PP_USE_HARDWARE_RNG
        /// </summary>
        PP_USE_HARDWARE_RNG = 38,

        /// <summary>
        /// PP_KEYSPEC
        /// </summary>
        PP_KEYSPEC = 39,

        /// <summary>
        /// PP_ENUMEX_SIGNING_PROT
        /// </summary>
        PP_ENUMEX_SIGNING_PROT = 40,

        /// <summary>
        /// PP_CRYPT_COUNT_KEY_USE
        /// </summary>
        PP_CRYPT_COUNT_KEY_USE = 41,
    }

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Add</i> (<b>Required: </b>FileName <b>Optional: </b>MachineStore, CertPassword, Exportable, StoreName  <b>Output: </b>Thumbprint, SubjectDName)</para>
    /// <para><i>GetBase64EncodedCertificate</i> (<b>Required:  Thumbprint or SubjectDName</b> <b> Optional:</b> MachineStore, <b>Output:</b> Base64EncodedCertificate)</para>
    /// <para><i>GetExpiryDate</i> (<b>Required: </b>  Thumbprint or SubjectDName<b> Optional: MachineStore, </b> <b>Output:</b> CertificateExpiryDate)</para>
    /// <para><i>GetInfo</i> (<b>Required: </b> Thumbprint or SubjectDName <b> Optional:</b> MachineStore, StoreName <b>Output:</b> CertInfo)</para>
    /// <para><i>Remove</i> (<b>Required: </b>Thumbprint or SubjectDName <b>Optional: </b>MachineStore, StoreName)</para>
    /// <para><i>SetUserRights</i> (<b>Required: </b> AccountName, Thumbprint or SubjectDName<b> Optional:</b> MachineStore, <b>Output:</b> )</para>
    /// <para><b>Remote Execution Support:</b> No</para>
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
    ///         <!-- Add a certificate -->
    ///         <MSBuild.ExtensionPack.Security.Certificate TaskAction="Add" FileName="C:\MyCertificate.cer" CertPassword="PASSW">
    ///             <Output TaskParameter="Thumbprint" PropertyName="TPrint"/>
    ///             <Output TaskParameter="SubjectDName" PropertyName="SName"/>
    ///         </MSBuild.ExtensionPack.Security.Certificate>
    ///         <Message Text="Thumbprint: $(TPrint)"/>
    ///         <Message Text="SubjectName: $(SName)"/>
    ///         <!-- Get Certificate Information -->
    ///         <MSBuild.ExtensionPack.Security.Certificate TaskAction="GetInfo" SubjectDName="$(SName)">
    ///             <Output TaskParameter="CertInfo" ItemName="ICertInfo" />
    ///         </MSBuild.ExtensionPack.Security.Certificate>
    ///         <Message Text="SubjectName: %(ICertInfo.SubjectName)"/>
    ///         <Message Text="SubjectNameOidValue: %(ICertInfo.SubjectNameOidValue)"/>
    ///         <Message Text="SerialNumber: %(ICertInfo.SerialNumber)"/>
    ///         <Message Text="Archived: %(ICertInfo.Archived)"/>
    ///         <Message Text="NotBefore: %(ICertInfo.NotBefore)"/>
    ///         <Message Text="NotAfter: %(ICertInfo.NotAfter)"/>
    ///         <Message Text="PrivateKeyFileName: %(ICertInfo.PrivateKeyFileName)"/>
    ///         <Message Text="FriendlyName: %(ICertInfo.FriendlyName)"/>
    ///         <Message Text="HasPrivateKey: %(ICertInfo.HasPrivateKey)"/>
    ///         <Message Text="Thumbprint: %(ICertInfo.Thumbprint)"/>
    ///         <Message Text="Version: %(ICertInfo.Version)"/>
    ///         <Message Text="PrivateKeyFileName: %(ICertInfo.PrivateKeyFileName)"/>
    ///         <Message Text="SignatureAlgorithm: %(ICertInfo.SignatureAlgorithm)"/>
    ///         <Message Text="IssuerName: %(ICertInfo.IssuerName)"/>
    ///         <Message Text="PrivateKeyFileName: %(ICertInfo.PrivateKeyFileName)"/>
    ///          <!-- Remove a certificate -->
    ///         <MSBuild.ExtensionPack.Security.Certificate TaskAction="Remove" Thumbprint="$(TPrint)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>    
    public class Certificate : BaseTask
    {
        private const string AddTaskAction = "Add";
        private const string RemoveTaskAction = "Remove";
        private const string SetUserRightsTaskAction = "SetUserRights";
        private const string GetExpiryDateTaskAction = "GetExpiryDate";
        private const string GetBase64EncodedCertificateTaskAction = "GetBase64EncodedCertificate";
        private const string GetInfoTaskAction = "GetInfo";
        private const string AccessRightsRead = "Read";
        private const string AccessRightsReadAndExecute = "ReadAndExecute";
        private const string AccessRightsWrite = "Write";
        private const string AccessRightsFullControl = "FullControl";
        private StoreName storeName = System.Security.Cryptography.X509Certificates.StoreName.My;

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
        /// The distinguished subject name of the certificate
        /// </summary>
        [Output]
        public string SubjectDName { get; set; }

        /// <summary>
        /// Gets or sets the Base 64 Encoded string of the certificate
        /// </summary>
        [Output]
        public string Base64EncodedCertificate { get; set; }

        /// <summary>
        /// Gets the thumbprint. Used to uniquely identify certificate in further tasks
        /// The thumprint  can be used in place of distinguished name to identify a certificate
        /// </summary>
        [Output]
        public string Thumbprint { get; set; }

        /// <summary>
        /// Gets the Distinguished Name for the certificate used to to uniquely identify certificate in further tasks.
        /// The distinguished name can be used in place of thumbprint to identify a certificate
        /// </summary>
        [Output]
        public string DistinguishedName { get; set; }

        /// <summary>
        /// Gets the Certificate Exprity Date.
        /// </summary>
        [Output]
        public string CertificateExpiryDate { get; set; }

        /// <summary>
        /// The name of user or group that needs to be given rights on the given certificate
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// The access rights that need to be given.
        /// </summary>    
        public string AccessRights { get; set; }

        /// <summary>
        /// Sets the name of the store. Defaults to My
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
            get { return this.storeName.ToString(); }
            set { this.storeName = (StoreName)Enum.Parse(typeof(StoreName), value); }
        }

        /// <summary>
        /// Sets the name of the file.
        /// </summary>
        [Output]
        public ITaskItem FileName { get; set; }

        /// <summary>
        /// Gets the item which contains the Certificate information. The following Metadata is populated: SubjectName, SignatureAlgorithm, SubjectNameOidValue, SerialNumber, Archived, NotAfter, NotBefore, FriendlyName, HasPrivateKey, Thumbprint, Version, PrivateKeyFileName, IssuerName
        /// </summary>
        [Output]
        public ITaskItem CertInfo { get; protected set; }

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
                case AddTaskAction:
                    this.Add();
                    break;
                case RemoveTaskAction:
                    this.Remove();
                    break;
                case SetUserRightsTaskAction:
                    this.SetUserAccessRights();
                    break;
                case GetExpiryDateTaskAction:
                    this.GetCertificateExpiryDate();
                    break;
                case GetBase64EncodedCertificateTaskAction:
                    this.GetCertificateAsBase64String();
                    break;
                case GetInfoTaskAction:
                    this.GetInfo();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        /// <summary>
        /// Extracts a certificate from the certificate Distinguished Name
        /// </summary>
        /// <param name="distinguishedName">The distinguished name of the certificate</param>
        /// <param name="certificateStore">The certificate store to look for certificate for.</param>        
        /// <returns>Returns the X509 certificate with the given DName</returns>
        private static X509Certificate2 GetCertificateFromDistinguishedName(string distinguishedName, X509Store certificateStore)
        {
            // Iterate through each certificate trying to find the first unexpired certificate
            return certificateStore.Certificates.Cast<X509Certificate2>().FirstOrDefault(certificate => string.Compare(certificate.Subject, distinguishedName, StringComparison.CurrentCultureIgnoreCase) == 0);
        }

        /// <summary>
        /// Extracts a certificate from the certificate Thumbprint Name
        /// </summary>
        /// <param name="thumbprint">The thumbprint of the certificate to look for</param>
        /// <param name="certificateStore">The certificate store to look for certificate for.</param>        
        /// <returns>Returns the X509 certificate with the given DName</returns>
        private static X509Certificate2 GetCertificateFromThumbprint(string thumbprint, X509Store certificateStore)
        {
            // Iterate through each certificate trying to find the first unexpired certificate
            return certificateStore.Certificates.Cast<X509Certificate2>().FirstOrDefault(certificate => certificate.Thumbprint == thumbprint);
        }

        /// <summary>
        /// The method search for the given Key Name in the Application Data folders and return the folder location 
        /// where the key file resides
        /// </summary>
        /// <param name="keyFileName">The name of the key file whose file location needs to be found</param>
        /// <returns>Returns the location of the given key file name</returns>
        private static string FindKeyLocation(string keyFileName)
        {
            // First search for the key in the Common (All User) Application Data directory
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string rsaFolder = appDataFolder + @"\Microsoft\Crypto\RSA\MachineKeys";
            if (Directory.GetFiles(rsaFolder, keyFileName).Length > 0)
            {
                return rsaFolder;
            }

            // If not found, search the key in the currently signed in user's Application Data directory
            appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            rsaFolder = appDataFolder + @"\Microsoft\Crypto\RSA\";
            string[] directoryList = Directory.GetDirectories(rsaFolder);
            if (directoryList.Length > 0)
            {
                foreach (string directoryName in directoryList)
                {
                    if (Directory.GetFiles(directoryName, keyFileName).Length != 0)
                    {
                        return directoryName;
                    }
                }
            }

            return string.Empty;
        }

        private static string GetKeyFileName(X509Certificate cert)
        {
            IntPtr hprovider = IntPtr.Zero; // CSP handle
            bool freeProvider = false; // Do we need to free the CSP ?
            const uint AcquireFlags = 0;
            int keyNumber = 0;
            string keyFileName = null;

            // Determine whether there is private key information available for this certificate in the key store
            if (NativeMethods.CryptAcquireCertificatePrivateKey(cert.Handle, AcquireFlags, IntPtr.Zero, ref hprovider, ref keyNumber, ref freeProvider))
            {
                IntPtr pbytes = IntPtr.Zero; // Native Memory for the CRYPT_KEY_PROV_INFO structure
                int cbbytes = 0; // Native Memory size
                try
                {
                    if (NativeMethods.CryptGetProvParam(hprovider, CryptGetProvParamType.PP_UNIQUE_CONTAINER, IntPtr.Zero, ref cbbytes, 0))
                    {
                        pbytes = Marshal.AllocHGlobal(cbbytes);

                        if (NativeMethods.CryptGetProvParam(hprovider, CryptGetProvParamType.PP_UNIQUE_CONTAINER, pbytes, ref cbbytes, 0))
                        {
                            byte[] keyFileBytes = new byte[cbbytes];

                            Marshal.Copy(pbytes, keyFileBytes, 0, cbbytes);

                            // Copy eveything except tailing null byte
                            keyFileName = System.Text.Encoding.ASCII.GetString(keyFileBytes, 0, keyFileBytes.Length - 1);
                        }
                    }
                }
                finally
                {
                    if (freeProvider)
                    {
                        NativeMethods.CryptReleaseContext(hprovider, 0);
                    }

                    // Free our native memory
                    if (pbytes != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pbytes);
                    }
                }
            }

            return keyFileName ?? string.Empty;
        }

        private void GetInfo()
        {
            StoreLocation locationFlag = this.MachineStore ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
            X509Store store = this.GetStore(locationFlag);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            X509Certificate2 cert = null;

            if (!string.IsNullOrEmpty(this.Thumbprint))
            {
                var matches = store.Certificates.Find(X509FindType.FindByThumbprint, this.Thumbprint, false);
                if (matches.Count > 1)
                {
                    this.Log.LogError("More than one certificate with Thumbprint '{0}' found in the {1} store.", this.Thumbprint, this.StoreName);
                    return;
                }

                if (matches.Count == 0)
                {
                    this.Log.LogError("No certificates with Thumbprint '{0}' found in the {1} store.", this.Thumbprint, this.StoreName);
                    return;
                }

                cert = matches[0];
            }
            else if (!string.IsNullOrEmpty(this.SubjectDName))
            {
                var matches = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, this.SubjectDName, false);
                if (matches.Count > 1)
                {
                    this.Log.LogError("More than one certificate with SubjectDName '{0}' found in the {1} store.", this.SubjectDName, this.StoreName);
                    return;
                }

                if (matches.Count == 0)
                {
                    this.Log.LogError("No certificates with SubjectDName '{0}' found in the {1} store.", this.SubjectDName, this.StoreName);
                    return;
                }

                cert = matches[0];
            }

            if (cert != null)
            {
                this.CertInfo = new TaskItem("CertInfo");
                this.CertInfo.SetMetadata("SubjectName", cert.SubjectName.Name);
                this.CertInfo.SetMetadata("SubjectNameOidValue", cert.SubjectName.Oid.Value ?? string.Empty);
                this.CertInfo.SetMetadata("SerialNumber", cert.SerialNumber ?? string.Empty);
                this.CertInfo.SetMetadata("Archived", cert.Archived.ToString());
                this.CertInfo.SetMetadata("NotBefore", cert.NotBefore.ToString(CultureInfo.CurrentCulture));
                this.CertInfo.SetMetadata("FriendlyName", cert.FriendlyName);
                this.CertInfo.SetMetadata("HasPrivateKey", cert.HasPrivateKey.ToString());
                this.CertInfo.SetMetadata("Thumbprint", cert.Thumbprint ?? string.Empty);
                this.CertInfo.SetMetadata("Version", cert.Version.ToString());
                this.CertInfo.SetMetadata("SignatureAlgorithm", cert.SignatureAlgorithm.FriendlyName);
                this.CertInfo.SetMetadata("IssuerName", cert.IssuerName.Name);
                this.CertInfo.SetMetadata("NotAfter", cert.NotAfter.ToString(CultureInfo.CurrentCulture));

                var privateKeyFileName = GetKeyFileName(cert);
                if (!string.IsNullOrEmpty(privateKeyFileName))
                {
                    // Adapted from the FindPrivateKey application.  See http://msdn.microsoft.com/en-us/library/aa717039(v=VS.90).aspx.
                    var keyFileDirectory = this.GetKeyFileDirectory(privateKeyFileName);
                    if (!string.IsNullOrEmpty(privateKeyFileName) && !string.IsNullOrEmpty(keyFileDirectory))
                    {
                        this.CertInfo.SetMetadata("PrivateKeyFileName", Path.Combine(keyFileDirectory, privateKeyFileName));
                    }
                }
            }

            store.Close();
        }

        private void Remove()
        {
            StoreLocation locationFlag = this.MachineStore ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
            X509Store store = this.GetStore(locationFlag);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            X509Certificate2 cert;
            if (!string.IsNullOrEmpty(this.Thumbprint))
            {
                var matches = store.Certificates.Find(X509FindType.FindByThumbprint, this.Thumbprint, false);
                if (matches.Count > 1)
                {
                    this.Log.LogError("More than one certificate with Thumbprint '{0}' found in the {1} store.", this.Thumbprint, this.StoreName);
                    return;
                }

                if (matches.Count == 0)
                {
                    this.Log.LogError("No certificates with Thumbprint '{0}' found in the {1} store.", this.Thumbprint, this.StoreName);
                    return;
                }

                cert = matches[0];
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing Certificate: {0}", cert.Thumbprint));
                store.Remove(cert);
            }
            else if (!string.IsNullOrEmpty(this.SubjectDName))
            {
                var matches = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, this.SubjectDName, false);
                if (matches.Count > 1)
                {
                    this.Log.LogError("More than one certificate with SubjectDName '{0}' found in the {1} store.", this.SubjectDName, this.StoreName);
                    return;
                }

                if (matches.Count == 0)
                {
                    this.Log.LogError("No certificates with SubjectDName '{0}' found in the {1} store.", this.SubjectDName, this.StoreName);
                    return;
                }

                cert = matches[0];
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing Certificate: {0}", cert.SubjectName));
                store.Remove(cert);
            }

            store.Close();
        }

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
            X509Store store = this.GetStore(locationFlag);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            store.Add(cert);
            store.Close();
            this.Thumbprint = cert.Thumbprint;
            this.SubjectDName = cert.SubjectName.Name;
        }

        private string GetKeyFileDirectory(string keyFileName)
        {
            // Look up All User profile from environment variable
            string allUserProfile = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            // set up searching directory
            string machineKeyDir = allUserProfile + "\\Microsoft\\Crypto\\RSA\\MachineKeys";

            // Seach the key file
            string[] fs = System.IO.Directory.GetFiles(machineKeyDir, keyFileName);

            // If found
            if (fs.Length > 0)
            {
                return machineKeyDir;
            }

            // Next try current user profile
            string currentUserProfile = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // seach all sub directory
            string userKeyDir = currentUserProfile + "\\Microsoft\\Crypto\\RSA\\";

            fs = System.IO.Directory.GetDirectories(userKeyDir);
            if (fs.Length > 0)
            {
                // for each sub directory
                foreach (string keyDir in fs)
                {
                    fs = System.IO.Directory.GetFiles(keyDir, keyFileName);
                    if (fs.Length == 0)
                    {
                        continue;
                    }

                    return keyDir;
                }
            }

            this.Log.LogError("Unable to locate private key file directory");
            return string.Empty;
        }

        /// <summary>
        /// Retrieves the Expiry Date of the Certificate
        /// </summary>
        private void GetCertificateExpiryDate()
        {
            StoreLocation locationFlag = this.MachineStore ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
            X509Store store = this.GetStore(locationFlag);
            X509Certificate2 certificate = null;

            try
            {
                store.Open(OpenFlags.ReadOnly);
                if (string.IsNullOrEmpty(this.Thumbprint) == false)
                {
                    certificate = GetCertificateFromThumbprint(this.Thumbprint, store);
                }
                else if (string.IsNullOrEmpty(this.DistinguishedName) == false)
                {
                    certificate = GetCertificateFromDistinguishedName(this.DistinguishedName, store);
                }

                if (certificate == null)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Error fetching expiry date. Could not find the certificate in the certificate store"));
                }
                else
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Returning Expiry Date of Certificate: {0}", certificate.Thumbprint));
                    this.CertificateExpiryDate = certificate.NotAfter.ToString("s", CultureInfo.CurrentCulture);
                }
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// Retrieves the Expiry Date of the Certificate
        /// </summary>
        private void GetCertificateAsBase64String()
        {
            StoreLocation locationFlag = this.MachineStore ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
            X509Store store = this.GetStore(locationFlag);
            X509Certificate2 certificate = null;

            try
            {
                store.Open(OpenFlags.ReadOnly);
                if (string.IsNullOrEmpty(this.Thumbprint) == false)
                {
                    certificate = GetCertificateFromThumbprint(this.Thumbprint, store);
                }
                else if (string.IsNullOrEmpty(this.DistinguishedName) == false)
                {
                    certificate = GetCertificateFromDistinguishedName(this.DistinguishedName, store);
                }

                if (certificate == null)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Error fetching base 64 encoded certificate string. Could not find the certificate in the certificate store"));
                }
                else
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Returning Expiry Date of Certificate: {0}", certificate.Thumbprint));
                    this.Base64EncodedCertificate = Convert.ToBase64String(certificate.RawData);
                }
            }
            finally
            {
                store.Close();
            }
        }

        private X509Store GetStore(StoreLocation locationFlag)
        {
            X509Store store = new X509Store(this.storeName, locationFlag);
            this.Log.LogMessage(MessageImportance.Low, "Opening store {0} at location {1}.", this.StoreName, locationFlag);
            return store;
        }

        /// <summary>
        /// Set the given user access rights on the given certificate to the given user
        /// </summary>
        private void SetUserAccessRights()
        {
            StoreLocation locationFlag = this.MachineStore ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
            X509Store store = this.GetStore(locationFlag);
            X509Certificate2 certificate = null;

            try
            {
                store.Open(OpenFlags.ReadOnly);
                if (string.IsNullOrEmpty(this.Thumbprint) == false)
                {
                    certificate = GetCertificateFromThumbprint(this.Thumbprint, store);
                }
                else if (string.IsNullOrEmpty(this.DistinguishedName) == false)
                {
                    certificate = GetCertificateFromDistinguishedName(this.DistinguishedName, store);
                }

                if (certificate == null)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Error in setting user rights on certificate. Could not find the certificate in the certificate store"));
                }
                else
                {
                    RSACryptoServiceProvider rsa = certificate.PrivateKey as RSACryptoServiceProvider;
                    FileSystemRights fileSystemAccessRights = FileSystemRights.ReadAndExecute;
                    if (rsa != null)
                    {
                        switch (this.AccessRights)
                        {
                            case AccessRightsRead:
                                fileSystemAccessRights = FileSystemRights.Read;
                                break;

                            case AccessRightsReadAndExecute:
                                fileSystemAccessRights = FileSystemRights.ReadAndExecute;
                                break;

                            case AccessRightsWrite:
                                fileSystemAccessRights = FileSystemRights.Write;
                                break;

                            case AccessRightsFullControl:
                                fileSystemAccessRights = FileSystemRights.FullControl;
                                break;
                        }

                        string keyfilepath = FindKeyLocation(rsa.CspKeyContainerInfo.UniqueKeyContainerName);
                        FileInfo file = new FileInfo(keyfilepath + "\\" + rsa.CspKeyContainerInfo.UniqueKeyContainerName);
                        FileSecurity fs = file.GetAccessControl();
                        NTAccount account = new NTAccount(this.AccountName);
                        fs.AddAccessRule(new FileSystemAccessRule(account, fileSystemAccessRights, AccessControlType.Allow));
                        file.SetAccessControl(fs);
                    }
                }
            }
            finally
            {
                store.Close();
            }
        }
    }
}
