//-----------------------------------------------------------------------
// <copyright file="StringExtension.cs">(c) http://www.codeplex.com/MSBuildExtensionPack. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack
{
    using System;
    using System.Globalization;
    using System.Text;

    internal static class StringExtension
    {
        public static string AppendFormat(this string originalValue, IFormatProvider provider, string format, params object[] args)
        {
            if (String.IsNullOrEmpty(format) || args == null)
            {
                return originalValue ?? String.Empty;
            }

            StringBuilder builder = new StringBuilder(originalValue ?? String.Empty);
            builder.AppendFormat(provider, format, args);
            return builder.ToString();
        }

        public static string AppendFormat(this string originalValue, string format, params object[] args)
        {
            return AppendFormat(originalValue, CultureInfo.CurrentCulture, format, args);
        }
    }
}