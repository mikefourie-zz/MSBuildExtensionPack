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
    using System.Text;
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
        private static readonly char[] fileLoggerParameterDelimiters = new[] { ';' };
        private static readonly char[] fileLoggerParameterValueSplitCharacter = new[] { '=' };
        private readonly ICollection<string> regExRules = new Collection<string>();
        private StreamWriter fileWriter;
        private string logFileName;
        private string ruleFileName;
        private bool append;
        private Encoding encoding;
        private int indent;
        private int warnings;
        private int errors;
        private DateTime startTime;

        /// <summary>
        /// Initialize Override
        /// </summary>
        /// <param name="eventSource">IEventSource</param>
        public override void Initialize(IEventSource eventSource)
        {
            this.logFileName = "securemsbuild.log";
            this.encoding = Encoding.Default;

            this.InitializeFileLogger();

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

        /// <summary>
        /// Shutdown() is guaranteed to be called by MSBuild at the end of the build, after all 
        /// events have been raised.
        /// </summary>
        public override void Shutdown()
        {
            if (this.fileWriter != null)
            {
                this.fileWriter.Close();
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
                string[] strArray = this.Parameters.Split(fileLoggerParameterDelimiters);
                foreach (string[] strArray2 in from t in strArray where t.Length > 0 select t.Split(fileLoggerParameterValueSplitCharacter))
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
                case "RULEFILE":
                    this.ruleFileName = parameterValue;
                    break;
                case "VERBOSITY":
                    this.Verbosity = (LoggerVerbosity)Enum.Parse(typeof(LoggerVerbosity), parameterValue);
                    break;
                case "APPEND":
                    this.append = Convert.ToBoolean(parameterValue, CultureInfo.InvariantCulture);
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
            string parameters = this.Parameters;
            if (parameters != null)
            {
                this.Parameters = "FORCENOALIGN;" + parameters;
            }
            else
            {
                this.Parameters = "FORCENOALIGN;";
            }

            this.ParseFileLoggerParameters();

            try
            {
                // if no rule file is supplied we add a single generic password regex
                if (string.IsNullOrEmpty(this.ruleFileName))
                {
                    this.regExRules.Add("(?i:.*password.*)");
                }
                else
                {
                    FileInfo ruleFile = new FileInfo(this.ruleFileName);

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
                }

                this.fileWriter = new StreamWriter(this.logFileName, this.append, this.encoding);
                this.fileWriter.AutoFlush = true;
            }
            catch (Exception exception)
            {
                if (NotExpectedException(exception))
                {
                    throw;
                }

                string message = string.Format(CultureInfo.InvariantCulture, "Invalid File Logger File {0}. {1}", this.logFileName, exception.Message);
                if (this.fileWriter != null)
                {
                    this.fileWriter.Close();
                }

                throw new LoggerException(message, exception.InnerException);
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
                this.fileWriter.Write("\t");
            }

            this.fileWriter.WriteLine(line);
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