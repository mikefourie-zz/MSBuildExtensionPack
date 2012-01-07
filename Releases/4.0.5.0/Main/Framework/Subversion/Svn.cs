//-----------------------------------------------------------------------
// <copyright file="Svn.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
/*
 * TODO:
 * - recognize more svn installations (ankh?, collabnet, sliksvn, visualsvn, wandisco, win32svn)
 * - implement the actual tasks
 * - figure out how to do 32/64 bit stuff in the 3.5 release
 * - documentation
 */
namespace MSBuild.ExtensionPack.Subversion
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.Win32;

    public class Svn : BaseTask
    {
        private static readonly string SvnPath = FindSvnPath();

        private const string SvnExecutableName = "svn.exe";
        private const string SvnVersionExecutableName = "svnversion.exe";

        protected override void InternalExecute()
        {
            throw new NotImplementedException(SvnPath);
        }

        #region finding SVN command-line tools
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
    }
}
