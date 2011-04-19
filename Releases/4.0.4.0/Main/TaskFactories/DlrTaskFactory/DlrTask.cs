//-----------------------------------------------------------------------
// <copyright file="DlrTask.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// This task is based on code from (http://github.com/jredville/DlrTaskFactory). It is used here with permission.
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.TaskFactory
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Xml.Linq;
    using IronRuby.Runtime;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Scripting.Hosting;

    /// <summary>
    /// A task that executes a custom script.
    /// </summary>
    /// <remarks>
    /// This task can implement <see cref="IGeneratedTask"/> to support task properties
    /// that are defined in the script itself and not known at compile-time of this task factory.
    /// </remarks>
    internal class DlrTask : Task, IDisposable, IGeneratedTask
    {
        private readonly XElement xelement;
        private readonly string language;
        private readonly ScriptEngine engine;
        private readonly dynamic scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="DlrTask"/> class.
        /// </summary>
        /// <param name="factory">The Factory</param>
        /// <param name="xmlElement">The XElement</param>
        /// <param name="taskFactoryLoggingHost">The taskFactoryLoggingHost</param>
        internal DlrTask(DlrTaskFactory factory, XElement xmlElement, IBuildEngine taskFactoryLoggingHost)
        {
            Contract.Requires(factory != null);
            Contract.Requires(xmlElement != null);
            Contract.Requires(taskFactoryLoggingHost != null);

            this.xelement = xmlElement;
            this.language = GetLanguage(xmlElement);

            var srs = new ScriptRuntimeSetup();
            srs.LanguageSetups.Add(IronRuby.Ruby.CreateRubySetup());
            srs.LanguageSetups.Add(IronPython.Hosting.Python.CreateLanguageSetup(null));
            var runtime = new ScriptRuntime(srs);
            this.engine = runtime.GetEngineByFileExtension(this.language);
            this.scope = this.engine.CreateScope();
            this.scope.log = this.Log;
        }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        /// <returns>
        /// true if the task successfully executed; otherwise, false.
        /// </returns>
        public override bool Execute()
        {
            try
            {
                this.engine.Execute(this.xelement.Value, this.scope);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the property value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The value of the property.</returns>
        public object GetPropertyValue(TaskPropertyInfo property)
        {
            Contract.Requires(property != null);

            dynamic res;
            dynamic variable = this.scope.GetVariable(RubyUtils.HasMangledName(property.Name) ? RubyUtils.TryMangleName(property.Name) : property.Name);
            variable.TryGetValue(out res);
            return res;
        }

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value to set.</param>
        public void SetPropertyValue(TaskPropertyInfo property, object value)
        {
            Contract.Requires(property != null);
            ((ScriptScope)this.scope).SetVariable(property.Name, value);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose of referenced objects implementing IDisposable here.
            }
        }

        private static string GetLanguage(XElement taskXml)
        {
            return taskXml.Attribute("Language").Value;
        }
    }
}
