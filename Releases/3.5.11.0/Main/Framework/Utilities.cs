//-----------------------------------------------------------------------
// <copyright file="Utilities.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
// The code was written by Jozsef Fejes (http://joco.name).
//-----------------------------------------------------------------------

namespace MSBuild.ExtensionPack
{
    using System;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Win32;

    internal static class Utilities
    {
        /// <summary>
        /// Runs the specified delegate with the HKLM\SOFTWARE key in the 32 and/or 64 bit registry. If both parameters are true,
        /// then the native registry will be used first (according to whether the current process is a 32 or 64 bit one). There is
        /// no 64 bit registry in a 32 bit OS, so naturally that is never checked.
        /// </summary>
        /// <remarks>
        /// Unlike in the 4.0 release, a 32 bit process can't look into the 64 bit registry because of the lack of native support
        /// for some required operations.
        /// </remarks>
        /// <param name="try32">try 32 bit registry</param>
        /// <param name="try64">try 64 bit registry</param>
        /// <param name="inner">the function to check the registry, should expect the HKLM\SOFTWARE key and return a path</param>
        /// <returns>the path if it is found, null otherwise</returns>
        public static string TryRegistry3264(bool try32, bool try64, Func<RegistryKey, string> inner)
        {
            string ret;

            // tricky, huh?
            var is64bitProcess = IntPtr.Size == 8;

            // try the native registry
            if ((is64bitProcess && try64) || (!is64bitProcess && try32))
            {
                using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE"))
                {
                    if ((ret = inner(key)) != null)
                    {
                        return ret;
                    }
                }
            }

            // if we are on 64 bits, then we can look into the 32 bit SOFTWARE\Wow6432Node key, and that's the best we can do
            if (is64bitProcess && try32)
            {
                using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node"))
                {
                    if ((ret = inner(key)) != null)
                    {
                        return ret;
                    }
                }
            }

            return null;
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

        public class CommandLineBuilder2 : CommandLineBuilder
        {
            /// <summary>
            /// Appends a fixed argument. This means that it is appended even if it is empty (as ""). It is quoted if necessary.
            /// </summary>
            /// <param name="value">the string to append</param>
            public void AppendFixedParameter(string value)
            {
                AppendSpaceIfNotEmpty();
                if (string.IsNullOrEmpty(value))
                {
                    AppendTextUnquoted("\"\"");
                }
                else
                {
                    AppendTextWithQuoting(value);
                }
            }
        }
    }
}
