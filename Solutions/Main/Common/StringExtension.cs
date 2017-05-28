//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtension.cs">(c) 2017 Mike Fourie and Contributors (http://www.MSBuildExtensionPack.com) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack
{
    using System;
    using System.Globalization;
    using System.Text;

    internal static class StringExtension
    {
        public static string AppendFormat(this string originalValue, IFormatProvider provider, string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format) || args == null)
            {
                return originalValue ?? string.Empty;
            }

            StringBuilder builder = new StringBuilder(originalValue ?? string.Empty);
            builder.AppendFormat(provider, format, args);
            return builder.ToString();
        }

        public static string AppendFormat(this string originalValue, string format, params object[] args)
        {
            return AppendFormat(originalValue, CultureInfo.CurrentCulture, format, args);
        }
    }
}