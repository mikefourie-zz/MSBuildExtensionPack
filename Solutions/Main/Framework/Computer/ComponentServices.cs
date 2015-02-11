//-----------------------------------------------------------------------
// <copyright file="ComponentServices.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.EnterpriseServices;
    using System.Globalization;
    using System.Linq;
    using COMAdmin;
    using Microsoft.Build.Framework;
    using Microsoft.Win32;

    internal enum CSActivation
    {
        /// <summary>
        /// Inproc
        /// </summary>
        Inproc = 0,

        /// <summary>
        /// Local
        /// </summary>
        Local = 1
    }

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddComponent</i> (<b>Required: </b>Path, ApplicationName <b>Optional: </b>Activation, Identity, UserPassword, Framework)</para>
    /// <para><i>AddNativeComponent</i> (<b>Required: </b>Path, ApplicationName <b>Optional: </b>Activation, Identity, UserPassword, Framework)</para>
    /// <para><i>CheckApplicationExists</i> (<b>Required: </b> ApplicationName <b>Output: </b>Exists)</para>
    /// <para><i>CreateApplication</i> (<b>Required: </b> ApplicationName <b>Optional: </b>Activation, EnforceAccessChecks, Identity, UserPassword)</para>
    /// <para><i>DeleteApplication</i> (<b>Required: </b>ApplicationName)</para>
    /// <para><i>RemoveComponent</i> (<b>Required: </b>Path <b>Optional: </b>Framework)</para>
    /// <para><i>SetConstructor</i> (<b>Required: </b>ApplicationName, ComponentName, ConstructorString)</para>
    /// <para><i>SetAccessIisIntrinsicProperties</i> (<b>Required: </b>ApplicationName, ComponentName <b>Optional: </b>AllowIntrinsicIisProperties)</para>
    /// <para><i>SetTransactionSupport</i> (<b>Required: </b>ApplicationName, ComponentName, Transaction)</para>
    /// <para><i>ShutDownApplication</i> (<b>Required: </b>ApplicationName)</para>
    /// <para><i>UpdateApplication</i> (<b>Required: </b>ApplicationName <b>Optional: </b>Activation, Identity, UserPassword)</para>
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
    ///         <!--- Add a component -->
    ///         <MSBuild.ExtensionPack.Computer.ComponentServices TaskAction="AddComponent" Path="C:\Projects\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\EntServices.dll" ApplicationName="MyApplication" Identity="Interactive User"/>
    ///         <!-- Check it exists -->
    ///         <MSBuild.ExtensionPack.Computer.ComponentServices TaskAction="CheckApplicationExists" ApplicationName="MyApplication">
    ///             <Output TaskParameter="Exists" PropertyName="DoI"/>
    ///         </MSBuild.ExtensionPack.Computer.ComponentServices>
    ///         <Message Text="Exists: $(DoI)"/>
    ///         <!--- Remove the component -->
    ///         <MSBuild.ExtensionPack.Computer.ComponentServices TaskAction="RemoveComponent" Path="C:\Projects\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\EntServices.dll" ApplicationName="MyApplication"/>
    ///         <!-- Check it exists again-->
    ///         <MSBuild.ExtensionPack.Computer.ComponentServices TaskAction="CheckApplicationExists" ApplicationName="MyApplication">
    ///             <Output TaskParameter="Exists" PropertyName="DoI"/>
    ///         </MSBuild.ExtensionPack.Computer.ComponentServices>
    ///         <Message Text="Exists: $(DoI)"/>
    ///         <!--- Add a component -->
    ///         <MSBuild.ExtensionPack.Computer.ComponentServices TaskAction="AddComponent" Path="C:\Projects\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\EntServices.dll" ApplicationName="MyApplication" Identity="Interactive User"/>
    ///         <!-- Check it exists -->
    ///         <MSBuild.ExtensionPack.Computer.ComponentServices TaskAction="CheckApplicationExists" ApplicationName="MyApplication">
    ///             <Output TaskParameter="Exists" PropertyName="DoI"/>
    ///         </MSBuild.ExtensionPack.Computer.ComponentServices>
    ///         <Message Text="Exists: $(DoI)"/>
    ///         <!-- Various quick tasks -->
    ///         <MSBuild.ExtensionPack.Computer.ComponentServices TaskAction="ShutDownApplication" ApplicationName="MyApplication"/>
    ///         <MSBuild.ExtensionPack.Computer.ComponentServices TaskAction="UpdateApplication" Activation="Inproc" ApplicationName="MyApplication"/>
    ///         <MSBuild.ExtensionPack.Computer.ComponentServices TaskAction="SetTransactionSupport" Transaction="RequiresNew" ComponentName="BankComponent.Account" ApplicationName="MyApplication"/>
    ///         <MSBuild.ExtensionPack.Computer.ComponentServices TaskAction="SetConstructor" ComponentName="BankComponent.Account" ApplicationName="MyApplication" ConstructorString="demo"/>
    ///         <MSBuild.ExtensionPack.Computer.ComponentServices TaskAction="SetConstructor" ComponentName="BankComponent.Account" ApplicationName="MyApplication" ConstructorString=""/>
    ///         <MSBuild.ExtensionPack.Computer.ComponentServices TaskAction="DeleteApplication" ApplicationName="MyApplication"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class ComponentServices : BaseTask
    {
        private ShellWrapper shellWrapper;
        private string framework = "v2.0.50727";
        private CSActivation activation = CSActivation.Local;
        private string pathToFramework;
        private TransactionOption compTransaction = TransactionOption.NotSupported;
        private bool enforceAccessChecks = true;

        /// <summary>
        /// Gets whether the application exists.
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Sets the name of the COM+ component
        /// </summary>
        public string ComponentName { get; set; }

        /// <summary>
        /// Sets the Transaction support for the component. Supports: Ignored, None [Default], Supported, Required, RequiresNew
        /// </summary>
        public string Transaction
        {
            get { return this.compTransaction.ToString(); }
            set { this.compTransaction = (TransactionOption)Enum.Parse(typeof(TransactionOption), value); }
        }

        /// <summary>
        /// Sets the constructor string for the specified COM+ component. If empty, then the constructor support is removed
        /// </summary>
        public string ConstructorString { get; set; }

        /// <summary>
        /// Sets the name of the COM+ Application.
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Sets the path to the DLL to be added to the application
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Sets the process identity for the application. Specify a valid user account or "Interactive User" to have the application assume the identity of the current logged-on user.
        /// </summary>
        public string Identity { get; set; }

        /// <summary>
        /// Sets the version of the .NET FrameWork. Defaults to "v2.0.50727"
        /// </summary>
        public string Framework
        {
            get { return this.framework; }
            set { this.framework = value; }
        }

        /// <summary>
        /// Sets the type of activation for the application. Defaults to "Local". Supports: Local (server application), Inproc (library application)
        /// </summary>
        public string Activation { get; set; }

        /// <summary>
        /// Sets whether or not component services enforces access checks for this application. Defaults to "True". Supports: True (Enforce access checks), False 
        /// </summary>
        public bool EnforceAccessChecks
        {
            get { return this.enforceAccessChecks; }
            set { this.enforceAccessChecks = value; }
        }

        /// <summary>
        /// Sets whether or not component services allows access to Intrinsic IIS properties, used for Windows 2003
        /// components on Windows 2008 and later. Defaults to "False". Supports: True, False (allow access to Intrinsic IIS properties)
        /// </summary>
        public bool AllowIntrinsicIisProperties { get; set; }

        protected override void InternalExecute()
        {
            if (!string.IsNullOrEmpty(this.Activation))
            {
                this.activation = (CSActivation)Enum.Parse(typeof(CSActivation), this.Activation, true);
            }

            this.GetPathToFramework();

            switch (this.TaskAction)
            {
                case "AddComponent":
                    this.AddComponent();
                    break;
                case "AddNativeComponent":
                    this.AddNativeComponent();
                    break;
                case "SetTransactionSupport":
                    this.SetTransactionSupport();
                    break;
                case "SetConstructor":
                    this.SetConstructor();
                    break;
                case "SetAccessIisIntrinsicProperties":
                    this.SetAccessIisIntrinsicProperties();
                    break;
                case "DeleteApplication":
                    this.DeleteApplication();
                    break;
                case "ShutDownApplication":
                    this.ShutDownApplication();
                    break;
                case "StartApplication":
                    this.StartApplication();
                    break;
                case "UpdateApplication":
                    this.UpdateApplication();
                    break;
                case "CheckApplicationExists":
                    this.Exists = this.CheckApplicationExists();
                    break;
                case "RemoveComponent":
                    this.RemoveComponent();
                    break;
                case "CreateApplication":
                    this.CreateApplication();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private static COMAdminCatalogCollection GetApplications()
        {
            var objAdmin = new COMAdmin.COMAdminCatalog();
            var objCollection = (COMAdmin.COMAdminCatalogCollection)objAdmin.GetCollection("Applications");
            objCollection.Populate();
            return objCollection;
        }

        private bool IsValidAssemblyFile(string path)
        {
            try
            {
                System.Reflection.Assembly.LoadFrom(path);
                return true;
            }
            catch (BadImageFormatException)
            {
                Log.LogError("The Assembly is not a valid .Net assembly");
                return false;
            }
        }

        private bool CheckApplicationExists()
        {
            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Checking whether Application exists: {0}", this.ApplicationName));
            COMAdminCatalogCollection appCollection = GetApplications();
            if (appCollection.Cast<COMAdminCatalogObject>().Any(app => app.Name.ToString() == this.ApplicationName))
            {
                this.Exists = true;
                return true;
            }

            return false;
        }

        private void GetPathToFramework()
        {
            RegistryKey runtimeKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\.NETFramework");
            if (runtimeKey != null)
            {
                this.pathToFramework = Convert.ToString(runtimeKey.GetValue("InstallRoot"), CultureInfo.CurrentCulture);
                runtimeKey.Close();
            }
        }

        private void SetTransactionSupport()
        {
            if (!this.CheckApplicationExists())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "SetTransactionSupport on Component: {0}", this.ComponentName));
            COMAdminCatalogCollection appCollection = GetApplications();
            foreach (COMAdmin.COMAdminCatalogObject app in appCollection)
            {
                if (app.Name.ToString() == this.ApplicationName)
                {
                    COMAdmin.ICatalogCollection componentCollection = (COMAdmin.ICatalogCollection)appCollection.GetCollection("Components", app.Key);
                    componentCollection.Populate();
                    foreach (COMAdmin.COMAdminCatalogObject component in componentCollection)
                    {
                        if (component.Name.ToString() == this.ComponentName)
                        {
                            if (!string.IsNullOrEmpty(this.Transaction))
                            {
                                component.set_Value("Transaction", this.compTransaction);
                            }

                            componentCollection.SaveChanges();
                            break;
                        }
                    }

                    break;
                }
            }

            appCollection.SaveChanges();
        }

        private void SetConstructor()
        {
            if (!this.CheckApplicationExists())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "SetConstructor on Component: {0}", this.ComponentName));
            COMAdminCatalogCollection appCollection = GetApplications();
            foreach (COMAdmin.COMAdminCatalogObject app in appCollection)
            {
                if (app.Name.ToString() == this.ApplicationName)
                {
                    COMAdmin.ICatalogCollection componentCollection = (COMAdmin.ICatalogCollection)appCollection.GetCollection("Components", app.Key);
                    componentCollection.Populate();
                    foreach (COMAdmin.COMAdminCatalogObject component in componentCollection)
                    {
                        if (component.Name.ToString() == this.ComponentName)
                        {
                            component.set_Value("ConstructionEnabled", !string.IsNullOrEmpty(this.ConstructorString));
                            component.set_Value("ConstructorString", this.ConstructorString ?? string.Empty);
                            componentCollection.SaveChanges();
                            break;
                        }
                    }

                    break;
                }
            }

            appCollection.SaveChanges();
        }

        private void SetAccessIisIntrinsicProperties()
        {
            if (!this.CheckApplicationExists())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "SetAccessIisIntrinsicProperties on Component: {0}", this.ComponentName));
            COMAdminCatalogCollection appCollection = GetApplications();
            foreach (COMAdmin.COMAdminCatalogObject app in appCollection)
            {
                if (app.Name.ToString() == this.ApplicationName)
                {
                    COMAdmin.ICatalogCollection componentCollection = (COMAdmin.ICatalogCollection)appCollection.GetCollection("Components", app.Key);
                    componentCollection.Populate();
                    foreach (COMAdmin.COMAdminCatalogObject component in componentCollection)
                    {
                        if (component.Name.ToString() == this.ComponentName)
                        {
                            component.set_Value("IISIntrinsics", this.AllowIntrinsicIisProperties);
                            componentCollection.SaveChanges();
                            break;
                        }
                    }

                    break;
                }
            }

            appCollection.SaveChanges();
        }

        private void UpdateApplication()
        {
            if (!this.CheckApplicationExists())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Updating: {0}", this.ApplicationName));
            COMAdminCatalogCollection appCollection = GetApplications();
            foreach (COMAdmin.COMAdminCatalogObject app in appCollection)
            {
                if (app.Name.ToString() == this.ApplicationName)
                {
                    if (!string.IsNullOrEmpty(this.Identity))
                    {
                        app.set_Value("Identity", this.Identity);
                        app.set_Value("Password", this.UserPassword ?? string.Empty);
                    }

                    app.set_Value("Activation", this.activation.ToString());
                    app.set_Value("ApplicationAccessChecksEnabled", this.EnforceAccessChecks);
                    appCollection.SaveChanges();
                    break;
                }
            }
        }

        private void StartApplication()
        {
            if (this.CheckApplicationExists())
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Starting Application: {0}", this.ApplicationName));
                COMAdmin.COMAdminCatalog f = new COMAdminCatalog();
                f.StartApplication(this.ApplicationName);
            }
        }

        private void ShutDownApplication()
        {
            if (this.CheckApplicationExists())
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Stopping Application: {0}", this.ApplicationName));
                COMAdmin.COMAdminCatalog f = new COMAdminCatalog();
                f.ShutdownApplication(this.ApplicationName);
            }
        }

        private void RemoveComponent()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Removing Component: {0}", this.Path));
            if (System.IO.File.Exists(this.Path) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Path not found: {0}", this.Path));
                return;
            }

            if (this.IsValidAssemblyFile(this.Path))
            {
                string args = string.Format(CultureInfo.CurrentCulture, @"/quiet /u ""{0}""", this.Path);
                this.shellWrapper = new ShellWrapper(System.IO.Path.Combine(System.IO.Path.Combine(this.pathToFramework, this.framework), "regsvcs.exe"), args);
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Executing {0} with {1}", this.shellWrapper.Executable, this.shellWrapper.Arguments));
                if (this.shellWrapper.Execute() != 0)
                {
                    this.Log.LogError("Shell execute failed: " + this.shellWrapper.StandardOutput);
                }
            }
        }

        private void DeleteApplication()
        {
            if (!this.CheckApplicationExists())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Deleting Application: {0}", this.ApplicationName));

            COMAdminCatalogCollection appCollection = GetApplications();
            int i = 0;
            foreach (COMAdmin.COMAdminCatalogObject cat in appCollection)
            {
                if (cat.Name.ToString() == this.ApplicationName)
                {
                    appCollection.Remove(i);
                    appCollection.SaveChanges();
                    break;
                }

                i++;
            }
        }

        private void CreateApplication()
        {
            if (this.CheckApplicationExists())
            {
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Creating Application: {0}", this.ApplicationName));

            COMAdminCatalogCollection appCollection = GetApplications();
            COMAdminCatalogObject app = (COMAdminCatalogObject)appCollection.Add();
            app.set_Value("Name", this.ApplicationName);
            
            if (!string.IsNullOrEmpty(this.Identity))
            {
                app.set_Value("Identity", this.Identity);
                app.set_Value("Password", this.UserPassword ?? string.Empty);
            }

            app.set_Value("Activation", this.activation.ToString());
            app.set_Value("ApplicationAccessChecksEnabled", this.EnforceAccessChecks);

            appCollection.SaveChanges();
        }

        private void AddComponent()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding Component: {0} to Application: {1}", this.Path, this.ApplicationName));
            if (System.IO.File.Exists(this.Path) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Path not found: {0}", this.Path));
                return;
            }

            if (this.IsValidAssemblyFile(this.Path))
            {
                this.shellWrapper = new ShellWrapper(System.IO.Path.Combine(System.IO.Path.Combine(this.pathToFramework, this.framework), "regsvcs.exe"), string.IsNullOrEmpty(this.ApplicationName) ? string.Format(CultureInfo.CurrentCulture, @"/quiet ""{0}""", this.Path) : string.Format(CultureInfo.CurrentCulture, @"/quiet ""{0}"" ""{1}""", this.Path, this.ApplicationName));
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Executing {0} with {1}", this.shellWrapper.Executable, this.shellWrapper.Arguments));
                if (this.shellWrapper.Execute() != 0)
                {
                    this.Log.LogError("Shell execute failed: " + this.shellWrapper.StandardOutput);
                }
            }
        }

        private void AddNativeComponent()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Adding Native Component: {0} to Application: {1}", this.Path, this.ApplicationName));
            if (System.IO.File.Exists(this.Path) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Path not found: {0}", this.Path));
                return;
            }

            COMAdminCatalogCollection appCollection = GetApplications();
            bool appExists = false;
            foreach (COMAdmin.COMAdminCatalogObject app in appCollection)
            {
                if (app.Name.ToString() == this.ApplicationName)
                {
                    appExists = true;
                    var cat = new COMAdminCatalog();
                    cat.InstallComponent(app.Key.ToString(), this.Path, string.Empty, string.Empty);
                    break;
                }
            }

            if (!appExists)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Application not found: {0}", this.ApplicationName));
            }
        }
    }
}