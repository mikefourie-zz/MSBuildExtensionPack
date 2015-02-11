//-----------------------------------------------------------------------
// <copyright file="Extensions.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
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