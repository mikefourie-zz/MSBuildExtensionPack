//-----------------------------------------------------------------------
// <copyright file="SecureFileLogger.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Loggers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using MSBuild.ExtensionPack.Loggers.Extended;

    /// <summary>
    /// SecureFileLogger
    /// </summary>
    public class SecureFileLogger : Logger
    {
        private const char SecureChar = '#';
        private readonly ICollection<string> regExRules = new Collection<string>();
        private StreamWriter streamWriter;
        private int indent;
        private int warnings;
        private int errors;
        private DateTime startTime;

        /// <summary>
        /// Shutdown() is guaranteed to be called by MSBuild at the end of the build, after all 
        /// events have been raised.
        /// </summary>
        public override void Shutdown()
        {
            if (this.streamWriter != null)
            {
                this.streamWriter.Close();
            }
        }

        /// <summary>
        /// Initialize Override
        /// </summary>
        /// <param name="eventSource">IEventSource</param>
        public override void Initialize(IEventSource eventSource)
        {
            if (null == Parameters)
            {
                throw new LoggerException("Parameters not supplied.");
            }

            string[] parameters = Parameters.Split(';');
            if (parameters.Length > 3)
            {
                throw new LoggerException("Too many parameters passed.");
            }

            if (String.IsNullOrEmpty(parameters[0]))
            {
                throw new LoggerException("Log file was not set.");
            }

            if (String.IsNullOrEmpty(parameters[1]))
            {
                throw new LoggerException("Rule file was not set.");
            }

            if (parameters.Length > 2)
            {
                this.Verbosity = (LoggerVerbosity)Enum.Parse(typeof(LoggerVerbosity), parameters[2]);
            }

            FileInfo logFile = new FileInfo(parameters[0]);
            FileInfo ruleFile = new FileInfo(parameters[1]);
            
            if (!ruleFile.Exists)
            {
                throw new LoggerException("Rule file does not exist.");
            }

            using (StreamReader r = new StreamReader(ruleFile.FullName))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    this.regExRules.Add(line);
                }
            }

            try
            {
                this.streamWriter = new StreamWriter(logFile.FullName);
                this.streamWriter.AutoFlush = true;
            }
            catch (Exception ex)
            {
                if
                (ex is UnauthorizedAccessException
                    || ex is ArgumentNullException
                    || ex is PathTooLongException
                    || ex is DirectoryNotFoundException
                    || ex is NotSupportedException
                    || ex is ArgumentException
                    || ex is SecurityException
                    || ex is IOException)
                {
                    throw new LoggerException("Failed to create log file: " + ex.Message);
                }

                // Unexpected failure
                throw;
            }

            eventSource.BuildFinished += this.BuildFinished;
            eventSource.BuildStarted += this.BuildStarted;
            eventSource.ErrorRaised += this.ErrorRaised;
            eventSource.MessageRaised += this.MessageRaised;
            eventSource.ProjectStarted += this.ProjectStarted;
            eventSource.TargetStarted += this.TargetStarted;
            eventSource.WarningRaised += this.WarningRaised;
            if (IsVerbosityAtLeast(LoggerVerbosity.Detailed))
            {
                eventSource.ProjectFinished += this.ProjectFinished;
                eventSource.TargetFinished += this.TargetFinished;
                eventSource.TaskStarted += this.TaskStarted;
                eventSource.TaskFinished += this.TaskFinished;
            }
        }

        private void TaskFinished(object sender, TaskFinishedEventArgs e)
        {
            string line = String.Format(CultureInfo.InvariantCulture, "{0}", e.Message);
            this.WriteLine(line);
        }

        private void TaskStarted(object sender, TaskStartedEventArgs e)
        {
            string line = String.Format(CultureInfo.InvariantCulture, "{0}", e.Message);
            this.WriteLine(line);
        }

        private void TargetFinished(object sender, TargetFinishedEventArgs e)
        {
            string line = String.Format(CultureInfo.InvariantCulture, "Done building target \"{0}\" in project \"{1}\"", e.TargetName, e.ProjectFile);
            this.indent--;
            this.WriteLine(line);
        }

        private void TargetStarted(object sender, TargetStartedEventArgs e)
        {
            string line = String.Format(CultureInfo.InvariantCulture, "Target {0}:", e.TargetName);
            this.WriteLine(line);
            this.indent++;
        }

        private void BuildStarted(object sender, BuildStartedEventArgs e)
        {
            this.startTime = DateTime.Now;
            string line = String.Format(CultureInfo.InvariantCulture, "{0} {1}", e.Message, e.Timestamp);
            this.WriteLine(line);
            this.WriteLine("__________________________________________________");
        }

        private void BuildFinished(object sender, BuildFinishedEventArgs e)
        {
            this.WriteLine(e.Message);
            this.WriteLine(String.Format(CultureInfo.InvariantCulture, "{0} Warning(s) ", this.warnings));
            this.WriteLine(String.Format(CultureInfo.InvariantCulture, "{0} Error(s) ", this.errors) + Environment.NewLine + Environment.NewLine);

            TimeSpan s = DateTime.Now - this.startTime;
            this.WriteLine(String.Format(CultureInfo.InvariantCulture, "Time Elapsed {0}", s));
        }

        private void ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            string line = String.Format(CultureInfo.InvariantCulture, "ERROR {0}({1},{2}): ", e.File, e.LineNumber, e.ColumnNumber);
            this.WriteLine(this.ProcessLine(line));
            this.errors++;
        }

        private void WarningRaised(object sender, BuildWarningEventArgs e)
        {
            string line = String.Format(CultureInfo.InvariantCulture, "Warning {0}({1},{2}): ", e.File, e.LineNumber, e.ColumnNumber);
            this.WriteLine(this.ProcessLine(line));
            this.warnings++;
        }

        private void MessageRaised(object sender, BuildMessageEventArgs e)
        {
            if ((e.Importance == MessageImportance.High && IsVerbosityAtLeast(LoggerVerbosity.Minimal)) || (e.Importance == MessageImportance.Normal && IsVerbosityAtLeast(LoggerVerbosity.Normal)) || (e.Importance == MessageImportance.Low && IsVerbosityAtLeast(LoggerVerbosity.Detailed)))
            {
                this.WriteLine(this.ProcessLine(e.Message));
            }
        }

        private void ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            string targets = string.IsNullOrEmpty(e.TargetNames) ? "default" : e.TargetNames;
            string line = String.Format(CultureInfo.InvariantCulture, "Project \"{0}\" ({1} target(s)):", e.ProjectFile, targets);
            this.WriteLine(line + Environment.NewLine);

            if (IsVerbosityAtLeast(LoggerVerbosity.Diagnostic))
            {
                this.WriteLine("Initial Properties:");

                SortedDictionary<string, string> sortedProperties = new SortedDictionary<string, string>();
                foreach (DictionaryEntry k in e.Properties.Cast<DictionaryEntry>())
                {
                    sortedProperties.Add(k.Key.ToString(), k.Value.ToString());
                }

                foreach (var p in sortedProperties)
                {
                    bool matched = this.regExRules.Select(s => new Regex(s)).Select(r => r.Match(p.Key)).Any(m => m.Success);

                    if (matched)
                    {
                        this.WriteLine(p.Key + "\t = " + SecureChar.Repeat(p.Value.Length));
                    }
                    else
                    {
                        this.WriteLine(p.Key + "\t = " + p.Value);
                    }
                }
            }
        }

        private void ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            this.indent--;
            this.WriteLine(e.Message);
        }

        private void WriteLine(string line)
        {
            for (int i = this.indent; i > 0; i--)
            {
                this.streamWriter.Write("\t");
            }

            this.streamWriter.WriteLine(line);
        }

        private string ProcessLine(string line)
        {
            foreach (string s in this.regExRules)
            {
                Regex r = new Regex(s);
                line = r.Replace(line, SecureChar.Repeat(s.Length));
            }

            return line;
        }
    }
}