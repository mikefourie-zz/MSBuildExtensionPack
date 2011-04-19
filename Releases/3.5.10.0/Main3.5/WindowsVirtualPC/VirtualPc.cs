//-----------------------------------------------------------------------
// <copyright file="VirtualPc.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Virtualisation
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.Threading;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.VirtualPC.Interop;

    /// <summary>
    /// Provides various tasks to work with Windows Virtual Pc (Requires Windows 7)
    /// <para/>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddHardDiskConnection</i> (<b>Required: </b>Name, FileName, DeviceNumber, BusNumber)</para>
    /// <para><i>ClickMouse</i> (<b>Required: </b>Name)</para>
    /// <para><i>DiscardSavedState</i> (<b>Required: </b>Name)</para>
    /// <para><i>DiscardUndoDisks</i> (<b>Required: </b>Name)</para>
    /// <para><i>IsHeartBeating</i> (<b>Required: </b>Name <b>Output: </b>Result)</para>
    /// <para><i>IsScreenLocked</i> (<b>Required: </b>Name <b>Output: </b>Result)</para>
    /// <para><i>List</i> (<b>Required: </b>Name <b>Output: </b>VirtualMachines)</para>
    /// <para><i>Logoff</i> (<b>Required: </b>Name <b>Optional: </b>WaitForCompletion)</para>
    /// <para><i>MergeUndoDisks</i> (<b>Required: </b>Name <b>Optional: </b>WaitForCompletion)</para>
    /// <para><i>Pause</i> (<b>Required: </b>Name)</para>
    /// <para><i>RemoveHardDiskConnection</i> (<b>Required: </b>Name, FileName)</para>
    /// <para><i>Reset</i> (<b>Required: </b>Name <b>Optional: </b>WaitForCompletion)</para>
    /// <para><i>Restart</i> (<b>Required: </b>Name <b>Optional: </b>WaitForCompletion)</para>
    /// <para><i>Resume</i> (<b>Required: </b>Name)</para>
    /// <para><i>Save</i> (<b>Required: </b>Name <b>Optional: </b>WaitForCompletion)</para>
    /// <para><i>Shutdown</i> (<b>Required: </b>Name <b>Optional: </b>WaitForCompletion)</para>
    /// <para><i>Startup</i> (<b>Required: </b>Name <b>Optional: </b>WaitForCompletion)</para>
    /// <para><i>TakeScreenshot</i> (<b>Required: </b>Name, FileName)</para>
    /// <para><i>TurnOff</i> (<b>Required: </b>Name <b>Optional: </b>WaitForCompletion)</para>
    /// <para><i>TypeAsciiText</i> (<b>Required: </b>Name, Text)</para>
    /// <para><i>TypeKeySequence</i> (<b>Required: </b>Name, Text)</para>
    /// <para><i>WaitForLowCpuUtilization</i> (<b>Required: </b>Name <b>Optional:</b> MaxCpuThreshold, MaxCpuUsage)</para>
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
    ///         <MSBuild.ExtensionPack.Virtualisation.VirtualPc TaskAction="List">
    ///             <Output ItemName="VMList" TaskParameter="VirtualMachines"/>
    ///         </MSBuild.ExtensionPack.Virtualisation.VirtualPc>
    ///         <Message Text="VM BaseBoardSerialNumber: %(VMList.BaseBoardSerialNumber)"/>
    ///         <Message Text="VM BIOSGUID: %(VMList.BIOSGUID)"/>
    ///         <Message Text="VM BIOSSerialNumber: %(VMList.BIOSSerialNumber)"/>
    ///         <Message Text="VM ChassisAssetTag: %(VMList.ChassisAssetTag)"/>
    ///         <Message Text="VM ChassisSerialNumber: %(VMList.ChassisSerialNumber)"/>
    ///         <Message Text="VM Memory: %(VMList.Memory)"/>
    ///         <Message Text="VM Name: %(VMList.Name)"/>
    ///         <Message Text="VM Notes: %(VMList.Notes)"/>
    ///         <Message Text="VM Undoable: %(VMList.Undoable)"/>
    ///         <Message Text="VM CanShutdown: %(VMList.CanShutdown)"/>
    ///         <Message Text="VM ComputerName: %(VMList.ComputerName)"/>
    ///         <Message Text="VM IntegrationComponentsVersion: %(VMList.IntegrationComponentsVersion)"/>
    ///         <Message Text="VM IsHeartbeating: %(VMList.IsHeartbeating)"/>
    ///         <Message Text="VM IsHostTimeSyncEnabled: %(VMList.IsHostTimeSyncEnabled)"/>
    ///         <Message Text="VM MultipleUserSessionsAllowed: %(VMList.MultipleUserSessionsAllowed)"/>
    ///         <Message Text="VM OSBuildNumber: %(VMList.OSBuildNumber)"/>
    ///         <Message Text="VM OSMajorVersion: %(VMList.OSMajorVersion)"/>
    ///         <Message Text="VM OSMinorVersion: %(VMList.OSMinorVersion)"/>
    ///         <Message Text="VM OSName: %(VMList.OSName)"/>
    ///         <Message Text="VM OSPlatformId: %(VMList.OSPlatformId)"/>
    ///         <Message Text="VM OSVersion: %(VMList.OSVersion)"/>
    ///         <Message Text="VM ScreenLocked: %(VMList.ScreenLocked)"/>
    ///         <Message Text="VM ServicePackMajor: %(VMList.ServicePackMajor)"/>
    ///         <Message Text="VM ServicePackMinor: %(VMList.ServicePackMinor)"/>
    ///         <Message Text="VM TerminalServerPort: %(VMList.TerminalServerPort)"/>
    ///         <Message Text="VM TerminalServicesInitialized: %(VMList.TerminalServicesInitialized)"/>
    ///         <MSBuild.ExtensionPack.Virtualisation.VirtualPc TaskAction="Startup" Name="Demo"/>
    ///         <MSBuild.ExtensionPack.Virtualisation.VirtualPc TaskAction="TakeScreenshot" Name="Demo" FileName="C:\demo.bmp"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.9.0/html/ebd4153c-2551-4a3b-e685-7447ecd35980.htm")]
    public class VirtualPc : BaseTask
    {
        private const string AddHardDiskConnectionTaskAction = "AddHardDiskConnection";
        private const string ClickMouseTaskAction = "ClickMouse";
        private const string DiscardSavedStateTaskAction = "DiscardSavedState";
        private const string DiscardUndoDisksTaskAction = "DiscardUndoDisks";
        private const string IsHeartBeatingTaskAction = "IsHeartBeating";
        private const string IsScreenLockedTaskAction = "IsScreenLocked";
        private const string ListTaskAction = "List";
        private const string LogoffTaskAction = "Logoff";
        private const string MergeUndoDisksTaskAction = "MergeUndoDisks";
        private const string PauseTaskAction = "Pause";
        private const string RemoveHardDiskConnectionTaskAction = "RemoveHardDiskConnection";
        private const string ResetTaskAction = "Reset";
        private const string RestartTaskAction = "Restart";
        private const string ResumeTaskAction = "Resume";
        private const string SaveTaskAction = "Save";
        private const string ShutdownTaskAction = "Shutdown";
        private const string StartupTaskAction = "Startup";
        private const string TakeScreenshotTaskAction = "TakeScreenshot";
        private const string TurnOffTaskAction = "TurnOff";
        private const string TypeAsciiTextTaskAction = "TypeAsciiText";
        private const string TypeKeySequenceTaskAction = "TypeKeySequence";
        private const string WaitForLowCpuUtilizationTaskAction = "WaitForLowCpuUtilization";

        private VMVirtualPC virtualPC;
        private VMVirtualMachine virtualMachine;
        private int maxCpuThreshold = 10;
        private int maxCpuUsage = 10;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(AddHardDiskConnectionTaskAction)]
        [DropdownValue(ClickMouseTaskAction)]
        [DropdownValue(DiscardSavedStateTaskAction)]
        [DropdownValue(DiscardUndoDisksTaskAction)]
        [DropdownValue(IsHeartBeatingTaskAction)]
        [DropdownValue(IsScreenLockedTaskAction)]
        [DropdownValue(ListTaskAction)]
        [DropdownValue(LogoffTaskAction)]
        [DropdownValue(MergeUndoDisksTaskAction)]
        [DropdownValue(PauseTaskAction)]
        [DropdownValue(RemoveHardDiskConnectionTaskAction)]
        [DropdownValue(ResetTaskAction)]
        [DropdownValue(RestartTaskAction)]
        [DropdownValue(ResumeTaskAction)]
        [DropdownValue(SaveTaskAction)]
        [DropdownValue(ShutdownTaskAction)]
        [DropdownValue(StartupTaskAction)]
        [DropdownValue(TakeScreenshotTaskAction)]
        [DropdownValue(TurnOffTaskAction)]
        [DropdownValue(TypeAsciiTextTaskAction)]
        [DropdownValue(TypeKeySequenceTaskAction)]
        [DropdownValue(WaitForLowCpuUtilizationTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the name of the VirtualPc
        /// </summary>
        [TaskAction(AddHardDiskConnectionTaskAction, true)]
        [TaskAction(ClickMouseTaskAction, true)]
        [TaskAction(DiscardSavedStateTaskAction, true)]
        [TaskAction(DiscardUndoDisksTaskAction, true)]
        [TaskAction(IsHeartBeatingTaskAction, true)]
        [TaskAction(IsScreenLockedTaskAction, true)]
        [TaskAction(LogoffTaskAction, true)]
        [TaskAction(MergeUndoDisksTaskAction, true)]
        [TaskAction(PauseTaskAction, true)]
        [TaskAction(RemoveHardDiskConnectionTaskAction, true)]
        [TaskAction(ResetTaskAction, true)]
        [TaskAction(RestartTaskAction, true)]
        [TaskAction(ResumeTaskAction, true)]
        [TaskAction(SaveTaskAction, true)]
        [TaskAction(ShutdownTaskAction, true)]
        [TaskAction(StartupTaskAction, true)]
        [TaskAction(TakeScreenshotTaskAction, true)]
        [TaskAction(TurnOffTaskAction, true)]
        [TaskAction(TypeAsciiTextTaskAction, true)]
        [TaskAction(TypeKeySequenceTaskAction, true)]
        [TaskAction(WaitForLowCpuUtilizationTaskAction, true)]
        public string Name { get; set; }

        /// <summary>
        /// Gets the collection of Virtual Machines. See the sample for available metadata
        /// </summary>
        [Output]
        public ITaskItem[] VirtualMachines { get; set; }

        /// <summary>
        /// Sets the Text collection
        /// </summary>
        [TaskAction(TypeAsciiTextTaskAction, true)]
        [TaskAction(TypeKeySequenceTaskAction, true)]
        public ITaskItem[] Text { get; set; }

        /// <summary>
        /// Sets the MaxCpuUsage in %. Default is 10
        /// </summary>
        [TaskAction(WaitForLowCpuUtilizationTaskAction, true)]
        public int MaxCpuUsage
        {
            get { return this.maxCpuUsage; }
            set { this.maxCpuUsage = value; }
        }

        /// <summary>
        /// Sets the MaxCpuThreshold in seconds. This is the period for which the virtual machine must be below the MaxCpuUsage. Default is 10.
        /// </summary>
        [TaskAction(WaitForLowCpuUtilizationTaskAction, true)]
        public int MaxCpuThreshold
        {
            get { return this.maxCpuThreshold; }
            set { this.maxCpuThreshold = value; }
        }

        /// <summary>
        /// Gets the number of virtual machines found
        /// </summary>
        [Output]
        public int VirtualMachineCount { get; set; }

        /// <summary>
        /// Gets the Result
        /// </summary>
        [Output]
        public bool Result { get; set; }

        /// <summary>
        /// Sets the FileName
        /// </summary>
        [TaskAction(AddHardDiskConnectionTaskAction, true)]
        [TaskAction(RemoveHardDiskConnectionTaskAction, true)]
        [TaskAction(TakeScreenshotTaskAction, true)]
        public ITaskItem FileName { get; set; }

        /// <summary>
        /// Sets the device to which the drive will be attached. 0 = The drive will be attached to the first device on the bus. 1 = The drive will be attached to the second device on the bus.
        /// </summary>
        [TaskAction(AddHardDiskConnectionTaskAction, true)]
        public int DeviceNumber { get; set; }

        /// <summary>
        /// Sets the bus to which the drive will be attached. 0 = The drive will be attached to the first bus. 1 = The drive will be attached to the second bus.
        /// </summary>
        [TaskAction(AddHardDiskConnectionTaskAction, true)]
        public int BusNumber { get; set; }

        /// <summary>
        /// The time, in milliseconds, that this method will wait for task completion before returning control to the caller. A value of -1 specifies that method will wait until the task completes without timing out. Other valid timeout values range from 0 to 4,000,000 milliseconds.
        /// </summary>
        [TaskAction(LogoffTaskAction, false)]
        [TaskAction(MergeUndoDisksTaskAction, false)]
        [TaskAction(ResetTaskAction, false)]
        [TaskAction(RestartTaskAction, false)]
        [TaskAction(SaveTaskAction, false)]
        [TaskAction(ShutdownTaskAction, false)]
        [TaskAction(StartupTaskAction, false)]
        [TaskAction(TurnOffTaskAction, false)]
        public int WaitForCompletion { get; set; }

        /// <summary>
        /// InternalExecute
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            this.virtualPC = new VMVirtualPC();
            switch (this.TaskAction)
            {
                case ListTaskAction:
                    this.LogTaskMessage(MessageImportance.Low, "Listing Virtual Machines");
                    this.VirtualMachines = new ITaskItem[this.virtualPC.VirtualMachines.Count];
                    this.VirtualMachineCount = this.virtualPC.VirtualMachines.Count;
                    int i = 0;
                    foreach (VMVirtualMachine vm in this.virtualPC.VirtualMachines)
                    {
                        this.VirtualMachines[i] = GetVirtualMachineDetails(vm);
                        i++;
                    }

                    break;
                case AddHardDiskConnectionTaskAction:
                case DiscardSavedStateTaskAction:
                case DiscardUndoDisksTaskAction:
                case LogoffTaskAction:
                case MergeUndoDisksTaskAction:
                case PauseTaskAction:
                case RemoveHardDiskConnectionTaskAction:
                case ResetTaskAction:
                case RestartTaskAction:
                case ResumeTaskAction:
                case SaveTaskAction:
                case ShutdownTaskAction:
                case StartupTaskAction:
                case TurnOffTaskAction:
                    this.ConrolVM();
                    break;
                case IsScreenLockedTaskAction:
                    this.IsScreenLocked();
                    break;
                case IsHeartBeatingTaskAction:
                    this.IsHeartBeating();
                    break;
                case TypeAsciiTextTaskAction:
                case TypeKeySequenceTaskAction:
                    this.ManageKeyBoard();
                    break;
                case WaitForLowCpuUtilizationTaskAction:
                    this.WaitForLowCpuUtilization();
                    break;
                case TakeScreenshotTaskAction:
                    this.TakeScreenshot();
                    break;
                case ClickMouseTaskAction:
                    this.ClickMouse();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private static ITaskItem GetVirtualMachineDetails(IVMVirtualMachine virtualMachine)
        {
            ITaskItem newItem = new TaskItem(virtualMachine.Name);
            newItem.SetMetadata("BaseBoardSerialNumber", virtualMachine.BaseBoardSerialNumber);
            newItem.SetMetadata("BIOSGUID", virtualMachine.BIOSGUID);
            newItem.SetMetadata("BIOSSerialNumber", virtualMachine.BIOSSerialNumber);
            newItem.SetMetadata("ChassisAssetTag", virtualMachine.ChassisAssetTag);
            newItem.SetMetadata("ChassisSerialNumber", virtualMachine.ChassisSerialNumber);
            newItem.SetMetadata("Memory", virtualMachine.Memory.ToString(CultureInfo.InvariantCulture));
            newItem.SetMetadata("Name", virtualMachine.Name);
            newItem.SetMetadata("Notes", virtualMachine.Notes);
            newItem.SetMetadata("Undoable", virtualMachine.Undoable.ToString(CultureInfo.InvariantCulture));

            if (virtualMachine.State == VMVMState.vmVMState_Running)
            {
                newItem.SetMetadata("CanShutdown", virtualMachine.GuestOS.CanShutdown.ToString(CultureInfo.InvariantCulture));
                newItem.SetMetadata("ComputerName", virtualMachine.GuestOS.ComputerName);
                newItem.SetMetadata("IntegrationComponentsVersion", virtualMachine.GuestOS.IntegrationComponentsVersion);
                newItem.SetMetadata("IsHeartbeating", virtualMachine.GuestOS.IsHeartbeating.ToString());
                newItem.SetMetadata("IsHostTimeSyncEnabled", virtualMachine.GuestOS.IsHostTimeSyncEnabled.ToString());
                newItem.SetMetadata("MultipleUserSessionsAllowed", virtualMachine.GuestOS.MultipleUserSessionsAllowed.ToString());
                newItem.SetMetadata("OSBuildNumber", virtualMachine.GuestOS.OSBuildNumber);
                newItem.SetMetadata("OSMajorVersion", virtualMachine.GuestOS.OSMajorVersion);
                newItem.SetMetadata("OSMinorVersion", virtualMachine.GuestOS.OSMinorVersion);
                newItem.SetMetadata("OSName", virtualMachine.GuestOS.OSName);
                newItem.SetMetadata("OSPlatformId", virtualMachine.GuestOS.OSPlatformId);
                newItem.SetMetadata("OSVersion", virtualMachine.GuestOS.OSVersion);
                newItem.SetMetadata("ServicePackMajor", virtualMachine.GuestOS.ServicePackMajor);
                newItem.SetMetadata("ServicePackMinor", virtualMachine.GuestOS.ServicePackMinor);
                newItem.SetMetadata("TerminalServerPort", virtualMachine.GuestOS.TerminalServerPort.ToString(CultureInfo.InvariantCulture));
                newItem.SetMetadata("TerminalServicesInitialized", virtualMachine.GuestOS.TerminalServicesInitialized.ToString(CultureInfo.InvariantCulture));
                newItem.SetMetadata("UpTime", virtualMachine.Accountant.UpTime.ToString(CultureInfo.InvariantCulture));
            }

            return newItem;
        }

        private void ClickMouse()
        {
            if (!this.GetVirtualMachine())
            {
                return;
            }

            foreach (ITaskItem i in this.Text)
            {
                switch (i.ItemSpec)
                {
                    case "ClickLeft":
                        this.LogTaskMessage(MessageImportance.Low, "Left-click mouse");
                        this.virtualMachine.Mouse.Click(VMMouseButton.vmMouseButton_Left);
                        break;
                    case "ClickRight":
                        this.LogTaskMessage(MessageImportance.Low, "Right-click mouse");
                        this.virtualMachine.Mouse.Click(VMMouseButton.vmMouseButton_Right);
                        break;
                    case "ClickCenter":
                        this.LogTaskMessage(MessageImportance.Low, "Middle-click mouse");
                        this.virtualMachine.Mouse.Click(VMMouseButton.vmMouseButton_Center);
                        break;
                }
            }
        }

        private void TakeScreenshot()
        {
            if (!this.GetVirtualMachine())
            {
                return;
            }

            this.LogTaskMessage(String.Format(CultureInfo.CurrentCulture, "Taking screenshot of: {0}", this.Name));
            VMDisplay display = this.virtualMachine.Display;
            if (display != null)
            {
                object thumbnailObject = display.Thumbnail;
                object[] thumbnail = (object[])thumbnailObject;
                using (Bitmap bmp = new Bitmap(64, 48, PixelFormat.Format32bppRgb))
                {
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            uint pixel = (uint)thumbnail[(y * bmp.Width) + x];

                            int b = (int)((pixel & 0xff000000) >> 24);
                            int g = (int)((pixel & 0x00ff0000) >> 16);
                            int r = (int)((pixel & 0x0000ff00) >> 8);

                            bmp.SetPixel(x, y, Color.FromArgb(r, g, b));
                        }
                    }

                    bmp.Save(this.FileName.ItemSpec);
                }
            }
        }

        private bool GetVirtualMachine()
        {
            if (string.IsNullOrEmpty(this.Name))
            {
                this.Log.LogError("Name is required.");
                return false;
            }

            this.virtualMachine = this.virtualPC.FindVirtualMachine(this.Name);
            if (this.virtualMachine == null)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Virtual Machine: {0} not found", this.Name));
                return false;
            }

            return true;
        }

        private void WaitForLowCpuUtilization()
        {
            if (!this.GetVirtualMachine())
            {
                return;
            }

            if (this.virtualMachine.State != VMVMState.vmVMState_Running)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Virtual Machine: {0} is not running: {1}", this.Name, this.virtualMachine.State));
                return;
            }

            this.LogTaskMessage(String.Format(CultureInfo.CurrentCulture, "Waiting for low CPU utilisation on: {0}", this.Name));
            int belowMaxCount = 0;
            while (belowMaxCount < this.MaxCpuThreshold)
            {
                if (this.virtualMachine.Accountant.CPUUtilization < this.MaxCpuUsage)
                {
                    belowMaxCount++;
                }
                else
                {
                    belowMaxCount = 0;
                }

                Thread.Sleep(1000);
            }
        }

        private void ManageKeyBoard()
        {
            if (!this.GetVirtualMachine())
            {
                return;
            }

            this.LogTaskMessage(String.Format(CultureInfo.CurrentCulture, "{0} Virtual Machine: {1}", this.TaskAction, this.Name));
            switch (this.TaskAction)
            {
                case TypeAsciiTextTaskAction:
                    foreach (ITaskItem i in this.Text)
                    {
                        this.virtualMachine.Keyboard.TypeAsciiText(i.ItemSpec);
                    }

                    break;
                case TypeKeySequenceTaskAction:
                    foreach (ITaskItem i in this.Text)
                    {
                        this.virtualMachine.Keyboard.TypeKeySequence(i.ItemSpec);
                    }

                    break;
            }
        }

        private void IsHeartBeating()
        {
            if (!this.GetVirtualMachine())
            {
                return;
            }

            this.Result = this.virtualMachine.GuestOS.IsHeartbeating;
        }

        private void IsScreenLocked()
        {
            if (!this.GetVirtualMachine())
            {
                return;
            }

            this.Result = this.virtualMachine.GuestOS.ScreenLocked;
        }

        private void ConrolVM()
        {
            if (!this.GetVirtualMachine())
            {
                return;
            }

            this.LogTaskMessage(String.Format(CultureInfo.CurrentCulture, "{0} Virtual Machine: {1}", this.TaskAction, this.Name));
            switch (this.TaskAction)
            {
                case LogoffTaskAction:
                    if (this.virtualMachine.State != (VMVMState.vmVMState_TurnedOff | VMVMState.vmVMState_TurningOff))
                    {
                        var s = this.virtualMachine.GuestOS.Logoff();
                        if (this.WaitForCompletion > 0)
                        {
                            s.WaitForCompletion(this.WaitForCompletion);
                        }
                    }

                    break;
                case RestartTaskAction:
                    if (this.virtualMachine.State != (VMVMState.vmVMState_TurnedOff | VMVMState.vmVMState_TurningOff))
                    {
                        var s = this.virtualMachine.GuestOS.Restart(true);
                        if (this.WaitForCompletion > 0)
                        {
                            s.WaitForCompletion(this.WaitForCompletion);
                        }
                    }

                    break;
                case StartupTaskAction:
                    if (this.virtualMachine.State == VMVMState.vmVMState_TurnedOff)
                    {
                        var s = this.virtualMachine.Startup();
                        if (this.WaitForCompletion > 0)
                        {
                            s.WaitForCompletion(this.WaitForCompletion);
                        }
                    }

                    break;
                case TurnOffTaskAction:
                    if (this.virtualMachine.State != (VMVMState.vmVMState_TurnedOff | VMVMState.vmVMState_TurningOff))
                    {
                        var s = this.virtualMachine.TurnOff();
                        if (this.WaitForCompletion > 0)
                        {
                            s.WaitForCompletion(this.WaitForCompletion);
                        }
                    }

                    break;
                case ShutdownTaskAction:
                    if (this.virtualMachine.State != (VMVMState.vmVMState_TurnedOff | VMVMState.vmVMState_TurningOff))
                    {
                        var s = this.virtualMachine.GuestOS.Shutdown(true);
                        if (this.WaitForCompletion > 0)
                        {
                            s.WaitForCompletion(this.WaitForCompletion);
                        }
                    }

                    break;
                case DiscardUndoDisksTaskAction:
                    if (this.virtualMachine.State == VMVMState.vmVMState_TurnedOff)
                    {
                        this.virtualMachine.DiscardUndoDisks();
                    }

                    break;
                case DiscardSavedStateTaskAction:
                    if (this.virtualMachine.State == VMVMState.vmVMState_TurnedOff)
                    {
                        this.virtualMachine.DiscardSavedState();
                    }

                    break;
                case MergeUndoDisksTaskAction:
                    if (this.virtualMachine.State == (VMVMState.vmVMState_TurnedOff | VMVMState.vmVMState_Saved) && this.virtualMachine.Undoable)
                    {
                        var s = this.virtualMachine.MergeUndoDisks();
                        if (this.WaitForCompletion > 0)
                        {
                            s.WaitForCompletion(this.WaitForCompletion);
                        }
                    }

                    break;
                case PauseTaskAction:
                    if (this.virtualMachine.State == VMVMState.vmVMState_Running)
                    {
                        this.virtualMachine.Pause();
                    }

                    break;
                case ResumeTaskAction:
                    if (this.virtualMachine.State == VMVMState.vmVMState_Paused)
                    {
                        this.virtualMachine.Resume();
                    }

                    break;
                case ResetTaskAction:
                    if (this.virtualMachine.State != (VMVMState.vmVMState_TurnedOff | VMVMState.vmVMState_TurningOff))
                    {
                        var s = this.virtualMachine.Reset();
                        if (this.WaitForCompletion > 0)
                        {
                            s.WaitForCompletion(this.WaitForCompletion);
                        }
                    }

                    break;
                case SaveTaskAction:
                    if (this.virtualMachine.State == VMVMState.vmVMState_Running)
                    {
                        var s = this.virtualMachine.Save();
                        if (this.WaitForCompletion > 0)
                        {
                            s.WaitForCompletion(this.WaitForCompletion);
                        }
                    }

                    break;
                case AddHardDiskConnectionTaskAction:
                    if (this.virtualMachine.State == VMVMState.vmVMState_Running)
                    {
                        this.virtualMachine.AddHardDiskConnection(this.FileName.ItemSpec, this.BusNumber, this.DeviceNumber);
                    }

                    break;
                case RemoveHardDiskConnectionTaskAction:
                    if (this.virtualMachine.State == VMVMState.vmVMState_Running)
                    {
                        foreach (VMHardDiskConnection vhd in this.virtualMachine.HardDiskConnections)
                        {
                            if (vhd.HardDisk.File == this.FileName.ItemSpec)
                            {
                                this.virtualMachine.RemoveHardDiskConnection(vhd);
                            }
                        }
                    }

                    break;
            }
        }
    }
}