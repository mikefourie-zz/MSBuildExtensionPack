//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="Extensions.cs">(c) 2017 Mike Fourie and Contributors (http://www.MSBuildExtensionPack.com) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Communication.Extended
{
    using System;

    /// <summary>
    /// Helper class used to convert FILETIME to DateTime
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Converts given datetime in FILETIME struct format and convert it to .Net DateTime.
        /// </summary>
        /// <param name="time">The given time in FileTime structure format</param>
        /// <returns>The DateTime equivalent of the given fileTime</returns>
        public static DateTime? ToDateTime(this NativeMethods.FILETIME time)
        {
            if (time.dwHighDateTime == 0 && time.dwLowDateTime == 0)
            {
                return null;
            }

            unchecked
            {
                uint low = (uint)time.dwLowDateTime;
                long ft = ((long)time.dwHighDateTime) << 32 | low;
                return DateTime.FromFileTimeUtc(ft);
            }
        }
    }
}