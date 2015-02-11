//-----------------------------------------------------------------------
// <copyright file="StringExtensions.cs">(c) http://www.msbuildextensionpack.com. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace MSBuild.ExtensionPack.Loggers.Extended
{
    using System.Text;

    /// <summary>
    /// StringExtensions
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Repeat a character
        /// </summary>
        /// <param name="chatToRepeat">The charToRepeat</param>
        /// <param name="repeat">The number of times to repeat</param>
        /// <returns>Repeated chatToRepeat</returns>
        public static string Repeat(this char chatToRepeat, int repeat)
        {
            return new string(chatToRepeat, repeat);
        }

        /// <summary>
        /// Repeat a string
        /// </summary>
        /// <param name="stringToRepeat">The stringToRepeat</param>
        /// <param name="repeat">The number of times to repeat</param>
        /// <returns>Repeated stringToRepeat</returns>
        public static string Repeat(this string stringToRepeat, int repeat)
        {
            var builder = new StringBuilder(repeat);
            for (int i = 0; i < repeat; i++)
            {
                builder.Append(stringToRepeat);
            }

            return builder.ToString();
        }
    }
}
