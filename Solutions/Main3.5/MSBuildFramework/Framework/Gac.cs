//-----------------------------------------------------------------------
// <copyright file="Gac.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Globalization;
    using System.Management;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddAssembly</i> (<b>Required: </b> AssemblyPath <b>Optional: </b>MachineName, RemoteAssemblyPath, UserName, UserPassword)</para>
    /// <para><i>CheckExists</i> (<b>Required: </b> AssemblyName <b>Optional: </b>MachineName)</para>
    /// <para><i>RemoveAssembly</i> (<b>Required: </b> AssemblyName <b>Optional: </b>MachineName, UserName, UserPassword)</para>
    /// <para><b>Remote Execution Support:</b> Partial (not for CheckExists)</para>
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
    ///         <!-- Add an assembly to the local cache -->
    ///         <MSBuild.ExtensionPack.Framework.Gac TaskAction="AddAssembly" AssemblyPath="c:\AnAssembly.dll"/>
    ///         <!-- Remove an assembly from the local cache -->
    ///         <MSBuild.ExtensionPack.Framework.Gac TaskAction="RemoveAssembly" AssemblyName="AnAssembly Version=3.0.8000.0,PublicKeyToken=f251491100750aea"/>
    ///         <!-- Add an assembly to a remote machine cache -->
    ///         <MSBuild.ExtensionPack.Framework.Gac TaskAction="AddAssembly" AssemblyPath="c:\aaa.dll" RemoteAssemblyPath="\\ANEWVM\c$\apath\aaa.dll" MachineName="ANEWVM" UserName="Administrator" UserPassword="O123"/>
    ///         <!-- Remove an assembly from a remote machine cache -->
    ///         <MSBuild.ExtensionPack.Framework.Gac TaskAction="RemoveAssembly" AssemblyName="aaa, Version=1.0.0.0,PublicKeyToken=e24a7ed7109b7e39" MachineName="ANEWVM" UserName="Admministrator" UserPassword="O123"/>
    ///         <!-- Check whether an assembly exists in the local cache -->
    ///         <MSBuild.ExtensionPack.Framework.Gac TaskAction="CheckExists" AssemblyName="aaa, Version=1.0.0.0,PublicKeyToken=e24a7ed7109b7e39">
    ///             <Output PropertyName="Exists2" TaskParameter="Exists"/>
    ///         </MSBuild.ExtensionPack.Framework.Gac>
    ///         <Message Text="Exists: $(Exists2)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Gac : BaseTask
    {
        /// <summary>
        /// Sets the remote path of the assembly
        /// </summary>
        public string RemoteAssemblyPath { get; set; }

        /// <summary>
        /// Sets the path to the assembly to be added the GAC
        /// </summary>
        public string AssemblyPath { get; set; }

        /// <summary>
        /// Sets the name of the assembly.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Set to True to force the file to be gacc'ed (overwrite any existing)
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Gets whether the assembly exists in the GAC
        /// </summary>
        [Output]
        public bool Exists { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.TaskAction)
            {
                case "AddAssembly":
                    this.AddAssembly();
                    break;
                case "CheckExists":
                    this.CheckExists();
                    break;
                case "RemoveAssembly":
                    this.RemoveAssembly();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        /// <summary>
        /// Gets the IAssemblyCache interface.
        /// </summary>
        /// <returns>
        /// An IAssemblyCache interface.
        /// </returns>
        private NativeMethods.IAssemblyCache GetIAssemblyCache()
        {
            // Get the IAssemblyCache interface
            NativeMethods.IAssemblyCache assemblyCache;
            int result = NativeMethods.CreateAssemblyCache(out assemblyCache, 0);

            // If the result is not zero throw an exception
            if (result != 0)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Failed to get the IAssemblyCache interface. Result Code: {0}", result));
                return null;
            }

            // Return the IAssemblyCache interface
            return assemblyCache;
        }

        private void Install(string path, bool force)
        {
            // Get the IAssemblyCache interface
            NativeMethods.IAssemblyCache assemblyCache = this.GetIAssemblyCache();

            // Set the flag depending on the value of force
            int flag = force ? 2 : 1;

            // Install the assembly in the cache
            int result = assemblyCache.InstallAssembly(flag, path, IntPtr.Zero);

            // If the result is not zero throw an exception
            if (result != 0)
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "Failed to install assembly into the global assembly cache. Result Code: {0}", result));
                return;
            }
        }

        private void Uninstall(string name)
        {
            // Get the IAssemblyCache interface
            NativeMethods.IAssemblyCache assemblyCache = this.GetIAssemblyCache();

            // Uninstall the assembly from the cache
            int disposition;
            int result = assemblyCache.UninstallAssembly(0, name, IntPtr.Zero, out disposition);

            // If the result is not zero or the disposition is not 1 then throw an exception
            if (result != 0)
            {
                // If result is not 0 then something happened. Check the value of disposition to see if we should throw an error.
                // Determined the values of the codes returned in disposition from
                switch (disposition)
                {
                    case 1:
                        // Assembly was removed from GAC.
                        break;
                    case 2:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "An application is using: {0} so it could not be uninstalled.", name));
                        return;
                    case 3:
                        // Assembly is not in the assembly. Don't throw an error, just proceed to install it.
                        break;
                    case 4:
                        // Not used.
                        break;
                    case 5:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "{0} was not uninstalled from the GAC because another reference exists to it.", name));
                        return;
                    case 6:
                        // Problem where a reference doesn't exist to the pointer. We aren't using the pointer so this shouldn't be a problem.
                        break;
                    default:
                        this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Failed to uninstall: {0} from the GAC.", name));
                        return;
                }
            }
        }

        private void CheckExists()
        {
            this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Checking if Assembly: {0} exists", this.AssemblyName));
            this.Exists = this.GetIAssemblyCache().QueryAssemblyInfo(0, this.AssemblyName, IntPtr.Zero) == 0;
        }

        private void RemoveAssembly()
        {
            if (string.Compare(this.MachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "UnGACing Assembly: {0}", this.AssemblyName));
                this.Uninstall(this.AssemblyName);
            }
            else
            {
                this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "UnGACing Assembly: {0} on Remote Server: {1}", this.AssemblyName, this.MachineName));
                this.Scope.Connect();
                ManagementClass m = new ManagementClass(this.Scope, new ManagementPath("Win32_Process"), new ObjectGetOptions(null, System.TimeSpan.MaxValue, true));
                ManagementBaseObject methodParameters = m.GetMethodParameters("Create");
                methodParameters["CommandLine"] = @"gacutil.exe /u " + "\"" + this.AssemblyName + "\"";
                ManagementBaseObject outParams = m.InvokeMethod("Create", methodParameters, null);

                if (outParams != null)
                {
                    this.Log.LogMessage(MessageImportance.Low, "Process returned: " + outParams["returnValue"]);
                    this.Log.LogMessage(MessageImportance.Low, "Process ID: " + outParams["processId"]);
                }
                else
                {
                    Log.LogError("Remote Remove returned null");
                    return;
                }
            }
        }

        private void AddAssembly()
        {
            if (System.IO.File.Exists(this.AssemblyPath) == false)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "The AssemblyPath was not found: {0}", this.AssemblyPath));
            }
            else
            {
                if (string.Compare(this.MachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "GACing Assembly: {0}", this.AssemblyPath));
                    this.Install(this.AssemblyPath, this.Force);
                }
                else
                {
                    if (string.IsNullOrEmpty(this.RemoteAssemblyPath))
                    {
                        this.Log.LogError("RemotePath is Required");
                        return;
                    }

                    this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "GACing Assembly: {0} on Remote Server: {1}", this.RemoteAssemblyPath, this.MachineName));
                    this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Copying Assembly from: {0} to: {1}", this.AssemblyPath, this.RemoteAssemblyPath));

                    // the assembly needs to be copied to the remote server for gaccing.
                    System.IO.File.Copy(this.AssemblyPath, this.RemoteAssemblyPath);

                    this.Scope.Connect();
                    ManagementClass m = new ManagementClass(this.Scope, new ManagementPath("Win32_Process"), new ObjectGetOptions(null, System.TimeSpan.MaxValue, true));
                    ManagementBaseObject methodParameters = m.GetMethodParameters("Create");
                    methodParameters["CommandLine"] = @"gacutil.exe /i " + this.RemoteAssemblyPath;
                    ManagementBaseObject outParams = m.InvokeMethod("Create", methodParameters, null);

                    if (outParams != null)
                    {
                        this.Log.LogMessage(MessageImportance.Low, "Process returned: " + outParams["returnValue"]);
                        this.Log.LogMessage(MessageImportance.Low, "Process ID: " + outParams["processId"]);
                    }
                    else
                    {
                        Log.LogError("Remote Create returned null");
                        return;
                    }
                }
            }
        }
    }
}