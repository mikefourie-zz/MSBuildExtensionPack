//-----------------------------------------------------------------------
// <copyright file="Xml.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Xml
{
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using System.Xml.Xsl;
    using Microsoft.Build.Framework;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Transform</i> (<b>Required: </b>Xml or XmlFile, XslTransform or XslTransformFile <b>Optional:</b> Indent, OmitXmlDeclaration, OutputFile, TextEncoding <b>Output: </b>Output)</para>
    /// <para><i>Validate</i> (<b>Required: </b>Xml or XmlFile, SchemaFiles <b>Output: </b>IsValid, Output)</para>
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
    ///         <ItemGroup>
    ///             <Schema Include="c:\Demo1\demo.xsd"/>
    ///         </ItemGroup>
    ///         <PropertyGroup>
    ///             <MyXml>
    ///                 &lt;![CDATA[
    ///                 <Parent>
    ///                     <Child1>Child1 data</Child1>
    ///                     <Child2>Child2 data</Child2>
    ///                 </Parent>]]&gt;
    ///             </MyXml>
    ///             <MyXsl>
    ///                 &lt;![CDATA[<?xml version='1.0'?>
    ///                 <xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0'>
    ///                     <xsl:template match='/Parent'>
    ///                         <Root>
    ///                             <C1>
    ///                                 <xsl:value-of select='Child1'/>
    ///                             </C1>
    ///                             <C2>
    ///                                 <xsl:value-of select='Child2'/>
    ///                             </C2>
    ///                         </Root>
    ///                     </xsl:template>
    ///                 </xsl:stylesheet>]]&gt;
    ///             </MyXsl>
    ///             <MyValidXml>
    ///                 &lt;![CDATA[
    ///                 <D>
    ///                     <Name full="Mike" type="3f3">
    ///                         <Place>aPlace</Place>
    ///                     </Name>
    ///                 </D>]]&gt;
    ///             </MyValidXml>
    ///         </PropertyGroup>
    ///         <!-- Validate an XmlFile -->
    ///         <MSBuild.ExtensionPack.Xml.XmlTask TaskAction="Validate" XmlFile="c:\Demo1\demo.xml" SchemaFiles="@(Schema)">
    ///             <Output PropertyName="Validated" TaskParameter="IsValid"/>
    ///             <Output PropertyName="Out" TaskParameter="Output"/>
    ///         </MSBuild.ExtensionPack.Xml.XmlTask>
    ///         <Message Text="Valid File: $(Validated)"/>
    ///         <Message Text="Output: $(Out)"/>
    ///         <!-- Validate a piece of Xml -->
    ///         <MSBuild.ExtensionPack.Xml.XmlTask TaskAction="Validate" Xml="$(MyValidXml)" SchemaFiles="@(Schema)">
    ///             <Output PropertyName="Validated" TaskParameter="IsValid"/>
    ///         </MSBuild.ExtensionPack.Xml.XmlTask>
    ///         <Message Text="Valid File: $(Validated)"/>
    ///         <!-- Transform an Xml file with an Xslt file -->
    ///         <MSBuild.ExtensionPack.Xml.XmlTask TaskAction="TransForm" XmlFile="C:\Demo1\XmlForTransform.xml" XslTransformFile="C:\Demo1\Transform.xslt">
    ///             <Output PropertyName="Out" TaskParameter="Output"/>
    ///         </MSBuild.ExtensionPack.Xml.XmlTask>
    ///         <Message Text="Transformed Xml: $(Out)"/>
    ///         <!-- Transfrom a piece of Xml with an Xslt file -->
    ///         <MSBuild.ExtensionPack.Xml.XmlTask TaskAction="TransForm" Xml="$(MyXml)" XslTransformFile="C:\Demo1\Transform.xslt">
    ///             <Output PropertyName="Out" TaskParameter="Output"/>
    ///         </MSBuild.ExtensionPack.Xml.XmlTask>
    ///         <Message Text="Transformed Xml: $(Out)"/>
    ///         <!-- Transfrom a piece of Xml with a piece of Xslt and write it out to a file with indented formatting -->
    ///         <MSBuild.ExtensionPack.Xml.XmlTask TaskAction="TransForm" Xml="$(MyXml)" XslTransform="$(MyXsl)" OutputFile="C:\newxml.xml" Indent="true">
    ///             <Output PropertyName="Out" TaskParameter="Output"/>
    ///         </MSBuild.ExtensionPack.Xml.XmlTask>
    ///         <Message Text="Transformed Xml: $(Out)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.1.0/html/3d383fd0-d8a7-4b93-3e03-39b48456dac1.htm")]
    public class XmlTask : BaseTask
    {
        private const string TransformTaskAction = "Transform";
        private const string ValidateTaskAction = "Validate";
        
        private XDocument xmlDoc;
        private Encoding fileEncoding = Encoding.UTF8;

        [DropdownValue(TransformTaskAction)]
        [DropdownValue(ValidateTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        /// <summary>
        /// Sets the XmlFile
        /// </summary>
        [TaskAction(TransformTaskAction, false)]
        [TaskAction(ValidateTaskAction, false)]
        public string XmlFile { get; set; }

        /// <summary>
        /// Sets the XslTransformFile
        /// </summary>
        [TaskAction(TransformTaskAction, false)]
        public string XslTransformFile { get; set; }

        /// <summary>
        /// Sets the XmlFile
        /// </summary>
        [TaskAction(TransformTaskAction, false)]
        [TaskAction(ValidateTaskAction, false)]
        public string Xml { get; set; }

        /// <summary>
        /// Sets the XslTransformFile
        /// </summary>
        [TaskAction(TransformTaskAction, false)]
        public string XslTransform { get; set; }

        /// <summary>
        /// Sets the OutputFile
        /// </summary>
        [TaskAction(TransformTaskAction, false)]
        public string OutputFile { get; set; }

        /// <summary>
        /// Sets the Schema Files collection
        /// </summary>
        [TaskAction(ValidateTaskAction, true)]
        public ITaskItem[] SchemaFiles { get; set; }

        /// <summary>
        /// Set the OmitXmlDeclaration option for TransForm. Default is False
        /// </summary>
        [TaskAction(TransformTaskAction, false)]
        public bool OmitXmlDeclaration { get; set; }

        /// <summary>
        /// Set the Indent option for TransForm. Default is False
        /// </summary>
        [TaskAction(TransformTaskAction, false)]
        public bool Indent { get; set; }

        /// <summary>
        /// Set the Encoding option for TransForm. Default is UTF8
        /// </summary>
        [TaskAction(TransformTaskAction, false)]
        public string TextEncoding
        {
            get { return this.fileEncoding.ToString(); }
            set { this.fileEncoding = System.Text.Encoding.GetEncoding(value); }
        }

        /// <summary>
        /// Gets whether an XmlFile is valid xml
        /// </summary>
        [Output]
        [TaskAction(ValidateTaskAction, false)]
        public bool IsValid { get; set; }

        /// <summary>
        /// Get the Output
        /// </summary>
        [Output]
        [TaskAction(ValidateTaskAction, false)]
        [TaskAction(TransformTaskAction, false)]
        public string Output { get; set; }

        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            if (!string.IsNullOrEmpty(this.XmlFile) && !File.Exists(this.XmlFile))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "XmlFile not found: {0}", this.XmlFile));
                return;
            }

            if (!string.IsNullOrEmpty(this.XmlFile))
            {
                // Load the XmlFile
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Loading XmlFile: {0}", this.XmlFile));
                this.xmlDoc = XDocument.Load(this.XmlFile);
            }
            else if (!string.IsNullOrEmpty(this.Xml))
            {
                // Load the Xml
                this.LogTaskMessage(MessageImportance.Low, "Loading Xml");
                this.xmlDoc = XDocument.Load(new StringReader(this.Xml));
            }
            else
            {
                this.Log.LogError("Xml or XmlFile must be specified");
                return;
            }

            switch (this.TaskAction)
            {
                case "TransForm":
                    this.Transform();
                    break;
                case "Validate":
                    this.Validate();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }
        }

        private void Transform()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Transforming: {0}", this.XmlFile));
            XDocument xslDoc;
            if (!string.IsNullOrEmpty(this.XslTransformFile) && !File.Exists(this.XslTransformFile))
            {
                this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "XslTransformFile not found: {0}", this.XslTransformFile));
                return;
            }

            if (!string.IsNullOrEmpty(this.XslTransformFile))
            {
                // Load the XslTransformFile
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Loading XslTransformFile: {0}", this.XslTransformFile));
                xslDoc = XDocument.Load(this.XslTransformFile);
            }
            else if (!string.IsNullOrEmpty(this.XslTransform))
            {
                // Load the XslTransform
                this.LogTaskMessage(MessageImportance.Low, "Loading XslTransform");
                xslDoc = XDocument.Load(new StringReader(this.XslTransform));
            }
            else
            {
                this.Log.LogError("XslTransform or XslTransformFile must be specified");
                return;
            }

            XDocument newxmlDoc = new XDocument();
            using (XmlWriter writer = newxmlDoc.CreateWriter())
            {
                // Load the style sheet.
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(XmlReader.Create(new StringReader(xslDoc.ToString())));

                // Execute the transform and output the results to a writer.
                xslt.Transform(this.xmlDoc.CreateReader(), writer);
            }

            this.Output = newxmlDoc.ToString();

            if (!string.IsNullOrEmpty(this.OutputFile))
            {
                XmlWriterSettings writerSettings = new XmlWriterSettings { Encoding = this.fileEncoding, Indent = this.Indent, OmitXmlDeclaration = this.OmitXmlDeclaration, CloseOutput = true };
                using (XmlWriter xw = XmlWriter.Create(this.OutputFile, writerSettings))
                {
                    if (xw != null)
                    {
                        newxmlDoc.WriteTo(xw);
                    }
                    else
                    {
                        Log.LogError("There was an error creating the XmlWriter for the OutputFile");
                        return;
                    }
                }
            }
        }

        private void Validate()
        {
            if (!string.IsNullOrEmpty(this.XmlFile))
            {
                this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Validating: {0}", this.XmlFile));
            }
            else
            {
                this.LogTaskMessage("Validating Xml");
            }

            XmlSchemaSet schemas = new XmlSchemaSet();
            foreach (ITaskItem i in this.SchemaFiles)
            {
                this.LogTaskMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, "Loading SchemaFile: {0}", i.ItemSpec));
                schemas.Add(string.Empty, i.ItemSpec);
            }

            bool errorEncountered = false;
            this.xmlDoc.Validate(
                schemas,
                (o, e) =>
                {
                    this.Output += e.Message;
                    this.Log.LogWarning("{0}", e.Message);
                    errorEncountered = true;
                });

            this.IsValid = errorEncountered ? false : true;
        }
    }
}