//-----------------------------------------------------------------------
// <copyright file="XmlFileLogger.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Loggers
{
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// <para>This logger can be used to log in xml format</para>
    /// <para><b>Syntax: </b></para>
    /// <para>     /l:XmlFileLogger,MSBuild.ExtensionPack.Loggers.dll;logfile=YOURLOGFILE;verbosity=YOURVERBOSITY;encoding=YOURENCODING</para>
    /// <para><b>Parameters: </b></para>
    /// <para>Logfile: A optional parameter that specifies the file in which to store the log information. Defaults to msbuild.xml</para>
    /// <para>Verbosity: An optional parameter that overrides the global verbosity setting for this file logger only. This enables you to log to several loggers, each with a different verbosity. The verbosity setting is case sensitive.</para>
    /// <para>Encoding: An optional parameter that specifies the encoding for the file, for example, UTF-8.</para>
    /// </summary>
    public class XmlFileLogger : Logger
    {
        private static readonly char[] FileLoggerParameterDelimiters = new[] { ';' };
        private static readonly char[] FileLoggerParameterValueSplitCharacter = new[] { '=' };
        private XmlTextWriter xmlWriter;
        private string logFileName;
        private Encoding encoding;
        private int warnings;
        private int errors;
        private DateTime startTime;

        /// <summary>
        /// Initialize Override
        /// </summary>
        /// <param name="eventSource">IEventSource</param>
        public override void Initialize(IEventSource eventSource)
        {
            this.logFileName = "msbuild.xml";
            this.encoding = Encoding.Default;

            this.InitializeFileLogger();

            eventSource.BuildFinished += this.BuildFinished;
            eventSource.BuildStarted += this.BuildStarted;
            eventSource.ErrorRaised += this.ErrorRaised;
            eventSource.WarningRaised += this.WarningRaised;

            if (this.Verbosity != LoggerVerbosity.Quiet)
            {
                eventSource.MessageRaised += this.MessageRaised;
                eventSource.CustomEventRaised += this.CustomBuildEventRaised;
                eventSource.ProjectStarted += this.ProjectStarted;
                eventSource.ProjectFinished += this.ProjectFinished;
            }

            if (this.IsVerbosityAtLeast(LoggerVerbosity.Normal))
            {
                eventSource.TargetStarted += this.TargetStarted;
                eventSource.TargetFinished += this.TargetFinished;
            }

            if (this.IsVerbosityAtLeast(LoggerVerbosity.Detailed))
            {
                eventSource.TaskStarted += this.TaskStarted;
                eventSource.TaskFinished += this.TaskFinished;
            }
        }

        /// <summary>
        /// Shutdown() is guaranteed to be called by MSBuild at the end of the build, after all 
        /// events have been raised.
        /// </summary>
        public override void Shutdown()
        {
            if (this.xmlWriter != null)
            {
                this.xmlWriter.Close();
            }
        }

        private static bool NotExpectedException(Exception e)
        {
            return ((!(e is UnauthorizedAccessException) && !(e is ArgumentNullException)) && (!(e is PathTooLongException) && !(e is DirectoryNotFoundException))) && ((!(e is NotSupportedException) && !(e is ArgumentException)) && (!(e is SecurityException) && !(e is IOException)));
        }

        private void ParseFileLoggerParameters()
        {
            if (this.Parameters != null)
            {
                string[] strArray = this.Parameters.Split(FileLoggerParameterDelimiters);
                foreach (string[] strArray2 in from t in strArray where t.Length > 0 select t.Split(FileLoggerParameterValueSplitCharacter))
                {
                    this.ApplyFileLoggerParameter(strArray2[0], strArray2.Length > 1 ? strArray2[1] : null);
                }
            }
        }

        private void ApplyFileLoggerParameter(string parameterName, string parameterValue)
        {
            switch (parameterName.ToUpperInvariant())
            {
                case "LOGFILE":
                    this.logFileName = parameterValue;
                    break;
                case "VERBOSITY":
                    this.Verbosity = (LoggerVerbosity)Enum.Parse(typeof(LoggerVerbosity), parameterValue);
                    break;
                case "ENCODING":
                    try
                    {
                        this.encoding = Encoding.GetEncoding(parameterValue);
                    }
                    catch (ArgumentException exception)
                    {
                        throw new LoggerException(exception.Message, exception.InnerException, "MSB4128", null);
                    }

                    break;
                case null:
                    return;
            }
        }

        private void InitializeFileLogger()
        {
            this.ParseFileLoggerParameters();
            try
            {
                this.xmlWriter = new XmlTextWriter(this.logFileName, this.encoding) { Formatting = Formatting.Indented };
                this.xmlWriter.WriteStartDocument();
                this.xmlWriter.WriteStartElement("build");
                this.xmlWriter.Flush();
            }
            catch (Exception exception)
            {
                if (NotExpectedException(exception))
                {
                    throw;
                }

                string message = string.Format(CultureInfo.InvariantCulture, "Invalid File Logger File {0}. {1}", this.logFileName, exception.Message);
                if (this.xmlWriter != null)
                {
                    this.xmlWriter.Close();
                }

                throw new LoggerException(message, exception.InnerException);
            }
        }

        private void BuildFinished(object sender, BuildFinishedEventArgs e)
        {
            this.xmlWriter.WriteStartElement("warnings");
            this.xmlWriter.WriteValue(this.warnings);
            this.xmlWriter.WriteEndElement();
            this.xmlWriter.WriteStartElement("errors");
            this.xmlWriter.WriteValue(this.errors);
            this.xmlWriter.WriteEndElement();
            this.xmlWriter.WriteStartElement("starttime");
            this.xmlWriter.WriteValue(this.startTime.ToString(CultureInfo.CurrentCulture));
            this.xmlWriter.WriteEndElement();
            this.xmlWriter.WriteStartElement("endtime");
            this.xmlWriter.WriteValue(DateTime.UtcNow.ToString(CultureInfo.CurrentCulture));
            this.xmlWriter.WriteEndElement();
            this.xmlWriter.WriteStartElement("timeelapsed");
            TimeSpan s = DateTime.UtcNow - this.startTime;
            this.xmlWriter.WriteValue(string.Format(CultureInfo.InvariantCulture, "{0}", s));
            this.xmlWriter.WriteEndElement();
            this.LogFinished();
        }

        private void BuildStarted(object sender, BuildStartedEventArgs e)
        {
            this.startTime = DateTime.UtcNow;
            this.LogStarted("build", string.Empty, string.Empty);
        }

        private void ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            this.errors++;
            this.LogErrorOrWarning("error", e.Message, e.Code, e.File, e.LineNumber, e.ColumnNumber, e.Subcategory);
        }

        private void MessageRaised(object sender, BuildMessageEventArgs e)
        {
            this.LogMessage("message", e.Message, e.Importance);
        }

        private void ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            this.LogFinished();
        }

        private void ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            this.LogStarted("project", e.TargetNames, e.ProjectFile);
            if (this.IsVerbosityAtLeast(LoggerVerbosity.Diagnostic))
            {
                this.xmlWriter.WriteStartElement("InitialProperties");
                SortedDictionary<string, string> sortedProperties = new SortedDictionary<string, string>();
                foreach (DictionaryEntry k in e.Properties.Cast<DictionaryEntry>())
                {
                    sortedProperties.Add(k.Key.ToString(), k.Value.ToString());
                }

                foreach (var p in sortedProperties)
                {
                    this.xmlWriter.WriteStartElement(p.Key);
                    this.xmlWriter.WriteCData(p.Value);
                    this.xmlWriter.WriteEndElement();
                }

                this.xmlWriter.WriteEndElement();
            }
        }

        private void TargetFinished(object sender, TargetFinishedEventArgs e)
        {
            this.LogFinished();
        }

        private void TargetStarted(object sender, TargetStartedEventArgs e)
        {
            this.LogStarted("target", e.TargetName, string.Empty);
        }

        private void TaskFinished(object sender, TaskFinishedEventArgs e)
        {
            this.LogFinished();
        }

        private void TaskStarted(object sender, TaskStartedEventArgs e)
        {
            this.LogStarted("task", e.TaskName, e.ProjectFile);
        }

        private void WarningRaised(object sender, BuildWarningEventArgs e)
        {
            this.warnings++;
            this.LogErrorOrWarning("warning", e.Message, e.Code, e.File, e.LineNumber, e.ColumnNumber, e.Subcategory);
        }

        private void CustomBuildEventRaised(object sender, CustomBuildEventArgs e)
        {
            this.LogMessage("custom", e.Message, MessageImportance.Normal);
        }

        private void LogStarted(string elementName, string stageName, string file)
        {
            if (elementName != "build")
            {
                this.xmlWriter.WriteStartElement(elementName);
            }

            this.SetAttribute(elementName == "project" ? "targets" : "name", stageName);
            this.SetAttribute("file", file);
            this.SetAttribute("started", DateTime.UtcNow);
            this.xmlWriter.Flush();
        }

        private void LogFinished()
        {
            this.xmlWriter.WriteEndElement();
            this.xmlWriter.Flush();
        }

        private void LogErrorOrWarning(string messageType, string message, string code, string file, int line, int column, string subcategory)
        {
            this.xmlWriter.WriteStartElement(messageType);
            this.SetAttribute("code", code);
            this.SetAttribute("file", file);
            this.SetAttribute("line", line);
            this.SetAttribute("column", column);
            this.SetAttribute("subcategory", subcategory);
            this.SetAttribute("started", DateTime.UtcNow);
            this.WriteMessage(message, code != "Properties");
            this.xmlWriter.WriteEndElement();
        }

        private void LogMessage(string messageType, string message, MessageImportance importance)
        {
            if (importance == MessageImportance.Low && this.Verbosity != LoggerVerbosity.Detailed && this.Verbosity != LoggerVerbosity.Diagnostic)
            {
                return;
            }

            if (importance == MessageImportance.Normal && (this.Verbosity == LoggerVerbosity.Minimal || this.Verbosity == LoggerVerbosity.Quiet))
            {
                return;
            }

            this.xmlWriter.WriteStartElement(messageType);
            this.SetAttribute("importance", importance);
            this.SetAttribute("started", DateTime.UtcNow);
            this.WriteMessage(message, false);
            this.xmlWriter.WriteEndElement();
        }

        private void WriteMessage(string message, bool escape)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            // Avoid CDATA in CDATA
            message = message.Replace("<![CDATA[", "");
            message = message.Replace("]]>", "");

            message = message.Replace("&", "&amp;");
            if (escape)
            {
                message = message.Replace("<", "&lt;");
                message = message.Replace(">", "&gt;");
            }

            this.xmlWriter.WriteCData(message);
        }

        private void SetAttribute(string name, object value)
        {
            if (value == null)
            {
                return;
            }

            Type t = value.GetType();
            if (t == typeof(int))
            {
                int number;
                if (int.TryParse(value.ToString(), out number))
                {
                    this.xmlWriter.WriteAttributeString(name, number.ToString(CultureInfo.InvariantCulture));
                }
            }
            else if (t == typeof(bool))
            {
                this.xmlWriter.WriteAttributeString(name, value.ToString());
            }
            else if (t == typeof(MessageImportance))
            {
                MessageImportance importance = (MessageImportance)value;
                this.xmlWriter.WriteAttributeString(name, importance.ToString());
            }
            else
            {
                string text = value.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    this.xmlWriter.WriteAttributeString(name, text);
                }
            }
        }
    }
}
