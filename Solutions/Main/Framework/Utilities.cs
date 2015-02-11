//-----------------------------------------------------------------------
// <copyright file="Utilities.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// The code was written by Jozsef Fejes (http://joco.name).
//-----------------------------------------------------------------------

namespace MSBuild.ExtensionPack
{
    using System;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Win32;

    public static class Utilities
    {
        /// <summary>
        /// For a 32 bit process, it returns the 32 bit HKLM\SOFTWARE registry key, otherwise the 64 bit one. May return null if
        /// it doesn't exist. Dispose of the return value.
        /// </summary>
        public static RegistryKey SoftwareRegistryNative
        {
            get
            {
                // no need to play with RegistryView, it is the simplest case
                return Registry.LocalMachine.OpenSubKey("SOFTWARE");
            }
        }

        /// <summary>
        /// For a 32 bit process, it returns the 64 bit HKLM\SOFTWARE registry key, otherwise the 32 bit one. May return null if
        /// it doesn't exist. Dispose of the return value.
        /// </summary>
        public static RegistryKey SoftwareRegistryNonnative
        {
            get
            {
                // a non-native registry only exists on 64 bit OS'es
                if (Environment.Is64BitOperatingSystem)
                {
                    // use RegistryView to get the other one
                    var view = Environment.Is64BitProcess ? RegistryView.Registry32 : RegistryView.Registry64;
                    using (var basekey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    {
                        return basekey.OpenSubKey("SOFTWARE");
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the 32 bit HKLM\SOFTWARE registry key. May return null if it doesn't exist. Dispose of the return value.
        /// </summary>
        public static RegistryKey SoftwareRegistry32Bit
        {
            get
            {
                // simple logic
                if (Environment.Is64BitProcess)
                {
                    return SoftwareRegistryNonnative;
                }

                return SoftwareRegistryNative;
            }
        }

        /// <summary>
        /// Returns the 64 bit HKLM\SOFTWARE registry key. May return null if it doesn't exist. Dispose of the return value.
        /// </summary>
        public static RegistryKey SoftwareRegistry64Bit
        {
            get
            {
                // simple logic
                if (Environment.Is64BitProcess)
                {
                    return SoftwareRegistryNative;
                }

                return SoftwareRegistryNonnative;
            }
        }

        /// <summary>
        /// Executes a tool, logs standard error and a nonzero exit code as errors, returns the output and optionally logs that
        /// as well.
        /// </summary>
        /// <param name="log">used for logging</param>
        /// <param name="executable">the name of the executable</param>
        /// <param name="args">the command line arguments</param>
        /// <param name="logOutput">should we log the output in real time</param>
        /// <returns>the output of the tool</returns>
        public static string ExecuteWithLogging(TaskLoggingHelper log, string executable, string args, bool logOutput)
        {
            if (log == null)
            {
                throw new ArgumentNullException("log");
            }

            log.LogMessage(MessageImportance.Low, "Executing tool: {0} {1}", executable, args);

            var exec = new ShellWrapper(executable, args);

            // stderr is logged as errors
            exec.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    log.LogError(e.Data);
                }
            };

            // stdout is logged normally if requested
            if (logOutput)
            {
                exec.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        log.LogMessage(MessageImportance.Normal, e.Data);
                    }
                };
            }

            // execute the process
            exec.Execute();

            // check the exit code
            if (exec.ExitCode != 0)
            {
                log.LogError("The tool {0} exited with error code {1}", executable, exec.ExitCode);
            }

            return exec.StandardOutput;
        }

        internal class CommandLineBuilder2 : CommandLineBuilder
        {
            /// <summary>
            /// Appends a fixed argument. This means that it is appended even if it is empty (as ""). It is quoted if necessary.
            /// </summary>
            /// <param name="value">the string to append</param>
            public void AppendFixedParameter(string value)
            {
                this.AppendSpaceIfNotEmpty();
                if (string.IsNullOrEmpty(value))
                {
                    this.AppendTextUnquoted("\"\"");
                }
                else
                {
                    this.AppendTextWithQuoting(value);
                }
            }
        }
    }
}
