//-----------------------------------------------------------------------
// <copyright file="Process.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Computer
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Management;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>CheckRunning</i> (<b>Required: </b>ProcessName <b>Output: </b> IsRunning)</para>
    /// <para><i>Create</i> (<b>Required: </b>Parameters <b>Output: </b> ReturnValue, ProcessId)</para>
    /// <para><i>Get</i> (<b>Required: </b>ProcessName, Value <b>Optional: </b>User, ProcessName, IncludeUserInfo <b>Output: </b> Processes)</para>
    /// <para><i>Terminate</i> (<b>Required: </b>ProcessName or ProcessId)</para>
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
    ///         <WmiExec3 Include="CommandLine#~#notepad.exe"/>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <MSBuild.ExtensionPack.Computer.Process TaskAction="Terminate" ProcessId="9564"/>
    ///         <MSBuild.ExtensionPack.Computer.Process TaskAction="Create" Parameters="@(WmiExec3)">
    ///             <Output TaskParameter="ReturnValue" PropertyName="Rval2"/>
    ///             <Output TaskParameter="ProcessId" PropertyName="PID"/>
    ///         </MSBuild.ExtensionPack.Computer.Process>
    ///         <Message Text="ReturnValue: $(Rval2). ProcessId: $(PID)"/>
    ///         <MSBuild.ExtensionPack.Computer.Process TaskAction="CheckRunning" ProcessName="notepad.exe">
    ///             <Output PropertyName="Running" TaskParameter="IsRunning"/>
    ///         </MSBuild.ExtensionPack.Computer.Process>
    ///         <Message Text="notepad.exe IsRunning: $(Running)"/>
    ///         <MSBuild.ExtensionPack.Computer.Process TaskAction="Terminate" ProcessName="notepad.exe"/>
    ///         <MSBuild.ExtensionPack.Computer.Process TaskAction="CheckRunning" ProcessName="notepad.exe">
    ///             <Output PropertyName="Running" TaskParameter="IsRunning"/>
    ///         </MSBuild.ExtensionPack.Computer.Process>
    ///         <Message Text="notepad.exe IsRunning: $(Running)"/>
    ///         <MSBuild.ExtensionPack.Computer.Process TaskAction="Get" IncludeUserInfo="true">
    ///             <Output ItemName="ProcessList" TaskParameter="Processes"/>
    ///         </MSBuild.ExtensionPack.Computer.Process>
    ///         <Message Text="%(ProcessList.Identity)  - %(ProcessList.User) - %(ProcessList.OwnerSID)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Process : BaseTask
    {
        private const string GetTaskAction = "Get";
        private const string CreateTaskAction = "Create";
        private const string TerminateTaskAction = "Terminate";
        private const string CheckRunningTaskAction = "CheckRunning";
        private string processName = ".*";
        private string user = ".*";

        /// <summary>
        /// Sets the regular expression to use for filtering processes. Default is .*
        /// </summary>
        public string ProcessName
        {
            get { return this.processName; }
            set { this.processName = value; }
        }

        /// <summary>
        /// Sets the regular expression to use for filtering processes. Default is .*
        /// </summary>
        public string User
        {
            get { return this.user; }
            set { this.user = value; }
        }

        /// <summary>
        /// Gets the ReturnValue for Create
        /// </summary>
        [Output]
        public string ReturnValue { get; set; }

        /// <summary>
        /// Gets or Sets the ProcessId
        /// </summary>
        [Output]
        public int ProcessId { get; set; }

        /// <summary>
        /// Sets the Parameters for Create. Use #~# separate name and value.
        /// </summary>
        public ITaskItem[] Parameters { get; set; }

        /// <summary>
        /// Sets whether to include user information for processes. Including this will slow the query. Default is false;
        /// </summary>
        public bool IncludeUserInfo { get; set; }

        /// <summary>
        /// Gets whether the process is running
        /// </summary>
        [Output]
        public bool IsRunning { get; set; }

        /// <summary>
        /// Gets the list of processes. The process name is used as the identity and the following metadata is set: Caption, Description, Handle, HandleCount, KernelModeTime, PageFaults, PageFileUsage, ParentProcessId, PeakPageFileUsage, PeakVirtualSize, PeakWorkingSetSize, Priority, PrivatePageCount, ProcessId, QuotaNonPagedPoolUsage, QuotaPagedPoolUsage, QuotaPeakNonPagedPoolUsage, QuotaPeakPagedPoolUsage, ReadOperationCount, ReadTransferCount, SessionId, ThreadCount, UserModeTime, VirtualSize, WindowsVersion, WorkingSetSize, WriteOperationCount, WriteTransferCount
        /// </summary>
        [Output]
        public ITaskItem[] Processes { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            this.GetManagementScope(@"\root\cimv2");
            switch (this.TaskAction)
            {
                case CreateTaskAction:
                    this.Create();
                    break;
                case GetTaskAction:
                    this.Get();
                    break;
                case TerminateTaskAction:
                    this.Kill();
                    break;
                case CheckRunningTaskAction:
                    this.CheckRunning();
                    break;
                default:
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Create()
        {
            if (this.Parameters == null)
            {
                this.Log.LogError("Parameters is required");
                return;
            }

            using (ManagementClass mgmtClass = new ManagementClass(this.Scope, new ManagementPath("Win32_Process"), null))
            {
                // Obtain in-parameters for the method
                ManagementBaseObject inParams = mgmtClass.GetMethodParameters("Create");
                if (this.Parameters != null)
                {
                    // Add the input parameters.
                    foreach (string[] data in this.Parameters.Select(param => param.ItemSpec.Split(new[] { "#~#" }, StringSplitOptions.RemoveEmptyEntries)))
                    {
                        this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Param: {0}. Value: {1}", data[0], data[1]));
                        inParams[data[0]] = data[1];
                    }
                }

                // Execute the method and obtain the return values.
                ManagementBaseObject outParams = mgmtClass.InvokeMethod("Create", inParams, null);
                if (outParams != null)
                {
                    this.ReturnValue = outParams["ReturnValue"].ToString();
                    this.ProcessId = Convert.ToInt32(outParams["ProcessId"], CultureInfo.CurrentCulture);
                }
            }
        }

        private void CheckRunning()
        {
            if (string.IsNullOrEmpty(this.ProcessName))
            {
                this.Log.LogError("ProcessName is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Checking whether Process is running: {0}", this.ProcessName));
            
            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_Process WHERE Name ='" + this.ProcessName + "'");
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.Scope, query, null))
            {
                ManagementObjectCollection moc = searcher.Get();
                if (moc.Count > 0)
                {
                    this.IsRunning = true;
                }
            }
        }

        private void Kill()
        {
            if (this.ProcessName == ".*" && this.ProcessId == 0)
            {
                this.Log.LogError("ProcessName or ProcessId is required");
                return;
            }

            ObjectQuery query = this.ProcessName != ".*" ? new ObjectQuery("SELECT * FROM Win32_Process WHERE Name ='" + this.ProcessName + "'") : new ObjectQuery("SELECT * FROM Win32_Process WHERE Handle ='" + this.ProcessId + "'");
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.Scope, query, null))
            {
                foreach (ManagementObject returnedProcess in searcher.Get())
                {
                    this.LogTaskMessage(this.ProcessName != ".*" ? string.Format(CultureInfo.CurrentCulture, "Terminating: {0}", this.ProcessName) : string.Format(CultureInfo.CurrentCulture, "Terminating: {0}", this.ProcessId));

                    ManagementBaseObject inParams = returnedProcess.GetMethodParameters("Terminate");
                    ManagementBaseObject outParams = returnedProcess.InvokeMethod("Terminate", inParams, null);
                   
                    // ReturnValue should be 0, else failure
                    if (outParams != null)
                    {
                        switch (Convert.ToInt32(outParams.Properties["ReturnValue"].Value, CultureInfo.CurrentCulture))
                        {
                            case 0:
                                this.LogTaskMessage("...Process Terminated");
                                break;
                            case 2:
                                this.Log.LogError("...Access Denied");
                                break;
                            case 3:
                                this.Log.LogError("...Insufficient Privilege");
                                break;
                            case 8:
                                this.Log.LogError("...Unknown Failure");
                                break;
                            case 9:
                                this.Log.LogError("...Path Not Found");
                                break;
                            case 21:
                                this.Log.LogError("...Invalid Parameter");
                                break;
                        }
                    }
                }
            }
        }

        private void Get()
        {
            if (string.IsNullOrEmpty(this.ProcessName))
            {
                this.Log.LogError("ProcessName is required");
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting Processes matching: {0}", this.ProcessName));

            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_Process");
            Regex userFilter = new Regex(this.User, RegexOptions.Compiled);
            Regex processFilter = new Regex(this.ProcessName, RegexOptions.Compiled);
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.Scope, query, null))
            {
                this.Processes = new ITaskItem[searcher.Get().Count];
                int i = 0;
                foreach (ManagementObject ret in searcher.Get())
                {
                    if (processFilter.IsMatch(ret["Name"].ToString()))
                    {
                        ITaskItem processItem = new TaskItem(ret["Name"].ToString());
                        processItem.SetMetadata("Caption", ret["Caption"].ToString());
                        processItem.SetMetadata("Description", ret["Description"].ToString());
                        processItem.SetMetadata("Handle", ret["Handle"].ToString());
                        processItem.SetMetadata("HandleCount", ret["HandleCount"].ToString());
                        processItem.SetMetadata("KernelModeTime", ret["KernelModeTime"].ToString());
                        processItem.SetMetadata("PageFaults", ret["PageFaults"].ToString());
                        processItem.SetMetadata("PageFileUsage", ret["PageFileUsage"].ToString());
                        processItem.SetMetadata("ParentProcessId", ret["ParentProcessId"].ToString());
                        processItem.SetMetadata("PeakPageFileUsage", ret["PeakPageFileUsage"].ToString());
                        processItem.SetMetadata("PeakVirtualSize", ret["PeakVirtualSize"].ToString());
                        processItem.SetMetadata("PeakWorkingSetSize", ret["PeakWorkingSetSize"].ToString());
                        processItem.SetMetadata("Priority", ret["Priority"].ToString());
                        processItem.SetMetadata("PrivatePageCount", ret["PrivatePageCount"].ToString());
                        processItem.SetMetadata("ProcessId", ret["ProcessId"].ToString());
                        processItem.SetMetadata("QuotaNonPagedPoolUsage", ret["QuotaNonPagedPoolUsage"].ToString());
                        processItem.SetMetadata("QuotaPagedPoolUsage", ret["QuotaPagedPoolUsage"].ToString());
                        processItem.SetMetadata("QuotaPeakNonPagedPoolUsage", ret["QuotaPeakNonPagedPoolUsage"].ToString());
                        processItem.SetMetadata("QuotaPeakPagedPoolUsage", ret["QuotaPeakPagedPoolUsage"].ToString());
                        processItem.SetMetadata("ReadOperationCount", ret["ReadOperationCount"].ToString());
                        processItem.SetMetadata("ReadTransferCount", ret["ReadTransferCount"].ToString());
                        processItem.SetMetadata("SessionId", ret["SessionId"].ToString());
                        processItem.SetMetadata("ThreadCount", ret["ThreadCount"].ToString());
                        processItem.SetMetadata("UserModeTime", ret["UserModeTime"].ToString());
                        processItem.SetMetadata("VirtualSize", ret["VirtualSize"].ToString());
                        processItem.SetMetadata("WindowsVersion", ret["WindowsVersion"].ToString());
                        processItem.SetMetadata("WorkingSetSize", ret["WorkingSetSize"].ToString());
                        processItem.SetMetadata("WriteOperationCount", ret["WriteOperationCount"].ToString());
                        processItem.SetMetadata("WriteTransferCount", ret["WriteTransferCount"].ToString());
                        if (this.IncludeUserInfo)
                        {
                            string[] o = new string[2];
                            ret.InvokeMethod("GetOwner", o);

                            if (o[0] == null)
                            {
                                continue;
                            }

                            if (!userFilter.IsMatch(o[0]))
                            {
                                continue;
                            }

                            processItem.SetMetadata("User", o[0]);

                            if (o[1] != null)
                            {
                                processItem.SetMetadata("Domain", o[1]);
                            }

                            string[] sid = new string[1];
                            ret.InvokeMethod("GetOwnerSid", sid);
                            if (sid[0] != null)
                            {
                                processItem.SetMetadata("OwnerSID", sid[0]);
                            }
                        }

                        this.Processes[i] = processItem;
                        i++;
                    }
                }
            }
        }
    }
}