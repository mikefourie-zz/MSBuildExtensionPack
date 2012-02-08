//-----------------------------------------------------------------------
// <copyright file="Utilities.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------

namespace MSBuild.ExtensionPack
{
    using System;
    using Microsoft.Win32;

    internal static class Utilities
    {
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
        public static string TryRegistry3264(bool try32, bool try64, Func<RegistryKey, string> inner)
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
                    using (var key = basekey.OpenSubKey("SOFTWARE"))
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
    }
}
