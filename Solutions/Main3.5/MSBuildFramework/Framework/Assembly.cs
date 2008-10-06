//-----------------------------------------------------------------------
// <copyright file="Assembly.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>GetMethodInfo</i> (<b>Required: </b>NetAssembly, NetClass, <b>Output: </b>OutputItems)</para>
    /// <para><i>Invoke</i> (<b>Required: </b>NetAssembly <b>Optional: </b>NetMethod, NetArguments<b>Output: </b>ReturnValue)</para>
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
    ///         <!-- This will cause a default constructor call only -->
    ///          <MSBuild.ExtensionPack.Framework.Assembly TaskAction="Invoke" NetClass="AssemblyDemo" NetAssembly="C:\Projects\CodePlex\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll"/>
    ///         <!--Invoke the assembly with the args collection of arguments -->
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="Invoke" NetArguments="@(Args)" NetClass="AssemblyDemo" NetMethod="AddNumbers" NetAssembly="C:\Projects\CodePlex\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll">
    ///             <Output TaskParameter="Result" PropertyName="R"/>
    ///         </MSBuild.ExtensionPack.Framework.Assembly>
    ///         <Message Text="Result: $(R)"/>
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="Invoke" NetArguments="@(ArgsM)" NetClass="AssemblyDemo" NetMethod="MultiplyNumbers" NetAssembly="C:\Projects\CodePlex\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll">
    ///             <Output TaskParameter="Result" PropertyName="R"/>
    ///         </MSBuild.ExtensionPack.Framework.Assembly>
    ///         <Message Text="Result: $(R)"/>
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="Invoke" NetArguments="@(ArgsF)" NetClass="AssemblyDemo" NetMethod="CreateFolder" NetAssembly="C:\Projects\CodePlex\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll"/>
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="Invoke" NetClass="AssemblyDemo" NetMethod="CreateDefaultFolder" NetAssembly="C:\Projects\CodePlex\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll"/>
    ///         <!-- Extract some information on the assembly interface -->
    ///         <MSBuild.ExtensionPack.Framework.Assembly TaskAction="GetMethodInfo" NetAssembly="C:\Projects\CodePlex\MSBuildExtensionPack\Solutions\Main3.5\SampleScratchpad\SampleBuildBinaries\AssemblyDemo.dll">
    ///             <Output TaskParameter="OutputItems" ItemName="TypeInfo"/>
    ///         </MSBuild.ExtensionPack.Framework.Assembly>
    ///         <Message Text="%(TypeInfo.Identity) %(TypeInfo.Parameters)" />
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Assembly : BaseTask
    {
        private System.Reflection.Assembly assembly;
        private List<ITaskItem> outputItems;

        /// <summary>
        /// Sets the name of the Assembly
        /// </summary>
        [Required]
        public string NetAssembly { get; set; }

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
        /// Gets the outputitems. For a call to GetMethodInfo, the outputitems are in the following format:
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

            this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Loading Assembly: {0}", this.NetAssembly));
            this.assembly = System.Reflection.Assembly.LoadFrom(this.NetAssembly);

            switch (this.TaskAction)
            {
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

        private void GetMethodInfo()
        {
            this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Getting MethodInfo for: {0}", this.NetAssembly));
            this.outputItems = new List<ITaskItem>();
            foreach (Type type in this.assembly.GetTypes())
            {
                this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Found Type: {0}", this.NetClass));
                ITaskItem t = new TaskItem(type.Name);
                string paras = string.Empty;
                foreach (MethodInfo mi in type.GetMethods())
                {
                    foreach (ParameterInfo pi in mi.GetParameters())
                    {
                        this.Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Found Parameter: {0}", pi.Name));
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
            foreach (Type type in this.assembly.GetTypes())
            {
                if (type.IsClass && type.Name == this.NetClass)
                {
                    this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Found Type: {0}", this.NetClass));
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
                    
                    this.Log.LogMessage(string.Format(CultureInfo.CurrentCulture, "Invoking: {0}", this.NetMethod));
    
                    if (this.NetMethod == null)
                    {
                        // allows call to the default constructor
                        this.NetMethod = string.Empty;                    
                    }

                    object result = type.InvokeMember(this.NetMethod, BindingFlags.Default | BindingFlags.InvokeMethod, null, Activator.CreateInstance(type), arguments, CultureInfo.CurrentCulture);

                    if (!(result == null))
                    {
                        this.Result = result.ToString();
                    }
                }
            }

            if (!typeFound)
            {
                Log.LogError(string.Format(CultureInfo.CurrentCulture, "Type not Found: {0}", this.NetClass));
                return;
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