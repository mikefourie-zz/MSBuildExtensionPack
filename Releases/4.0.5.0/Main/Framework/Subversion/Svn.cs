//-----------------------------------------------------------------------
// <copyright file="Svn.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
/*
 * TODO:
 * - recognize more svn installations (ankh?, collabnet, sliksvn, visualsvn, wandisco, win32svn)
 * - implement the actual tasks
 * - required attribute validation
 * - documentation
 */
namespace MSBuild.ExtensionPack.Subversion
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Win32;

    /// <summary>
    /// <b>Valid TaskActions are:</b>
    /// <para><i>Version</i> (<b>Required: </b>Item <b>Output: </b>Info)</para>
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Project ToolsVersion="4.0" DefaultTargets="Default" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    ///     <PropertyGroup>
    ///         <TPath>$(MSBuildProjectDirectory)\..\MSBuild.ExtensionPack.tasks</TPath>
    ///         <TPath Condition="Exists('$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks')">$(MSBuildProjectDirectory)\..\..\Common\MSBuild.ExtensionPack.tasks</TPath>
    ///     </PropertyGroup>
    ///     <Import Project="$(TPath)"/>
    ///     <Target Name="Default">
    ///         <!-- Version -->
    ///         <MSBuild.ExtensionPack.Subversion.Svn TaskAction="Version" Item="c:\path\to\working\copy">
    ///             <Output TaskParameter="Info" ItemName="Info"/>
    ///         </MSBuild.ExtensionPack.Subversion.Svn>
    ///         <Message Text="MinRevision: %(Info.MinRevision), MaxRevision: %(Info.MaxRevision), IsMixed: %(Info.IsMixed), IsModified: %(Info.IsModified), IsSwitched: %(Info.IsSwitched), IsPartial: %(Info.IsPartial)"/>
    ///     </Target>
    /// </Project>
    /// ]]></code>    
    /// </example>
    public class Svn : BaseTask
    {
        protected const string SvnExecutableName = "svn.exe";
        protected const string SvnVersionExecutableName = "svnversion.exe";

        private const string VersionTaskAction = "Version";

        private static readonly string SvnPath = FindSvnPath();

        [DropdownValue(VersionTaskAction)]
        public override string TaskAction
        {
            get { return base.TaskAction; }
            set { base.TaskAction = value; }
        }

        [TaskAction(VersionTaskAction, true)]
        public ITaskItem Item { get; set; }

        [Output]
        [TaskAction(VersionTaskAction, false)]
        public ITaskItem Info { get; set; }

        protected override void InternalExecute()
        {
            if (!this.TargetingLocalMachine())
            {
                return;
            }

            if (SvnPath == null)
            {
                Log.LogError("A supported SVN client installation was not found");
                return;
            }

            switch (this.TaskAction)
            {
                case VersionTaskAction:
                    this.Version();
                    break;

                default:
                    this.Log.LogError("Invalid TaskAction passed: {0}", this.TaskAction);
                    return;
            }
        }

        #region finding SVN command-line tools, all static
        /// <summary>
        /// Checks if a path is a valid SVN path where svn.exe and svnversion.exe can be found.
        /// </summary>
        /// <param name="dir">the path to check</param>
        /// <returns><paramref name="dir"/> if it is valid, null otherwise</returns>
        private static bool IsSvnPath(string dir)
        {
            return Path.IsPathRooted(dir) // for a consistent behavior
                && File.Exists(Path.Combine(dir, SvnExecutableName))
                && File.Exists(Path.Combine(dir, SvnVersionExecutableName));
        }

        /// <summary>
        /// Tries to find an SVN installation in the PATH environment variable.
        /// </summary>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TryEnvironmentPath()
        {
            var paths = Environment.GetEnvironmentVariable("PATH");
            if (paths != null)
            {
                return paths.Split(Path.PathSeparator).FirstOrDefault(path => IsSvnPath(path));
            }

            return null;
        }

        /// <summary>
        /// Tries to find an SVN installation in all Cygwin installations.
        /// </summary>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TryCygwin()
        {
            // Cygwin installations are registered at HKLM\SOFTWARE\Cygwin\Installations (32 bit only), and they are NT object
            // manager paths. SVN is installed under /usr/bin.
            using (var basekey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            using (var key = basekey.OpenSubKey(@"SOFTWARE\Cygwin\Installations"))
            {
                foreach (var value in key.GetValueNames().Select(name => key.GetValue(name) as string))
                {
                    if (value == null)
                    {
                        continue;
                    }

                    if (value.StartsWith(@"\??\UNC\", StringComparison.OrdinalIgnoreCase))
                    {
                        // NT object manager UNC path
                        var dir = Path.Combine(@"\\" + value.Substring(8), "usr", "bin");
                        if (IsSvnPath(dir))
                        {
                            return dir;
                        }
                    }
                    else if (value.StartsWith(@"\??\", StringComparison.OrdinalIgnoreCase))
                    {
                        // NT object manager local drive path
                        var dir = Path.Combine(value.Substring(4), "usr", "bin");
                        if (IsSvnPath(dir))
                        {
                            return dir;
                        }
                    }
                    else
                    {
                        // maybe a regular path
                        var dir = Path.Combine(value, "usr", "bin");
                        if (IsSvnPath(dir))
                        {
                            return dir;
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Tries to find a TortoiseSVN installation from the given registry key name.
        /// </summary>
        /// <param name="view">registry view</param>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TryTortoiseSvn(RegistryView view)
        {
            // HKLM\SOFTWARE\TortoiseSVN!Directory points to the base installation dir, binaries are under \bin
            using (var basekey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
            using (var key = basekey.OpenSubKey(@"SOFTWARE\TortoiseSVN"))
            {
                var dir = key.GetValue("Directory") as string;
                if (dir != null)
                {
                    dir = Path.Combine(dir, "bin");
                    if (IsSvnPath(dir))
                    {
                        return dir;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Tries to find an SVN installation from all possible places.
        /// </summary>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string FindSvnPath()
        {
            string ret;

            // PATH environment variable
            if ((ret = TryEnvironmentPath()) != null)
            {
                return ret;
            }

            // Cygwin
            if ((ret = TryCygwin()) != null)
            {
                return ret;
            }

            // TortoiseSVN native
            if ((ret = TryTortoiseSvn(RegistryView.Default)) != null)
            {
                return ret;
            }

            // TortoiseSVN 32<->64
            if (Environment.Is64BitOperatingSystem)
            {
                // in a 32 or 64 bit process, the default view above refers to the 32 or 64 bit registry respectively, but
                // on a 64 bit system, a 32 or 64 bit process can also look into the 64 or 32 bit registry respectively
                var view = Environment.Is64BitProcess ? RegistryView.Registry32 : RegistryView.Registry64;
                if ((ret = TryTortoiseSvn(view)) != null)
                {
                    return ret;
                }
            }

            // didn't find it, will report it as an error from where it's used
            return null;
        }
        #endregion

        #region task implementations
        private void Version()
        {
            var output = new StringBuilder();
            this.Execute(SvnVersionExecutableName, string.Format(CultureInfo.CurrentCulture, "-q \"{0}\"", this.Item.ItemSpec), output);

            if (!this.Log.HasLoggedErrors)
            {
                var s = output.ToString();

                // unversioned/uncommitted, sadly there's no better way to tell, at least the help is explicit about these strings
                if (s.StartsWith("Unversioned", StringComparison.Ordinal) || s.StartsWith("Uncommitted", StringComparison.Ordinal))
                {
                    return;
                }

                // decode the response
                var m = Regex.Match(s, @"^\s?(?<min>[0-9]+)(:(?<max>[0-9]+))?(?<sw>[MSP]*)\s?$");
                if (!m.Success)
                {
                    Log.LogError("Invalid output from SVN tool");
                    return;
                }

                // fill up the output
                this.Info = new TaskItem(this.Item);
                this.Info.SetMetadata("MinRevision", m.Groups["min"].Value);
                var mixed = !string.IsNullOrEmpty(m.Groups["max"].Value);
                this.Info.SetMetadata("MaxRevision", m.Groups[mixed ? "max" : "min"].Value);
                this.Info.SetMetadata("IsMixed", mixed.ToString());
                this.Info.SetMetadata("IsModified", m.Groups["sw"].Value.Contains("M").ToString());
                this.Info.SetMetadata("IsSwitched", m.Groups["sw"].Value.Contains("S").ToString());
                this.Info.SetMetadata("IsPartial", m.Groups["sw"].Value.Contains("P").ToString());
            }
        }
        #endregion

        #region helper methods
        /// <summary>
        /// Executes a tool. Standard error is output as task errors. Standard output is either gathered in
        /// <paramref name="output"/> if it's not null or output as task messages. A non-zero exit code is also treated as an
        /// error.
        /// </summary>
        /// <param name="executable">the name of the executable</param>
        /// <param name="args">the command line arguments</param>
        /// <param name="output">will gather output if not null</param>
        private void Execute(string executable, string args, StringBuilder output)
        {
            var filename = Path.Combine(SvnPath, executable);
            Log.LogMessage(MessageImportance.Low, "Executing tool: {0} {1}", filename, args);

            // set up the process
            using (var proc = new Process())
            {
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true
                };

                // handler stderr
                proc.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        return;
                    }

                    Log.LogError(e.Data);
                };

                // handler stdout
                proc.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        return;
                    }

                    if (output != null)
                    {
                        output.AppendLine(e.Data);
                    }
                    else
                    {
                        Log.LogMessage(MessageImportance.Normal, e.Data);
                    }
                };

                // run the process
                proc.Start();
                proc.StandardInput.Close();
                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                {
                    Log.LogError("The tool {0} exited with error code {1}", executable, proc.ExitCode);
                }
            }
        }
        #endregion
    }
}
