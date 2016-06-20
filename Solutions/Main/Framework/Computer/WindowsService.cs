//-----------------------------------------------------------------------
// <copyright file="WindowsService.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Management;
    using System.ServiceProcess;
    using Microsoft.Build.Framework;
    using Microsoft.Win32;

    /// <summary>
    /// Start mode of the Windows base service.
    /// </summary>
    internal enum ServiceStartMode
    {
        /// <summary>
        /// Service to be started automatically by the Service Control Manager during system startup.
        /// </summary>
        Automatic,

        /// <summary>
        /// Device driver started by the operating system loader. This value is valid only for driver services.
        /// </summary>
        Boot,

        /// <summary>
        /// Device driver started by the operating system initialization process. This value is valid only for driver services.
        /// </summary>
        System,

        /// <summary>
        /// Service to be started by the Service Control Manager when a process calls the StartService method.
        /// </summary>
        Manual,

        /// <summary>
        /// Service that can no longer be started.
        /// </summary>
        Disabled,

        /// <summary>
        /// Service to be started automatically by the Service Control Manager after all the service designated as Automatic have been started.
        /// </summary>
        AutomaticDelayedStart,
    }

    /// <summary>
    /// The return code from the WMI Class Win32_Service
    /// </summary>
    internal enum ServiceReturnCode
    {
        /// <summary>
        /// Success
        /// </summary>
        Success = 0,

        /// <summary>
        /// Not Supported
        /// </summary>
        NotSupported = 1,

        /// <summary>
        /// Access Denied
        /// </summary>
        AccessDenied = 2,

        /// <summary>
        /// Dependent Services Running
        /// </summary>
        DependentServicesRunning = 3,

        /// <summary>
        /// Invalid Service Control
        /// </summary>
        InvalidServiceControl = 4,

        /// <summary>
        /// Service Cannot Accept Control
        /// </summary>
        ServiceCannotAcceptControl = 5,

        /// <summary>
        /// Service Not Active
        /// </summary>
        ServiceNotActive = 6,

        /// <summary>
        /// Service Request Timeout
        /// </summary>
        ServiceRequestTimeout = 7,

        /// <summary>
        /// Unknown Failure
        /// </summary>
        UnknownFailure = 8,

        /// <summary>
        /// Path Not Found
        /// </summary>
        PathNotFound = 9,

        /// <summary>
        /// Service Already Running
        /// </summary>
        ServiceAlreadyRunning = 10,

        /// <summary>
        /// Service Database Locked
        /// </summary>
        ServiceDatabaseLocked = 11,

        /// <summary>
        /// Service Dependency Deleted
        /// </summary>
        ServiceDependencyDeleted = 12,

        /// <summary>
        /// Service Dependency Failure
        /// </summary>
        ServiceDependencyFailure = 13,

        /// <summary>
        /// Service Disabled
        /// </summary>
        ServiceDisabled = 14,

        /// <summary>
        /// Service Logon Failure
        /// </summary>
        ServiceLogOnFailure = 15,

        /// <summary>
        /// Service Marked For Deletion
        /// </summary>
        ServiceMarkedForDeletion = 16,

        /// <summary>
        /// Service No Thread
        /// </summary>
        ServiceNoThread = 17,

        /// <summary>
        /// Status Circular Dependency
        /// </summary>
        StatusCircularDependency = 18,

        /// <summary>
        /// Status Duplicate Name
        /// </summary>
        StatusDuplicateName = 19,

        /// <summary>
        /// Status Invalid Name
        /// </summary>
        StatusInvalidName = 20,

        /// <summary>
        /// Status Invalid Parameter
        /// </summary>
        StatusInvalidParameter = 21,

        /// <summary>
        /// Status Invalid Service Account
        /// </summary>
        StatusInvalidServiceAccount = 22,

        /// <summary>
        /// Status Service Exists
        /// </summary>
        StatusServiceExists = 23,

        /// <summary>
        /// Service Already Paused
        /// </summary>
        ServiceAlreadyPaused = 24
    }

    /// <summary>
    /// Type of services provided to processes that call them.
    /// </summary>
    [Flags]
    internal enum ServiceTypes
    {
        /// <summary>
        /// Kernel Driverr
        /// </summary>
        KernalDriver = 1,

        /// <summary>
        /// File System Driver
        /// </summary>
        FileSystemDriver = 2,

        /// <summary>
        /// Adapter
        /// </summary>
        Adapter = 4,

        /// <summary>
        /// Recognizer Driver
        /// </summary>
        RecognizerDriver = 8,

        /// <summary>
        /// Own Process
        /// </summary>
        OwnProcess = 16,

        /// <summary>
        /// Share Process
        /// </summary>
        ShareProcess = 32,

        /// <summary>
        /// Interactive Process
        /// </summary>
        InteractiveProcess = 256
    }

    /// <summary>
    /// Severity of the error if the Create method fails to start. The value indicates the action taken by the startup program if failure occurs. All errors are logged by the system. 
    /// </summary>
    internal enum ServiceErrorControl
    {
        /// <summary>
        /// User is not notified.
        /// </summary>
        UserNotNotified = 0,

        /// <summary>
        /// User is notified.
        /// </summary>
        UserNotified = 1,

        /// <summary>
        /// System is restarted with the last-known-good configuration.
        /// </summary>
        SystemRestartedWithLastKnownGoodConfiguration = 2,

        /// <summary>
        /// System attempts to start with a good configuration.
        /// </summary>
        SystemAttemptsToStartWithAGoodConfiguration = 3
    }

    /// <summary>
    /// Current state of the base service
    /// </summary>
    internal enum ServiceState
    {
        /// <summary>
        /// Running
        /// </summary>
        Running,

        /// <summary>
        /// Stopped
        /// </summary>
        Stopped,

        /// <summary>
        /// Paused
        /// </summary>
        Paused,

        /// <summary>
        /// Start Pending
        /// </summary>
        StartPending,

        /// <summary>
        /// Stop Pending
        /// </summary>
        StopPending,

        /// <summary>
        /// Pause Pending
        /// </summary>
        PausePending,

        /// <summary>
        /// Continue Pending
        /// </summary>
        ContinuePending
    }

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CheckExists</i> (<b>Required: </b> ServiceName <b>Optional: </b>MachineName, RemoteUser, RemoteUserPassword <b>Output: </b>Exists)</para>
    /// <para><i>Delete</i> (<b>Required: </b> ServiceName <b>Optional: </b>MachineName)</para>
    /// <para><i>Disable</i> (<b>Required: </b> ServiceName <b>Optional: </b>MachineName)</para>
    /// <para><i>Install</i> (<b>Required: </b> ServiceName, ServicePath, User<b>Optional: </b>Force, StartupType, CommandLineArguments, Description, ServiceDependencies, ServiceDisplayName, MachineName, RemoteUser, RemoteUserPassword)</para>
    /// <para><i>Restart</i> (<b>Required: </b> ServiceName <b>Optional: </b>MachineName). Any running directly dependent services will be restarted too.</para>
    /// <para><i>SetAutomatic</i> (<b>Required: </b> ServiceName <b>Optional: </b>MachineName)</para>
    /// <para><i>SetManual</i> (<b>Required: </b> ServiceName <b>Optional: </b>MachineName)</para>
    /// <para><i>Start</i> (<b>Required: </b> ServiceName or Services <b>Optional: </b>MachineName, RetryAttempts)</para>
    /// <para><i>Stop</i> (<b>Required: </b> ServiceName or Services <b>Optional: </b>MachineName, RetryAttempts)</para>
    /// <para><i>Uninstall</i> (<b>Required: </b> ServicePath <b>Optional: </b>MachineName, RemoteUser, RemoteUserPassword)</para>
    /// <para><i>UpdateIdentity</i> (<b>Required: </b> ServiceName, User, Password <b>Optional: </b>MachineName)</para>
    /// <para><b>Remote Execution Support:</b> Yes</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///         <User>serviceAcct</User>
    ///         <Password>P2ssw0rd</Password>
    ///         <RemoteMachine>VSTS2008</RemoteMachine>
    ///         <RemoteUser>vsts2008\tfssetup</RemoteUser>
    ///         <RemoteUserPassword>1Setuptfs</RemoteUserPassword>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- check whether a service exists (this should return true in most cases) -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="CheckExists" ServiceName="Schedule">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.WindowsService>
    ///         <Message Text="Schedule service exists: $(DoesExist)"/>
    ///         <!-- check whether another service exists (this should return false)-->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="CheckExists" ServiceName="ThisServiceShouldNotExist">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.WindowsService>
    ///         <Message Text="ThisServiceShouldNotExist service exists: $(DoesExist)"/>
    ///         <!-- Check whether a service exists on a Remote Machine(this should return true in most cases) -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="CheckExists" ServiceName="Schedule" RemoteUser="$(RemoteUser)" RemoteUserPassword="$(RemoteUserPassword)" MachineName="$(RemoteMachine)">
    ///             <Output TaskParameter="Exists" PropertyName="DoesExist"/>
    ///         </MSBuild.ExtensionPack.Computer.WindowsService>
    ///         <Message Text="Schedule service exists on '$(RemoteMachine)': $(DoesExist)"/>
    ///         <!-- Start a service -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="Start" ServiceName="MSSQLSERVER"/>
    ///         <!-- Start a service on a Remote Machine -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="Start" ServiceName="BITS" RemoteUser="$(RemoteUser)" RemoteUserPassword="$(RemoteUserPassword)" MachineName="$(RemoteMachine)" />
    ///         <!-- Stop a service -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="Stop" ServiceName="MSSQLSERVER"/>
    ///         <!-- Stop a service on a Remote Machine -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="Stop" ServiceName="BITS" RemoteUser="$(RemoteUser)" RemoteUserPassword="$(RemoteUserPassword)" MachineName="$(RemoteMachine)"/>
    ///         <!-- Uninstall a service on the Local Machine -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="Uninstall" ServiceName="__TestService1" ServicePath="c:\WINDOWS\system32\taskmgr.exe" />
    ///         <!-- Uninstall a service on a Remote Machine -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="Uninstall" ServiceName="__TestService1" ServicePath="c:\WINDOWS\system32\taskmgr.exe" RemoteUser="$(RemoteUser)" RemoteUserPassword="$(RemoteUserPassword)" MachineName="$(RemoteMachine)" />
    ///         <!-- Install a service on the Local machine -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="Install" ServiceName="__TestService1" User="$(User)" Password="$(password)" ServicePath="c:\WINDOWS\system32\taskmgr.exe" />
    ///         <!-- Install a service on a Remote Machine -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="Install" ServiceName="__TestService1" User="$(User)" Password="$(password)" ServicePath="c:\WINDOWS\system32\taskmgr.exe" RemoteUser="$(RemoteUser)" RemoteUserPassword="$(RemoteUserPassword)" MachineName="$(RemoteMachine)" />
    ///         <!-- Disable a service -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="Disable" ServiceName="__TestService1"/>
    ///         <!-- Disable a service on a Remote Machine -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="Disable" ServiceName="__TestService1" RemoteUser="$(RemoteUser)" RemoteUserPassword="$(RemoteUserPassword)" MachineName="$(RemoteMachine)"/>
    ///         <!-- Set a service to start automatically on system startup-->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="SetAutomatic" ServiceName="__TestService1"/>
    ///         <!-- Set a service to start automatically on system startup on a Remote Machine -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="SetAutomatic" ServiceName="__TestService1" RemoteUser="$(RemoteUser)" RemoteUserPassword="$(RemoteUserPassword)" MachineName="$(RemoteMachine)"/>
    ///         <!-- Set a service to start manually -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="SetManual" ServiceName="__TestService1"/>
    ///         <!-- Set a service to start manually on a Remote Machine -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="SetManual" ServiceName="__TestService1" RemoteUser="$(RemoteUser)" RemoteUserPassword="$(RemoteUserPassword)" MachineName="$(RemoteMachine)"/>
    ///         <!-- Update the Identity that the service runs in -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="UpdateIdentity" ServiceName="__TestService1" User="$(User)" Password="$(Password)"/>
    ///         <!-- Update the Identity that the service on a Remote Machine runs in -->
    ///         <MSBuild.ExtensionPack.Computer.WindowsService TaskAction="UpdateIdentity" ServiceName="__TestService1" User="$(User)" Password="$(Password)" RemoteUser="$(RemoteUser)" RemoteUserPassword="$(RemoteUserPassword)" MachineName="$(RemoteMachine)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class WindowsService : BaseTask
    {
        private const string CheckExistsTaskAction = "CheckExists";
        private const string DeleteTaskAction = "Delete";
        private const string DisableTaskAction = "Disable";
        private const string InstallTaskAction = "Install";
        private const string RestartTaskAction = "Restart";
        private const string SetAutomaticTaskAction = "SetAutomatic";
        private const string SetManualTaskAction = "SetManual";
        private const string StartTaskAction = "Start";
        private const string StopTaskAction = "Stop";
        private const string UninstallTaskAction = "Uninstall";
        private const string UpdateIdentityTaskAction = "UpdateIdentity";
        private const string StartupTypeAutomatic = "Automatic";
        private const string StartupTypeAutomaticDelayed = "AutomaticDelayedStart";
        private const string StartupTypeDisabled = "Disabled";
        private const string StartupTypeManual  = "Manual";
        private const bool RemoteExecutionAvailable = true;
        private int retryAttempts = 60;

        /// <summary>
        /// Sets the number of times to attempt Starting / Stopping a service. Default is 60.
        /// </summary>
        public int RetryAttempts
        {
            get { return this.retryAttempts; }
            set { this.retryAttempts = value; }
        }

        /// <summary>
        /// Gets whether the service exists
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Sets the user.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// The Name of the service. Note, this is the 'Service Name' as displayed in services.msc, NOT the 'Display Name'
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// The Display Name of the service. Defaults to ServiceName.
        /// </summary>
        public string ServiceDisplayName { get; set; }

        /// <summary>
        /// Sets the path of the service executable
        /// </summary>
        public ITaskItem ServicePath { get; set; }

        /// <summary>
        /// Sets user password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Sets a value indicating whether to delete a service if it already exists when calling Install
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Sets the service description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Sets the user to impersonate on remote server.
        /// </summary>
        public string RemoteUser { get; set; }

        /// <summary>
        /// Sets the password for the user to impersonate on remote server.
        /// </summary>
        public string RemoteUserPassword { get; set; }

        /// <summary>
        /// Sets the services upon which the installed service depends.
        /// </summary>
        public ITaskItem[] ServiceDependencies { get; set; }

        /// <summary>
        /// Sets the command line arguments to be passed to the service.
        /// </summary>
        public string CommandLineArguments { get; set; }

        /// <summary>
        /// Sets the Startup Type of the service. 
        /// </summary>
        public string StartupType { get; set; }

        /// <summary>
        /// Sets the collection of Services to target in parallel. See TaskAction parameters for which TaskActions support this.
        /// </summary>
        public ITaskItem[] Services { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine(RemoteExecutionAvailable) && (string.IsNullOrEmpty(this.RemoteUser) || string.IsNullOrEmpty(this.RemoteUserPassword)))
            {
                this.LogTaskMessage(MessageImportance.Low, "No RemoteUser or RemoteUserPassword supplied. Attempting Integrated Security.");
            }

            if (this.Services == null)
            {
                if (string.IsNullOrEmpty(this.ServiceDisplayName))
                {
                    this.ServiceDisplayName = this.ServiceName;
                }

                if (this.ServiceDoesExist(this.ServiceName) == false && this.TaskAction != InstallTaskAction && this.TaskAction != CheckExistsTaskAction && this.TaskAction != UninstallTaskAction && this.TaskAction != DeleteTaskAction)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Service does not exist: {0}", this.ServiceDisplayName));
                    return;
                }
            }

            switch (this.TaskAction)
            {
                case InstallTaskAction:
                    this.Install();
                    break;
                case DeleteTaskAction:
                    this.DeleteService();
                    break;
                case UninstallTaskAction:
                    this.Uninstall();
                    break;
                case StopTaskAction:
                    this.Stop();
                    break;
                case StartTaskAction:
                    this.Start();
                    break;
                case RestartTaskAction:
                    this.Restart();
                    break;
                case DisableTaskAction:
                    this.SetStartupType(StartupTypeDisabled);
                    break;
                case SetManualTaskAction:
                    this.SetStartupType(StartupTypeManual);
                    break;
                case SetAutomaticTaskAction:
                    this.SetStartupType(StartupTypeAutomatic);
                    break;
                case CheckExistsTaskAction:
                    this.CheckExists(this.ServiceName, false);
                    break;
                case UpdateIdentityTaskAction:
                    this.UpdateIdentity();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private static string GetServiceStartupType(string startupType)
        {
            if (string.IsNullOrEmpty(startupType) || (string.Compare(startupType, StartupTypeAutomaticDelayed, StringComparison.CurrentCultureIgnoreCase) == 0))
            {
                return StartupTypeAutomatic;
            }

            return startupType;
        }

        private void Restart()
        {
            this.LogTaskMessage(MessageImportance.High, string.Format(CultureInfo.CurrentCulture, "Restarting: {0} on {1}", this.ServiceName, this.MachineName));
            using (ServiceController sc = new ServiceController(this.ServiceName, this.MachineName))
            {
                List<ServiceController> runningDependencies = sc.DependentServices.Where(s => s.Status == ServiceControllerStatus.Running).ToList();

                if (!sc.Status.Equals(ServiceControllerStatus.Stopped) && !sc.Status.Equals(ServiceControllerStatus.StopPending))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "...Stopping: {0}", this.ServiceName));
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "...Starting: {0}", this.ServiceName));
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running);

                    foreach (ServiceController s in runningDependencies)
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "...Starting dependent service: {0}", s.ServiceName));
                        s.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running);
                    }
                }
                else
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Service is stopped: {0}", this.ServiceName));
                }
            }
        }

        private void UpdateIdentity()
        {
            bool targetLocal = this.TargetingLocalMachine(RemoteExecutionAvailable);
            string userName = this.User;
            if (userName.IndexOf('\\') < 0)
            {
                userName = ".\\" + userName;
            }

            if (this.ServiceDoesExist(this.ServiceName))
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Updating Identity: {0} on '{1}' to '{2}'", this.ServiceDisplayName, this.MachineName, userName));

                ManagementObject wmi = this.RetrieveManagementObject(this.ServiceName, targetLocal);

                object[] paramList = new object[] { null, null, null, null, null, null, userName, this.Password };
                object result = wmi.InvokeMethod("Change", paramList);
                int returnCode = Convert.ToInt32(result, CultureInfo.InvariantCulture);
                if ((ServiceReturnCode)returnCode != ServiceReturnCode.Success)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Error changing service identity of {0} on '{1}' to '{2}'", this.ServiceDisplayName, this.MachineName, userName));
                }
            }
            else
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Service: {0} does not exist on: {1}.", this.ServiceDisplayName, this.MachineName));
            }
        }

        private void CheckExists(string serviceName, bool overrideSDN)
        {
            string displayName = this.ServiceDisplayName;

            if (overrideSDN)
            {
                displayName = serviceName;
            } 
            
            if (this.ServiceDoesExist(serviceName))
            {
                this.Exists = true;
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Service: {0} exists on: {1}.", displayName, this.MachineName));
            }
            else
            {
                this.Exists = false;
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Service: {0} does not exist on: {1}.", displayName, this.MachineName));
            }
        }

        private void SetStartupType(string startup)
        {
            bool targetLocal = this.TargetingLocalMachine(RemoteExecutionAvailable);
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Setting StartUp Type to {0} for {1} on '{2}'.", startup, this.ServiceDisplayName, this.MachineName));
            try
            {
                ManagementObject wmi = this.RetrieveManagementObject(this.ServiceName, targetLocal);

                object[] paramList = new object[] { startup };
                object result = wmi.InvokeMethod("ChangeStartMode", paramList);
                int returnCode = Convert.ToInt32(result, CultureInfo.InvariantCulture);
                if ((ServiceReturnCode)returnCode != ServiceReturnCode.Success)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "SetStartupType [{2}] failed with return code '[{0}] {1}'", returnCode, (ServiceReturnCode)returnCode, startup));
                }
            }
            catch (Exception ex)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "SetStartupType [{0}] failed with error '{1}'", startup, ex.Message));
                throw;
            }
        }

        private ServiceStartMode GetServiceStartMode(string serviceName)
        {
            ServiceStartMode toReturn = ServiceStartMode.Manual;
            bool targetLocal = this.TargetingLocalMachine(RemoteExecutionAvailable);
            try
            {
                ManagementObject wmi = this.RetrieveManagementObject(serviceName, targetLocal);

                string startMode = wmi.Properties["StartMode"].Value.ToString().Trim();
                switch (startMode)
                {
                    case "Auto":
                        toReturn = ServiceStartMode.Automatic;
                        break;
                    case "Boot":
                        toReturn = ServiceStartMode.Boot;
                        break;
                    case "Disabled":
                        toReturn = ServiceStartMode.Disabled;
                        break;
                    case "Manual":
                        toReturn = ServiceStartMode.Manual;
                        break;
                    case "System":
                        toReturn = ServiceStartMode.System;
                        break;
                }
            }
            catch (Exception ex)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "An error occurred in GetServiceStartMode of {0} on '{1}'.  Message: {2}", serviceName, this.MachineName, ex.Message));
                throw;
            }

            return toReturn;
        }

        private void Start()
        {
            if (this.Services == null)
            {
                this.StartLogic(this.ServiceName, false);
                return;
            }

            System.Threading.Tasks.Parallel.ForEach(this.Services, service => this.StartLogic(service.ItemSpec, true));
        }

        private void StartLogic(string serviceName, bool overrideSDN)
        {
            string displayName = this.ServiceDisplayName;

            if (overrideSDN)
            {
                displayName = serviceName;
            }

            // If the Service is disabled then we will just error out 60 times inside StartService() so we will
            // short-circuit the errors and let the user know.
            // Possible enhancement [SStJean]:  Add ForceStart property to Task and change StartMode to Manual instead of throwing error.
            if (this.GetServiceStartMode(serviceName) == ServiceStartMode.Disabled)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Cannot start service '{0}' on '{1}': Service is Disabled", displayName, this.MachineName));
                return;
            }

            int i = 1;
            while (i <= this.RetryAttempts)
            {
                ServiceState state = this.GetServiceState(serviceName, overrideSDN);
                switch (state)
                {
                        // We can't do anything when Service is in these states, so we log, count, pause and loop.
                    case ServiceState.ContinuePending:
                    case ServiceState.PausePending:
                    case ServiceState.StartPending:
                    case ServiceState.StopPending:
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Please wait, Service state: {0} on '{1}' - {2}...", displayName, this.MachineName, state));
                        ++i;
                        break;
                    case ServiceState.Paused:
                    case ServiceState.Stopped:
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Starting: {0} on '{1}' - {2}...", displayName, this.MachineName, state));
                        this.StartService(serviceName, overrideSDN);
                        ++i;
                        break;
                    case ServiceState.Running:
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Started: {0}", displayName));
                        return;
                }

                if (i == this.RetryAttempts)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Could not start: {0}", displayName));
                    return;
                }

                System.Threading.Thread.Sleep(2000);
            }
        }

        private void StartService(string serviceName, bool overrideSDN)
        {
            string displayName = this.ServiceDisplayName;

            if (overrideSDN)
            {
                displayName = serviceName;
            } 
            
            bool targetLocal = this.TargetingLocalMachine(RemoteExecutionAvailable);
            try
            {
                ManagementObject wmi = this.RetrieveManagementObject(serviceName, targetLocal);

                object[] paramList = new object[] { };
                object result = wmi.InvokeMethod("StartService", paramList);
                int returnCode = Convert.ToInt32(result, CultureInfo.InvariantCulture);
                if ((ServiceReturnCode)returnCode != ServiceReturnCode.Success)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Start Service failed with return code '[{0}] {1}'", returnCode, (ServiceReturnCode)returnCode));
                }
            }
            catch (Exception ex)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Start Service [{0} on {1}] failed with error '{2}'", displayName, this.MachineName, ex.Message));
                throw;
            }
        }
        
        private bool Stop()
        {
            if (this.Services == null)
            {
                return this.StopLogic(this.ServiceName, false);
            }
            
            System.Threading.Tasks.Parallel.ForEach(this.Services, service => this.StopLogic(service.ItemSpec, true));

            return true;
        }

        private bool StopLogic(string serviceName, bool overrideSDN)
        {
            if (!this.ServiceDoesExist(serviceName))
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Service not found: {0} on '{1}'", serviceName, this.MachineName));
                return true;
            }

            string displayName = this.ServiceDisplayName;

            if (overrideSDN)
            {
                displayName = serviceName;
            }
            
            try
            {
                int i = 1;
                while (i <= this.RetryAttempts)
                {
                    ServiceState state = this.GetServiceState(serviceName, overrideSDN);
                    switch (state)
                    {
                            // We can't do anything when Service is in these states, so we log, count, pause and loop.
                        case ServiceState.ContinuePending:
                        case ServiceState.PausePending:
                        case ServiceState.StartPending:
                        case ServiceState.StopPending:
                            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Please wait, Service state: {0} on '{1}' - {2}...", displayName, this.MachineName, state));
                            ++i;
                            break;
                        case ServiceState.Paused:
                        case ServiceState.Running:
                            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Stopping: {0} on '{1}' - {2}...", displayName, this.MachineName, state));
                            if (!this.StopService(serviceName, overrideSDN))
                            {
                                return false;
                            }

                            ++i;
                            break;
                        case ServiceState.Stopped:
                            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Stopped: {0} on '{1}'", displayName, this.MachineName));
                            return true;
                    }

                    if (i == this.RetryAttempts)
                    {
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Could not stop: {0} on '{1}'", displayName, this.MachineName));
                        return false;
                    }

                    System.Threading.Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "{0}", ex.Message));
            }

            return true;
        }

        private bool StopService(string serviceName, bool overrideSDN)
        {
            string displayName = this.ServiceDisplayName;

            if (overrideSDN)
            {
                displayName = serviceName;
            }
            
            bool targetLocal = this.TargetingLocalMachine(RemoteExecutionAvailable);
            bool noErrors = true;
            try
            {
                ManagementObject wmi = this.RetrieveManagementObject(serviceName, targetLocal);

                object[] paramList = new object[] { };

                // Execute the method and obtain the return values.
                object result = wmi.InvokeMethod("StopService", paramList);
                int returnCode = Convert.ToInt32(result, CultureInfo.InvariantCulture);
                if ((ServiceReturnCode)returnCode != ServiceReturnCode.Success)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Stop Service failed with return code '[{0}] {1}'", returnCode, (ServiceReturnCode)returnCode));
                    noErrors = false;
                }
            }
            catch (Exception ex)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Stop Service [{0}on {1}] failed with error '{2}'", displayName, this.MachineName, ex.Message));
                throw;
            }

            return noErrors;
        }

        private ManagementObject RetrieveManagementObject(string service, bool targetLocal)
        {
            string path = targetLocal ? "\\root\\CIMV2" : string.Format(CultureInfo.InvariantCulture, "\\\\{0}\\root\\CIMV2", this.MachineName);

            string servicePath = string.Format(CultureInfo.InvariantCulture, "Win32_Service.Name='{0}'", service);
            ManagementObject wmiReturnObject;
            using (ManagementObject wmi = new ManagementObject(path, servicePath, null))
            {
                if (!targetLocal)
                {
                    wmi.Scope.Options.Username = this.RemoteUser;
                    wmi.Scope.Options.Password = this.RemoteUserPassword;
                }

                wmiReturnObject = wmi;
            }

            return wmiReturnObject;
        }

        private ServiceState GetServiceState(string serviceName, bool overrideSDN)
        {
            string displayName = this.ServiceDisplayName;

            if (overrideSDN)
            {
                displayName = serviceName;
            }

            ServiceState toReturn = ServiceState.Stopped;
            bool targetLocal = this.TargetingLocalMachine(RemoteExecutionAvailable);
            try
            {
                ManagementObject wmi = this.RetrieveManagementObject(serviceName, targetLocal);

                string state = wmi.Properties["State"].Value.ToString().Trim();
                switch (state)
                {
                    case "Running":
                        toReturn = ServiceState.Running;
                        break;
                    case "Stopped":
                        toReturn = ServiceState.Stopped;
                        break;
                    case "Paused":
                        toReturn = ServiceState.Paused;
                        break;
                    case "Start Pending":
                        toReturn = ServiceState.StartPending;
                        break;
                    case "Stop Pending":
                        toReturn = ServiceState.StopPending;
                        break;
                    case "Continue Pending":
                        toReturn = ServiceState.ContinuePending;
                        break;
                    case "Pause Pending":
                        toReturn = ServiceState.PausePending;
                        break;
                }
            }
            catch (Exception ex)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "An error occurred in GetState of {0} on '{1}'.  Message: {2}", displayName, this.MachineName, ex.Message));
                throw;
            }

            return toReturn;
        }

        private bool ServiceDoesExist(string serviceName)
        {
            bool targetLocal = this.TargetingLocalMachine(RemoteExecutionAvailable);

            ManagementObject wmi = this.RetrieveManagementObject(serviceName, targetLocal);

            try
            {
                wmi.InvokeMethod("InterrogateService", null);
                return true;
            }
            catch (ManagementException ex)
            {
                if (ex.ErrorCode == ManagementStatus.NotFound)
                {
                    return false;
                }

                throw;
            }
        }

        private void Install()
        {
            bool targetLocal = this.TargetingLocalMachine(RemoteExecutionAvailable);

            // check to see if the exe path has been provided
            if (this.ServicePath == null)
            {
                this.Log.LogError("ServicePath was not provided.");
                return;
            }

            if (string.IsNullOrEmpty(this.User))
            {
                this.Log.LogError("User was not provided.");
                return;
            }

            if (string.IsNullOrEmpty(this.ServiceName))
            {
                this.Log.LogError("ServiceName was not provided.");
                return;
            }

            if (this.ServiceDoesExist(this.ServiceName) && !this.Force)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Install Service failed with code: '{0}'", ServiceReturnCode.StatusServiceExists));
                return;
            }
            
            if (this.ServiceDoesExist(this.ServiceName) && this.Force)
            {
                if (!this.DeleteService())
                {
                    return;
                }
            }
            
            // check to see if the correct path has been provided
            if (targetLocal && (System.IO.File.Exists(this.ServicePath.GetMetadata("FullPath")) == false))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "ServicePath does not exist: {0}", this.ServicePath));
                return;
            }

            var serviceDependencies = new List<string>();
            if (this.ServiceDependencies != null)
            {
                serviceDependencies.AddRange(this.ServiceDependencies.Select(dep => dep.ItemSpec));
            }

            string serviceStartupType = GetServiceStartupType(this.StartupType);
            ServiceReturnCode ret = this.Install(this.MachineName, this.ServiceName, this.ServiceDisplayName, this.ServicePath.ToString(), serviceStartupType, this.User, this.Password, serviceDependencies.ToArray(), false, this.RemoteUser, this.RemoteUserPassword);
            if (ret != ServiceReturnCode.Success)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Install Service failed with code: '{0}'", ret));
            }
            else
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Install Service succeeded for '{0}' on '{1}'", this.ServiceDisplayName, this.MachineName));
                if (!string.IsNullOrEmpty(this.Description))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "...Setting Description for '{0}' on '{1}'", this.ServiceDisplayName, this.MachineName));
                    using (RegistryKey registryKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, this.MachineName, RegistryView.Registry32))
                    {
                        RegistryKey subKey = registryKey.OpenSubKey(@"System\CurrentControlSet\Services\" + this.ServiceName, true);
                        if (subKey != null)
                        {
                            subKey.SetValue("Description", this.Description);
                            subKey.Close();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(this.CommandLineArguments))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "...Setting command line arguments for '{0}' on '{1}'", this.ServiceDisplayName, this.MachineName));
                    using (RegistryKey registryKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, this.MachineName, RegistryView.Registry32))
                    {
                        RegistryKey subKey = registryKey.OpenSubKey(@"System\CurrentControlSet\Services\" + this.ServiceName, true);
                        if (subKey != null)
                        {
                            object imagePathValue = subKey.GetValue("ImagePath");
                            imagePathValue = imagePathValue + " " + this.CommandLineArguments;
                            subKey.SetValue("ImagePath", imagePathValue, RegistryValueKind.ExpandString);
                        }
                    }
                }

                if (string.Compare(this.StartupType, StartupTypeAutomaticDelayed, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "...Setting delayed start argument registry setting for '{0}' on '{1}'", this.ServiceDisplayName, this.MachineName));
                    using (RegistryKey registryKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, this.MachineName, RegistryView.Registry32))
                    {
                        RegistryKey subKey = registryKey.OpenSubKey(@"System\CurrentControlSet\Services\" + this.ServiceName, true);
                        if (subKey != null)
                        {
                            const uint DelayedAutoStart = 1;
                            subKey.SetValue("DelayedAutostart", DelayedAutoStart, RegistryValueKind.DWord);
                        }
                    }
                }
            }
        }

        private bool DeleteService()
        {
            bool targetLocal = this.TargetingLocalMachine(RemoteExecutionAvailable);
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Attempting to Delete the '{0}' service on '{1}' machine", this.ServiceName, this.MachineName));
            if (!this.ServiceDoesExist(this.ServiceName))
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Service does not exist: {0} on '{1}'", this.ServiceDisplayName, this.MachineName));
                return true;
            }

            bool noErrors = true;
            try
            {
                ManagementObject wmi = this.RetrieveManagementObject(this.ServiceName, targetLocal);
                
                // Execute the method and obtain the return values.
                ManagementBaseObject result = wmi.InvokeMethod("delete", null, null);
                if (result != null)
                {
                    int returnCode = Convert.ToInt32(result["ReturnValue"], CultureInfo.InvariantCulture);
                    if ((ServiceReturnCode)returnCode != ServiceReturnCode.Success)
                    {
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Delete Service failed with return code '[{0}] {1}'", returnCode, (ServiceReturnCode)returnCode));
                        noErrors = false;
                    }
                }
            }
            catch (Exception ex)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Delete Service [{0}on {1}] failed with error '{2}'", this.ServiceDisplayName, this.MachineName, ex.Message));
                throw;
            }

            return noErrors;
        }

        private ServiceReturnCode Install(string machineName, string name, string displayName, string physicalLocation, string startupType, string userName, string password, string[] dependencies, bool interactWithDesktop, string installingUser, string installingUserPassword)
        {
            bool targetLocal = this.TargetingLocalMachine(RemoteExecutionAvailable);

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Attempting to install the '{0}' service to the '{1}' machine", displayName, machineName));
            if (userName.IndexOf('\\') < 0)
            {
                userName = ".\\" + userName;
            }

            try
            {
                string path = targetLocal ? "\\root\\CIMV2" : string.Format(CultureInfo.InvariantCulture, "\\\\{0}\\root\\CIMV2", machineName);

                using (ManagementClass wmi = new ManagementClass(path, "Win32_Service", null))
                {
                    if (!targetLocal)
                    {
                        wmi.Scope.Options.Username = installingUser;
                        wmi.Scope.Options.Password = installingUserPassword;
                    }

                    object[] paramList = new object[]
                                             {
                                                 name,
                                                 displayName,
                                                 physicalLocation,
                                                 Convert.ToInt32(ServiceTypes.OwnProcess, CultureInfo.InvariantCulture),
                                                 Convert.ToInt32(ServiceErrorControl.UserNotified, CultureInfo.InvariantCulture),
                                                 startupType,
                                                 interactWithDesktop,
                                                 userName,
                                                 password,
                                                 null,
                                                 null,
                                                 dependencies
                                             };

                    // Execute the method and obtain the return values.
                    object result = wmi.InvokeMethod("Create", paramList);
                    int returnCode = Convert.ToInt32(result, CultureInfo.InvariantCulture);
                    return (ServiceReturnCode)returnCode;
                }
            }
            catch (Exception ex)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Install Service [{0} on {1}] failed with error '{2}'", this.ServiceDisplayName, this.MachineName, ex.Message));
                return ServiceReturnCode.UnknownFailure;
            }
        }

        private void Uninstall()
        {
            bool targetLocal = this.TargetingLocalMachine(RemoteExecutionAvailable);

            if (!this.ServiceDoesExist(this.ServiceName))
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Service does not exist: {0} on '{1}'", this.ServiceDisplayName, this.MachineName));
                return;
            }

            if (this.Stop())
            {
                try
                {
                    ManagementObject wmi = this.RetrieveManagementObject(this.ServiceName, targetLocal);

                    object[] paramList = new object[] { };
                    object result = wmi.InvokeMethod("Delete", paramList);
                    ServiceReturnCode returnCode = (ServiceReturnCode)Convert.ToInt32(result, CultureInfo.InvariantCulture);
                    if (returnCode != ServiceReturnCode.Success)
                    {
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Uninstall Service failed with code: '{0}'", returnCode));
                    }
                    else
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Uninstall Service succeeded for '{0}' on '{1}'", this.ServiceDisplayName, this.MachineName));
                    }
                }
                catch (Exception ex)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Uninstall Service [{0} on {1}] failed with error '{2}'", this.ServiceDisplayName, this.MachineName, ex.Message));
                }
            }
        }
    }
}
