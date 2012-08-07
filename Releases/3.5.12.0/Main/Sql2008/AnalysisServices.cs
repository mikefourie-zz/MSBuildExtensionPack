//-----------------------------------------------------------------------
// <copyright file="AnalysisServices.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Sql2008
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using Microsoft.AnalysisServices;
    using Microsoft.Build.Framework;
    using AMO = Microsoft.AnalysisServices;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>ScriptAlter</i> (<b>Required: </b>DatabaseItem, OutputFile)</para>
    /// <para><i>ScriptCreate</i> (<b>Required: </b>DatabaseItem, OutputFile)</para>
    /// <para><i>ScriptDelete</i> (<b>Required: </b>DatabaseItem, OutputFile)</para>
    /// <para><i>Execute</i> (<b>Required: </b>InputFile)</para>
    /// <para><i>Process</i> (<b>Required: </b>DatabaseItem <b>Optional: ProcessType</b>)</para>
    /// <para><b>Remote Execution Support:</b> Yes</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// ]]></code>    
    /// </example>
    [HelpUrl("http://www.msbuildextensionpack.com/help/3.5.11.0/html/88b4a474-46da-7cac-130e-37ad139a5aa1.htm")]
    public class AnalysisServices : BaseTask
    {
        private const string ScriptCreateTaskAction = "ScriptCreate";
        private const string ScriptAlterTaskAction = "ScriptAlter";
        private const string ScriptDeleteTaskAction = "ScriptDelete";
        private const string ExecuteTaskAction = "Execute";
        private const string ProcessTaskAction = "Process";
        private AMO.Server server;
        private ProcessType processType = Microsoft.AnalysisServices.ProcessType.ProcessDefault;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        [DropdownValue(ScriptCreateTaskAction)]
        [DropdownValue(ScriptAlterTaskAction)]
        [DropdownValue(ScriptDeleteTaskAction)]
        [DropdownValue(ProcessTaskAction)]
        public override string TaskAction
        {
            get
            {
                return base.TaskAction;
            }

            set
            {
                base.TaskAction = value;
            }
        }

        /// <summary>
        /// Set the target database.
        /// </summary>
        [TaskAction(ScriptCreateTaskAction, true)]
        [TaskAction(ScriptAlterTaskAction, true)]
        [TaskAction(ScriptDeleteTaskAction, true)]
        [TaskAction(ProcessTaskAction, true)]
        public ITaskItem DatabaseItem { get; set; }

        /// <summary>
        /// Sets the OutputFile 
        /// </summary>
        [TaskAction(ScriptCreateTaskAction, true)]
        [TaskAction(ScriptAlterTaskAction, true)]
        [TaskAction(ScriptDeleteTaskAction, true)]
        public ITaskItem OutputFile { get; set; }

        /// <summary>
        /// Sets the InputFile containing query to execute
        /// </summary>
        [TaskAction(ExecuteTaskAction, true)]
        public ITaskItem InputFile { get; set; }

        /// <summary>
        /// Sets the ProcessType (enum Microsoft.AnalysisServices.ProcessType). Default is ProcessDefault.
        /// </summary>
        [TaskAction(ProcessTaskAction, false)]
        public string ProcessType
        {
            get { return this.processType.ToString(); }
            set { this.processType = (ProcessType)Enum.Parse(typeof(ProcessType), value); }
        }
        
        /// <summary>
        /// Performs the action of this task.
        /// </summary>
        protected override void InternalExecute()
        {
            this.server = new AMO.Server();
            StringBuilder con = new StringBuilder(string.Format(CultureInfo.CurrentCulture, "Data Source={0};", this.MachineName));
            if (string.IsNullOrEmpty(this.UserName))
            {
                this.LogTaskMessage(MessageImportance.Low, "Using a Trusted Connection");
                con.Append("Integrated Security=SSPI;");
            }
            else
            {
                con.AppendFormat("UserName={0}", this.UserName);
            }

            if (!string.IsNullOrEmpty(this.UserPassword))
            {
                con.AppendFormat("Password={0}", this.UserPassword);
            }

            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Connect to server: {0}", this.MachineName));
            this.server.Connect(con.ToString());
            switch (this.TaskAction)
            {
                case ScriptCreateTaskAction:
                    this.Script((scripter, objects, xmlWriter, dependantObjects) => scripter.ScriptCreate(objects, xmlWriter, dependantObjects));
                    break;
                case ScriptAlterTaskAction:
                    this.Script((scripter, objects, xmlWriter, dependantObjects) => scripter.ScriptAlter(objects, xmlWriter, dependantObjects));
                    break;
                case ScriptDeleteTaskAction:
                    this.Script((scripter, objects, xmlWriter, dependantObjects) => scripter.ScriptDelete(objects, xmlWriter, dependantObjects));
                    break;
                case ProcessTaskAction:
                    this.Process();
                    break;
                case ExecuteTaskAction:
                    this.ExecuteScript();
                    break;
                default:
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "Invalid TaskAction passed: {0}", this.TaskAction));
                    return;
            }

            this.server.Disconnect();
        }

        private void Script(Action<Scripter, MajorObject[], XmlWriter, bool> scripterMethod)
        {
            if (this.OutputFile == null)
            {
                this.Log.LogError("OutputFilePath is required");
                return;
            }

            AMO.Database db = this.server.Databases[this.DatabaseItem.ItemSpec];
            string objectsToScriptMetadata = this.DatabaseItem.GetMetadata("ObjectsToScript");
            string[] objectsToScriptNames = !string.IsNullOrEmpty(objectsToScriptMetadata) ? objectsToScriptMetadata.Split(new[] { ';' }) : new string[0];

            using (FileStream fileStream = File.OpenWrite(this.OutputFile.GetMetadata("FullPath")))
            {
                XmlTextWriter xmlTextWriter = new XmlTextWriter(fileStream, Encoding.UTF8);
                List<MajorObject> objectsToScript = new List<MajorObject>();
                foreach (DataSourceView dataSourceView in db.DataSourceViews)
                {
                    if (objectsToScriptNames.Length == 0 || objectsToScriptNames.Contains(dataSourceView.Name))
                    {
                        this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Scripting DataSourceView: {0}", dataSourceView.Name));
                        objectsToScript.Add(dataSourceView);
                    }
                }

                foreach (Dimension dimension in db.Dimensions.Cast<Dimension>().Where(dimension => objectsToScriptNames.Length == 0 || objectsToScriptNames.Contains(dimension.Name)))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Scripting Dimension: {0}", dimension.Name));
                    objectsToScript.Add(dimension);
                }

                foreach (Cube cube in db.Cubes.Cast<Cube>().Where(cube => objectsToScriptNames.Length == 0 || objectsToScriptNames.Contains(cube.Name)))
                {
                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Scripting Cube: {0}", cube.Name));
                    objectsToScript.Add(cube);
                }

                Scripter scripter = new Scripter();
                scripterMethod(scripter, objectsToScript.ToArray(), xmlTextWriter, false);
                xmlTextWriter.Flush();
            }
        }

        private void ExecuteScript()
        {
            if (this.InputFile == null)
            {
                Log.LogError("InputFile is required");
                return;
            }

            using (StreamReader fileStream = File.OpenText(this.InputFile.GetMetadata("FullPath")))
            {
                XmlaResultCollection resultCollection = this.server.Execute(fileStream.ReadToEnd());

                foreach (XmlaResult xmlaResult in resultCollection)
                {
                    IEnumerable<XmlaError> errors = xmlaResult.Messages.OfType<XmlaError>();
                    IEnumerable<XmlaWarning> warnings = xmlaResult.Messages.OfType<XmlaWarning>();

                    foreach (XmlaWarning xmlaWarning in warnings)
                    {
                        this.LogTaskWarning(string.Format(CultureInfo.CurrentCulture, "XMLA warning code: {0}\nDescription: {1}\nSource: {2}\nHelpFile: {3}", xmlaWarning.WarningCode, xmlaWarning.Description, xmlaWarning.Source, xmlaWarning.HelpFile));
                    }

                    if (errors.FirstOrDefault() != default(XmlaError))
                    {
                        foreach (XmlaError xmlaError in errors)
                        {
                            Log.LogError(string.Format(CultureInfo.CurrentCulture, "XMLA error code: {0}\nDescription: {1}\nSource: {2}\nHelpFile: {3}", xmlaError.ErrorCode, xmlaError.Description, xmlaError.Source, xmlaError.HelpFile));
                        }

                        return;
                    }

                    this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "XMLA request executed with success ({0} warnings)", warnings.Count()));
                }
            }
        }

        private void Process()
        {
            this.LogTaskMessage(string.Format(CultureInfo.CurrentCulture, "Processing database {0} with process type {1}", this.DatabaseItem.ItemSpec, this.ProcessType));
            this.server.Databases[this.DatabaseItem.ItemSpec].Process(this.processType);
        }
    }
}
