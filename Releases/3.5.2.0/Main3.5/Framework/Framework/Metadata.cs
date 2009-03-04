//-----------------------------------------------------------------------
// <copyright file="DateAndTime.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Globalization;

namespace MSBuild.ExtensionPack.Framework
{
    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Add</i></para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project xmlns="http:///schemas.microsoft.com/developer/msbuild/2003" DefaultTargets="Demo">
    ///
    ///  <PropertyGroup>
    ///    <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///    <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///  </PropertyGroup>
    ///  <Import Project="$(TPath)"/>
    ///
    ///  
    ///  <ItemGroup>
    ///    <Server Include="dev01;dev02;dev03">
    ///      <DbServer>dev-db01</DbServer>
    ///    </Server>
    ///  </ItemGroup>
    ///  
    ///  <Target Name="Demo">
    ///    <MSBuild.ExtensionPack.Framework.Metadata TaskAction="Add" Items="@(Server)"
    ///                  NewMetadata="Source=server01;Dest=server02">
    ///      <!-- No way to change the existing item, only to make a new one. -->
    ///      <Output ItemName="Server2" TaskParameter="ResultItems"/>
    ///    </MSBuild.ExtensionPack.Framework.Metadata>
    ///
    ///    <Message Text="Result:%0d%0a@(Server2->'%(Identity)=Source: %(Source) Dest: %(Dest) DbServer: %(DbServer)','%0d%0a')"/>
    ///    
    ///  </Target>
    ///
    ///</Project>
    /// ]]></code>
    /// </example>
    public class Metadata : BaseTask
    {
        private const string AddTaskAction = "Add";

        [DropdownValue(AddTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }
        /// <summary>
        /// Sets the source Items.
        /// </summary>
        [Required]
        public ITaskItem[] Items
        { get; set; }
        /// <summary>
        /// Sets the string which contains the metadata.<br/>
        /// This should be in the format <i>n1=v1;n2=v2;...</i>
        /// </summary>
        [Required]
        public string NewMetadata
        { get; set; }
        /// <summary>
        /// Gets the item which contains the result.
        /// </summary>
        [Output]
        public ITaskItem[] ResultItems
        { get; protected set; }

        protected override void InternalExecute()
        {
            if (string.Compare(TaskAction, AddTaskAction, StringComparison.OrdinalIgnoreCase) != 0)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                return;
            }

            if (Items == null || Items.Length <= 0)
            {
                Log.LogWarning("No items to attach metadata to", null);
                return;
            }

            if(string.IsNullOrEmpty(NewMetadata))
            {
                Log.LogWarning("No metadata to attach to items", null);
                return;
            }

            IDictionary<string, string> metadataBag = ParseParameters(this.NewMetadata);
            if (metadataBag.Count <= 0)
            {
                Log.LogWarning("No metadata to attach to items", null);
                return;
            }

            ResultItems = new ITaskItem[Items.Length];
            for (int i = 0; i < ResultItems.Length; i++)
            {
                ITaskItem newItem = new TaskItem(Items[i]);
                ResultItems[i] = newItem;
                Items[i].CopyMetadataTo(newItem);
                foreach (string metadataName in metadataBag.Keys)
                {
                    string metadataValue = metadataBag[metadataName];
                    if (string.IsNullOrEmpty(metadataName)
                        || string.IsNullOrEmpty(metadataValue))
                    {
                        continue;
                    }
                    newItem.SetMetadata(metadataName, metadataValue);
                    //Need to set this so it is not reset
                    newItem.SetMetadata("RecursiveDir", Items[i].GetMetadata("RecursiveDir"));
                }
            }
        }

        /// <summary>
        /// Can be used to create a dictionary with all the key/value pairs 
        /// that are contained in <c>parameters</c>.
        /// </summary>
        /// <param name="parameters">string to parse</param>
        /// <returns></returns>
        private static IDictionary<string, string> ParseParameters(string parameters)
        {
            if (parameters == null) { throw new ArgumentNullException("parameters"); }

            IDictionary<string, string> paramaterBag;
            paramaterBag = new Dictionary<string, string>();
            if (parameters == null || parameters.Length <= 0)
            {
                return paramaterBag;
            }
            foreach (string paramString in parameters.Split(";".ToCharArray()))
            {
                string[] keyValue = paramString.Split("=".ToCharArray());
                if (keyValue == null || keyValue.Length < 2)
                {
                    continue;
                }
                AddToParameters(paramaterBag, keyValue[0], keyValue[1]);
            }
            return paramaterBag;
        }
        private static void AddToParameters(IDictionary<string, string> paramBag, string name, string value)
        {
            if (paramBag == null) { throw new ArgumentNullException("paramBag"); }
            if (name == null) { throw new ArgumentNullException("name"); }
            if (value == null) { throw new ArgumentNullException("value"); }

            if (paramBag.ContainsKey(name))
            {
                paramBag.Remove(name);
            }
            paramBag.Add(name, value);
        }
    }
}
