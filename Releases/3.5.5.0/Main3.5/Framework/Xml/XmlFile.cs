//-----------------------------------------------------------------------
// <copyright file="XmlFile.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Xml
{
    using System.Globalization;
    using System.Xml;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddAttribute</i> (<b>Required: </b>File, Element)</para>
    /// <para><i>AddElement</i> (<b>Required: </b>File, Element, ParentElement, Key, Value)</para>
    /// <para><i>RemoveAttribute</i> (<b>Required: </b>File, Element, Key)</para>
    /// <para><i>RemoveElement</i> (<b>Required: </b>File, Element, ParentElement)</para>
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
    ///     <ItemGroup>
    ///         <ConfigSettingsToDeploy Include="c:\machine.config">
    ///             <Action>RemoveElement</Action>
    ///             <Element>processModel</Element>
    ///             <ParentElement>/configuration/system.web</ParentElement>
    ///         </ConfigSettingsToDeploy>
    ///         <ConfigSettingsToDeploy Include="c:\machine.config">
    ///             <Action>AddElement</Action>
    ///             <Element>processModel</Element>
    ///             <ParentElement>/configuration/system.web</ParentElement>
    ///         </ConfigSettingsToDeploy>
    ///         <ConfigSettingsToDeploy Include="c:\machine.config">
    ///             <Action>AddAttribute</Action>
    ///             <Key>enable</Key>
    ///             <ValueToAdd>true</ValueToAdd>
    ///             <Element>/configuration/system.web/processModel</Element>
    ///         </ConfigSettingsToDeploy>
    ///         <ConfigSettingsToDeploy Include="c:\machine.config">
    ///             <Action>AddAttribute</Action>
    ///             <Key>timeout</Key>
    ///             <ValueToAdd>Infinite</ValueToAdd>
    ///             <Element>/configuration/system.web/processModel</Element>
    ///         </ConfigSettingsToDeploy>
    ///         <ConfigSettingsToDeploy Include="c:\machine.config">
    ///             <Action>RemoveAttribute</Action>
    ///             <Key>timeout</Key>
    ///             <Element>/configuration/system.web/processModel</Element>
    ///         </ConfigSettingsToDeploy>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <MSBuild.ExtensionPack.Xml.XmlFile TaskAction="%(ConfigSettingsToDeploy.Action)" File="%(ConfigSettingsToDeploy.Identity)" Key="%(ConfigSettingsToDeploy.Key)" Value="%(ConfigSettingsToDeploy.ValueToAdd)" Element="%(ConfigSettingsToDeploy.Element)" ParentElement="%(ConfigSettingsToDeploy.ParentElement)" Condition="'%(ConfigSettingsToDeploy.Identity)'!=''"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.4.0/html/4009fe8c-73c1-154f-ee8c-e9fda7f5fd96.htm")]
    public class XmlFile : BaseTask
    {
        private const string AddAttributeTaskAction = "AddAttribute";
        private const string AddElementTaskAction = "AddElement";
        private const string RemoveAttributeTaskAction = "RemoveAttribute";
        private const string RemoveElementTaskAction = "RemoveElement";
        private XmlDocument xmlFileDoc;

        [DropdownValue(AddAttributeTaskAction)]
        [DropdownValue(AddElementTaskAction)]
        [DropdownValue(RemoveAttributeTaskAction)]
        [DropdownValue(RemoveElementTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the element.
        /// </summary>
        [TaskAction(AddAttributeTaskAction, true)]
        [TaskAction(AddElementTaskAction, true)]
        [TaskAction(RemoveAttributeTaskAction, true)]
        [TaskAction(RemoveElementTaskAction, true)]
        [Required]
        public string Element { get; set; }

        /// <summary>
        /// Sets the parent element.
        /// </summary>
        [TaskAction(AddElementTaskAction, true)]
        [TaskAction(RemoveElementTaskAction, true)]
        public string ParentElement { get; set; }

        /// <summary>
        /// Sets the key.
        /// </summary>
        [TaskAction(AddAttributeTaskAction, true)]
        [TaskAction(RemoveAttributeTaskAction, true)]
        public string Key { get; set; }

        /// <summary>
        /// Sets the key value.
        /// </summary>
        [TaskAction(AddAttributeTaskAction, true)]
        public string Value { get; set; }

        /// <summary>
        /// Sets the file.
        /// </summary>
        [Required]
        [TaskAction(AddAttributeTaskAction, true)]
        [TaskAction(AddElementTaskAction, true)]
        [TaskAction(RemoveAttributeTaskAction, true)]
        [TaskAction(RemoveElementTaskAction, true)]
        public ITaskItem File { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (!System.IO.File.Exists(this.File.ItemSpec))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "File not found: {0}", this.File.ItemSpec));
                return;
            }

            this.xmlFileDoc = new XmlDocument();
            this.xmlFileDoc.Load(this.File.ItemSpec);

            switch (this.TaskAction)
            {
                case AddElementTaskAction:
                    this.AddElement();
                    break;
                case AddAttributeTaskAction:
                    this.AddAttribute();
                    break;
                case RemoveAttributeTaskAction:
                    this.RemoveAttribute();
                    break;
                case RemoveElementTaskAction:
                    this.RemoveElement();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void RemoveAttribute()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Remove Attribute: {0} from {1}", this.Key, this.File.ItemSpec));

            XmlNode elementNode = this.xmlFileDoc.SelectSingleNode(this.Element);
            if (elementNode == null)
            {
                Log.LogError(string.Format(CultureInfo.CurrentUICulture, "Element not found: {0}", this.Element));
                return;
            }

            XmlAttribute attNode = elementNode.Attributes.GetNamedItem(this.Key) as XmlAttribute;
            if (attNode != null)
            {
                elementNode.Attributes.Remove(attNode);
                this.xmlFileDoc.Save(this.File.ItemSpec);
            }
        }

        private void AddAttribute()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Set Attribute: {0}={1} for {2}", this.Key, this.Value, this.File.ItemSpec));

            this.xmlFileDoc.Save(this.File.ItemSpec);

            XmlNode elementNode = this.xmlFileDoc.SelectSingleNode(this.Element);
            if (elementNode == null)
            {
                Log.LogError(string.Format(CultureInfo.CurrentUICulture, "Element not found: {0}", this.Element));
                return;
            }

            XmlAttribute attNode = elementNode.Attributes.GetNamedItem(this.Key) as XmlAttribute;
            if (attNode == null)
            {
                attNode = this.xmlFileDoc.CreateAttribute(this.Key);
                attNode.Value = this.Value;
                elementNode.Attributes.Append(attNode);
            }
            else
            {
                attNode.Value = this.Value;
            }

            this.xmlFileDoc.Save(this.File.ItemSpec);
        }

        private void AddElement()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Add Element: {0} to {1}", this.Element, this.File.ItemSpec));

            XmlNode parentNode = this.xmlFileDoc.SelectSingleNode(this.ParentElement);
            if (parentNode == null)
            {
                Log.LogError("ParentElement not found: " + this.ParentElement);
                return;
            }

            // Ensure node does not already exist
            XmlNode newNode = this.xmlFileDoc.SelectSingleNode(this.ParentElement + "/" + this.Element);
            if (newNode == null)
            {
                parentNode.AppendChild(this.xmlFileDoc.CreateElement(this.Element));
                this.xmlFileDoc.Save(this.File.ItemSpec);
            }
        }

        private void RemoveElement()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Remove Element: {0} from {1}", this.Element, this.File.ItemSpec));

            XmlNode parentNode = this.xmlFileDoc.SelectSingleNode(this.ParentElement);
            if (parentNode == null)
            {
                Log.LogError("ParentElement not found: " + this.ParentElement);
                return;
            }

            XmlNode nodeToRemove = this.xmlFileDoc.SelectSingleNode(this.ParentElement + "/" + this.Element);
            if (nodeToRemove != null)
            {
                parentNode.RemoveChild(nodeToRemove);
                this.xmlFileDoc.Save(this.File.ItemSpec);
            }
        }
    }
}