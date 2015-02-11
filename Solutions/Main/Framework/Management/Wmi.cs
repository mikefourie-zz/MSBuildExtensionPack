//-----------------------------------------------------------------------
// <copyright file="Wmi.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Management
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Management;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Execute</i> (<b>Required: </b> Class, Namespace, Method <b> Optional: </b>Instance, MethodParameters <b>Output: </b>ReturnValue)</para>
    /// <para><i>Query</i> (<b>Required: </b> Class, Properties <b>Output: </b>Info (ITaskItem))</para>
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
    ///     <Target Name="Default">
    ///         <ItemGroup>
    ///             <WmiProps Include="BIOSVersion"/>
    ///             <WmiProps Include="CurrentLanguage"/>
    ///             <WmiProps Include="Manufacturer"/>
    ///             <WmiProps Include="SerialNumber"/>
    ///             <Wmi2Props Include="InstanceName"/>
    ///             <!-- Note that #~# is used as a separator-->
    ///             <WmiExec Include="Description#~#ExtensionPack Description"/>
    ///             <WmiExec2 Include="Name#~#MyNewShare;Path#~#C:\demo;Type#~#0"/>
    ///             <WmiExec3 Include="CommandLine#~#calc.exe"/>
    ///         </ItemGroup>
    ///         <!-- Start the Calculator -->
    ///         <MSBuild.ExtensionPack.Management.Wmi TaskAction="Execute" Class="Win32_Process" Method="Create" MethodParameters="@(WmiExec3)" Namespace="\root\CIMV2">
    ///             <Output TaskParameter="ReturnValue" PropertyName="Rval2"/>
    ///         </MSBuild.ExtensionPack.Management.Wmi>
    ///         <Message Text="ReturnValue: $(Rval2)"/>
    ///         <!-- Create a share -->
    ///         <MSBuild.ExtensionPack.Management.Wmi TaskAction="Execute" Class="Win32_Share" Method="Create" MethodParameters="@(WmiExec2)" Namespace="\root\CIMV2">
    ///             <Output TaskParameter="ReturnValue" PropertyName="Rval2"/>
    ///         </MSBuild.ExtensionPack.Management.Wmi>
    ///         <Message Text="ReturnValue: $(Rval2)"/>
    ///         <!-- Set share details using the WmiExec ItemGroup info-->
    ///         <MSBuild.ExtensionPack.Management.Wmi TaskAction="Execute" Class="Win32_Share" Method="SetShareInfo" Instance="Name='ashare'" MethodParameters="@(WmiExec)" Namespace="\root\CIMV2">
    ///             <Output TaskParameter="ReturnValue" PropertyName="Rval"/>
    ///         </MSBuild.ExtensionPack.Management.Wmi>
    ///         <Message Text="ReturnValue: $(Rval)"/>
    ///         <!-- Stop a service -->
    ///         <MSBuild.ExtensionPack.Management.Wmi TaskAction="Execute" Class="Win32_Service" Method="StopService" Instance="Name='SQLSERVERAGENT'" Namespace="\root\CIMV2">
    ///             <Output TaskParameter="ReturnValue" PropertyName="Rval2"/>
    ///         </MSBuild.ExtensionPack.Management.Wmi>
    ///         <Message Text="ReturnValue: $(Rval2)"/>
    ///         <!-- Query the Bios properties -->
    ///         <MSBuild.ExtensionPack.Management.Wmi TaskAction="Query" Class="Win32_BIOS" Properties="@(WmiProps)" Namespace="\root\cimv2">
    ///             <Output TaskParameter="Info" ItemName="Info"/>
    ///         </MSBuild.ExtensionPack.Management.Wmi>
    ///         <Message Text="WMI Info for Win32_BIOS on %(Info.Identity): BIOSVersion=%(Info.BIOSVersion), CurrentLanguage=%(Info.CurrentLanguage), Manufacturer=%(Info.Manufacturer), SerialNumber=%(Info.SerialNumber)"/>
    ///         <!-- Query the server settings properties -->
    ///         <MSBuild.ExtensionPack.Management.Wmi TaskAction="Query" Class="ServerSettings" Properties="@(Wmi2Props)" Namespace="\root\Microsoft\SqlServer\ComputerManagement">
    ///             <Output TaskParameter="Info" ItemName="Info2"/>
    ///         </MSBuild.ExtensionPack.Management.Wmi>
    ///         <Message Text="WMI Info for ServerSettings on %(Info2.Identity): InstanceName=%(Info2.InstanceName)"/>
    ///         <!-- Query a remote server -->
    ///         <MSBuild.ExtensionPack.Management.Wmi TaskAction="Query" MachineName="AREMOTESERVER" UserName="ADOMAIN\AUSERNAME" UserPassword="APASSWORD" Class="Win32_BIOS" Properties="@(WmiProps)" Namespace="\root\cimv2">
    ///             <Output TaskParameter="Info" ItemName="Info2"/>
    ///         </MSBuild.ExtensionPack.Management.Wmi>
    ///         <Message Text="WMI Info for %(Info2.Identity): BIOSVersion=%(Info2.BIOSVersion), CurrentLanguage=%(Info2.CurrentLanguage), Manufacturer=%(Info2.Manufacturer), SerialNumber=%(Info2.SerialNumber)"/>
    ///         <!-- Let's stop Paint.net -->
    ///         <ItemGroup>
    ///             <WmiProps2 Include="Name"/>
    ///             <WmiProps2 Include="ProcessID"/>
    ///         </ItemGroup>
    ///         <MSBuild.ExtensionPack.Management.Wmi TaskAction="Query" Class="Win32_Process WHERE Name='paintdotnet.exe'" Namespace="\root\CIMV2" Properties="@(WmiProps2)" MachineName="192.168.0.6">
    ///             <Output TaskParameter="Info" ItemName="Info"/>
    ///         </MSBuild.ExtensionPack.Management.Wmi>
    ///         <Message Text="WMI Info for Win32_Processes: Name: %(Info.Name), ProcessID: %(Info.ProcessID)"/>
    ///         <Message Text="Stopping Paint.NET" Condition="%(Info.ProcessID) != ''"/>
    ///         <MSBuild.ExtensionPack.Management.Wmi TaskAction="Execute" Class="Win32_Process" Method="Terminate" Namespace="\root\CIMV2" Instance="Handle=%(Info.ProcessID)" MachineName="192.168.0.6" Condition="%(Info.ProcessID) != ''"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Wmi : BaseTask
    {
        private List<ITaskItem> info;
        private List<ITaskItem> properties;

        /// <summary>
        /// Sets the namespace.
        /// </summary>
        [Required]
        public string Namespace { get; set; }

        /// <summary>
        /// Gets the WMI info.
        /// </summary>
        [Output]
        public ITaskItem[] Info
        {
            get { return this.info.ToArray(); }
            set { this.info = new List<ITaskItem>(value); }
        }

        /// <summary>
        /// Sets the WMI class.
        /// </summary>
        [Required]
        public string Class { get; set; }

        /// <summary>
        /// Gets the ReturnValue for Execute
        /// </summary>
        [Output]
        public string ReturnValue { get; set; }

        /// <summary>
        /// Sets the Method used in Execute
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Sets the MethodParameters. Use #~# separate name and value.
        /// </summary>
        public ITaskItem[] MethodParameters { get; set; }

        /// <summary>
        /// Sets the Wmi Instance used in Execute
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// An Item Collection of Properties to get
        /// </summary>
        public ITaskItem[] Properties
        {
            get { return this.properties.ToArray(); }
            set { this.properties = new List<ITaskItem>(value); }
        }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case "Execute":
                    this.ExecuteWmi();
                    break;
                case "Query":
                    this.Query();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void ExecuteWmi()
        {
            this.GetManagementScope(this.Namespace);
            string managementPath = this.Class;
            if (!string.IsNullOrEmpty(this.Instance))
            {
                managementPath += "." + this.Instance;

                using (var classInstance = new ManagementObject(this.Scope, new ManagementPath(managementPath), null))
                {
                    // Obtain in-parameters for the method
                    ManagementBaseObject inParams = classInstance.GetMethodParameters(this.Method);
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Method: {0}", this.Method));

                    if (this.MethodParameters != null)
                    {
                        // Add the input parameters.
                        foreach (string[] data in this.MethodParameters.Select(param => param.ItemSpec.Split(new[] { "#~#" }, StringSplitOptions.RemoveEmptyEntries)))
                        {
                            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Param: {0}. Value: {1}", data[0], data[1]));
                            inParams[data[0]] = data[1];
                        }
                    }

                    // Execute the method and obtain the return values.
                    ManagementBaseObject outParams = classInstance.InvokeMethod(this.Method, inParams, null);
                    if (outParams != null)
                    {
                        this.ReturnValue = outParams["ReturnValue"].ToString();
                    }
                }
            }
            else
            {
                using (ManagementClass mgmtClass = new ManagementClass(this.Scope, new ManagementPath(managementPath), null))
                {
                    // Obtain in-parameters for the method
                    ManagementBaseObject inParams = mgmtClass.GetMethodParameters(this.Method);
                    this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Method: {0}", this.Method));

                    if (this.MethodParameters != null)
                    {
                        // Add the input parameters.
                        foreach (string[] data in this.MethodParameters.Select(param => param.ItemSpec.Split(new[] { "#~#" }, StringSplitOptions.RemoveEmptyEntries)))
                        {
                            this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Param: {0}. Value: {1}", data[0], data[1]));
                            inParams[data[0]] = data[1];
                        }
                    }

                    // Execute the method and obtain the return values.
                    ManagementBaseObject outParams = mgmtClass.InvokeMethod(this.Method, inParams, null);
                    if (outParams != null)
                    {
                        this.ReturnValue = outParams["ReturnValue"].ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the remote info.
        /// </summary>
        private void Query()
        {
            this.info = new List<ITaskItem>();
            this.GetManagementScope(this.Namespace);
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Executing WMI query: SELECT * FROM {0}", this.Class));
            ObjectQuery query = new ObjectQuery("SELECT * FROM " + this.Class);
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(this.Scope, query))
            {
                ManagementObjectCollection queryCollection = searcher.Get();
                foreach (ManagementObject m in queryCollection)
                {
                    ITaskItem item = new TaskItem(this.MachineName);
                    foreach (ITaskItem prop in this.Properties)
                    {
                        try
                        {
                            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Extracting Property: {0}", prop.ItemSpec));
                            string value = string.Empty;

                            // sometimes the properties might be arrays.....
                            try
                            {
                                string[] propertiesArray = (string[])m[prop.ItemSpec];
                                value = propertiesArray.Aggregate(value, (current, arrValue) => current + (arrValue + "~~~"));
                                value = value.Remove(value.Length - 3, 3);
                            }
                            catch
                            {
                                value = m[prop.ItemSpec].ToString();
                            }

                            item.SetMetadata(prop.ItemSpec, value + string.Empty);
                        }
                        catch
                        {
                            this.LogTaskWarning(string.Format(CultureInfo.CurrentCulture, "Property Not Found: {0}", prop.ItemSpec));
                        }
                    }

                    this.info.Add(item);
                }
            }
        }
    }
}