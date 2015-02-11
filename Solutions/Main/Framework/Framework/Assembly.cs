//-----------------------------------------------------------------------
// <copyright file="Assembly.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>GetInfo</i> (<b>Required: </b>NetAssembly <b>Output: </b>OutputItems)</para>
    /// <para><i>GetMethodInfo</i> (<b>Required: </b>NetAssembly, NetClass, <b>Output: </b>OutputItems)</para>
    /// <para><i>Invoke</i> (<b>Required: </b>NetAssembly <b>Optional: </b>NetMethod, NetArguments<b>Output: </b>ReturnValue)</para>
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
    ///         <ItemGroup>
    ///             <!-- define a set of arguments to use against an assembly -->
    ///             <Args Include="1">
    ///                 <Type>int</Type>
    ///             </Args>
    ///             <Args Include="2">
    ///                 <Type>int</Type>
    ///             </Args>
    ///             <ArgsM Include="2.9845">
    ///                 <Type>decimal</Type>
    ///             </ArgsM>
    ///             <ArgsM Include="1.9845">
    ///                 <Type>decimal</Type>
    ///             </ArgsM>
    ///             <ArgsF Include="C:\Demo1 - Please Delete">
    ///                 <Type>string</Type>
    ///             </ArgsF>
    ///         </ItemGroup>
    ///         <!-- Get information on an assembly -->
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="GetInfo" NetAssembly="C:\Projects\MSBuildExtensionPack\Solutions\Main3.5\BuildBinaries\MSBuild.ExtensionPack.dll">
    ///             <Output TaskParameter="OutputItems" ItemName="Info"/>
    ///         </MSBuild.ExtensionPack.Framework.Assembly>
    ///         <Message Text="Identity: %(Info.Identity)" />
    ///         <Message Text="FullName: %(Info.FullName)" />
    ///         <Message Text="PublicKeyToken: %(Info.PublicKeyToken)" />
    ///         <Message Text="Culture: %(Info.Culture)" />
    ///         <Message Text="CultureDisplayName: %(Info.CultureDisplayName)" />
    ///         <Message Text="FileVersion: %(Info.FileVersion)" />
    ///         <Message Text="AssemblyVersion: %(Info.AssemblyVersion)" />
    ///         <Message Text="AssemblyInformationalVersion: %(Info.AssemblyInformationalVersion)" />
    ///         <!-- This will cause a default constructor call only -->
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="Invoke" NetClass="AssemblyDemo" NetAssembly="C:\Projects\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll"/>
    ///         <!--Invoke the assembly with the args collection of arguments -->
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="Invoke" NetArguments="@(Args)" NetClass="AssemblyDemo" NetMethod="AddNumbers" NetAssembly="C:\Projects\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll">
    ///             <Output TaskParameter="Result" PropertyName="R"/>
    ///         </MSBuild.ExtensionPack.Framework.Assembly>
    ///         <Message Text="Result: $(R)"/>
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="Invoke" NetArguments="@(ArgsM)" NetClass="AssemblyDemo" NetMethod="MultiplyNumbers" NetAssembly="C:\Projects\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll">
    ///             <Output TaskParameter="Result" PropertyName="R"/>
    ///         </MSBuild.ExtensionPack.Framework.Assembly>
    ///         <Message Text="Result: $(R)"/>
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="Invoke" NetArguments="@(ArgsF)" NetClass="AssemblyDemo" NetMethod="CreateFolder" NetAssembly="C:\Projects\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll"/>
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="Invoke" NetClass="AssemblyDemo" NetMethod="CreateDefaultFolder" NetAssembly="C:\Projects\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll"/>
    ///         <!-- Extract some information on the assembly interface -->
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="GetMethodInfo" NetAssembly="C:\Projects\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll">
    ///             <Output TaskParameter="OutputItems" ItemName="TypeInfo"/>
    ///         </MSBuild.ExtensionPack.Framework.Assembly>
    ///         <Message Text="%(TypeInfo.Identity) %(TypeInfo.Parameters)" />
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Assembly : BaseAppDomainIsolatedTask
    {
        private System.Reflection.Assembly loadedAssembly;
        private List<ITaskItem> outputItems;

        /// <summary>
        /// Sets the name of the Assembly
        /// </summary>
        [Required]
        public ITaskItem NetAssembly { get; set; }

        /// <summary>
        /// Sets the name of the Class
        /// </summary>
        public string NetClass { get; set; }

        /// <summary>
        /// Sets the name of the Method. If this is not provided, a call is made to the default constructor.
        /// </summary>
        public string NetMethod { get; set; }

        /// <summary>
        /// Gets any Result that is returned
        /// </summary>
        [Output]
        public string Result { get; set; }

        /// <summary>
        /// Gets the outputitems.
        /// <para/>For a call to GetMethodInfo, OutputItems provides the following metadata: Parameters
        /// <para/>For a call to GetInfo, OutputItems provides the following metadata: AssemblyVersion, FileVersion, Culture, CultureDisplayName, FullName, PublicKeyToken, AssemblyInformationalVersion
        /// </summary>
        [Output]
        public ITaskItem[] OutputItems
        {
            get { return this.outputItems.ToArray(); }
            set { this.outputItems = new List<ITaskItem>(value); }
        }

        /// <summary>
        /// Sets the arguments to use for invoking a method. The arguments must be specified with a type, i.e.
        ///    <Args Include="1">
        ///        <Type>int</Type>
        ///    </Args>
        /// </summary>
        public ITaskItem[] NetArguments { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            if (!System.IO.File.Exists(this.NetAssembly.GetMetadata("FullPath")))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "File not found: {0}", this.NetAssembly.GetMetadata("FullPath")));

                // set the OutputItems so we dont get a null ref exception.
                this.OutputItems = new ITaskItem[0];
                return;
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Loading Assembly: {0}", this.NetAssembly.GetMetadata("FullPath")));
            this.loadedAssembly = System.Reflection.Assembly.LoadFrom(this.NetAssembly.GetMetadata("FullPath"));

            switch (this.TaskAction)
            {
                case "GetInfo":
                    this.GetInfo();
                    break;
                case "Invoke":
                    this.Invoke();
                    break;
                case "GetMethodInfo":
                    this.GetMethodInfo();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void GetInfo()
        {
            if (this.loadedAssembly != null)
            {
                this.outputItems = new List<ITaskItem>();
                ITaskItem t = new TaskItem(this.NetAssembly.GetMetadata("FileName"));

                // get the PublicKeyToken
                byte[] pt = this.loadedAssembly.GetName().GetPublicKeyToken();
                StringBuilder s = new System.Text.StringBuilder();
                for (int i = 0; i < pt.GetLength(0); i++)
                {
                    s.Append(pt[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                // set some other metadata items
                t.SetMetadata("PublicKeyToken", s.ToString());
                t.SetMetadata("FullName", this.loadedAssembly.GetName().FullName);
                t.SetMetadata("Culture", this.loadedAssembly.GetName().CultureInfo.Name);
                t.SetMetadata("CultureDisplayName", this.loadedAssembly.GetName().CultureInfo.DisplayName);
                t.SetMetadata("AssemblyVersion", this.loadedAssembly.GetName().Version.ToString());
                t.SetMetadata("AssemblyInformationalVersion", FileVersionInfo.GetVersionInfo(this.loadedAssembly.Location).ProductVersion);
                
                // get the assembly file version
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(this.loadedAssembly.Location);
                System.Version v = new System.Version(versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.FilePrivatePart);
                t.SetMetadata("FileVersion", v.ToString());              
                this.outputItems.Add(t);
            }
        }

        private void GetMethodInfo()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Getting MethodInfo for: {0}", this.NetAssembly.GetMetadata("FullPath")));
            this.outputItems = new List<ITaskItem>();
            foreach (Type type in this.loadedAssembly.GetTypes())
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Found Type: {0}", this.NetClass));
                ITaskItem t = new TaskItem(type.Name);
                string paras = string.Empty;
                foreach (MethodInfo mi in type.GetMethods())
                {
                    foreach (ParameterInfo pi in mi.GetParameters())
                    {
                        this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Found Parameter: {0}", pi.Name));
                        paras += string.Format(CultureInfo.CurrentCulture, "[{0}] {1} | ", pi.ParameterType, pi.Name);
                    }

                    // remove the last object paramater which appears on methods.
                    t.SetMetadata("Parameters", paras.Replace(" | [System.Object] obj | ", string.Empty));
                }
                
                this.outputItems.Add(t);
            }
        }

        private void Invoke()
        {
            bool typeFound = false;
            foreach (Type type in this.loadedAssembly.GetTypes())
            {
                if (type.IsClass && type.Name == this.NetClass)
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Found Type: {0}", this.NetClass));
                    typeFound = true;
                    object[] arguments = new object[0];
                    if (this.NetArguments != null)
                    {
                        arguments = new object[this.NetArguments.Length];
                        int i = 0;
                        foreach (ITaskItem it in this.NetArguments)
                        {
                            object to = this.GetTypedObject(it);
                            arguments[i] = to;
                            i++;
                        }
                    }
                    
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Invoking: {0}", this.NetMethod));
    
                    if (this.NetMethod == null)
                    {
                        // allows call to the default constructor
                        this.NetMethod = string.Empty;                    
                    }

                    object result = type.InvokeMember(this.NetMethod, BindingFlags.Default | BindingFlags.InvokeMethod, null, Activator.CreateInstance(type), arguments, CultureInfo.CurrentCulture);

                    if (result != null)
                    {
                        this.Result = result.ToString();
                    }
                }
            }

            if (!typeFound)
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "Type not Found: {0}", this.NetClass));
            }
        }

        private object GetTypedObject(ITaskItem it)
        {
            switch (it.GetMetadata("Type").ToUpperInvariant())
            {
                case "INT":
                    return Convert.ToInt32(it.ItemSpec, CultureInfo.CurrentCulture);
                case "STRING":
                    return it.ItemSpec;
                case "DOUBLE":
                    return Convert.ToDouble(it.ItemSpec, CultureInfo.CurrentCulture);
                case "BOOL":
                    return Convert.ToBoolean(it.ItemSpec, CultureInfo.CurrentCulture);
                case "CHAR":
                    return Convert.ToChar(it.ItemSpec, CultureInfo.CurrentCulture);
                case "LONG":
                    return Convert.ToInt64(it.ItemSpec, CultureInfo.CurrentCulture);
                case "DECIMAL":
                    return Convert.ToDecimal(it.ItemSpec, CultureInfo.CurrentCulture);
                case "DATETIME":
                    return Convert.ToDateTime(it.ItemSpec, CultureInfo.CurrentCulture);
                default:
                    Log.LogError("Invalid Type supplied");
                    break;
            }

            return null;
        }
    }
}