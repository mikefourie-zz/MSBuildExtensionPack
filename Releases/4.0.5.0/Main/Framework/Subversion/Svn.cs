//-----------------------------------------------------------------------
// <copyright file="Svn.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
/*
 * TODO:
 * - recognize more svn installations
 *   - visualsvn
 *   - wandisco
 *   - win32svn
 * - implement the actual tasks
 *   - info
 *   - update
 *   - propget
 *   - propset
 *   - what else?
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
    /// <remarks>
    /// The task needs a command-line SVN client (svn.exe and svnversion.exe) to be available. The following are supported
    /// (but versions other than those indicated may also work):
    /// <list type="bullet">
    ///     <item>any SVN client in the PATH environment variable, so the user can set a preference</item>
    ///     <item>Cygwin 1.7, with the subversion package installed</item>
    ///     <item>TortoiseSVN 1.7, with the command line component installed</item>
    ///     <item>CollabNet Subversion Client 1.7</item>
    ///     <item>Slik SVN 1.7, with the Subversion client component installed</item>
    /// </list>
    /// </remarks>
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
    ///         <Message Text="MinRevision: %(Info.MinRevision), MaxRevision: %(Info.MaxRevision), IsMixed: %(Info.IsMixed), IsModified: %(Info.IsModified), IsSwitched: %(Info.IsSwitched), IsPartial: %(Info.IsPartial), IsClean: %(Info.IsClean)"/>
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

        [Required]
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
        /// <param name="software">the HKLM\SOFTWARE registry key</param>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TryCygwin(RegistryKey software)
        {
            // Cygwin installations are registered at HKLM\SOFTWARE\Cygwin\Installations (32 bit only), and they are NT object
            // manager paths. SVN is installed under /usr/bin.
            using (var key = software.OpenSubKey(@"Cygwin\Installations"))
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
        /// Tries to find a TortoiseSVN installation in the given registry view.
        /// </summary>
        /// <param name="software">the HKLM\SOFTWARE registry key</param>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TryTortoiseSvn(RegistryKey software)
        {
            // HKLM\SOFTWARE\TortoiseSVN!Directory points to the base installation dir, binaries are under \bin
            using (var key = software.OpenSubKey(@"TortoiseSVN"))
            {
                if (key != null)
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
                }

                return null;
            }
        }

        /// <summary>
        /// Tries to find a CollabNet Subversion Client installation in the given registry view.
        /// </summary>
        /// <param name="software">the HKLM\SOFTWARE registry key</param>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TryCollabNet(RegistryKey software)
        {
            // HKLM\SOFTWARE\CollabNet\Subversion!Client Version contains the version number
            // HKLM\SOFTWARE\CollabNet\Subversion\[version]\Client!Install Location contains the actual directory
            using (var key = software.OpenSubKey(@"CollabNet\Subversion"))
            {
                if (key != null)
                {
                    var version = key.GetValue("Client Version") as string;
                    if (version != null)
                    {
                        using (var subkey = key.OpenSubKey(version + @"\Client"))
                        {
                            var dir = subkey.GetValue(@"Install Location") as string;
                            if (IsSvnPath(dir))
                            {
                                return dir;
                            }
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Tries to find a SlikSvn installation in the given registry view.
        /// </summary>
        /// <param name="software">the HKLM\SOFTWARE registry key</param>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TrySlikSvn(RegistryKey software)
        {
            // HKLM\SOFTWARE\SlikSvn\Install!Location points to the binaries directory
            using (var key = software.OpenSubKey(@"SlikSvn\Install"))
            {
                if (key != null)
                {
                    var dir = key.GetValue("Location") as string;
                    if (dir != null)
                    {
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
        /// Runs the specified delegate with the HKLM\SOFTWARE key in the 32 and/or 64 bit registry. If both parameters are true,
        /// then the native registry will be used first (according to whether the current process is a 32 or 64 bit one). There is
        /// no 64 bit registry in a 32 bit OS, so naturally that is never checked.
        /// </summary>
        /// <remarks>
        /// We could use a RegistryView parameter in the delegate, but instead we use the HKLM\SOFTWARE key itself because it
        /// allows for easier migration to the 3.5 release where we can use HKLM\SOFTWARE\Wow6432Node only.
        /// </remarks>
        /// <param name="try32">try 32 bit registry</param>
        /// <param name="try64">try 64 bit registry</param>
        /// <param name="inner">the function to check the registry, should expect the HKLM\SOFTWARE key and return a path</param>
        /// <returns>the path if it is found, null otherwise</returns>
        private static string TryRegistry3264(bool try32, bool try64, Func<RegistryKey, string> inner)
        {
            string ret;

            // try the native registry
            if ((Environment.Is64BitProcess && try64) || (!Environment.Is64BitProcess && try32))
            {
                using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE"))
                {
                    if ((ret = inner(key)) != null)
                    {
                        return ret;
                    }
                }
            }

            // try the non-native registry if it exists
            if (Environment.Is64BitOperatingSystem)
            {
                if ((Environment.Is64BitProcess && try32) || (!Environment.Is64BitProcess && try64))
                {
                    var view = Environment.Is64BitProcess ? RegistryView.Registry32 : RegistryView.Registry64;
                    using (var basekey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE"))
                    {
                        if ((ret = inner(key)) != null)
                        {
                            return ret;
                        }
                    }
                }
            }

            return null;
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
            if ((ret = TryRegistry3264(true, false, TryCygwin)) != null)
            {
                return ret;
            }

            // TortoiseSVN
            if ((ret = TryRegistry3264(true, true, TryTortoiseSvn)) != null)
            {
                return ret;
            }

            // CollabNet
            if ((ret = TryRegistry3264(true, true, TryCollabNet)) != null)
            {
                return ret;
            }

            // SlikSvn
            if ((ret = TryRegistry3264(true, true, TrySlikSvn)) != null)
            {
                return ret;
            }

            // didn't find it, will report it as an error from where it's used
            return null;
        }
        #endregion

        #region task implementations
        private void Version()
        {
            if (this.Item == null)
            {
                Log.LogError("The Item parameter is required");
                return;
            }

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

                var mixed = !string.IsNullOrEmpty(m.Groups["max"].Value);
                var sw = m.Groups["sw"].Value;

                // fill up the output
                this.Info = new TaskItem(this.Item);
                this.Info.SetMetadata("MinRevision", m.Groups["min"].Value);
                this.Info.SetMetadata("MaxRevision", m.Groups[mixed ? "max" : "min"].Value);
                this.Info.SetMetadata("IsMixed", mixed.ToString());
                this.Info.SetMetadata("IsModified", sw.Contains("M").ToString());
                this.Info.SetMetadata("IsSwitched", sw.Contains("S").ToString());
                this.Info.SetMetadata("IsPartial", sw.Contains("P").ToString());
                this.Info.SetMetadata("IsClean", (!mixed && sw.Length == 0).ToString());
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
