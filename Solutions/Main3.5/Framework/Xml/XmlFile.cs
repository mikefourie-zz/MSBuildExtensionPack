//-----------------------------------------------------------------------
// <copyright file="XmlFile.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// Portions of this task are based on the http://www.codeplex.com/sdctasks. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Xml
{
    using System.Globalization;
    using System.Xml;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>AddAttribute</i> (<b>Required: </b>File, Element or XPath, Key, Value)</para>
    /// <para><i>AddElement</i> (<b>Required: </b>File, Element and ParentElement or Element and XPath, <b>Optional:</b> Key, Value)</para>
    /// <para><i>RemoveAttribute</i> (<b>Required: </b>File, Element or XPath, Key)</para>
    /// <para><i>RemoveElement</i> (<b>Required: </b>File, Element and ParentElement or Element and XPath)</para>
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
    ///         <XMLConfigElementsToAdd Include="c:\machine.config">
    ///             <XPath>/configuration/configSections</XPath>
    ///             <Name>section</Name>
    ///             <KeyAttributeName>name</KeyAttributeName>
    ///             <KeyAttributeValue>enterpriseLibrary.ConfigurationSource</KeyAttributeValue>
    ///         </XMLConfigElementsToAdd>
    ///         <XMLConfigElementsToAdd Include="c:\machine.config">
    ///             <XPath>/configuration</XPath>
    ///             <Name>enterpriseLibrary.ConfigurationSource</Name>
    ///             <KeyAttributeName>selectedSource</KeyAttributeName>
    ///             <KeyAttributeValue>MyKeyAttribute</KeyAttributeValue>
    ///         </XMLConfigElementsToAdd>
    ///         <XMLConfigElementsToAdd Include="c:\machine.config">
    ///             <XPath>/configuration/enterpriseLibrary.ConfigurationSource</XPath>
    ///             <Name>sources</Name>
    ///         </XMLConfigElementsToAdd>
    ///         <XMLConfigElementsToAdd Include="c:\machine.config">
    ///             <XPath>/configuration/enterpriseLibrary.ConfigurationSource/sources</XPath>
    ///             <Name>add</Name>
    ///             <KeyAttributeName>name</KeyAttributeName>
    ///             <KeyAttributeValue>MyKeyAttribute</KeyAttributeValue>
    ///         </XMLConfigElementsToAdd>
    ///         <XMLConfigAttributesToAdd Include="c:\machine.config">
    ///             <XPath>/configuration/configSections/section[@name='enterpriseLibrary.ConfigurationSource']</XPath>
    ///             <Name>type</Name>
    ///             <Value>Microsoft.Practices.EnterpriseLibrary.Common.Configuration.ConfigurationSourceSection, Microsoft.Practices.EnterpriseLibrary.Common, Version=4.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35</Value>
    ///         </XMLConfigAttributesToAdd>
    ///         <XMLConfigAttributesToAdd Include="c:\machine.config">
    ///             <XPath>/configuration/enterpriseLibrary.ConfigurationSource/sources/add[@name='MyKeyAttribute']</XPath>
    ///             <Name>type</Name>
    ///             <Value>MyKeyAttribute.Common, MyKeyAttribute.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=fb2f49125f05d89</Value>
    ///         </XMLConfigAttributesToAdd>
    ///         <XMLConfigElementsToDelete Include="c:\machine.config">
    ///             <XPath>/configuration/configSections/section[@name='enterpriseLibrary.ConfigurationSource']</XPath>
    ///         </XMLConfigElementsToDelete>
    ///         <XMLConfigElementsToDelete Include="c:\machine.config">
    ///             <XPath>/configuration/enterpriseLibrary.ConfigurationSource[@selectedSource='MyKeyAttribute']</XPath>
    ///         </XMLConfigElementsToDelete>
    ///     </ItemGroup>
    ///     <Target Name="Default">
    ///         <!-- Work through some manipulations that don't use XPath-->
    ///         <MSBuild.ExtensionPack.Xml.XmlFile TaskAction="%(ConfigSettingsToDeploy.Action)" File="%(ConfigSettingsToDeploy.Identity)" Key="%(ConfigSettingsToDeploy.Key)" Value="%(ConfigSettingsToDeploy.ValueToAdd)" Element="%(ConfigSettingsToDeploy.Element)" ParentElement="%(ConfigSettingsToDeploy.ParentElement)" Condition="'%(ConfigSettingsToDeploy.Identity)'!=''"/>
    ///         <!-- Work through some manipulations that use XPath-->
    ///         <MSBuild.ExtensionPack.Xml.XmlFile TaskAction="RemoveElement" File="%(XMLConfigElementsToDelete.Identity)" XPath="%(XMLConfigElementsToDelete.XPath)" Condition="'%(XMLConfigElementsToDelete.Identity)'!=''"/>
    ///         <MSBuild.ExtensionPack.Xml.XmlFile TaskAction="AddElement" File="%(XMLConfigElementsToAdd.Identity)" Key="%(XMLConfigElementsToAdd.KeyAttributeName)" Value="%(XMLConfigElementsToAdd.KeyAttributeValue)" Element="%(XMLConfigElementsToAdd.Name)" XPath="%(XMLConfigElementsToAdd.XPath)" Condition="'%(XMLConfigElementsToAdd.Identity)'!=''"/>
    ///         <MSBuild.ExtensionPack.Xml.XmlFile TaskAction="AddAttribute" File="%(XMLConfigAttributesToAdd.Identity)" Key="%(XMLConfigAttributesToAdd.Name)" Value="%(XMLConfigAttributesToAdd.Value)" XPath="%(XMLConfigAttributesToAdd.XPath)" Condition="'%(XMLConfigAttributesToAdd.Identity)'!=''"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.6.0/html/4009fe8c-73c1-154f-ee8c-e9fda7f5fd96.htm")]
    public class XmlFile : BaseTask
    {
        private const string AddAttributeTaskAction = "AddAttribute";
        private const string AddElementTaskAction = "AddElement";
        private const string RemoveAttributeTaskAction = "RemoveAttribute";
        private const string RemoveElementTaskAction = "RemoveElement";
        private XmlDocument xmlFileDoc;
        private XmlNamespaceManager namespaceManager;
        private XmlNodeList elements;

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
        /// Specifies the XPath to be used
        /// </summary>
        public string XPath { get; set; }

        /// <summary>
        /// TaskItems specifiying "Prefix" and "Uri" attributes for use with the specified XPath
        /// </summary>
        public ITaskItem[] Namespaces { get; set; }

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
            if (!string.IsNullOrEmpty(this.XPath))
            {
                this.namespaceManager = this.GetNamespaceManagerForDoc();
                this.elements = this.xmlFileDoc.SelectNodes(this.XPath, this.namespaceManager);
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "XmlFile: {0}", this.File.ItemSpec));
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
            if (string.IsNullOrEmpty(this.XPath))
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Remove Attribute: {0}", this.Key));
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
            else
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Remove Attribute: {0}", this.Key));
                if (this.elements != null && this.elements.Count > 0)
                {
                    foreach (XmlNode element in this.elements)
                    {
                        XmlAttribute attNode = element.Attributes.GetNamedItem(this.Key) as XmlAttribute;
                        if (attNode != null)
                        {
                            element.Attributes.Remove(attNode);
                            this.xmlFileDoc.Save(this.File.ItemSpec);
                        }
                    }

                    this.xmlFileDoc.Save(this.File.ItemSpec);
                }
            }
        }

        private void AddAttribute()
        {
            if (string.IsNullOrEmpty(this.XPath))
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Set Attribute: {0}={1}", this.Key, this.Value));
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
            else
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Set Attribute: {0}={1}", this.Key, this.Value));
                if (this.elements != null && this.elements.Count > 0)
                {
                    foreach (XmlNode element in this.elements)
                    {
                        XmlNode attrib = element.Attributes[this.Key] ?? element.Attributes.Append(this.xmlFileDoc.CreateAttribute(this.Key));
                        attrib.Value = this.Value;
                    }

                    this.xmlFileDoc.Save(this.File.ItemSpec);
                }
            }
        }

        private XmlNamespaceManager GetNamespaceManagerForDoc()
        {
            XmlNamespaceManager localnamespaceManager = new XmlNamespaceManager(this.xmlFileDoc.NameTable);

            // If we have had namespace declarations specified add them to the Namespace Mgr for the XML Document.
            if (this.Namespaces != null && this.Namespaces.Length > 0)
            {
                foreach (ITaskItem item in this.Namespaces)
                {
                    string prefix = item.GetMetadata("Prefix");
                    string uri = item.GetMetadata("Uri");

                    localnamespaceManager.AddNamespace(prefix, uri);
                }
            }

            return localnamespaceManager;
        }

        private void AddElement()
        {
            if (string.IsNullOrEmpty(this.XPath))
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Add Element: {0}", this.Element));
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
                    newNode = this.xmlFileDoc.CreateElement(this.Element);

                    if (!string.IsNullOrEmpty(this.Key))
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Add Attribute: {0} to: {1}", this.Key, this.Element));

                        XmlAttribute attNode = this.xmlFileDoc.CreateAttribute(this.Key);
                        attNode.Value = this.Value;
                        newNode.Attributes.Append(attNode);
                    }

                    parentNode.AppendChild(newNode);
                    this.xmlFileDoc.Save(this.File.ItemSpec);
                }
            }
            else
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Add Element: {0}", this.XPath));
                if (this.elements != null && this.elements.Count > 0)
                {
                    foreach (XmlNode element in this.elements)
                    {
                        XmlNode newNode = this.xmlFileDoc.CreateElement(this.Element);
                        if (!string.IsNullOrEmpty(this.Key))
                        {
                            this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Add Attribute: {0} to: {1}", this.Key, this.Element));

                            XmlAttribute attNode = this.xmlFileDoc.CreateAttribute(this.Key);
                            attNode.Value = this.Value;
                            newNode.Attributes.Append(attNode);
                        }

                        element.AppendChild(newNode);
                    }

                    this.xmlFileDoc.Save(this.File.ItemSpec);
                }
            }
        }

        private void RemoveElement()
        {
            if (string.IsNullOrEmpty(this.XPath))
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Remove Element: {0}", this.Element));
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
            else
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentUICulture, "Remove Element: {0}", this.XPath));
                if (this.elements != null && this.elements.Count > 0)
                {
                    foreach (XmlNode element in this.elements)
                    {
                        element.ParentNode.RemoveChild(element);
                    }

                    this.xmlFileDoc.Save(this.File.ItemSpec);
                }
            }
        }
    }
}