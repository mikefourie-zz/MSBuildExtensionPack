//-----------------------------------------------------------------------
// <copyright file="Metadata.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Add</i> (<b>Required:</b> Items, NewMetadata <b>Output: </b> NewItems)</para>
    /// <para><b>Remote Execution Support:</b> NA</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project xmlns="http:///schemas.microsoft.com/developer/msbuild/2003" DefaultTargets="Demo">
    ///  <PropertyGroup>
    ///    <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///    <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///  </PropertyGroup>
    ///  <Import Project="$(TPath)"/>
    ///  <ItemGroup>
    ///    <Server Include="dev01;dev02;dev03">
    ///      <DbServer>dev-db01</DbServer>
    ///    </Server>
    ///  </ItemGroup>
    ///  <Target Name="Demo">
    ///    <MSBuild.ExtensionPack.Framework.Metadata TaskAction="Add" Items="@(Server)" NewMetadata="Source=server01;Dest=server02">
    ///      <!-- No way to change the existing item, only to make a new one. -->
    ///      <Output ItemName="Server2" TaskParameter="ResultItems"/>
    ///    </MSBuild.ExtensionPack.Framework.Metadata>
    ///    <Message Text="Result:%0d%0a@(Server2->'%(Identity)=Source: %(Source) Dest: %(Dest) DbServer: %(DbServer)','%0d%0a')"/>
    ///  </Target>
    /// </Project>
    /// ]]></code>
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.4.0/html/469bf63a-ec25-9248-f956-7bf87e364cbd.htm")]
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
        [TaskAction(AddTaskAction, true)]
        public ITaskItem[] Items { get; set; }

        /// <summary>
        /// Sets the string which contains the metadata.<br/>
        /// This should be in the format <i>n1=v1;n2=v2;...</i>
        /// </summary>
        [Required]
        [TaskAction(AddTaskAction, true)]
        public string NewMetadata { get; set; }
        
        /// <summary>
        /// Gets the item which contains the result.
        /// </summary>
        [Output]
        [TaskAction(AddTaskAction, false)]
        public ITaskItem[] ResultItems { get; protected set; }

        protected override void InternalExecute()
        {
            if (string.Compare(this.TaskAction, AddTaskAction, StringComparison.OrdinalIgnoreCase) != 0)
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                return;
            }

            if (this.Items == null || this.Items.Length <= 0)
            {
                this.LogTaskWarning("No items to attach metadata to");
                return;
            }

            if (string.IsNullOrEmpty(this.NewMetadata))
            {
                this.LogTaskWarning("No metadata to attach to items");
                return;
            }

            IDictionary<string, string> metadataBag = ParseParameters(this.NewMetadata);
            if (metadataBag.Count <= 0)
            {
                this.LogTaskWarning("No metadata to attach to items");
                return;
            }

            this.ResultItems = new ITaskItem[this.Items.Length];
            for (int i = 0; i < this.ResultItems.Length; i++)
            {
                ITaskItem newItem = new TaskItem(this.Items[i]);
                this.ResultItems[i] = newItem;
                this.Items[i].CopyMetadataTo(newItem);
                foreach (string metadataName in metadataBag.Keys)
                {
                    string metadataValue = metadataBag[metadataName];
                    if (string.IsNullOrEmpty(metadataName)
                        || string.IsNullOrEmpty(metadataValue))
                    {
                        continue;
                    }

                    newItem.SetMetadata(metadataName, metadataValue);

                    // Need to set this so it is not reset
                    newItem.SetMetadata("RecursiveDir", this.Items[i].GetMetadata("RecursiveDir"));
                }
            }
        }

        /// <summary>
        /// Can be used to create a dictionary with all the key/value pairs 
        /// that are contained in <c>parameters</c>.
        /// </summary>
        /// <param name="parameters">string to parse</param>
        /// <returns>IDictionary</returns>
        private static IDictionary<string, string> ParseParameters(string parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            IDictionary<string, string> paramaterBag = new Dictionary<string, string>();
            if (parameters.Length <= 0)
            {
                return paramaterBag;
            }

            foreach (string paramString in parameters.Split(";".ToCharArray()))
            {
                string[] keyValue = paramString.Split("=".ToCharArray());
                if (keyValue.Length < 2)
                {
                    continue;
                }

                AddToParameters(paramaterBag, keyValue[0], keyValue[1]);
            }

            return paramaterBag;
        }

        private static void AddToParameters(IDictionary<string, string> parametersBag, string name, string value)
        {
            if (parametersBag == null)
            {
                throw new ArgumentNullException("parametersBag");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (parametersBag.ContainsKey(name))
            {
                parametersBag.Remove(name);
            }

            parametersBag.Add(name, value);
        }
    }
}
